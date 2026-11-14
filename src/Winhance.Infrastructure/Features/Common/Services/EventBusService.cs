using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Interfaces;

namespace Winhance.Infrastructure.Features.Common.Services
{
    /// <summary>
    /// Event bus service for decoupled communication between components
    /// </summary>
    public class EventBusService : IEventBusService
    {
        private readonly ConcurrentDictionary<Type, ConcurrentBag<IEventHandler>> _handlers;
        private readonly SemaphoreSlim _publishSemaphore;

        public EventBusService()
        {
            _handlers = new ConcurrentDictionary<Type, ConcurrentBag<IEventHandler>>();
            _publishSemaphore = new SemaphoreSlim(1, 1);
        }

        public void Subscribe<T>(IEventHandler<T> handler) where T : class
        {
            ArgumentNullException.ThrowIfNull(handler);
            
            var eventType = typeof(T);
            _handlers.AddOrUpdate(eventType,
                new ConcurrentBag<IEventHandler> { handler },
                (key, existingHandlers) =>
                {
                    existingHandlers.Add(handler);
                    return existingHandlers;
                });
        }

        public void Unsubscribe<T>(IEventHandler<T> handler) where T : class
        {
            ArgumentNullException.ThrowIfNull(handler);
            
            var eventType = typeof(T);
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                var updatedHandlers = new ConcurrentBag<IEventHandler>(
                    handlers.Where(h => !ReferenceEquals(h, handler)));
                
                if (updatedHandlers.IsEmpty)
                {
                    _handlers.TryRemove(eventType, out _);
                }
                else
                {
                    _handlers.TryUpdate(eventType, updatedHandlers, handlers);
                }
            }
        }

        public async Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(eventData);
            
            var eventType = typeof(T);
            
            if (!_handlers.TryGetValue(eventType, out var handlers) || handlers.IsEmpty)
            {
                return; // No handlers registered
            }

            await _publishSemaphore.WaitAsync(cancellationToken);
            try
            {
                var tasks = handlers
                    .OfType<IEventHandler<T>>()
                    .Select(handler => HandleEventSafely(handler, eventData, cancellationToken))
                    .ToArray();

                await Task.WhenAll(tasks);
            }
            finally
            {
                _publishSemaphore.Release();
            }
        }

        public void Publish<T>(T eventData) where T : class
        {
            _ = PublishAsync(eventData);
        }

        private async Task HandleEventSafely<T>(IEventHandler<T> handler, T eventData, CancellationToken cancellationToken) where T : class
        {
            try
            {
                await handler.HandleAsync(eventData, cancellationToken);
            }
            catch (Exception)
            {
                // Log error but don't throw - other handlers should still process
                // In a production app, you'd want to log this properly
            }
        }

        // Missing interface methods
        public void Subscribe<T>(Func<T, Task> handler) where T : class
        {
            var wrapperHandler = new FuncEventHandler<T>(handler);
            Subscribe(wrapperHandler);
        }

        public void ClearHandlers<T>() where T : class
        {
            var eventType = typeof(T);
            _handlers.TryRemove(eventType, out _);
        }

        public void ClearAllHandlers()
        {
            _handlers.Clear();
        }

        public int GetHandlerCount<T>() where T : class
        {
            var eventType = typeof(T);
            return _handlers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }

        public void Dispose()
        {
            _handlers.Clear();
            _publishSemaphore?.Dispose();
        }
    }

    /// <summary>
    /// Wrapper for Func<T, Task> handlers
    /// </summary>
    internal class FuncEventHandler<T> : IEventHandler<T> where T : class
    {
        private readonly Func<T, Task> _handler;

        public FuncEventHandler(Func<T, Task> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public async Task HandleAsync(T eventData, CancellationToken cancellationToken = default)
        {
            await _handler(eventData);
        }

        public async Task HandleAsync(object eventData, CancellationToken cancellationToken = default)
        {
            if (eventData is T typedData)
            {
                await HandleAsync(typedData, cancellationToken);
            }
        }
    }
}

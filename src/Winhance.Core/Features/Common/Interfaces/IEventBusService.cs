using System;
using System.Threading;
using System.Threading.Tasks;

namespace Winhance.Core.Features.Common.Interfaces
{
    /// <summary>
    /// Event bus service interface for decoupled communication between components
    /// </summary>
    public interface IEventBusService
    {
        /// <summary>
        /// Subscribe to an event type with a handler
        /// </summary>
        void Subscribe<T>(IEventHandler<T> handler) where T : class;

        /// <summary>
        /// Subscribe to an event type with an action
        /// </summary>
        void Subscribe<T>(Func<T, Task> handler) where T : class;

        /// <summary>
        /// Unsubscribe a handler from an event type
        /// </summary>
        void Unsubscribe<T>(IEventHandler<T> handler) where T : class;

        /// <summary>
        /// Publish an event to all subscribers
        /// </summary>
        Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Clear all handlers for a specific event type
        /// </summary>
        void ClearHandlers<T>() where T : class;

        /// <summary>
        /// Clear all handlers for all event types
        /// </summary>
        void ClearAllHandlers();

        /// <summary>
        /// Get the number of handlers for a specific event type
        /// </summary>
        int GetHandlerCount<T>() where T : class;
    }

    /// <summary>
    /// Interface for event handlers
    /// </summary>
    public interface IEventHandler
    {
        /// <summary>
        /// Handle the event
        /// </summary>
        Task HandleAsync(object eventData, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Generic interface for typed event handlers
    /// </summary>
    public interface IEventHandler<T> : IEventHandler where T : class
    {
        /// <summary>
        /// Handle the typed event
        /// </summary>
        Task HandleAsync(T eventData, CancellationToken cancellationToken = default);
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Winhance.WPF.Features.FileManager.ViewModels;

namespace Winhance.WPF.Features.FileManager.Controls
{
    public class EnhancedContextMenu : ContextMenu
    {
        public EnhancedContextMenu()
        {
            Loaded += EnhancedContextMenu_Loaded;
        }

        private void EnhancedContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            // Add dynamic menu items based on selection
        }

        protected override void OnOpened(RoutedEventArgs e)
        {
            base.OnOpened(e);
            
            // Refresh menu items when opened
            UpdateMenuItems();
        }

        private void UpdateMenuItems()
        {
            // Get the view model from the placement target
            if (PlacementTarget is FrameworkElement element && element.DataContext is DualPaneBrowserViewModel vm)
            {
                // Clear existing items
                Items.Clear();
                
                // Add standard items
                AddStandardMenuItems(vm);
                
                // Add advanced selection items
                AddSelectionMenuItems(vm);
                
                // Add copy/move to other pane items
                AddPaneTransferItems(vm);
                
                // Add advanced operations
                AddAdvancedOperations(vm);
            }
        }

        private void AddStandardMenuItems(DualPaneBrowserViewModel vm)
        {
            // Open
            var openItem = new MenuItem
            {
                Header = "Open",
                Icon = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Icons/FolderOpen.png"))
                }
            };
            openItem.Click += (s, e) => vm.OpenItemCommand.Execute(vm.IsLeftPaneActive ? vm.SelectedLeftItem : vm.SelectedRightItem);
            Items.Add(openItem);

            Items.Add(new Separator());

            // Copy
            var copyItem = new MenuItem
            {
                Header = "Copy",
                InputGestureText = "Ctrl+C",
                Icon = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Icons/ContentCopy.png"))
                }
            };
            copyItem.Click += (s, e) => vm.CopyToClipboardCommand.Execute(null);
            Items.Add(copyItem);

            // Cut
            var cutItem = new MenuItem
            {
                Header = "Cut",
                InputGestureText = "Ctrl+X",
                Icon = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Icons/ContentCut.png"))
                }
            };
            cutItem.Click += (s, e) => vm.CutToClipboardCommand.Execute(null);
            Items.Add(cutItem);

            // Paste
            var pasteItem = new MenuItem
            {
                Header = "Paste",
                InputGestureText = "Ctrl+V",
                Icon = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Icons/ContentPaste.png"))
                }
            };
            pasteItem.Click += (s, e) => vm.PasteFromClipboardCommand.Execute(null);
            Items.Add(pasteItem);

            Items.Add(new Separator());

            // Delete
            var deleteItem = new MenuItem
            {
                Header = "Delete",
                InputGestureText = "Del",
                Icon = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Icons/Delete.png"))
                }
            };
            deleteItem.Click += (s, e) => vm.DeleteCommand.Execute(null);
            Items.Add(deleteItem);

            // Rename
            var renameItem = new MenuItem
            {
                Header = "Rename",
                InputGestureText = "F2",
                Icon = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Icons/RenameBox.png"))
                }
            };
            renameItem.Click += (s, e) => vm.StartRenameCommand.Execute(null);
            Items.Add(renameItem);

            Items.Add(new Separator());

            // New Folder
            var newFolderItem = new MenuItem
            {
                Header = "New Folder",
                InputGestureText = "Ctrl+Shift+N",
                Icon = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Icons/FolderPlus.png"))
                }
            };
            newFolderItem.Click += (s, e) => vm.NewFolderCommand.Execute(null);
            Items.Add(newFolderItem);
        }

        private void AddSelectionMenuItems(DualPaneBrowserViewModel vm)
        {
            Items.Add(new Separator());

            // Select submenu
            var selectMenu = new MenuItem { Header = "Select" };
            
            var selectAllItem = new MenuItem { Header = "Select All", InputGestureText = "Ctrl+A" };
            selectAllItem.Click += (s, e) => vm.SelectCommand.Execute(null);
            selectMenu.Items.Add(selectAllItem);

            var invertItem = new MenuItem { Header = "Invert Selection", InputGestureText = "Ctrl+I" };
            invertItem.Click += (s, e) => vm.InvertSelectionCommand.Execute(null);
            selectMenu.Items.Add(invertItem);

            var clearItem = new MenuItem { Header = "Clear Selection", InputGestureText = "Esc" };
            clearItem.Click += (s, e) => vm.ClearSelectionCommand.Execute(null);
            selectMenu.Items.Add(clearItem);

            Items.Add(selectMenu);
        }

        private void AddPaneTransferItems(DualPaneBrowserViewModel vm)
        {
            Items.Add(new Separator());

            // Copy to Other Pane
            var copyToPaneItem = new MenuItem
            {
                Header = "Copy to Other Pane",
                Icon = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Icons/ArrowRightBold.png"))
                }
            };
            copyToPaneItem.Click += (s, e) => vm.CopyToOtherPaneCommand.Execute(null);
            Items.Add(copyToPaneItem);

            // Move to Other Pane
            var moveToPaneItem = new MenuItem
            {
                Header = "Move to Other Pane",
                Icon = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Icons/FileMove.png"))
                }
            };
            moveToPaneItem.Click += (s, e) => vm.MoveToOtherPaneCommand.Execute(null);
            Items.Add(moveToPaneItem);
        }

        private void AddAdvancedOperations(DualPaneBrowserViewModel vm)
        {
            Items.Add(new Separator());

            // Open in Explorer
            var explorerItem = new MenuItem
            {
                Header = "Open in Explorer",
                Icon = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Icons/FolderOutline.png"))
                }
            };
            explorerItem.Click += (s, e) => vm.OpenInExplorerCommand.Execute(null);
            Items.Add(explorerItem);

            // Properties
            var propertiesItem = new MenuItem
            {
                Header = "Properties",
                Icon = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Icons/InformationOutline.png"))
                }
            };
            propertiesItem.Click += (s, e) => vm.ShowPropertiesCommand.Execute(null);
            Items.Add(propertiesItem);

            // Advanced submenu
            var advancedMenu = new MenuItem { Header = "Advanced" };
            
            var calculateHashItem = new MenuItem { Header = "Calculate Checksum" };
            calculateHashItem.Click += (s, e) => 
            {
                System.Windows.MessageBox.Show(
                    "Checksum calculation would be performed here.\n\nSupported algorithms:\n- MD5\n- SHA1\n- SHA256\n- SHA512",
                    "Calculate Checksum",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            };
            advancedMenu.Items.Add(calculateHashItem);

            var createLinkItem = new MenuItem { Header = "Create Symbolic Link" };
            createLinkItem.Click += (s, e) => 
            {
                System.Windows.MessageBox.Show(
                    "Symbolic link creation would be performed here.\n\nLink types:\n- Symbolic Link (symlink)\n- Hard Link\n- Junction\n\nRequires elevated privileges.",
                    "Create Symbolic Link",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            };
            advancedMenu.Items.Add(createLinkItem);

            var attributesItem = new MenuItem { Header = "Change Attributes" };
            attributesItem.Click += (s, e) => 
            {
                System.Windows.MessageBox.Show(
                    "File attributes dialog would be shown here.\n\nAvailable attributes:\n- Read-only\n- Hidden\n- System\n- Archive\n- Compressed\n- Encrypted\n\nChanges would be applied to selected items.",
                    "Change Attributes",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            };
            advancedMenu.Items.Add(attributesItem);

            Items.Add(advancedMenu);
        }
    }
}

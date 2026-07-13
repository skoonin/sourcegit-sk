using System;
using System.IO;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace SourceGit.Views
{
    public partial class LauncherPagesCommandPalette : UserControl
    {
        public LauncherPagesCommandPalette()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            FilterTextBox.Focus(NavigationMethod.Directional);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (DataContext is not ViewModels.LauncherPagesCommandPalette vm)
                return;

            if (e.Key == Key.Enter)
            {
                Activate(vm);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (RepoListBox.IsKeyboardFocusWithin)
                {
                    if (vm.VisiblePages.Count > 0)
                    {
                        PageListBox.Focus(NavigationMethod.Directional);
                        vm.SelectedPage = vm.VisiblePages[^1];
                    }
                    else
                    {
                        FilterTextBox.Focus(NavigationMethod.Directional);
                    }

                    e.Handled = true;
                    return;
                }

                if (PageListBox.IsKeyboardFocusWithin)
                {
                    FilterTextBox.Focus(NavigationMethod.Directional);
                    e.Handled = true;
                    return;
                }
            }
            else if (e.Key == Key.Down || e.Key == Key.Tab)
            {
                if (FilterTextBox.IsKeyboardFocusWithin)
                {
                    if (vm.VisiblePages.Count > 0)
                    {
                        PageListBox.Focus(NavigationMethod.Directional);
                        vm.SelectedPage = vm.VisiblePages[0];
                    }
                    else if (vm.VisibleRepos.Count > 0)
                    {
                        RepoListBox.Focus(NavigationMethod.Directional);
                        vm.SelectedRepo = vm.VisibleRepos[0];
                    }

                    e.Handled = true;
                    return;
                }

                if (PageListBox.IsKeyboardFocusWithin)
                {
                    if (vm.VisibleRepos.Count > 0)
                    {
                        RepoListBox.Focus(NavigationMethod.Directional);
                        vm.SelectedRepo = vm.VisibleRepos[0];
                    }
                    else if (e.Key == Key.Tab)
                    {
                        FilterTextBox.Focus(NavigationMethod.Directional);
                    }

                    e.Handled = true;
                    return;
                }

                if (RepoListBox.IsKeyboardFocusWithin && e.Key == Key.Tab)
                {
                    FilterTextBox.Focus(NavigationMethod.Directional);
                    e.Handled = true;
                    return;
                }
            }
        }

        private void OnItemTapped(object sender, TappedEventArgs e)
        {
            if (DataContext is ViewModels.LauncherPagesCommandPalette vm)
            {
                Activate(vm);
                e.Handled = true;
            }
        }

        private void Activate(ViewModels.LauncherPagesCommandPalette vm)
        {
            if (vm.SelectedRepo is ViewModels.OpenOtherRepositoryAction)
                BrowseRepository(vm);
            else
                vm.OpenOrSwitchTo();
        }

        private async void BrowseRepository(ViewModels.LauncherPagesCommandPalette vm)
        {
            // Resolve the TopLevel before the palette closes; afterwards this control leaves the visual tree.
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            var preference = ViewModels.Preferences.Instance;
            var workspace = preference.GetActiveWorkspace();
            var initDir = workspace.DefaultCloneDir;
            if (string.IsNullOrEmpty(initDir) || !Directory.Exists(initDir))
                initDir = preference.GitDefaultCloneDir;

            var options = new FolderPickerOpenOptions() { AllowMultiple = false };
            if (Directory.Exists(initDir))
            {
                var folder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(initDir);
                options.SuggestedStartLocation = folder;
            }

            try
            {
                var selected = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
                if (selected.Count == 1)
                {
                    var folder = selected[0];
                    var path = folder is { Path: { IsAbsoluteUri: true } uri } ? uri.LocalPath : folder?.Path.ToString();
                    if (!string.IsNullOrEmpty(path))
                        vm.OpenPath(path);
                }
            }
            catch (Exception exception)
            {
                Models.Notification.Send(null, $"Failed to open repository: {exception.Message}", true);
            }
        }
    }
}

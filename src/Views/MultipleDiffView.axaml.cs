using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace SourceGit.Views
{
    public partial class MultipleDiffView : UserControl
    {
        public MultipleDiffView()
        {
            InitializeComponent();
        }

        private void OnToggleButtonPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ToggleButton.IsCheckedProperty && DataContext is ViewModels.MultipleDiffContext ctx)
                ctx.CheckSettings();
        }

        private void OnGotoFirstFile(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GotoFile(int.MinValue);
            e.Handled = true;
        }

        private void OnGotoPrevFile(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GotoFile(-1);
            e.Handled = true;
        }

        private void OnGotoNextFile(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GotoFile(1);
            e.Handled = true;
        }

        private void OnGotoLastFile(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            GotoFile(int.MaxValue);
            e.Handled = true;
        }

        private void GotoFile(int direction)
        {
            if (DataContext is not ViewModels.MultipleDiffContext ctx || ctx.Files.Count == 0)
                return;

            var offsets = new double[ctx.Files.Count];
            for (var i = 0; i < ctx.Files.Count; i++)
            {
                if (FilesList.ContainerFromIndex(i) is not Control container)
                    return;

                offsets[i] = container.TranslatePoint(new Avalonia.Point(0, 0), FilesList)?.Y ?? 0;
            }

            // The file whose header sits at (or just above) the current scroll position.
            var current = 0;
            for (var i = 0; i < offsets.Length; i++)
            {
                if (offsets[i] <= StackScroller.Offset.Y + 1)
                    current = i;
                else
                    break;
            }

            var target = direction switch
            {
                int.MinValue => 0,
                int.MaxValue => offsets.Length - 1,
                _ => Math.Clamp(current + direction, 0, offsets.Length - 1)
            };

            StackScroller.Offset = new Avalonia.Vector(0, offsets[target]);
        }

        private void OnFileHeaderPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is Border { DataContext: ViewModels.FileDiff file })
            {
                var expand = !file.IsExpanded;

                // Option/Alt-click applies the new state to every file (macOS disclosure-triangle convention).
                if (e.KeyModifiers.HasFlag(KeyModifiers.Alt) && DataContext is ViewModels.MultipleDiffContext ctx)
                {
                    foreach (var f in ctx.Files)
                        f.IsExpanded = expand;
                }
                else
                {
                    file.IsExpanded = expand;
                }
            }

            e.Handled = true;
        }
    }
}

using Avalonia.Controls;
using Avalonia.Input;

namespace SourceGit.Views
{
    public partial class MultipleDiffView : UserControl
    {
        public MultipleDiffView()
        {
            InitializeComponent();
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

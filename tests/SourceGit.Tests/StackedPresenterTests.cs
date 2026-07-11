using Avalonia.Controls;
using Avalonia.Headless.XUnit;

using SourceGit.Models;
using SourceGit.ViewModels;
using SourceGit.Views;

using Xunit;

namespace SourceGit.Tests
{
    public class StackedPresenterTests
    {
        // Sanity check that the headless harness itself (fonts, styles, Skia) boots.
        [AvaloniaFact]
        public void Headless_Harness_Boots()
        {
            var textBox = new TextBox();
            var window = new Window { Content = textBox, Width = 400, Height = 300 };
            window.Show();

            Assert.True(window.IsVisible);
        }

        // The stacked view depends on the presenter sizing to content under the outer
        // StackPanel's infinite-height measure (ThemedTextDiffPresenter.MeasureOverride).
        // Headless layout does not reproduce the near-zero-height failure seen on macOS,
        // so this asserts the sizing invariant rather than the historical bug itself.
        [AvaloniaFact]
        public void Stacked_TextDiffView_Sizes_To_Content_Under_Infinite_Measure()
        {
            var view = new TextDiffView { UseStacked = true, DataContext = MakeCombinedDiff(50) };
            var panel = new StackPanel { Children = { view } };
            var window = new Window { Content = panel, Width = 800, Height = 600 };

            window.Show();
            window.UpdateLayout();

            Assert.True(view.Bounds.Height > 100, $"Expected content-sized height, got {view.Bounds.Height}");
        }

        [AvaloniaFact]
        public void Unstacked_TextDiffView_Keeps_Viewport_Behavior()
        {
            var view = new TextDiffView { DataContext = MakeCombinedDiff(5000) };
            var window = new Window { Content = view, Width = 800, Height = 600 };

            window.Show();
            window.UpdateLayout();

            Assert.True(view.Bounds.Height <= 600, $"Expected viewport-bounded height, got {view.Bounds.Height}");
        }

        private static CombinedTextDiff MakeCombinedDiff(int lineCount)
        {
            var change = new Change { Path = "file.txt" };
            change.Set(ChangeState.None, ChangeState.Modified);

            var textDiff = new TextDiff();
            for (var i = 0; i < lineCount; i++)
                textDiff.Lines.Add(new TextDiffLine(TextDiffLineType.Added, $"line {i}", [], 0, i + 1));

            return new CombinedTextDiff(new DiffOption(change, true), textDiff);
        }
    }
}

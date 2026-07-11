using System;

using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media.Fonts;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(SourceGit.Tests.TestAppBuilder))]

namespace SourceGit.Tests
{
    // Mirrors the real application's styles, theme resources and embedded fonts, but not its
    // entry point: SourceGit.App.Main sets up the user's data directory and preferences,
    // which tests must never touch.
    public class TestApp : Application
    {
        public override void OnFrameworkInitializationCompleted()
        {
            Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://SourceGit.Tests"))
            {
                Source = new Uri("avares://SourceGit/Resources/Icons.axaml")
            });
            Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://SourceGit.Tests"))
            {
                Source = new Uri("avares://SourceGit/Resources/Themes.axaml")
            });

            Styles.Add(new FluentTheme());
            Styles.Add(new StyleInclude(new Uri("avares://SourceGit.Tests"))
            {
                Source = new Uri("avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml")
            });
            Styles.Add(new StyleInclude(new Uri("avares://SourceGit.Tests"))
            {
                Source = new Uri("avares://SourceGit/Resources/Styles.axaml")
            });

            base.OnFrameworkInitializationCompleted();
        }
    }

    public class TestAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp()
        {
            // The headless platform ships no system fonts; register the same embedded fonts as the app.
            return AppBuilder.Configure<TestApp>()
                .WithInterFont()
                .ConfigureFonts(manager =>
                {
                    var monospace = new EmbeddedFontCollection(
                        new Uri("fonts:SourceGit", UriKind.Absolute),
                        new Uri("avares://SourceGit/Resources/Fonts", UriKind.Absolute));
                    manager.AddFontCollection(monospace);
                })
                // Real Skia text shaping: the stub headless drawing backend cannot load the embedded fonts.
                .UseSkia()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
        }
    }
}

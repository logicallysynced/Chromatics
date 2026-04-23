using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;

namespace Chromatics.Views
{
    /// <summary>
    /// Minimal Avalonia Application used only to host the themed
    /// CrashFeedbackDialog when the main Chromatics app failed to boot.
    /// Carries just the FluentTheme + the Chromatics accent brushes the
    /// dialog references, with no settings or services attached.
    /// </summary>
    public partial class CrashApp : Application
    {
        // Static handoff: AppBuilder.Configure<T> instantiates T with no args,
        // so we stash the exception on the type before calling Start.
        public static Exception PendingException { get; set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

                var dlg = new CrashFeedbackDialog(PendingException);
                desktop.MainWindow = dlg;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}

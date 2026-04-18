using Avalonia.Media;
using Avalonia.Threading;
using Chromatics.Core;
using Chromatics.Models;
using System;
using System.Collections.ObjectModel;
using DrawingColor = System.Drawing.Color;

namespace Chromatics.ViewModels
{
    public sealed class ConsoleViewModel : ViewModelBase, IDisposable
    {
        // ~2000 entries is roughly equivalent to the 200K-char cap the WinForms
        // RichTextBox used. Bump if logs get denser over time.
        private const int MaxEntries = 2000;
        private const int TrimChunk  = 500;

        public ObservableCollection<ConsoleEntry> Entries { get; } = new();

        public event EventHandler EntryAdded;

        private bool _isLightTheme;

        public ConsoleViewModel()
        {
            _isLightTheme = Avalonia.Application.Current?.ActualThemeVariant
                            != Avalonia.Styling.ThemeVariant.Dark;

            Logger.OnConsoleLogged += OnConsoleLogged;
            App.ThemeVariantChanged += OnThemeVariantChanged;
        }

        private void OnConsoleLogged(object source, OnConsoleLoggedEventArgs e)
        {
            if (Dispatcher.UIThread.CheckAccess())
                Append(e);
            else
                Dispatcher.UIThread.Post(() => Append(e));
        }

        private void Append(OnConsoleLoggedEventArgs e)
        {
            var brush = ResolveBrush(e.Color, _isLightTheme);
            Entries.Add(new ConsoleEntry(e.Message, e.Color, brush));

            if (Entries.Count > MaxEntries)
            {
                for (int i = 0; i < TrimChunk && Entries.Count > 0; i++)
                    Entries.RemoveAt(0);
            }

            EntryAdded?.Invoke(this, EventArgs.Empty);
        }

        private void OnThemeVariantChanged(bool isLight)
        {
            _isLightTheme = isLight;
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var entry in Entries)
                    entry.Foreground = ResolveBrush(entry.RawColor, isLight);
            });
        }

        // Black is the "default" message class — flip to white on dark theme so it's
        // readable. Any other color (errors red, warnings yellow, etc.) passes
        // through unchanged.
        private static IBrush ResolveBrush(DrawingColor raw, bool isLight)
        {
            bool isBlack = raw.R == 0 && raw.G == 0 && raw.B == 0;
            if (isBlack && !isLight)
                return new SolidColorBrush(Colors.White);
            return new SolidColorBrush(Color.FromArgb(raw.A, raw.R, raw.G, raw.B));
        }

        public void Dispose()
        {
            Logger.OnConsoleLogged -= OnConsoleLogged;
            App.ThemeVariantChanged -= OnThemeVariantChanged;
        }
    }
}

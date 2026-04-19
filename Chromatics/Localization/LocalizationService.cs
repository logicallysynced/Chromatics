#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Chromatics.Enums;
using Newtonsoft.Json;

namespace Chromatics.Localization
{
    public sealed class LocalizationService : INotifyPropertyChanged
    {
        public static LocalizationService Instance { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private Dictionary<string, string> _translations = new();
        private Language _currentLanguage = (Language)(-1);
        private int _version;

        private LocalizationService() { }

        public string this[string key] =>
            _translations.TryGetValue(key, out var v) ? v : key;

        // Bumped on every language change. TrExtension binds to this as a
        // simple-path trigger (the path parser can't handle indexer keys
        // with spaces), and the converter pulls the translated text from
        // the indexer on re-evaluation.
        public int Version
        {
            get => _version;
            private set
            {
                _version = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Version)));
            }
        }

        public void SetLanguage(Language language)
        {
            if (_currentLanguage == language)
                return;
            Load(language);
        }

        private void Load(Language language)
        {
            _currentLanguage = language;
            var next = new Dictionary<string, string>();

            var file = Path.Combine(AppContext.BaseDirectory, "locale", GetFileName(language));
            if (File.Exists(file))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (dict != null)
                        next = dict;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LocalizationService] Failed to load {file}: {ex.Message}");
                }
            }

            _translations = next;
            Version++;
        }

        private static string GetFileName(Language lang) => lang switch
        {
            Language.English  => "en.json",
            Language.Japanese => "ja.json",
            Language.French   => "fr.json",
            Language.German   => "de.json",
            Language.Spanish  => "es.json",
            Language.Korean   => "ko.json",
            Language.Chinese  => "zh_CN.json",
            _                 => "en.json"
        };
    }
}

using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Linq;

namespace BogoTV.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        private string _codePage = "Unicode";
        private string _typingMethod = "Telex";
        private bool _autoStart = false;
        private bool _showOnScreenKeyboard = false;
        private bool _useClipboardFallback = false;
        private bool _blockCapslock = false;
        private string _toggleHotkey = "Ctrl + .";
        private bool _enabled = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BogoTV", "settings.xml");

        public string CodePage
        {
            get => _codePage;
            set { _codePage = value; OnPropertyChanged(nameof(CodePage)); }
        }

        public string TypingMethod
        {
            get => _typingMethod;
            set { _typingMethod = value; OnPropertyChanged(nameof(TypingMethod)); }
        }

        public bool AutoStart
        {
            get => _autoStart;
            set { _autoStart = value; OnPropertyChanged(nameof(AutoStart)); }
        }

        public bool ShowOnScreenKeyboard
        {
            get => _showOnScreenKeyboard;
            set { _showOnScreenKeyboard = value; OnPropertyChanged(nameof(ShowOnScreenKeyboard)); }
        }

        public bool UseClipboardFallback
        {
            get => _useClipboardFallback;
            set { _useClipboardFallback = value; OnPropertyChanged(nameof(UseClipboardFallback)); }
        }

        public bool BlockCapslock
        {
            get => _blockCapslock;
            set { _blockCapslock = value; OnPropertyChanged(nameof(BlockCapslock)); }
        }

        public string ToggleHotkey
        {
            get => _toggleHotkey;
            set { _toggleHotkey = value; OnPropertyChanged(nameof(ToggleHotkey)); }
        }

        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(nameof(Enabled)); }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ResetToDefault()
        {
            CodePage = "Unicode";
            TypingMethod = "Telex";
            AutoStart = false;
            ShowOnScreenKeyboard = false;
            UseClipboardFallback = false;
            BlockCapslock = false;
            ToggleHotkey = "Ctrl + .";
            Enabled = true;
        }

        public void Save()
        {
            try
            {
                string? dir = Path.GetDirectoryName(SettingsPath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                XElement xml = new XElement("BogoTVSettings",
                    new XElement("CodePage", CodePage),
                    new XElement("TypingMethod", TypingMethod),
                    new XElement("AutoStart", AutoStart),
                    new XElement("ShowOnScreenKeyboard", ShowOnScreenKeyboard),
                    new XElement("UseClipboardFallback", UseClipboardFallback),
                    new XElement("BlockCapslock", BlockCapslock),
                    new XElement("ToggleHotkey", ToggleHotkey),
                    new XElement("Enabled", Enabled)
                );
                xml.Save(SettingsPath);
            }
            catch
            {
            }
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return;

                XElement xml = XElement.Load(SettingsPath);
                CodePage = xml.Element("CodePage")?.Value ?? "Unicode";
                TypingMethod = xml.Element("TypingMethod")?.Value ?? "Telex";
                bool.TryParse(xml.Element("AutoStart")?.Value, out _autoStart);
                bool.TryParse(xml.Element("ShowOnScreenKeyboard")?.Value, out _showOnScreenKeyboard);
                bool.TryParse(xml.Element("UseClipboardFallback")?.Value, out _useClipboardFallback);
                bool.TryParse(xml.Element("BlockCapslock")?.Value, out _blockCapslock);
                ToggleHotkey = xml.Element("ToggleHotkey")?.Value ?? "Ctrl + .";
                bool.TryParse(xml.Element("Enabled")?.Value, out _enabled);
            }
            catch
            {
            }
        }
    }
}

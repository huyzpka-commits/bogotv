using System;
using System.Windows;
using System.Windows.Controls;
using BogoTV.Engine;
using BogoTV.Models;
using BogoTV.Views;

namespace BogoTV
{
    public partial class MainWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly VietnameseEngine _engine;
        private readonly GlobalKeyboardHook _hook;
        private bool _isAdvancedVisible = false;

        public NotifyIconController? TrayController { get; set; }

        public MainWindow(AppSettings settings, VietnameseEngine engine, GlobalKeyboardHook hook)
        {
            InitializeComponent();
            _settings = settings;
            _engine = engine;
            _hook = hook;

            LoadSettings();
            UpdateStatus(_engine.IsEnabled);
            _hook.EnabledChanged += (s, enabled) =>
            {
                Dispatcher.Invoke(() => UpdateStatus(enabled));
            };
        }

        private void LoadSettings()
        {
            SelectComboBoxItem(cmbCodePage, _settings.CodePage);
            SelectComboBoxItem(cmbTypingMethod, _settings.TypingMethod);
            chkAutoStart.IsChecked = _settings.AutoStart;
            chkClipboard.IsChecked = _settings.UseClipboardFallback;
            chkBlockCapslock.IsChecked = _settings.BlockCapslock;
            chkShowOSK.IsChecked = _settings.ShowOnScreenKeyboard;
            txtHotkey.Text = _settings.ToggleHotkey;

            ApplyEngineSettings();
        }

        private void ApplyEngineSettings()
        {
            _engine.Encoding = ParseEnum<CodePage>(_settings.CodePage);
            _engine.Method = ParseEnum<TypingMethod>(_settings.TypingMethod);
        }

        private static T ParseEnum<T>(string value) where T : struct
        {
            return Enum.TryParse<T>(value, true, out T result) ? result : default;
        }

        private static void SelectComboBoxItem(ComboBox combo, string value)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboBoxItem item &&
                    string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            combo.SelectedIndex = 0;
        }

        private void UpdateStatus(bool enabled)
        {
            if (enabled)
            {
                statusIndicator.Fill = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
                txtStatus.Text = "Dang hoat dong";
                txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50));
            }
            else
            {
                statusIndicator.Fill = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35));
                txtStatus.Text = "Da tam tat";
                txtStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35));
            }
        }

        private void cmbCodePage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCodePage.SelectedItem is ComboBoxItem item)
            {
                _settings.CodePage = item.Content?.ToString() ?? "Unicode";
                _engine.Encoding = ParseEnum<CodePage>(_settings.CodePage);
                _settings.Save();
            }
        }

        private void cmbTypingMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTypingMethod.SelectedItem is ComboBoxItem item)
            {
                _settings.TypingMethod = item.Content?.ToString() ?? "Telex";
                _engine.Method = ParseEnum<TypingMethod>(_settings.TypingMethod);
                _settings.Save();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            TrayController?.ShowBalloon("BogoTV", "Ung dung dang chay o khay he thong.");
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            ShutdownApp();
        }

        private void ShutdownApp()
        {
            _settings.Save();
            _hook.Stop();
            TrayController?.Dispose();
            Application.Current.Shutdown();
        }

        private void btnExpand_Click(object sender, RoutedEventArgs e)
        {
            _isAdvancedVisible = !_isAdvancedVisible;
            advancedPanel.Visibility = _isAdvancedVisible ? Visibility.Visible : Visibility.Collapsed;
            btnExpand.Content = _isAdvancedVisible ? "Thu gon" : "Mo rong";

            if (_isAdvancedVisible)
            {
                this.Height = 600;
            }
            else
            {
                this.Height = 440;
            }
        }

        private void btnDefault_Click(object sender, RoutedEventArgs e)
        {
            _settings.ResetToDefault();
            LoadSettings();
            _engine.Reset();
            UpdateStatus(true);
            _engine.IsEnabled = true;
            MessageBox.Show("Da khoi phuc mac dinh: Unicode + Telex.",
                "BogoTV", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow help = new HelpWindow
            {
                Owner = this
            };
            help.ShowDialog();
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow
            {
                Owner = this
            };
            about.ShowDialog();
        }

        private void btnToggle_Click(object sender, RoutedEventArgs e)
        {
            _engine.IsEnabled = !_engine.IsEnabled;
            _settings.Enabled = _engine.IsEnabled;
            _settings.Save();
            UpdateStatus(_engine.IsEnabled);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            TrayController?.ShowBalloon("BogoTV", "Ung dung dang chay o khay he thong.");
            base.OnClosing(e);
        }
    }
}

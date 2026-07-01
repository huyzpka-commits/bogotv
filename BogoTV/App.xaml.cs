using System;
using System.Windows;
using BogoTV.Engine;
using BogoTV.Models;

namespace BogoTV
{
    public partial class App : Application
    {
        private AppSettings _settings = null!;
        private VietnameseEngine _engine = null!;
        private GlobalKeyboardHook _hook = null!;
        private MainWindow _mainWindow = null!;
        private NotifyIconController _trayController = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _settings = new AppSettings();
            _settings.Load();

            _engine = new VietnameseEngine
            {
                Method = ParseEnum<TypingMethod>(_settings.TypingMethod),
                Encoding = ParseEnum<CodePage>(_settings.CodePage),
                IsEnabled = _settings.Enabled
            };

            _hook = new GlobalKeyboardHook(_engine);
            _hook.Start();

            _mainWindow = new MainWindow(_settings, _engine, _hook);

            _trayController = new NotifyIconController(
                this,
                onShowMainWindow: () =>
                {
                    _mainWindow.Show();
                    _mainWindow.WindowState = WindowState.Normal;
                    _mainWindow.Activate();
                },
                onExit: () =>
                {
                    ShutdownApp();
                },
                getEnabled: () => _engine.IsEnabled,
                toggleEnabled: () =>
                {
                    _engine.IsEnabled = !_engine.IsEnabled;
                    _settings.Enabled = _engine.IsEnabled;
                    _settings.Save();
                    _trayController.UpdateTooltip();
                }
            );
            _trayController.UpdateTooltip();
            _mainWindow.TrayController = _trayController;

            _mainWindow.Show();
        }

        private void ShutdownApp()
        {
            _settings.Save();
            _hook.Stop();
            _hook.Dispose();
            _trayController.Dispose();
            Shutdown();
        }

        private static T ParseEnum<T>(string value) where T : struct
        {
            return Enum.TryParse<T>(value, true, out T result) ? result : default;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _settings.Save();
            _hook?.Stop();
            _hook?.Dispose();
            _trayController?.Dispose();
            base.OnExit(e);
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;

namespace BogoTV
{
    public class NotifyIconController : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly System.Windows.Application _app;
        private readonly Action _onShowMainWindow;
        private readonly Action _onExit;
        private readonly Func<bool> _getEnabled;
        private readonly Action _toggleEnabled;

        public NotifyIconController(
            System.Windows.Application app,
            Action onShowMainWindow,
            Action onExit,
            Func<bool> getEnabled,
            Action toggleEnabled)
        {
            _app = app;
            _onShowMainWindow = onShowMainWindow;
            _onExit = onExit;
            _getEnabled = getEnabled;
            _toggleEnabled = toggleEnabled;

            _notifyIcon = new NotifyIcon
            {
                Icon = CreateDefaultIcon(),
                Visible = true,
                Text = "BogoTV - Bo go Tieng Viet"
            };

            BuildContextMenu();
        }

        private void BuildContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            menu.Items.Add("Mo bang dieu khien", null, (s, e) =>
            {
                _onShowMainWindow?.Invoke();
            });

            menu.Items.Add(new ToolStripSeparator());

            var toggleItem = new ToolStripMenuItem("Bat/Tat bo go");
            toggleItem.Click += (s, e) =>
            {
                _toggleEnabled?.Invoke();
                UpdateTooltip();
            };
            menu.Items.Add(toggleItem);

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add("Thoat BogoTV", null, (s, e) =>
            {
                _notifyIcon.Visible = false;
                _onExit?.Invoke();
            });

            _notifyIcon.ContextMenuStrip = menu;

            _notifyIcon.DoubleClick += (s, e) =>
            {
                _onShowMainWindow?.Invoke();
            };
        }

        public void UpdateTooltip()
        {
            bool enabled = _getEnabled?.Invoke() ?? true;
            _notifyIcon.Text = enabled
                ? "BogoTV - Dang hoat dong"
                : "BogoTV - Da tam tat";
        }

        public void ShowBalloon(string title, string message)
        {
            _notifyIcon.ShowBalloonTip(2000, title, message, ToolTipIcon.Info);
        }

        private static Icon CreateDefaultIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(63, 81, 181)))
                {
                    g.FillEllipse(bg, 2, 2, 28, 28);
                }
                using (SolidBrush fg = new SolidBrush(Color.White))
                using (Font font = new Font("Segoe UI", 12, FontStyle.Bold))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("TV", font, fg, new RectangleF(2, 2, 28, 28), sf);
                }
            }
            IntPtr hIcon = bmp.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            return icon;
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }
}

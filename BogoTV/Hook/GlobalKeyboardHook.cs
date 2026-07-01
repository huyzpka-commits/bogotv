using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BogoTV.Hook
{
    public class GlobalKeyboardHook : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const byte VK_BACK = 0x08;
        private const byte VK_RETURN = 0x0D;
        private const byte VK_SPACE = 0x20;
        private const byte VK_LEFT = 0x25;
        private const byte VK_UP = 0x26;
        private const byte VK_RIGHT = 0x27;
        private const byte VK_DOWN = 0x28;
        private const byte VK_DELETE = 0x2E;
        private const byte VK_SHIFT = 0x10;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_MENU = 0x12;
        private const byte VK_CAPITAL = 0x14;

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private IntPtr _hookID = IntPtr.Zero;
        private HookProc _hookProc;
        private readonly Engine.VietnameseEngine _engine;
        private bool _isProcessing = false;
        private bool _ctrlPressed = false;
        private bool _altPressed = false;
        private bool _winPressed = false;

        public bool IsEnabled
        {
            get => _engine.IsEnabled;
            set => _engine.IsEnabled = value;
        }

        public event EventHandler<bool>? EnabledChanged;

        public GlobalKeyboardHook(Engine.VietnameseEngine engine)
        {
            _engine = engine;
            _hookProc = HookCallback;
        }

        public void Start()
        {
            if (_hookID != IntPtr.Zero)
                return;

            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc,
                    GetModuleHandle(curModule?.ModuleName ?? "user32"), 0);
            }

            if (_hookID == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"SetWindowsHookEx failed: {err}");
            }
        }

        public void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !_isProcessing)
            {
                int msg = wParam.ToInt32();
                KBDLLHOOKSTRUCT kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                uint vk = kb.vkCode;

                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    if (vk == VK_CONTROL || vk == 162 || vk == 163) _ctrlPressed = true;
                    if (vk == VK_MENU || vk == 165) _altPressed = true;
                    if (vk == 91 || vk == 92) _winPressed = true;

                    if (vk == 190 && _ctrlPressed)
                    {
                        _engine.IsEnabled = !_engine.IsEnabled;
                        EnabledChanged?.Invoke(this, _engine.IsEnabled);
                        return (IntPtr)1;
                    }

                    if (_ctrlPressed || _altPressed || _winPressed)
                    {
                        _engine.OnNonCharKey();
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }

                    if (vk == VK_BACK)
                    {
                        _engine.OnBackspace();
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }

                    if (vk == VK_RETURN || vk == VK_SPACE || vk == VK_DELETE)
                    {
                        _engine.OnNonCharKey();
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }

                    if (vk == VK_LEFT || vk == VK_UP || vk == VK_RIGHT || vk == VK_DOWN)
                    {
                        _engine.OnArrowKey();
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }

                    if (_engine.IsEnabled && vk >= 32 && vk < 255)
                    {
                        bool handled = ProcessKey(vk, kb.flags);
                        if (handled)
                            return (IntPtr)1;
                    }
                    else if (!_engine.IsEnabled)
                    {
                        _engine.OnNonCharKey();
                    }
                }
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    if (vk == VK_CONTROL || vk == 162 || vk == 163) _ctrlPressed = false;
                    if (vk == VK_MENU || vk == 165) _altPressed = false;
                    if (vk == 91 || vk == 92) _winPressed = false;
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool ProcessKey(uint vk, uint flags)
        {
            bool shiftPressed = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

            if (vk >= 'A' && vk <= 'Z')
            {
                char c = (char)vk;
                bool capsLock = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
                bool isUpper = shiftPressed ^ capsLock;
                if (isUpper)
                    c = char.ToUpperInvariant(c);
                else
                    c = char.ToLowerInvariant(c);

                var outcome = _engine.ProcessKey(c, true);
                return HandleOutcome(outcome);
            }

            if (vk >= '0' && vk <= '9')
            {
                char c;
                if (shiftPressed)
                {
                    c = MapShiftedDigit(vk);
                    if (c == '\0')
                        c = (char)vk;
                }
                else
                {
                    c = (char)vk;
                }
                var outcome = _engine.ProcessKey(c, true);
                return HandleOutcome(outcome);
            }

            char oemChar = MapOemKey(vk, shiftPressed);
            if (oemChar != '\0')
            {
                var outcome = _engine.ProcessKey(oemChar, true);
                return HandleOutcome(outcome);
            }

            return false;
        }

        private static char MapShiftedDigit(uint vk)
        {
            return vk switch
            {
                '0' => ')',
                '1' => '!',
                '2' => '@',
                '3' => '#',
                '4' => '$',
                '5' => '%',
                '6' => '^',
                '7' => '&',
                '8' => '*',
                '9' => '(',
                _ => '\0'
            };
        }

        private bool HandleOutcome(Engine.TransformOutcome outcome)
        {
            if (outcome.Result == Engine.TransformResult.Transformed)
            {
                _isProcessing = true;
                try
                {
                    if (outcome.BackspaceCount > 0)
                        Engine.InputSimulator.SendBackspace(outcome.BackspaceCount);

                    if (!string.IsNullOrEmpty(outcome.Output))
                        Engine.InputSimulator.SendUnicodeString(outcome.Output);
                }
                finally
                {
                    _isProcessing = false;
                }
                return true;
            }

            return false;
        }

        private static char MapOemKey(uint vk, bool shiftPressed)
        {
            return vk switch
            {
                192 => shiftPressed ? '~' : '`',
                189 => shiftPressed ? '_' : '-',
                187 => shiftPressed ? '+' : '=',
                219 => shiftPressed ? '{' : '[',
                221 => shiftPressed ? '}' : ']',
                220 => shiftPressed ? '|' : '\\',
                186 => shiftPressed ? ':' : ';',
                222 => shiftPressed ? '"' : '\'',
                188 => shiftPressed ? '<' : ',',
                190 => shiftPressed ? '>' : '.',
                191 => shiftPressed ? '?' : '/',
                _ => '\0'
            };
        }

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public void Dispose()
        {
            Stop();
        }
    }
}

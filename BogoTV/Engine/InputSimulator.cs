using System;
using System.Runtime.InteropServices;

namespace BogoTV.Engine
{
    internal static class InputSimulator
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetMessageExtraInfo();

        internal const int INPUT_KEYBOARD = 1;
        internal const uint KEYEVENTF_UNICODE = 0x0004;
        internal const uint KEYEVENTF_KEYUP = 0x0002;
        internal const byte VK_BACK = 0x08;

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            public int type;
            public KEYBDINPUT ki;
        }

        internal static void SendBackspace(int count)
        {
            if (count <= 0) return;
            INPUT[] inputs = new INPUT[count * 2];
            int idx = 0;
            for (int i = 0; i < count; i++)
            {
                inputs[idx].type = INPUT_KEYBOARD;
                inputs[idx].ki.wVk = VK_BACK;
                inputs[idx].ki.wScan = 0;
                inputs[idx].ki.dwFlags = 0;
                inputs[idx].ki.dwExtraInfo = GetMessageExtraInfo();
                idx++;

                inputs[idx].type = INPUT_KEYBOARD;
                inputs[idx].ki.wVk = VK_BACK;
                inputs[idx].ki.wScan = 0;
                inputs[idx].ki.dwFlags = KEYEVENTF_KEYUP;
                inputs[idx].ki.dwExtraInfo = GetMessageExtraInfo();
                idx++;
            }
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        internal static void SendUnicodeString(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            INPUT[] inputs = new INPUT[text.Length * 2];
            int idx = 0;
            foreach (char c in text)
            {
                inputs[idx].type = INPUT_KEYBOARD;
                inputs[idx].ki.wVk = 0;
                inputs[idx].ki.wScan = (ushort)c;
                inputs[idx].ki.dwFlags = KEYEVENTF_UNICODE;
                inputs[idx].ki.dwExtraInfo = GetMessageExtraInfo();
                idx++;

                inputs[idx].type = INPUT_KEYBOARD;
                inputs[idx].ki.wVk = 0;
                inputs[idx].ki.wScan = (ushort)c;
                inputs[idx].ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
                inputs[idx].ki.dwExtraInfo = GetMessageExtraInfo();
                idx++;
            }
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        internal static void SendUnicodeChar(char c)
        {
            INPUT[] inputs = new INPUT[2];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].ki.wVk = 0;
            inputs[0].ki.wScan = (ushort)c;
            inputs[0].ki.dwFlags = KEYEVENTF_UNICODE;
            inputs[0].ki.dwExtraInfo = GetMessageExtraInfo();

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].ki.wVk = 0;
            inputs[1].ki.wScan = (ushort)c;
            inputs[1].ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
            inputs[1].ki.dwExtraInfo = GetMessageExtraInfo();

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}

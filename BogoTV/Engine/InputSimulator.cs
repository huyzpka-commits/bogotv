using System;
using System.Runtime.InteropServices;

namespace BogoTV.Engine
{
    internal static class InputSimulator
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, IntPtr pInputs, int cbSize);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetMessageExtraInfo();

        internal const int INPUT_KEYBOARD = 1;
        internal const uint KEYEVENTF_UNICODE = 0x0004;
        internal const uint KEYEVENTF_KEYUP = 0x0002;
        internal const byte VK_BACK = 0x08;

        internal static void SendBackspace(int count)
        {
            if (count <= 0) return;

            int inputSize = 40;
            int totalSize = inputSize * count * 2;
            IntPtr pInputs = Marshal.AllocHGlobal(totalSize);
            try
            {
                int offset = 0;
                for (int i = 0; i < count; i++)
                {
                    WriteKeyInput(pInputs, offset, VK_BACK, 0, 0);
                    offset += inputSize;
                    WriteKeyInput(pInputs, offset, VK_BACK, 0, KEYEVENTF_KEYUP);
                    offset += inputSize;
                }

                uint sent = SendInput((uint)(count * 2), pInputs, inputSize);
                DebugLogger.Log($"  SendBackspace: inputSize={inputSize} sent={sent}/{count*2} err={Marshal.GetLastWin32Error()}");
            }
            finally
            {
                Marshal.FreeHGlobal(pInputs);
            }
        }

        internal static void SendUnicodeString(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            int inputSize = 40;
            int totalSize = inputSize * text.Length * 2;
            IntPtr pInputs = Marshal.AllocHGlobal(totalSize);
            try
            {
                int offset = 0;
                foreach (char c in text)
                {
                    WriteKeyInput(pInputs, offset, 0, (ushort)c, KEYEVENTF_UNICODE);
                    offset += inputSize;
                    WriteKeyInput(pInputs, offset, 0, (ushort)c, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP);
                    offset += inputSize;
                }

                uint sent = SendInput((uint)(text.Length * 2), pInputs, inputSize);
                DebugLogger.Log($"  SendUnicodeString: inputSize={inputSize} sent={sent}/{text.Length*2} err={Marshal.GetLastWin32Error()}");
                foreach (char c in text)
                {
                    DebugLogger.Log($"    char=0x{(int)c:X4}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pInputs);
            }
        }

        private static void WriteKeyInput(IntPtr pBase, int offset, ushort wVk, ushort wScan, uint dwFlags)
        {
            int baseOff = offset;
            Marshal.WriteInt32(pBase, baseOff + 0, INPUT_KEYBOARD);
            Marshal.WriteInt16(pBase, baseOff + 8, (short)wVk);
            Marshal.WriteInt16(pBase, baseOff + 10, (short)wScan);
            Marshal.WriteInt32(pBase, baseOff + 12, (int)dwFlags);
            Marshal.WriteInt32(pBase, baseOff + 16, 0);
            Marshal.WriteInt64(pBase, baseOff + 24, 0);
        }
    }
}

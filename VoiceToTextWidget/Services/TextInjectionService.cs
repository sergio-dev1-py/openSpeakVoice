using System.Runtime.InteropServices;
using System.Windows.Forms;
using InputSimulatorEx;

namespace VoiceToTextWidget.Services;

public sealed class TextInjectionService : IDisposable
{
    private readonly InputSimulator _simulator = new();

    public void InjectText(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            Clipboard.SetText(text);
            SendCtrlV();
        }
        catch
        {
            try
            {
                _simulator.Keyboard.TextEntry(text);
            }
            catch
            {
            }
        }
    }

    private static void SendCtrlV()
    {
        var inputs = new INPUT[4];

        inputs[0] = CreateKeyInput(VirtualKeyShort.CONTROL, 0);
        inputs[1] = CreateKeyInput(VirtualKeyShort.KEY_V, 0);
        inputs[2] = CreateKeyInput(VirtualKeyShort.KEY_V, KEYEVENTF_KEYUP);
        inputs[3] = CreateKeyInput(VirtualKeyShort.CONTROL, KEYEVENTF_KEYUP);

        var size = Marshal.SizeOf<INPUT>();
        var sent = SendInput((uint)inputs.Length, inputs, size);
        if (sent == 0)
        {
            SendKeys.SendWait("^v");
        }
    }

    private static INPUT CreateKeyInput(VirtualKeyShort key, uint flags)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = key,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public VirtualKeyShort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private enum VirtualKeyShort : ushort
    {
        CONTROL = 0x11,
        KEY_V = 0x56
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    public void Dispose() { }
}

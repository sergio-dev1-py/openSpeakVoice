using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VoiceToTextWidget.Models;
using VoiceToTextWidget.Native;

namespace VoiceToTextWidget.Services;

public sealed class HotKeyService : IDisposable
{
    private static readonly string LogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VoiceToTextWidget", "debug.log");

    private readonly AppSettings _settings;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _disposed;
    private bool _keyPressed;
    private WinApi.LowLevelKeyboardProc? _proc;

    public bool IsRegistered => _hookId != IntPtr.Zero;

    public event Action? HotKeyPressed;

    public HotKeyService(AppSettings settings)
    {
        _settings = settings;
        _proc = HookCallback;
        InstallHook();
    }

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [HotKey] {message}\n";
        try { System.IO.File.AppendAllText(LogPath, line); } catch { }
    }

    private void InstallHook()
    {
        var modHandle = WinApi.GetModuleHandle("user32.dll");
        Log($"GetModuleHandle(user32.dll) = 0x{modHandle.ToInt64():X}");

        if (modHandle == IntPtr.Zero)
        {
            Log("user32.dll not found, trying kernel32.dll");
            modHandle = WinApi.GetModuleHandle("kernel32.dll");
            Log($"GetModuleHandle(kernel32.dll) = 0x{modHandle.ToInt64():X}");
        }

        _hookId = WinApi.SetWindowsHookEx(
            WinApi.WH_KEYBOARD_LL,
            _proc!,
            modHandle,
            0);

        var error = Marshal.GetLastWin32Error();
        Log($"SetWindowsHookEx result: hookId=0x{_hookId.ToInt64():X}, lastError={error}");

        if (_hookId == IntPtr.Zero)
        {
            Log("FAILED to install keyboard hook!");
        }
        else
        {
            Log("Keyboard hook installed successfully");
        }
    }

    public void Reregister()
    {
        Log("Reregister");
        UninstallHook();
        InstallHook();
    }

    private void UninstallHook()
    {
        if (_hookId != IntPtr.Zero)
        {
            WinApi.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            var kbdStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            bool isKeyDown = msg == WinApi.WM_KEYDOWN || msg == WinApi.WM_SYSKEYDOWN;
            bool isKeyUp = msg == WinApi.WM_KEYUP || msg == WinApi.WM_SYSKEYUP;

            if (isKeyDown && !_keyPressed)
            {
                if (MatchesHotKey(kbdStruct.vkCode))
                {
                    _keyPressed = true;
                    Log($"HOTKEY TRIGGERED: vkCode=0x{kbdStruct.vkCode:X} ({(Keys)kbdStruct.vkCode})");
                    HotKeyPressed?.Invoke();
                    return (IntPtr)1;
                }
            }
            else if (isKeyUp)
            {
                if ((Keys)kbdStruct.vkCode == _settings.HotKey)
                {
                    _keyPressed = false;
                }
            }
        }

        return WinApi.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool MatchesHotKey(int vkCode)
    {
        Keys key = (Keys)vkCode;

        if (key != _settings.HotKey)
            return false;

        bool ctrlRequired = (_settings.Modifiers & ModifierKeys.Control) != 0;
        bool altRequired = (_settings.Modifiers & ModifierKeys.Alt) != 0;
        bool shiftRequired = (_settings.Modifiers & ModifierKeys.Shift) != 0;

        bool ctrlPressed = (Control.ModifierKeys & Keys.Control) != 0;
        bool altPressed = (Control.ModifierKeys & Keys.Alt) != 0;
        bool shiftPressed = (Control.ModifierKeys & Keys.Shift) != 0;

        if (ctrlRequired != ctrlPressed) return false;
        if (altRequired != altPressed) return false;
        if (shiftRequired != shiftPressed) return false;

        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UninstallHook();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }
}

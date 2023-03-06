#pragma warning disable CS0649
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using static UkrChatSupportPlugin.Sys.Forms;

// ReSharper disable IdentifierTypo

namespace UkrChatSupportPlugin.Sys;

public class KeyboardHook : IDisposable
{
    public delegate void ErrorEventHandler(Exception e);

    public delegate void LocalKeyEventHandler(Keys key, bool shift, bool ctrl, bool alt, ref bool skipNext);

    public enum KeyCode : ushort
    {
        MEDIA_NEXT_TRACK = 176,
        MEDIA_PLAY_PAUSE = 179,
        MEDIA_PREV_TRACK = 177,
        MEDIA_STOP = 178,
        ADD = 107,
        MULTIPLY = 106,
        DIVIDE = 111,
        SUBTRACT = 109,
        BROWSER_BACK = 166,
        BROWSER_FAVORITES = 171,
        BROWSER_FORWARD = 167,
        BROWSER_HOME = 172,
        BROWSER_REFRESH = 168,
        BROWSER_SEARCH = 170,
        BROWSER_STOP = 169,
        NUMPAD0 = 96,
        NUMPAD1 = 97,
        NUMPAD2 = 98,
        NUMPAD3 = 99,
        NUMPAD4 = 100,
        NUMPAD5 = 101,
        NUMPAD6 = 102,
        NUMPAD7 = 103,
        NUMPAD8 = 104,
        NUMPAD9 = 105,
        F1 = 112,
        F10 = 121,
        F11 = 122,
        F12 = 123,
        F13 = 124,
        F14 = 125,
        F15 = 126,
        F16 = 0x7F,
        F17 = 0x80,
        F18 = 129,
        F19 = 130,
        F2 = 113,
        F20 = 131,
        F21 = 132,
        F22 = 133,
        F23 = 134,
        F24 = 135,
        F3 = 114,
        F4 = 115,
        F5 = 116,
        F6 = 117,
        F7 = 118,
        F8 = 119,
        F9 = 120,
        OEM_1 = 186,
        OEM_102 = 226,
        OEM_2 = 191,
        OEM_3 = 192,
        OEM_4 = 219,
        OEM_5 = 220,
        OEM_6 = 221,
        OEM_7 = 222,
        OEM_8 = 223,
        OEM_CLEAR = 254,
        OEM_COMMA = 188,
        OEM_MINUS = 189,
        OEM_PERIOD = 190,
        OEM_PLUS = 187,
        KEY_0 = 48,
        KEY_1 = 49,
        KEY_2 = 50,
        KEY_3 = 51,
        KEY_4 = 52,
        KEY_5 = 53,
        KEY_6 = 54,
        KEY_7 = 55,
        KEY_8 = 56,
        KEY_9 = 57,
        KEY_A = 65,
        KEY_B = 66,
        KEY_C = 67,
        KEY_D = 68,
        KEY_E = 69,
        KEY_F = 70,
        KEY_G = 71,
        KEY_H = 72,
        KEY_I = 73,
        KEY_J = 74,
        KEY_K = 75,
        KEY_L = 76,
        KEY_M = 77,
        KEY_N = 78,
        KEY_O = 79,
        KEY_P = 80,
        KEY_Q = 81,
        KEY_R = 82,
        KEY_S = 83,
        KEY_T = 84,
        KEY_U = 85,
        KEY_V = 86,
        KEY_W = 87,
        KEY_X = 88,
        KEY_Y = 89,
        KEY_Z = 90,
        VOLUME_DOWN = 174,
        VOLUME_MUTE = 173,
        VOLUME_UP = 175,
        SNAPSHOT = 44,
        RIGHT_CLICK = 93,
        BACKSPACE = 8,
        CANCEL = 3,
        CAPS_LOCK = 20,
        CONTROL = 17,
        ALT = 18,
        DECIMAL = 110,
        DELETE = 46,
        DOWN = 40,
        END = 35,
        ESC = 27,
        HOME = 36,
        INSERT = 45,
        LAUNCH_APP1 = 182,
        LAUNCH_APP2 = 183,
        LAUNCH_MAIL = 180,
        LAUNCH_MEDIA_SELECT = 181,
        LCONTROL = 162,
        LEFT = 37,
        LSHIFT = 160,
        LWIN = 91,
        PAGEDOWN = 34,
        NUMLOCK = 144,
        PAGE_UP = 33,
        RCONTROL = 163,
        ENTER = 13,
        RIGHT = 39,
        RSHIFT = 161,
        RWIN = 92,
        SHIFT = 0x10,
        SPACE_BAR = 0x20,
        TAB = 9,
        UP = 38
    }

    public enum KeyEvents
    {
        KeyDown = 0x100,
        KeyUp = 257,
        SKeyDown = 260,
        SKeyUp = 261
    }

    public const int WM_KEYDOWN = 256;

    public const int WM_KEYUP = 257;

    public const int WM_CHAR = 261;

    private readonly bool Global;

    private readonly IntPtr HookID = nint.Zero;

    private readonly CallbackDelegate TheHookCB;

    private bool IsFinalized;

    public KeyboardHook(bool Global)
    {
        this.Global = Global;
        TheHookCB = KeybHookProc;
        if (Global)
        {
            var hInstance = LoadLibrary("User32");
            HookID = SetWindowsHookEx(HookType.WH_KEYBOARD_LL, TheHookCB, hInstance, 0);
        }
        else
            HookID = SetWindowsHookEx(HookType.WH_KEYBOARD, TheHookCB, nint.Zero, GetCurrentThreadId());
    }

    public void Dispose()
    {
        if (!IsFinalized)
        {
            UnhookWindowsHookEx(HookID);
            IsFinalized = true;
        }
    }

    public event LocalKeyEventHandler? KeyDown;

    public event LocalKeyEventHandler? KeyUp;

    public event ErrorEventHandler? OnError;

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr SetWindowsHookEx(
        HookType idHook, CallbackDelegate lpfn, IntPtr hInstance,
        int threadId);

    [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
    private static extern bool UnhookWindowsHookEx(IntPtr idHook);

    [DllImport("user32", CallingConvention = CallingConvention.StdCall)]
    private static extern int CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int GetCurrentThreadId();

    [DllImport("kernel32.dll")]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint thread);

    public static void SendString(string inputStr)
    {
        var list = new List<INPUT>();
        foreach (ushort num in inputStr)
            switch (num)
            {
                case 8:
                {
                    var item5 = default(INPUT);
                    item5.Type = 1u;
                    item5.Data.Keyboard.Vk = 9;
                    item5.Data.Keyboard.Flags = 0u;
                    item5.Data.Keyboard.Scan = 0;
                    list.Add(item5);
                    var item6 = default(INPUT);
                    item6.Type = 1u;
                    item6.Data.Keyboard.Vk = 9;
                    item6.Data.Keyboard.Flags = 2u;
                    item6.Data.Keyboard.Scan = 0;
                    list.Add(item6);
                    break;
                }
                case 10:
                {
                    var item3 = default(INPUT);
                    item3.Type = 1u;
                    item3.Data.Keyboard.Vk = 13;
                    item3.Data.Keyboard.Flags = 0u;
                    item3.Data.Keyboard.Scan = 0;
                    list.Add(item3);
                    var item4 = default(INPUT);
                    item4.Type = 1u;
                    item4.Data.Keyboard.Vk = 13;
                    item4.Data.Keyboard.Flags = 2u;
                    item4.Data.Keyboard.Scan = 0;
                    list.Add(item4);
                    break;
                }
                default:
                {
                    var item = default(INPUT);
                    item.Type = 1u;
                    item.Data.Keyboard.Vk = 0;
                    item.Data.Keyboard.Flags = 4u;
                    item.Data.Keyboard.Scan = num;
                    list.Add(item);
                    var item2 = default(INPUT);
                    item2.Type = 1u;
                    item2.Data.Keyboard.Vk = 0;
                    item2.Data.Keyboard.Flags = 6u;
                    item2.Data.Keyboard.Scan = num;
                    list.Add(item2);
                    break;
                }
            }

        SendInput((uint)list.Count, list.ToArray(), Marshal.SizeOf(typeof(INPUT)));
    }

    public static void SendCharUnicode(int utf32)
    {
        var text = char.ConvertFromUtf32(utf32);
        var array = new INPUT[text.Length];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = default;
            array[i].Type = 1u;
            array[i].Data.Keyboard.Vk = 0;
            array[i].Data.Keyboard.Scan = text[i];
            array[i].Data.Keyboard.Flags = 4u;
        }

        SendInput((uint)array.Length, array, Marshal.SizeOf(typeof(INPUT)));
    }

    public static CultureInfo GetCurrentKeyboardLayout()
    {
        try
        {
            return new CultureInfo(GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow(), nint.Zero))
                                       .ToInt32() & 0xFFFF);
        }
        catch
        {
            return new CultureInfo(1033);
        }
    }

    public static CultureInfo GetCurrentKeyboardLayout(uint aWindowThreadProcessId)
    {
        try
        {
            return new CultureInfo(GetKeyboardLayout(aWindowThreadProcessId).ToInt32() & 0xFFFF);
        }
        catch
        {
            return new CultureInfo(1033);
        }
    }

    public void test()
    {
        if (OnError != null) OnError(new Exception("test"));
    }

    ~KeyboardHook()
    {
        if (!IsFinalized)
        {
            UnhookWindowsHookEx(HookID);
            IsFinalized = true;
        }
    }

    [STAThread]
    private int KeybHookProc(int Code, IntPtr W, IntPtr L)
    {
        if (Code < 0) return CallNextHookEx(HookID, Code, W, L);
        var skipNext = false;
        try
        {
            if (!Global)
            {
                if (Code == 3)
                {
                    var num = L.ToInt32() >> 30;
                    switch (num)
                    {
                        case 0 when KeyDown != null:
                            KeyDown((Keys)(int)W, GetShiftPressed(), GetCtrlPressed(), GetAltPressed(), ref skipNext);
                            break;
                        case -1 when KeyUp != null:
                            KeyUp((Keys)(int)W, GetShiftPressed(), GetCtrlPressed(), GetAltPressed(), ref skipNext);
                            break;
                    }
                }
            }
            else
            {
                var keyEvents = (KeyEvents)(int)W;
                var key = Marshal.ReadInt32(L);
                switch (keyEvents)
                {
                    case KeyEvents.KeyDown or KeyEvents.SKeyDown when KeyDown != null:
                        KeyDown((Keys)key, GetShiftPressed(), GetCtrlPressed(), GetAltPressed(), ref skipNext);
                        break;
                    case KeyEvents.KeyUp or KeyEvents.SKeyUp when KeyUp != null:
                        KeyUp((Keys)key, GetShiftPressed(), GetCtrlPressed(), GetAltPressed(), ref skipNext);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            OnError?.Invoke(e);
        }

        return skipNext ? -1 : CallNextHookEx(HookID, Code, W, L);
    }

    [DllImport("user32.dll")]
    public static extern short GetKeyState(Keys nVirtKey);

    public static bool GetCapslock()
    {
        return Convert.ToBoolean(GetKeyState(Keys.Capital));
    }

    public static bool GetNumlock()
    {
        return Convert.ToBoolean(GetKeyState(Keys.NumLock));
    }

    public static bool GetScrollLock()
    {
        return Convert.ToBoolean(GetKeyState(Keys.Scroll));
    }

    public static bool GetShiftPressed()
    {
        int keyState = GetKeyState(Keys.ShiftKey);
        if (keyState > 1 || keyState < -1) return true;
        return false;
    }

    public static bool GetCtrlPressed()
    {
        int keyState = GetKeyState(Keys.ControlKey);
        if (keyState > 1 || keyState < -1) return true;
        return false;
    }

    public static bool GetAltPressed()
    {
        int keyState = GetKeyState(Keys.Menu);
        if (keyState > 1 || keyState < -1) return true;
        return false;
    }

    private delegate int CallbackDelegate(int code, IntPtr w, IntPtr l);

    private enum HookType
    {
        WH_JOURNALRECORD,
        WH_JOURNALPLAYBACK,
        WH_KEYBOARD,
        WH_GETMESSAGE,
        WH_CALLWNDPROC,
        WH_CBT,
        WH_SYSMSGFILTER,
        WH_MOUSE,
        WH_HARDWARE,
        WH_DEBUG,
        WH_SHELL,
        WH_FOREGROUNDIDLE,
        WH_CALLWNDPROCRET,
        WH_KEYBOARD_LL,
        WH_MOUSE_LL
    }

    public struct KBDLLHookStruct
    {
        public int vkCode;

        public int scanCode;

        public int flags;

        public int time;

        public int dwExtraInfo;
    }

    internal struct INPUT
    {
        public uint Type;

        public MOUSEKEYBDHARDWAREINPUT Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MOUSEKEYBDHARDWAREINPUT
    {
        [FieldOffset(0)]
        public HARDWAREINPUT Hardware;

        [FieldOffset(0)]
        public KEYBDINPUT Keyboard;

        [FieldOffset(0)]
        public MOUSEINPUT Mouse;
    }

    internal struct MOUSEINPUT
    {
        public int X;

        public int Y;

        public uint MouseData;

        public uint Flags;

        public uint Time;

        public IntPtr ExtraInfo;
    }

    internal struct HARDWAREINPUT
    {
        public uint Msg;

        public ushort ParamL;

        public ushort ParamH;
    }

    internal struct KEYBDINPUT
    {
        public ushort Vk;

        public ushort Scan;

        public uint Flags;

        public uint Time;

        public IntPtr ExtraInfo;
    }
}
#pragma warning restore CS0649

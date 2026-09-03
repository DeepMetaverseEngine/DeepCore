using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Input
{
    public enum MouseButton
    {
        [Desc("No mouse button was pressed.")]
        None = 0,
        [Desc("The left mouse button was pressed.")]
        Left = 1048576,
        [Desc("The right mouse button was pressed.")]
        Right = 2097152,
        [Desc("The middle mouse button was pressed.")]
        Middle = 4194304,
        [Desc("The first XButton (XBUTTON1) on Microsoft IntelliMouse Explorer was pressed.")]
        XButton1 = 8388608,
        [Desc("The second XButton (XBUTTON2) on Microsoft IntelliMouse Explorer was pressed.")]
        XButton2 = 16777216
    }
    public enum KeyCode
    {
        [Desc("The bitmask to extract modifiers from a key value.")]
        Modifiers = -65536,
        [Desc("No key pressed.")]
        None = 0,
        [Desc("The left mouse button.")]
        LButton = 1,
        [Desc("The right mouse button.")]
        RButton = 2,
        [Desc("The CANCEL key.")]
        Cancel = 3,
        [Desc("The middle mouse button (three-button mouse).")]
        MButton = 4,
        [Desc("The first x mouse button (five-button mouse).")]
        XButton1 = 5,
        [Desc("The second x mouse button (five-button mouse).")]
        XButton2 = 6,
        [Desc("The BACKSPACE key.")]
        Back = 8,
        [Desc("The TAB key.")]
        Tab = 9,
        [Desc("The LINEFEED key.")]
        LineFeed = 10,
        [Desc("The CLEAR key.")]
        Clear = 12,
        [Desc("The RETURN key.")]
        Return = 13,
        [Desc("The ENTER key.")]
        Enter = 13,
        [Desc("The SHIFT key.")]
        ShiftKey = 16,
        [Desc("The CTRL key.")]
        ControlKey = 17,
        [Desc("The ALT key.")]
        Menu = 18,
        [Desc("The PAUSE key.")]
        Pause = 19,
        [Desc("The CAPS LOCK key.")]
        Capital = 20,
        [Desc("The CAPS LOCK key.")]
        CapsLock = 20,
        [Desc("The IME Kana mode key.")]
        KanaMode = 21,
        [Desc("The IME Hanguel mode key. (maintained for compatibility; use HangulMode)")]
        HanguelMode = 21,
        [Desc("The IME Hangul mode key.")]
        HangulMode = 21,
        [Desc("The IME Junja mode key.")]
        JunjaMode = 23,
        [Desc("The IME final mode key.")]
        FinalMode = 24,
        [Desc("The IME Hanja mode key.")]
        HanjaMode = 25,
        [Desc("The IME Kanji mode key.")]
        KanjiMode = 25,
        [Desc("The ESC key.")]
        Escape = 27,
        [Desc("The IME convert key.")]
        IMEConvert = 28,
        [Desc("The IME nonconvert key.")]
        IMENonconvert = 29,
        [Desc("The IME accept key, replaces System.Windows.Forms.Keys.IMEAceept.")]
        IMEAccept = 30,
        [Desc("The IME accept key. Obsolete, use System.Windows.Forms.Keys.IMEAccept instead.")]
        IMEAceept = 30,
        [Desc("The IME mode change key.")]
        IMEModeChange = 31,
        [Desc("The SPACEBAR key.")]
        Space = 32,
        [Desc("The PAGE UP key.")]
        Prior = 33,
        [Desc("The PAGE UP key.")]
        PageUp = 33,
        [Desc("The PAGE DOWN key.")]
        Next = 34,
        [Desc("The PAGE DOWN key.")]
        PageDown = 34,
        [Desc("The END key.")]
        End = 35,
        [Desc("The HOME key.")]
        Home = 36,
        [Desc("The LEFT ARROW key.")]
        Left = 37,
        [Desc("The UP ARROW key.")]
        Up = 38,
        [Desc("The RIGHT ARROW key.")]
        Right = 39,
        [Desc("The DOWN ARROW key.")]
        Down = 40,
        [Desc("The SELECT key.")]
        Select = 41,
        [Desc("The PRINT key.")]
        Print = 42,
        [Desc("The EXECUTE key.")]
        Execute = 43,
        [Desc("The PRINT SCREEN key.")]
        Snapshot = 44,
        [Desc("The PRINT SCREEN key.")]
        PrintScreen = 44,
        [Desc("The INS key.")]
        Insert = 45,
        [Desc("The DEL key.")]
        Delete = 46,
        [Desc("The HELP key.")]
        Help = 47,
        [Desc("The 0 key.")]
        D0 = 48,
        [Desc("The 1 key.")]
        D1 = 49,
        [Desc("The 2 key.")]
        D2 = 50,
        [Desc("The 3 key.")]
        D3 = 51,
        [Desc("The 4 key.")]
        D4 = 52,
        [Desc("The 5 key.")]
        D5 = 53,
        [Desc("The 6 key.")]
        D6 = 54,
        [Desc("The 7 key.")]
        D7 = 55,
        [Desc("The 8 key.")]
        D8 = 56,
        [Desc("The 9 key.")]
        D9 = 57,
        [Desc("The A key.")]
        A = 65,
        [Desc("The B key.")]
        B = 66,
        [Desc("The C key.")]
        C = 67,
        [Desc("The D key.")]
        D = 68,
        [Desc("The E key.")]
        E = 69,
        [Desc("The F key.")]
        F = 70,
        [Desc("The G key.")]
        G = 71,
        [Desc("The H key.")]
        H = 72,
        [Desc("The I key.")]
        I = 73,
        [Desc("The J key.")]
        J = 74,
        [Desc("The K key.")]
        K = 75,
        [Desc("The L key.")]
        L = 76,
        [Desc("The M key.")]
        M = 77,
        [Desc("The N key.")]
        N = 78,
        [Desc("The O key.")]
        O = 79,
        [Desc("The P key.")]
        P = 80,
        [Desc("The Q key.")]
        Q = 81,
        [Desc("The R key.")]
        R = 82,
        [Desc("The S key.")]
        S = 83,
        [Desc("The T key.")]
        T = 84,
        [Desc("The U key.")]
        U = 85,
        [Desc("The V key.")]
        V = 86,
        [Desc("The W key.")]
        W = 87,
        [Desc("The X key.")]
        X = 88,
        [Desc("The Y key.")]
        Y = 89,
        [Desc("The Z key.")]
        Z = 90,
        [Desc("The left Windows logo key (Microsoft Natural Keyboard).")]
        LWin = 91,
        [Desc("The right Windows logo key (Microsoft Natural Keyboard).")]
        RWin = 92,
        [Desc("The application key (Microsoft Natural Keyboard).")]
        Apps = 93,
        [Desc("The computer sleep key.")]
        Sleep = 95,
        [Desc("The 0 key on the numeric keypad.")]
        NumPad0 = 96,
        [Desc("The 1 key on the numeric keypad.")]
        NumPad1 = 97,
        [Desc("The 2 key on the numeric keypad.")]
        NumPad2 = 98,
        [Desc("The 3 key on the numeric keypad.")]
        NumPad3 = 99,
        [Desc("The 4 key on the numeric keypad.")]
        NumPad4 = 100,
        [Desc("The 5 key on the numeric keypad.")]
        NumPad5 = 101,
        [Desc("The 6 key on the numeric keypad.")]
        NumPad6 = 102,
        [Desc("The 7 key on the numeric keypad.")]
        NumPad7 = 103,
        [Desc("The 8 key on the numeric keypad.")]
        NumPad8 = 104,
        [Desc("The 9 key on the numeric keypad.")]
        NumPad9 = 105,
        [Desc("The multiply key.")]
        Multiply = 106,
        [Desc("The add key.")]
        Add = 107,
        [Desc("The separator key.")]
        Separator = 108,
        [Desc("The subtract key.")]
        Subtract = 109,
        [Desc("The decimal key.")]
        Decimal = 110,
        [Desc("The divide key.")]
        Divide = 111,
        [Desc("The F1 key.")]
        F1 = 112,
        [Desc("The F2 key.")]
        F2 = 113,
        [Desc("The F3 key.")]
        F3 = 114,
        [Desc("The F4 key.")]
        F4 = 115,
        [Desc("The F5 key.")]
        F5 = 116,
        [Desc("The F6 key.")]
        F6 = 117,
        [Desc("The F7 key.")]
        F7 = 118,
        [Desc("The F8 key.")]
        F8 = 119,
        [Desc("The F9 key.")]
        F9 = 120,
        [Desc("The F10 key.")]
        F10 = 121,
        [Desc("The F11 key.")]
        F11 = 122,
        [Desc("The F12 key.")]
        F12 = 123,
        [Desc("The F13 key.")]
        F13 = 124,
        [Desc("The F14 key.")]
        F14 = 125,
        [Desc("The F15 key.")]
        F15 = 126,
        [Desc("The F16 key.")]
        F16 = 127,
        [Desc("The F17 key.")]
        F17 = 128,
        [Desc("The F18 key.")]
        F18 = 129,
        [Desc("The F19 key.")]
        F19 = 130,
        [Desc("The F20 key.")]
        F20 = 131,
        [Desc("The F21 key.")]
        F21 = 132,
        [Desc("The F22 key.")]
        F22 = 133,
        [Desc("The F23 key.")]
        F23 = 134,
        [Desc("The F24 key.")]
        F24 = 135,
        [Desc("The NUM LOCK key.")]
        NumLock = 144,
        [Desc("The SCROLL LOCK key.")]
        Scroll = 145,
        [Desc("The left SHIFT key.")]
        LShiftKey = 160,
        [Desc("The right SHIFT key.")]
        RShiftKey = 161,
        [Desc("The left CTRL key.")]
        LControlKey = 162,
        [Desc("The right CTRL key.")]
        RControlKey = 163,
        [Desc("The left ALT key.")]
        LMenu = 164,
        [Desc("The right ALT key.")]
        RMenu = 165,
        [Desc("The browser back key.")]
        BrowserBack = 166,
        [Desc("The browser forward key.")]
        BrowserForward = 167,
        [Desc("The browser refresh key.")]
        BrowserRefresh = 168,
        [Desc("The browser stop key.")]
        BrowserStop = 169,
        [Desc("The browser search key.")]
        BrowserSearch = 170,
        [Desc("The browser favorites key.")]
        BrowserFavorites = 171,
        [Desc("The browser home key.")]
        BrowserHome = 172,
        [Desc("The volume mute key.")]
        VolumeMute = 173,
        [Desc("The volume down key.")]
        VolumeDown = 174,
        [Desc("The volume up key.")]
        VolumeUp = 175,
        [Desc("The media next track key.")]
        MediaNextTrack = 176,
        [Desc("The media previous track key.")]
        MediaPreviousTrack = 177,
        [Desc("The media Stop key.")]
        MediaStop = 178,
        [Desc("The media play pause key.")]
        MediaPlayPause = 179,
        [Desc("The launch mail key.")]
        LaunchMail = 180,
        [Desc("The select media key.")]
        SelectMedia = 181,
        [Desc("The start application one key.")]
        LaunchApplication1 = 182,
        [Desc("The start application two key.")]
        LaunchApplication2 = 183,
        [Desc("The OEM Semicolon key on a US standard keyboard.")]
        OemSemicolon = 186,
        [Desc("The OEM 1 key.")]
        Oem1 = 186,
        [Desc("The OEM plus key on any country/region keyboard.")]
        Oemplus = 187,
        [Desc("The OEM comma key on any country/region keyboard.")]
        Oemcomma = 188,
        [Desc("The OEM minus key on any country/region keyboard.")]
        OemMinus = 189,
        [Desc("The OEM period key on any country/region keyboard.")]
        OemPeriod = 190,
        [Desc("The OEM question mark key on a US standard keyboard.")]
        OemQuestion = 191,
        [Desc("The OEM 2 key.")]
        Oem2 = 191,
        [Desc("The OEM tilde key on a US standard keyboard.")]
        Oemtilde = 192,
        [Desc("The OEM 3 key.")]
        Oem3 = 192,
        [Desc("The OEM open bracket key on a US standard keyboard.")]
        OemOpenBrackets = 219,
        [Desc("The OEM 4 key.")]
        Oem4 = 219,
        [Desc("The OEM pipe key on a US standard keyboard.")]
        OemPipe = 220,
        [Desc("The OEM 5 key.")]
        Oem5 = 220,
        [Desc("The OEM close bracket key on a US standard keyboard.")]
        OemCloseBrackets = 221,
        [Desc("The OEM 6 key.")]
        Oem6 = 221,
        [Desc("The OEM singled/double quote key on a US standard keyboard.")]
        OemQuotes = 222,
        [Desc("The OEM 7 key.")]
        Oem7 = 222,
        [Desc("The OEM 8 key.")]
        Oem8 = 223,
        [Desc("The OEM angle bracket or backslash key on the RT 102 key keyboard.")]
        OemBackslash = 226,
        [Desc("The OEM 102 key.")]
        Oem102 = 226,
        [Desc("The PROCESS KEY key.")]
        ProcessKey = 229,
        [Desc("Used to pass Unicode characters as if they were keystrokes. The Packet key value is the low word of a 32-bit virtual-key value used for non-keyboard input methods.")]
        Packet = 231,
        [Desc("The ATTN key.")]
        Attn = 246,
        [Desc("The CRSEL key.")]
        Crsel = 247,
        [Desc("The EXSEL key.")]
        Exsel = 248,
        [Desc("The ERASE EOF key.")]
        EraseEof = 249,
        [Desc("The PLAY key.")]
        Play = 250,
        [Desc("The ZOOM key.")]
        Zoom = 251,
        [Desc("A constant reserved for future use.")]
        NoName = 252,
        [Desc("The PA1 key.")]
        Pa1 = 253,
        [Desc("The CLEAR key.")]
        OemClear = 254,
        [Desc("The bitmask to extract a key code from a key value.")]
        KeyCode = 65535,
        [Desc("The SHIFT modifier key.")]
        ModifierShift = 65536,
        [Desc("The CTRL modifier key.")]
        ModifierControl = 131072,
        [Desc("The ALT modifier key.")]
        ModifierAlt = 262144
    }


}

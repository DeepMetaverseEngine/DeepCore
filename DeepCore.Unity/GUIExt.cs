using DeepCore.GUI.Input;
using System;
using UnityEngine;
using BKeyCode = DeepCore.GUI.Input.KeyCode;

namespace DeepCore.Unity
{
    public static class InputHelper
    {
        public static bool IsMouse(out MouseButton btn, Func<int, bool> test)
        {
            if (test(0)) { btn = MouseButton.Left; return true; }
            if (test(1)) { btn = MouseButton.Right; return true; }
            if (test(2)) { btn = MouseButton.Middle; return true; }
            btn = MouseButton.None; return false;
        }
        public static bool IsMouseDown(out MouseButton btn)
        {
            return IsMouse(out btn, Input.GetMouseButtonDown);
        }
        public static bool IsMouseUp(out MouseButton btn)
        {
            return IsMouse(out btn, Input.GetMouseButtonUp);
        }
        public static bool IsMouseHold(out MouseButton btn)
        {
            btn = GetMouseButton();
            return btn != GUI.Input.MouseButton.None;
        }
        public static MouseButton GetMouseButton()
        {
            if (Input.GetMouseButton(0)) { return MouseButton.Left; }
            if (Input.GetMouseButton(1)) { return MouseButton.Right; }
            if (Input.GetMouseButton(2)) { return MouseButton.Middle; }
            return GUI.Input.MouseButton.None;
        }

        public static bool IsKey(out BKeyCode kc, Func<UnityEngine.KeyCode, bool> test)
        {
            if (test(UnityEngine.KeyCode.Backspace))       /**/{ kc = BKeyCode.Back; return true; }
            if (test(UnityEngine.KeyCode.Delete))          /**/{ kc = BKeyCode.Delete; return true; }
            if (test(UnityEngine.KeyCode.Tab))             /**/{ kc = BKeyCode.Tab; return true; }
            if (test(UnityEngine.KeyCode.Clear))           /**/{ kc = BKeyCode.Clear; return true; }
            if (test(UnityEngine.KeyCode.Return))          /**/{ kc = BKeyCode.Return; return true; }
            if (test(UnityEngine.KeyCode.Pause))           /**/{ kc = BKeyCode.Pause; return true; }
            if (test(UnityEngine.KeyCode.Escape))          /**/{ kc = BKeyCode.Escape; return true; }
            if (test(UnityEngine.KeyCode.Space))           /**/{ kc = BKeyCode.Space; return true; }
            if (test(UnityEngine.KeyCode.Keypad0))         /**/{ kc = BKeyCode.NumPad0; return true; }
            if (test(UnityEngine.KeyCode.Keypad1))         /**/{ kc = BKeyCode.NumPad1; return true; }
            if (test(UnityEngine.KeyCode.Keypad2))         /**/{ kc = BKeyCode.NumPad2; return true; }
            if (test(UnityEngine.KeyCode.Keypad3))         /**/{ kc = BKeyCode.NumPad3; return true; }
            if (test(UnityEngine.KeyCode.Keypad4))         /**/{ kc = BKeyCode.NumPad4; return true; }
            if (test(UnityEngine.KeyCode.Keypad5))         /**/{ kc = BKeyCode.NumPad5; return true; }
            if (test(UnityEngine.KeyCode.Keypad6))         /**/{ kc = BKeyCode.NumPad6; return true; }
            if (test(UnityEngine.KeyCode.Keypad7))         /**/{ kc = BKeyCode.NumPad7; return true; }
            if (test(UnityEngine.KeyCode.Keypad8))         /**/{ kc = BKeyCode.NumPad8; return true; }
            if (test(UnityEngine.KeyCode.Keypad9))         /**/{ kc = BKeyCode.NumPad9; return true; }
            if (test(UnityEngine.KeyCode.KeypadPeriod))    /**/{ kc = BKeyCode.OemPeriod; return true; }
            if (test(UnityEngine.KeyCode.KeypadDivide))    /**/{ kc = BKeyCode.Divide; return true; }
            if (test(UnityEngine.KeyCode.KeypadMultiply))  /**/{ kc = BKeyCode.Multiply; return true; }
            if (test(UnityEngine.KeyCode.KeypadMinus))     /**/{ kc = BKeyCode.OemMinus; return true; }
            if (test(UnityEngine.KeyCode.KeypadPlus))      /**/{ kc = BKeyCode.Oemplus; return true; }
            if (test(UnityEngine.KeyCode.KeypadEnter))     /**/{ kc = BKeyCode.Enter; return true; }
            //if (test(UnityEngine.KeyCode.KeypadEquals))    /**/{ kc = BKeyCode.KeypadEquals; return true; }
            if (test(UnityEngine.KeyCode.UpArrow))         /**/{ kc = BKeyCode.Up; return true; }
            if (test(UnityEngine.KeyCode.DownArrow))       /**/{ kc = BKeyCode.Down; return true; }
            if (test(UnityEngine.KeyCode.RightArrow))      /**/{ kc = BKeyCode.Right; return true; }
            if (test(UnityEngine.KeyCode.LeftArrow))       /**/{ kc = BKeyCode.Left; return true; }
            if (test(UnityEngine.KeyCode.Insert))          /**/{ kc = BKeyCode.Insert; return true; }
            if (test(UnityEngine.KeyCode.Home))            /**/{ kc = BKeyCode.Home; return true; }
            if (test(UnityEngine.KeyCode.End))             /**/{ kc = BKeyCode.End; return true; }
            if (test(UnityEngine.KeyCode.PageUp))          /**/{ kc = BKeyCode.PageUp; return true; }
            if (test(UnityEngine.KeyCode.PageDown))        /**/{ kc = BKeyCode.PageDown; return true; }
            if (test(UnityEngine.KeyCode.F1))              /**/{ kc = BKeyCode.F1; return true; }
            if (test(UnityEngine.KeyCode.F2))              /**/{ kc = BKeyCode.F2; return true; }
            if (test(UnityEngine.KeyCode.F3))              /**/{ kc = BKeyCode.F3; return true; }
            if (test(UnityEngine.KeyCode.F4))              /**/{ kc = BKeyCode.F4; return true; }
            if (test(UnityEngine.KeyCode.F5))              /**/{ kc = BKeyCode.F5; return true; }
            if (test(UnityEngine.KeyCode.F6))              /**/{ kc = BKeyCode.F6; return true; }
            if (test(UnityEngine.KeyCode.F7))              /**/{ kc = BKeyCode.F7; return true; }
            if (test(UnityEngine.KeyCode.F8))              /**/{ kc = BKeyCode.F8; return true; }
            if (test(UnityEngine.KeyCode.F9))              /**/{ kc = BKeyCode.F9; return true; }
            if (test(UnityEngine.KeyCode.F10))             /**/{ kc = BKeyCode.F10; return true; }
            if (test(UnityEngine.KeyCode.F11))             /**/{ kc = BKeyCode.F11; return true; }
            if (test(UnityEngine.KeyCode.F12))             /**/{ kc = BKeyCode.F12; return true; }
            if (test(UnityEngine.KeyCode.F13))             /**/{ kc = BKeyCode.F13; return true; }
            if (test(UnityEngine.KeyCode.F14))             /**/{ kc = BKeyCode.F14; return true; }
            if (test(UnityEngine.KeyCode.F15))             /**/{ kc = BKeyCode.F15; return true; }
            if (test(UnityEngine.KeyCode.Alpha0))          /**/{ kc = BKeyCode.D0; return true; }
            if (test(UnityEngine.KeyCode.Alpha1))          /**/{ kc = BKeyCode.D1; return true; }
            if (test(UnityEngine.KeyCode.Alpha2))          /**/{ kc = BKeyCode.D2; return true; }
            if (test(UnityEngine.KeyCode.Alpha3))          /**/{ kc = BKeyCode.D3; return true; }
            if (test(UnityEngine.KeyCode.Alpha4))          /**/{ kc = BKeyCode.D4; return true; }
            if (test(UnityEngine.KeyCode.Alpha5))          /**/{ kc = BKeyCode.D5; return true; }
            if (test(UnityEngine.KeyCode.Alpha6))          /**/{ kc = BKeyCode.D6; return true; }
            if (test(UnityEngine.KeyCode.Alpha7))          /**/{ kc = BKeyCode.D7; return true; }
            if (test(UnityEngine.KeyCode.Alpha8))          /**/{ kc = BKeyCode.D8; return true; }
            if (test(UnityEngine.KeyCode.Alpha9))          /**/{ kc = BKeyCode.D9; return true; }
            //if (test(UnityEngine.KeyCode.Exclaim))         /**/{ kc = BKeyCode.Exclaim; return true; }
            //if (test(UnityEngine.KeyCode.DoubleQuote))     /**/{ kc = BKeyCode.DoubleQuote; return true; }
            //if (test(UnityEngine.KeyCode.Hash))            /**/{ kc = BKeyCode.Hash; return true; }
            //if (test(UnityEngine.KeyCode.Dollar))          /**/{ kc = BKeyCode.Dollar; return true; }
            //if (test(UnityEngine.KeyCode.Percent))         /**/{ kc = BKeyCode.Percent; return true; }
            //if (test(UnityEngine.KeyCode.Ampersand))       /**/{ kc = BKeyCode.Ampersand; return true; }
            if (test(UnityEngine.KeyCode.Quote))           /**/{ kc = BKeyCode.OemQuotes; return true; }
            //if (test(UnityEngine.KeyCode.LeftParen))       /**/{ kc = BKeyCode.Left; return true; }
            //if (test(UnityEngine.KeyCode.RightParen))      /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.Asterisk))        /**/{ kc = BKeyCode.Back; return true; }
            if (test(UnityEngine.KeyCode.Plus))            /**/{ kc = BKeyCode.Oemplus; return true; }
            if (test(UnityEngine.KeyCode.Comma))           /**/{ kc = BKeyCode.Oemcomma; return true; }
            if (test(UnityEngine.KeyCode.Minus))           /**/{ kc = BKeyCode.OemMinus; return true; }
            if (test(UnityEngine.KeyCode.Period))          /**/{ kc = BKeyCode.OemPeriod; return true; }
            if (test(UnityEngine.KeyCode.Slash))           /**/{ kc = BKeyCode.OemBackslash; return true; }
            if (test(UnityEngine.KeyCode.Colon))           /**/{ kc = BKeyCode.OemSemicolon; return true; }
            if (test(UnityEngine.KeyCode.Semicolon))       /**/{ kc = BKeyCode.OemSemicolon; return true; }
            //if (test(UnityEngine.KeyCode.Less))            /**/{ kc = BKeyCode.; return true; }
            //if (test(UnityEngine.KeyCode.Equals))          /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.Greater))         /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.Question))        /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.At))              /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.LeftBracket))     /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.Backslash))       /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.RightBracket))    /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.Caret))           /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.Underscore))      /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.BackQuote))       /**/{ kc = BKeyCode.Back; return true; }
            if (test(UnityEngine.KeyCode.A))               /**/{ kc = BKeyCode.A; return true; }
            if (test(UnityEngine.KeyCode.B))               /**/{ kc = BKeyCode.B; return true; }
            if (test(UnityEngine.KeyCode.C))               /**/{ kc = BKeyCode.C; return true; }
            if (test(UnityEngine.KeyCode.D))               /**/{ kc = BKeyCode.D; return true; }
            if (test(UnityEngine.KeyCode.E))               /**/{ kc = BKeyCode.E; return true; }
            if (test(UnityEngine.KeyCode.F))               /**/{ kc = BKeyCode.F; return true; }
            if (test(UnityEngine.KeyCode.G))               /**/{ kc = BKeyCode.G; return true; }
            if (test(UnityEngine.KeyCode.H))               /**/{ kc = BKeyCode.H; return true; }
            if (test(UnityEngine.KeyCode.I))               /**/{ kc = BKeyCode.I; return true; }
            if (test(UnityEngine.KeyCode.J))               /**/{ kc = BKeyCode.J; return true; }
            if (test(UnityEngine.KeyCode.K))               /**/{ kc = BKeyCode.K; return true; }
            if (test(UnityEngine.KeyCode.L))               /**/{ kc = BKeyCode.L; return true; }
            if (test(UnityEngine.KeyCode.M))               /**/{ kc = BKeyCode.M; return true; }
            if (test(UnityEngine.KeyCode.N))               /**/{ kc = BKeyCode.N; return true; }
            if (test(UnityEngine.KeyCode.O))               /**/{ kc = BKeyCode.O; return true; }
            if (test(UnityEngine.KeyCode.P))               /**/{ kc = BKeyCode.P; return true; }
            if (test(UnityEngine.KeyCode.Q))               /**/{ kc = BKeyCode.Q; return true; }
            if (test(UnityEngine.KeyCode.R))               /**/{ kc = BKeyCode.R; return true; }
            if (test(UnityEngine.KeyCode.S))               /**/{ kc = BKeyCode.S; return true; }
            if (test(UnityEngine.KeyCode.T))               /**/{ kc = BKeyCode.T; return true; }
            if (test(UnityEngine.KeyCode.U))               /**/{ kc = BKeyCode.U; return true; }
            if (test(UnityEngine.KeyCode.V))               /**/{ kc = BKeyCode.V; return true; }
            if (test(UnityEngine.KeyCode.W))               /**/{ kc = BKeyCode.W; return true; }
            if (test(UnityEngine.KeyCode.X))               /**/{ kc = BKeyCode.X; return true; }
            if (test(UnityEngine.KeyCode.Y))               /**/{ kc = BKeyCode.Y; return true; }
            if (test(UnityEngine.KeyCode.Z))               /**/{ kc = BKeyCode.Z; return true; }
            if (test(UnityEngine.KeyCode.Numlock))         /**/{ kc = BKeyCode.NumLock; return true; }
            if (test(UnityEngine.KeyCode.CapsLock))        /**/{ kc = BKeyCode.CapsLock; return true; }
            if (test(UnityEngine.KeyCode.ScrollLock))      /**/{ kc = BKeyCode.Scroll; return true; }
            if (test(UnityEngine.KeyCode.RightShift))      /**/{ kc = BKeyCode.ShiftKey | BKeyCode.ModifierShift; return true; }
            if (test(UnityEngine.KeyCode.LeftShift))       /**/{ kc = BKeyCode.LShiftKey | BKeyCode.ModifierShift; return true; }
            if (test(UnityEngine.KeyCode.RightControl))    /**/{ kc = BKeyCode.ControlKey | BKeyCode.ModifierControl; return true; }
            if (test(UnityEngine.KeyCode.LeftControl))     /**/{ kc = BKeyCode.LControlKey | BKeyCode.ModifierControl; return true; }
            if (test(UnityEngine.KeyCode.RightAlt))        /**/{ kc = BKeyCode.Menu | BKeyCode.ModifierAlt; return true; }
            if (test(UnityEngine.KeyCode.LeftAlt))         /**/{ kc = BKeyCode.Menu | BKeyCode.ModifierAlt; return true; }
            //if (test(UnityEngine.KeyCode.LeftCommand))     /**/{ kc = BKeyCode.Oemco; return true; }
            //if (test(UnityEngine.KeyCode.LeftApple))       /**/{ kc = BKeyCode.Back; return true; }
            if (test(UnityEngine.KeyCode.LeftWindows))     /**/{ kc = BKeyCode.LWin; return true; }
            //if (test(UnityEngine.KeyCode.RightCommand))    /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.RightApple))      /**/{ kc = BKeyCode.Back; return true; }
            if (test(UnityEngine.KeyCode.RightWindows))    /**/{ kc = BKeyCode.RWin; return true; }
            //if (test(UnityEngine.KeyCode.AltGr))           /**/{ kc = BKeyCode.ModifierAlt; return true; }
            if (test(UnityEngine.KeyCode.Help))            /**/{ kc = BKeyCode.Help; return true; }
            //if (test(UnityEngine.KeyCode.Print))           /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.SysReq))          /**/{ kc = BKeyCode.Back; return true; }
            //if (test(UnityEngine.KeyCode.Break))           /**/{ kc = BKeyCode.; return true; }
            if (test(UnityEngine.KeyCode.Menu))            /**/{ kc = BKeyCode.Menu; return true; }
            kc = BKeyCode.None;
            return false;
        }
        public static bool IsKeyDown(out BKeyCode btn)
        {
            return IsKey(out btn, Input.GetKeyDown);
        }
        public static bool IsKeyUp(out BKeyCode btn)
        {
            return IsKey(out btn, Input.GetKeyUp);
        }
    }
}

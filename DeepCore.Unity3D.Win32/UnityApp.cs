using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.XR;
using static DeepCore.Colors;

namespace DeepCore.Unity
{
    public class UnityApp
    {
        public static int ParentHWND()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.TryIndexOf("-parentHWND", out var index))
            {
                try
                {
                    return Parser.ParseInt(args[index + 1].Trim());
                    //parentHWND = System.Diagnostics.Process.GetProcessById();
                }
                catch { }
            }
            return 0;
        }
        public enum GetWindowCMD
        {
            // The highest window in the Z-order having the same parent as the given window.
            GW_HWNDFIRST = 0,
            //The lowest window in the Z-order having the same parent as the given window.
            GW_HWNDLAST = 1,
            //The window below the given window in the Z-order.
            GW_HWNDNEXT = 2,
            //The window above the given window in the Z-order.
            GW_HWNDPREV = 3,
            //The window that owns the given window (not to be confused with the parent window).
            GW_OWNER = 4,
            //The topmost of the given window's child windows. This has the same effect as using the GetTopWindow function.
            GW_CHILD = 5,
        }

        public static IntPtr GetWindow(int hwnd, GetWindowCMD cmd)
        {
            return GetWindow(new IntPtr(hwnd), new IntPtr((int)cmd));
        }
        public static IntPtr GetWindow(int hwnd)
        {
            return GetWindow(new IntPtr(hwnd), new IntPtr(0));
        }

        public static IntPtr GetCurrentWindowHandle()
        {
            IntPtr returnHwnd = IntPtr.Zero;
            var threadId = GetCurrentThreadId();
            EnumThreadWindows(threadId,
                (hWnd, lParam) =>
                {
                    if (returnHwnd == IntPtr.Zero) returnHwnd = hWnd;
                    return true;
                }, IntPtr.Zero);
            return returnHwnd;
        }


        public delegate bool EnumThreadDelegate(IntPtr hwnd, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "GetWindow")] static extern IntPtr GetWindow(IntPtr hwnd, IntPtr cmd);
        [DllImport("user32.dll")] public static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern IntPtr GetFocus();
        [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
        [DllImport("Kernel32.dll")] public static extern int GetCurrentThreadId();

    }
}

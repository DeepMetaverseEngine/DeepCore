using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DeepCore
{
    /*
     * Since Windows 10 Anniversary Update, console can use ANSI/VT100 color codes
     * You need set flag ENABLE_VIRTUAL_TERMINAL_PROCESSING(0x4) by SetConsoleMode
     * Use sequences:
     * "\x1b[48;5;" + s + "m" - set background color by index in table (0-255)
     * "\x1b[38;5;" + s + "m" - set foreground color by index in table (0-255)
     * "\x1b[48;2;" + r + ";" + g + ";" + b + "m" - set background by r,g,b values
     * "\x1b[38;2;" + r + ";" + g + ";" + b + "m" - set foreground by r,g,b values
     * Important notice: Internally Windows have only 256 (or 88) colors in table and Windows will used nearest to (r,g,b) value from table.
     */
    public class ColorConsole
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr handle, out int mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int handle);

        public ColorConsole()
        {
            var handle = GetStdHandle(-11);
            GetConsoleMode(handle, out int mode);
            SetConsoleMode(handle, mode | 0x4);
        }
        public ColorConsole Write(string ch)
        {
            Write(ch, 0xFFFFFF);
            return this;
        }
        public ColorConsole WriteLine(string ch)
        {
            WriteLine(ch, 0xFFFFFF);
            return this;
        }

        public ColorConsole Write(string ch, byte R, byte G, byte B)
        {
            Console.Write($"\x1b[38;2;{R};{G};{B}m{ch}");
            return this;
        }
        public ColorConsole WriteLine(string ch, byte R, byte G, byte B)
        {
            Console.WriteLine($"\x1b[38;2;{R};{G};{B}m{ch}");
            return this;
        }

        public ColorConsole Write(string ch, int RGB)
        {
            DecodeRGB(RGB, out var r, out var g, out var b);
            return Write(ch, r, g, b);
        }
        public ColorConsole WriteLine(string ch, int RGB)
        {
            DecodeRGB(RGB, out var r, out var g, out var b);
            return WriteLine(ch, r, g, b);
        }

        public static void DecodeRGB(int rgb, out byte r, out byte g, out byte b)
        {
            r = (byte)((0x00ff0000 & rgb) >> 16);
            g = (byte)((0x0000ff00 & rgb) >> 8);
            b = (byte)((0x000000ff & rgb) >> 0);
        }
    }
}

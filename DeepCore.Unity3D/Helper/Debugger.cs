using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
#if UNITY_EDITOR
using System.Linq;
#endif

namespace DeepCore.Unity3D
{
    public class Debugger
    {
        private static bool mUseDebug = false;

        public static bool UseDebug
        {
            set { mUseDebug = value; }
            get { return mUseDebug; }
        }

        private static Dictionary<string, string> mTypeColor = new Dictionary<string, string>();

        private static Dictionary<string, int> mColorUsed = new Dictionary<string, int>();
        public enum LogType
        { 
            ALL,
            LOG,
            LOG_WARING,
            LOG_ERROR,
            NONE,
        }

        public static LogType LogLevel = LogType.ALL;

        //static StringBuilder sb = new StringBuilder(100);
        public static bool IsDebugBuild = false;

        public static string FormatString(string text)
        {
#if UNITY_EDITOR && !UNITY_ANDROID
        var l = text.IndexOf('[');

        if (l >= 0)
        {
            var r = text.IndexOf(']', l);
            if (r >= 0 && (r - l) > 1 && (l + 1 + r) <= text.Length)
            {
                var type = text.Substring(l + 1, r);
                string color;
                if (mTypeColor.ContainsKey(type))
                {
                    color = mTypeColor[type];
                }
                else
                {
                    if (mColorUsed.Count == 0)
                    {
                        mColorUsed["aqua"] = 0;
                        mColorUsed["green"] = 0;
                        mColorUsed["lightblue"] = 0;
                        mColorUsed["lime"] = 0;
                        mColorUsed["magenta"] = 0;
                        mColorUsed["orange"] = 0;
                        mColorUsed["teal"] = 0;
                        mColorUsed["yellow"] = 0;
                        mColorUsed["blue"] = 0;
                    }
                    var lst = mColorUsed.ToList();
                    lst.Sort((firstPair, nextPair) =>
                    {
                        return firstPair.Value.CompareTo(nextPair.Value);
                    }
                    );
                    color = lst[0].Key;
                    mTypeColor[type] = color;
                    mColorUsed[color] = mColorUsed[color] + 1;
                }

                text = text.Insert(r, "</color>");
                text = text.Insert(l + 1, "<color='" + color + "'>");
            }
        }
        return System.DateTime.Now.ToString("<color='olive'>(H:mm:ss.fff)</color> ") + text;
#else
            //sb.Length = 0;
            //sb.Append(System.DateTime.Now.ToString("(H:mm:ss.fff) "));
            //sb.Append(text);
            //return sb.ToString();
            return text;
#endif
        }

        public static void Log(string str, params object[] args)
        {
            if (LogLevel <= LogType.LOG)
            {
                str = string.Format(str, args);
                Debug.Log(FormatString(str));
            }
        }

        public static void LogWarning(string str, params object[] args)
        {
            if (LogLevel <= LogType.LOG_WARING)
            {
                str = string.Format(str, args);
                Debug.LogWarning(FormatString(str));
            }
        }

        public static void LogError(string str, params object[] args)
        {
            if (LogLevel <= LogType.LOG_ERROR)
            {
                str = string.Format(str, args);
                Debug.LogError(FormatString(str));
            }
        }

        public static void Log(string str)
        {
            if (LogLevel <= LogType.LOG)
            {
                Debug.Log(FormatString(str));
            }
        }

        public static void LogWarning(string str)
        {
            if (LogLevel <= LogType.LOG_WARING)
            {
                Debug.LogWarning(FormatString(str));
            }
        }

        public static void LogError(string str)
        {
            if (LogLevel <= LogType.LOG_ERROR)
            {
                Debug.LogError(FormatString(str));
            }  
        }

        public static void LogException(Exception e)
        {
            Debug.LogError(FormatString(e.Message + "\n" + e.StackTrace));
        }
    }
}
using UnityEngine;
using System;
using System.Collections.Generic;
 
/// <summary>
/// A console that displays the contents of Unity's debug log.
/// </summary>
/// <remarks>
/// Developed by Matthew Miner (www.matthewminer.com)
/// Permission is given to use this script however you please with absolutely no restrictions.
/// </remarks>
///
namespace TianyuConsole
{
    public class Console : MonoBehaviour
    {

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        private UnityEngine.Object m_MsgLock = new UnityEngine.Object();

        struct ConsoleMessage
        {
            public readonly string message;
            public readonly string stackTrace;
            public readonly LogType type;

            public ConsoleMessage(string message, string stackTrace, LogType type)
            {
                this.message = message;
                this.stackTrace = stackTrace;
                this.type = type;
            }
        }

        public KeyCode toggleKey = KeyCode.BackQuote;

        List<ConsoleMessage> entries = new List<ConsoleMessage>();
        Vector2 scrollPos;
        bool show;
        bool collapse;

        Rect windowRect = new Rect(0.02f * Screen.width, 0.02f * Screen.height, Screen.width - (2 * 0.02f * Screen.width), Screen.height - (2 * 0.02f * Screen.height));

        GUIContent clearLabel = new GUIContent("Clear", "Clear the contents of the console.");
        GUIContent collapseLabel = new GUIContent("Collapse", "Hide repeated messages.");

        void OnEnable()
        {
            Application.RegisterLogCallback(HandleLog);
        }

        void OnDisable()
        {
            Application.RegisterLogCallback(null);
        }

        void Awake()
        {
            GameObject.DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                show = !show;
            }

            UpdateHelper();
        }

        bool bOut = true;

        void OnGUI()
        {
            if (GUI.Button(new Rect(0, 0, 0.05f * Screen.width, 0.03f * Screen.height), "Debug"))
            {
                show = !show;
            }

            bOut = GUI.Toggle(new Rect(0.05f * Screen.width, 0, 0.05f * Screen.width, 0.03f * Screen.height), bOut, "D");

            if (!show)
            {
                return;
            }

            windowRect = GUILayout.Window(123456, windowRect, ConsoleWindow, "Console");
        }

        /// <summary>
        /// A window displaying the logged messages.
        /// </summary>
        /// <param name="windowID">The window's ID.</param>
        void ConsoleWindow(int windowID)
        {
            lock (m_MsgLock)
            {
                scrollPos = GUILayout.BeginScrollView(scrollPos);

                // Go through each logged entry
                for (int i = 0; i < entries.Count; i++)
                {
                    ConsoleMessage entry = entries[i];

                    // If this message is the same as the last one and the collapse feature is chosen, skip it
                    if (collapse && i > 0 && entry.message == entries[i - 1].message)
                    {
                        continue;
                    }

                    // Change the text colour according to the log type
                    switch (entry.type)
                    {
                        case LogType.Error:
                        case LogType.Exception:
                            GUI.contentColor = Color.red;
                            break;

                        case LogType.Warning:
                            GUI.contentColor = Color.yellow;
                            break;

                        default:
                            GUI.contentColor = Color.white;
                            break;
                    }

                    GUILayout.Label(entry.message);
                }

                GUI.contentColor = Color.white;

                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();

                // Clear button
                if (GUILayout.Button(clearLabel))
                {
                    entries.Clear();
                }

                // Collapse toggle
                collapse = GUILayout.Toggle(collapse, collapseLabel, GUILayout.ExpandWidth(false));

                GUILayout.EndHorizontal();

                // Set the window to be draggable by the top title bar
                GUI.DragWindow(new Rect(0, 0, 10000, 20));
            }
        }

        /// <summary>
        /// Logged messages are sent through this callback function.
        /// </summary>
        /// <param name="message">The message itself.</param>
        /// <param name="stackTrace">A trace of where the message came from.</param>
        /// <param name="type">The type of message: error/exception, warning, or assert.</param>
        void HandleLog(string message, string stackTrace, LogType type)
        {
            ABFileLog.Write("Tianyu", type, message + " ->  " + stackTrace);

            if (!bOut)
                return;

            AutoClear();

            lock (m_MsgLock)
            {
                ConsoleMessage entry = new ConsoleMessage(message, stackTrace, type);
                entries.Add(entry);
            }
        }

        public void Log(string message)
        {
            AutoClear();

            lock (m_MsgLock)
            {
                ConsoleMessage entry = new ConsoleMessage(message, "", LogType.Log);
                entries.Add(entry);
            }
        }

        public void LogWarning(string message)
        {
            AutoClear();

            lock (m_MsgLock)
            {
                ConsoleMessage entry = new ConsoleMessage(message, "", LogType.Warning);
                entries.Add(entry);
            }
        }

        public void LogError(string message)
        {
            AutoClear();

            lock (m_MsgLock)
            {
                ConsoleMessage entry = new ConsoleMessage(message, "", LogType.Error);
                entries.Add(entry);
            }
        }

        private void AutoClear()
        {
            lock (m_MsgLock)
            {
                if (entries.Count > 200)
                {
                    entries.RemoveRange(0, 100);
                    //entries.Clear();
                }
            }
        }


        #region

        private void UpdateHelper()
        {
            FTime = Time.realtimeSinceStartup;
        }

        public static float FTime = 0.0f;

        public static string STime
        {
            get
            {
                string t1 = FormatTime(FTime);
                string t2 = ("[ " + System.DateTime.Now + " ] \t");

                //			long sec = System.DateTime.Now.Ticks / 10000000;
                //			long ms  = System.DateTime.Now.Ticks % 10000000;
                //			string t2 = ("[ " + sec + "." + ms + " ] \t");

                return t1 + t2;
            }
        }

        public static string FormatTime(float fTime)
        {
            int h = (int)(fTime / (60 * 60));
            int m = (int)((fTime % (60 * 60)) / 60);
            int s = (int)(fTime % 60);
            string sh = h < 10 ? ("0" + h) : ("" + h);
            string sm = m < 10 ? ("0" + m) : ("" + m);
            string ss = s < 10 ? ("0" + s) : ("" + s);

            string t1 = ("[ " + sh + " : " + sm + " : " + ss + " ] \t");
            return t1;
        }

        #endregion
    }

}

using DeepCore;
using DeepCore.Reflection;
using System;
using UnityEngine;
public class UnityExeLauncher : MonoBehaviour
{
    public static Properties CommandLineArgs = new Properties();
    public string MainClass;
    public Type MainType;

    protected virtual void Start()
    {
        try
        {
            new UnityLoggerFactory();
            string[] args = Environment.GetCommandLineArgs();
            CommandLineArgs = Properties.ParseArgs(args);
            //ReflectionUtil.LoadDlls(new FileInfo(GetType().Assembly.Location).Directory);
            if (CommandLineArgs.TryGetValue("MainClass", out var _MainClass))
            {
                MainClass = _MainClass;
            }
            if (MainClass != null)
            {
                MainType = ReflectionUtil.GetType(MainClass);
                var comp = this.gameObject.AddComponent(MainType);
            }
            else
            {
                Debug.LogError("No Main Class");
                Application.Quit(-1);
            }
        }
        catch (Exception err)
        {
            Debug.LogError(err);
            Application.Quit(-1);
        }
    }

    protected virtual void Update()
    {

    }
}
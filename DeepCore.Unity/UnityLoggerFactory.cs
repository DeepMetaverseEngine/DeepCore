using DeepCore.Log;
using System;


public partial class UnityLoggerFactory : LoggerFactory
{
    protected override Logger CreateLogger(object name)
    {
        return new UnityLogger(name.ToString());
    }
}
public partial class UnityLogger : Logger
{
    public UnityLogger(string name) : base(LoggerFactory.CurrentFactory, name)
    {
    }

    protected override void Print(LoggerLevel level, object format, string msg, Exception err)
    {
        base.Print(level, format, msg, err);
        if (err != null)
        {
            UnityEngine.Debug.LogException(err);
        }
    }
    protected override void PrintText(string text, LoggerLevel level)
    {
        if (IsErrorEnabled && (level & LoggerLevel.ERROR) != 0)
        {
            UnityEngine.Debug.LogError(text);
        }
        else if (IsFatalEnabled && (level & LoggerLevel.FATAL) != 0)
        {
            UnityEngine.Debug.LogError(text);
        }
        else if (IsWarnEnabled && (level & LoggerLevel.WARNNING) != 0)
        {
            UnityEngine.Debug.LogWarning(text);
        }
        else
        {
            UnityEngine.Debug.Log(text);
        }
    }



}

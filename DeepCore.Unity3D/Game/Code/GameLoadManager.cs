using System;

namespace Code;

public class GameLoadManager
{
    public static GameLoadManager Instance = new Lazy<GameLoadManager>(() => new GameLoadManager()).Value;
    private int process = -1;
    public bool LoadComplete { get; private set; }
    
    private float Loading()
    {
        var bProcess = BattleRuntimeManager.Instance.GetLoadingProcess();
        switch (process)
        {
            case (int) Process.Start:
                break;
            case (int) Process.TemplateLoadComplete:
                if(bProcess is not BattleRuntime.ProcessState.Template)
                    return process;
                break;
            case (int) Process.ResLoadComplete:
                if (bProcess is not BattleRuntime.ProcessState.ResEnd)
                    return process;
                break;
            case (int) Process.BattleLoadComplete:
                if (bProcess is not BattleRuntime.ProcessState.Complete)
                    return process;
                break;
            case (int) Process.HUDLoadComplete:
                if (!HUDLoadComplete())
                    return process;
                break;
            case (int) Process.Other:
                break;
        }

        process++;
        var complete = (int) Process.Complete;
        if (process >= complete)
        {
            process = complete;
            LoadComplete = true;
        }
        
        return process / 100f;
    }

    private bool HUDLoadComplete()
    {
        //todo 
        return true;
    }

    private GameLoadManager() { }
    
    private enum Process
    {
        Start = 0,
        
        TemplateLoadComplete = 30,
        ResLoadComplete = 65,
        BattleLoadComplete = 75,
        
        HUDLoadComplete = 89,
        Other = 93,
        
        Complete = 100,
    }
    
    
}
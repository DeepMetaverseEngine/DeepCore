using System;
using System.Collections.Generic;
using Code.BattleView;
using DeepCore;
using Gate.Client;
using Gate.Client.Modules;
using IOGame.Client.Unity.IOBattle;

namespace Code.Battle;

public class BattleRuntimeManager
{
    public UnityBattle CurrentBattle => mBattleRuntime?.UnityBattle;
    public bool UseSample = true;

    public const bool UseCache = false;
    public static BattleRuntimeManager Instance = new Lazy<BattleRuntimeManager>(() => new BattleRuntimeManager()).Value;
    private BattleRuntimeManager(){}
    
    /// <summary>
    /// 运行的场景
    /// </summary>
    private BattleRuntime mBattleRuntime;
    /// <summary>
    /// 缓存
    /// </summary>
    private HashMap<int, BattleRuntime> mCache;
    
    private bool IsLocal;
    
    private void Load(GateBattle battle)
    {
        mBattleRuntime = new BattleRuntime(battle.Layer.Data.ID, IsLocal).Load(battle);
        
    }

    private bool CanUseCache()
    {
        return UseCache;
    }


    #region API
    
    

    public void Init()
    {
        if (GameClient.Instance.Client.TryGetModel<AreaModule>(out var area))
        {
            area.OnZoneChanged += OnZoneChanged;
        }
        
    }

    public void Start(int stage, params object[] args)
    {
    }

    private void OnZoneChanged(GateBattle battle)
    {
        mBattleRuntime.Load(battle);
    }
    
    public BattleRuntime.ProcessState GetLoadingProcess()
    {
        return (mBattleRuntime?.mProcess ?? BattleRuntime.ProcessState.None);
    }

    public void LaunchSkill(int id)
    {
        mBattleRuntime.UnityBattle.Actor.LaunchSkill(id);
    }

    public void LaunchNormalAttack()
    {
        mBattleRuntime.UnityBattle.Actor.LaunchNormalAttack();
    }
    
    #endregion
}
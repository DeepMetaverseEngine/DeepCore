using System;
using System.Collections;
using Code.BattleView;
using Code.System.AB;
using Code.System.Resource;
using DeepCore.Game3D.Host.ZoneRuntime;
using DeepCore.GameData.Zone.ZoneEditor;
using DeepCore.IO;
using Gate.Client;
using IOGame.Client;
using IOGame.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Battle;

public class BattleRuntime
{
    public GameObject gameObject;
    public Transform transform;
    public UnityBattle UnityBattle;

    public int StageID;
    public bool IsLocal;
    public SceneData SceneData;
    
    public bool IsEnable;
    public ProcessState mProcess = ProcessState.None;
    
    
    public BattleRuntime(int stage, bool local)
    {
        StageID = stage;
        IsLocal = local;
    }

    public BattleRuntime Load()
    {
        if (IsLoadComplete() && !IsEnable)
        {
            Reset();
        }
        else if (mProcess < 0)
        {
            GlobalCoroutine.Instance.StartCoroutine(StartLoad());
        }


        return this;
    }

    public BattleRuntime Load(GateBattle battle)
    {
        
        if (IsLoadComplete() && !IsEnable)
        {
            Reset();
        }
        else if (mProcess < 0)
        {
            GlobalCoroutine.Instance.StartCoroutine(StartLoad(battle));
        }
        return this;
    }
    

    private IEnumerator StartLoad()
    {
        mProcess = 0;
        while (mProcess < ProcessState.Complete)
        {
            InitBattle();
            yield return null;
        }
        
    }
    private IEnumerator StartLoad(GateBattle battle)
    {
        mProcess = 0;
        while (mProcess < ProcessState.Complete)
        {
            
            yield return null;
        }
        
    }

    private int InitBattle()
    {
        switch (mProcess)
        {
            case ProcessState.Start:
                break;
            case ProcessState.Template:
                if (!LoadTemplate())
                    return (int)mProcess;
                break;
            case ProcessState.ResEnd:
                CreateBattle();
                break;
        }

        mProcess++;
        if (mProcess >= ProcessState.Complete)
        {
            mProcess = ProcessState.Complete;
            LoadComplete();
        }
        
        return (int)mProcess;
    }

    private bool LoadTemplate()
    {
        var template = IOGameClientManager.Battle.DataRoot;
        if (template != null)
        {
            SceneData = template.LoadScene(StageID, true, !IsLocal);
        }
        return template != null;
    }

    private void CreateBattle()
    {
        var name = Resource.GetFileNameWithoutExtension(SceneData.FileName);
        gameObject = new GameObject(name);
        transform = gameObject.transform;
        UnityBattle = new UnityBattle();
        
        UnityBattle.Init(gameObject, new BattleLocalPlay(GateClientManager.Battle.DataRoot, SceneData));
        
    }

    private void LoadComplete()
    {
        mLoadComplete = true;
        Reset();
    }

    private void Reset()
    {
        if (gameObject != null && !IsEnable)
        {
            IsEnable = true;
            gameObject.SetActive(true);
        }
    }

    private bool mLoadComplete;
    private bool IsLoadComplete()
    {
        return mLoadComplete;
    }



    public enum ProcessState
    {
        None = -1,
        Start,
        Template,
        ResEnd,
        Complete,
    }
    
}
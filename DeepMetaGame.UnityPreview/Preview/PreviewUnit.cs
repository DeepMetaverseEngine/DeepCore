using DeepCore;
using DeepCore.Geometry;
using DeepCore.Unity;
using DeepCore.Unity.Expose;
using DeepCore.Unity.OnGUI;
using DeepCore.Unity3D;
using DeepCore.Unity3D.Voxel;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Unity.BattleView;
using System.Security.Cryptography;
using UnityEngine;
using static DeepCore.EventTrigger.Data.AI.LLMAgentValue;

namespace DeepMetaGame.Unity.Preview.Preview
{

    //-----------------------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------------------


    public abstract class PreviewUnit : PreviewObject, IZoneUnit
    {
        public abstract UnitInfo Template { get; }
        public abstract UnitSkillAbility ASkill { get; }
        public float BodyScale { get => 1f; }
        public bool IsActive { get => true; }
        public bool IsControllable { get => true; }
        public float LayerUpward => 0;
        public UnitActionStatus CurrentActionStatus => UnitActionStatus.Idle;
        public string CurrentActionSubstate => string.Empty;
    }

    public abstract class PreviewUnit<T> : PreviewUnit
    {
        public T Data { get; private set; }
        public void Init(T data)
        {
            Data = data;
            base.Init(data);
        }
        sealed protected override void DoInit(object data)
        {
            Data = (T)data;
            DoInit((T)data);
        }
        protected virtual void DoInit(T data) { }
    }
    public interface IPreviewUnit
    {
        PreviewObject Preview { get; }
        UnitInfo UnitData { get; }
    }
}

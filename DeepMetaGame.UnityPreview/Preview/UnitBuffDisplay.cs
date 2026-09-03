using DeepCore.Unity;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Template;

namespace DeepMetaGame.Unity.Preview.Preview
{
    //---------------------------------------------------------------------------------------------------------------------------------

    public class UnitBuffDisplay : PreviewUnit<ValueTuple<UnitInfo, BuffTemplate>>, IPreviewUnit
    {
        public PreviewObject Preview => this;
        public const int MAX_IDLE_TIME_MS = 3000;
        public UnitInfo UnitData { get => Data.Item1; }
        public BuffTemplate BuffData { get => Data.Item2; }
        public override UnitInfo Template => UnitData;
        public override UnitSkillAbility ASkill => askill;

        private IViewResource unit_res;
        private UnitSkillAbility askill;
        // private PopupKeyFrames<BuffTemplate.KeyFrame> current_frames = new PopupKeyFrames<BuffTemplate.KeyFrame>();

        protected override void Awake()
        {
            base.Awake();
            RTG.AddEditorObject(gameObject);
        }
        protected override void DoInit(ValueTuple<UnitInfo, BuffTemplate> tuple)
        {
            var unit = UnitData;
            this.askill = unit.Abilities.GetComponentAs<UnitSkillAbility>();
            this.BodyHeight = unit.BodyHeight;
            if (unit.Abilities.TryGetComponentAs<UnitResourceAbility>(out var u_res))
            {
                unit_res = LoadRes(u_res.FileName, DeepMetaGame.Data.ResourceType.Object);
                if (unit_res != null)
                {
                    //unit_res.go.SetParticleEmission(true);
                    //Proxy.PlayEffect(unit_res, BuffData.LifeTimeMS);
                    //RTG.LookAt(unit_res.transform);
                    //RTG.TargetObject = this.gameObject;
                }
            }
            var buff = BuffData;
            AddBuffNode(new DeepMetaGame.Data.Misc.LaunchBuff()
            {
                BuffID = buff.ID,
                BuffLevel = 0,
                LaunchPercent = 100f,
            }, this, this);
            //             if (buff.Abilities.TryGetComponentAs<BuffEffectAbility>(out var effects))
            //             {
            //                 LoadEffect(effects.BindingEffect);
            //                 if (effects.BindingEffectList != null)
            //                 {
            //                     foreach (var e in effects.BindingEffectList)
            //                     {
            //                         LoadEffect(e);
            //                     }
            //                 }
            //             }
        }
        protected override void DoReplay()
        {
            //             current_frames.Clear();
            //             current_frames.AddRange(BuffData.KeyFrames);
        }
        protected override void DoUpdate()
        {
            try
            {
                //                 using (var kfs = ObjectPool.AllocList<BuffTemplate.KeyFrame>())
                //                 {
                //                     int kfs_count = current_frames.PopKeyFrames(PassTimeMS, kfs);
                //                     foreach (var kf in kfs)
                //                     {
                //                         if (kf.Effect != null)
                //                         {
                //                             ShowEffect(kf.Effect);
                //                         }
                //                     }
                //                 }
            }
            finally
            {
            }
        }
        protected override void DoDestory()
        {
            //  current_frames.Clear();
        }
    }


    //---------------------------------------------------------------------------------------------------------------------------------

}

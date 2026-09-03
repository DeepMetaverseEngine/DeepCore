using DeepCore;
using DeepCore.EventTrigger.Data;
using DeepCore.Geometry;
using DeepCore.XCSV;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Text;
using static DeepCore.GUI.Cell.SpriteSet;

namespace DeepMetaGame.Data.ZoneGeometry
{

    public interface IZoneSpell : IZoneObject
    {
        bool IsFinish { get; }
        bool IsHitted { get; }
        SpellTemplate Template { get; }
        LaunchSpell LaunchData { get; }
        IZoneObject Sender { get; }
        IZoneUnit LauncherUnit { get; }
        bool IsNextChain { get; }
        double PassTimeMS { get; }
        bool IsForceSync { get; }
        Vector3 PrevPos { get; }
        Vector3 RemotePosition { get; }
        bool IsFromSpellMagnitude { get; }
        //---------------------------------------------------------------------
        Vector3? TargetPos { get; set; }
        IZoneUnit TargetUnit { get; set; }
        Vector3? StartNormal { get; set; }
        float StartSpeed { get; set; }
        float SpellDistance { get; set; }
        float SpellSize { get; set; }
        float SpellDisplayDistance { get; set; }
        float SpellDisplaySize { get; set; }
        Vector3? RayTouchPoint { get; set; }
        ISpellMotion Motion { get; }
        void FaceTo(float dir);
        void FaceTo(Vector3 dir);
        void Turn(float dir);
        void SetPosition(Vector3 position);

        //---------------------------------------------------------------------

        bool TrySeekAttackable(float range, bool postEvent, out IZoneUnit target);
        bool TryRayCastTouchEndUnit(VoxelStripe ray, out IZoneUnit target);

        /// <summary>
        /// 检查Binding效果是否有效，否则终止法术
        /// </summary>
        /// <param name="target"></param>
        /// <returns>False=终止法术</returns>
        bool CheckBinding(IZoneObject target);
        bool CheckRemoveOnBindingSkillOver(IZoneUnit target);

        void Finish(bool destoryImmediately = false);

        //---------------------------------------------------------------------
    }

    public abstract class ISpellMotion : Recyclable
    {
        public IZoneSpell Spell { get; private set; }
        public IZone Zone => Spell.Zone;
        public SpellTemplate Info => Spell.Template;
        public abstract float CurrentSpeed { get; set; }
        public virtual ISpellMotion Init(IZoneSpell spell)
        {
            this.Spell = spell;
            return this;
        }
        protected override void Disposing()
        {
            this.Spell = default;
        }
        protected override void Destructing()
        {
        }
        public abstract void OnAdded();
        public abstract void UpdateMotion(float intervalMS);
        public abstract Vector3 GetBindingPos(IZoneObject target);

    }

}
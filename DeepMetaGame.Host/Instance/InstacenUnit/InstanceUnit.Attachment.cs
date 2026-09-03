using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    public partial class InstanceUnit
    {
        //---------------------------------------------------------------------------------------------------------
        protected virtual void InitAttachments(UnitAttachmentAbility attachment)
        {
            if (attachment.UnitDockings != null)
            {
                foreach (var unitAttachment in attachment.UnitDockings)
                {
                    var add = Zone.AttachUnit(this, unitAttachment);

                }
            }
        }
        private HashMap<uint, InstanceUnit> attachments = new HashMap<uint, InstanceUnit>();
        //---------------------------------------------------------------------------------------------------------
        public int AttachmentsCount { get { return attachments.Count; } }

        //---------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 添加挂载物
        /// </summary>
        /// <param name="attachment"></param>
        /// <param name="dockingOffset"></param>
        public bool AddAttachment(InstanceUnit attachment, DockingOffset dockingOffset)
        {
            if (attachment.CurrentDockingParent != null)
            {
                attachment.DetachFromParent();
            }
            attachments.Put(attachment.ObjectID, attachment);
            return attachment.SetDockingParent(this, dockingOffset);
        }
        /// <summary>
        /// 移除挂载物
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        public InstanceUnit RemoveAttachment(uint oid)
        {
            if (attachments.RemoveByKey(oid) is InstanceUnit att)
            {
                att.ClearDockingParent();
                return att;
            }
            return null;
        }
        //---------------------------------------------------------------------------------------------------------
        public bool DockToParent(InstanceUnit parent, DockingOffset dockingOffset)
        {
            if (parent != null && !parent.IsDisposing)
            {
                return parent.AddAttachment(this, dockingOffset);
            }
            return false;
        }
        public InstanceUnit DetachFromParent()
        {
            if (CurrentDockingParent is InstanceUnit parent)
            {
                parent.RemoveAttachment(this.ObjectID);
                return parent;
            }
            return null;
        }
        //---------------------------------------------------------------------------------------------------------
        public void ClearAttachments()
        {
            using (var attrs = ObjectPool.AllocList<InstanceUnit>(attachments.Values))
            {
                foreach (var att in attrs)
                {
                    att.ClearDockingParent();
                }
            }
            attachments.Clear();
        }
        public bool TryGetAttachment(uint oid, out InstanceUnit att)
        {
            return attachments.TryGetValue(oid, out att);
        }
        public bool ForEachAttachments<ST>(in ST st, ForEachPredicate<ST, InstanceUnit> action)
        {
            using (var attrs = ObjectPool.AllocList<InstanceUnit>(attachments.Values))
            {
                foreach (var att in attrs)
                {
                    if (action(st, att))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public InstanceUnit FindAttachment<ST>(in ST st, TryGetPredicate<ST, InstanceUnit> find)
        {
            using (var attrs = ObjectPool.AllocList<InstanceUnit>(attachments.Values))
            {
                foreach (var att in attrs)
                {
                    if (find(st, att))
                    {
                        return att;
                    }
                }
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------
        private HashMap<uint, InstanceUnit> summons = new HashMap<uint, InstanceUnit>();
        public int SummonsCount { get { return summons.Count; } }
        public void AddSummoned(InstanceUnit summoned)
        {
            summons.Put(summoned.ObjectID, summoned);
            summoned.OnRemoved += (a) =>
            {
                this.RemoveSummoned(a.ObjectID);
            };
        }
        protected virtual InstanceUnit RemoveSummoned(uint oid)
        {
            if (summons.RemoveByKey(oid) is InstanceUnit att)
            {
                return att;
            }
            return null;
        }
        protected virtual void ClearSummons()
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>(summons.Values))
            {
                foreach (var att in list)
                {
                    att.ClearDockingParent();
                }
            }
            summons.Clear();
        }
        public bool TryGetSummoned(uint oid, out InstanceUnit att)
        {
            return summons.TryGetValue(oid, out att);
        }
        public bool ForEachSummons<ST>(in ST st, ForEachPredicate<ST, InstanceUnit> action)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>(summons.Values))
            {
                foreach (var att in list)
                {
                    if (action(st, att))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void ForEachSummons<ST>(in ST st, ForEachAction<ST, InstanceUnit> action)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>(summons.Values))
            {
                foreach (var att in list)
                {
                    action(st, att);
                }
            }
        }
        public InstanceUnit FindSummoned<ST>(in ST st, TryGetPredicate<ST, InstanceUnit> find)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>(summons.Values))
            {
                foreach (var att in list)
                {
                    if (find(st, att))
                    {
                        return att;
                    }
                }
            }
            return null;
        }

        //---------------------------------------------------------------------------------------------------------
        #region DockingParent

        [Desc("挂载到的父亲")]
        public InstanceZoneObject CurrentDockingParent => _Docking?.DockingParent;
        [Desc("挂载到的父亲ID")]
        public uint DockingParentID => _Docking == null ? 0 : _Docking.DockingParentID;
        [Desc("挂载姿势")]
        public DockingOffset DockingOffset => _Docking?.DockingOffset;
        [Desc("挂载是否焊死角度")]
        public bool IsDockingSolidFace => _Docking != null && _Docking.IsDockingSolidFace;
        //---------------------------------------------------------------------------------------------------------
        private DockingContext _Docking;
        protected virtual DockingContext CreateDockingContext(InstanceUnit owner, InstanceZoneObject parent, DockingOffset offset)
        {
            return HostFactory.CreateDockingContext(owner, parent, offset);
        }
        public class DockingContext : Recyclable
        {
            private InstanceUnit _DockingOwner;
            private uint _DockingParentID;
            private InstanceZoneObject _DockingParent;
            private DockingOffset _DockingOffset;
            private int? _tailsCount;

            protected readonly LinkedList<Vector3> tails = new LinkedList<Vector3>();
            protected Vector3? lastPos;
            protected TimeInterval interval;

            public uint DockingParentID => _DockingParentID;
            public InstanceZoneObject DockingParent => _DockingParent;
            public DockingOffset DockingOffset => _DockingOffset;
            public Vector3 ParentNextPos => tails.Count > 0 ? tails.First.Value : _DockingParent.Position;
            public int? TailsCount => _tailsCount;
            public bool IsDockingSolidFace
            {
                get
                {
                    if (_DockingOffset != null && _DockingOffset.SolidFaceAngle.HasValue)
                    {
                        return true;
                    }
                    return false;
                }
            }
            public static InstanceUnit.DockingContext Alloc(InstanceUnit owner, InstanceZoneObject parent, DockingOffset offset)
            {
                return owner.ObjectPool.Alloc<InstanceUnit.DockingContext>().Init(owner, parent, offset);
            }
            public virtual DockingContext Init(InstanceUnit owner, InstanceZoneObject parent, DockingOffset offset)
            {
                this._DockingOwner = owner;
                this._DockingParentID = parent == null ? 0 : parent.ObjectID;
                this._DockingParent = parent;
                this._DockingOffset = offset;
                this._tailsCount = offset.TailsCount;
                this.interval = new TimeInterval(parent.Zone.CFG.SYSTEM_FPS / 1000f);
                this.lastPos = null;
                _DockingOwner.OnTransport += Owner_OnTransport;
                return this;
            }

            protected override void Disposing()
            {
                _DockingOwner.OnTransport -= Owner_OnTransport;
                this._DockingOwner.DetachFromParent();
                this._DockingOwner = null;
                this._DockingParentID = 0;
                this._DockingParent = null;
                this._DockingOffset = null;
                this._tailsCount = null;
                this.tails.Clear();
                this.lastPos = null;
                this.interval?.Dispose();
                this.interval = null;
            }
            protected virtual void Owner_OnTransport(InstanceUnit sender, Vector3 oldpos)
            {
                tails.Clear();
            }

            protected void InternalSetPos(InstanceUnit owner,Geometry.Vector3 pos)
            {
                owner.InternalSetPos(pos);
            }
            protected void InternalFaceTo(InstanceUnit owner, float d)
            {
                owner.InternalFaceTo(d);
            }
            internal bool UpdateDocking(InstanceUnit owner)
            {
                if (_DockingParent != null && _DockingOffset != null)
                {
                    this.OnUpdateDockingWithParent(owner, _DockingParent, _DockingOffset);
                    return true;
                }
                return false;
            }
            protected virtual void OnUpdateDockingWithParent(InstanceUnit owner, InstanceZoneObject parent, DockingOffset offset)
            {
                var pos = parent.Position;
                // 先计算挂载位置
                {
                    if (offset.Radius != 0)
                    {
                        if (offset.BindBodyRotation)
                        {
                            Geometry.VectorHelper.MovePolar(ref pos, parent.BodyDirection + offset.Angle, offset.Radius);
                        }
                        else
                        {
                            Geometry.VectorHelper.MovePolar(ref pos, parent.Direction + offset.Angle, offset.Radius);
                        }
                    }
                    pos.Z += offset.Z;
                }
                // 如果有尾巴
                if (_tailsCount.HasValue)
                {
                    if (interval.Update(owner.Parent.UpdateIntervalMS))
                    {
                        if (lastPos != null && lastPos.Value != pos)
                        {
                            if (tails.Count < _tailsCount.Value)
                            {
                                tails.AddLast(pos);
                            }
                            else
                            {
                                var head = tails.First;
                                tails.Remove(head);
                                head.Value = pos;
                                tails.AddLast(head);
                            }
                        }
                        lastPos = pos;
                    }
                    owner.InternalSetPos(ParentNextPos);
                }
                else
                {
                    owner.InternalSetPos(pos);
                }
                if (offset.SolidFaceAngle.HasValue)
                {
                    owner.InternalFaceTo(DockingParent.Direction + offset.SolidFaceAngle.Value);
                }
            }
        }
        //---------------------------------------------------------------------------------------------------------
        internal bool SetDockingParent(InstanceZoneObject parent, DockingOffset offset)
        {
            if (parent == null)
            {
                ClearDockingParent();
                return true;
            }
            else if (offset != null)
            {
                _Docking?.Dispose();
                _Docking = this.CreateDockingContext(this, parent, offset);
                syncFields(UnitFieldMask.MASK_DOCKING_OBJ, _Docking.DockingParentID);
                syncFields(UnitFieldMask.MASK_DOCKING_POS, _Docking.DockingOffset);
                UpdateDocking();
                return true;
            }
            else
            {
                return false;
            }
        }
        internal void ClearDockingParent()
        {
            _Docking?.Dispose();
            _Docking = null;
            syncFields(UnitFieldMask.MASK_DOCKING_OBJ, 0);
            syncFields(UnitFieldMask.MASK_DOCKING_POS, null);
        }
        internal bool UpdateDocking()
        {
            if (_Docking != null)
            {
                return _Docking.UpdateDocking(this);
            }
            return false;
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------


    }
}

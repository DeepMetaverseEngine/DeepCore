using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.GameData.Data;
using DeepCore.Geometry;
using DeepMetaGame.Data.GUI;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using static DeepCore.Colors;

namespace DeepCore.GameData.EventTrigger
{
    public class ZoneEventTriggerAdapterAPI : Recyclable, IEventAPI
    {
        //-------------------------------------------------------------------------------------
        public EditorScene Zone { get; private set; }
        public InstanceUnit Unit { get; private set; }
        public InstanceZone.HostGUIForm Form { get; private set; }
        public ZoneEventTriggerAdapterAPI Init(EditorScene zone)
        {
            this.Zone = zone;
            this.Unit = null;
            this.Form = null;
            return this;
        }
        public ZoneEventTriggerAdapterAPI Init(InstanceUnit unit)
        {
            this.Zone = unit.Parent as EditorScene;
            this.Unit = unit;
            return this;
        }
        public ZoneEventTriggerAdapterAPI Init(InstanceZone.HostGUIForm form)
        {
            this.Zone = form.Zone as EditorScene;
            this.Unit = form.BindingPlayer;
            this.Form = form;
            return this;
        }
        protected override void Disposing()
        {
            this.Zone = null;
            this.Unit = null;
            this.Form = null;
        }
        //-------------------------------------------------------------------------------------
        public AbstractCollectionPool ObjectPool => Zone.ObjectPool;
        public TemplateManager Templates { get => Zone.Templates; }
        public Random RandomN => Zone.RandomN;
        public TimeSpan PassTime => Zone.PassTime;
        public TimeSpanAlarm PassTimeAlarm => Zone.PassTimeAlarm;
        public DateTime DateTime => Zone.DateTime;
        public DateTimeAlarm DateTimeAlarm => Zone.DateTimeAlarm;

        public void Run(System.Action task)
        {
            Zone.Run(task);
        }
        public TimeTaskMS AddTimeTask(int intervalMS, int delayMS, int repeat, TickHandler handler)
        {
            return Zone.AddTimeTask(intervalMS, delayMS, repeat, handler);
        }
        public TimeTaskMS AddTimeDelayMS(int delayMS, TickHandler handler)
        {
            return Zone.AddTimeDelayMS(delayMS, handler);
        }
        public TimeTaskMS AddTimePeriodicMS(int intervalMS, TickHandler handler)
        {
            return Zone.AddTimePeriodicMS(intervalMS, handler);
        }
        public void SetEnvironmentVar(string key, object value, bool sync)
        {
            Zone.SetEnvironmentVar(key, value, sync);
        }
        public T GetEnvironmentVarAs<T>(string key)
        {
            return Zone.GetEnvironmentVarAs<T>(key);
        }
        public bool TryGetEnvironmentVar(string key, out object value)
        {
            return Zone.TryGetEnvironmentVar(key, out value);
        }
        public bool TryGetEnvironmentVarAs<T>(string key, out T value)
        {
            return Zone.TryGetEnvironmentVarAs(key, out value);
        }
        public IEventArguments AllocEventArguments(EventExecutor exe, AbstractTrigger trigger, EventBehaviorExecutor behavior)
        {
            return new EventArguments(exe as IEventTriggerAdapter, trigger, behavior);
        }
        public IEventArguments AllocEventArguments(EventExecutor exe, IEventArguments args)
        {
            return ((EventArguments)args).Clone();
        }
    }

    public struct EventArguments : IDisposable, IEventArguments
    {
        private IEventTriggerAdapter exe;
        private AbstractTrigger Listener;
        private EventBehaviorExecutor Behavior;
        public EventArguments(IEventTriggerAdapter api, AbstractTrigger listener, EventBehaviorExecutor behavior)
        {
            this.exe = api;
            this.Listener = listener;
            this.Behavior = behavior;
        }
        public void Dispose()
        {
        }
        public EventArguments Clone()
        {
            EventArguments ret = this;
            if (this.attributes != null)
            {
                ret.attributes = new HashMap<object, object>(this.attributes);
            }
            return ret;
        }
        //----------------------------------------------------------------
        public IEventTriggerAdapter API => exe;
        EventExecutor IEventArguments.API => API;
        EventBehaviorExecutor IEventArguments.Behavior => Behavior;
        AbstractTrigger IEventArguments.Listener { get => this.Listener; set => this.Listener = value; }
        //----------------------------------------------------------------
        public object ReturnValue { get; set; }
        //----------------------------------------------------------------
        public bool TriggingBoolValue { get; set; }
        public double TriggingNumberValue { get; set; }
        public string TriggingStringValue { get; set; }
        public Vector3? TriggingPositionValue { get; set; }
        //----------------------------------------------------------------
        public InstanceUnit TriggingUnit { get; set; }
        public InstanceUnit TriggingCounterPart { get; set; }
        public InstanceUnit.EquipSkill TriggingEquipSkill { get; set; }
        public InstanceUnit.EquipAura TriggingEquipAura { get; set; }
        public InstanceUnit.EquipBuff TriggingEquipBuff { get; set; }

        public InstanceFlag TriggingFlag { get; set; }
        public InstanceItem TriggingItem { get; set; }
        public InstanceSpell TriggingSpell { get; set; }

        // public SpellChainContext TriggingChainInfo { get; set; }


        public ItemTemplate TriggingItemTemplate { get; set; }
        public BuffTemplate TriggingBuffTemplate { get; set; }
        public CardTemplate TriggingCardTemplate { get; set; }
        public InstanceUnit TriggingBuffSender { get; set; }
        public TAttackSource TriggingAttack { get; set; }
        public TAttackResult? TriggingDamage { get; set; }


        public SkillTemplate TriggingSkillTemplate { get; set; }
        public SpellTemplate TriggingSpellTemplate { get; set; }
        public AuraTemplate TriggingAuraTemplate { get; set; }
        public InstanceUnit TriggingAuraOwner { get; set; }
        public string TriggingQuestID { get; set; }
        public string TriggingQuestStatusValue { get; set; }
        public string TriggingQuestStatusKey { get; set; }
        public int TriggingZoneInfoFlag { get; set; }
        //----------------------------------------------------------------
        public ZoneRegion TriggingRegion { get { return TriggingFlag as ZoneRegion; } }
        public ZoneWayPoint TriggingPoint { get { return TriggingFlag as ZoneWayPoint; } }
        public ZoneDecoration TriggingDecoration { get { return TriggingFlag as ZoneDecoration; } }
        public ZoneArea TriggingArea { get { return TriggingFlag as ZoneArea; } }
        //----------------------------------------------------------------
        public int IteratingInt32 { get; set; }
        public object IteratingObject { get; set; }
        //----------------------------------------------------------------
        public ObjectAoiStatus TriggerAoiStatus { get; set; }
        //----------------------------------------------------------------
        public InstanceZone.HostGUIForm TriggingForm { get; set; }
        public InstanceZone.HostGUIComponent TriggingComponent { get; set; }
        public string DialogResult { get; set; }
        public string TriggingComponentName { get; set; }
        public string TriggingComponentSubURL { get; set; }
        //----------------------------------------------------------------
        public object Tag { get; set; }

        //----------------------------------------------------------------

        //         public void PutAttribute(object key, object value)
        //         {
        //             if (attributes == null)
        //             {
        //                 attributes = new HashMap<object, object>();
        //             }
        //             attributes.Put(key, value);
        //         }
        //         public object GetAttribute(object key)
        //         {
        //             return attributes?.Get(key);
        //         }
        //         public bool TryGetAttribute(object key, out object value)
        //         {
        //             value = null;
        //             if (attributes == null) return false;
        //             return attributes.TryGetValue(key, out value);
        //         }
        // 
        //         public T GetAttributeAs<T>(object key)
        //         {
        //             if (TryGetAttribute(key, out var value))
        //             {
        //                 return (T)value;
        //             }
        //             return default(T);
        //         }
        //         public bool TryGetAttributeAs<T>(object key, out T ret)
        //         {
        //             if (TryGetAttribute(key, out var value))
        //             {
        //                 ret = (T)value;
        //                 return true;
        //             }
        //             ret = default(T);
        //             return false;
        //         }
        // 
        //         public void PutType<T>(T value)
        //         {
        //             this.PutAttribute(typeof(T), value);
        //         }
        //         public T GetType<T>()
        //         {
        //             return GetAttributeAs<T>(typeof(T));
        //         }
        //         public bool TryGetType<T>(out T value)
        //         {
        //             return TryGetAttributeAs<T>(typeof(T), out value);
        //         }
        //----------------------------------------------------------------

        //----------------------------------------------------------------
        private HashMap<object, object> attributes;
        private object arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, argA;
        public void PutArg(int id, object arg)
        {
            switch (id)
            {
                case 0x0: arg0 = arg; break;
                case 0x1: arg1 = arg; break;
                case 0x2: arg2 = arg; break;
                case 0x3: arg3 = arg; break;
                case 0x4: arg4 = arg; break;
                case 0x5: arg5 = arg; break;
                case 0x6: arg6 = arg; break;
                case 0x7: arg7 = arg; break;
                case 0x8: arg8 = arg; break;
                case 0x9: arg9 = arg; break;
                case 0xA: argA = arg; break;
                default:
                    if (attributes == null)
                    {
                        attributes = new HashMap<object, object>();
                    }
                    attributes.Put($"ARG:[{id}]", arg);
                    break;
            }
        }
        public object GetArg(int id)
        {
            switch (id)
            {
                case 0x0: return arg0;
                case 0x1: return arg1;
                case 0x2: return arg2;
                case 0x3: return arg3;
                case 0x4: return arg4;
                case 0x5: return arg5;
                case 0x6: return arg6;
                case 0x7: return arg7;
                case 0x8: return arg8;
                case 0x9: return arg9;
                case 0xA: return argA;
                default:
                    if (attributes != null && attributes.TryGetValue($"ARG:[{id}]", out var value))
                    {
                        return value;
                    }
                    return null;
            }
        }
        public T GetArgAs<T>(int id)
        {
            try
            {
                if (id >= 0 && id <= 0xA)
                {
                    return (T)GetArg(id);
                }
                if (attributes != null && attributes.TryGetValue($"ARG:[{id}]", out var value))
                {
                    return (T)value;
                }
            }
            catch { }
            return default(T);
        }

        //----------------------------------------------------------------
    }
}

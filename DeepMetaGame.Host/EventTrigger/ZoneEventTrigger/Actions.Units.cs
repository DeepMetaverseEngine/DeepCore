using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("单位绑定事件", "[游戏]/单位/单位触发器")]
    public class UnitBindEventAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("事件触发器名字")]
        [TemplateID(typeof(UnitEventTemplate))]
        public int EventID;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({0})绑定事件({1});", Unit, EventID);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                unit.BindUnitEvent(EventID);
            }

            return null;
        }
    }

    //--------------------------------------------------------------------------------------------
    [Desc("激活单位事件触发器", "[游戏]/单位/单位触发器")]
    public class UnitEventTriggerActive : ZoneAbstractAction
    {
        [Desc("事件触发器名字")]
        [UnitEventIDAttribute]
        public string EventName;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("激活单位事件触发器({0});", EventName);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.Group.EventActive(EventName);
            return null;
        }
    }

    [Desc("关闭单位事件触发器", "[游戏]/单位/单位触发器")]
    public class UnitEventTriggerDeactive : ZoneAbstractAction
    {
        [Desc("事件触发器名字")]
        [UnitEventIDAttribute]
        public string EventName;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("关闭单位事件触发器({0});", EventName);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.Group.EventDeactive(EventName);
            return null;
        }
    }

    //--------------------------------------------------------------------------------------------
    [Desc("激活单位事件触发器组", "[游戏]/单位/单位触发器")]
    public class UnitEventGroupTriggerActive : ZoneAbstractAction
    {
        [Desc("事件触发器组")][UnitEventGroup] public string EventGroup;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("激活单位事件触发器组({0});", EventGroup);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.Group.ForEachEvents(e =>
            {
                if (e.EditorPath.StartsWith(EventGroup))
                {
                    e.IsActive = true;
                }
            });
            return null;
        }
    }

    [Desc("关闭单位事件触发器组", "[游戏]/单位/单位触发器")]
    public class UnitEventGroupTriggerDeactive : ZoneAbstractAction
    {
        [Desc("事件触发器组")][UnitEventGroup] public string EventGroup;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("关闭单位事件触发器组({0});", EventGroup);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.Group.ForEachEvents(e =>
            {
                if (e.EditorPath.StartsWith(EventGroup))
                {
                    e.IsActive = false;
                }
            });
            return null;
        }
    }
    //--------------------------------------------------------------------------------------------

    [Desc("复制空气墙碰撞", "[游戏]/单位/单位碰撞")]
    public class CopyDecorationShape : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [SceneObjectIDAttribute(typeof(DecorationData))]
        [Desc("复制空气墙碰撞", "碰撞")]
        public string DecorationName;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}复制空气墙{1}碰撞;", Unit, DecorationName);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var flag = api.ZoneAPI.GetFlag<ZoneDecoration>(DecorationName);
            var unit = Unit.GetValueAs(api, args);
            if (unit != null && flag != null)
            {
                unit.FaceTo(flag.Direction);
                unit.Transport(flag.Position);
                unit.ZoneShape = flag.ZoneShape;
            }

            return null;
        }
    }

    //-------------------------------------------------------------------------------------------

    #region __单位动作__

    [Desc("添加单位", "[游戏]/单位")]
    public class AddUnitAction : ZoneAbstractAction<InstanceUnit>
    {
        [Desc("单位模板ID")]
        [TemplateID(typeof(UnitInfo))]
        public int UnitTemplateID = 0;

        [Desc("单位模板ID组（随机一个）")]
        [TemplateGroup(typeof(UnitInfo))]
        public string UnitGroupPath;

        [Desc("单位等级")]
        [TemplateLevelAttribute]
        public int UnitLevel = 0;

        [Desc("召唤者")] public AbstractValue<InstanceUnit> Owner = new UnitValue.Trigging();
        [Desc("单位阵营")] public AbstractValue<double> Force = new IntegerValue.VALUE(0);
        [Desc("用户定义名字(编辑器名字)")] public string UnitName;
        [Desc("显示名字")] public AbstractValue<string> DisplayName = new ZoneStringValue.VALUE();
        [Desc("别名")] public AbstractValue<string> Alias = new ZoneStringValue.VALUE();
        [Desc("位置")] public AbstractValue<Vector3?> Position = new PositionValue.VALUE();
        [Desc("朝向")] public float Direction;
        [Desc("开始寻路")] public AbstractValue<InstanceFlag> StartPoint;

        //protected InstanceUnit AddedUnit;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("添加单位({0})到({1});", UnitTemplateID, Position);
        }

        override protected InstanceUnit Run(IEventTriggerAdapter api, EventArguments args)
        {
            var pos = Position.GetValueAs(api, args);
            if (pos != null)
            {
                var temp = api.ZoneAPI.Templates.GetUnit(UnitTemplateID);
                if (!string.IsNullOrEmpty(UnitGroupPath))
                {
                    using (var array = api.ZoneAPI.ObjectPool.AllocList<UnitInfo>())
                    {
                        api.ZoneAPI.Templates.GetAllUnitsByPath(UnitGroupPath, array);
                        temp = api.ZoneAPI.RandomN.GetRandomInCollection(array);
                    }
                }

                if (temp != null)
                {
                    // InstanceUnit unit = api.ZoneAPI.AddUnit(UnitTemplateID, UnitName, (byte)Force.GetValueAs(api, args), UnitLevel, pos.x, pos.y, Direction);
                    var unit = api.ZoneAPI.AddUnit(new Data.AddUnitParam()
                    {
                        template = temp,
                        name = UnitName,
                        //player_uuid = UnitName,
                        summoner = Owner.GetValueAs(api, args),
                        displayName = DisplayName.GetValueAs(api, args),
                        alias = Alias.GetValueAs(api, args),
                        force = (byte)Force.GetValueAs(api, args),
                        level = UnitLevel,
                        pos = pos.Value,
                        direction = Direction,
                    });
                    if (StartPoint != null)
                    {
                        InstanceFlag flag = StartPoint.GetValueAs(api, args);
                        if (flag is ZoneWayPoint wp)
                        {
                            unit.StartAttackTo(wp);
                        }
                    }

                    return unit;
                }
            }

            return null;
        }
    }


    [Desc("单位增加金钱", "[游戏]/单位")]
    public class UnitAddMoneyAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("金钱")] public AbstractValue<double> Money = new IntegerValue.VALUE(100);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("给({0})增加金钱({1});", Unit, Money);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            int money = (int)Money.GetValueAs(api, args);
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null && money != 0)
            {
                unit.AddMoney(money);
            }

            return unit;
        }
    }

    [Desc("杀死单位", "[游戏]/单位")]
    public class KillUnitAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("杀死({0});", Unit);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                unit.Kill();
            }

            return unit;
        }
    }

    [Desc("直接移除单位", "[游戏]/单位")]
    public class RemoveUnitAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("直接移除({0});", Unit);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                api.ZoneAPI.RemoveObject(unit);
            }

            return unit;
        }
    }


    [Desc("单位直接使用物品", "[游戏]/单位")]
    public class UnitUseItemAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("物品")] public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("给({0})使用物品({1});", Unit, Item);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate item = Item.GetValueAs(api, args);
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null && item != null)
            {
                unit.UseItem(item, unit);
            }

            return unit;
        }
    }

    [Desc("给单位背包添加道具", "[游戏]/单位-背包")]
    public class UnitAddInventoryItemAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("物品")] public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();
        [Desc("数量")] public AbstractValue<double> Count = new IntegerValue.VALUE(1);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("给({0})背包添加{2}个物品({1});", Unit, Item, Count);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate item = Item.GetValueAs(api, args);
            InstanceUnit unit = Unit.GetValueAs(api, args);
            int count = (int)Count.GetValueAs(api, args);
            if (unit != null && item != null && count > 0)
            {
                unit.Bag.AddItemToEmptyInventory(item, count);
            }

            return unit;
        }
    }

    [Desc("从单位背包丢弃道具", "[游戏]/单位-背包")]
    public class UnitDropInventoryItemAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("物品")] public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();
        [Desc("数量")] public AbstractValue<double> Count = new IntegerValue.VALUE(1);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("从({0})背包丢弃{2}个物品({1});", Unit, Item, Count);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate item = Item.GetValueAs(api, args);
            InstanceUnit unit = Unit.GetValueAs(api, args);
            int count = (int)Count.GetValueAs(api, args);
            if (unit != null && item != null && count > 0)
            {
                unit.Bag.DropInventoryItemByType(item.ID, count);
            }

            return unit;
        }
    }

    [Desc("从单位背包清除道具", "[游戏]/单位-背包")]
    public class UnitClearInventoryItemAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("物品")] public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("从({0})背包清除物品({1});", Unit, Item);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate item = Item.GetValueAs(api, args);
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null && item != null)
            {
                unit.Bag.ClearInventoryItemByType(item.ID);
            }

            return unit;
        }
    }

    [Desc("使用背包内的道具", "[游戏]/单位-背包")]
    public class UnitUseInventoryItemAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("物品")] public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();
        [Desc("数量")] public AbstractValue<double> Count = new IntegerValue.VALUE(1);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("使用({0})背包内的{2}个道具({1});", Unit, Item, Count);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate item = Item.GetValueAs(api, args);
            InstanceUnit unit = Unit.GetValueAs(api, args);
            int count = (int)Count.GetValueAs(api, args);
            if (unit != null && item != null && count > 0)
            {
                unit.Bag.UseInventoryItemByType(item.ID, count);
            }

            return unit;
        }
    }
    /*
    [Desc("给单位添加被动触发", "单位")]
    public class UnitAddTriggerAction : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("UnitTrigger模板ID")]
        [TemplateIDAttribute(typeof(UnitTriggerTemplate))]
        public int TriggerTemplateID;
        public override string ToString()
        {
            return string.Format("给({0})添加BUFF({1})", Unit, TriggerTemplateID);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args) as InstanceUnit;
            if(unit != null)
            {
                unit.addTrigger(TriggerTemplateID);
            }
        }
    }
    */

    [Desc("单位添加HP", "[游戏]/单位")]
    public class UnitAddHPAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("增加HP")] public AbstractValue<double> AddHP = new IntegerValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("给({0})添加({1})HP;", Unit, AddHP);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args) as InstanceUnit;
            if (unit != null)
            {
                int add_hp = (int)AddHP.GetValueAs(api, args);
                unit.AddHP(add_hp);
            }

            return unit;
        }
    }

    [Desc("单位给单位添加HP", "[游戏]/单位")]
    public class UnitAddHPToUnitAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("施放单位")] public AbstractValue<InstanceUnit> Sender = new UnitValue.Trigging();
        [Desc("增加HP")] public AbstractValue<double> AddHP = new IntegerValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})给({1})添加({2})HP;", Sender, Unit, AddHP);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args) as InstanceUnit;
            InstanceUnit sender = Sender.GetValueAs(api, args) as InstanceUnit;
            if (unit != null && sender != null)
            {
                int add_hp = (int)AddHP.GetValueAs(api, args);
                unit.AddHP(add_hp, sender);
            }

            return unit;
        }
    }

    [Desc("单位添加MP", "[游戏]/单位")]
    public class UnitAddMPAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("增加MP")] public AbstractValue<double> AddMP = new IntegerValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("给({0})添加({1})MP;", Unit, AddMP);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args) as InstanceUnit;
            if (unit != null)
            {
                int addmp = (int)AddMP.GetValueAs(api, args);
                unit.AddMP(addmp);
            }

            return unit;
        }
    }

    [Desc("单位添加HP百分比", "[游戏]/单位")]
    public class UnitAddHPPctAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("增加HP百分比")] public AbstractValue<double> AddHPPct = new RealValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("给({0})添加({1})%HP;", Unit, AddHPPct);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args) as InstanceUnit;
            if (unit != null)
            {
                float add_hp = (float)AddHPPct.GetValueAs(api, args);
                unit.AddHP_Pct(add_hp, unit);
            }

            return unit;
        }
    }

    [Desc("单位传送", "[游戏]/单位")]
    public class UnitTransportAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("位置")] public AbstractValue<Vector3?> Pos = new PositionValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}传送到{1};", Unit, Pos);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            var pos = Pos.GetValueAs(api, args);
            if (unit != null && pos.HasValue)
            {
                unit.Transport(pos.Value);
            }

            return unit;
        }
    }

    [Desc("单位暂停/继续逻辑", "[游戏]/单位")]
    public class UnitPauseAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        [Desc("暂停/继续")] public AbstractValue<bool> Pause = new BooleanValue.VALUE();

        [Desc("暂停时长（毫秒）")] public AbstractValue<double> PauseTimeMS = new IntegerValue.VALUE(0);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.暂停逻辑={1};", Unit, Pause);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            Boolean pause = Pause.GetValueAs(api, args);
            int timeMS = (int)PauseTimeMS.GetValueAs(api, args);
            if (unit != null)
            {
                if (pause) unit.Pause(timeMS);
                else unit.Resume();
            }

            return unit;
        }
    }

    [Desc("克隆单位", "[游戏]/单位")]
    public class CloneUnitAction : ZoneAbstractAction
    {
        [Desc("克隆原始体")] public AbstractValue<InstanceUnit> SrcUnit = new UnitValue.Trigging();

        [Desc("克隆单位类型")] public UnitType NewType = UnitType.TYPE_MANUAL;
        [Desc("克隆单位名字")] public string NewName = "Clone";
        [Desc("克隆单位阵营")] public AbstractValue<double> NewForce = new IntegerValue.VALUE(0);
        [Desc("克隆单位等级")] public int NewLevel = 0;
        [Desc("位置")] public AbstractValue<Vector3?> Position = new PositionValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("克隆单位({0})到({1});", SrcUnit, NewName);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var pos = Position.GetValueAs(api, args);
            var src = SrcUnit.GetValueAs(api, args);
            if (pos.HasValue && src != null)
            {
                var info = src.Parent.CloneData(src.TemplateData);
                info.UType = NewType;
                //InstanceUnit u = api.ZoneAPI.AddUnit(info, NewName, (byte)NewForce.GetValueAs(api, args), NewLevel, pos.x, pos.y, src.Direction);
                var u = api.ZoneAPI.AddUnit(new Data.AddUnitParam()
                {
                    template = info,
                    name = NewName,
                    force = (byte)NewForce.GetValueAs(api, args),
                    level = NewLevel,
                    pos = pos.Value,
                    direction = src.Direction
                });
                if (u != null)
                {
                    u.SetVisibleInfo(src.VisibleInfo, false);
                }
            }

            return src;
        }
    }


    [Desc("召唤宠物", "[游戏]/单位")]
    public class SummonPetAction : ZoneAbstractAction<InstanceUnit>
    {
        [Desc("召唤者")] public AbstractValue<InstanceUnit> SrcUnit = new UnitValue.Trigging();
        [Desc("召唤物")] public SummonUnit Summon = new SummonUnit();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})召唤宠物({1});", SrcUnit, Summon);
        }

        override protected InstanceUnit Run(IEventTriggerAdapter api, EventArguments args)
        {
            var src = SrcUnit.GetValueAs(api, args);
            if (src != null)
            {
                return src.Parent.UnitSummonUnit(src, Summon);
            }
            return null;
        }
    }

    [Desc("单位开始逃跑", "[游戏]/单位")]
    public class UnitEscapeAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> SrcUnit = new UnitValue.Trigging();
        [Desc("持续时间（ms）")] public AbstractValue<double> KeepTimeMS = new IntegerValue.VALUE(10000);
        [Desc("逃跑距离")] public AbstractValue<double> EscapeDistance = new RealValue.VALUE(20);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({0})开始逃跑;", SrcUnit);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var src = SrcUnit.GetValueAs(api, args);
            if (src != null)
            {
                var distance = (float)EscapeDistance.GetValueAs(api, args);
                var time = (int)KeepTimeMS.GetValueAs(api, args);
                src.StartEscape(time, distance);
            }

            return src;
        }
    }

    [Desc("单位开始乱跑", "[游戏]/单位")]
    public class UnitChaosAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> SrcUnit = new UnitValue.Trigging();
        [Desc("持续时间（ms）")] public AbstractValue<double> KeepTimeMS = new IntegerValue.VALUE(10000);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({0})开始逃跑;", SrcUnit);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var src = SrcUnit.GetValueAs(api, args);
            if (src != null)
            {
                var time = (int)KeepTimeMS.GetValueAs(api, args);
                src.StartChaos(time);
            }

            return src;
        }
    }

    [Desc("单位复活", "[游戏]/单位")]
    public class UnitRebrithAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("回复HP")] public AbstractValue<double> AddHP = new IntegerValue.VALUE(1);
        [Desc("回复MP")] public AbstractValue<double> AddMP = new IntegerValue.VALUE(1);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("复活({0});", Unit);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args) as InstanceUnit;
            if (unit != null)
            {
                var add_hp = (int)AddHP.GetValueAs(api, args);
                var add_mp = (int)AddMP.GetValueAs(api, args);
                unit.Rebirth(add_hp, add_mp);
            }

            return unit;
        }
    }

    [Desc("单位开始复活", "[游戏]/单位")]
    public class UnitStartRebrithAction : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("回复HP")] public AbstractValue<double> AddHP = new IntegerValue.VALUE(1);
        [Desc("回复MP")] public AbstractValue<double> AddMP = new IntegerValue.VALUE(1);
        [Desc("回复时间（毫秒）")] public AbstractValue<double> TimeMS;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("复活({0});", Unit);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args) as InstanceUnit;
            if (unit != null)
            {
                var add_hp = (int)AddHP.GetValueAs(api, args);
                var add_mp = (int)AddMP.GetValueAs(api, args);
                unit.StartRebirth(add_hp, add_mp, TimeMS?.GetValueAs<float>(api, args));
            }

            return unit;
        }
    }


    [Desc("建筑物搬家", "[游戏]/单位")]
    public class BuildingRebuildAction : ZoneAbstractAction
    {
        [Desc("建筑物单位")] public AbstractValue<InstanceUnit> Building = new UnitValue.Trigging();
        [Desc("新位置")] public AbstractValue<Vector3?> NewPos = new PositionValue.VALUE();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("建筑物({0})搬家到({1});", Building, NewPos);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Building.GetValueAs(api, args) as InstanceBuilding;
            if (unit != null)
            {
                var newPos = NewPos.GetValueAs(api, args);
                if (newPos.HasValue)
                {
                    unit.RebuildAt(newPos.Value);
                }
            }

            return unit;
        }
    }



    [Desc("单位缩放", "[游戏]/单位")]
    public class UnitSetScale : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("缩放")] public AbstractValue<double> Scale = new RealValue.VALUE(1);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})缩放({1});", Unit, Scale);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            float scale = (float)Scale.GetValueAs(api, args);
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null && scale != 0)
            {
                unit.BodyScale = (scale);
            }

            return unit;
        }
    }

    [Desc("单位变大", "[游戏]/单位")]
    public class UnitSetScaleAdd : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("缩放")] public AbstractValue<double> ScaleAdd = new RealValue.VALUE(0.1f);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})单位缩放增加({1});", Unit, ScaleAdd);
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            float scale = (float)ScaleAdd.GetValueAs(api, args);
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null && scale != 0)
            {
                unit.BodyScale += (scale);
            }

            return unit;
        }
    }
    [Desc("单位缩放", "[游戏]/单位")]
    public class UnitGetScale : ZoneRealValue
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})单位缩放", Unit);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                return unit.BodyScale;
            }
            return 1;
        }
    }


    [Desc("单位气泡聊天", "[游戏]/单位")]
    public class UnitBubbleTalk : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> TalkUnit = new UnitValue.Trigging();

        [Desc("内容")] public AbstractValue<string> TalkContent = new StringValue.TriggingValue();

        [Desc("持续时间(秒)")] public AbstractValue<double> TalkKeepTimeSEC;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}说:{1};", TalkUnit, TalkContent);
        }

        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = TalkUnit?.GetValueAs(api, args);
            if (unit != null)
            {
                var e = api.ZoneAPI.ObjectPool.Alloc<BubbleTalkNotify>();
                e.TalkInfos.Clear();
                var info = new BubbleTalkNotify.TalkInfo()
                {
                    TalkUnit = unit.ObjectID,
                    TalkContent = TalkContent?.GetValueAs(api, args),
                };
                if (TalkKeepTimeSEC != null)
                {
                    info.TalkKeepTimeMS = (int)(TalkKeepTimeSEC.GetValueAs(api, args) * 1000);
                }

                e.TalkInfos.Add(info);
                api.ZoneAPI.SendEvent(e);
            }

            return null;
        }
    }
    [Desc("持续单位气泡聊天", "[游戏]/单位")]
    public class UnitBubbleTalkNext : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> TalkUnit = new UnitValue.Trigging();
        [Desc("内容")] public AbstractValue<string> TalkContent = new StringValue.TriggingValue();
        [Desc("持续时间(秒)")] public AbstractValue<double> TalkKeepTimeSEC;
        [Desc("时间结束后")] public AbstractAction OnTalkOver = new DoNoting();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}说:{1};", TalkUnit, TalkContent);
            if (OnTalkOver != null)
            {
                sw.AppendLine();
                sw.AppendFormat("时间结束后执行:{0}", OnTalkOver);
            }
        }

        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = TalkUnit?.GetValueAs(api, args);
            var timems = (int)(TalkKeepTimeSEC.GetValueAs(api, args) * 1000);
            if (unit != null)
            {
                var e = api.ZoneAPI.ObjectPool.Alloc<BubbleTalkNotify>();
                e.TalkInfos.Clear();
                var info = new BubbleTalkNotify.TalkInfo()
                {
                    TalkUnit = unit.ObjectID,
                    TalkContent = TalkContent?.GetValueAs(api, args),
                };
                if (TalkKeepTimeSEC != null)
                {
                    info.TalkKeepTimeMS = timems;
                }
                e.TalkInfos.Add(info);
                api.ZoneAPI.SendEvent(e);
            }
            if (OnTalkOver != null)
            {
                api.ZoneAPI.AddTimeDelayMS(timems, (OnTalkOver, api, args), static (st, t) => { st.OnTalkOver.Invoke(st.api, st.args); });
            }

            return null;
        }
    }
    [Desc("DoSomething", "[游戏]/单位")]
    public class UnitDoSomething : ZoneAbstractAction
    {
        [Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.DoSomething", Unit);
        }

        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit?.GetValueAs(api, args);
            unit?.DoSomething();
            return null;
        }
    }
    #endregion

    //-------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------
    #region __单组位动作__

    [Desc("遍历某个阵营所有单位", "[游戏]/遍历单位组")]
    public class EveryForceUnitDoAction : ZoneAbstractAction
    {
        [Desc("阵营")] public AbstractValue<double> SelectForce = new IntegerValue.VALUE(0);
        [Desc("动作")] public AbstractAction Action = new DoNoting();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendForEach(
                sw1 => sw.AppendFormat("所有阵营{0}的单位", SelectForce),
                sw2 => sw.AppendLine(Action));
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            byte force = (byte)SelectForce.GetValueAs(api, args);
            api.ZoneAPI.ForEachForceUnits(force, (api, args, Action), static (st, u) =>
            {
                st.args.IteratingObject = (u);
                st.Action.Invoke(st.api, st.args);
                st.args.IteratingObject = (null);
                return false;
            });
            return null;
        }
        [TriggingArg("迭代中的单位")] public InstanceUnit Iterating(EventArguments args) => args.IteratingObject as InstanceUnit;
    }

    [Desc("遍历坐标半径范围内所有单位", "[游戏]/遍历单位组")]
    public class EveryRangedUnitDoAction : ZoneAbstractAction
    {
        [Desc("坐标")] public AbstractValue<Vector3?> Position = new PositionValue.VALUE();
        [Desc("半径")] public float Range = 10f;
        [Desc("高度")] public float Height = 1f;
        [Desc("动作")] public AbstractAction Action = new DoNoting();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendForEach(
                sw1 => sw.AppendFormat("坐标({0})半径({1})范围内的单位", Position, Range),
                sw2 => sw.AppendLine(Action));
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var pos = Position.GetValueAs(api, args);
            if (pos != null)
            {
                api.ZoneAPI.ForEachObjectsInCylinder<InstanceUnit>(new Geometry.VoxelCylinder(pos.Value, Range, Height), (u) =>
                {
                    args.IteratingObject = (u);
                    Action.Invoke(api, args);
                    args.IteratingObject = (null);
                    return false;
                });
                //                 using (var list = new List<InstanceUnit>())
                //                 {
                //                     //var args2 = args.Clone();
                //                     api.ZoneAPI.getObjectsRoundRange<InstanceUnit>(
                //                         Collider.Object_Pos_IncludeInRound,
                //                         pos.x, pos.y, Range,
                //                         list);
                //                     foreach (InstanceUnit u in list)
                //                     {
                //                         args.IteratingUnit = (u);
                //                         Action.DoAction(api, args);
                //                         args.IteratingUnit = (null);
                //                     }
                //                 }
            }

            return null;
        }
        [TriggingArg("迭代中的单位")] public InstanceUnit Iterating(EventArguments args) => args.IteratingObject as InstanceUnit;
    }

    [Desc("遍历区域内所有单位", "[游戏]/遍历单位组")]
    public class EveryUnitInRegionDoAction : ZoneAbstractAction
    {
        [Desc("区域")] public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

        [Desc("动作")] public AbstractAction Action = new DoNoting();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendForEach(
                sw1 => sw.AppendFormat("区域({0})内的单位", Region),
                sw2 => sw.AppendLine(Action));
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
            if (region != null)
            {
                region.ForEachObjectsInRegion<InstanceUnit>((u) =>
                {
                    args.IteratingObject = (u);
                    Action.Invoke(api, args);
                    args.IteratingObject = (null);
                    return false;
                });
            }

            return null;
        }
        [TriggingArg("迭代中的单位")] public InstanceUnit Iterating(EventArguments args) => args.IteratingObject as InstanceUnit;
    }

    [Desc("遍历区域产生的所有单位", "[游戏]/遍历单位组")]
    public class EverySpawnedUnitInRegionDoAction : ZoneAbstractAction
    {
        [Desc("区域")] public AbstractValue<InstanceFlag> Region = new FlagValue.EditorRegion();

        [Desc("动作")] public AbstractAction Action = new DoNoting();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendForEach(
                sw1 => sw.AppendFormat("区域({0})产生的单位", Region),
                sw2 => sw.AppendLine(Action));
        }

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            ZoneRegion region = Region.GetValueAs(api, args) as ZoneRegion;
            if (region != null)
            {
                region.SpawnCollection.ForEachSpawnedObjectsInRegion<InstanceUnit>((u) =>
                {
                    args.IteratingObject = (u);
                    Action.Invoke(api, args);
                    args.IteratingObject = (null);
                    return false;
                });
            }

            return null;
        }
        [TriggingArg("迭代中的单位")] public InstanceUnit Iterating(EventArguments args) => args.IteratingObject as InstanceUnit;
    }

    #endregion
}
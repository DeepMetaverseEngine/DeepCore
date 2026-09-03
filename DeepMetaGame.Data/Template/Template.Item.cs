
using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;

namespace DeepMetaGame.Data.Template
{
    //---------------------------------------------------------------------------------//
    /// <summary>
    /// 掉落道具类数据结构
    /// </summary>
    [MessageType(BattleConstants. ItemTemplate)]
    [Desc("掉落道具类数据结构")]
    public class ItemTemplate : TemplateData
    {
        [Desc(Category = "1.基础", Desc = "拾取范围，半径")]
        public float BodySize = 1f;
        [Desc(Category = "1.基础", Desc = "拾取高度")]
        public float BodyHeight = 1f;
        [Desc(Category = "1.基础", Desc = "客户端可见")]
        public bool ClientVisible = true;

        public bool NoLifeTime { get => LifeTimeMS == 0; }
        [Desc(Category = "3.掉落", Desc = "道具产生后，持续时间(毫秒)，0表示无限")]
        public int LifeTimeMS = 10000;


        [Desc(Category = "3.掉落", Desc = "获得即使用")]
        public bool GotOnUse = true;
        [Desc(Category = "3.掉落", Desc = "掉落金币最小值")]
        public int DropMoneyMin = 0;
        [Desc(Category = "3.掉落", Desc = "掉落金币最大值")]
        public int DropMoneyMax = 0;
        [Desc(Category = "3.掉落-特效", Desc = "获得后的特效(检取者)")]
        public LaunchEffect GotEffect;
        [Desc(Category = "3.掉落-特效", Desc = "掉落时的特效")]
        public LaunchEffect DropEffect;
        [Desc(Category = "3.掉落-特效", Desc = "检取中的特效（PickTimeMS>0）")]
        public LaunchEffect PickingEffect;

        [Desc(Category = "9.扩展", Desc = "能力")]
        [NotNull]
        public ArrayList<IItemTemplateAbility> Abilities = new();
        [Desc(Category = "9.扩展", Desc = "道具扩展属性")]
        [Expandable]
        [NotNull]
        public IItemProperties Properties;

        public override IPropertiesData PropertiesData => this.Properties;
        public ItemTemplate()
        {
            Abilities.Add(new ItemResource());
            Abilities.Add(new ItemPickable());
            Abilities.Add(new ItemUseable());
            Properties = ZoneDataFactory.Factory.CreateProperties<IItemProperties>(this);
        }


    }
    //---------------------------------------------------------------------------------//
    public abstract class IItemTemplateAbility : IDataAbility
    {
    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.ItemUseValue)]
    [Desc("8.物品使用基础（依赖物品使用）")]
    public class ItemUseValue : IItemTemplateAbility
    {
        [Desc("添加HP")] public int AddHP;
        [Desc("添加MP")] public int AddMP;
        [Desc("添加SP")] public int AddSP;
        [Desc("增加经验")] public int AddEXP;
        [Desc("增加金币")] public int AddMoney;

        [Desc("添加HP（百分比）")] public float AddHP_Pct;
        [Desc("添加MP（百分比）")] public float AddMP_Pct;
        [Desc("添加SP（百分比）")] public float AddSP_Pct;

    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.ItemMotion)]
    [Desc("7.物品移动")]
    public class ItemMotion : IItemTemplateAbility
    {
        [Desc("移动速度")]
        public float MotionSpeedSEC = 10f;
        [Desc("飞行结束后特效")]
        public LaunchEffect MoveFinishEffect;
        [Desc("飞行到目标发生特效")]
        public LaunchEffect MoveTargetEffect;
    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.ItemBuyable)]
    [Desc("6.物品购买")]
    public class ItemBuyable : IItemTemplateAbility
    {
        [Desc(Category = "购买", Desc = "购买花费金币")]
        public bool BuyCostMoney;
        [Desc(Category = "购买", Desc = "出售花费金币")]
        public bool SellGotMoney;
    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.ItemInventory)]
    [Desc("5.物品背包")]
    public class ItemInventory : IItemTemplateAbility
    {
        [Desc(Category = "背包", Desc = "在背包内最大堆叠数量")]
        public int MaxStackCount = 100;
        [Desc(Category = "背包", Desc = "最大持有数量，0表示无上限")]
        public int HoldingLimit = 0;
        [Desc(Category = "背包", Desc = "是否在背包内可使用")]
        public bool IsInventoryUseable = true;
        [Desc(Category = "背包", Desc = "是否可携带多个(包括堆叠)")]
        public bool IsDuplicateInventory = true;
        [Desc(Category = "背包", Desc = "同步到服务器（可持久化保存的道具）")]
        public bool SyncToServer = false;
        [Desc(Category = "背包", Desc = "背包道具使用读条(毫秒)")]
        public int UseInProgressTimeMS = 0;
    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.ItemUseable)]
    [Desc("4.物品使用")]
    public class ItemUseable : IItemTemplateAbility
    {
        [Desc(Category = "使用", Desc = "使用间隔时间（毫秒）")]
        public int UseCoolDownTimeMS = 0;
        [Desc(Category = "使用", Desc = "使用后的特效")]
        public LaunchEffect UseEffect;
        [Desc(Category = "使用", Desc = "使用后释放一个法术")]
        public LaunchSpell UseSpell;
        [Desc(Category = "使用", Desc = "使用后召唤单位")]
        public SummonUnit UseSummon;
        [Desc(Category = "使用", Desc = "使用后增加BUFF列表")]
        [ListDesc(typeof(LaunchBuff))]
        public ArrayList<LaunchBuff> UseBuffs = new ArrayList<LaunchBuff>();
        [Desc(Category = "使用", Desc = "使用后获得词缀")]
        [ListDesc(typeof(CardSlot))]
        public ArrayList<CardSlot> UseCards = new ArrayList<CardSlot>();
    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.ItemEquip)]
    [Desc("3.物品装备")]
    public class ItemEquip : IItemTemplateAbility
    {
        [Desc("装备", Desc = "装备后增加BUFF列表")]
        [ListDesc(typeof(LaunchBuff))]
        public ArrayList<LaunchBuff> EquipBuffs = new ArrayList<LaunchBuff>();
    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.ItemPickable)]
    [Desc("2.物品检取")]
    public class ItemPickable : IItemTemplateAbility
    {
        [Desc("3.检取", Desc = "掉落后多久可以获得（毫秒）")]
        public int GotCoolDownTimeMS = 500;
        [Desc("2.检取", Desc = "道具拾取时检查物品bodysize")]
        public bool CheckItemBodySize = true;
        [Desc(Category = "2.检取", Desc = "手动检取读条时间（毫秒）为0则自动拾取")]
        public int PickTimeMS = 2000;
        [Desc(Category = "2.捡取", Desc = "拾取后删除")]
        public bool RemoveOnFinishPick = true;
        [Desc(Category = "2.捡取", Desc = "拾取次数次数")]
        [DependOnProperty(nameof(RemoveOnFinishPick))]
        public int PickTimes = 1;
        [Desc(Category = "2.捡取", Desc = "连续拾取")]
        public bool ContinuousPick = false;

        [Desc(Category = "2.捡取", Desc = "可以多个单位一起捡取")]
        public bool TogetherPicking = true;

        [Desc(Category = "3.掉落", Desc = "仅接受的单位类型")]
        public UnitType[] DropAcceptUnitTypes;
        [Desc(Category = "3.掉落", Desc = "阻止的单位类型")]
        public UnitType[] DropDenyUnitTypes;

        [Desc(Category = "3.掉落", Desc = "掉落道具仅玩家有效")]
        public bool PlayerOnly = true;
        [Desc(Category = "3.掉落", Desc = "掉落道具全阵营有效")]
        public bool DropForAll;

        [Desc(Category = "3.掉落", Desc = "获得后的特效(物品自身)")]
        public LaunchEffect GotEffectSelf;
    }
    //---------------------------------------------------------------------------------//
    [MessageType(BattleConstants.ItemResource)]
    [Desc("1.物品资源")]
    public class ItemResource : IItemTemplateAbility
    {

        [Desc(Category = "2.资源", Desc = "资源模型名字")]
        [ResourceID(ResourceType.Object)] public string FileName;
        [Desc(Category = "2.资源Id", Desc = "资源模型名字")]
        public int FileResId
        {
            get
            {
                if (Parser.TryParseInt(FileName, out var resId))
                    return resId;
                return 0;
            }
        }

        [Desc("缩放比率", "2.资源")]
        public float BodyScale = 1;
        [Desc("高度对齐方式", "2.资源")]
        public VoxelAnchor BodyVoxelAnchor = VoxelAnchor.Floating;

        [Desc("绑定特效", "2.资源")]
        public LaunchEffect BindingEffect;
    }
    //---------------------------------------------------------------------------------//

}

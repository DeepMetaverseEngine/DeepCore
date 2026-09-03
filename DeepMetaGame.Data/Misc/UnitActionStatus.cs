using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{

    [Desc("单位主状态")]
    public enum UnitActionStatus : byte
    {
        [Desc("暂停")]
        Pause = 0,
        [Desc("休闲")]
        Idle = 1,
        [Desc("移动")]
        Move = 2,
        [Desc("技能")]
        Skill = 3,
        [Desc("受伤")]
        Damage = 4,
        [Desc("死亡")]
        Dead = 5,
        [Desc("昏迷")]
        Stun = 6,
        [Desc("拾取")]
        Pick = 7,
        [Desc("混乱")]
        Chaos = 8,
        [Desc("逃跑")]
        Escape = 9,
        [Desc("骑乘")]
        Ride = 10,
        [Desc("跳跃")]
        Jump = 11,
        [Desc("攀爬")]
        Climb = 12,
        [Desc("散步")]
        Walk = 13,
        [Desc("准备战斗")] Ready = 14,
        [Desc("躲避")] Evasion = 15,
        [Desc("盾档")] ShieldBlock = 16,
        [Desc("格挡")] WeaponBlock = 17,

        A0 = 20, A1, A2, A3, A4, A5, A6, A7, A8, A9,
        B0 = 30, B1, B2, B3, B4, B5, B6, B7, B8, B9,
        C0 = 40, C1, C2, C3, C4, C5, C6, C7, C8, C9,
        D0 = 50, D1, D2, D3, D4, D5, D6, D7, D8, D9,


        [Desc("出生")]
        Spawn = 100,
        [Desc("复活")]
        Rebirth,
        [Desc("传送")]
        Transport,


        Build,
        Work,
        Forge,
        Cook,
        Operate,
        Collect,
        Cut,
        Dig,
        Swim,

        Eat,
        Drink,
        Rest,
        Sleep,

        Happy,
        Cheer,
        Relax,

        Sing,
        Talk,
        Hold,
        Wait,
        Think,
        Dance,

        Pee,
        Poo,

        Angry,
        Party, //派对

        [Desc("大轻功")]
        ClientCustom = 200,
        [Desc("表演")]
        Show = 201,
        [Desc("翻滚闪避")]
        Somersault = 202,



        NA = 255,
    }

    public enum SkillActiveState : byte
    {
        Active = 0,
        Deactive = 1,
        DeactiveAndPause = 2,
        Hide = 3,
        ActiveAndHide = 4,
    }


}

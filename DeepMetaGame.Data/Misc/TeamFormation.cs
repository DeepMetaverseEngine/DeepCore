using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{

    [MessageType(BattleConstants.TeamFormation)]
    [Desc("单位组阵型")]
    [Expandable]
    public class TeamFormation : IBaseFuncData
    {
        public enum Formation : byte
        {
            [Desc("随机")]
            Random = 0,

            [Desc("方阵")]
            Square = 1,

            [Desc("以中心点放射出去的圆阵")]
            Round = 2,

            [Desc("圆环")]
            Cycle = 3,

            [Desc("蜂窝阵")]
            Beehive = 4,

            [Desc("圆环随机")]
            RandomCycle = 5,


            [Desc("横队")]
            Horizontal = 6,

            [Desc("纵队")]
            Vertical = 7,
        }

        [Desc("阵型")]
        public Formation TFormation = Formation.Random;
        [DependOnProperty(nameof(TFormation))]
        public bool IsSquare { get { return TFormation == Formation.Square; } }


        [Desc("单位间距，间距不计算身体半径，0表示按最大身体计算")]
        public float SpacingSize = 5;

        [Desc("方阵: 每行固定人数，0表示行列相等")]
        [DependOnProperty(nameof(IsSquare))]
        public int SquareEachRowCount = 0;

        public override string ToString()
        {
            return string.Format("阵型:{0}", TFormation);
        }
    }

}

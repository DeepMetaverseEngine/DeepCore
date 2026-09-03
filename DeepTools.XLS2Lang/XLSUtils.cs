using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepTools.LanguageXLS
{
   public static class XLSUtils
    {

        public static bool IsRowEmpty(IRow row)
        {
            if (row == null) return true;
            for (int ci = row.FirstCellNum; ci < row.LastCellNum; ci++)
            {
                ICell cell = row.GetCell(ci);
                if (cell != null && cell.CellType != CellType.Blank)
                {
                    return false;
                }
            }
            return true;
        }

    }
}

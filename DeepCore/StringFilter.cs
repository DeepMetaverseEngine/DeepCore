using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DeepCore
{

    public class StringFilters
    {
        protected List<Regex> filters_add = new List<Regex>();
        protected List<Regex> filters_dec = new List<Regex>();

        /// <summary>
        /// <pre>
        /// + 代表包含(默认)，- 代表不包含。
        /// 	比如: -.svn (排除所有.svn目录)
        /// 多项时用 ; 分隔。
        /// 	比如: +.png;+.jpg (只匹配.png和.jpg)
        /// </pre>
        /// </summary>
        /// <param name="regex"></param>
        public StringFilters(string regex)
        {
            string[] fts = regex.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string fti in fts)
            {
                var ft = fti.Trim();
                if (!string.IsNullOrEmpty(ft))
                {
                    if (ft.StartsWith("-"))
                    {
                        filters_dec.Add(new Regex(ft.Substring(1)));
                    }
                    else if (ft.StartsWith("+"))
                    {
                        filters_add.Add(new Regex(ft.Substring(1)));
                    }
                    else
                    {
                        filters_add.Add(new Regex(ft));
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="text"></param>
        /// <returns> true 符合， false 排除</returns>
        public bool Accept(string text)
        {
            if (filters_dec.Count > 0)
            {
                // 判断所有需要排除的
                foreach (var ft in filters_dec)
                {
                    if (ft.IsMatch(text))
                    {
                        return false;
                    }
                }
            }
            if (filters_add.Count > 0)
            {
                // 判断所有需要包含的
                foreach (var ft in filters_add)
                {
                    if (ft.IsMatch(text))
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }
        public const string USAGE = @"
格式：
  匹配项1;匹配项2;...匹配项N;
匹配项：
  (前缀)(正则表达式)(后缀)(分隔符)
前缀：
  + 代表包含(默认)
  比如： +\.png$ (包含.png后缀的文件)
  - 代表排除
  比如： -\.bmp$ (排除.bmp后缀的文件)
后缀：
  / 表示是个目录 (只在文件系统启效)
  比如： -\.svn$/ (排除所有.svn目录)
分隔符：
  ; 分割匹配项
  比如： +\.png$;+\.jpg$;-\.bmp$;-\.svn$/; (只匹配.png和.jpg，并排除.bmp和.svn目录)
注意：
  排除项比包含项优先级更高
  比如： +data/;-\.bmp$; (包含所有data目录，并且排除所有.bmp文件)
";
        public static string Usage(string line_prefix)
        {
            return USAGE.Trim();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCrystal.RPC
{
    public interface ICornJobContext
    {
        /// <summary>
        /// 本次：实际执行时间
        /// </summary>
        DateTimeOffset FireTimeUtc { get; }
        /// <summary>
        /// 本次：计划执行时间
        /// </summary>
        DateTimeOffset? ScheduledFireTimeUtc { get; }
        /// <summary>
        /// 计划下次执行时间
        /// </summary>
        DateTimeOffset? NextFireTimeUtc { get; }
        /// <summary>
        /// 计划上次执行时间
        /// </summary>
        DateTimeOffset? PreviousFireTimeUtc { get; }
        object State { get; }
    }

    /// <summary>
    /// 错过行为
    /// </summary>
    public enum CornJobMissFirePolicy
    {
        /// <summary>
        /// 错过后什么都不做
        /// </summary>
        DoNothing,
        /// <summary>
        /// 错过后，执行一次
        /// </summary>
        FireOnceNow,
        /// <summary>
        /// 错过后，执行历史记录，执行所有错过时间点
        /// </summary>
        FireHistory,
    }
    public enum CornMonthOfYear
    {
        JAN, FEB, MAR, APR, MAY, JUN, JUL, AUG, SEP, OCT, NOV, DEC
    }
    public enum CornDayOfWeek
    {
        SUN, MON, TUE, WED, THU, FRI, SAT
    }
    /// <summary>
    /// <see>http://cron.qqe2.com/</see>
    /// <para>Cron Expressions</para>
    /// <para>cron的表达式被用来配置CronTrigger实例。 cron的表达式是字符串，实际上是由七子表达式，描述个别细节的时间表。这些子表达式是分开的空白，代表：</para>
    /// <list type="number">
    /// <item><term>Seconds</term><description>description</description></item>
    /// <item><term>Minutes</term><description>description</description></item>
    /// <item><term>Hours</term><description>description</description></item>
    /// <item><term>Day-of-Month</term><description>description</description></item>
    /// <item><term>Month</term><description>description</description></item>
    /// <item><term>Day-of-Week</term><description>description</description></item>
    /// <item><term>Year (可选字段)</term><description>description</description></item>
    /// </list>    
    /// <para>例  "0 0 12 ? * WED" 在每星期三下午12:00 执行,个别子表达式可以包含范围, 例如，在前面的例子里("WED")可以替换成 "MON-FRI", "MON, WED, FRI"甚至"MON-WED,SAT".“*” 代表整个时间段.</para>
    /// <para>每一个字段都有一套可以指定有效值，如</para>
    /// <list type="number">
    /// <item><term>Seconds(秒)</term><description>可以用数字0－59 表示，</description></item>
    /// <item><term>Minutes(分)</term><description>可以用数字0－59 表示，</description></item>
    /// <item><term>Hours(时)</term><description>可以用数字0-23表示,</description></item>
    /// <item><term>Day-of-Month(天)</term><description>可以用数字1-31 中的任一一个值，但要注意一些特别的月份</description></item>
    /// <item><term>Month(月)</term><description>可以用0-11 或用字符串  “JAN, FEB, MAR, APR, MAY, JUN, JUL, AUG, SEP, OCT, NOV and DEC” 表示</description></item>
    /// <item><term>Day-of-Week(每周)</term><description>可以用数字1-7表示（1 ＝ 星期日）或用字符口串“SUN, MON, TUE, WED, THU, FRI and SAT”表示</description></item>
    /// <item><term>Year(可选字段)</term><description>可以用数字XXXX(比如2009)表示</description></item>
    /// <item><term>“*”</term><description>代表整个时间段.</description></item>
    /// <item><term>“/”</term><description>为特别单位，表示为“每”如“0/15”表示每隔15分钟执行一次,“0”表示为从“0”分开始, “3/20”表示表示每隔20分钟执行一次，“3”表示从第3分钟开始执行</description></item>
    /// <item><term>“?”</term><description>表示每月的某一天，或第周的某一天</description></item>
    /// <item><term>“L”</term><description>用于每月，或每周，表示为每月的最后一天，或每个月的最后星期几如“6L”表示“每月的最后一个星期五”</description></item>
    /// <item><term>“W”</term><description>表示为最近工作日，如“15W”放在每月（day-of-month）字段上表示为“到本月15日最近的工作日”</description></item>
    /// <item><term>“,”</term><description>指定数个值，多个并列值，每天的0点、13点、18点、21点都执行一次：13,18,21</description></item>
    /// <item><term>“#”</term><description>是用来指定“的”每月第n个工作日,例 在每周（day-of-week）这个字段中内容为"6#3" or "FRI#3" 则表示“每月第三个星期五”</description></item>
    /// </list>    
    /// <para>Cron表达式的格式：秒 分 时 日 月 周 年(可选)。</para>
    /// 
    ///                字段名                 允许的值                        允许的特殊字符  
    ///                秒                         0-59                               , - * /  
    ///                分                         0-59                               , - * /  
    ///                小时                     0-23                               , - * /  
    ///                日                         1-31                               , - * ? / L W C  
    ///                月                         1-12 or JAN-DEC         , - * /  
    ///                周几                     1-7 or SUN-SAT           , - * ? / L C #  
    ///                年 (可选字段)         empty, 1970-2099      , - * /
    /// 
    ///                “?”字符：表示不确定的值
    ///                “,”字符：指定数个值
    ///                “-”字符：指定一个值的范围
    ///                “/”字符：指定一个值的增加幅度。n/m表示从n开始，每次增加m
    ///                “L”字符：用在日表示一个月中的最后一天，用在周表示该月最后一个星期X
    ///                “W”字符：指定离给定日期最近的工作日(周一到周五)
    ///                “#”字符：表示该月第几个周X。6#3表示该月第3个周五
    /// 
    /// </summary>
    public class CornJobExample
    {
        /// <summary>
        /// “*”字符：代表整个时间段
        /// </summary>
        public const string CHAR_ALL = "*";
        /// <summary>
        /// 字符：表示不确定的值
        /// </summary>
        public const string CHAR_UNKNOW = "?";
        /// <summary>
        /// 字符：指定数个值
        /// </summary>
        public const string CHAR_AND = ",";
        /// <summary>
        /// 字符：指定一个值的范围
        /// </summary>
        public const string CHAR_RANGE = "-";
        /// <summary>
        /// “/”字符：指定一个值的增加幅度。表示为“每”如“0/15”表示每隔15分钟执行一次,“0”表示为从“0”分开始
        /// </summary>
        public const string CHAR_START_INTERVAL = "/";
        /// <summary>
        /// “L”字符：用在日表示一个月中的最后一天，用在周表示该月最后一个星期X
        /// </summary>
        public const string CHAR_LAST = "L";
        /// <summary>
        /// “W”字符：指定离给定日期最近的工作日(周一到周五)
        /// </summary>
        public const string CHAR_NEAR_WORKDAY = "W";
        /// <summary>
        /// “#”字符：表示该月第几个周X。6#3表示该月第3个周五
        /// </summary>
        public const string CHAR_WHICH_OF = "#";

        /// <summary>
        /// Cron表达式的格式：秒 分 时 日 月 周 年(可选)。
        /// </summary>
        /// <param name="sec">秒，允许的值：0-59；允许的特殊字符：, - * /</param>
        /// <param name="min">分，允许的值：0-59；允许的特殊字符：, - * /</param>
        /// <param name="hor">时，允许的值：0-23；允许的特殊字符：, - * /</param>
        /// <param name="day">日，允许的值：1-31；允许的特殊字符：, - * ? / L W C</param>
        /// <param name="mon">月，允许的值：1-12 JAN-DEC；允许的特殊字符：, - * /</param>
        /// <param name="wek">周，允许的值：1-7 SUN-SAT；允许的特殊字符：, - * ? / L C #</param>
        /// <param name="year">年(可选)，允许的值：empty 1970-2099；允许的特殊字符：</param>
        /// <returns></returns>
        public static string CreateCornExpression(string sec, string min = "*", string hor = "*", string day = "*", string mon = "*", string wek = "?", string year = "")
        {
            return $"{sec} {min} {hor} {day} {mon} {wek} {year}".Trim();
        }


        /// <summary>
        /// 每隔5秒执行一次：*/5 * * * * ?
        /// </summary>
        public const string Every_5Secends = "*/5 * * * * ?";

        /// <summary>
        /// 每隔1分钟执行一次：0 */1 * * * ?
        /// </summary>
        public const string Every_1Minutes = "0 */1 * * * ?";

        /// <summary>
        /// 每天23点执行一次：0 0 23 * * ?
        /// </summary>
        public const string Every_23PMoclock_OfDay = "0 0 23 * * ?";

        /// <summary>
        /// 每天凌晨1点执行一次：0 0 1 * * ?
        /// </summary>
        public const string Every_01AMoclock_OfDay = "0 0 1 * * ?";

        /// <summary>
        /// 每月1号凌晨1点执行一次：0 0 1 1 * ?
        /// </summary>
        public const string Every_01Day_01AMoclock_OfMonth = "0 0 1 1 * ?";

        /// <summary>
        /// 每月最后一天23点执行一次：0 0 23 L* ?
        /// </summary>
        public const string Every_LastDay_23PMoclock_OfMonth = "0 0 23 L* ?";

        /// <summary>
        /// 每周星期天凌晨1点实行一次：0 0 1 ? * L
        /// </summary>
        public const string Every_Sunday_01AMoclock_OfWeek = "0 0 1 ? * L";

        /// <summary>
        /// 在26分、29分、33分执行一次：0 26,29,33 * * * ?
        /// </summary>
        public const string Every_26Min_29Min_33Min_OfHour = "0 26,29,33 * * * ?";

        /// <summary>
        /// 每天的0点、13点、18点、21点都执行一次：0 0 0,13,18,21 * * ?
        /// </summary>
        public const string Every_00AMoclock_13PMoclock_18PMoclock_21PMoclock_OfDay = "0 26,29,33 * * * ?";

        /// <summary>
        /// 每天、特定整点、执行
        /// </summary>
        /// <param name="oclock">Day of oclock 0~23</param>
        /// <returns></returns>
        public static string Every_Clock_OfDay(int clockOfDay) { return $"0 0 {clockOfDay} * * ?"; }

        /// <summary>
        /// 每月、特定天、特定点、执行
        /// </summary>
        public static string Every_Day_Clock_OfMonth(CornMonthOfYear dayOfMonth, int clockOfDay) { return $"0 0 {clockOfDay} {dayOfMonth} * ?"; }

        /// <summary>
        /// 每周、特定天、特定点、执行
        /// </summary>
        /// <param name="dayOfWeek"></param>
        /// <param name="clockOfDay"></param>
        /// <returns></returns>
        public static string Every_Weekday_Clock_OfWeek(CornDayOfWeek dayOfWeek, int clockOfDay) { return $"0 0 {clockOfDay} ? * {dayOfWeek}"; }
    }
}

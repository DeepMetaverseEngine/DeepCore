using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Formula
{
    [Desc("数字比较运算")]
    public enum NumericComparisonOP
    {
        [Desc(" == ")]
        EQUAL,
        [Desc(" != ")]
        NOT_EQUAL,
        [Desc(" < ")]
        LESS_THAN,
        [Desc(" <= ")]
        LESS_THAN_OR_EQUAL,
        [Desc(" > ")]
        GREATER_THAN,
        [Desc(" >= ")]
        GREATER_THAN_OR_EQUAL,
    }

    [Desc("对象比较运算")]
    public enum ObjectComparisonOP
    {
        [Desc(" == ")]
        EQUAL,
        [Desc(" != ")]
        NOT_EQUAL,
    }

    [Desc("布尔运算")]
    public enum BooleanOP
    {
        [Desc(" == ")]
        EQUAL,
        [Desc(" != ")]
        NOT_EQUAL,
        AND,
        OR,
        XOR,
    }

    [Desc("数字运算")]
    public enum NumericOP
    {
        [Desc(" + ")]
        ADD,
        [Desc(" - ")]
        SUB,
        [Desc(" * ")]
        MUL,
        [Desc(" / ")]
        DIV,
        [Desc(" MOD ")]
        MOD,
    }

    public static class FormulaHelper
    {
        public static bool Compare<T>(T a, NumericComparisonOP op, T b) where T : IComparable
        {
            int d = a.CompareTo(b);
            switch (op)
            {
                case NumericComparisonOP.EQUAL:
                    return d == 0;
                case NumericComparisonOP.NOT_EQUAL:
                    return d != 0;
                case NumericComparisonOP.LESS_THAN:
                    return d < 0;
                case NumericComparisonOP.LESS_THAN_OR_EQUAL:
                    return d <= 0;
                case NumericComparisonOP.GREATER_THAN:
                    return d > 0;
                case NumericComparisonOP.GREATER_THAN_OR_EQUAL:
                    return d >= 0;
            }
            throw new Exception("NumericComparisonOP未识别的操作数: " + op);
        }

        public static bool Compare(object a, ObjectComparisonOP op, object b)
        {
            if (a != null)
            {
                switch (op)
                {
                    case ObjectComparisonOP.EQUAL:
                        if (a != null && b != null) return a.Equals(b);
                        return a == b;
                    case ObjectComparisonOP.NOT_EQUAL:
                        if (a != null && b != null) return !a.Equals(b);
                        return a != b;
                }
            }
            else
            {
                return a == b;
            }
            throw new Exception("ObjectComparisonOP未识别的操作数: " + op);
        }

        public static bool Compare<T>(T a, ObjectComparisonOP op, T b) where T : class
        {
            switch (op)
            {
                case ObjectComparisonOP.EQUAL:
                    if (a != null && b != null) return a.Equals(b);
                    return a == b;
                case ObjectComparisonOP.NOT_EQUAL:
                    if (a != null && b != null) return !a.Equals(b);
                    return a != b;
            }
            throw new Exception("ObjectComparisonOP未识别的操作数: " + op);
        }

        public static bool Compare(bool a, ObjectComparisonOP op, bool b)
        {
            switch (op)
            {
                case ObjectComparisonOP.EQUAL:
                    return a == b;
                case ObjectComparisonOP.NOT_EQUAL:
                    return a != b;
            }
            throw new Exception("ObjectComparisonOP未识别的操作数: " + op);
        }
        public static bool Calculate(Func<bool> a, BooleanOP op, Func<bool> b)
        {
            switch (op)
            {
                case BooleanOP.EQUAL:
                    return a() == b();
                case BooleanOP.NOT_EQUAL:
                    return a() != b();
                case BooleanOP.AND:
                    return a() && b();
                case BooleanOP.OR:
                    return a() || b();
                case BooleanOP.XOR:
                    return a() ^ b();
            }
            throw new Exception("BooleanOP未识别的操作数: " + op);
        }
        public static bool Calculate(bool a, BooleanOP op, bool b)
        {
            switch (op)
            {
                case BooleanOP.EQUAL:
                    return a == b;
                case BooleanOP.NOT_EQUAL:
                    return a != b;
                case BooleanOP.AND:
                    return a && b;
                case BooleanOP.OR:
                    return a || b;
                case BooleanOP.XOR:
                    return a ^ b;
            }
            throw new Exception("BooleanOP未识别的操作数: " + op);
        }


        public static int Calculate(int a, NumericOP op, int b)
        {
            switch (op)
            {
                case NumericOP.ADD:
                    return a + b;
                case NumericOP.SUB:
                    return a - b;
                case NumericOP.MUL:
                    return a * b;
                case NumericOP.DIV:
                    return a / b;
                case NumericOP.MOD:
                    return a % b;
            }
            throw new Exception("NumericOP未识别的操作数: " + op);
        }

        public static float Calculate(float a, NumericOP op, float b)
        {
            switch (op)
            {
                case NumericOP.ADD:
                    return a + b;
                case NumericOP.SUB:
                    return a - b;
                case NumericOP.MUL:
                    return a * b;
                case NumericOP.DIV:
                    return a / b;
                case NumericOP.MOD:
                    return a % b;
            }
            throw new Exception("NumericOP未识别的操作数: " + op);
        }
        public static double Calculate(double a, NumericOP op, double b)
        {
            switch (op)
            {
                case NumericOP.ADD:
                    return a + b;
                case NumericOP.SUB:
                    return a - b;
                case NumericOP.MUL:
                    return a * b;
                case NumericOP.DIV:
                    return a / b;
                case NumericOP.MOD:
                    return a % b;
            }
            throw new Exception("NumericOP未识别的操作数: " + op);
        }

        public static string ToString(NumericComparisonOP op)
        {
            switch (op)
            {
                case NumericComparisonOP.EQUAL:
                    return "等于";
                case NumericComparisonOP.NOT_EQUAL:
                    return "不等于";
                case NumericComparisonOP.LESS_THAN:
                    return "小于";
                case NumericComparisonOP.LESS_THAN_OR_EQUAL:
                    return "小于或等于";
                case NumericComparisonOP.GREATER_THAN:
                    return "大于";
                case NumericComparisonOP.GREATER_THAN_OR_EQUAL:
                    return "大于或等于";
            }
            return " nop ";
        }

        public static string ToString(ObjectComparisonOP op)
        {
            switch (op)
            {
                case ObjectComparisonOP.EQUAL:
                    return "等于";
                case ObjectComparisonOP.NOT_EQUAL:
                    return "不等于";
            }
            return " nop ";
        }


        public static string ToString(BooleanOP op)
        {
            switch (op)
            {
                case BooleanOP.EQUAL:
                    return "等于";
                case BooleanOP.NOT_EQUAL:
                    return "不等于";
                case BooleanOP.AND:
                    return "并且";
                case BooleanOP.OR:
                    return "或者";
                case BooleanOP.XOR:
                    return "异或";
            }
            return " nop ";
        }


        public static string ToString(NumericOP op)
        {
            switch (op)
            {
                case NumericOP.ADD:
                    return "+";
                case NumericOP.SUB:
                    return "-";
                case NumericOP.MUL:
                    return "*";
                case NumericOP.DIV:
                    return "/";
                case NumericOP.MOD:
                    return "%";
            }
            return " nop ";
        }
    }

}

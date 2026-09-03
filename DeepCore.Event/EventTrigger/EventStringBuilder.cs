using DeepCore.EventTrigger.Data;
using DeepCore.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace DeepCore.EventTrigger
{

    public class EventStringBuilder
    {
        private StringBuilder sb = new StringBuilder();
        private string indent = "";
        public EventStringBuilder() { }
        public EventStringBuilder(EventStringBuilder esb)
        {
            this.sb = esb.sb;
            this.indent = esb.indent;
        }
        sealed public override string ToString()
        {
            return sb.ToString();
        }
        public string DisplayText { get { return sb.ToString(); } }

        #region APPEND
        //重载加法运算符+
        public static EventStringBuilder operator +(EventStringBuilder f, object g)
        {
            return f.Append(g);
        }
        public static EventStringBuilder operator +(object f, EventStringBuilder g)
        {
            return g.Append(f);
        }
        public static EventStringBuilder operator +(EventStringBuilder f, EventExternalizable g)
        {
            return f.Append(g);
        }
        private const string INDENT = XmlUtil.CHAR_TAB;
        private EventStringBuilder Indent(int count)
        {
            var ret = count * INDENT.Length;
            if (count > 0)
            {
                indent += CUtils.SequenceString(INDENT, count);
            }
            if (count < 0)
            {
                indent = indent.Substring(-ret);
                if (sb.Length >= -ret)
                {
                    for (int i = (-count) - 1; i >= 0; --i)
                    {
                        var end = sb.ToString(sb.Length - INDENT.Length, INDENT.Length);
                        if (end == INDENT)
                        {
                            sb.Remove(sb.Length - INDENT.Length, INDENT.Length);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            return this;
        }
        public EventStringBuilder IndentBegin()
        {
            Indent(1);
            AppendLine();
            return this;
        }
        public EventStringBuilder IndentBegin(string txt)
        {
            Indent(1);
            AppendLine(txt);
            return this;
        }
        public EventStringBuilder IndentEnd()
        {
            Indent(-1);
            //AppendLine();
            return this;
        }
        public EventStringBuilder IndentEnd(string txt)
        {
            Indent(-1);
            Append(txt);
            return this;
        }
        public EventStringBuilder Append(string arg)
        {
            sb.Append(arg);
            return this;
        }
        public EventStringBuilder Append(object value)
        {
            if (value is EventExternalizable ext)
            {
                ext.BuildText(this);
                //                 if (ext.OwnerNode is EventBehaviorNode node)
                //                 {
                //                     ext.GetText(this);
                //                     if (node is EventBehaviorAction action)
                //                     {
                //                         if (action.NEXT!=null && action.NEXT.Count > 0)
                //                         {
                //                             AppendLine();
                //                             for (int i = 0; i < action.NEXT.Count; i++)
                //                             {
                //                                 Append(action.NEXT[i]?.Data);
                //                                 if (i < action.NEXT.Count - 1)
                //                                 {
                //                                     AppendLine();
                //                                 }
                //                             }
                //                         }
                //                     }
                //                     ext.GetEndText(this);
                //                 }
                //                 else
                //                 {
                //                     ext.GetText(this);
                //                     ext.GetEndText(this);
                //                 }
            }
            else if (value != null)
            {
                sb.Append(value.ToString());
            }
            else
            {
                sb.Append("<c color='" + COLOR_CONST + "'>NULL</c>");
            }
            return this;
        }
        public EventStringBuilder AppendLine(string arg)
        {
            sb.AppendLine(arg);
            sb.Append(indent);
            return this;
        }
        public EventStringBuilder AppendLine(object arg)
        {
            Append(arg);
            sb.AppendLine();
            sb.Append(indent);
            return this;
        }
        public EventStringBuilder AppendLine(ICollection args)
        {
            foreach (var arg in args)
            {
                this.AppendLine(arg);
            }
            return this;
        }
        public EventStringBuilder AppendLine()
        {
            sb.AppendLine();
            sb.Append(indent);
            return this;
        }
        public EventStringBuilder AppendFormat(string format, params object[] args)
        {
            var argst = Array.ConvertAll(args, c => FunctionText(c, this));
            return Append(string.Format(format, argst));
        }

        public EventStringBuilder AppendForEach(Action<EventStringBuilder> forAction, Action<EventStringBuilder> doAction)
        {
            Append("<c color='" + COLOR_KEYWORKD + "'>FOR IN</c> ");
            forAction(this);
            Append(" <c color='" + COLOR_KEYWORKD + "'>DO</c>").AppendLine();
            IndentBegin("{");
            doAction(this);
            IndentEnd("}");
            return this;
        }

        public EventStringBuilder AppendInColor(string argb, Action<EventStringBuilder> action)
        {
            Append(COLOR_BEGIN(argb));
            action(this);
            Append(COLOR_END());
            return this;
        }
        public EventStringBuilder AppendInColor(int rgb, Action<EventStringBuilder> action)
        {
            Append(COLOR_BEGIN(rgb));
            action(this);
            Append(COLOR_END());
            return this;
        }

        #endregion

        private static string FunctionText(object value, EventStringBuilder src)
        {
            var sb = new EventStringBuilder();
            if (src != null)
            {
                sb.indent = src.indent;
            }
            sb.Append(value);
            return sb.ToString();
        }

        public readonly string COLOR_KEYWORKD = "FF0000FF";
        public readonly string COLOR_CONST = "FFFF0000";
        public readonly string COLOR_COMMENT = "FF008000";

        public static string COLOR_TEXT(int rgb) => (0xFF000000 | rgb).ToString("X8");
        public static string COLOR_BEGIN(int rgb) => $"<c color='{COLOR_TEXT(rgb)}'>";
        public static string COLOR_BEGIN(string argb) => $"<c color='{argb}'>";
        public static string COLOR_END() => "</c>";


        public static XmlDocument FunctionDocument(object value)
        {
            if (value is IEventDataNode node)
            {
                return FunctionDocument(node);
            }
            else if (value is EventBehaviorNode bnode)
            {
                return FunctionDocument(bnode);
            }
            var txt = FunctionText(value, null);
            return XmlUtil.FromString("<doc>" + txt + "</doc>");
        }

        public static XmlDocument FunctionDocument(IEventDataNode node)
        {
            var sb = new EventStringBuilder();
            sb.Append("<doc>");
            if (node != null)
            {
                if (node.EventLocalVars.Count > 0)
                {
                    sb.Indent(1);
                    sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 定义变量：</f>");
                    for (int i = 0; i < node.EventLocalVars.Count; i++)
                    {
                        sb.AppendLine(node.EventLocalVars[i]);
                    }
                    sb.Indent(-1);
                }
                if (node.EventTriggers.Count > 0)
                {
                    sb.Indent(1);
                    sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 事件起因：</f>");
                    for (int i = 0; i < node.EventTriggers.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append("<f color='FF0000FF'>OR</f> ");
                        }
                        sb.AppendLine(node.EventTriggers[i]);
                    }
                    sb.Indent(-1);
                }
                else
                {
                    sb.Append("(无起因)");
                }
                if (node.EventConditions.Count > 0)
                {
                    sb.Indent(1);
                    sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 满足条件：</f>");
                    for (int i = 0; i < node.EventConditions.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append("<f color='FF0000FF'>AND</f> ");
                        }
                        sb.AppendLine(node.EventConditions[i]);
                    }
                    sb.Indent(-1);
                }
                else
                {
                    sb.Append("(无条件)");
                }
                if (node.EventActions.Count > 0)
                {
                    sb.Indent(1);
                    sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 执行动作：</f>");
                    for (int i = 0; i < node.EventActions.Count; i++)
                    {
                        sb.AppendLine(node.EventActions[i]);
                    }
                    sb.Indent(-1);
                }
                else
                {
                    sb.Append("(无动作)");
                }
            }
            sb.Append("</doc>");
            var txt = sb.ToString();
            return XmlUtil.FromString(txt, true);
        }

        private static void FunctionDocument(EventBehaviorNode node, EventStringBuilder sb)
        {
            if (node != null)
            {
                var value = node.EventData;
                if (node is EventBehaviorTrigger)
                {
                    sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 开始监听：</f>");              
                }
                sb.AppendLine(value);
                if (node is EventBehaviorTrigger trigger)
                {
                    if (trigger.CALL.Count > 0)
                    {
                        sb.IndentBegin("{");
                        sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 执行动作：</f>");
                        for (int i = 0; i < trigger.CALL.Count; i++)
                        {
                            FunctionDocument(trigger.CALL[i], sb);
                        }
                        sb.IndentEnd("}");
                        sb.AppendLine();
                    }
                }
            }

            //             else if (node is EventBehaviorAction action)
            //             {
            //                 if (action.NEXT.Count > 0)
            //                 {
            //                     sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 继续执行动作：</f>");
            //                     for (int i = 0; i < action.NEXT.Count; i++)
            //                     {
            //                         FunctionDocument(action.NEXT[i], sb);
            //                     }
            //                 }
            //             }
        }
        public static XmlDocument FunctionDocument(EventBehaviorNode node)
        {
            var sb = new EventStringBuilder();
            sb.Append("<doc>");
            FunctionDocument(node, sb);
            sb.Append("</doc>");
            return XmlUtil.FromString(sb.ToString(), true);
        }

        public static XmlDocument BehaviorDocument(EventBehaviorAssembly behavior)
        {
            var sb = new EventStringBuilder();
            sb.Append("<doc>");
            {
                if (behavior.LocalVars.Count > 0)
                {
                    sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 定义变量：</f>");
                    foreach (var localvar in behavior.LocalVars)
                    {
                        sb.AppendLine(localvar.VAR);
                    }
                }
                if (behavior.Triggers.Count > 0)
                {
                    sb.AppendLine("-----------------------------------------------------------------");
                    foreach (var trigger in behavior.Triggers)
                    {
                        sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 开始监听：</f>");
                        sb.AppendLine(trigger.Trigger);
                        if (trigger.CALL.Count > 0)
                        {
                            sb.IndentBegin("{");
                            sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 执行动作：</f>");
                            for (int i = 0; i < trigger.CALL.Count; i++)
                            {
                                FunctionDocument(trigger.CALL[i], sb);
                            }
                            sb.IndentEnd("}");
                            sb.AppendLine();
                        }
                        sb.AppendLine("-----------------------------------------------------------------");
                    }
                }
            }
            sb.Append("</doc>");
            var txt = sb.ToString();
            return XmlUtil.FromString(txt, true);
        }

        public static XmlDocument BehaviorDocument(IReadOnlyList<IEventDataNode> events)
        {
            var sb = new EventStringBuilder();
            sb.Append("<doc>"); 
            sb.AppendLine("-----------------------------------------------------------------");
            CUtils.ForEachLast(events.Count, (index, last) => 
            { 
                var e = events[index];
                if (e.EventBehavior != null)
                {
                    sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'>***" + e.EventName + "***</f>");
                    var behavior = new EventBehaviorAssembly().Init(e.EventBehavior);
                    {
                        if (behavior.LocalVars.Count > 0)
                        {
                            sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 定义变量：</f>");
                            foreach (var localvar in behavior.LocalVars)
                            {
                                sb.AppendLine(localvar.VAR);
                            }
                        }
                        if (behavior.Triggers.Count > 0)
                        {
                            sb.AppendLine("-----------------------------------------------------------------");
                            CUtils.ForEachLast(behavior.Triggers.Count, (index, last) =>
                            {
                                var trigger = behavior.Triggers[index];
                                sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 开始监听：</f>");
                                sb.AppendLine(trigger.Trigger);
                                if (trigger.CALL.Count > 0)
                                {
                                    sb.IndentBegin("{");
                                    sb.AppendLine("<f color='" + sb.COLOR_COMMENT + "'># 执行动作：</f>");
                                    for (int i = 0; i < trigger.CALL.Count; i++)
                                    {
                                        FunctionDocument(trigger.CALL[i], sb);
                                    }
                                    sb.IndentEnd("}");
                                    sb.AppendLine();
                                }
                                if (!last)
                                {
                                    sb.AppendLine("-----------------------------------------------------------------");
                                }
                            });
                        }
                    }
                    if (!last)
                    {
                        sb.AppendLine("-----------------------------------------------------------------");
                    }
                }
            });
            sb.AppendLine("-----------------------------------------------------------------");
            sb.Append("</doc>");
            var txt = sb.ToString();
            return XmlUtil.FromString(txt, true);
        }


    }

    public static class EventExt
    {

        public static bool IsNullOrEmpty(this AbstractAction act)
        {
            if (act == null) return true;
            if (act is DoNoting noting)
            {
                if (noting.OwnerNode is EventBehaviorNode node)
                {
                    if (node is EventBehaviorAction action)
                    {
                        if (action.NEXT != null && action.NEXT.Count > 0)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }
    }
}

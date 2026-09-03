using DeepCore.Game3D.Slave.Layer;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Slave.GUI
{
    public static class ZoneGUIExt
    {
        public static string ToStatusText(this LayerUnit unit)
        {
            StringBuilder tl = new StringBuilder();
            using (var timelines = unit.ObjectPool.AllocList<bool>())
            {
                unit.GetMultiTimeLineStatus(timelines);
                foreach (var e in timelines) { tl.Append((e ? 1 : 0)); }
            }
            var nextExp = unit.Parent.DataRoot.DataCenter.GetUnitNeedExp(unit.Parent.Data, unit.Info, unit.Level + 1);
            string text = $"OID={unit.ObjectID}" +
                $" UUID={unit.PlayerUUID}" +
                $" Name={unit.Name}" +
                $" Force={unit.Force}" +
                $" HP={unit.HP}/{unit.MaxHP}" +
                $" MP={unit.MP}/{unit.MaxMP}" +
                $" SP={unit.SP}/{unit.MaxSP}" +
                $" State={unit.CurrentState}/{unit.CurrentSubState}" +
                $" Speed={unit.MoveSpeedSEC}" +
                $" FCR={unit.FastCastRate}" +
                $" Level={unit.Level}" +
                $" EXP={unit.Exp}/{nextExp}" +
                $" Money={unit.Money}" +
                $" Skin={unit.Skin}" +
                $" TL[{tl}]";
            return text;
        }
        /*
        public static string ZoneUnitDesc(this LayerUnit unit)
        {
            float sy = start_y;
            var text_line = new TextLine() { anchor = anc };
            var gauge_line = new GaugeStrip() { anchor = anc };
            {
                float sh = 24;
                var text = LayerDisplay.ToStatusText(unit);
                sy += text_line.SetText(text.ToString()).Draw(g, start_x, sy, 0, sh).Height;
            }

            using (var skills = unit.ObjectPool.AllocList<LayerUnit.SkillState>())
            {
                unit.GetSkillStatus(skills);
                if (skills.Count > 0)
                {
                    var gauge_fan = new GaugeRectFan() { anchor = anc };
                    float sw = 50;
                    float sh = 50;
                    float sx = start_x;
                    float dy = 0;
                    int i = 0;
                    foreach (var ss in skills)
                    {
                        gauge_fan.text_brush = ss.IsActive ? Brushes.White : Brushes.Red;
                        gauge_fan.ToolTips =
                            $"Skill: {ss.Data}\n" +
                            $"  level: {ss.Level}\n" +
                            //$"  pass: {TimeSpan.FromMilliseconds(ss.PassTimeMS)}\n" +
                            $"  expire: {TimeSpan.FromMilliseconds(ss.ExpireTimeMS)}\n" +
                            $"  action index: {ss.CurrentActionID}\n" +
                            $"  speed: {ss.ActionSpeed}\n" +
                            $"  active state: {ss.ActiveState}";
                        dy = gauge_fan
                             .SetText(ss.Data.Name, (ss.IsActive ? ToSkillShortKey(i) : null))
                             .SetAmount(ss.CDAmount)
                             .Draw(g, sx, sy, sw, sh).Height;
                        sx += sw;
                        i++;
                    }
                    sy += dy;
                }
            }


            if (unit is LayerPlayer actor)
            {
                using (var items = unit.ObjectPool.AllocList<LayerPlayer.ItemSlot>())
                {
                    actor.GetItemSlots(items);
                    if (items.Count > 0)
                    {
                        var gauge_fan = new GaugeRectFan() { anchor = anc };
                        float sw = 40;
                        float sh = 40;
                        float sx = start_x;
                        float dy = 0;
                        int i = 0;
                        foreach (var item in items)
                        {
                            if (!item.IsEmpty)
                            {
                                string text1 = null;
                                string text2 = null;
                                float pct = 0;
                                text1 = item.Data.Name;
                                text2 = "x" + item.Count;
                                var cd = actor.GetCoolDownItem(item.Data.ID);
                                if (cd != null) pct = cd.Amount;
                                gauge_fan.ToolTips =
                                    $"Item: {item.Data}\n" +
                                    $"  count: {item.Count}\n" +
                                    $"  expire: {(cd != null ? TimeSpan.FromMilliseconds(cd.ExpireTimeMS) : string.Empty)}";
                                dy = gauge_fan
                                    .SetText(text1, text2)
                                    .SetAmount(pct)
                                    .Draw(g, sx, sy, sw, sh).Height;
                                sx += sw;
                            }
                            i++;
                        }
                        sy += dy;
                    }
                }
            }

            using (var buffs = unit.ObjectPool.AllocList<LayerUnit.BuffState>())
            {
                unit.GetBuffStatus(buffs);
                if (buffs.Count > 0)
                {
                    var gauge_fan = new GaugeRectFan() { anchor = anc };
                    float sw = 40;
                    float sh = 40;
                    float sx = start_x;
                    float dy = 0;
                    int i = 0;
                    foreach (var bs in buffs)
                    {
                        gauge_fan.ToolTips =
                            $"Buff: {bs.Data}\n" +
                            $"  level: {bs.BuffLevel}\n" +
                            $"  expire: {TimeSpan.FromMilliseconds(bs.ExpireTimeMS)}\n" +
                            $"  overlay level: {bs.OverlayLevel}\n" +
                            $"  is equip: {bs.isEquip}\n" +
                            $"  sender id: {bs.SenderID}\n" +
                            $"  ";
                        dy = gauge_fan.SetText(bs.Data.Name, (bs.OverlayLevel != 0) ? bs.OverlayLevel.ToString() : string.Empty).
                            SetAmount(bs.CDAmount).Draw(g, sx, sy, sw, sh).Height;
                        if (bs.OverlayLevel != 0)
                        {

                        }
                        sx += sw;
                        i++;
                    }
                    sy += dy;
                }
            }
            using (var cards = unit.ObjectPool.AllocList<LayerUnit.CardSlot>())
            {
                unit.GetCards(cards);
                if (cards.Count > 0)
                {
                    var gauge_fan = new TextRectBody() { anchor = anc };
                    float sw = 40;
                    float sh = 40;
                    float sx = start_x;
                    float dy = 0;
                    int i = 0;
                    foreach (var bs in cards)
                    {
                        gauge_fan.ToolTips =
                            $"Card: {bs.Card}\n" +
                            $"  level: {bs.Level}";
                        dy = gauge_fan.SetText($"{bs.Card.Name}", $"{bs.Level}").Draw(g, sx, sy, sw, sh).Height;
                        sx += sw;
                        i++;
                    }
                    sy += dy;
                }
            }
            {

                float sh = 24;
                if (unit.ChantingSkill != null)
                {
                    var ss = unit.ChantingSkill;
                    float pct = (unit.ChantingSkillPassMS / (float)unit.ChantingSkillTotalMS);
                    sy += gauge_line
                        .SetText("吟唱：" + ss.Data.Name)
                        .SetAmount(pct)
                        .Draw(g, start_x + 4, sy, 200, sh).Height;
                }
                if (unit.CurrentSkillAction != null)
                {
                    var skill = unit.CurrentSkillAction;
                    sy += gauge_line
                        .SetText("引导：" + skill.SkillData.Name + "(" + skill.CurrentActionIndex + ")")
                        .SetAmount(1 - skill.ExpirePercent)
                        .Draw(g, start_x + 4, sy, 200, sh).Height;
                }
                if (unit.PickEvent != null)
                {
                    var pick = unit.PickEvent;
                    sy += gauge_line
                        .SetText("检取：" + pick.Tag.object_id)
                        .SetAmount(pick.Amount)
                        .Draw(g, start_x + 4, sy, 200, sh).Height;
                }
                {
                    sy += text_line.SetText(unit.DisplayName).Draw(g, start_x, sy, 0, sh).Height;
                }
            }
        }
        */
    }
}

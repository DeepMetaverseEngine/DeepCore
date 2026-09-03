using DeepCore;
using DeepCore.EventTrigger;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepMetaGame.Data.Template;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using static DeepCore.FuncData.FuncTableGroup;
using static DeepMetaGame.Data.Template.CardTemplate;

namespace DeepMetaGame.Data.FuncData
{
    public static class CardManager
    {
        public delegate bool TryGetTableLevel<ST>(ST st, int tableName, out int tableLevel);
        public delegate void ForEachFuncDataFieldsAction<ST>(ST st, ICardRuntime runtime, IFuncData owner, FuncTable fields, IDynamicMemberInfo field, CardTemplate card, CardField cardField, CardFieldCell cardCell);
        public delegate void ForEachFuncTableAction<ST>(ST st, ICardRuntime runtime, IFuncData owner);


        public static void ForEachFuncDataFields<ST>(this ICardRuntime runtime, object root, IFuncData owner, IDynamicTypeInfo dtype,
            ST st,
            TryGetTableLevel<ST> ownerFuncs,
            ForEachFuncDataFieldsAction<ST> action)
        {
            if (owner.HasFuncID(out var group))
            {
                foreach (var funcTable in group.Tables)
                {
                    if (runtime.TryGetOriginCard(funcTable.TableName, out var card))
                    {
                        if (funcTable.Fields != null && ownerFuncs.Invoke(st, funcTable.TableName, out var cardLevel))
                        {
                            foreach (var findex in funcTable.Fields)
                            {
                                if (card.TryGetField(findex.ColumnName, out var cardField))
                                {
                                    if (cardField.TryGetLevel(cardLevel, out var cardCell, out var clamp, out var clampLevel))
                                    {
                                        if (clamp)
                                        {
                                            runtime.Log.Warn($"Root:'{root.GetType().ToDesc()}':'{root}' : Card:'{card}' Level:'{cardLevel}' Not Exist using Clamp : '{clampLevel}'");
                                        }
                                        var dfield = dtype.GetField(findex.FieldName);
                                        if (dfield != null)
                                        {
                                            action(st, runtime, owner, funcTable, dfield, card, cardField, cardCell);
                                            continue;
                                        }
                                        var propty = dtype.GetProperty(findex.FieldName);
                                        if (propty != null)
                                        {
                                            action(st, runtime, owner, funcTable, propty, card, cardField, cardCell);
                                            continue;
                                        }
                                        runtime.Log.Warn($"Root:'{root.GetType().ToDesc()}':'{root}' : Card:'{card}' : Owner:'{owner.GetType()}' FieldName:'{findex.FieldName}' Not Exist");
                                    }
                                    else
                                    {
                                        runtime.Log.Warn($"Root:'{root.GetType().ToDesc()}':'{root}' : Card:'{card}' Level:'{cardLevel}' Not Exist");
                                    }
                                }
                                else
                                {
                                    runtime.Log.Warn($"Root:'{root.GetType().ToDesc()}':'{root}' : Card:'{card}' Column:'{findex.ColumnName}' Not Exist !");
                                }
                            }
                        }
                    }
                    else
                    {
                        runtime.Log.Warn($"Root:'{root.GetType().ToDesc()}':'{root}' : Card:'{funcTable.TableName}' Not Exist !");
                    }
                }
            }
        }
        public static void ForEachFuncDataFields(this ICardRuntime runtime, object root, IFuncData owner, IDynamicTypeInfo dtype, int cardLevel, ForEachFuncDataFieldsAction<int> action)
        {
            ForEachFuncDataFields(runtime, root, owner, dtype, cardLevel, static (int st, int tableName, out int tableLevel) =>
            {
                tableLevel = st;
                return true;
            }, action);
        }
        public static void ForEachFuncDataFields(this ICardRuntime runtime, object root, IFuncData owner, IDynamicTypeInfo dtype, HashMap<int, int> ownerFuncs, ForEachFuncDataFieldsAction<HashMap<int, int>> action)
        {
            ForEachFuncDataFields(runtime, root, owner, dtype, ownerFuncs, static (HashMap<int, int> st, int tableName, out int tableLevel) =>
            {
                return st.TryGetValue(tableName, out tableLevel);
            }, action);
        }


        public static void ForEachFuncTables<ST>(this ICardRuntime runtime, ST st, object data, ForEachFuncTableAction<ST> action)
        {
            if (data == null) return;
            var type = data.GetType();
            if (type == typeof(FuncTableGroup)) return;
            if (type.IsPrimitiveFuncType()) return;
            if (type.IsArray)
            {
                var array = (Array)data;
                foreach (var e in array)
                {
                    ForEachFuncTables(runtime, st, e, action);
                }
            }
            else if (data is IDictionary map)
            {
                foreach (var e in map.Values)
                {
                    ForEachFuncTables(runtime, st, e, action);
                }
            }
            else if (data is IList list)
            {
                foreach (var e in list)
                {
                    ForEachFuncTables(runtime, st, e, action);
                }
            }
            else
            {
                var dtype = DynamicTypeFactory.Instance.GetTypeInfo(type);
                if (dtype != null)
                {
                    foreach (var dfield in dtype.GetFields())
                    {
                        ForEachFuncTables(runtime, st, dfield.GetValue(data), action);
                    }
                    if (data is IFuncData fdata)
                    {
                        action.Invoke(st, runtime, fdata);
                    }
                }
            }
        }

        /// <summary>
        /// 搜集所有词缀关联的模板放到 cartridges
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="ownerFuncs"></param>
        /// <param name="cartridges"></param>
        public static void CollectCartridges(this ICardRuntime runtime, HashMap<int, int> ownerFuncs, HashMap<Type, HashMap<int, TemplateData>> cartridges)
        {
            {
                // 搜集所有词缀关联的模板放到 cartridges
                // [Card ID] -> 等级 //
                foreach (var func in ownerFuncs)
                {
                    var cardID = func.Key;
                    // 找到词缀
                    if (runtime.TryGetOriginCard(cardID, out var card))
                    {
                        //找到所有受到天赋影响的模板数据//
                        if (!card.OnlyForSelfTemplate && card.Fields != null)
                        {
                            foreach (var field in card.Fields)
                            {
                                //var useTemplateType = field.OwnerTemplateType;
                                //var useTemplateID = field.OwnerTemplateID;
                                if (field.UsedTemplates?.References != null)
                                {
                                    foreach (var used in field.UsedTemplates.References)
                                    {
                                        if (used.Value != null)
                                        {
                                            var useTemplateType = used.Key;
                                            foreach (var tid in used.Value)
                                            {
                                                var useTemplateID = tid;
                                                var cartridge = cartridges.GetOrNew(useTemplateType);
                                                if (!cartridge.ContainsKey(useTemplateID))
                                                {
                                                    //获得原始子弹
                                                    if (runtime.TryGetOriginTemplate(useTemplateType, useTemplateID, out var bullet))
                                                    {
                                                        cartridge.Add(useTemplateID, runtime.Codec.Clone<TemplateData>(bullet));
                                                    }
                                                    else
                                                    {
                                                        runtime.Log.Warn($"{card} : Can not found Binding {useTemplateType} : {useTemplateID}");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        runtime.Log.Warn($"Card Or Affect Not Founds : {cardID}");
                    }
                }
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------
        #region 填充数据
        /// <summary>
        /// 运行时由服务器传入的[FuncID,FuncLevel]来初始化战斗模板数据。
        /// </summary>
        public static void FillTemplates(this ICardRuntime runtime, TemplateData self, HashMap<int, int> ownerFuncs, HashMap<Type, HashMap<int, TemplateData>> cartridges)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // 搜集所有词缀关联的模板放到 cartridges
                {
                    cartridges.Clear();
                    CollectCartridges(runtime, ownerFuncs, cartridges);
                    cartridges.GetOrNew(self.GetType()).Put(self.ID, self);
                }
                // 通过代码生成器填充数据
                if (HasScriptFillData)
                {
                    foreach (var func in ownerFuncs)
                    {
                        var cardID = func.Key;
                        var cardLevel = func.Value;
                        // 找到词缀
                        if (runtime.TryGetOriginCard(cardID, out var card))
                        {
                            if (card.Fields != null && card.Fields.Count > 0)
                            {
                                if (cardLevel >= card.LevelsCount)
                                {
                                    runtime.Log.Error($"Card: {card} Level Exceeds Maximum: {card.LevelsCount} Level: {cardLevel}");
                                    cardLevel = card.LevelsCount - 1;
                                }
                                if (!TryFillCardDelegate(runtime, card, cardLevel, cartridges))
                                {
                                    runtime.Log.Error($"Card: {card} Fill Script Not Found");
                                }
                            }
                        }
                    }
                }
                else
                {
                    // 填充所有模板数据，VerySlowly
                    try
                    {
                        //runtime.FillData(self, ownerFuncs);
                        foreach (var cartridge in cartridges.Values)
                        {
                            foreach (var t in cartridge)
                            {
                                runtime.DeepFillData(t.Value, ownerFuncs);
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        runtime.Log.Error(err);
                    }
                }
            }
            finally
            {
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    runtime.Log.Warn($"FillTemplates Elapsed : {stopwatch.ElapsedMilliseconds} ms");
                }
            }
        }
        private static void DeepFillDataFieldInternal(this ICardRuntime runtime, IFuncData owner, IDynamicMemberInfo member, CardTemplate card, CardField cardField, CardFieldCell cardCell, FieldOperation op)
        {
            try
            {
                var fvalue = cardCell.FieldValue;
                switch (op)
                {
                    case FieldOperation.ADD:
                        if (fvalue != null)
                        {
                            var src = (decimal)Convert.ChangeType(member.GetValue(owner), typeof(decimal));
                            var dec = (decimal)Convert.ChangeType(fvalue, typeof(decimal));
                            member.SetValue(owner, Convert.ChangeType(src + dec, member.MemberType));
                        }
                        return;
                    case FieldOperation.SUB:
                        if (fvalue != null)
                        {
                            var src = (decimal)Convert.ChangeType(member.GetValue(owner), typeof(decimal));
                            var dec = (decimal)Convert.ChangeType(fvalue, typeof(decimal));
                            member.SetValue(owner, Convert.ChangeType(src - dec, member.MemberType));
                        }
                        return;
                    case FieldOperation.MUL:
                        if (fvalue != null)
                        {
                            var src = (decimal)Convert.ChangeType(member.GetValue(owner), typeof(decimal));
                            var dec = (decimal)Convert.ChangeType(fvalue, typeof(decimal));
                            member.SetValue(owner, Convert.ChangeType(src * dec, member.MemberType));
                        }
                        return;
                    case FieldOperation.DIV:
                        if (fvalue != null)
                        {
                            var src = (decimal)Convert.ChangeType(member.GetValue(owner), typeof(decimal));
                            var dec = (decimal)Convert.ChangeType(fvalue, typeof(decimal));
                            member.SetValue(owner, Convert.ChangeType(src / dec, member.MemberType));
                        }
                        return;
                    case FieldOperation.DEL:
                        {
                            member.SetValue(owner, null);
                        }
                        return;
                    case FieldOperation.SET:
                    default:
                        {
                            member.SetValue(owner, fvalue);
                        }
                        return;
                }
            }
            catch (Exception err)
            {
                runtime.Log.Error($"FillDataFieldInternal : Card={card} Field={cardField}", err);
            }
        }

        /// <summary>
        /// 将FuncData从表填充，一般用于灌入从表里直接创建的数据字段，用于防止表数据变了，填充的数据还没变。
        /// </summary>
        /// <param name="data">要填充的数据</param>
        private static void DeepFillFromFuncID(this ICardRuntime runtime, object data)
        {
            DeepFillFromFuncIDInternal(runtime, data, data);
        }
        private static void DeepFillFromFuncIDInternal(ICardRuntime runtime, object root, object data)
        {
            if (data == null) return;
            var type = data.GetType();
            if (type == typeof(FuncTableGroup)) return;
            if (type.IsPrimitiveData()) return;
            if (type.IsArray)
            {
                var array = (Array)data;
                foreach (var e in array)
                {
                    DeepFillFromFuncIDInternal(runtime, root, e);
                }
            }
            else if (data is IDictionary map)
            {
                foreach (var e in map.Values)
                {
                    DeepFillFromFuncIDInternal(runtime, root, e);
                }
            }
            else if (data is IList list)
            {
                foreach (var e in list)
                {
                    DeepFillFromFuncIDInternal(runtime, root, e);
                }
            }
            else
            {
                var dtype = DynamicTypeFactory.Instance.GetTypeInfo(type);
                if (dtype != null)
                {
                    foreach (var dfield in dtype.GetFields())
                    {
                        DeepFillFromFuncIDInternal(runtime, root, dfield.GetValue(data));
                    }
                    if (data is IFuncData fdata)
                    {
                        //扫描所有FuncData绑定的模板ID////从模板数据填充FuncData//
                        runtime.ForEachFuncDataFields(root, fdata, dtype, 0, static (level, runtime, owner, fields, field, card, cardf, cell) =>
                        {
                            field.SetValue(owner, cell.FieldValue);
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 运行时由服务器传入的[FuncID,FuncLevel]来找到对应填充的字段。
        /// </summary>
        /// <param name="data">要填充的数据</param>
        /// <param name="ownerFuncs">存在的天赋[FuncID,FuncLevel]</param>
        private static void DeepFillData(this ICardRuntime runtime, object data, HashMap<int, int> ownerFuncs)
        {
            DeepFillDataInternal(runtime, data, data, ownerFuncs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="data"></param>
        /// <param name="ownerFuncs">[FuncID,FuncLevel]</param>
        private static void DeepFillDataInternal(ICardRuntime runtime, object root, object data, HashMap<int, int> ownerFuncs)
        {
            if (data == null) return;
            if (ownerFuncs == null || ownerFuncs.Count == 0) return;
            var type = data.GetType();
            if (type == typeof(FuncTableGroup)) return;
            if (type.IsPrimitiveFuncType()) return;
            if (type.IsArray)
            {
                var array = (Array)data;
                foreach (var e in array)
                {
                    DeepFillDataInternal(runtime, root, e, ownerFuncs);
                }
            }
            else if (data is IDictionary map)
            {
                foreach (var e in map.Values)
                {
                    DeepFillDataInternal(runtime, root, e, ownerFuncs);
                }
            }
            else if (data is IList list)
            {
                foreach (var e in list)
                {
                    DeepFillDataInternal(runtime, root, e, ownerFuncs);
                }
            }
            else
            {
                var dtype = DynamicTypeFactory.Instance.GetTypeInfo(type);
                if (dtype != null)
                {
                    foreach (var dfield in dtype.GetFields())
                    {
                        DeepFillDataInternal(runtime, root, dfield.GetValue(data), ownerFuncs);
                    }
                    if (data is IFuncData fdata)
                    {
                        //扫描所有FuncData绑定的模板ID//
                        runtime.ForEachFuncDataFields(root, fdata, dtype, ownerFuncs, static (ownerFuncs, runtime, owner, fields, field, card, cardField, cardCell) =>
                        {
                            DeepFillDataFieldInternal(runtime, owner, field, card, cardField, cardCell, cardField.FieldOP);
                        });
                    }
                }
            }
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------------
        #region 复制数据
        /// <summary>
        /// DST.F = SRC.F
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="usedCards"></param>
        public static void CopyData(TemplateData src, TemplateData dst, List<CardTemplate> usedCards)
        {
            if (HasScriptCopyData)
            {
                foreach (var card in usedCards)
                {
                    if (!TryCopyCardDelegate(card, src, dst))
                    {
                        //runtime.Log.Error($"Card: {card} Copy Script Not Found");
                    }
                }
            }
            else
            {
                DeepCopyDataInternal(src, dst, src, dst);
            }
        }
        private static void DeepCopyDataInternal(object srcRoot, object dstRoot, object srcData, object dstData)
        {
            if (srcData == null) return;
            if (dstData == null) return;
            var type = srcData.GetType();
            if (type != dstData.GetType()) return;
            if (typeof(IFuncTableGroup).IsAssignableFrom(type)) return;
            if (type.IsPrimitiveFuncType()) return;
            if (type.IsArray)
            {
                var srcArray = (Array)srcData;
                var dstArray = (Array)dstData;
                for (int i = Math.Min(srcArray.Length, dstArray.Length) - 1; i >= 0; --i)
                {
                    var srcItem = srcArray.GetValue(i);
                    var dstItem = dstArray.GetValue(i);
                    if (srcItem.GetType().IsPrimitiveFuncType())
                    {
                        //dstArray.SetValue(srcItem, i);
                    }
                    else
                    {
                        DeepCopyDataInternal(srcRoot, dstRoot, srcItem, dstItem);
                    }
                }
            }
            else if (srcData is IDictionary)
            {
                var srcMap = (IDictionary)srcData;
                var dstMap = (IDictionary)dstData;
                foreach (var key in srcMap.Keys)
                {
                    var srcItem = srcMap[key];
                    var dstItem = dstMap[key];
                    if (srcItem.GetType().IsPrimitiveFuncType())
                    {
                        //dstMap[key] = srcItem;
                    }
                    else
                    {
                        DeepCopyDataInternal(srcRoot, dstRoot, srcItem, dstItem);
                    }
                }
            }
            else if (srcData is IList)
            {
                var srcList = (IList)srcData;
                var dstList = (IList)dstData;
                for (int i = Math.Min(srcList.Count, dstList.Count) - 1; i >= 0; --i)
                {
                    var srcItem = srcList[i];
                    var dstItem = dstList[i];
                    if (srcItem.GetType().IsPrimitiveFuncType())
                    {
                        //dstList[i] = srcItem;
                    }
                    else
                    {
                        DeepCopyDataInternal(srcRoot, dstRoot, srcItem, dstItem);
                    }
                }
            }
            else
            {
                var dtype = DynamicTypeFactory.Instance.GetTypeInfo(type);
                if (dtype != null)
                {
                    if (dstData is IFuncData fdata && fdata.Tables is FuncTableGroup group)
                    {
                        var tuple = ValueTuple.Create(dtype, srcData, dstData);
                        group.ForEachFields(tuple, static (tuple, table, index) =>
                        {
                            var dtype = tuple.Item1;
                            var dfield = dtype.GetField(index.FieldName);
                            if (dfield != null && dfield.Field.FieldType.IsPrimitiveFuncType())
                            {
                                var srcData = tuple.Item2;
                                var dstData = tuple.Item3;
                                var srcItem = dfield.GetValue(srcData);
                                var dstItem = dfield.GetValue(dstData);
                                dfield.SetValue(dstData, srcItem);
                            }
                            return false;
                        });
                    }
                    {
                        foreach (var dfield in dtype.GetFields())
                        {
                            var srcItem = dfield.GetValue(srcData);
                            var dstItem = dfield.GetValue(dstData);
                            if (dfield.Field.FieldType.IsPrimitiveFuncType())
                            {
                                //dfield.SetValue(dstData, srcItem);
                            }
                            else
                            {
                                DeepCopyDataInternal(srcRoot, dstRoot, srcItem, dstItem);
                            }
                        }
                    }
                }
            }
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------------
        #region 代码生成器代理
        public static bool HasScriptFillData => script_fill_data.Count > 0;
        public static bool HasScriptCopyData => script_copy_data.Count > 0;

        static HashMap<int, FillDataDelegate> script_fill_data = new HashMap<int, FillDataDelegate>();
        static HashMap<int, CopyDataDelegate> script_copy_data = new HashMap<int, CopyDataDelegate>();
        public static void RegistDelegate(int cardID, FillDataDelegate func1, CopyDataDelegate func2)
        {
            if (func1 != null) script_fill_data.Put(cardID, func1);
            if (func2 != null) script_copy_data.Put(cardID, func2);
        }
        public static void RegistFillDataDelegate(int cardID, FillDataDelegate func1)
        {
            if (func1 != null) script_fill_data.Put(cardID, func1);
        }
        public static void RegistCopyDataDelegate(int cardID, CopyDataDelegate func2)
        {
            if (func2 != null) script_copy_data.Put(cardID, func2);
        }
        public static void RegistFillDataAssembly(IFillCartridgesAssembly assembly)
        {
            foreach (var method in assembly.GetType().GetMethods())
            {
                if (method.TryGetAttribute<FillTemplateAttribute>(out var attr1))
                {
                    RegistFillDataDelegate(attr1.CardID, new FillDataDelegate((runtime, card, level, cartridges) =>
                    {
                        method.Invoke(assembly, new object[] { runtime, card, level, cartridges });
                    }));
                }
                if (method.TryGetAttribute<CopyTemplateAttribute>(out var attr2))
                {
                    RegistCopyDataDelegate(attr2.CardID, new CopyDataDelegate((src, dst) =>
                    {
                        method.Invoke(assembly, new object[] { src, dst });
                    }));
                }
            }
        }
        public static bool TryFillCardDelegate(ICardRuntime runtime, CardTemplate card, int level, HashMap<Type, HashMap<int, TemplateData>> cartridges)
        {
            if (script_fill_data.TryGetValue(card.ID, out var func))
            {
                func.Invoke(runtime, card, level, new TCartridgesMeta() { Map = cartridges });
                return true;
            }
            return false;
        }
        public static bool TryCopyCardDelegate(CardTemplate card, object src, object dst)
        {
            if (script_copy_data.TryGetValue(card.ID, out var func))
            {
                func.Invoke(src, dst);
                return true;
            }
            return false;
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------------


    }

    public static class CardTemplateEditor
    {
        public static T AS<T>(this object obj)
        {
            return (T)obj;
        }
        public static CardFieldCell NewCell(this CardField f)
        {
            if (f?.Levels != null && f.Levels.Length > 0)
            {
                return new CardFieldCell()
                {
                    FieldValue = f.Levels[f.Levels.Length - 1].FieldValue,
                };
            }
            else if (f?.FieldType != null)
            {
                return new CardFieldCell()
                {
                    FieldValue = f.FieldType.NewPrimitiveValue(),
                };
            }
            else
            {
                return new CardFieldCell()
                {
                    FieldValue = null,
                };
            }
        }
        public static CardField Clone(this CardField f)
        {
            return XmlUtil.CloneObject(f);
        }

        public static int GetMaxLevel(this CardTemplate card)
        {
            int max = 0;
            foreach (var f in card.Fields)
            {
                max = Math.Max(f.LevelsCount, max);
            }
            return max;
        }

        public static int ExpandMaxLevel(this CardTemplate card)
        {
            var max = card.GetMaxLevel();
            foreach (var f in card.Fields)
            {
                ExpandMaxLevel(f, max);
            }
            return max;
        }
        public static int ExpandMaxLevel(this CardField f, int maxlevel)
        {
            f.Levels = CUtils.SetArrayLength(f.Levels, f, maxlevel, static (f, i) => f.NewCell());
            return maxlevel;
        }
        public static bool TryGetField(this CardTemplate card, string columnName, out CardField field)
        {
            try
            {
                field = card.Fields.Find(f => f.ColumnName == columnName);
                return field != null;
            }
            catch (Exception err)
            {
                throw new Exception($"TryGetField Error : Card='{card}' Column='{columnName}'", err);
            }
        }
        public static bool AcceptField(this CardTemplate card, FuncFieldIndex fieldIndex, MemberInfo member)
        {
            if (card.TryGetField(fieldIndex.ColumnName, out var field))
            {
                if (member is FieldInfo f) return f.FieldType == field.FieldType;
                if (member is PropertyInfo p) return p.PropertyType == field.FieldType;
            }
            return false;
        }
        public static bool TryGetLevel(this CardField field, int level, out CardFieldCell cell, out bool clamp, out int clampLevel)
        {
            if (field.Levels != null && field.Levels.Length > 0)
            {
                if (level >= 0 && level < field.Levels.Length)
                {
                    clamp = false;
                    cell = field.Levels[level];
                    clampLevel = level;
                    return true;
                }
                else
                {
                    clamp = true;
                    level = Math.Max(level, 0);
                    level = Math.Min(level, field.Levels.Length - 1);
                    cell = field.Levels[level];
                    clampLevel = level;
                    return true;
                }
            }
            clamp = false;
            cell = null;
            clampLevel = 0;
            return false;
        }




    }



    //------------------------------------------------------------------------------------------------
    #region 结构
    public interface ICardRuntime
    {
        Logger Log { get; }
        IExternalizableFactory Codec { get; }
        IReadOnlyCollection<CardTemplate> AllOriginCards { get; }

        bool TryGetOriginTemplate(Type templateType, int templateID, out TemplateData temp);
        bool TryGetOriginCard(int tableName, out CardTemplate card);
    }

    public struct TCartridgesMeta
    {
        public HashMap<Type, HashMap<int, TemplateData>> Map;
        public bool TryGetTemplateData(Type type, int id, out TemplateData temp)
        {
            if (Map.TryGetValue(type, out var map) && map.TryGetValue(id, out temp))
            {
                return true;
            }
            temp = null;
            return false;
        }
        public bool TryGetTemplateData<T>(int id, out T temp) where T : TemplateData
        {
            if (TryGetTemplateData(typeof(T), id, out var _temp))
            {
                temp = _temp as T;
                return true;
            }
            temp = null;
            return false;
        }
    }
    //------------------------------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Method)]
    public class FillTemplateAttribute : Attribute
    {
        public int CardID { get; }
        public FillTemplateAttribute(int cardID)
        {
            CardID = cardID;
        }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class CopyTemplateAttribute : Attribute
    {
        public int CardID { get; }
        public CopyTemplateAttribute(int cardID)
        {
            CardID = cardID;
        }
    }
    public delegate void FillDataDelegate(ICardRuntime runtime, CardTemplate card, int level, TCartridgesMeta cartridges);
    public delegate void CopyDataDelegate(object src, object dst);
    public interface IFillCartridgesAssembly
    {

    }
    #endregion
    //------------------------------------------------------------------------------------------------

}

using DeepCore;
using DeepCore.Concurrent;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Reflection.Modeling;
using DeepMetaGame.Data;
using DeepMetaGame.Data.FuncData;
using DeepMetaGame.Data.Template;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DeepCore.FuncData.FuncTableGroup;
using static DeepMetaGame.Data.Template.CardTemplate;

namespace DeepMetaGame.Tools
{
    public delegate void OnSaveTemplateData(CardTemplate card, TemplateData data, UmlValueNode dataFieldNode, CardFieldCell cardCell);

    public abstract class CardRuntimeAdapter : ICardRuntime
    {
        public IExternalizableFactory Codec => ZoneDataFactory.Codec;
        public abstract Logger Log { get; }
        public abstract IReadOnlyCollection<CardTemplate> AllOriginCards { get; }
        public abstract bool TryGetOriginCard(int tableName, out CardTemplate card);
        public abstract bool TryGetOriginTemplate(Type templateType, int templateID, out TemplateData temp);
        public abstract IReadOnlyCollection<TemplateData> GetAllTemplatesData();
        public bool TryRefreshAffectBindings(CardTemplate card, bool check, OnSaveTemplateData onDataChange = null)
        {
            bool needrefresh = card.OnlyForSelfTemplate;
            foreach (var field in card.Fields)
            {
                //if (field.OwnerTemplateType == null || field.OwnerTemplateID == 0)
                if (field.UsedTemplates?.References == null || field.UsedTemplates.References.Count == 0)
                {
                    needrefresh = true;
                    break;
                }
                if (field.UsedTemplates.References.Any((e) => (e.Value == null) || (e.Value.Count == 0) || e.Value.Any(ee => ee == 0)))
                {
                    needrefresh = true;
                    break;
                }
                //                 foreach (var used in field.UsedTemplates)
                //                 {
                //                     if (used.OwnerTemplateType == null || used.OwnerTemplateID == 0)
                //                     {
                //                         needrefresh = true;
                //                         break;
                //                     }
                //                 }
                if (needrefresh) break;
            }
            if (needrefresh)
            {
                SaveOnlyForSelfTemplate(GetAllTemplatesData(), card, check, onDataChange);
                RefreshAffectBindings(GetAllTemplatesData(), card, check, onDataChange);
            }
            return needrefresh;
        }
        public void SaveOnlyForSelfTemplate(IReadOnlyCollection<TemplateData> alltemp, CardTemplate card, bool check, OnSaveTemplateData onDataChange = null)
        {
            var t = this;
            if (card.OnlyForSelfTemplate)
            {
                if (check)
                {
                    if (card.Fields != null)
                    {
                        foreach (var cardF in card.Fields)
                        {
                            cardF.UsedTemplates = null;
                        }
                    }
                }
                var affect = new AffectBindingTemplates();
                foreach (var data in alltemp)
                {
                    UmlDocument uml = new UmlDocument(data);
                    UmlValueNode node = uml.DocumentElement;
                    t.GenAffectBinding(card, affect, data, node, onDataChange);
                }
            }
        }
        public AffectBindingTemplates RefreshAffectBindings(IReadOnlyCollection<TemplateData> alltemp, CardTemplate card, bool check, OnSaveTemplateData onDataChange = null)
        {
            var t = this;
            //if (!card.OnlyForSelfTemplate)
            {
                if (check)
                {
                    if (card.Fields != null)
                    {
                        foreach (var cardF in card.Fields)
                        {
                            cardF.UsedTemplates = null;
                        }
                    }
                }
                var affect = new AffectBindingTemplates();
                foreach (var data in alltemp)
                {
                    UmlDocument uml = new UmlDocument(data);
                    UmlValueNode node = uml.DocumentElement;
                    t.GenAffectBinding(card, affect, data, node, onDataChange);
                }
                if (card.Fields != null)
                {
                    foreach (var cardF in card.Fields)
                    {
                        if (cardF.UsedTemplates != null && cardF.UsedTemplates.References != null)
                        {
                            foreach (var e in cardF.UsedTemplates.References)
                            {
                                e.Value.Sort(static (a, b) => a - b);
                            }
                        }
                    }
                }
                return affect;
            }
            //return null;
        }

        public void CleanUpAffectBinding(TemplateData temp, bool cleanup)
        {
            CleanUpAffectBindingInternal(temp, temp, cleanup);
        }
        private void CleanUpAffectBindingInternal(object root, object data, bool cleanup)
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
                    CleanUpAffectBindingInternal(root, e, cleanup);
                }
            }
            else if (data is IDictionary map)
            {
                foreach (var e in map.Values)
                {
                    CleanUpAffectBindingInternal(root, e, cleanup);
                }
            }
            else if (data is IList list)
            {
                foreach (var e in list)
                {
                    CleanUpAffectBindingInternal(root, e, cleanup);
                }
            }
            else
            {
                var dtype = DynamicTypeFactory.Instance.GetTypeInfo(type);
                if (dtype != null)
                {
                    foreach (var dfield in dtype.GetFields())
                    {
                        CleanUpAffectBindingInternal(root, dfield.GetValue(data), cleanup);
                    }
                    if (data is IFuncData fdata)
                    {
                        //扫描所有FuncData绑定的模板ID//
                        if (fdata.HasFuncID(out var group))
                        {
                            foreach (var funcTable in group.Tables.ArrayCopy())
                            {
                                if (TryGetOriginCard(funcTable.TableName, out var card) && funcTable.Fields != null)
                                {
                                    foreach (var findex in funcTable.Fields)
                                    {
                                        if (card.TryGetField(findex.ColumnName, out var cardField))
                                        {
                                            var dfield = dtype.GetField(findex.FieldName);
                                            if (dfield != null)
                                            {
                                                continue;
                                            }
                                            var propty = dtype.GetProperty(findex.FieldName);
                                            if (propty != null)
                                            {
                                                continue;
                                            }
                                            Log.Warn($"Root:'{root.GetType().ToDesc()}':'{root}' : Card:'{card}' : Owner:'{data.GetType()}' FieldName:'{findex.FieldName}' Not Exist");
                                        }
                                        else
                                        {
                                            Log.Warn($"Root:'{root.GetType().ToDesc()}':'{root}' : Card:'{card}' Column:'{findex.ColumnName}' Not Exist !");
                                            if (cleanup)
                                            {
                                                Log.Error($"Root:'{root.GetType().ToDesc()}':'{root}' : Remove Field:'{findex.FieldName}' !");
                                                group.CleanField(funcTable.TableName, findex.FieldName);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Log.Warn($"Root:'{root.GetType().ToDesc()}':'{root}' : Card:'{funcTable.TableName}' Not Exist !");
                                    if (cleanup)
                                    {
                                        Log.Error($"Root:'{root.GetType().ToDesc()}':'{root}' : Remove Card:'{funcTable.TableName}' !");
                                        group.CleanTable(funcTable.TableName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public HashMap<CardTemplate, AffectBindingTemplates> GenAffectBindings(IRangeValue p, OnSaveTemplateData onDataChange = null)
        {
            var cards = AllOriginCards;
            var ret = new HashMap<CardTemplate, AffectBindingTemplates>();
            p?.SetMax(cards.Count);
            foreach (var card in AllOriginCards)
            {
                p?.SetText($"{card}");
                var affect = GenAffectBindings(card, onDataChange);
                if (affect != null)
                {
                    ret.Add(card, affect);
                }
                p?.Add(1);
            }
            return ret;
        }
        public AffectBindingTemplates GenAffectBindings(CardTemplate card, OnSaveTemplateData onDataChange = null)
        {
            var affect = new AffectBindingTemplates();
            var alltemp = new HashSet<TemplateData>();
            foreach (var field in card.Fields)
            {
                //                 if (TryGetTemplate(field.OwnerTemplateType, field.OwnerTemplateID, out var temp))
                //                 {
                //                     alltemp.Add(temp);
                //                 }
                if (field.UsedTemplates?.References != null)
                {
                    foreach (var e in field.UsedTemplates.References)
                    {
                        var OwnerTemplateType = e.Key;
                        if (e.Value != null)
                        {
                            foreach (var OwnerTemplateID in e.Value)
                            {
                                if (TryGetOriginTemplate(OwnerTemplateType, OwnerTemplateID, out var temp))
                                {
                                    alltemp.Add(temp);
                                }
                            }
                        }
                    }
                }
            }
            foreach (var data in alltemp)
            {
                UmlDocument uml = new UmlDocument(data);
                UmlValueNode node = uml.DocumentElement;
                this.GenAffectBinding(card, affect, data, node, onDataChange);
            }
            return affect;
        }
        private void GenAffectBinding(CardTemplate srcCard, AffectBindingTemplates affect, TemplateData root, UmlValueNode node, OnSaveTemplateData onDataChange)
        {
            if (node.IsLeaf)
            {
                return;
            }
            else
            {
                var data = node.Value;
                if (data is IFuncData fdata && fdata.HasFuncID(out var groups))
                {
                    //扫描所有FuncData绑定的模板ID//
                    foreach (var table in groups.Tables)
                    {
                        var fid = table.TableName;
                        if (srcCard.ID == fid)
                        {
                            if (TryGetTemplateRootAffectFieldsUsage(affect, root.GetType(), root.ID, out var taffects, out var usage))
                            {
                                //如果自身天赋包含模板ID//
                                var dtype = DynamicTypeFactory.Instance.GetTypeInfo(data.GetType());
                                this.ForEachFuncDataFields(root, fdata, dtype, 0, (level, runtime, owner, fields, field, card, cardField, cardCell) =>
                                {
                                    var fieldNode = node.GetChild(field.Name) as UmlValueNode;
                                    if (fields.TryGetField(field.Name, out var findex))
                                    {
                                        //恢复未绑定的OwnerID
                                        //if (cardField.OwnerTemplateType == null || cardField.OwnerTemplateID == 0)
                                        //{
                                        //cardField.OwnerTemplateID = root.ID;
                                        //cardField.OwnerTemplateType = root.GetType();
                                        //}
                                        if (srcCard.OnlyForSelfTemplate)
                                        {
                                            if (!fieldNode.Value.Equals(cardCell.FieldValue))
                                            {
                                                fieldNode.SetValue(cardCell.FieldValue);
                                                Log.Warn($"Root:'{root.GetType().ToDesc()}':'{root}' : Card:'{card}' OnlyForSelfTemplate : {fieldNode.Name} <= {cardCell.FieldValue} !");
                                                onDataChange?.Invoke(srcCard, root, fieldNode, cardCell);
                                            }
                                        }
                                        if (TryAddCardFieldUseage(cardField, root.ID, root.GetType()))
                                        {
                                            Log.Warn($"Root:'{root.GetType().ToDesc()}':'{root}' : Card:'{card}' 恢复未绑定的OwnerID !");
                                        }
                                        if (card.ID == srcCard.ID)
                                        {
                                            usage.FieldsUsage.Add(new AffectFieldUsage()
                                            {
                                                TemplateType = root.GetType(),
                                                TemplateID = root.ID,
                                                FieldUsage = $"{node.GetUMLPath()}{field.Name} {cardField.FieldOP.ToShortString()} [{card.ID}:{findex.ColumnName}]",
                                                UmlNode = fieldNode,
                                                Field = cardField,
                                            }); ;
                                        }
                                    }
                                    else
                                    {
                                        if (card.ID == srcCard.ID)
                                        {
                                            usage.FieldsUsage.Add(new AffectFieldUsage()
                                            {
                                                TemplateType = root.GetType(),
                                                TemplateID = root.ID,
                                                FieldUsage = $"{node.GetUMLPath()}{field.Name} = 'unknow'",
                                                UmlNode = fieldNode,
                                                Field = cardField,
                                            });
                                        }
                                    }
                                });
                            }
                        }
                    }
                }
                foreach (UmlValueNode sub in node.ChildNodes)
                {
                    GenAffectBinding(srcCard, affect, root, sub, onDataChange);
                }
            }
        }
        private bool TryGetTemplateRootAffectFieldsUsage(AffectBindingTemplates affect, Type type, int templateID, out AffectTemplatesUsage tusage, out AffectFieldsUsage fusage)
        {
            tusage = null;
            fusage = null;
            if (typeof(TemplateData).IsAssignableFrom(type))
            {
                tusage = affect.Affects.GetOrNew(type);
                fusage = tusage.Templates.GetOrAdd(templateID, tid => new AffectFieldsUsage() { });
                return true;
            }
            return false;
        }

        public static bool TryAddCardFieldUseage(CardField field, int templateID, Type templateType)
        {
            if (field.UsedTemplates?.References == null)
            {
                field.UsedTemplates = new CardReference()
                {
                    References = new HashMap<Type, List<int>>(),
                };
            }
            foreach (var reference in field.UsedTemplates.References)
            {
                if (reference.Key == templateType && reference.Value != null)
                {
                    foreach (var tid in reference.Value)
                    {
                        if (tid == templateID)
                        {
                            return false;
                        }
                    }
                }
            }
            var used = field.UsedTemplates.References.GetOrAdd(templateType, static type => new List<int>(1));
            used.Add(templateID);
            //             CUtils.ArrayAppend(field.UsedTemplates, new CardUsed()
            //             {
            //                 OwnerTemplateType = templateType,
            //                 OwnerTemplateID = templateID,
            //             });
            return true;
        }
        public static bool TryAddCardFieldUseage(CardTemplate card, CardField field)
        {
            foreach (var f in card.Fields)
            {
                if (f.ColumnName == field.ColumnName)
                {
                    f.UsedTemplates = field.UsedTemplates;
                    return false;
                }
            }
            card.Fields.Add(field);
            return true;
        }

    }

    //-------------------------------------------------------------------------------------------------------------------

    //     [Desc(Editable = false)]
    //     public AffectBindingTemplates TemplateAffects; [MessageType(Constants.CARD_TEMPLATE + 4)]
    public class AffectBindingTemplates : ISerializable
    {
        public HashMap<Type, AffectTemplatesUsage> Affects = new HashMap<Type, AffectTemplatesUsage>();
    }
    public class AffectTemplatesUsage : ISerializable
    {
        public HashMap<int, AffectFieldsUsage> Templates = new HashMap<int, AffectFieldsUsage>();
    }
    public class AffectFieldsUsage : ISerializable
    {
        public List<AffectFieldUsage> FieldsUsage = new List<AffectFieldUsage>();
    }
    public class AffectFieldUsage : ISerializable
    {
        public Type TemplateType;
        public int TemplateID;
        public UmlValueNode UmlNode;
        public string FieldUsage;
        public CardField Field;
        public override string ToString()
        {
            return $"{FieldUsage}";
        }
    }

    //-------------------------------------------------------------------------------------------------------------------
    public static class FuncTableExt
    {
        public static void CleanUpFuncID(this ICardRuntime runtime, object data)
        {
            runtime.ForEachFuncTables(data, data, static (st, runtime, owner) =>
            {
                if (owner.HasFuncID(out var group))
                {
                    group.CleanUp(runtime);
                }
            });
        }
        static public bool TryGetOrCreate(this FuncTableGroup _this, int tableName, out FuncTable table)
        {
            if (_this.TryGetFuncTable(tableName, out table))
            {
                return true;
            }
            else
            {
                table = new FuncTable() { TableName = tableName, };
                if (_this.Tables == null)
                {
                    _this.Tables = new FuncTable[] { table };
                }
                else
                {
                    _this.Tables = _this.Tables.ArrayAppend(table);
                }
                return false;
            }
        }
        static public bool TryRemoveTable(this FuncTableGroup _this, FuncTable table)
        {
            if (_this.Tables != null && _this.Tables.TryIndexOf(table, out var index))
            {
                _this.Tables = CUtils.ArrayRemove(_this.Tables, index);
                return true;
            }
            return false;
        }

        static public void AddField(this FuncTableGroup _this, int cardID, object fieldName, string columnName)
        {
            _this.TryGetOrCreate(cardID, out var table);
            table.TryGetOrCreate(fieldName.ToString(), columnName, out var index);
            index.ColumnName = columnName;
        }
        static public bool TryRemoveField(this FuncTableGroup _this, object fieldName)
        {
            if (_this.ForEachFields(fieldName, (fieldName, table, field) =>
            {
                if (field.FieldName == fieldName.ToString())
                {
                    table.TryRemove(field);
                    if (table.FieldsCount == 0 && _this.Tables.TryIndexOf(table, out var index))
                    {
                        _this.Tables = _this.Tables.ArrayRemove(index);
                        if (_this.Tables.Length == 0)
                        {
                            _this.Tables = null;
                        }
                    }
                    return true;
                }
                return false;
            }))
            {
                return true;
            }
            return false;
        }
        static public int ClearFields(this FuncTableGroup _this, object fieldName)
        {
            int count = 0;
            _this.ForEachFields(fieldName, (fieldName, table, field) =>
            {
                if (field.FieldName == fieldName.ToString())
                {
                    count++;
                    table.TryRemove(field);
                    if (table.FieldsCount == 0 && _this.Tables.TryIndexOf(table, out var index))
                    {
                        _this.Tables = _this.Tables.ArrayRemove(index);
                        if (_this.Tables.Length == 0)
                        {
                            _this.Tables = null;
                        }
                    }
                }
                return false;
            });
            return count;
        }
        static public void CleanUp(this FuncTableGroup _this, ICardRuntime runtime)
        {
            if (_this.Tables != null)
            {
                for (int i = _this.Tables.Length - 1; i >= 0; --i)
                {
                    var table = _this.Tables[i];
                    if (table.FieldsCount == 0)
                    {
                        _this.Tables = _this.Tables.ArrayRemove(i);
                    }
                }
            }
            if (_this.TablesCount == 0)
            {
                _this.Tables = null;
            }
        }
        static public int CleanField(this FuncTableGroup _this, int tableName, object fieldName)
        {
            int count = 0;
            if (_this.Tables != null)
            {
                foreach (var table in _this.Tables)
                {
                    if (table.TableName == tableName)
                    {
                        if (table.Fields != null)
                        {
                            var fields = table.Fields.ArrayCopy();
                            for (int i = 0; i < fields.Length; i++)
                            {
                                var field = fields[i];
                                if (field.FieldName == fieldName.ToString())
                                {
                                    table.Fields = table.Fields.ArrayRemove(i);
                                    count++;
                                }
                            }
                        }
                    }
                }
            }
            return count;
        }
        static public int CleanTable(this FuncTableGroup _this, int tableName)
        {
            int count = 0;
            if (_this.Tables != null)
            {
                var tables = _this.Tables.ArrayCopy();
                for (int i = 0; i < tables.Length; i++)
                {
                    var table = tables[i];
                    if (table.TableName == tableName)
                    {
                        _this.Tables = _this.Tables.ArrayRemove(i);
                        count++;
                    }
                }
            }
            return count;
        }

    }

    //-------------------------------------------------------------------------------------------------------------------

}

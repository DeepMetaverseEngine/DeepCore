using DeepCore.Reflection;
using DeepCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Collections;

namespace DeepTools.CodeGen
{

    public class FieldDepend
    {
        public Type OwnerType { get; private set; }
        public DependOnPropertyAttribute Depend { get; private set; }
        public FieldInfo DependField { get; private set; }
        public PropertyInfo DependProperty { get; private set; }
        public FieldDepend(Type type, DependOnPropertyAttribute depend)
        {
            this.OwnerType = type;
            this.Depend = depend;
            this.DependField = type.GetField(depend.MemberName);
            this.DependProperty = type.GetProperty(depend.MemberName);
        }
    }
    public class FieldGroup
    {
        public Type OwnerType { get; private set; }
        public string Key { get; private set; }
        public List<FieldDepend> Depends { get; private set; }
        public List<FieldInfo> Fields { get; private set; }
        public List<FieldInfo> DependsFields { get; private set; }
        public bool IsDepends { get { return Depends.Count > 0; } }
        public FieldGroup(Type type, string key, IEnumerable<DependOnPropertyAttribute> depends)
        {
            this.OwnerType = type;
            this.Key = key;
            this.Fields = new List<FieldInfo>();
            this.Depends = new List<FieldDepend>();
            this.DependsFields = new List<FieldInfo>();
            foreach (var dep in depends)
            {
                Depends.Add(new FieldDepend(type, dep));
            }
        }
        public override string ToString()
        {
            return Key;
        }
        public void Add(FieldInfo f)
        {
            this.Fields.Add(f);
            var deps = DependOnPropertyAttribute.ListDependFields(OwnerType, f);
            foreach (var dep in deps)
            {
                if (!DependsFields.Contains(dep))
                {
                    DependsFields.Add(dep);
                }
            }
        }
        public bool FindDeepDepend(string field)
        {
            foreach (var depend in DependsFields)
            {
                if (field == depend.Name) return true;
                //                 if (DependOnPropertyAttribute.TryFindInDepend(OwnerType, depend.DependField, field, out var fd)) return true;
                //                 if (DependOnPropertyAttribute.TryFindInDepend(OwnerType, depend.DependProperty, field, out fd)) return true;
            }
            return false;
        }
        public bool FindField(string field)
        {
            foreach (var depend in Fields)
            {
                if (field == depend.Name) return true;
            }
            return false;
        }
    }
    public class FieldGroupMap
    {
        public Type OwnerType { get; }
        private HashMap<string, FieldGroup> fieldgroups = new HashMap<string, FieldGroup>();
        public FieldGroupMap(Type type, bool decleard, bool is_static, bool haveBase)
        {
            OwnerType = type; var list = new List<FieldInfo>();
            {
                GetFields(OwnerType, is_static, list);
                foreach (var f in list)
                {
                    if (!decleard ||
                        f.DeclaringType == null ||
                        f.DeclaringType == type || 
                        (!haveBase && type.IsSubclassOf(f.DeclaringType)))
                    {
                        this.TryAddFieldGroup(OwnerType, f);
                    }
                }
            }
        }
        private static void GetFields(Type type, bool isStatic, List<FieldInfo> fields)
        {
            if (isStatic)
            {
                if (type.BaseType != null) GetFields(type.BaseType, isStatic, fields);
                foreach (var f in type.GetFields())
                {
                    if (f.IsStatic)
                    {
                        fields.Add(f);
                    }
                }
            }
            else
            {
                foreach (var f in type.GetFields())
                {
                    if (!f.IsStatic)
                    {
                        fields.Add(f);
                    }
                }
            }
        }
        public static bool CheckDepends(Type type, MemberInfo f)
        {
            var depends = f.GetCustomAttributes<DependOnPropertyAttribute>();
            foreach (var depend in depends)
            {
                var dependField = type.GetField(depend.MemberName);
                var dependProperty = type.GetProperty(depend.MemberName);
                if (dependField == null && dependProperty == null)
                {
                    throw new Exception($"Depend Field Or Property Not Found : '{f.Name}' depends '{depend.MemberName}' @'{type.Name}'");
                }
                if (dependProperty != null)
                {
                    if (!dependProperty.TryGetAttributes<DependOnPropertyAttribute>(out var ddps))
                    {
                        throw new Exception($"Depended Property Is Not Have Depend : '{f.Name}' depends '{depend.MemberName}' @'{type.Name}'");
                    }
                    foreach(var ddp in ddps)
                    {
                        var ddpf = type.GetField(ddp.MemberName);
                        if (ddpf == null)
                        {
                            throw new Exception($"Depended Property Field Not Found : '{f.Name}' depends '{depend.MemberName}' @'{type.Name}'");
                        }
                        // 被依赖的根Field不能依赖别的字段
                        if (ddpf.TryGetAttribute<DependOnPropertyAttribute>(out var dddp))
                        {
                            throw new Exception($"Depended Root Field Can Not Have Depend : '{f.Name}' depends '{ddp.MemberName}' @'{type.Name}'");
                        }
                    }
                }
                if (dependField != null)
                {
                    // 被依赖的根Field不能依赖别的字段
                    if (dependField.TryGetAttribute<DependOnPropertyAttribute>(out var ddp))
                    {
                        throw new Exception($"Depended Root Field Can Not Have Depend : '{f.Name}' depends '{ddp.MemberName}' @'{type.Name}'");
                    }
                }
            }
            return true;
        }
        public FieldGroup TryAddFieldGroup(Type type, FieldInfo f)
        {
            CheckDepends(type, f);
            var depends = f.GetCustomAttributes<DependOnPropertyAttribute>();
            var key = ToGroupKey(f);
            var flist = fieldgroups.GetOrAdd(key, (k) => { return new FieldGroup(type, k, depends); });
            flist.Add(f);
            return flist;
        }
        public string ToGroupKey(FieldInfo f)
        {
            var depends = f.GetCustomAttributes<DependOnPropertyAttribute>();
            if (depends != null && depends.Count() > 0)
            {
                return CUtils.ListToString(new List<DependOnPropertyAttribute>(depends), (o) => { return o.MemberName + "==" + o.Expect; });
            }
            return "";
        }

        public List<FieldGroup> Groups
        {
            get
            {
                var ret = new List<FieldGroup>(fieldgroups.Values);
                Sort(ret);
                return ret;
            }
        }
        private bool IsDependOn(FieldGroup x, FieldGroup y)
        {
            if (x == y) return false;
            //             if (y.Fields.TryFind(yd => yd.Name == "IsJumpToTarget", out var ydd))
            //             {
            //                 if (x.Fields.TryFind(xd => xd.Name == "BodyHit", out var xdd))
            //                 {
            // 
            //                 }
            //             }

            //x的依赖存在于y中//
            foreach (var yf in y.Fields)
            {
                if (x.FindDeepDepend(yf.Name))
                {
                    return true;
                }
            }

            //x没有依赖//
            //if (x.IsDepends == false) return -1;
            //y没有依赖//
            //if (y.IsDepends == false) return 1;
            //y的依赖存在于x中，或者x的依赖中
            //             foreach (var xf in x.Fields)
            //             {
            //                 if (y.FindDeepDepend(xf.Name))
            //                 {
            //                     return -1;
            //                 }
            //             }
            //             //x的依赖存在于y中//
            //             foreach (var yf in y.Fields)
            //             {
            //                 if (x.FindDeepDepend(yf.Name))
            //                 {
            //                     return 1;
            //                 }
            //             }
            return false;// x.DependsFields.Count - y.DependsFields.Count;
        }
        private void Sort(List<FieldGroup> arr)
        {
            while (true)
            {
                var next = false;
                for (int i = 0; i < arr.Count && !next; i++)  //外层循环控制排序趟数
                {
                    for (int j = 0; j < arr.Count && !next; j++)  //内层循环控制每一趟排序多少次
                    {
                        if (IsDependOn(arr[i], arr[j]) && j > i)
                        {
                            var jj = arr[j];
                            arr.RemoveAt(j);
                            arr.Insert(i, jj);
                            next = true;
                        }
                    }
                }
                if (next == false) { break; }
            }
        }
    }

}

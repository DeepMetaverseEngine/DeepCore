using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.XCSV;
using DeepCore.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore
{
    [Reflectible]
    public abstract class TemplateLoader : Disposable
    {
        public delegate void OnLoadTempaletData(string fileName, string sheetName, object keyFieldValue, object data);
        public delegate void OnLoadTempaletData<K, T>(string fileName, string sheetName, K keyFieldValue, T data) where T : new();
        public static TemplateLoader Instance { get; private set; }
        public static bool EnableLog { get; set; } = false;
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        protected readonly LazyLogger log;
        public TemplateLoader(bool instance)
        {
            log = new LazyLogger(GetType().Name);
            if (instance)
            {
                TemplateLoader.Instance = this;
            }
        }
        protected override void Disposing()
        {
        }
        public abstract void GC();
        public abstract string FILE_SUFFIX { get; }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 加载所有模板
        /// </summary>
        /// <param name="xlsFile"></param>
        /// <param name="sheetName">如果为空，则遍历所有sheet</param>
        /// <param name="dataType"></param>
        /// <param name="keyField">如果为空，则没有指定KeyField</param>
        /// <param name="createNew"></param>
        /// <param name="onLoad"></param>
        /// <returns></returns>
        protected abstract void LoadTemplatesImpl( TemplateDataCenter center, string xlsFile, string[] sheetName, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData onLoad);
        protected abstract Task LoadTemplatesImplAsync(TemplateDataCenter center, string xlsFile, string[] sheetName, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData onLoad);
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void LoadTemplates(TemplateDataCenter center, string xlsFile, string[] sheetName, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData onLoad)
        {
            this.LoadTemplatesImpl( center, xlsFile, sheetName, dataType, keyField, keyType, createNew, onLoad);
        }
        public async Task LoadTemplatesAsync(TemplateDataCenter center, string xlsFile, string[] sheetName, Type dataType, string keyField, Type keyType, Func<Type, object> createNew, OnLoadTempaletData onLoad)
        {
            await this.LoadTemplatesImplAsync( center, xlsFile, sheetName, dataType, keyField, keyType, createNew, onLoad);
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void LoadTemplates(TemplateDataCenter center, string xlsFile, Type dataType, Func<Type, object> createNew, OnLoadTempaletData onLoad)
        {
            this.LoadTemplates(center, xlsFile, null, dataType, null, null, createNew, onLoad);
        }
        public Task LoadTemplatesAsync(TemplateDataCenter center, string xlsFile, Type dataType, Func<Type, object> createNew, OnLoadTempaletData onLoad)
        {
            return this.LoadTemplatesAsync(center, xlsFile, null, dataType, null, null, createNew, onLoad);
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void LoadTemplates<K, T>(TemplateDataCenter center, string xlsFile, string keyField, OnLoadTempaletData<K, T> onLoad) where T : new()
        {
            this.LoadTemplates(center, xlsFile, null, typeof(T), keyField, typeof(K), static (t) => new T(), (f, s, k, t) =>
            {
                try
                {
                    onLoad(f, s, (K)k, (T)t);
                }
                catch (Exception err)
                {
                    throw new Exception($"OnLoad Error: Convert Error: Type={typeof(T).FullName} Xls={xlsFile} Sheet={s} KeyField={k} " + err.Message, err);
                }
            });
        }
        public Task LoadTemplatesAsync<K, T>(TemplateDataCenter center, string xlsFile, string keyField, OnLoadTempaletData<K, T> onLoad) where T : new()
        {
            return this.LoadTemplatesAsync(center, xlsFile, null, typeof(T), keyField, typeof(K), static (t) => new T(), (f, s, k, t) =>
            {
                try
                {
                    onLoad(f, s, (K)k, (T)t);
                }
                catch (Exception err)
                {
                    throw new Exception($"OnLoad Error: Convert Error: Type={typeof(T).FullName} Xls={xlsFile} Sheet={s} KeyField={k} " + err.Message, err);
                }
            });
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void LoadTemplates<K, T>(TemplateDataCenter center, string xlsFile, string[] sheetName, string keyField, OnLoadTempaletData<K, T> onLoad) where T : new()
        {
            this.LoadTemplates(center, xlsFile, sheetName, typeof(T), keyField, typeof(K), static (t) => new T(), (f, s, k, t) =>
            {
                try
                {
                    onLoad(f, s, (K)k, (T)t);
                }
                catch (Exception err)
                {
                    throw new Exception($"OnLoad Error: Convert Error: Type={typeof(T).FullName} Xls={xlsFile} Sheet={s} KeyField={k} " + err.Message, err);
                }
            });
        }
        public Task LoadTemplatesAsync<K, T>(TemplateDataCenter center, string xlsFile, string[] sheetName, string keyField, OnLoadTempaletData<K, T> onLoad) where T : new()
        {
            return this.LoadTemplatesAsync(center, xlsFile, sheetName, typeof(T), keyField, typeof(K), static (t) => new T(), (f, s, k, t) =>
            {
                try
                {
                    onLoad(f, s, (K)k, (T)t);
                }
                catch (Exception err)
                {
                    throw new Exception($"OnLoad Error: Convert Error: Type={typeof(T).FullName} Xls={xlsFile} Sheet={s} KeyField={k} " + err.Message, err);
                }
            });
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public HashMap<K, T> LoadTemplates<K, T>(TemplateDataCenter center, string xlsFile, string keyField) where T : new()
        {
            var ret = new HashMap<K, T>();
            this.LoadTemplates<K, T>(center, xlsFile, null, keyField, (f, s, k, t) =>
            {
                if (ret.ContainsKey(k))
                {
                    throw new Exception("模板Key冲突 : " + xlsFile + " : Sheet : " + s + " : Key Already Exist : " + k);
                }
                else
                {
                    ret.Add(k, t);
                }
            });
            return ret;
        }
        public async Task<HashMap<K, T>> LoadTemplatesAsync<K, T>(TemplateDataCenter center, string xlsFile, string keyField) where T : new()
        {
            var ret = new HashMap<K, T>();
            await this.LoadTemplatesAsync<K, T>(center, xlsFile, null, keyField, (f, s, k, t) =>
            {
                if (ret.ContainsKey(k))
                {
                    throw new Exception("模板Key冲突 : " + xlsFile + " : Sheet : " + s + " : Key Already Exist : " + k);
                }
                else
                {
                    ret.Add(k, t);
                }
            });
            return ret;
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public HashMap<K, T> LoadTemplates<K, T>(TemplateDataCenter center, string xlsFile, string[] sheetName, string keyField) where T : new()
        {
            var ret = new HashMap<K, T>();
            this.LoadTemplates<K, T>(center, xlsFile, sheetName, keyField, (f, s, k, t) =>
            {
                if (ret.ContainsKey(k))
                {
                    throw new Exception("模板Key冲突 : " + xlsFile + " : Sheet : " + s + " : Key Already Exist : " + k);
                }
                else
                {
                    ret.Add(k, t);
                }
            });
            return ret;
        }
        public async Task<HashMap<K, T>> LoadTemplatesAsync<K, T>(TemplateDataCenter center, string xlsFile, string[] sheetName, string keyField) where T : new()
        {
            var ret = new HashMap<K, T>();
            await this.LoadTemplatesAsync<K, T>(center, xlsFile, sheetName, keyField, (f, s, k, t) =>
            {
                if (ret.ContainsKey(k))
                {
                    throw new Exception("模板Key冲突 : " + xlsFile + " : Sheet : " + s + " : Key Already Exist : " + k);
                }
                else
                {
                    ret.Add(k, t);
                }
            });
            return ret;
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public ArrayList<T> LoadTemplatesAsList<K, T>(TemplateDataCenter center, string xlsFile, string keyField) where T : new()
        {
            var ret = new ArrayList<T>();
            this.LoadTemplates<K, T>(center, xlsFile, null, keyField, (f, s, k, t) =>
            {
                ret.Add(t);
            });
            center.SortTemplateList(ret, keyField);
            return ret;
        }
        public async Task<ArrayList<T>> LoadTemplatesAsListAsync<K, T>(TemplateDataCenter center, string xlsFile, string keyField) where T : new()
        {
            var ret = new ArrayList<T>();
            await this.LoadTemplatesAsync<K, T>(center, xlsFile, null, keyField, (f, s, k, t) =>
            {
                ret.Add(t);
            });
            center.SortTemplateList(ret, keyField);
            return ret;
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public ArrayList<T> LoadTemplatesAsList<K, T>(TemplateDataCenter center, string xlsFile, string[] sheetName, string keyField) where T : new()
        {
            var ret = new ArrayList<T>();
            this.LoadTemplates<K, T>(center, xlsFile, sheetName, keyField, (f, s, k, t) => { ret.Add(t); });
            center.SortTemplateList(ret, keyField);
            return ret;
        }
        public async Task<ArrayList<T>> LoadTemplatesAsListAsync<K, T>(TemplateDataCenter center, string xlsFile, string[] sheetName, string keyField) where T : new()
        {
            var ret = new ArrayList<T>();
            await this.LoadTemplatesAsync<K, T>(center, xlsFile, sheetName, keyField, (f, s, k, t) => { ret.Add(t); });
            center.SortTemplateList(ret, keyField);
            return ret;
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        
    }



}
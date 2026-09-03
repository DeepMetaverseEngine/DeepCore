using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace DeepCore.FuncData
{
    [Reflectible]
    public interface IFuncData
    {
        /// <summary>
        /// 编辑器内填写FuncID，运行时由服务器传入的[FuncID,FuncLevel]来找到对应填充的字段。
        /// </summary>
        IFuncTableGroup Tables { get; set; }
    }

    [Reflectible]
    public interface IFuncTemplateData : IFuncData, ISerializable
    {
        string TemplateID { get; }
        string TemplateName { get; set; }
    }

    [Reflectible]
    public interface IFuncTableGroup : IExternalizable
    {

    }
}

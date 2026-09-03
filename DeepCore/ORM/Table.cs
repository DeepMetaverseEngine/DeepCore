using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace DeepCore.ORM
{
    //------------------------------------------------------------------------------

    /// <summary>
    /// ORM存储映射
    /// </summary>

    [Reflectible]
    public interface IObjectMapping
    {
       
    }

    //------------------------------------------------------------------------------

    /// <summary>
    /// ORM存储映射，不包含子映射，适用于简单对象。
    /// 如果该类型存在于集合中，则集合容器Mapping作为父容器。
    /// 比如父节点为: MappingDictionary<string, IStructMapping>
    /// </summary>

    [Reflectible]
    public interface IStructMapping : ISerializable
    {
    }
    /// <summary>
    /// ORM存储映射，自定义存储结构。
    /// 如果该类型存在于集合中，则集合容器Mapping作为父容器。
    /// 比如父节点为: MappingDictionary<string, IStructMapping>
    /// </summary>
    public interface IBinaryStructMapping : IStructMapping, IExternalizable
    {

    }
    /// <summary>
    /// ORM存储映射，自定义存储结构。
    /// 如果该类型存在于集合中，则集合容器Mapping作为父容器。
    /// 比如父节点为: MappingDictionary<string, IStructMapping>
    /// </summary>
    public interface ITextStructMapping : IStructMapping, IExternalizable
    {

    }
    //------------------------------------------------------------------------------
    /// <summary>
    /// 无论存储在集合里还是本身，都作为一个整体存入ORM。
    /// 比如父节点为: HashMap<string, IStructWrapper>
    /// 则父节点也作为一个整体落地。
    /// </summary>
    public interface IStructWrapper : IStructMapping
    {

    }

    /// <summary>
    /// 不会被拆分的整体存入ORM
    /// </summary>
    public interface IPrimitiveWrapper
    {

    }
}

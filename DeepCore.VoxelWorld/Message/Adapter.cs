using DeepCore.Geometry;
using DeepCore.Protocol;
using DeepCore.Voxel.StreamingVoxel.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.VoxelWorld.Message
{



    public interface ISCVoxelAdapter : IDisposable
    {
        WorldInfo WorldInfo { get; }
        //-----------------------------------------------------------------------------------------------
        Task<FetchChunkByUUIDResponse> FetchChunkByUUIDAsync(string uuid);
        //-----------------------------------------------------------------------------------------------
        /// <summary>
        /// 获取静态地表
        /// </summary>
        Task<FetchMapChunkResponse> FetchMapChunkAsync(Location3D location , int lod);
        /// <summary>
        /// 监听地块改变
        /// </summary>
        /// <param name="cb"></param>
        event Action<MapCubeChanged> OnMapCubeChanged;
        /// <summary>
        /// 玩家修改地块
        /// </summary>
        /// <param name="change"></param>
        /// <returns></returns>
        Task<PostResponse> PostPlayerChangeMapCubeAsync(PlayerChangeMapCube change);
        //-----------------------------------------------------------------------------------------------
        /// <summary>
        /// 监听静态物件添加
        /// </summary>
        event Action<ObjectEnter> OnObjectEnter;
        /// <summary>
        /// 监听静态物件移除
        /// </summary>
        event Action<ObjectLeave> OnObjectLeave;
        /// <summary>
        /// 玩家向世界添加静态物件
        /// </summary>
        /// <param name="add"></param>
        /// <returns></returns>
        Task<PostResponse> PostPlayerAddObjecToWorldAsync(PlayerAddObjectToWorld add);
        /// <summary>
        /// 玩家想世界删除静态物件
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="result"></param>
        Task<PostResponse> PostPlayerRemoveObjecToWorldAsync(string oid);
        //-----------------------------------------------------------------------------------------------
        /// <summary>
        /// 监听单位进入
        /// </summary>
        /// <param name="cb"></param>
        event Action<ActorEnter> OnActorEnter;
        /// <summary>
        /// 监听单位离开
        /// </summary>
        /// <param name="cb"></param>
        event Action<ActorLeave> OnActorLeave;
        /// <summary>
        /// 监听单位AOI移动，动态物件移动
        /// </summary>
        /// <param name="cb"></param>
        event Action<ActorStatusChange> OnActorStatusChange;
        /// <summary>
        /// 获取单位信息
        /// </summary>
        Task<FetchActorInfoResponse> FetchActorInfoAsync(string uid);
        /// <summary>
        /// 玩家更新坐标
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        Task<PostResponse> PostPlayerUpdateActorStatusAsync(PlayerUpdateActorStatus update);
        //-----------------------------------------------------------------------------------------------
        /// <summary>
        /// 玩家进入世界
        /// </summary>
        /// <param name="enter"></param>
        /// <returns></returns>
        Task<PostResponse> PostPlayerEnterWorldAsync(PlayerEnterWorld enter);
        /// <summary>
        /// 玩家进入世界
        /// </summary>
        /// <param name="enter"></param>
        /// <returns></returns>
        Task<PostResponse> PostPlayerLeaveWorldAsync(PlayerLeaveWorld leave);
    }
}

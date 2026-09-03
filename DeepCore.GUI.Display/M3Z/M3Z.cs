using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepCore.GUI.M3Z
{
    //////////////////////////////////////////////////////////////////////////
    // M3Z
    //////////////////////////////////////////////////////////////////////////

    public enum M3ZType
    {
        M3Z_TYPE_PNG = 0x00474e50,
        M3Z_TYPE_JPG = 0x0047504a,
        M3Z_TYPE_BMP = 0x00504d42,

        M3Z_TYPE_PVR4 = 0x34525650,
        M3Z_TYPE_PVR2 = 0x32525650,

        M3Z_TYPE_PVR1 = 0x31525650,
        M3Z_TYPE_PVRA = 0x41525650,

        M3Z_TYPE_PKM1 = 0x314d4b50,
        M3Z_TYPE_PKMA = 0x414d4b50,

        M3Z_TYPE_ATC_E = 0x45435441,
        M3Z_TYPE_ATC_I = 0x49435441,
        M3Z_TYPE_ATC_RGB = 0x33435441,

        M3Z_TYPE_ETC1 = 0x31435445,
        M3Z_TYPE_ETCA = 0x41435445,

        M3Z_TYPE_ETC2 = 0x32435445,
        M3Z_TYPE_ASTC = 0x43545341,

        M3Z_TYPE_RGBA8 = 0x41424752,
        M3Z_TYPE_A8 = 0x00003841,

        M3Z_TYPE_DXT1 = 0x31545844,
        M3Z_TYPE_DXT3 = 0x33545844,
        M3Z_TYPE_DXT5 = 0x35545844,
    }

    public class M3ZHeaderMeta<T> where T : M3ZTrunkMeta
    {
        public const uint M3Z_HEADER = 0x5a33464d; //"MF3Z";

        /**文件头*/
        public uint header { get; private set; }
        /**文件尺寸*/
        public uint version { get; private set; }
        /**原始图片宽*/
        public int srcWidth { get; private set; }
        /**原始图片高*/
        public int srcHeight { get; private set; }
        /**原始图片是否包含半透明*/
        public bool srcHasAlpha { get; private set; }
        /**附加数据*/
        public string extUTFData { get; private set; }
        /**纹理数量*/
        public int trunkCount { get; private set; }
        /**纹理块*/
        public T[] trunks { get; private set; }

        public M3ZHeaderMeta(Stream data)
        {
            this.header = LittleEdian.GetU32(data);
            if (header != M3Z_HEADER)
            {
                throw new Exception("Invalid M3Z data");
            }
            this.version = LittleEdian.GetU32(data);

            const uint v_0100 = 0x00000100;

            if (version == v_0100)
            {
                srcWidth = LittleEdian.GetS32(data);
                srcHeight = LittleEdian.GetS32(data);
                srcHasAlpha = LittleEdian.GetBool(data);
                extUTFData = LittleEdian.GetUTF(data);
                trunkCount = LittleEdian.GetS32(data);

                trunks = new T[trunkCount];
                for (int i = 0; i < trunkCount; i++)
                {
                    trunks[i] = LoadTrunkData(data, version);
                }
            }
            else
            {
                // 君王2老M3Z文件格式代码
                srcWidth = LittleEdian.GetS32(data);
                srcHeight = LittleEdian.GetS32(data);
                srcHasAlpha = LittleEdian.GetBool(data);
                trunkCount = LittleEdian.GetS32(data);

                trunks = new T[trunkCount];
                for (int i = 0; i < trunkCount; i++)
                {
                    trunks[i] = LoadTrunkData(data, version);
                }
            }
        }

        protected virtual T LoadTrunkData(Stream data, uint version)
        {
            return new M3ZTrunkMeta(data, version) as T;
        }
    }

    public class M3ZTrunkMeta
    {
        /// <summary>
        /// 类型
        /// </summary>
        public M3ZType type { get; private set; }
        public uint flags { get; private set; }
        /// <summary>
        /// 是否包含半透明
        /// </summary>
        public bool hasAlpha { get; private set; }
        /// <summary>
        /// 二的冥宽
        /// </summary>
        public int pixelW { get; private set; }
        /// <summary>
        /// 二的冥高
        /// </summary>
        public int pixelH { get; private set; }
        /// <summary>
        /// 实际像素点宽
        /// </summary>
        public int realPixelW { get; private set; }
        /// <summary>
        /// 实际像素点高
        /// </summary>
        public int realPixelH { get; private set; }
        /// <summary>
        /// 扩展数据
        /// </summary>
        public string extUTFData { get; private set; }
        /// <summary>
        /// 数据段尺寸
        /// </summary>
        public int texDataSize { get; private set; }


        public M3ZTrunkMeta(Stream data, uint version)
        {
            const uint v_0100 = 0x00000100;

            if (version == v_0100)
            {
                this.type = (M3ZType)LittleEdian.GetU32(data);
                this.hasAlpha = LittleEdian.GetBool(data);
                this.pixelW = LittleEdian.GetS32(data);
                this.pixelH = LittleEdian.GetS32(data);
                this.realPixelW = LittleEdian.GetS32(data);
                this.realPixelH = LittleEdian.GetS32(data);
                this.extUTFData = LittleEdian.GetUTF(data);
                this.texDataSize = LittleEdian.GetS32(data);
            }
            else
            {
                this.type = (M3ZType)LittleEdian.GetU32(data);
                this.flags = LittleEdian.GetU32(data);
                this.hasAlpha = LittleEdian.GetBool(data);
                this.pixelW = LittleEdian.GetS32(data);
                this.pixelH = LittleEdian.GetS32(data);
                this.realPixelW = pixelW;
                this.realPixelH = pixelH;
                this.texDataSize = LittleEdian.GetS32(data);
            }
            LoadTextureData(data, version);
        }

        protected virtual void LoadTextureData(Stream data, uint version)
        {
            data.Position += texDataSize;
        }
    }

}

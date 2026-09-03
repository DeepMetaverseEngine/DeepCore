using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Voxel.Data
{
    public static class VoxelIO
    {
        struct PutHeadAction : IDisposable
        {
            readonly IOutputStream stream;
            readonly long pos1;
            public PutHeadAction(IOutputStream os)
            {
                this.stream = os;
                this.pos1 = os.Position;
                os.PutS64(0);
            }
            public void Dispose()
            {
                var pos2 = stream.Position;
                stream.Position = pos1;
                var len = pos2 - pos1;
                stream.PutS64(len);
                stream.Position = pos2;
            }
        }
        /// <summary>
        /// 预先占领长度，写入完毕时，写入长度
        /// </summary>
        /// <returns></returns>
        public static IDisposable BeginPutHeadLength(this IOutputStream output)
        {
            return new PutHeadAction(output);
        }
        struct GetHeadAction : IDisposable
        {
            readonly IInputStream stream;
            readonly long pos1;
            readonly long len;
            public GetHeadAction(IInputStream os)
            {
                this.stream = os;
                this.pos1 = os.Position;
                this.len = os.GetS64();
            }
            public void Dispose()
            {
                var pos2 = pos1 + len;
                stream.Position = pos2;
            }
        }
        /// <summary>
        /// 预先占领长度，读取完毕时，检查长度
        /// </summary>
        /// <returns></returns>
        public static IDisposable BeginGetHeadLength(this IInputStream input)
        {
            return new GetHeadAction(input);
        }

    }
}

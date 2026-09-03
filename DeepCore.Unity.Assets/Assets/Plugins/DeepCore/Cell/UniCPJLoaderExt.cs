using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Unity3D.Cell
{
    public static class UniCPJLoaderEXT
    {

        public static UniTask LoadAsync(this UniCPJFileResource file)
        {
            return file._LoadAsync().AsUniTask();
        }
    }
}

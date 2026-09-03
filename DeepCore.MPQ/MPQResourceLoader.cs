using DeepCore.IO;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DeepCore.MPQ
{
    public class MPQResourceLoader : IResourceLoader
    {
        private MPQFileSystem mMPQ;
        public MPQResourceLoader(MPQFileSystem fs)
        {
            this.mMPQ = fs;
            Resource.AddLoader(this);
        }
        public bool TryGetPath(string path, out string suffix)
        {
            if (Resource.IsStartWith(path, Resource.PREFIX_MPQ, out suffix))
            {
                return mMPQ.FindEntry(suffix) != null || mMPQ.GetDirectory(path) != null;
            }
            else
            {
                suffix = path;
                return mMPQ.FindEntry(suffix) != null || mMPQ.GetDirectory(path) != null;
            }
        }
        public bool IsStartWith(string path)
        {
            return TryGetPath(path, out _);
        }
        //-----------------------------------------------------------------------------------------------------

        //-----------------------------------------------------------------------------------------------------
        public string GetParent(string path)
        {
            path = Resource.FormatPath(path);
            if (path.TryLastIndexOf('/', out var indexR))
            {
                return path.Substring(indexR);
            }
            if (path.TryLastIndexOf('\\', out var indexL))
            {
                return path.Substring(indexL);
            }
            return path;
        }
        public bool ExistData(string path)
        {
            if (TryGetPath(path, out var suffix))
            {
                if (mMPQ.FindEntry(suffix) != null)
                {
                    return true;
                }
            }
            return false;
        }
        public Task<bool> ExistDataAsync(string path)
        {
            return Task.FromResult(ExistData(path));
        }
        public byte[] LoadData(string path)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
                    return mMPQ.GetData(suffix);
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not Load Data : {path}");
            }
            return null;
        }
        public async Task<byte[]> LoadDataAsync(string path)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
                    return await mMPQ.GetDataAsync(suffix);
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not Load Data : {path}");
            }
            return null;

        }
        public Stream OpenStream(string path)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
                    return mMPQ.OpenStream(suffix);
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not OpenStream : {path}");
            }
            return null;
        }
        public Task<Stream> OpenStreamAsync(string path)
        {
            return Task.FromResult(OpenStream(path));
        }
        public string[] ListFiles(string path, bool fullPath)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
                    var dir = mMPQ.GetDirectory(suffix);
                    if (dir != null)
                    {
                        return Array.ConvertAll(dir.GetFiles(), e => fullPath ? e.FullPath : e.Name);
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not ListFiles : {path}");
            }
            return null;
        }

        public string[] ListDirectories(string path, bool fullPath)
        {
            try
            {
                if (TryGetPath(path, out var suffix))
                {
                    var dir = mMPQ.GetDirectory(suffix);
                    if (dir != null)
                    {
                        return Array.ConvertAll(dir.GetDirectories(), e => fullPath ? e.FullPath : e.Name);
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace($"Can Not ListDirectories : {path}");
            }
            return null;
        }
        public Task<string[]> ListFilesAsync(string path, bool fullPath)
        {
            return Task.FromResult(ListFiles(path, fullPath));
        }
        public Task<string[]> ListDirectoriesAsync(string path, bool fullPath)
        {
            return Task.FromResult(ListDirectories(path, fullPath));
        }
        //-----------------------------------------------------------------------------------------------------

    }

}

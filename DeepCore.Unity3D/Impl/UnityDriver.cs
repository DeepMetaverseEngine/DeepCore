using DeepCore.GUI.Data;
using DeepCore.GUI.Display;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Unity3D.Platform;
using System.Diagnostics;
using System.IO;
using UnityEngine;


namespace DeepCore.Unity3D.Impl
{

    public partial class UnityDriver : GraphicsDriver
    {
        public static bool IsDebug = false;

        private static UnityDriver sInstance;
        //         private static IUnityPlatform sPlatform = new DummyUnityPlatform();
        //         public static IUnityPlatform Platform
        //         {
        //             get { return sPlatform; }
        //         }

        public static UnityDriver UnityInstance
        {
            get
            {
                if (sInstance == null)
                {
                    sInstance = new UnityDriver();
                }
                return sInstance;
            }
        }

        //         public static void SetDirver()
        //         {
        //             if (sInstance == null)
        //             {
        //                 sInstance = new UnityDriver();
        //             }
        // // 
        // //             if (IsIOS)
        // //             {
        // //                 SetDirver("DeepCore.Unity3D_IOS.UnityPlatformIOS");
        // //             }
        // //             else if (IsAndroid)
        // //             {
        // //                 SetDirver("DeepCore.Unity3D_Android.UnityPlatformAndroid");
        // //             }
        // //             else
        // //             {
        // //                 SetDirver("DeepCore.Unity3D_Win32.UnityPlatformWin32");
        // //             }
        //             //             if (UnityDriver.IsIOS)
        //             //             {
        //             //                 UnityDriver.SetDirver(new DeepCore.Unity3D_IOS.UnityPlatformIOS());
        //             //             }
        //             //             else if (UnityDriver.IsAndroid)
        //             //             {
        //             //                 UnityDriver.SetDirver(new DeepCore.Unity3D_Android.UnityPlatformAndroid());
        //             //             }
        //             //             else
        //             //             {
        //             //                 UnityDriver.SetDirver(new DeepCore.Unity3D_Win32.UnityPlatformWin32());
        //             //             }
        //         }
        //         public static void SetDirver(IUnityPlatform platformDriver)
        //         {
        //             if (sPlatform is DummyUnityPlatform)
        //             {
        //                 try
        //                 {
        //                    // Type driver = ReflectionUtil.GetType(platformDriver);
        //                     if (driver != null)
        //                     {
        //                         SetDirver((IUnityPlatform)ReflectionUtil.CreateInstance(driver));
        //                         UnityEngine.Debug.Log("- Create Platform Driver : " + platformDriver);
        //                     }
        //                     else
        //                     {
        //                         UnityEngine.Debug.LogWarning("- Can Not Create Platform Driver : " + platformDriver);
        //                     }
        //                 }
        //                 catch (Exception err)
        //                 {
        //                     UnityEngine.Debug.LogError(err.Message + "\n" + err.StackTrace);
        //                 }
        //             }
        //         }
        public static void SetDirver()
        {
            sInstance = new UnityDriver();
            //             if (platform != null)
            //             {
            //                 if (sPlatform is DummyUnityPlatform)
            //                 {
            //                     UnityEngine.Debug.Log("- Set Platform Driver : " + platform);
            //                     sPlatform = platform;
            //                 }
            //             }
        }
        public static void SetUnityDriver(UnityDriver unityDriver)
        {
            sInstance = unityDriver;
        }
        public UnityDriver() : base()
        {
            //Resource.AddLoaderAt(new UnityResourceLoader(Application.dataPath), 0);
            //UnityShaders.InitShaders();
            LoggerFactory.SetFactory(new UnityLoggerFactory());
#if MPQ
            new MPQAdapterFactory();
#endif
        }
        public static bool IsObjectExists(UnityEngine.Object go)
        {
            return go != null && !go.Equals(null);
        }

        public static bool IsWin32
        {
            get
            {
                return Application.platform == RuntimePlatform.WindowsEditor ||
                    Application.platform == RuntimePlatform.WindowsPlayer;
            }
        }
        public static bool IsIOS
        {
            get
            {
                return Application.platform == RuntimePlatform.IPhonePlayer;
            }
        }
        public static bool IsAndroid
        {
            get
            {
                return Application.platform == RuntimePlatform.Android;
            }
        }
        //---------------------------------------------------------------------------------
        public static Process ShowInFolder(FileSystemInfo fs)
        {
            if (fs is FileInfo file)
            {
                return System.Diagnostics.Process.Start("explorer.exe", $"/select, {file.FullName}");
            }
            else if (fs is DirectoryInfo dir)
            {
                return System.Diagnostics.Process.Start("explorer.exe", $"{dir.FullName}");
            }

            return null;
        }
        //---------------------------------------------------------------------------------

#if HZUI
        public class DummyUnityPlatform : IUnityPlatform
        {
            public bool IsNativeUnzip { get { return false; } }

            public Texture2D SysFontTexture(string text, bool readable, TextFontStyle style, float fontSize, uint fontColor, TextBorderStyle borderTime, uint borderColor, Vector2 expectSize, out int boundW, out int boundH)
            {
                boundW = 8;
                boundH = 8;
                return new Texture2D(8, 8, TextureFormat.ARGB32, false, true);
            }
            public bool TestTextLineBreak(string text, float size, TextFontStyle style, TextBorderStyle borderTime, float testWidth, out float realWidth, out float realHeight)
            {
                realWidth = 8;
                realHeight = 8;
                return false;
            }
            public void CopyPixels(Texture2D src, int sx, int sy, int sw, int sh, Texture2D dst, int dx, int dy) { }


            public long GetAvaliableSpace(string path) { return long.MaxValue; }
            public long GetTotalSpace(string path) { return long.MaxValue; }
            //             public virtual bool NativeDecompressFile(MPQUpdater updater, MPQUpdater.RemoteFileInfo zip_file, MPQUpdater.RemoteFileInfo mpq_file, AtomicLong current_unzip_bytes)
            //             {
            //                 // return SharpZipLib.Unzip.SharpZipLib_RunUnzipMPQ(updater, zip_file, mpq_file, current_unzip_bytes);
            //                 return false;
            //             }
            //             public virtual bool NativeDecompressMemory(ArraySegment<byte> src, ArraySegment<byte> dst)
            //             {
            //                 //  return SharpZipLib.Unzip.SharpZipLib_DecompressZ(src, dst);
            //                 return false;
            //             }

        }
#endif
    }


}

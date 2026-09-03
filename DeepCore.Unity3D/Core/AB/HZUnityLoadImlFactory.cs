
namespace DeepCore.Unity3D
{
    public class HZUnityLoadImlFactory
    {

        private static HZUnityLoadImlFactory mInstance = null;

        public static void  SetFactory(HZUnityLoadImlFactory factory)
        {
            mInstance = factory;
        }
        protected HZUnityLoadImlFactory()
        {
            mInstance = this;
        }

        public static HZUnityLoadImlFactory GetInstance()
        {
            if(mInstance == null)
            {
                new HZUnityLoadImlFactory();
            }

            return mInstance;
        }

        public virtual HZUnityLoadIml CreateLoadIml()
        {
            return new HZUnityAssetBundleLoader();
        }
    }
}
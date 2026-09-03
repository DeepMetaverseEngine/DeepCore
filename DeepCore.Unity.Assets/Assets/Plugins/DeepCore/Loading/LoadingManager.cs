using Cysharp.Threading.Tasks;
using DeepCore.Concurrent;
using DeepCore.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepCore.Unity.Loading
{
    public abstract class LoadingManager : MonoBehaviour
    {
        public static LoadingManager Instance { get; private set; }
        private ActionQueue<LoadingManager> tasks;
        private AtomicRangeValue progress;
        private string currentTitle;

        public abstract string LoadingText { get; set; }
        public abstract float LoadingAmount { get; set; }
        public abstract bool IsLoadingActive { get; set; }

        protected virtual void Awake()
        {
            Instance = this;
            this.tasks = new ActionQueue<LoadingManager>();
            this.progress = new AtomicRangeValue();
        }
        protected virtual void Update()
        {
            tasks.ProcessMessages(this);
            if (IsLoadingActive)
            {
                if (progress != null)
                {
                    this.LoadingAmount = progress.Rate;
                    this.LoadingText = $"{currentTitle} {progress.Text}";
                }
                else
                {
                    this.LoadingAmount = 1f;
                    this.LoadingText = $"{currentTitle}";
                }
            }
        }
        public TaskCompletionSource<bool> ShowLoading(string title)
        {
            var tcs = new TaskCompletionSource<bool>();
            this.ShowLoadingAsync(title, new Func<IRangeValue, Task>(p => tcs.Task));
            return tcs;
        }
        public UniTask ShowLoading(string title, Action<IRangeValue> doLoading, Action<IRangeValue> onFinish = null)
        {
            return this.ShowLoadingAsync(title, p =>
            {
                doLoading(p);
                return Task.CompletedTask;
            },
            onFinish);
        }
        public UniTask ShowLoadingAsync(string title, Func<IRangeValue, Task> doLoading, Action<IRangeValue> onFinish = null)
        {
            progress.SetIdentity();
            this.currentTitle = title;
            this.LoadingText = title;
            this.LoadingAmount = 0;
            this.IsLoadingActive = true;
            var tcs = new UniTaskCompletionSource<bool>();
            var t = new Thread(() =>
            {
                try
                {
                    var task = doLoading(progress);
                    task.Wait();
                    progress.SetMax();
                    progress.SetText(string.Empty);
                    tasks.Enqueue((mgr) =>
                    {
                        this.IsLoadingActive = false;
                        this.LoadingText = title;
                        this.LoadingAmount = 1f;
                        tcs.TrySetResult(true);
                        onFinish?.Invoke(progress);
                    });
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                    tasks.Enqueue((mgr) =>
                    {
                        this.IsLoadingActive = false;
                        tcs.TrySetException(err);
                        onFinish?.Invoke(progress);
                    });
                }
            });
            t.Name = "ShowLoading";
            t.IsBackground = true;
            t.Priority = System.Threading.ThreadPriority.BelowNormal;
            t.Start();
            return tcs.Task;
        }
        public async UniTask ShowLoadingAsync(string title, Func<IRangeValue, UniTask> doLoading, Action<IRangeValue> onFinish = null, bool autohideLoading = false)
        {
            progress.SetIdentity();
            this.currentTitle = title;
            this.LoadingText = title;
            this.LoadingAmount = 0;
            this.IsLoadingActive = true;
            try
            {
                await doLoading(progress);
                progress.SetMax();
                progress.SetText(string.Empty);
                this.IsLoadingActive = false;
                this.LoadingText = title;
                this.LoadingAmount = 1f;
            }
            catch (Exception err)
            {
                this.IsLoadingActive = false;
                err.PrintStackTrace();
            }
            finally
            {
                onFinish?.Invoke(progress);
            }
        }

    }

}
#if false
using Cysharp.Threading.Tasks;
using DeepCore.Concurrent;
using DeepCore.Threading;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AOT.Loading
{
    public class UILoadingMgr : MonoBehaviour
    {
        public static UILoadingMgr ins;
        // Start is called before the first frame update
        [SerializeField] private GameObject loadingUI;
        [SerializeField] private TMP_Text text;
        [SerializeField] private ExtendedImage bar;
        [SerializeField] private GameObject headPic;
        private ActionQueue<UILoadingMgr> tasks;
        private AtomicRangeValue progress;

        public Action<string, LoadingType, float,Action> HandleGameLoading;
        public delegate void HandleCloseGameLoading(bool force ,Action callback);
        public  HandleCloseGameLoading  OnHandleCloseGameLoading;
        private LoadingType curLoadingType = LoadingType.None;
        private bool _isLoading;
        public bool IsShowLoading => _isLoading;

        private void Awake()
        {
            ins = this;
            tasks = new ActionQueue<UILoadingMgr>();
            progress = new AtomicRangeValue();
            if (bar)
            {
                barRectTransform = bar.GetComponent<RectTransform>();
            }

            if (headPic)
            {
                headPicRectTransform = headPic.GetComponent<RectTransform>();
            }

            // if (text)
            // {
            //     var material = new Material(text.fontSharedMaterial);
            //     text.fontMaterial = material;
            //     text.fontMaterial.EnableKeyword("UNDERLAY_ON");
            //     text.fontMaterial.SetFloat("_UnderlayOffsetX", 0.3f);  // 水平偏移
            //     text.fontMaterial.SetColor("_UnderlayColor", new Color(128 ,128, 128, 1f));  // 阴影颜色和透明度
            //
            // }
        }
        
        private RectTransform headPicRectTransform;
        private RectTransform barRectTransform;

        private void Update()
        {
            tasks.ProcessMessages(this);
            if (!this.loadingUI || !this.loadingUI.activeSelf)
            {
                return;
            }

            if (curLoadingType == LoadingType.CloudLoading)
            {
                return;
            }
            if (curLoadingType != LoadingType.Init&& HandleGameLoading != null)
            {
                HandleGameLoading.Invoke(progress.Text, curLoadingType, progress.Rate,null);
                return;
            }
            if (this.bar)
            {
                this.bar.fillAmount = progress.Rate;
                UpdateHeadPicPosition();
            }
            if (this.text)
            {
                if (string.IsNullOrEmpty(progress.Text))
                {
                    this.text.text = string.Empty;
                }
                else
                {
                    this.text.text = progress.Text;
                }
            }
        }


        private void OnDestroy()
        {
            _isLoading = false;
            tasks?.Dispose();
        }

        public enum LoadingType
        {
            None,
            Init,//初始化
            // InternalLoading,//内部loading
            FullScreenLoading,//全屏loading
            PVPLoading,//PVP loading
            CloudLoading,//云loading
            
        }
        public void ShowLoading(LoadingType loadingType = LoadingType.Init)
        {   
            addLoadCount();
            progress.SetRange(0, 1, 0);
            progress.SetText(string.Empty);
            if (text)
                this.text.text = string.Empty;
            if (this.bar)
                this.bar.fillAmount = 0;
            curLoadingType = loadingType;
            _isLoading = true;

            switch (loadingType)
            {
                case LoadingType.Init:
                    if (this.loadingUI)
                    {
                        this.loadingUI?.SetActive(true);
                    }

                    break;
                // case LoadingType.InternalLoading:
                default:
                    if (HandleGameLoading != null)
                    {
                        HandleGameLoading?.Invoke("", loadingType, 0,null);
                    }
                    else
                    {
                        if (this.loadingUI)
                            this.loadingUI?.SetActive(true);
                    }
                    break;
            }

        }
        private void UpdateHeadPicPosition()
        {
            if (headPic&& bar )
            {
                // 计算headPic的新位置
                float newX = barRectTransform.rect.width * bar.fillAmount;
                headPicRectTransform.anchoredPosition = new Vector2(newX, headPicRectTransform.anchoredPosition.y);
            }
        }

        private int count = 0;
        public void CloseLoading()
        {
            HideLoading().Forget();
        }

        public void SetLoadType(LoadingType loadingType)
        {
            if (_isLoading && curLoadingType == LoadingType.CloudLoading)
            {
                HideLoading(true).Forget();
            }
            curLoadingType = loadingType;
        }

        public TaskCompletionSource<bool> ShowLoading()
        {
            var tcs = new TaskCompletionSource<bool>();
            this.ShowLoading(p => tcs.Task);
            return tcs;
        }
        public UniTask ShowLoading(Action<IRangeValue> doLoading, Action<IRangeValue> onFinish = null)
        {
            curLoadingType = LoadingType.Init;
            return this.ShowLoading(p => { doLoading(p); return Task.CompletedTask; }, onFinish);
        }

        public UniTask ShowLoading(Func<IRangeValue, Task> doLoading, Action<IRangeValue> onFinish = null)
        {
            addLoadCount();
            progress.SetValue(0);
            _isLoading = true;
            if (this.text)
                this.text.text = string.Empty;
            if (this.bar)
                this.bar.fillAmount = 0;
            if (curLoadingType != LoadingType.Init && HandleGameLoading != null)
            {
                HandleGameLoading.Invoke("", curLoadingType, 0,null);
            }
            else
            {
                if (this.loadingUI)
                    this.loadingUI?.SetActive(true);
            }
            var tcs = new UniTaskCompletionSource<bool>();
            var t = new Thread(() =>
            {
                try
                {
                    var task = doLoading(progress);
                    task.Wait();
                    progress.SetValue(progress.Max);
                    tasks.Enqueue((mgr) =>
                    {
                        if (this.text)
                            this.text.text = string.Empty;
                        if (this.bar)
                            this.bar.fillAmount = 1;
                        tcs.TrySetResult(true);
                        onFinish?.Invoke(progress);
                        if (curLoadingType == LoadingType.Init)
                        {
                            HideLoading().Forget();
                        }
                       
                    });
                }
                catch (Exception err)
                {
                    Debug.LogError(err);
                    tasks.Enqueue((mgr) =>
                    {
                        if (this.text)
                            this.text.text = string.Empty;
                        if (this.bar)
                            this.bar.fillAmount = 1;
                        tcs.TrySetException(err);
                        onFinish?.Invoke(progress);
                        if (curLoadingType == LoadingType.Init)
                        {
                            HideLoading().Forget();
                        }
                    });
                }
               
            });
            t.IsBackground = true;
            t.Priority = System.Threading.ThreadPriority.BelowNormal;
            t.Start();
            return tcs.Task;
        }

        private void addLoadCount()
        {
            if (IsShowLoading)
            {
                count--;
                Debug.Log($"正在加载loading:{count}"+new StackTrace());
            }
            count++;
            Debug.Log($"addLoadCount:{count}"+new StackTrace());
        }

        public async UniTask ShowLoading(Func<IRangeValue, UniTask> doLoading, Action<IRangeValue> onFinish = null,bool autohideLoading = false)
        {
            addLoadCount();
            progress.SetValue(0);
            if (this.text)
                this.text.text = string.Empty;
            if (this.bar)
                this.bar.fillAmount = 0;
            if (curLoadingType != LoadingType.Init && HandleGameLoading != null)
            {
                HandleGameLoading.Invoke("", curLoadingType, 0,null);
            }
            else
            {
                if (this.loadingUI)
                    this.loadingUI?.SetActive(true);
            }
            _isLoading = true;
            await doLoading(progress);
            progress.SetValue(progress.Max);
            if (this.text)
                this.text.text = string.Empty;
            if (this.bar)
                this.bar.fillAmount = 1;
            onFinish?.Invoke(progress);
            if (autohideLoading)
            {//临时黑科技一下
                await HideLoading();
            }
           
        }

        public async UniTask ShowCloudLoading(Action showOver)
        {
            addLoadCount();
            curLoadingType = LoadingType.CloudLoading;
            _isLoading = true;
            HandleGameLoading?.Invoke("", curLoadingType, 0,showOver);
        }

        public async UniTask ShowLoading(Action<IRangeValue> onFinish,bool closeloading ,params Func<IRangeValue, UniTask>[] doLoadings )
        {
            addLoadCount();
            progress.SetValue(0);
            if (this.text)
                this.text.text = string.Empty;
            if (this.bar)
                this.bar.fillAmount = 0;
            if (curLoadingType != LoadingType.Init && HandleGameLoading != null)
            {
                HandleGameLoading.Invoke("", curLoadingType, 0,null);
            }
            else
            {
                if (this.loadingUI)
                    this.loadingUI?.SetActive(true);
            }
            _isLoading = true;
            try
            {
                for (int i = 0; i < doLoadings.Length; i++)
                {
                    await doLoadings[i](progress);
                }
            }
            catch (Exception e)
            {
               Debug.LogError("ShowLoading error:"+e);
               Debug.LogException(e);
            }
            progress.SetValue(progress.Max);
            if (this.text)
                this.text.text = string.Empty;
            if (this.bar)
                this.bar.fillAmount = 1;
            onFinish?.Invoke(progress);
            if (closeloading)
            {
                await HideLoading();
            }
        }
        
        private async UniTask HideLoading(bool Force = false)
        {
            if (!_isLoading)
            {
                return;
            }
            count--;
            Debug.Log($"HideLoading:{count}"+new StackTrace());
            if (count > 0 && !Force)
            {
                return;
            }
            Debug.Log($"loading完成{curLoadingType}");
            
            progress.SetRange(0, 1, 1);
            progress.SetText(string.Empty);
            if (text)
                this.text.text = string.Empty;
            if (this.bar)
                this.bar.fillAmount = 0;
            if (curLoadingType != LoadingType.CloudLoading)
            {
                _isLoading = false;
            }
            if (curLoadingType != LoadingType.Init && OnHandleCloseGameLoading != null)
            {
                OnHandleCloseGameLoading?.Invoke(Force,() =>
                {
                    if (curLoadingType == LoadingType.CloudLoading)
                    {
                        _isLoading = false;
                        curLoadingType = LoadingType.None;
                    }
                });
            }
            else
            {
                //需要等login界面打开
                await UniTask.Delay(1000);
                Debug.Log($"ugui loading关闭");
                if (this.loadingUI)
                {
                    //一次性的 销毁即可
                    DestroyImmediate(this.loadingUI);
                    this.loadingUI = null;
                }
                    
            }
        }
    }


    /*
    public class AtomicRangeValue : IRangeValue
    {
        private long mMin = 0;
        private long mMax = 0;
        private long mValue = 0;
        public bool Break = false;
        private string text;
        public long Min { get { return mMin; } }
        public long Max { get { return mMax; } }
        public long Value { get { return mValue; } }
        public float Rate { get { { return (float)((mMax == mMin) ? 1 : (mValue - mMin) / (double)(mMax - mMin)); } } }
        public string Text { get => text; }
        public AtomicRangeValue() : this(0, 0, 1) { }
        public AtomicRangeValue(long value, long min, long max)
        {
            mMin = Math.Min(min, max);
            mMax = Math.Max(min, max);
            SetValue(value);
        }
        public override string ToString()
        {
            lock (this)
            {
                var p = mValue - mMin;
                var len = mMax - mMin;
                return $"{p}/{len}";
            }
        }
        public string ToStringPercent()
        {
            lock (this)
            {
                var p = mValue - mMin;
                var len = mMax - mMin;
                return $"{(100 * p / len)}%";
            }
        }
        public IRangeValue Reset(long max)
        {
            lock (this)
            {
                mMin = 0;
                mMax = max;
                mValue = 0;
            }
            return this;
        }
        public IRangeValue SetRange(long min, long max, long value)
        {
            lock (this)
            {
                mMin = Math.Min(min, max);
                mMax = Math.Max(min, max);
                mValue = Math.Min(mValue, mMax);
                mValue = Math.Max(mValue, mMin);
            }
            return this;
        }
        public IRangeValue SetMin(long min)
        {
            lock (this)
            {
                if (min != mMin && min <= mMax)
                {
                    mMin = min;
                    mValue = Math.Min(mValue, mMax);
                    mValue = Math.Max(mValue, mMin);
                }
            }
            return this;
        }
        public IRangeValue SetMax(long max, bool autoGenValue = false)
        {
            lock (this)
            {
                if (max != mMax && max >= mMin)
                {
                    if (autoGenValue)
                    {
                        double addrat = (max / (double)mMax) - 1f;
                        mMax = max;
                        Add((int)(mValue * addrat));
                    }
                    else
                    {
                        mMax = max;
                        mValue = Math.Min(mValue, mMax);
                        mValue = Math.Max(mValue, mMin);
                    }
                }
            }
            return this;
        }
        public IRangeValue SetValue(long value)
        {
            lock (this)
            {
                if (value != mValue)
                {
                    mValue = value;
                    return this;
                }
            }
            return this;
        }
        public IRangeValue Add(long add)
        {
            if (add != 0)
            {
                return SetValue(mValue + add);
            }
            return this;
        }
        public IRangeValue SetText(string txt)
        {
            lock (this) { text = txt; }
            Thread.Sleep(1);
            return this;
        }
    }
    */
}

#endif
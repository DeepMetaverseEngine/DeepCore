namespace Code.System.AB
{
    internal abstract class Loader : Reference
    {
        public enum LoaderStatus
        {
            Invalid,
            Inited,
            Started,
            Completed,
        }

        public LoaderStatus Status { get; protected set; }
        public bool IsInLoadingQueue { get; internal set; }
        public bool IsInUnloadingQueue { get; internal set; }
        
        protected override void OnClear()
        {
            Status = LoaderStatus.Invalid;
            IsInLoadingQueue = false;
            IsInUnloadingQueue = false;
        }
    }
}

using DeepCore.Game3D.Slave.Layer;
using DeepCore.Protocol;
using DeepCore.Threading;
using DeepMetaGame.Data.GUI;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Threading.Tasks;

namespace DeepCore.Game3D.Slave.Runtime
{
    public abstract class AbstractBattle : Disposable, ILayerZoneListener
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(AbstractBattle));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        public static int ActiveZoneLayerCount { get { return Alloc.ActiveCount; } }
        public static int AllocZoneLayerCount { get { return Alloc.AllocCount; } }
        //-----------------------------------------------------------------------------------------------------------
        private LayerZone layer;
        private MessageActionQueue<AbstractBattle> task_queue;
        private bool pause = false;
        //-----------------------------------------------------------------------------------------------------------
        public MessageActionQueue<AbstractBattle> TaskQueue { get { return task_queue; } }
        public ZoneSlaveFactory SlaveFactory { get; }

        //-----------------------------------------------------------------------------------------------------------
        public AbstractBattle(EditorTemplates datas, ZoneSlaveFactory slaveFactory)
        {
            Alloc.RecordConstructor(this.GetType());
            this.SlaveFactory = slaveFactory;
            this.DataRoot = datas;
            this.layer = slaveFactory.CreateZoneLayer(datas, this);
            this.task_queue = new MessageActionQueue<AbstractBattle>();
        }
        ~AbstractBattle()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(this.GetType());
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(this.GetType());
        }
        protected override void Disposing()
        {
            this.OnPauseChanged = null;
            this.task_queue.Dispose();
            this.task_queue = null;
            this.layer.Dispose();
            this.layer = null;
            this.DataRoot = null;
        }
        public virtual void LowMemory()
        {
            if (IsDisposing) return;
            layer?.ObjectPool.LowMemory();
        }
        //-----------------------------------------------------------------------------------------------------------
        public EditorTemplates DataRoot { get; private set; }
        public TemplateManager Templates => DataRoot.Templates;
        public LayerZone Layer { get { return layer; } }
        public LayerPlayer Actor { get { return layer != null ? layer.Actor : null; } }
        public int CurrentPing { get { return this.Layer.CurrentPing; } }
        public int NetPing { get { return this.Layer.NetPing; } }
        public abstract bool IsNet { get; }
        public abstract long RecvPackages { get; }
        public abstract long SendPackages { get; }
        public bool Pause
        {
            get => pause;
            set
            {
                if (pause != value)
                {
                    pause = value;
                    OnPauseChanged?.Invoke(this, value);
                }
            }
        }
        public float TimeScale { get; set; } = 1f;


        abstract public void Start();
        public virtual void BeginUpdate(float intervalMS)
        {
            if (!IsDisposing)
            {
                if (!Pause)
                {
                    this.layer.BeginUpdate(intervalMS);
                }
                else
                {
                    this.layer.BeginUpdate(0);
                }
            }
        }
        public virtual void Update()
        {
            this.task_queue.ProcessMessages(this);
            if (!IsDisposing)
            {
                this.layer.Update();
            }
        }
        //-------------------------------------------------------------------
        public abstract bool TryLoadSceneData(ClientEnterScene msg, out SceneData sdata);
        public abstract void SendAction(BattleAction action);
        public virtual void ReleaseMessage(IMessage message)
        {
        }
        //-------------------------------------------------------------------
        public void QueueTask(Action task)
        {
            if (IsDisposing) return;
            task_queue.Enqueue(task);
        }
        public void QueueTask(Func<Task> task)
        {
            if (IsDisposing) return;
            task_queue.Enqueue(() =>
            {
                task();
            });
        }
        public void QueueTask(Action<AbstractBattle> task)
        {
            if (IsDisposing) return;
            task_queue.Enqueue(task);
        }
        public void QueueTask<ST>(ST st, Action<AbstractBattle, ST> task)
        {
            if (IsDisposing) return;
            task_queue.Enqueue(st, task);
        }

        //-------------------------------------------------------------------
        public delegate void BattleStart(AbstractBattle bc);
        public delegate void BattleEnd(AbstractBattle bc);
        public delegate void BattleError(AbstractBattle error, Exception err);
        public delegate void BattlePause(AbstractBattle bc, bool pause);

        public abstract event BattleStart OnStart;
        public abstract event BattleEnd OnEnd;
        public abstract event BattleError OnError;
        public event BattlePause OnPauseChanged;


        //-------------------------------------------------------------------
    }


}

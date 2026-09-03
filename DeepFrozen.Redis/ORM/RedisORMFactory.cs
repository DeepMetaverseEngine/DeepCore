using DeepCore;
using DeepCore.Threading;
using DeepCrystal.Threading;
using DeepFrozen.MySQL;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DeepCrystal.ORM.Redis
{
    public partial class RedisORMFactory : ORMFactory
    {
        //------------------------------------------------------------------------------------------------------------------
        internal readonly ConfigurationOptions mOptions;
        internal readonly int mDefaultDataBaseNum;
        internal readonly ConnectionMultiplexer mMultiplexer;
        internal readonly IServer mServer;
        private RedisMappingAdapter mDefaultAdapter;
        private RedisDatabase mDefaultDatabase;
        private ConcurrentDictionary<string, RedisMappingAdapter> mDatabaseMap = new ConcurrentDictionary<string, RedisMappingAdapter>();
        private RedisConditions mConditions;
        private MySQLConnectPool mMySql;
        //------------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------------------------------------------------------------
        public static RedisORMFactory RedisInstance { get; private set; }
        public string ClientName => mMultiplexer.ClientName;
        public bool IsConnected => mMultiplexer.IsConnected;
        public int TimeoutMilliseconds => mMultiplexer.TimeoutMilliseconds;
        public IServer RedisServer { get => mServer; }
        public MySQLConnectPool MySQL { get => mMySql; }
        public RedisMappingAdapter RedisDefaultAdapter { get => mDefaultAdapter; }
        public override IMappingAdapter DefaultAdapter { get => mDefaultAdapter; }
        public override IMappingDatabase DefaultDatabase { get => mDefaultDatabase; }
        public override IConditions Conditions { get => mConditions; }

        [Obsolete("only use for code generator")]
        public RedisORMFactory()
        {
            mOptions = null;
            mDefaultDataBaseNum = 0;
            mMultiplexer = null;
            mServer = null;
        }
        public RedisORMFactory(string redis_opstr, string mysql_opstr = null)
        {
            var redis_kv = redis_opstr.Split(';');
            var optstr = redis_kv[0];
            var ext = Properties.ParseLines(redis_kv[1].Split(','), PropertiesFormat.Default);
            if (!Parser.TryParseInt(redis_kv[1], out var db))
            {
                if (!ext.TryGetAsInt("db", out db))
                {
                }
            }
            RedisInstance = this;
            mOptions = ConfigurationOptions.Parse(optstr);
            mDefaultDataBaseNum = db;
            mMultiplexer = ConnectionMultiplexer.Connect(mOptions, Console.Out);
            mServer = mMultiplexer.GetServer(mMultiplexer.GetEndPoints()[0]);
            mDefaultAdapter = new RedisMappingAdapter(GetDatabase(mDefaultDataBaseNum.ToString()));
            mConditions = new RedisConditions();
            mDefaultDatabase = mDefaultAdapter.redis_db;
            InitScripts();
            if (!string.IsNullOrEmpty(mysql_opstr))
            {
                //string MySQLConnectString = "server=localhost;User ID=root;Password=121121;database=orm;";
                this.mMySql = new MySQLConnectPool(mysql_opstr);
                RedisDump.MySQLInit(RedisDefaultAdapter, mMySql);
            }
            //StackExchange.Redis.ConnectionPool.PooledConnectionMultiplexer;
        }
        protected override void Disposing()
        {
            DisposingScripts();
            mDatabaseMap.Clear();
            try { mMultiplexer?.Dispose(); }
            catch (Exception err) { log.Error(err); }
            mConditions = null;
            mDefaultAdapter = null;
            mDatabaseMap = null;
        }
        //------------------------------------------------------------------------------------------------------------------
        public override object DecodeObject(IConvertible obj, Type type)
        {
            if (obj == null) return null;
            return RedisConverters.ToObject((RedisValue)obj, type);
        }
        public override IConvertible EncodeObject(object obj, Type type)
        {
            return RedisConverters.ToRedisValue(obj, type);
        }
        //------------------------------------------------------------------------------------------------------------------
        private IDatabase GetDatabase(string db)
        {
            int number = mDefaultDataBaseNum;
            if (db != null)
            {
                Parser.TryParseInt(db, out number);
            }
            var redis_db = this.mMultiplexer.GetDatabase(number);
            return redis_db;
        }
        public override IMappingAdapter GetAdapter(string db)
        {
            return mDatabaseMap.GetOrAdd(db, (k) => new RedisMappingAdapter(GetDatabase(k)));
        }
        public override IMappingDatabase CreateDatabase(string db)
        {
            return mDatabaseMap.GetOrAdd(db, (k) => new RedisMappingAdapter(GetDatabase(k))).redis_db;
        }
        public override ITransactionDatabase CreateTransaction(IMappingDatabase adapter)
        {
            var ad = adapter as RedisDatabase;
            var tran = (ad.db_sync as IDatabase).CreateBatch();
            return new RedisBatchDatabase(ad, tran);
        }
        public override ITransactionDatabase CreateTransaction(IMappingDatabase adapter, ICondition condition)
        {
            var ad = adapter as RedisDatabase;
            var tran = (ad.db_sync as IDatabase).CreateTransaction();
            var cond = ((RedisCondition)condition).cond;
            tran.AddCondition(cond);
            return new RedisTransactionDatabase(ad, tran);
        }
        public override ITransactionDatabase CreateTransaction(IMappingDatabase adapter, ICondition[] conditions)
        {
            var ad = adapter as RedisDatabase;
            var tran = (ad.db_sync as IDatabase).CreateTransaction();
            foreach (var c in conditions)
            {
                var cond = ((RedisCondition)c).cond;
                tran.AddCondition(cond);
            }
            return new RedisTransactionDatabase(ad, tran);
        }
        //------------------------------------------------------------------------------------------------------------------
        public override IChannel GetChannel(string channel, ITaskExecutor exe = null)
        {
            return new RedisChannel(channel, mMultiplexer.GetSubscriber(), exe);
        }
        //------------------------------------------------------------------------------------------------------------------
        private LoadedLuaScript ObjectBatchQueryScript;
        private void InitScripts()
        {
            var prepared = LuaScript.Prepare(RedisORMFactory.BATCH_QUERY_OBJECT_SCRIPT);
            this.ObjectBatchQueryScript = prepared.Load(RedisORMFactory.RedisInstance.RedisServer);
        }
        private void DisposingScripts()
        {
        }
        public Task<RedisResult> EvaluateObjectBatchQueryScriptAsync(IDatabase db, string key)
        {
            return ObjectBatchQueryScript.EvaluateAsync(db, new { key = key });
            //             var prepared = LuaScript.Prepare(RedisORMFactory.BATCH_QUERY_OBJECT_SCRIPT);
            //             var result = await db.ScriptEvaluateAsync(prepared, new { key = key });
            //             return result;
        }
        public static string BATCH_QUERY_OBJECT_SCRIPT = @"
local function ToList(list, key)
    list[#list + 1] = '-'
    list[#list + 1] = key
    local all = redis.call('HGETALL', key)
    for j=1,#all do
        list[#list + 1] = all[j]
    end
    for j=1,#all,2 do
        if all[j]:sub(1,1) == '*' then
            ToList(list, all[j+1])
        end
    end
end

local ret = { }
ToList(ret, @key)
return ret
";
    }

}

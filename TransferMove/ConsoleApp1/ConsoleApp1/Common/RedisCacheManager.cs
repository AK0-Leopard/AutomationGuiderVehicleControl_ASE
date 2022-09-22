
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Common
{
    public class RedisCacheManager
    {
        public bool IsConnection { get { return GetConnection().IsConnected; } }
        static string productID = null;
        static string REDIS_KEY_WORK_REDIS_USING_COUNT = "REDIS_USING_COUNT";
        static readonly string KEY_WORD_REDIS_CONTEXT = "redis_context";


        private static Lazy<ConfigurationOptions> configOptions
                    = new Lazy<ConfigurationOptions>(() =>
                    {
                        var configOptions = new ConfigurationOptions();
                        configOptions.ClientName = "OHxCRedisConnection";
                        configOptions.ConnectTimeout = 100000;
                        configOptions.SyncTimeout = 100000;
                        configOptions.AbortOnConnectFail = false;
                        return configOptions;
                    });

        private static Lazy<ConnectionMultiplexer> conn
                    = new Lazy<ConnectionMultiplexer>(
                        () => ConnectionMultiplexer.Connect(configOptions.Value));

        int DBConnectionNum = -1;
        public RedisCacheManager(string product_id, int db_connection_num = -1)
        {
            DBConnectionNum = db_connection_num;
            productID = product_id;
            REDIS_KEY_WORK_REDIS_USING_COUNT = $"{productID}_{REDIS_KEY_WORK_REDIS_USING_COUNT}";
            GetConnection().ConnectionFailed += RedisCacheManager_ConnectionFailed;
            GetConnection().ConnectionRestored += RedisCacheManager_ConnectionRestored;
        }

        private void RedisCacheManager_ConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            Console.WriteLine("ConnectionRestored");
        }

        private void RedisCacheManager_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            Console.WriteLine($"Redis ConnectionFailed.ConnectionType[{ e.ConnectionType}],FailureType[{e.FailureType}]");

        }

        object _lock = new object();
        bool redisConnectionValid = true;

        private ConnectionMultiplexer GetConnection()
        {
            return conn.Value;
            //if (RedisConnection != null && RedisConnection.IsConnected) return RedisConnection;

            //lock (_lock)
            //{
            //    if (RedisConnection != null && RedisConnection.IsConnected) return RedisConnection;

            //    if (RedisConnection != null)
            //    {
            //        logger.Warn("Redis connection disconnected. Disposing connection...");
            //        RedisConnection.Dispose();
            //    }

            //    logger.Warn("Creating new instance of Redis Connection");
            //    var options = ConfigurationOptions.Parse(SCAppConstants.ConnectionSetting.REDIS_SERVER_CONFIGURATION);
            //    RedisConnection = ConnectionMultiplexer.Connect(options);
            //}
            //return RedisConnection;
        }

        private IServer GetServer()
        {
            var conn = GetConnection();
            var end_point = conn.GetEndPoints()[0];
            return conn.GetServer(end_point);
        }

        public IDatabase Database()
        {
            try
            {
                int db_index = -1;
                //#if DEBUG
                //                db_index = 1;
                //#endif

                return !redisConnectionValid ? null : GetConnection().GetDatabase(DBConnectionNum);
            }
            catch (Exception ex)
            {
                redisConnectionValid = false;
                Console.WriteLine($"Unable to create Redis connection: {ex.Message}");
                return null;
            }
        }

        public ISubscriber Subscriber()
        {
            try
            {
                return !redisConnectionValid ? null : GetConnection().GetSubscriber();
            }
            catch (Exception ex)
            {
                redisConnectionValid = false;
                Console.WriteLine($"Unable to create Redis connection: {ex.Message}");
                return null;
            }
        }


        public void CloseRedisConnection()
        {
            GetConnection().Close();
        }

        public bool KeyExists(string key)
        {
            IDatabase db = Database();
            if (db == null) return false;
            UsingCount();
            key = $"{productID}_{key}";
            return db.KeyExists(key);
        }
        public bool KeyDelete(string key, bool isNeedAddProduct = true)
        {
            IDatabase db = Database();
            if (db == null) return false;
            UsingCount();
            if (isNeedAddProduct)
                key = $"{productID}_{key}";
            return db.KeyDelete(key);
        }
        public long KeyDelete(RedisKey[] keys)
        {
            IDatabase db = Database();
            if (db == null) return -1;
            UsingCount();
            return db.KeyDelete(keys);
        }


        public bool stringSetAsync(string key, RedisValue set_object, TimeSpan? timeOut = null, When when = When.Always)
        {
            IDatabase db = Database();
            if (db == null) return false;
            key = $"{productID}_{key}";
            db.StringSetAsync(key, set_object, timeOut, when);
            UsingCount();
            return true;
        }
        public bool stringSet(string key, RedisValue set_object, TimeSpan? timeOut = null, When when = When.Always)
        {
            bool is_success = false;
            IDatabase db = Database();
            if (db == null) return false;
            key = $"{productID}_{key}";
            is_success = db.StringSet(key, set_object, timeOut, when);
            UsingCount();
            return is_success;
        }

        public bool StringIncrementAsync(string key, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            IDatabase db = Database();
            if (db == null) return false;
            key = $"{productID}_{key}";
            db.StringIncrementAsync(key, value, flags);
            return true;
        }

        public bool Obj2ByteArraySetAsync(string key, byte[] set_byteArray, TimeSpan? timeOut = null)
        {
            IDatabase db = Database();
            if (db == null) return false;
            key = $"{productID}_{key}";
            db.StringSetAsync(key, set_byteArray, timeOut);
            UsingCount();
            return true;
        }

        public IEnumerable<RedisKey> SearchKey(string pattern)
        {
            IServer server = GetServer();
            if (server == null) return null;
            pattern = $"{productID}_{pattern}";
            var keylist = server.Keys(database: Database().Database, pattern: pattern);
            return keylist;
        }

        public RedisValue StringGet(string key, bool isNeedAddProduct = true)
        {
            IDatabase db = Database();
            if (db == null) return string.Empty;
            UsingCount();
            if (isNeedAddProduct)
                key = $"{productID}_{key}";
            var value = db.StringGet(key);
            if (!value.HasValue)
            {
                // logger.Warn($"redis key not exist:{key}");
                return string.Empty;
            }
            return value;
        }

        public RedisValue[] StringGet(RedisKey[] keys)
        {
            IDatabase db = Database();
            if (db == null) return null;
            UsingCount();
            var value = db.StringGet(keys);
            return value;
        }

        public long ListLength(string list_key)
        {
            IDatabase db = Database();
            if (db == null) return 0;
            UsingCount();
            list_key = $"{productID}_{list_key}";
            return db.ListLength(list_key);
        }

        public void ListRightPush(string list_key, IEnumerable<string> values, TimeSpan? timeSpan = null)
        {
            IDatabase db = Database();
            if (db == null) return;
            StackExchange.Redis.RedisValue[] redisArray = values.Select(v => (RedisValue)v).ToArray();
            list_key = $"{productID}_{list_key}";
            Task taskSand = db.ListRightPushAsync(list_key, redisArray);
            if (timeSpan != null)
            {
                db.KeyExpireAsync(list_key, timeSpan);
            }
            UsingCount();
        }

        public List<string> ListRange(string list_key)
        {
            IDatabase db = Database();
            if (db == null) new List<string>();
            list_key = $"{productID}_{list_key}";
            RedisValue[] redisValues = db.ListRangeAsync(list_key).Result;
            List<string> values = redisValues.Select(vs => (string)vs).ToList();
            UsingCount();
            return values;
        }

        public RedisValue ListLeftPopAsync(string list_key)
        {
            IDatabase db = Database();
            if (db == null) return default(RedisValue);
            list_key = $"{productID}_{list_key}";
            Task<RedisValue> taskSand = db.ListLeftPopAsync(list_key);
            UsingCount();
            return taskSand.Result;
            //taskSand.Wait();
        }

        public void ListSetByIndexAsync(string list_key, string vh_id, string value)
        {
            IDatabase db = Database();
            if (db == null) return;
            int vh_num = 0;
            if (int.TryParse(vh_id.Substring(vh_id.Length - 2), out vh_num))
            {
                list_key = $"{productID}_{list_key}";
                Task taskSand = db.ListSetByIndexAsync(list_key, vh_num - 1, value);
                UsingCount();
                //taskSand.Wait();
            }
        }

        public void UsingCount()
        {
            IDatabase db = Database();
            if (db == null) return;

            db.StringIncrementAsync(REDIS_KEY_WORK_REDIS_USING_COUNT);
        }

        public void StringIncrementAsync(string key)
        {
            IDatabase db = Database();
            if (db == null) return;
            key = $"{productID}_{key}";
            db.StringIncrementAsync(key);
        }
        public void StringDecrementAsync(string key)
        {
            IDatabase db = Database();
            if (db == null) return;
            key = $"{productID}_{key}";
            db.StringDecrementAsync(key);
        }

        public void PublishEvent(string key, byte[] set_byteArray)
        {
            ISubscriber sub = Subscriber();
            if (sub != null)
            {
                key = $"{productID}_{key}";
                sub.Publish(key, set_byteArray);
            }
        }

        public void SubscriptionEvent(string subscription_key, Action<RedisChannel, RedisValue> action)
        {
            ISubscriber sub = Subscriber();
            if (sub != null)
            {
                subscription_key = $"{productID}_{subscription_key}";
                sub.Subscribe(subscription_key, action);
            }
        }
        public void UnsubscribeEvent(string subscription_key, Action<RedisChannel, RedisValue> action)
        {
            ISubscriber sub = Subscriber();
            if (sub != null)
            {
                subscription_key = $"{productID}_{subscription_key}";
                sub.Unsubscribe(subscription_key, action);
            }
        }



        public bool HashExists(RedisKey key, RedisValue hashField)
        {
            IDatabase db = Database();
            if (db == null) return false;
            key = $"{productID}_{key}";
            db.HashExists(key, hashField);
            UsingCount();
            return true;
        }

        public Task<RedisValue[]> HashValuesAsync(string key)
        {
            IDatabase db = Database();
            if (db == null) return null;
            var value = db.HashValuesAsync(key);
            UsingCount();
            return value;
        }
        public Task<RedisValue[]> HashValuesProductOnlyAsync(string key)
        {
            IDatabase db = Database();
            if (db == null) return null;
            key = $"{productID}_{key}";
            var value = db.HashValuesAsync(key);
            UsingCount();
            return value;
        }
        public RedisValue[] HashKeys(string key)
        {
            IDatabase db = Database();
            if (db == null) return null;
            key = $"{productID}_{key}";
            var value = db.HashKeys(key);
            UsingCount();
            return value;
        }


    }
}

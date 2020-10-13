using System;
using System.Diagnostics;
using System.Threading;
using StackExchange.Redis;

namespace RealtimeOnlineUser
{
    class Program
    {
        static void Main(string[] args)
        {
            var redis = new RedisHelper("localhost:6379", "", 15);
            new Program(redis.GetDatabase()).TestOnlineUser();
        }

        private IDatabase _db;
        private string _key = "online";

        public Program(IDatabase db)
        {
            this._db = db;
        }

        public void TestOnlineUser()
        {
            Console.WriteLine("------ TestOnlineUser ------");

            Console.WriteLine("Add user per 10s.");
            var tokens = new string[] { "1", "2", "3", "4", "5" };
            foreach (var token in tokens)
            {
                Thread.Sleep(10 * 1000); // 每10s添加一用户
                Console.WriteLine($"{token} Logined.");
                Use(token);
            }

            Console.WriteLine("Waiting for 35s.");
            Thread.Sleep(35 * 1000);
            Console.WriteLine("Clean overdue user.");
            Clean();

            var count = _db.SortedSetLength(_key);
            Console.WriteLine($"Online user count: {count}.");

            Debug.Assert(count == 3);
        }

        private void Use(string token)
        {
            long timestamp = GetCurrentMilliseconds() + 60 * 1000; // 缓存60s

            _db.SortedSetAdd(_key, token, timestamp);
        }

        private void Clean()
        {
            // 清空过期的用户
            long timestamp = GetCurrentMilliseconds();
            _db.SortedSetRemoveRangeByScore(_key, 0, timestamp);
        }

        private long GetCurrentMilliseconds()
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区
            long timestamp = (long) (DateTime.Now - startTime).TotalMilliseconds; // 相差秒数
            return timestamp;
        }
    }
}
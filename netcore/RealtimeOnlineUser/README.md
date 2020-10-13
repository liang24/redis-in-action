### 前言

本系列教程是在学习《Redis实战》同时，利用 Redis 解决实际的业务问题。

### 问题

项目里有一个功能是实时数据看板，其中有一项数据是实时在线用户数。

### 解决方案

常见的解决方案有三种：

1. 列表
2. 数据库
3. Redis

#### 列表

使用编程语言里的列表，比如 `C#` 的 `List` 或者 `Java` 的 `ArrayList`，保存到内存里。

- 好处：读写快，访问内存快。
- 坏处：程序内无法共享，比如在 api  层记录用户状态，在后台显示数据。

#### 数据库

把用户的状态保存到数据库里，读取时也从数据库查询。

- 好处：连接上数据库的应用都能访问。
- 坏处：并发高时，数据库会出现性能问题。

#### Redis

以上两种方法，都存在各自的问题，而 `Redis`  有以上两种方法的好处，没有它们的坏处。

`Redis` 基于内存进行读写的，性能比数据库要高很多，支持分布式访问，其他应用都能够使用。

解决步骤：

1. 记录用户状态（api）。使用**有序列表**保存用户状态，并且设置一个过期时间作为 `Score`。
2. 获取实时在线用户数（api）。直接通过**有序列表**获取长度，例如 `StackExchange.Redis` 里的 `SortedSetLength`。
3. 定时清理过期的在线用户（后台服务）。例如 `StackExchange.Redis` 里的 `SortedSetRemoveRangeByScore`。

```C#
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
                SetToken(token);
            }

            Console.WriteLine("Waiting for 35s.");
            Thread.Sleep(35 * 1000);
            Console.WriteLine("Clean overdue user.");
            Clean();

            var count = _db.SortedSetLength(_key);
            Console.WriteLine($"Online user count: {count}.");

            Debug.Assert(count == 3);
        }

        private void SetToken(string token)
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
```

### 总结

上面通过 `Redis` 来实现`实时在线用户数`功能。其他类似的数据也可以通过 `Redis` 来实现，比如最多访问URL top10、用户区域分布等等。
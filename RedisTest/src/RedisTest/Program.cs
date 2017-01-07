using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;


namespace RedisTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ConnectionMultiplexer redis1 = ConnectionMultiplexer.Connect("192.168.4.201:7000,192.168.4.211:7000");
            IDatabase db1 = redis1.GetDatabase();
            String value1 = "redis1";
            db1.StringSet("rediskey1", value1);
            Console.WriteLine(db1.StringGet("rediskey1"));

            ConnectionMultiplexer redis2 = ConnectionMultiplexer.Connect("192.168.4.202:7000,192.168.4.212:7000");
            IDatabase db2 = redis2.GetDatabase();
            String value2 = "redis2";
            db2.StringSet("rediskey1", value2);
            Console.WriteLine(db1.StringGet("rediskey1"));

            ConnectionMultiplexer redis3 = ConnectionMultiplexer.Connect("192.168.4.203:7000,192.168.4.213:7000");
            IDatabase db3 = redis1.GetDatabase();
            String value3 = "redis3";
            db3.StringSet("rediskey1", value3);
            Console.WriteLine(db1.StringGet("rediskey1"));
        }
    }
}

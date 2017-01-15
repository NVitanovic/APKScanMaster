using System;
using System.Threading;
using StackExchange.Redis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using APKScanSharedClasses;
using System.Net.Http;

namespace main
{
    public class Program
    {
        public static String INVALID_APK = "0 error";
        public static Config config = configuration("config.json");
        public static void Main(string[] args)
        {
            Console.WriteLine("STARTED VERSION: 26");
            Config config = configuration("config.json");
            ADBLibrary.ADBClient.logcatTimeout = int.Parse(config.android_vm_wait_time);
            Console.WriteLine("pre sendEmail");
            Thread t = new Thread(() =>
            {
                EmailNotify.SendEmail(config, "Program started at " + DateTime.Now);
            });
            t.Start();
            Console.WriteLine("posle sendEmail");

            try{
                connectToAllAndroidVM();
            }
            catch (Exception e){
                Console.WriteLine(e.StackTrace);
                Thread tt = new Thread(() =>
                {
                    EmailNotify.SendEmail(config,"StackTrace:\n" + e.StackTrace + "\n\nData:\n" + e.Data + "\n\nMessage:\n" + e.Message + "\n\nSource:\n" + e.Source + "\n\nInnerException:\n" + e.InnerException);
                });
                tt.Start();
            }

            try
            {
                ConnectionMultiplexer redis1 = ConnectionMultiplexer.Connect("192.168.4.201:7000,192.168.4.202:7000,192.168.4.203:7000");
                IDatabase db1 = redis1.GetDatabase();
                ISubscriber sub = redis1.GetSubscriber();
                redisSubscribe(db1, sub);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Thread tt = new Thread(() =>
                {
                    EmailNotify.SendEmail(config, "StackTrace:\n" + e.StackTrace + "\n\nData:\n" + e.Data + "\n\nMessage:\n" + e.Message + "\n\nSource:\n" + e.Source + "\n\nInnerException:\n" + e.InnerException);
                });
                tt.Start();
            }
            //downloadFile("http://www.cigani.xyz/1/", "vpn.jpg", ".jpg", "TESTDL2");
            //downloadFile("http://www.cdfgdfgdfgdfgdfgdfgdfgdgi.xyz/1/", "vpn.jpg", ".jpg", "TESTDL2");
            //downloadFile("http://www.cigani.xyz/1", "vpn.jpg", ".jpg", "TESTDL2");

            Console.WriteLine("END MAIN");
            Console.ReadLine();
        }

        public static void redisSubscribe(IDatabase db1, ISubscriber sub)
        {
            Console.WriteLine("thread redisSubscribe started");
            //Thread t = new Thread(()  => {
                while (true)
                {
                    sub.Subscribe("send", (channel, message) =>
                    {
                    string work = db1.ListRightPop("send");
                        if (work != null)
                        {
                            Console.WriteLine((string)work);
                            try
                            {
                                RedisSend data = JsonConvert.DeserializeObject<RedisSend>(work);
                                Console.WriteLine("upload IP je " + data.upload_ip);
                                downloadFile(config.download_server, data.hash, ".apk" ,config.download_location);//super 1337 hax to find file extension
                                //check if file is really .apk
                                ADBLibrary.ADBClient.clearLogcat(config.android_vm[1]);
                                if (ADBLibrary.ADBClient.installApk(config.android_vm[1], config.download_location + data.hash + ".apk"))
                                {
                                    RedisReceive result = new RedisReceive();
                                    Dictionary<String, String> results = ADBLibrary.ADBClient.parseLogcat(config.android_vm[1],config.android_vm_antivirus_keywords.ToArray());
                                    
                                    for (int i = 0; i < results.Count; i++)
                                    {
                                        Console.WriteLine("[" + config.android_vm_antivirus_app[i] + "] says that file is a virus " + results[config.android_vm_antivirus_keywords[i]]);
                                        result.av_results.Add(config.android_vm_antivirus_app[i], results[config.android_vm_antivirus_keywords[i]]);
                                    }

                                    result.master_id = "master1";
                                    result.hash = data.hash;
                                    result.upload_date = data.upload_date;
                                    result.upload_ip = data.upload_ip;
                                    result.filename = data.filename;
                                    Console.WriteLine(db1.ListLeftPush("receive", JsonConvert.SerializeObject(result), flags: CommandFlags.None));
                                    Console.WriteLine(sub.Publish("receive", "x"));
                                    Console.WriteLine("Returning to 'receive' redis queue:\n" + JsonConvert.SerializeObject(result));
                                }
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine(e.StackTrace);
                                Thread t = new Thread(() =>
                                {
                                    EmailNotify.SendEmail(config, "StackTrace:\n" + e.StackTrace + "\n\nData:\n" + e.Data + "\n\nMessage:\n" + e.Message + "\n\nSource:\n" + e.Source + "\n\nInnerException:\n" + e.InnerException);
                                });
                                t.Start();
                                
                            }
                            Console.WriteLine("***********************************\n\n");
                        }
                    });
                    Thread.Sleep(100);
                }
            //});
            //t.Start();
        }

        public static Config configuration(String path)
        {
            string jsonFromFile = File.ReadAllText(path);

            Config config = JsonConvert.DeserializeObject<Config>(jsonFromFile);
            /*
            dynamic parsedJsonObject = JObject.Parse(jsonFromFile);
            var redis = parsedJsonObject.redis;
            var masters = redis.masters;
            var slaves = redis.slaves;
            var android_vm = parsedJsonObject.android_vm;
            var download_server = parsedJsonObject.download_server;
            var android_vm_wait_time = parsedJsonObject.android_vm_wait_time;
            var download_location = parsedJsonObject.download_location;
            var logcat_timeout = parsedJsonObject.logcat_timeout;
            var android_vm_antivirus_keywords = parsedJsonObject.android_vm_antivirus_keywords;


            config.masters = masters.ToObject<List<string>>();
            config.slaves = slaves.ToObject<List<string>>();
            config.android_vm = android_vm.ToObject<List<string>>();
            config.android_vm_wait_time = android_vm_wait_time.ToObject<String>();
            config.download_server = download_server.ToObject<String>();
            config.download_location = download_location.ToObject<String>();
            config.logcat_timeout = logcat_timeout.ToObject<String>();
            config.android_vm_antivirus_keywords = android_vm_antivirus_keywords.ToObject<List<string>>();
            */
            return config;
        }

        public static void connectToAllAndroidVM()
        {
            foreach(String ip in config.android_vm)
            {
                ADBLibrary.ADBClient.connectToDevice(ip);
            }
        }

        public static bool downloadFile(String uri, String fileName, String fileExtension, String path)
        {
            try
            {
                Console.WriteLine("downloadFile: started");
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(uri);
                client.Timeout = TimeSpan.FromMinutes(5);
                string requestUrl = uri + fileName;

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);


                var sendTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                var response = sendTask.Result;
                if (response.Content.Headers.ContentLength == null)
                {
                    return false;
                }
                else
                {
                    var httpStream = response.Content.ReadAsStreamAsync();

                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    var pathOnDisk = Path.Combine(path, fileName + fileExtension);

                    var fileStream = File.Create(pathOnDisk);
                    var reader = new StreamReader(httpStream.Result);

                    httpStream.Result.CopyTo(fileStream);
                    fileStream.Flush();
                    fileStream.Dispose();
                    Console.WriteLine("downloadFile: ended");
                    return true;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Thread t = new Thread(() =>
                {
                    EmailNotify.SendEmail(config, "StackTrace:\n" + e.StackTrace + "\n\nData:\n" + e.Data + "\n\nMessage:\n" + e.Message + "\n\nSource:\n" + e.Source + "\n\nInnerException:\n" + e.InnerException);
                });
                return false;
            }
           
            

        }
    }
}

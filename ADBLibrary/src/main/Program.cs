using System;
using System.Threading;
using StackExchange.Redis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using APKScanSharedClasses;
using System.Net.Http;
using System.Diagnostics;

namespace main
{
    public class Program
    {
        public static String INVALID_APK = "File doesn't have package!";
        public static Config config = configuration("config.json");
        public static bool[] androidVMavailable;
        public static int[] androidVMtimeWhenAvailable;
        public static RedisSend data;
        public static int vmPosition=0;
        public static bool zauzet = false;
        public static int brojReq = 0;
        public static List<string> queue;
        public static void Main(string[] args)
        {
            Console.WriteLine("STARTED VERSION: 32");
            Config config = configuration("config.json");

            androidVMavailable = new bool[config.android_vm.Count];
            androidVMtimeWhenAvailable = new int[config.android_vm.Count];
            for (int i = 0; i < config.android_vm.Count; i++)
            {
                androidVMavailable[i] = true;
                androidVMtimeWhenAvailable[i] = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            }

            ADBLibrary.ADBClient.logcatTimeout = int.Parse(config.logcat_wait);
            
            Thread t = new Thread(() =>
            {
                //EmailNotify.SendEmail(config, "Program started at " + DateTime.Now);
            });
            t.Start();
            
            try{
                //connectToAllAndroidVM();
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
                ConnectionMultiplexer redis1 = ConnectionMultiplexer.Connect("localhost");
                //ConnectionMultiplexer redis1 = ConnectionMultiplexer.Connect("192.168.4.201:7000,192.168.4.202:7000,192.168.4.203:7000");
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
            String packageName;
            writeLineColored("\nthread redisSubscribe started", ConsoleColor.Magenta);
            //Thread t = new Thread(()  => {
                //while (true)
                //{
                
                sub.Subscribe("send", (channel, message) =>
                    {
                        while (true)
                        {
                            //Console.WriteLine("Desio se publish. Zauzet je ");
                            //writeLineColored(zauzet.ToString(), ConsoleColor.Cyan);
                            if (!zauzet)
                            {
                                if (brojReq >= config.android_vm.Count)
                                {
                                    zauzet = true;
                                    writeLineColored("Zauzeto.", ConsoleColor.Red);
                                    continue;
                                }
                                string work = db1.ListRightPop("send");
                                
                                if (work != null)
                                {
                                    brojReq++;
                                    Console.WriteLine((string)work);
                                    Console.WriteLine("brojReq nakon pop je " + brojReq);
                                    try
                                    {
                                        data = JsonConvert.DeserializeObject<RedisSend>(work);
                                        //if (data != null)
                                            //brojReq++;
                                        
                                        //downloadFile(config.download_server, data.hash, ".apk" ,config.download_location);
                                        //packageName = ADBLibrary.ADBClient.getPackageNameFromApk(config.download_location + data.hash + ".apk");
                                        packageName = "com.google";
                                        if (packageName == INVALID_APK)
                                        {
                                            Console.WriteLine("Invalid .apk");
                                            Thread tt = new Thread(() =>
                                            {
                                                EmailNotify.SendEmail(config, "User: " + data.upload_ip + "\nat: " + data.upload_date + "\ntried to upload invalid file: " + data.hash);
                                            });
                                            tt.Start();
                                        }
                                        else
                                        {
                                            Console.WriteLine("x");
                                            String currentVM;

                                            //for (int i = 0; i < config.android_vm.Count; i++)
                                            //{
                                            writeLineColored("\n==============================", ConsoleColor.DarkCyan);
                                            Console.WriteLine("vmPosition=" + vmPosition);
                                            if (androidVMavailable[vmPosition])
                                            {
                                                androidVMavailable[vmPosition] = false;
                                                //connectToAllAndroidVM(); //after reset/snapshot master needs to ADB connect to all devices again                                           
                                                currentVM = config.android_vm[vmPosition];
                                                Console.WriteLine(vmPosition + " is available " + config.android_vm[vmPosition]);
                                                Console.WriteLine(vmPosition + " started processing apk in " + config.android_vm[vmPosition]);
                                                Thread processApk = new Thread(() =>
                                                {
                                                    vmPosition++;
                                                    if (vmPosition == config.android_vm.Count)
                                                    {
                                                        vmPosition = 0;
                                                    }
                                                    processApkInVM(db1, sub, data.hash, packageName, currentVM, vmPosition == 0 ? 0 : vmPosition - 1);
                                                    brojReq--;
                                                    Console.WriteLine("br requesta nakon pozivanja processAPK " + brojReq);
                                                    if (brojReq < config.android_vm.Count)
                                                    {
                                                        zauzet = false;
                                                    }
                                                    Console.WriteLine("***********************************\n\n");
                                                });
                                                processApk.Start();

                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.StackTrace);
                                        Thread t = new Thread(() =>
                                        {
                                            EmailNotify.SendEmail(config, "StackTrace:\n" + e.StackTrace + "\n\nData:\n" + e.Data + "\n\nMessage:\n" + e.Message + "\n\nSource:\n" + e.Source + "\n\nInnerException:\n" + e.InnerException);
                                        });
                                        t.Start();
                                    }
                                }
                            }
                        }
                    });
                    //Thread.Sleep(1000);
                //}
            //});
            //t.Start();
        }

        private static void processApkInVM(IDatabase db1, ISubscriber sub, string hash, string packageName, string currentVM, int i)
        {
            Console.WriteLine("processApkInVM started");
            Console.WriteLine("currentVM: " + currentVM);
            /*
            //connectToAllAndroidVM();
            ADBLibrary.ADBClient.clearLogcat(currentVM);
            if (ADBLibrary.ADBClient.installApk(currentVM, config.download_location + hash + ".apk"))
            {
                RedisReceive result = new RedisReceive();
                Dictionary<String, String> results = ADBLibrary.ADBClient.parseLogcat(currentVM, config.android_vm_antivirus_keywords.ToArray());

                for (int j = 0; j < results.Count; j++)
                {
                    Console.WriteLine("[" + config.android_vm_antivirus_app[j] + "] says that file is a virus " + results[config.android_vm_antivirus_keywords[j]]);
                    result.av_results.Add(config.android_vm_antivirus_app[j], results[config.android_vm_antivirus_keywords[j]]);
                }
                //reset machine via proxmox api
                    Console.ForegroundColor = ConsoleColor.Red;
                    //ADBLibrary.ADBClient.unInstallApk(currentVM, packageName);
                    //ADBLibrary.ADBClient.runADB(currentVM, "reboot", false);
                    ADBLibrary.ADBClient.runADB(currentVM, "disconnect " + currentVM, false);
                    Console.WriteLine("Idi rucno vrati snapshot na masinu " + currentVM);
                    Console.ResetColor();
                //^ this is temporary
                //////////////////////////////
                result.master_id = "master1";
                result.hash = data.hash;
                result.upload_date = data.upload_date;
                result.upload_ip = data.upload_ip;
                result.filename = data.filename;
                Console.WriteLine(db1.ListLeftPush("receive", JsonConvert.SerializeObject(result), flags: CommandFlags.None));
                Console.WriteLine(sub.Publish("receive", "x"));
                Console.WriteLine("Returning to 'receive' redis queue:\n" + JsonConvert.SerializeObject(result));
                File.Delete(config.download_location + data.hash + ".apk");
            
                Thread.Sleep(int.Parse(config.android_vm_wait_time_reboot) * 1000);
                //ADBLibrary.ADBClient.connectToDevice(currentVM);
                androidVMavailable[i] = true;
                Console.WriteLine("processApkInVM ended");
            }
            */
            androidVMavailable[i] = true;
            //Thread.CurrentThread.Sleep(int.Parse(config.android_vm_wait_time_reboot) * 1000);
            Console.WriteLine("pre stopwatch");
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (true)
            {
                //some other processing to do possible
                if (stopwatch.ElapsedMilliseconds >= (int.Parse(config.android_vm_wait_time_reboot) * 1000))
                {
                    break;
                }
            }
            Console.WriteLine("posle stopwatch");
            Console.WriteLine("currentVM: " + currentVM);
            Console.Write("Ended processing hash ");
            writeLineColored(hash, ConsoleColor.Red);
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
            //ADBLibrary.ADBClient.runADB("", "kill-server", false);
            ADBLibrary.ADBClient.runADB("", "start-server", false);
            foreach (String ip in config.android_vm)
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

        public static void writeLineColored(String message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}

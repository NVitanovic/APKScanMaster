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
        public static int vmPosition = 0;
        public static bool zauzet = false;
        public static int brojReq = 0;
        public static List<string> queue;
        public static Object lockObj = new Object();
        public static int brojObradjenih = 0;//za testiranje
        public static void Main(string[] args)
        {
            Console.WriteLine("STARTED VERSION: 33");
            //Config config = configuration("config.json");

            androidVMavailable = new bool[config.AndroidVM.Count];
            androidVMtimeWhenAvailable = new int[config.AndroidVM.Count];
            for (int i = 0; i < config.AndroidVM.Count; i++)
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

            try
            {
                //connectToAllAndroidVM();
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

            try
            {
                //ConnectionMultiplexer redis1 = ConnectionMultiplexer.Connect("localhost");
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
            String packageName;
            writeLineColored("\nthread redisSubscribe started", ConsoleColor.Magenta);

            sub.Subscribe("send", (channel, message) =>
            {
                while (true)
                {
                    if (!zauzet)
                    {
                        if (brojReq >= config.AndroidVM.Count)
                        {
                            lock (lockObj)
                            {
                                zauzet = true;
                            }
                            writeLineColored("Zauzeto.", ConsoleColor.Red);
                            continue;
                        }
                        string work = db1.ListRightPop("send");

                        if (work != null)
                        {
                            lock (lockObj)
                            {
                                brojReq++;
                            }
                            Console.WriteLine((string)work);
                            Console.WriteLine("brojReq nakon pop je " + brojReq);
                            try
                            {
                                data = JsonConvert.DeserializeObject<RedisSend>(work);

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

                                    writeLineColored("\n==============================", ConsoleColor.DarkCyan);
                                    Console.WriteLine("vmPosition=" + vmPosition);
                                    if (androidVMavailable[vmPosition])
                                    {
                                        androidVMavailable[vmPosition] = false;
                                        currentVM = config.AndroidVM[vmPosition].android_vm;
                                        Console.WriteLine(vmPosition + " is available " + currentVM);
                                        Console.WriteLine(vmPosition + " started processing apk in " + currentVM);
                                        ThreadStart starter = delegate { threadProcessAPK(db1, sub, data.hash, packageName, currentVM); };
                                        Thread processApk = new Thread(starter);
                                        processApk.Start();
                                        Thread.Sleep(1000);
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

            //PROXMOX
            RedisProxmox resetRequest = new RedisProxmox();
            resetRequest.task = eTask.rollbackSnapshot;
            resetRequest.vm_id = findVMid(currentVM);
            resetRequest.auth = config.master.auth;
            resetRequest.master_id = config.master.master_id;

            writeLineColored(config.proxmox_channel, ConsoleColor.Red);
            writeLineColored(JsonConvert.SerializeObject(resetRequest), ConsoleColor.Green);

            ADBLibrary.ADBClient.runADB(currentVM, "disconnect", false);

            db1.ListLeftPush(config.proxmox_channel, JsonConvert.SerializeObject(resetRequest));
            sub.Publish(config.proxmox_channel, config.proxmox_channel);

            Console.WriteLine("pre stopwatch");
            Thread.Sleep(Int32.Parse(config.android_vm_wait_time_reboot) * 1000);
            Console.WriteLine("posle stopwatch");
            Console.WriteLine("currentVM: " + currentVM);
            Console.Write("Ended processing hash ");
            writeLineColored(hash, ConsoleColor.Red);

            ADBLibrary.ADBClient.connectToDevice(currentVM);
        }

        private static String findVMid(String currentVM)
        {
            String result = null;

            for (int i = 0; i < config.AndroidVM.Count; i++)
            {
                if (config.AndroidVM[i].android_vm == currentVM)
                {
                    result = config.AndroidVM[i].android_vm_id;
                    break;
                }
            }
            return result;
        }

        public static Config configuration(String path)
        {
            string jsonFromFile = File.ReadAllText(path);

            Config config = JsonConvert.DeserializeObject<Config>(jsonFromFile);

            return config;
        }

        public static void connectToAllAndroidVM()
        {
            //ADBLibrary.ADBClient.runADB("", "kill-server", false);
            ADBLibrary.ADBClient.runADB("", "start-server", false);
            foreach (var vm in config.AndroidVM)
            {
                ADBLibrary.ADBClient.connectToDevice(vm.android_vm);
            }
        }

        public static bool downloadFile(String uri, String fileName, String fileExtension, String path)
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

            var httpStream = response.Content.ReadAsStreamAsync();

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var pathOnDisk = Path.Combine(path, fileName + fileExtension);

            var fileStream = File.Create(pathOnDisk);
            var reader = new StreamReader(httpStream.Result);

            httpStream.Result.CopyTo(fileStream);
            fileStream.Flush();
            fileStream.Dispose();
            Console.WriteLine("downloadFile: ended");
            return true;
        }

        public static void writeLineColored(String message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void threadProcessAPK(IDatabase db, ISubscriber sub, string hash, string packageName, string currentVM)
        {
            lock (lockObj)
            {
                vmPosition++;
                if (vmPosition == config.AndroidVM.Count)
                {
                    vmPosition = 0;
                }
            }
            processApkInVM(db, sub, data.hash, packageName, currentVM, vmPosition);
            lock (lockObj)
            {
                brojReq--;
                androidVMavailable[vmPosition] = true;
                brojObradjenih++;
                Console.WriteLine("Broj obradjenih zahteva: " + brojObradjenih.ToString());
            }
            Console.WriteLine("br requesta nakon pozivanja processAPK " + brojReq);
            if (brojReq < config.AndroidVM.Count)
            {
                lock (lockObj)
                {
                    zauzet = false;
                }
            }
            Console.WriteLine("***********************************\n\n");
        }

    }
}

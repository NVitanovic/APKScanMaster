using System;
using System.Threading;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using APKScanSharedClasses;
using System.Net.Http;

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
        public static int processedAPKs = 0;
        public static void Main(string[] args)
        {
            Console.WriteLine("FINAL VERSION: ");
            writeLineColored("1.0.3", ConsoleColor.Cyan);
            Console.WriteLine("Publish date 21.1.2017.");

            androidVMavailable = new bool[config.AndroidVM.Count];
            //androidVMtimeWhenAvailable = new int[config.AndroidVM.Count];
            for (int i = 0; i < config.AndroidVM.Count; i++)
            {
                androidVMavailable[i] = true;
                //androidVMtimeWhenAvailable[i] = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            }

            ADBLibrary.ADBClient.logcatTimeout = int.Parse(config.logcat_wait);

            Thread t = new Thread(() =>
            {
                EmailNotify.SendEmail(config, "Program started at " + DateTime.Now);
            });
            t.Start();

            try
            {
                connectToAllAndroidVM();
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + ": " + e.StackTrace);
                Thread tt = new Thread(() =>
                {
                    EmailNotify.SendEmail(config, "StackTrace:\n" + e.StackTrace + "\n\nData:\n" + e.Data + "\n\nMessage:\n" + e.Message + "\n\nSource:\n" + e.Source + "\n\nInnerException:\n" + e.InnerException);
                });
                tt.Start();
            }

            try
            {
                String connectionString = "";
                for (int i = 0; i < config.masters.Count; i++)
                {
                    connectionString += config.masters[i] + ",";
                }
                connectionString = connectionString.Substring(0, connectionString.Length - 1);
                ConnectionMultiplexer redis1 = ConnectionMultiplexer.Connect(connectionString);
                IDatabase db1 = redis1.GetDatabase();
                ISubscriber sub = redis1.GetSubscriber();
                redisSubscribe(db1, sub);
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + ": " + e.StackTrace);
                Thread tt = new Thread(() =>
                {
                    EmailNotify.SendEmail(config, "StackTrace:\n" + e.StackTrace + "\n\nData:\n" + e.Data + "\n\nMessage:\n" + e.Message + "\n\nSource:\n" + e.Source + "\n\nInnerException:\n" + e.InnerException);
                });
                tt.Start();
            }

            while (true)
            {
                Thread.Sleep(100);
            }
        }

        public static void redisSubscribe(IDatabase db1, ISubscriber sub)
        {
            String packageName;

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
                            writeLineColored("Queue full. Waiting for some VM to become free.", ConsoleColor.Red);
                            continue;
                        }
                        string work = db1.ListRightPop("send");

                        if (work != null)
                        {
                            lock (lockObj)
                            {
                                brojReq++;
                            }
                            Console.WriteLine(DateTime.Now + ": " + (string)work);
                            Console.WriteLine(DateTime.Now + ": " + "Files waiting to be processed: " + brojReq);
                            try
                            {
                                data = JsonConvert.DeserializeObject<RedisSend>(work);

                                downloadFile(config.download_server, data.hash, ".apk", config.download_location);
                                packageName = ADBLibrary.ADBClient.getPackageNameFromApk(config.download_location + data.hash + ".apk");
                                if (packageName == INVALID_APK)
                                {
                                    lock (lockObj)
                                    {
                                        brojReq--;
                                    }
                                    sendWhenInvalidApk(db1, sub);
                                    Thread tt = new Thread(() =>
                                    {
                                        EmailNotify.SendEmail(config, "User: " + data.upload_ip + "\nat: " + data.upload_date + "\ntried to upload invalid file: " + data.filename + " with hash " + data.hash);
                                    });
                                    tt.Start();
                                }
                                else
                                {
                                    String currentVM;

                                    writeLineColored("\n==============================", ConsoleColor.DarkCyan);

                                    if (androidVMavailable[vmPosition])
                                    {
                                        lock (lockObj)
                                        {
                                            androidVMavailable[vmPosition] = false;
                                            currentVM = config.AndroidVM[vmPosition].android_vm;
                                        }
                                        ThreadStart starter = delegate { threadProcessAPK(db1, sub, data, packageName, currentVM); };
                                        Thread processApk = new Thread(starter);
                                        processApk.Start();
                                        Thread.Sleep(1000);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(DateTime.Now + ": " + e.StackTrace);
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

        private static void processApkInVM(IDatabase db1, ISubscriber sub, RedisSend data, string packageName, string currentVM, int i)
        {
            Console.WriteLine(DateTime.Now + ": " + "processApkInVM {0} started. file", findVMid(currentVM));
            writeLineColored(data.filename, ConsoleColor.Red);
            String currentVMipport = findVMid(currentVM);

            ADBLibrary.ADBClient.clearLogcat(currentVM);
            if (ADBLibrary.ADBClient.installApk(currentVM, config.download_location + data.hash + ".apk"))
            {
                RedisReceive result = new RedisReceive();
                Dictionary<String, String> results = ADBLibrary.ADBClient.parseLogcat(currentVM, config.android_vm_antivirus_keywords.ToArray());


                lock (lockObj)
                {
                    for (int j = 0; j < results.Count; j++)
                    {
                        Console.WriteLine(DateTime.Now + ": " + "[" + config.android_vm_antivirus_app[j] + "] says that file + " + data.filename + " is a virus " + results[config.android_vm_antivirus_keywords[j]]);
                        result.av_results.Add(config.android_vm_antivirus_app[j], results[config.android_vm_antivirus_keywords[j]]);
                    }



                    result.master_id = config.master.master_id;
                    result.hash = data.hash;
                    result.upload_date = data.upload_date;
                    result.upload_ip = data.upload_ip;
                    result.filename = data.filename;
                }
                Console.WriteLine(db1.ListLeftPush("receive", JsonConvert.SerializeObject(result), flags: CommandFlags.None));
                Console.WriteLine(sub.Publish("receive", "x"));
                Console.WriteLine(DateTime.Now + ": " + "Returning to 'receive' redis queue:\n");
                writeLineColored(JsonConvert.SerializeObject(result), ConsoleColor.Cyan);

                File.Delete(config.download_location + data.hash + ".apk");
            }


            //PROXMOX
            RedisProxmox resetRequest = new RedisProxmox();
            resetRequest.task = eTask.rollbackSnapshot;
            resetRequest.vm_id = findVMid(currentVM);
            resetRequest.auth = config.master.auth;
            resetRequest.master_id = config.master.master_id;

            writeLineColored(JsonConvert.SerializeObject(resetRequest), ConsoleColor.Green);

            writeLineColored("disconnecting from " + currentVM, ConsoleColor.Green);
            ADBLibrary.ADBClient.disconnect(currentVM);
            Thread.Sleep(1000);

            db1.ListLeftPush(config.proxmox_channel, JsonConvert.SerializeObject(resetRequest));
            sub.Publish(config.proxmox_channel, config.proxmox_channel);


            Thread.Sleep(Int32.Parse(config.android_vm_wait_time_reboot) * 1000);
            Console.Write("Snapshot returned at ");
            writeLineColored(currentVMipport, ConsoleColor.Red);
            Console.Write("processApkInVM ended processing file: ");
            writeLineColored(data.filename, ConsoleColor.Cyan);

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
            foreach (var vm in config.AndroidVM)
            {
                ADBLibrary.ADBClient.connectToDevice(vm.android_vm);
            }
        }

        public static bool downloadFile(String uri, String fileName, String fileExtension, String path)
        {

            Console.WriteLine(DateTime.Now + ": " + "downloadFile {0}: started", fileName);
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
            Console.WriteLine(DateTime.Now + ": " + "downloadFile {0}: ended", fileName);
            return true;
        }

        public static void writeLineColored(String message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void threadProcessAPK(IDatabase db, ISubscriber sub, RedisSend data, string packageName, string currentVM)
        {
            lock (lockObj)
            {
                vmPosition++;
                if (vmPosition == config.AndroidVM.Count)
                {
                    vmPosition = 0;
                }
            }
            processApkInVM(db, sub, data, packageName, currentVM, vmPosition);
            lock (lockObj)
            {
                brojReq--;
                androidVMavailable[vmPosition] = true;
                processedAPKs++;
                Console.Write("APKs processed so far: ");
                writeLineColored(processedAPKs.ToString(), ConsoleColor.Cyan);
            }
            if (brojReq < config.AndroidVM.Count)
            {
                lock (lockObj)
                {
                    zauzet = false;
                }
            }
            Console.WriteLine(DateTime.Now + ": " + "***********************************\n\n");
        }



        private static void sendWhenInvalidApk(IDatabase db1, ISubscriber sub)
        {
            Console.WriteLine(DateTime.Now + ": " + "Invalid apk. Returning to 'receive' redis queue:\n");
            RedisReceive result = new RedisReceive();
            Dictionary<String, String> results = new Dictionary<string, string>();

            lock (lockObj)
            {
                for (int j = 0; j < config.android_vm_antivirus_keywords.Count; j++)
                {
                    result.av_results.Add(config.android_vm_antivirus_app[j], "false");
                }



                result.master_id = config.master.master_id;
                result.hash = data.hash;
                result.upload_date = data.upload_date;
                result.upload_ip = data.upload_ip;
                result.filename = data.filename;
            }
            Console.WriteLine(db1.ListLeftPush("receive", JsonConvert.SerializeObject(result), flags: CommandFlags.None));
            Console.WriteLine(sub.Publish("receive", "x"));
        }
    }
}

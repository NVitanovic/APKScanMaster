﻿using System;
using System.Threading;
using StackExchange.Redis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;

namespace main
{
    public class Program
    {
        public static String INVALID_APK = "0 error";
        public static Config config = new Config();
        public static void Main(string[] args)
        {
            Console.WriteLine("STARTED VERSION: 23");
            Config config = configuration("config.json");
            startAllAndroidVM();

            ConnectionMultiplexer redis1 = ConnectionMultiplexer.Connect("192.168.4.201:7000,192.168.4.202:7000,192.168.4.203:7000");
            IDatabase db1 = redis1.GetDatabase();
            ISubscriber sub = redis1.GetSubscriber();
            redisSubscribe(db1, sub);

            //Console.WriteLine(ADBLibrary.ADBClient.getPackageNameFromApk("/home/koma/koma/apk/z.apk"));
            String result = ADBLibrary.ADBClient.getPackageNameFromApk("/home/koma/koma/apk/testvirus.txt");
            if(result != INVALID_APK)   //shitty test if file is valid APK
            {
                Console.WriteLine(result);
            }else {
                

                /*
                ConnectionMultiplexer redis1 = ConnectionMultiplexer.Connect("192.168.4.201:7000,192.168.4.202:7000,192.168.4.203:7000");
                IDatabase db1 = redis1.GetDatabase();
                ISubscriber sub = redis1.GetSubscriber();

                Thread threadSubscribe = new Thread(() => redisSubscribe(db1, sub));
                threadSubscribe.Start();

                while (true)
                {
                    Console.Write("Enter your message: ");
                    String message = Console.ReadLine();
                    Console.WriteLine(db1.ListLeftPush("receive", message, flags: CommandFlags.None));
                    Console.WriteLine(sub.Publish("receive", "x"));
                }
                */
                /*

                ADBLibrary.ADBClient.connectToDevice("192.168.4.101");
                ADBLibrary.ADBClient.installApk("/home/koma/koma/apk/testvirus.apk");
                Console.WriteLine("Package name is " + ADBLibrary.ADBClient.getPackageNameFromApk("/home/koma/koma/apk/testvirus.apk"));

                ADBLibrary.ADBClient.clearLogcat();
                ADBLibrary.ADBClient.logcatTimeout = 58;

                String[] logcatAntivirusKeyword = {
                    "virus",
                    "VirusScannerShieldDialogActivity", //AVAST
                    "pera",
                    "com.antivirus/.ui.scan.UnInstall", //AVG
                    "com.cleanmaster.security/ks.cm.antivirus.installmonitor.InstallMonitorNoticeActivity",   //CM Security
                    "com.bitdefender.antivirus/.NotifyUserMalware", //BIT DEFENDER
                    "org.malwarebytes.antimalware/.security.scanner.activity.alert.MalwareAppAlertActivity" //MALWAREBYTES
                };

                Dictionary<String, bool> results = ADBLibrary.ADBClient.parseLogcat(logcatAntivirusKeyword);
                for (int i = 0; i < results.Count; i++)
                {
                    Console.WriteLine("[" + logcatAntivirusKeyword[i] + "] says that file is a virus " + results[logcatAntivirusKeyword[i]]);
                }

                if (ADBLibrary.ADBClient.downloadFile(config.download_server + "com.spotify.music-15994536", config.download_location))
                    Console.WriteLine("File saved");
                else
                    Console.WriteLine("Error while downloading");
                */
                //-------------------------------------------------
            }
        
            Console.WriteLine("END MAIN");
            Console.ReadLine();
        }

        public static void redisSubscribe(IDatabase db1, ISubscriber sub)
        {
            Console.WriteLine("thread redisSubscribe started");
            while (true)
            {
                sub.Subscribe("send", (channel, message) =>
                {
                    //Console.WriteLine("************");
                    string work = db1.ListRightPop("send");
                    if (work != null)
                    {
                        Console.WriteLine((string)work);
                    }
                });
                Thread.Sleep(100);
            }
        }

        public static Config configuration(String path)
        {
            string jsonFromFile = File.ReadAllText(path);
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
            return config;
        }

        public static void startAllAndroidVM()
        {
            foreach(String ip in config.android_vm)
            {
                ADBLibrary.ADBClient.connectToDevice(ip);
            }
        }
    }
}

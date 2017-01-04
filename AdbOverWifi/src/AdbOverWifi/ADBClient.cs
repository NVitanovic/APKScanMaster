using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace netcore_redis
{
    public class ADBClient
    {
        public static void getDevices()
        {
            runADB("devices", 1);
        }

        public static void runADB(String arguments, int t)
        {
            int timeout = t;
            DateTime now;
            DateTime startTime;
            TimeSpan difference;

            //Console.WriteLine("Start time is: " + startTime);

            String line = "";
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    //FileName = "adb",
                    FileName = @"D:\Minimal ADB and Fastboot\adb.exe",//koristi liniju iznad za linux
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            //int i=0;
            bool waitLogcat = true;

            startTime = DateTime.UtcNow;
            while (waitLogcat)
            {
                line = proc.StandardOutput.ReadLine();

                now = DateTime.UtcNow;
                difference = now.Subtract(startTime);
                if (difference.TotalSeconds > timeout)
                    waitLogcat = false;
                if (!String.IsNullOrWhiteSpace(line))//treba da se testira
                    Console.WriteLine(line);
            };

            Console.WriteLine("END runADB");
        }
    }
}

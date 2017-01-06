using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;

namespace ADBLibrary
{
    public class ADBClient
    {
        public static List<ADBDevice> devices;
        public static int logcatTimeout = 45;

        public int LogcatTimeout
        {
            get { return logcatTimeout; }
            set { logcatTimeout = value; }
        }

        public ADBClient(int logcatTimeout = 45)
        {
            devices = new List<ADBDevice>();
            ADBClient.logcatTimeout = logcatTimeout;
        }

        public static void getDevices()
        {
            Process proc = runADB("devices");
            String line = proc.StandardOutput.ReadLine();
            while(!String.IsNullOrEmpty(line))
            {
                String[] str = line.Split('\t');
                ADBDevice device = new ADBDevice(str[0], str[1]);
                devices.Add(device);
            }
        }

        public static void connectToDevice(String ip)
        {
            IPAddress tmp;
            if (IPAddress.TryParse(ip, out tmp))
                runADB("connect " + ip);
            else
                throw new Exception("Invalid ip address.");
        }

        public static void clearLogcat()
        {
            runADB("logcat -c");
        }

        public static String getLogcat(int timeout)
        {
            clearLogcat();
            DateTime now, startTime;
            TimeSpan timeDifference;
            String logcat = null;
            bool waitLogcat = true;

            Process proc = runADB("logcat ActivityManager:I *:S");
            startTime = DateTime.UtcNow;
            while(waitLogcat)
            {
                logcat += proc.StandardOutput.ReadLine();
                now = DateTime.UtcNow;
                timeDifference = now.Subtract(startTime);
                if (timeDifference.TotalSeconds > timeout)
                {
                    waitLogcat = false;
                    proc.Dispose();
                }
            }
            return logcat;
        }

        public static Dictionary<String, bool> parseLogcat(String[] keyphrases)
        {
            Dictionary<String, bool> results = new Dictionary<string, bool>();
            String logcat = getLogcat(logcatTimeout);
            for(int i = 0; i < keyphrases.Length; i++)
            {
                if (logcat.IndexOf(keyphrases[i]) != -1)
                    results.Add(keyphrases[i], true);
                else
                    results.Add(keyphrases[i], false);
            }

            return results;
        }

        private static Process runADB(String args)
        {
            
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            return proc;
        }
    }

    public class ADBDevice
    {
        public String connection;//prvi deo adb devices
        public String name;//drugi deo adb devices

        public ADBDevice(String connection, String name)
        {
            this.connection = connection;
            this.name = name;
        }
    }
}

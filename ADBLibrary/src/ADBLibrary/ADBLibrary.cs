using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Net;

namespace ADBLibrary
{
    public class ADBClient
    {
        public static List<ADBDevice> devices;
        public static int logcatTimeout = 45;

        public static void Main()
        {
        }

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
            Process proc = runADB("devices",false);
            String line = proc.StandardOutput.ReadLine();
            while (!String.IsNullOrEmpty(line))
            {
                String[] str = line.Split('\t');
                if (!line.Contains("List of devices attached"))
                {
                    ADBDevice device = new ADBDevice(str[0], str[1]);
                    devices.Add(device);
                }
                line = proc.StandardOutput.ReadLine();
            }
        }

        public static void connectToDevice(String ip)   //TODO: add PORT, currently not using it
        {
            IPAddress tmp;
            if (IPAddress.TryParse(ip, out tmp))
                runADB("connect " + ip, false);
            else
                throw new Exception("Invalid ip address.");
        }

        public static void clearLogcat()
        {
            runADB("logcat -c",false);
        }

        public static String getLogcat(int timeout)
        {
            clearLogcat();
            String logcat = null;

            Process proc = runADB("logcat ActivityManager:I *:S", true);    //silence all other except from Activitymanager
            //Process proc = runADB("logcat",true);

            logcat = proc.StandardOutput.ReadToEnd();

            proc.Dispose();
            return logcat;
        }

        public static Dictionary<String, bool> parseLogcat(String[] keyphrases)
        {
            Console.WriteLine("parseLogcat: STARTED");
            Dictionary<String, bool> results = new Dictionary<String, bool>();
            String logcat = getLogcat(logcatTimeout);
            if (String.IsNullOrEmpty(logcat))
            {
                throw new Exception("logcat is empty");
            }
            for (int i = 0; i < keyphrases.Length; i++)
            {
                Console.WriteLine("parseLogcat: finding");
                if (logcat.IndexOf(keyphrases[i], StringComparison.CurrentCultureIgnoreCase) != -1)
                    results.Add(keyphrases[i], true);
                else
                    results.Add(keyphrases[i], false);
            }
            Console.WriteLine("parseLogcat: ENDED");
            return results;
        }

        private static Process runADB(String args, bool killIfLogcat)
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
            if (killIfLogcat)
            {
                if (!proc.WaitForExit(logcatTimeout * 1000))
                {
                    proc.Kill();
                }
            }
                
            return proc;
        }

        public static bool installApk(String path)
        {
            return true;
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

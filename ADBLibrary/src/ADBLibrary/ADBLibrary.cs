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
        public static String INVALID_APK = "0 error";
        public static void Main()
        {
        }

        public int LogcatTimeout
        {
            get { return logcatTimeout; }
            set { logcatTimeout = value; }
        }

        public ADBClient(int logcatTimeout)
        {
            devices = new List<ADBDevice>();
            ADBClient.logcatTimeout = logcatTimeout;
        }

        public static void getDevices()
        {
            Process proc = runADB("", "devices", false);
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
            /*
            IPAddress tmp;
            if (IPAddress.TryParse(ip, out tmp))
                runADB("connect " + ip, false);
            else
                throw new Exception("Invalid ip address.");
            */
            Process proc = runADB("", "connect " + ip, false);
            String result = proc.StandardOutput.ReadToEnd();
            if (result.IndexOf("connected", StringComparison.CurrentCultureIgnoreCase) == -1)
            {
                Console.WriteLine("Can't connect to " + ip);
            }
        }

        public static void clearLogcat(String ipport)
        {
            runADB(ipport, "logcat -c", false);
        }

        public static String getLogcat(String ipport, int timeout)
        {
            clearLogcat(ipport);
            String logcat = null;

            Process proc = runADB(ipport, "logcat ActivityManager:I *:S", true);    //silence all other except from Activitymanager

            logcat = proc.StandardOutput.ReadToEnd();

            proc.Dispose();
            return logcat;
        }

        public static Dictionary<String, String> parseLogcat(String ipport, String[] keyphrases)
        {
            //Console.WriteLine("parseLogcat: STARTED");
            Dictionary<String, String> results = new Dictionary<String, String>();
            String logcat = getLogcat(ipport, logcatTimeout);
            if (String.IsNullOrEmpty(logcat))
            {
                throw new Exception("logcat is empty");
            }
            for (int i = 0; i < keyphrases.Length; i++)
            {
                //Console.WriteLine("parseLogcat: finding");
                if (logcat.IndexOf(keyphrases[i], StringComparison.CurrentCultureIgnoreCase) != -1)
                    results.Add(keyphrases[i], "true");
                else
                    results.Add(keyphrases[i], "false");
            }
            //Console.WriteLine("parseLogcat: ENDED");
            return results;
        }

        private static Process runADB(String ipport, String args, bool killIfLogcat)
        {
            String arguments = "";
            if (ipport != "")
            {
                arguments = " -s " + ipport + " ";
            }
            arguments += args;
            //Console.WriteLine("runADB arguments " + arguments);
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = arguments,
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

        public static bool installApk(String ipport, String path)
        {
            //Console.WriteLine("installing apk " + path);
            //Console.WriteLine("ipport " + ipport);
            Process proc = runADB(ipport, " install " + path, false);
            String result = proc.StandardOutput.ReadToEnd();
            if (result.IndexOf("Success", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                if (result.IndexOf("Failure", StringComparison.CurrentCultureIgnoreCase) != -1)//just in case that file is success.apk
                {
                    Console.WriteLine("Failure while installing");
                    return false;
                }
                else
                {
                    Console.WriteLine("Installed successfully");
                    return true;
                }
            }
            else
            {
                Console.WriteLine("Failure while installing!!!");
                return false;
            }
        }
        //aapt dump badging <putanja do apk> | grep package:\ name
        //result is:
        //package: name='com.androidantivirus.testvirus' versionCode='8' versionName='1.8' platformBuildVersionName='5.1.1-1819727'
        public static String getPackageNameFromApk(String path)
        {
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "aapt",
                    Arguments = "dump badging " + path + " | grep package:\\ name",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            String result = "";
            try
            {
                proc.Start();
                result = proc.StandardOutput.ReadToEnd();
                String[] results = result.Split(' ');
                result = results[1].Substring(6, results[1].Length - 7);
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e);
                Console.WriteLine(e.StackTrace);
                //TODO: send email
                return INVALID_APK;
            }
        }


        public static bool unInstallApk(String ipport, String packageName)
        {
            Process proc = runADB(ipport, "uninstall " + packageName, false);
            String result = proc.StandardOutput.ReadToEnd();
            if (result.IndexOf("Success", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                if (result.IndexOf("Failure", StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    Console.WriteLine("Failure while uninstalling");
                    return false;
                }
                else
                {
                    Console.WriteLine("Uninstalled successfully");
                    return true;
                }
            }
            else
            {
                Console.WriteLine("Failure while uninstalling!!!");
                return false;
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
}

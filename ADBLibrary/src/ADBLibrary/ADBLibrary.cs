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
            Process proc = runADB("install " + path,false);
            String result = proc.StandardOutput.ReadToEnd();
            if (result.IndexOf("Success", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                if (result.IndexOf("Failure", StringComparison.CurrentCultureIgnoreCase) != -1)//just in case that file is success.apk
                {
                    Console.WriteLine("Failure while installing");
                    return false;
                }else
                {
                    Console.WriteLine("Installed successfully");
                    return true;
                }
            }else
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
                    FileName ="aapt",
                    Arguments = "dump badging " + path + " | grep package:\\ name", 
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            String result = proc.StandardOutput.ReadToEnd();
            String[] results = result.Split(' ');
            result = results[1].Substring(6, results[1].Length - 7);
            return result;
        }

        public static bool unInstallApk(String packageName)
        {
            Process proc = runADB("uninstall " + packageName, false);
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

        public static bool downloadFile(String url)
        {
            url += " -q -P /home/koma/koma/"; //quiet, save files to directory
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wget",
                    Arguments = url,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            String result = proc.StandardOutput.ReadToEnd();
            if (result.IndexOf("' saved [", StringComparison.CurrentCultureIgnoreCase) != -1) //wget returns this if success
            {
                Console.WriteLine("Error while download a file");
                return false;
            }else
            {
                return true;
            }


           
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

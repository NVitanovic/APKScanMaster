using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace ADBLibrary
{
    public class ADBClient
    {
        public static List<ADBDevice> devices;
        public static int logcatTimeout = 45;
        public static String INVALID_APK = "File doesn't have package!";
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

        public static void connectToDevice(String ipport)
        {
            Process proc = runADB(ipport, "connect", false);
            String result = proc.StandardOutput.ReadToEnd();
            if (result.IndexOf("connected", StringComparison.CurrentCultureIgnoreCase) == -1)
            {
                Console.WriteLine(DateTime.Now + ": " + "Can't connect to " + ipport);
            }
            Console.WriteLine(DateTime.Now + ": " + "Connecting to " + ipport + " result: " + result);
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
            Dictionary<String, String> results = new Dictionary<String, String>();
            String logcat = getLogcat(ipport, logcatTimeout);
            if (String.IsNullOrEmpty(logcat))
            {
                throw new Exception("logcat is empty");
            }
            for (int i = 0; i < keyphrases.Length; i++)
            {

                if (logcat.IndexOf(keyphrases[i], StringComparison.CurrentCultureIgnoreCase) != -1)
                    results.Add(keyphrases[i], "true");
                else
                    results.Add(keyphrases[i], "false");
            }
            return results;
        }

        public static void disconnect(String ipport)
        {
            runADB(ipport, "disconnect", false);
        }

        public static Process runADB(String ipport, String args, bool killIfLogcat)
        {
            String arguments = "";

            if (ipport == "")
            {
                arguments = args;
            }
            else
            {
                if (args == "disconnect")
                {
                    arguments = "disconnect " + ipport;

                }
                else if (args == "connect")
                {
                    arguments = "connect " + ipport;
                }
                else
                {
                    arguments = "-s " + ipport + " " + args;
                }
            }

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
            Console.WriteLine(DateTime.Now + ": " + "installing apk " + path);
            Process proc = runADB(ipport, "install " + path, false);
            String result = proc.StandardOutput.ReadToEnd();
            if (result.IndexOf("Success", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                if (result.IndexOf("Failure", StringComparison.CurrentCultureIgnoreCase) != -1)//just in case that file is success.apk
                {
                    Console.WriteLine(DateTime.Now + ": " + "Failure while installing");
                    return false;
                }
                else
                {
                    Console.WriteLine(DateTime.Now + ": " + "Installed successfully");
                    return true;
                }
            }
            else
            {
                Console.WriteLine(DateTime.Now + ": " + "Failure while installing!!!");
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
                //Console.WriteLine("package name from aapt: " + result);
                if (result.Contains(' ') && !String.IsNullOrWhiteSpace(result))
                {
                    String[] results = result.Split(' ');
                    result = results[1].Substring(6, results[1].Length - 7);
                    Console.WriteLine(DateTime.Now + ": " + " package name of " + path + " is " + result);
                    return result;
                }
                return INVALID_APK;
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + ": " + "Exception: " + e);
                Console.WriteLine(DateTime.Now + ": " + e.StackTrace);
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
                    Console.WriteLine(DateTime.Now + ": " + "Failure while uninstalling");
                    return false;
                }
                else
                {
                    Console.WriteLine(DateTime.Now + ": " + "Uninstalled successfully");
                    return true;
                }
            }
            else
            {
                Console.WriteLine(DateTime.Now + ": " + "Failure while uninstalling!!!");
                return false;
            }
        }

        public class ADBDevice
        {
            public String connection;
            public String name;

            public ADBDevice(String connection, String name)
            {
                this.connection = connection;
                this.name = name;
            }
        }

    }
}

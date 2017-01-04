using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace netcore_redis
{
    public class Program
    {
		//how to run:
		//dotnet   run   IP:PORT   PATH_TO_APK  TIMEOUT
		
		//dotnet run 192.168.4.101:5556 /home/koma/koma/apk/z.apk 5
        public static void Main(string[] args)
        {	
			int timeout = int.Parse(args[2]);
			runADB("logcat -c", 1);	//this is done instantly
            runADB("connect " + args[0],1);
            //Thread threadInstall = new Thread(() => runADB("install " + args[1], 10));
			//threadInstall.Start();
			Console.WriteLine("\n************LOGCAT STARTED****************\n");
			runADB("logcat", timeout);
			//runADB("logcat ActivityManager:I *:S", timeout);
			Console.WriteLine("END MAIN.");
            Console.ReadLine();
        }

        private static void runADB(String arguments, int t)
        {
			int timeout = t;
			DateTime now;
			DateTime startTime;
			TimeSpan difference ;

			//Console.WriteLine("Start time is: " + startTime);

            String line="";
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
			//int i=0;
			bool waitLogcat=true;
			
			startTime = DateTime.UtcNow;
            while (waitLogcat)
            {
                line = proc.StandardOutput.ReadLine();
			
				now = DateTime.UtcNow;
				difference = now.Subtract(startTime);
				if (difference.TotalSeconds > timeout)
					waitLogcat=false;
                if(!String.IsNullOrWhiteSpace(line))//treba da se testira
				    Console.WriteLine(line);
            };
			
			Console.WriteLine("END runADB");
        }
    } 
}

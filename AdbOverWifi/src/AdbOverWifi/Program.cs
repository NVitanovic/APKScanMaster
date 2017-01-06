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
            //ADBClient.runADB("logcat -c", 0);	//this is done instantly
            //ADBClient.runADB("connect " + args[0],1);
            ADBClient.getDevices();
            //Thread threadInstall = new Thread(() => runADB("install " + args[1], 10));
            //threadInstall.Start();
            Console.WriteLine("\n************LOGCAT STARTED****************\n");
            //ADBClient.runADB("logcat", timeout);
            Console.WriteLine("\n************LOGCAT ENDED*****************\n");
            //runADB("logcat ActivityManager:I *:S", timeout);
            Console.WriteLine("END MAIN.");
            Console.ReadLine();
        }

        
    } 
}

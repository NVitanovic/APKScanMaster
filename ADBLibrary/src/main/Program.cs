using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace main
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /*
            Console.ReadLine();
            Console.WriteLine("STARTED VERSION: 11");
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
            */
            if (ADBLibrary.ADBClient.downloadFile("www.cigani.xyz/1/vpn.jpg"))
                Console.WriteLine("File saved");
            else
                Console.WriteLine("Error while downloading");
            Console.WriteLine("END MAIN");
            Console.ReadLine();
        }
    }
}

using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APKScanSharedClasses;

namespace main
{
    public class Config : Redis
    {
        public List<string> android_vm { get; set; }
        public String download_server { get; set; }
        public String android_vm_wait_time { get; set; }
        public String download_location { get; set; }
        public List<string> android_vm_antivirus_keywords { get; set; }
        public List<string> android_vm_antivirus_app { get; set; }
        public String email_notify_addr { get; set; }
        public ConfigEmail email { get; set; }
    }
}

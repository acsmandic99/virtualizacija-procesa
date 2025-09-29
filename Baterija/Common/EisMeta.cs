using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class EisMeta
    {
        /*e {BatteryId: B01..B11, TestId: Test_1|Test_2,
SoC% iz naziva fajla, FileName, TotalRows};*/
        [DataMember]
        public string BatteryId { get; set; }
        [DataMember]
        public string TestId { get; set; }
        [DataMember]
        public int SoC { get; set; }
        [DataMember]
        public string FileName { get; set; }
        [DataMember]
        public int TotalRows { get; set; }
        [DataMember]
        public string FilePath { get; set; }    
    }
}

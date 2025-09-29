using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class Response
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string Status { get; set; } 

        [DataMember]
        public string Message { get; set; }
        [DataMember]

        public List<EventInfo> Events { get; set; } = new List<EventInfo>();
    }

    public class EventInfo
    {
        public string Type { get; set; }  
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

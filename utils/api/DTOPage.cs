using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace es.dmoreno.utils.api
{
    [DataContract]
    public class DTOPage
    {
        [DataMember(Name = "current")]
        public int Current { get; set; }

        [DataMember(Name = "last")]
        public int Last { get; set; }
    }
}

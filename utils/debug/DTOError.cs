using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace es.dmoreno.utils.debug
{
    [DataContract]
    public class DTOError
    {
        [DataMember(Name = "code")]
        public int Code { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}

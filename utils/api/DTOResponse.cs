using es.dmoreno.utils.debug;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace es.dmoreno.utils.api
{
    [DataContract]
    public class DTOResponse<T>
    {
        [DataMember(Name = "version", IsRequired = false)]
        public int Version { get; set; } = 0;

        [DataMember(Name = "response")]
        public T Response { get; set; }

        [DataMember(Name = "error", IsRequired = false)]
        public DTOError Error { get; set; } = null;        
    }
}

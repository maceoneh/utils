using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace es.dmoreno.utils.api
{
    [DataContract]
    public class DTORequest<T>
    {
        [DataMember(Name = "version", IsRequired = false)]
        public int Version { get; set; } = 0;

        [DataMember(Name = "token")]
        public string Token { get; set; }

        [DataMember(Name = "parameters", IsRequired = true)]
        public T Parameters { get; set; }
    }
}

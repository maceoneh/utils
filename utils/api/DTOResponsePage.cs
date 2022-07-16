using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace es.dmoreno.utils.api
{
    [DataContract]
    internal class DTOResponsePage<T> : DTOResponse<T>
    {
        [DataMember(Name = "pageInfo", IsRequired = false)]
        public DTOPage PageInfo { get; set; } = null;
    }
}

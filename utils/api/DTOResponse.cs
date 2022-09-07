using es.dmoreno.utils.debug;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace es.dmoreno.utils.api
{
    public class DTOResponse<T>
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 0;

        [JsonPropertyName("response")]
        public T Response { get; set; }

        [JsonPropertyName("error")]
        public DTOError Error { get; set; } = null;        
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace es.dmoreno.utils.api
{
    public class DTORequest<T>
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 0;

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("parameters")]
        public T Parameters { get; set; }
    }
}

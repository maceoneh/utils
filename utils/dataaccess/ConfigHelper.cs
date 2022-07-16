using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace es.dmoreno.utils.dataaccess
{
    [DataContract]
    public class ConfigValue
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "value")]
        public string Value { get; set; }
    }

    [DataContract]
    public class ConfigGroup
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "values")]
        public List<ConfigValue> Values { get; set; }
    }

    [DataContract]
    public class ConfigHelper
    {
        public string FileName { get; private set; }

        [DataMember(Name = "groups")]
        public List<ConfigGroup> Groups { get; set; }

        public ConfigHelper(string filename)
        {
            this.FileName = filename;
        }
    }
}

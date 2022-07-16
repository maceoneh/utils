using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.db
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class IndexAttribute : Attribute
    {
        public string Name { get; set; }
        public bool Unique { get; set; } = false;
    }
}

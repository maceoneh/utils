using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.filters
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class FilterAttribute : Attribute
    {
        public string Name { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.db
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class SortableAttribute : Attribute
    {
        public string Name { get; set; }
        public string FieldName { get; set; } = null;
    }
}

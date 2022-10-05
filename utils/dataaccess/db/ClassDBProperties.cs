using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace es.dmoreno.utils.dataaccess.db
{
    internal class ClassDBProperties
    {
        public string TableName;
        public Type Type;
        public List<PropertyInfo> Properties;
        public List<FieldAttribute> DBFieldAttributes;
        public List<FieldFilterSchema> DBFiltersAttributes;
        public List<SortableAttribute> DBSortableAttributes;
    }
}

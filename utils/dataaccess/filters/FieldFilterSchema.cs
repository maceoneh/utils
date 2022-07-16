using es.dmoreno.utils.dataaccess.db;
using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.filters
{
    public class FieldFilterSchema
    {
        public string TableName { get; internal set; }
        public string FieldName { get; internal set; }
        public string FilterName { get; internal set; }
        public ParamType FieldType { get; internal set; }
        public bool AllowNull { get; set; }
    }
}

using es.dmoreno.utils.dataaccess.db;
using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.permissions
{
    [Table(Name = "tables_permissions")]
    internal class DTOTableWithPermission
    {
        public const string TAG = "DTOTableWithPermission";
        public const string FilterName = TAG + "Name";

        [Field(FieldName = "id", IsPrimaryKey = true, IsAutoincrement = true, Type = ParamType.Int32)]
        public int ID { get; set; }

        [Field(FieldName = "name", Type = ParamType.String, AllowNull = false)]
        public string Name { get; set; }

        [Field(FieldName = "filename", AllowNull = false, Type = ParamType.String)]
        public string File { get; set; }
    }
}

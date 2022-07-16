using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.db
{
    [Table(Name = "files_per_table")]
    internal class FilePerTable
    {
        internal const string TAG = "FilePerTable";
        internal const string FilterNameFile = TAG + "NameFile";

        /// <summary>
        /// Referencia al statement de conectado a la tabla
        /// </summary>
        internal SQLStatement StatementToFile { get; set; } = null;

        [Field(FieldName = "id", IsAutoincrement = true, IsPrimaryKey = true, Type = ParamType.Int32)]
        public int ID { get; set; }
       
        [Filter(Name = FilterNameFile)]
        [Field(FieldName = "tablename", Type = ParamType.String)]
        public string Table { get; set; }

        [Field(FieldName = "filename", Type = ParamType.String)]
        public string NameFile { get; set; }
    }
}

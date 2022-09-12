using es.dmoreno.utils.dataaccess.db;
using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.permissions
{
    [Table(Name = "subjects_pertain_to_group")]
    internal class DTOSubjectPertainGroup
    {
        public const string TAG = "DTOSubjectPertainGroup";
        internal const string FilterRefGroup = TAG + "RefGroup";
        internal const string FilterRemoteUUID = TAG + "RemoteUUID";

        [Filter(Name = FilterRefGroup)]
        [Field(FieldName = "ref_group", IsPrimaryKey = true, IsAutoincrement = false, Type = ParamType.Int32)]
        internal int RefGroup { get; set; }

        [Filter(Name = FilterRemoteUUID)]
        [Field(FieldName = "remote_uuid", IsPrimaryKey = true, IsAutoincrement = false, Type = ParamType.String)]
        internal string RemoteUUID { get; set; }
    }
}

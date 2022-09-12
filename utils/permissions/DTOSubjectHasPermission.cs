using es.dmoreno.utils.dataaccess.db;
using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.permissions
{
    [Table(Name = "subject_has_permission")]
    public class DTOSubjectHasPermission
    {
        public const string TAG = "DTOSubjectHasPermission";
        internal const string FilterRefEntity = TAG + "RefEntity";
        internal const string FilterRefAction = TAG + "RefAction";
        internal const string FilterRemoteUUID = TAG + "RemoteUUID";

        public DTOPermission Permission { get; internal set; }

        [Filter(Name = FilterRefEntity)]
        [Field(FieldName = "ref_entity", IsPrimaryKey = true, IsAutoincrement = false, Type = ParamType.Int32)]
        internal int RefEntity { get; set; }

        [Filter(Name = FilterRefAction)]
        [Field(FieldName = "ref_action", IsPrimaryKey = true, IsAutoincrement = false, Type = ParamType.Int32)]
        internal int RefAction { get; set; }

        [Filter(Name = FilterRemoteUUID)]
        [Field(FieldName = "remote_uuid", IsPrimaryKey = true, IsAutoincrement = false, Type = ParamType.String)]
        internal string RemoteUUID { get; set; }
    }
}

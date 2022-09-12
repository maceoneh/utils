using es.dmoreno.utils.dataaccess.db;
using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.permissions
{
    [Table(Name = "groups")]
    public class DTOGroup
    {
        public const string TAG = "DTOGroup";
        public const string FilterId = TAG + "ID";
        internal const string FilterRemoteUUID = TAG + "RemoteUUID";
        internal const string IdxRemoteUUID = TAG + "RemoteUUID";

        [Filter(Name = FilterId)]
        [Field(FieldName = "id", IsAutoincrement = true, IsPrimaryKey = true, Type = ParamType.Int32)]
        internal int ID { get; set; }

        [Filter(Name = FilterRemoteUUID)]
        [Index(Name = IdxRemoteUUID, Unique = true)]
        [Field(FieldName = "remote_uuid", Type = ParamType.String)]
        public string RemoteUUID { get; set; }

        [Field(FieldName = "name", Type = ParamType.String)]
        public string Name { get; set; }
    }
}

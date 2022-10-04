using es.dmoreno.utils.dataaccess.db;
using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.permissions
{
    [Table(Name = "data_permission")]
    internal class DTODataPermission
    {
        public const string TAG = "DTODataPermission";
        public const string FilterIdentityRecord = TAG + "IdentityRecord";
        public const string IdxIdentityRecord = TAG + "IdentityRecord";
        public const string FilterRefPermission = TAG + "RefPermission";
        public const string IdxRefPermission = TAG + "RefPermission";

        [Filter(Name = FilterIdentityRecord)]
        [Field(FieldName = "identity_record", IsPrimaryKey = true, Type = ParamType.String)]
        public string IdentityRecord { get; set; }

        [Filter(Name = FilterRefPermission)]
        [Index(Name = IdxRefPermission)]
        [Field(FieldName = "ref_permission", Type = ParamType.Int32)]
        public int RefPermission { get; set; }
    }
}

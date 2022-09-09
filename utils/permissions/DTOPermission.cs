using es.dmoreno.utils.dataaccess.db;
using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.permissions
{
    [Table(Name = "permissions")]
    public class DTOPermission
    {
        public const string TAG = "DTOPermission";
        internal const string FilterRefEntity = TAG + "RefEntity";
        internal const string FilterRefAction = TAG + "RefAction";

        public DTOEntity Entity { get; internal set; }

        [Filter(Name = FilterRefEntity)]
        [Field(FieldName = "ref_entity", IsPrimaryKey = true, IsAutoincrement = false, Type = ParamType.Int32)]
        internal int RefEntity { get; set; }

        public DTOAction Action { get; internal set; }

        [Filter(Name = FilterRefAction)]
        [Field(FieldName = "ref_action", IsPrimaryKey = true, IsAutoincrement = false, Type = ParamType.Int32)]
        internal int RefAction { get; set; }

        [Field(FieldName = "description", Type = ParamType.String)]
        public string Description { get; set; }

        public DTOPermission CopyTo(DTOPermission p)
        {
            p.RefEntity = this.RefEntity;
            p.RefAction = this.RefAction;
            p.Description = this.Description;
            p.Entity = this.Entity.CopyTo(new DTOEntity());
            p.Action = this.Action.CopyTo(new DTOAction());
            return p;
        }
    }
}
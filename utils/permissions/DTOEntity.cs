using es.dmoreno.utils.dataaccess.db;
using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace es.dmoreno.utils.permissions
{
    [Table(Name = "entities")]
    public class DTOEntity
    {
        public const string TAG = "DTOEntity";
        internal const string FilterName = TAG + "Name";
        internal const string FilterID = TAG + "ID";

        [Filter(Name = FilterID)]
        [Field(FieldName = "id", IsPrimaryKey = true, IsAutoincrement = true, Type = ParamType.Int32)]
        public int ID { get; set; }

        [Filter(Name = FilterName)]
        [Field(FieldName = "name", Type = ParamType.String)]
        public string Name { get; set; }

        public DTOEntity CopyTo(DTOEntity e)
        {
            e.ID = this.ID;
            e.Name = this.Name;
            return e;
        }
    }
}

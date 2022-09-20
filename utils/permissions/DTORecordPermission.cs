using es.dmoreno.utils.dataaccess.db;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace es.dmoreno.utils.permissions
{
    [Table(Name = "record_permissions", FilePerTable = true)]
    public class DTORecordPermission
    {
        private string ValueGroupPermission = null;

        [Field(FieldName = "id", IsAutoincrement = true, IsPrimaryKey = true, Type = ParamType.Int32)]
        public int ID { get; set; }

        [Field(FieldName = "entity", Type = ParamType.String)]
        public string Entity { get; set; }

        [Field(FieldName = "owner", Type = ParamType.String)]
        public string Owner { get; set; }

        [Field(FieldName = "group_permissions", Type = ParamType.String)]
        public string GroupPermissionsAsString 
        {
            get
            {
                return this.ValueGroupPermission;
            }
            set
            {
                try
                {
                    //Si se deserializa sin problemas se asigna
                    var o = JsonSerializer.Deserialize<DTOListUUIDRecordPermission>(value);
                    if (o != null)
                    {
                        this.ValueGroupPermission = value;
                    }
                }
                catch
                { }
            }
        }

        [Field(FieldName = "all_can_read", Type = ParamType.Boolean)]
        public bool AllCanRead { get; set; }

        public DTOUUIDRecordPermision[] UUIDRecordPermissions
        {
            get
            {
                return JsonSerializer.Deserialize<DTOUUIDRecordPermision[]>(this.ValueGroupPermission);
            }
            set
            {
                this.ValueGroupPermission = JsonSerializer.Serialize(value);
            }
        }
    }
}

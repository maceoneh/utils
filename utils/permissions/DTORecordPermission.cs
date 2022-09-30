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
        //private string ValueUUIDPermission = null;
        private DTOUUIDRecordPermision[] ValueUUIDPermission = null;

        [Field(FieldName = "id", IsAutoincrement = true, IsPrimaryKey = true, Type = ParamType.Int32)]
        internal int ID { get; set; }

        [Field(FieldName = "entity", Type = ParamType.String)]
        internal string Entity { get; set; }

        [Field(FieldName = "owner", Type = ParamType.String)]
        public string UUIDOwner { get; set; }

        [Field(FieldName = "record_permissions", Type = ParamType.String)]
        public string UUIDRecordPermissionsAsString
        {
            get
            {
                return JsonSerializer.Serialize<DTOUUIDRecordPermision[]>(this.ValueUUIDPermission);
                //return this.ValueUUIDPermission;
            }
            set
            {
                if (value == null)
                {
                    this.ValueUUIDPermission = null;
                }
                else
                {
                    this.ValueUUIDPermission = JsonSerializer.Deserialize<DTOUUIDRecordPermision[]>(value);
                }
                //try
                //{

                //var o = JsonSerializer.Deserialize<DTOUUIDRecordPermision[]>(value);
                //if (o != null)
                //{
                //    this.ValueUUIDPermission = o;
                //}
                //}
                //catch { }
            }
        }

        public DTOUUIDRecordPermision[] UUIDRecordPermissions
        {
            get
            {
                return this.ValueUUIDPermission;
                //return JsonSerializer.Deserialize<DTOUUIDRecordPermision[]?>(this.ValueUUIDPermission);
            }
            set
            {
                this.ValueUUIDPermission = value;
                //this.ValueUUIDPermission = JsonSerializer.Serialize(value);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            var o = (DTORecordPermission)obj;
            if (o == null)
            {
                return false;
            }
            var error = this.Entity != o.Entity ||
                        this.UUIDOwner != o.UUIDOwner;
            if (error)
            {
                return false;
            }
            if (this.UUIDRecordPermissions == null && o.UUIDRecordPermissions == null)
            {
                return true;
            }
            else
            {
                if (this.UUIDRecordPermissions != null && o.UUIDRecordPermissions != null)
                {
                    if (this.UUIDRecordPermissions.Length != o.UUIDRecordPermissions.Length)
                    {
                        return false;
                    }
                    for (int i = 0; i < this.UUIDRecordPermissions.Length; i++)
                    {
                        var item_local = this.UUIDRecordPermissions[i];
                        var item_remote = o.UUIDRecordPermissions[i];
                        if (!item_local.Equals(item_remote))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}

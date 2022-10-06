using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.permissions
{
    public class DTOUUIDRecordPermision
    {
        public string UUID { get; set; }
        public bool CanWrite { get; set; }
        public bool CanRead { get; set; }
        public bool CanDelete { get; set; }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            var o = (DTOUUIDRecordPermision)obj;
            if (o == null)
            {
                return false;
            }
            return this.UUID.Equals(o.UUID) && this.CanWrite.Equals(o.CanWrite) && this.CanRead.Equals(o.CanRead) && this.CanDelete.Equals(o.CanDelete);
        }
    }
}

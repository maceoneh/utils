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
    }
}

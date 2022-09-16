using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.permissions
{
    internal class DTOGroupRecordPermision
    {
        public string UUID { get; set; }
        public bool CanWrite { get; set; }
        public bool CanRead { get; set; }
    }
}

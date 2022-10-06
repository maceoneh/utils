using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.permissions
{
    public class PermissionException : Exception
    {
        public PermissionException(string msg) : base(msg) { }
        public PermissionException() : base() { }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.db
{
    public class DataBaseLogicException : Exception
    {
        public const int E_GENERIC = 1;

        public int Code { get; private set; } = 0;

        internal DataBaseLogicException(int code, string message) : base(message)
        {
            this.Code = code;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.db
{
    public class FilePerTableException : Exception
    {
        public const int NO_ERROR = 0;
        
        /// <summary>
        /// No existe el fichero al que se hace referencia desde la configuración de la tabla
        /// </summary>
        public const int FILE_NOT_EXIST = 1;

        public int Code { get; set; } = 0;

        public FilePerTableException(int code, string message) : base(message)
        {
            this.Code = code;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.db
{
    [AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TableAttribute : Attribute
    {
        public string Name { get; set; }

        public EngineType Type { get; set; }

        /// <summary>
        /// Si el gestor de base de datos lo permite crea un fichero solo para almacenar esta tabla
        /// </summary>
        public bool FilePerTable { get; set; } = false;
    }
}

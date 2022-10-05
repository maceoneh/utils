using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.db
{
    public class StatementOptions
    {
        /// <summary>
        /// Asigna de forma manual una sentencia SQL a la consulta (Avanzado)
        /// </summary>
        public string SQL { get; set; } = "";

        /// <summary>
        /// Asigna de forma manual un order by a la sentencia (Avanzado)
        /// </summary>
        public string OrderBy { get; set; } = "";

        /// <summary>
        /// Asigna de forma manual el listado de parámetros (Avanzado)
        /// </summary>
        public List<StatementParameter> Parameters { get; set; } = null;

        /// <summary>
        /// Genera la parte WHERE de la consulta a razon de los filtros intruducidos
        /// </summary>
        public List<Filter> Filters { get; set; } = null;

        /// <summary>
        /// Genera la parte ORDER BY a razon de los orders introducidos
        /// </summary>
        public List<Order> Orders { get; set; } = null;
        
        /// <summary>
        /// Agrega un limite a la consulta
        /// </summary>        
        public int LimitTo { get; set; } = 0;

        /// <summary>
        /// Agrega el tamaño del limite a la consulta
        /// </summary>
        public int LimitLength { get; set; } = 0;
    }
}

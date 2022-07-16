using es.dmoreno.utils.dataaccess.filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.db
{
    public class StatementOptions
    {
        public string SQL { get; set; } = "";
        public List<Filter> Filters { get; set; } = null;
        public string OrderBy { get; set; } = "";
        public List<StatementParameter> Parameters { get; set; } = null;
        public int LimitTo { get; set; } = 0;
        public int LimitLength { get; set; } = 0;
    }
}

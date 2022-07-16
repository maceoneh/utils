using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.filters
{
    public enum FilterType
    {
        Equal = 0,
        Less = 1,
        LessOrEqual = 2,
        Greater = 3,
        GreaterOrEqual = 4,
        Like = 5,
        Between = 6,
        NotEqual = 7,
        In = 8
    }
}

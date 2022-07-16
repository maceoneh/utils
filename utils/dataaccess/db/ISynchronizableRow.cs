using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.db
{
    public interface ISynchronizableRow
    {
        string UpdateState { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace es.dmoreno.utils.dataaccess.db
{
    public class SynchronizeListOptions<T>
    {
        public delegate Task VoidFunction(T r);

        public string ForeignKey { get; set; }

        public List<T> List { get; set; }

        public bool DeleteIfNotExists { get; set; } = true;

        public DataReport DataReport { get; set; } = null;

        public VoidFunction OnBeforeDeleteRecord { get; set; } = null;

        public VoidFunction OnBeforeInsertRecord { get; set; } = null;

        public VoidFunction OnBeforeUpdateRecord { get; set; } = null;

        public VoidFunction OnAfterDeleteRecord { get; set; } = null;

        public VoidFunction OnAfterInsertRecord { get; set; } = null;

        public VoidFunction OnAfterUpdateRecord { get; set; } = null;
    }
}

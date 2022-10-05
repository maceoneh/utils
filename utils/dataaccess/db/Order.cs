using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.dataaccess.db
{
    public class Order
    {
        public string Name { get; set; }
        public EOrderType OrderType { get; set; } = EOrderType.Asc;
    }
}

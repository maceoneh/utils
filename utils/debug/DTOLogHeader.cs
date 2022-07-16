using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.debug
{
    internal class DTOLogHeader
    {
        public DateTime Time { get; set; }

        public string Tag { get; set; }

        public ETypeLog Type { get; set; }

        public string TextHeader { get; set; }
    }
}

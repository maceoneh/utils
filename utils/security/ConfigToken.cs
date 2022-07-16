using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.security
{
    public class ConfigToken
    {
        internal static char[] Characters { get; } = new char[] { 
            //0-9
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            //10-35
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            //36-61
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
        };

        public bool Letters { get; set; } = true;

        public bool UpperCase { get; set; } = false;

        public bool Numbers { get; set; } = true;

        public int Length { get; set; } = 20;
    }
}

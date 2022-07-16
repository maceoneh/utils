using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.apps
{
    public class DTOArg
    {
        public string DataPath { get; set; } = null;
        public string RootPath { get; set; } = null;
        public List<int> Ports { get; } = new List<int>();

        public override string ToString()
        {
            var txt = "Data Path: " + this.DataPath + Environment.NewLine;
            txt += "Root Path: " + this.RootPath + Environment.NewLine;
            txt += "Ports: ";
            foreach (var item in Ports)
            {
                txt += item.ToString() + " ";
            }
            txt = txt.TrimEnd() + Environment.NewLine;
            return txt;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.debug
{
    public enum ETypeLog
    {
        Error = 10,
        Warning = 5,
        Verbose = 2,
        Debug = 1,
    }

    static public class TypeLog
    {
        static public ETypeLog[] GetArray(IEnumerable<string> array)
        {
            var result = new List<ETypeLog>();

            foreach (var item in array)
            {
                if (item == "Error")
                {
                    result.Add(ETypeLog.Error);
                }
                else if (item == "Verbose")
                {
                    result.Add(ETypeLog.Verbose);
                }
                else if (item == "Warning")
                {
                    result.Add(ETypeLog.Warning);
                }
                else if (item == "Debug")
                {
                    result.Add(ETypeLog.Debug);
                }
            }

            return result.ToArray();
        }
    }
}

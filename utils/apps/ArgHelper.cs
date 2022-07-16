using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace es.dmoreno.utils.apps
{
    static public class ArgHelper
    {
        static public DTOArg Parse(string[] args)
        {
            var arg = new DTOArg {
                DataPath = "." + Path.DirectorySeparatorChar + "data" + Path.DirectorySeparatorChar,
                RootPath = "." + Path.DirectorySeparatorChar
            };
            foreach (var item in args)
            {
                var regex = new Regex("--port=([0-9]+)");
                var match = regex.Match(item);
                if (match.Success)
                {
                    var port = Convert.ToInt32(match.Groups[1].Value);
                    arg.Ports.Add(port);
                }
                else
                {
                    regex = new Regex("--rootpath=(.*)");
                    match = regex.Match(item);
                    if (match.Success)
                    {
                        var path = match.Groups[1].Value;
                        if (!path.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                        {
                            path += System.IO.Path.DirectorySeparatorChar;
                        }
                        arg.RootPath = path;
                    }
                    else
                    {
                        regex = new Regex("--datapath=(.*)");
                        match = regex.Match(item);
                        if (match.Success)
                        {
                            var path = match.Groups[1].Value;
                            if (!path.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                            {
                                path += System.IO.Path.DirectorySeparatorChar;
                            }
                            arg.DataPath = path;
                        }
                    }
                }
            }
            return arg;
        }
    }
}

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace es.dmoreno.utils.corenet.api.middleware
{
    public delegate bool function_bool_httpcontext(HttpContext c);
    public class AuthorizationScripts
    {
        public string Endpoint { get; set; }
        public string Method { get; set; }
        public function_bool_httpcontext Script { get; set; }
    }
}

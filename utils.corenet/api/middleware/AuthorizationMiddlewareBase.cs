using es.dmoreno.utils.api;
using es.dmoreno.utils.debug;
using es.dmoreno.utils.security;
using es.dmoreno.utils.serialize;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace es.dmoreno.utils.corenet.api.middleware
{
    public abstract class AuthorizationMiddlewareBase
    {
        private RequestDelegate _next;

        private bool authsended;

        /// <summary>
        /// Listado de criterios que debe cumplir una URL para no ser verificada:
        /// 
        /// * Forma de realizar checkeo (start o regex)
        /// * Recurso
        /// * Método
        /// </summary>
        static protected string[] ResourcesWithoutAuthorization { get; set; } = new string[]
        {
            //Forma de realizar checkeo (start o regex)
            //Recurso
            //Metodo
        };

        static protected string[] ResourcesByTypeWithAuthorization { get; set; } = new string[]
        {
            //Forma de realizar checkeo (start o regex)
            //Recurso
            //Metodo
            //Tipo de autorizacion
        };

        public AuthorizationMiddlewareBase(RequestDelegate next)
        {
            this._next = next;
        }

        protected virtual async Task<bool> checkValidationAsync(HttpContext context)
        {
            return true;
        }

        static private bool checkIfNeedAuthorization(HttpContext context)
        {
            var res = context.Request.Path.Value;
            var method = context.Request.Method;

            for (int i = 0; i < ResourcesWithoutAuthorization.Length; i = i + 3)
            {
                if (ResourcesWithoutAuthorization[i].Equals("start"))
                {
                    if (res.StartsWith(ResourcesWithoutAuthorization[i + 1]))
                    {
                        if (ResourcesWithoutAuthorization[i + 2].Equals(method))
                        {
                            return false;
                        }
                    }
                }
                else if (ResourcesWithoutAuthorization[i].Equals("regex"))
                {
                    if (ResourcesWithoutAuthorization[i + 2].Equals(method))
                    {
                        var regex = new Regex(ResourcesWithoutAuthorization[i + 1]);

                        if (regex.IsMatch(res))
                        {
                            return false;
                        }
                    }
                }
                else if (ResourcesWithoutAuthorization[i].Equals("equal"))
                {
                    if (ResourcesWithoutAuthorization[i + 2].Equals(method))
                    {
                        if (ResourcesWithoutAuthorization[i + 1].Equals(res))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        static protected string GetAuthorizationType(HttpContext context)
        {
            //Formato
            //Forma de realizar checkeo (start o regex)
            //Recurso
            //Metodo
            //Tipo de autorizacion

            var res = context.Request.Path.Value;
            var method = context.Request.Method;

            for (int i = 0; i < ResourcesByTypeWithAuthorization.Length; i = i + 4)
            {
                if (ResourcesByTypeWithAuthorization[i].Equals("equal"))
                {
                    if (ResourcesByTypeWithAuthorization[i + 2].Equals(method))
                    {
                        if (ResourcesByTypeWithAuthorization[i + 1].Equals(res))
                        {
                            return ResourcesByTypeWithAuthorization[i + 3];
                        }
                    }
                }
            }
            return "";
        }

        public async Task Invoke(HttpContext context)
        {
            this.extractAuthorizationType(context);

            if (checkIfNeedAuthorization(context))
            {
                if (authsended)
                {
                    if (await this.checkValidationAsync(context))
                    {
                        await this._next.Invoke(context);
                    }
                    else
                    {
                        var data = Encoding.UTF8.GetBytes(JSon.serializeJSON<DTOResponse<DTOEmptyResponse>>(new DTOResponse<DTOEmptyResponse>()
                        {
                            Error = new DTOError()
                            {
                                Code = StatusCodes.Status401Unauthorized,
                                Message = "Not valid authorization"
                            }
                        }));

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        await context.Response.Body.WriteAsync(data, 0, data.Length);
                        await Log.WriteAsync(ETypeLog.Error, this.GetType().Name, "Authorization header is empty on: " + context.Request.Method + " " + context.Request.Path.Value);
                        return;
                    }
                }
                else
                {
                    var data = Encoding.UTF8.GetBytes(JSon.serializeJSON<DTOResponse<DTOEmptyResponse>>(new DTOResponse<DTOEmptyResponse>()
                    {
                        Error = new DTOError()
                        {
                            Code = StatusCodes.Status401Unauthorized,
                            Message = "Authorization header is empty for this method"
                        }
                    }));

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.Body.WriteAsync(data, 0, data.Length);
                    await Log.WriteAsync(ETypeLog.Error, this.GetType().Name, "Authorization header is empty on: " + context.Request.Method + " " + context.Request.Path.Value);
                    return;
                }
            }
            else
            {
                await Log.WriteAsync(ETypeLog.Debug, this.GetType().Name, "Skipping authorization on: " + context.Request.Method + " " + context.Request.Path.Value);
                await this._next.Invoke(context);
            }
        }

        private void extractAuthorizationType(HttpContext context)
        {
            StringValues headauth;
            string auth;

            this.authsended = true;

            if (context.Request.Headers.TryGetValue("Authorization", out headauth))
            {
                if (headauth[0].ToLower().Contains("basic"))
                {
                    auth = headauth[0].Substring("basic ".Length);
                    auth = Base64.Decode(auth);

                    var basic = auth.Split(':');

                    context.Request.Headers.Add("_user", basic[0]);
                    context.Request.Headers.Add("_password", basic[1]);                    
                    context.Request.Headers.Add("_auth_type", "basic");
                    context.Items.Add("_user", basic[0]);
                    context.Items.Add("_password", basic[1]);
                    context.Items.Add("_auth_type", "basic");
                }
                else if (headauth[0].ToLower().Contains("bearer"))
                {
                    auth = headauth[0].Substring("bearer ".Length);
                    var auth_wo_decode = auth;
                    auth = Base64.Decode(auth);

                    context.Request.Headers.Add("_token", auth);
                    context.Request.Headers.Add("_token_wo_decode", auth_wo_decode);
                    context.Request.Headers.Add("_auth_type", "bearer");
                    context.Items.Add("_token", auth);
                    context.Items.Add("_token_wo_decode", auth_wo_decode);
                    context.Items.Add("_auth_type", "bearer");
                }
                else
                {
                    context.Request.Headers.Add("_auth_type", "");
                    context.Items.Add("_auth_type", "");
                    authsended = false;
                }
            }
            else
            {
                context.Request.Headers.Add("_auth_type", "");
                context.Items.Add("_auth_type", "");
                authsended = false;
            }
        }
    }
}

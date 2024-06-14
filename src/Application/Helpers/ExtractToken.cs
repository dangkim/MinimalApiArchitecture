using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MinimalApiArchitecture.Application.Helpers
{
    public static class ExtractToken
    {
        public static string GetToken(HttpContext httpContext)
        {
            // We check both cases client using header (API) and cookies (From Web)
            var authHeaderParts = httpContext!.Request.Headers.Authorization.ToString().Split(' ');

            var bearerValue = (authHeaderParts.Length == 2 && authHeaderParts[0] == "Bearer") ? authHeaderParts[1] : string.Empty;
            var access_token = httpContext!.Request.Cookies["stk"];

            //var tokenObject = cookiesString;//httpContext!.Request.Cookies["stk"] == null ? new Token() : JsonSerializer.Deserialize<Token>(cookiesString!);

            var tokenString = string.IsNullOrEmpty(access_token) ? bearerValue : access_token;
        
            return tokenString;
        }
    }
}

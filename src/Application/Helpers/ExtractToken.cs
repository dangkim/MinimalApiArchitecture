using Microsoft.AspNetCore.Http;
using MinimalApiArchitecture.Application.Model;
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
            var authHeaderParts = httpContext!.Request.Headers.Authorization.ToString().Split(' ');

            var bearerValue = (authHeaderParts.Length == 2 && authHeaderParts[0] == "Bearer") ? authHeaderParts[1] : string.Empty;
            var cookiesObject = httpContext!.Request.Cookies["stk"];

            var tokenObject = httpContext!.Request.Cookies["stk"] == null ? new Token() : JsonSerializer.Deserialize<Token>(cookiesObject!);

            var tokenString = string.IsNullOrEmpty(tokenObject!.access_token) ? bearerValue : tokenObject!.access_token;
        
            return tokenString;
        }
    }
}

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MinimalApiArchitecture.Application.Helpers
{
    public static class ValidateTokenHelper
    {
        public static string? ValidateAndExtractToken(HttpContext httpContext, out IResult? validationResult)
        {
            var tokenString = ExtractToken.GetToken(httpContext);

            if (string.IsNullOrEmpty(tokenString))
            {
                validationResult = Results.Problem("Unauthorized", "", (int)HttpStatusCode.Unauthorized);
                return null;
            }

            validationResult = null;
            return tokenString;
        }
    }

}

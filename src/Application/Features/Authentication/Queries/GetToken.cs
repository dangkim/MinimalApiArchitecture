using AutoMapper;
using AutoMapper.QueryableExtensions;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Domain.Entities;
using MinimalApiArchitecture.Application.Features.Products.EventHandlers;
using MinimalApiArchitecture.Application.Helpers;
using MinimalApiArchitecture.Application.Infrastructure.Persistence;
using MinimalApiArchitecture.Application.Model;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class GetToken : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/getToken", (IMediator mediator, GetTokenQuery query) =>
        {
            return mediator.Send(query);
        })
        .WithName(nameof(GetToken));
    }

    public class GetTokenQuery : IRequest<IResult>
    {
        public string UserName { get; set; }
        public string Password { get; set; }

    }

    public class GetTokenHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetTokenHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetTokenQuery, IResult>
    {
        public async Task<IResult> Handle(GetTokenQuery request, CancellationToken cancellationToken)
        {
            byte[] bytes = Convert.FromBase64String(request.Password);
            string decryptedPassword = Encoding.UTF8.GetString(bytes);

            var httpTokenClient = httpClientFactory.CreateClient("SimTokenClient");

            using (httpTokenClient)
            {
                try
                {
                    var formData = new Dictionary<string, string>
                                        {
                                            { "grant_type", "password" },
                                            { "client_id", configuration["client_id"] },
                                            { "client_secret", configuration["client_secret"] },
                                            { "username", request.UserName },
                                            { "password", decryptedPassword }
                                        };

                    var content = new FormUrlEncodedContent(formData);

                    using var response = await httpTokenClient.PostAsync("token", content, cancellationToken);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                       var responseData = await response.Content.ReadAsStringAsync(cancellationToken);

                        using var httpProfileClient = httpClientFactory.CreateClient("SimApiClient");

                        var url = string.Format("profile");

                        httpProfileClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JsonSerializer.Deserialize<Token>(responseData)!.access_token);

                        using var responseProfile = await httpProfileClient.GetAsync(url, cancellationToken);

                        if (responseProfile.IsSuccessStatusCode)
                        {
                            var responseProfileData = await responseProfile.Content.ReadFromJsonAsync<object>(cancellationToken);

                            var responseWithCookies = httpContextAccessor.HttpContext.Response;

                            var cookieOptions = new CookieOptions
                            {
                                Expires =  DateTimeOffset.UtcNow.AddDays(356),
                                Secure = true,
                                HttpOnly = true,
                                SameSite = SameSiteMode.None
                            };

                            responseWithCookies.Cookies.Append("stk", responseData, cookieOptions);

                            return Results.Ok(responseProfileData);
                        }
                        else
                        {
                            string responseProfileData = await response.Content.ReadAsStringAsync(cancellationToken);
                            var responseObject = JsonSerializer.Deserialize<ProblemDetails>(responseData);
                            return Results.Problem(responseObject!);
                        }
                    }
                    else
                    {
                        string responseData = await response.Content.ReadAsStringAsync(cancellationToken);
                        var responseObject = JsonSerializer.Deserialize<ProblemDetails>(responseData);
                        return Results.Problem(responseObject!);
                    }

                }
                catch (Exception ex)
                {
                    logger.LogWarning("GetTokenHandler: {Message}", ex.InnerException);
                    return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }

    public class GetTokenResponse
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; }
    }

}
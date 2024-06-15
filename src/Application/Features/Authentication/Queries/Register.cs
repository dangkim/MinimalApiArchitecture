using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Model;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class Register : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/register", (IMediator mediator, RegisterQuery query) =>
        {
            return mediator.Send(query);
        })
        .WithName(nameof(Register));
    }

    public class RegisterQuery : IRequest<IResult>
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class RegisterHandler(IDistributedCache cache, IHttpContextAccessor httpContextAccessor, ILogger<RegisterHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<RegisterQuery, IResult>
    {
        public async Task<IResult> Handle(RegisterQuery request, CancellationToken cancellationToken)
        {
            byte[] bytes = Convert.FromBase64String(request.Password);
            byte[] bytesConfirm = Convert.FromBase64String(request.ConfirmPassword);
            string decryptedPassword = Encoding.UTF8.GetString(bytes);
            string decryptedConfirmPassword = Encoding.UTF8.GetString(bytesConfirm);

            // Use IHttpClientFactory to create an instance of HttpClient
            var httpClient = httpClientFactory.CreateClient("SimApiClient");
            var httpTokenClient = httpClientFactory.CreateClient("SimTokenClient");

            var cacheTokenKey = $"UserToken-{request.UserName}";

            var cacheOptions = new DistributedCacheEntryOptions()
                                                    .SetSlidingExpiration(TimeSpan.FromDays(366));

            var cachedUserToken = await cache.GetStringAsync(cacheTokenKey, token: cancellationToken);

            // Create an instance of the request body
            var requestBody = new
            {
                email = request.UserName,
                password = decryptedPassword,
                confirmPassword = decryptedConfirmPassword
            };

            // Serialize the request body to JSON
            var requestBodyJson = JsonSerializer.Serialize(requestBody);

            // Create an instance of HttpClient
            using (httpClient)
            {
                try
                {
                    // Create an instance of HttpContent with the serialized request body
                    var httpContent = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

                    // Send a POST request with the HttpContent
                    HttpResponseMessage response = await httpClient.PostAsync("SimRegister", httpContent, cancellationToken);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        var responseWithCookies = httpContextAccessor.HttpContext.Response;

                        // Read the response content as a string
                        var responseData = await response.Content.ReadFromJsonAsync<RegisterResponse>(cancellationToken);

                        // Get token
                        var formData = new Dictionary<string, string>
                                        {
                                            { "grant_type", "password" },
                                            { "client_id", configuration["client_id"] },
                                            { "client_secret", configuration["client_secret"] },
                                            { "username", request.UserName },
                                            { "password", decryptedPassword }
                                        };

                        var content = new FormUrlEncodedContent(formData);

                        using var responseToken = await httpTokenClient.PostAsync("token", content, cancellationToken);

                        if (responseToken.IsSuccessStatusCode)
                        {
                            var responseTokenData = await responseToken.Content.ReadFromJsonAsync<ResponseTokenData>(cancellationToken: cancellationToken);//.ReadAsStringAsync(cancellationToken);

                            var cookieOptions = new CookieOptions
                            {
                                Expires = DateTimeOffset.UtcNow.AddDays(366),
                                Secure = true,
                                HttpOnly = true,
                                SameSite = SameSiteMode.None
                            };

                            responseWithCookies.Cookies.Append("stk", responseTokenData.Access_token, cookieOptions);

                            var tokenList = string.IsNullOrEmpty(cachedUserToken)
                                            ? []
                                            : JsonSerializer.Deserialize<List<string>>(cachedUserToken) ?? [];

                            if (!tokenList.Contains(responseTokenData.Access_token))
                            {
                                tokenList.Add(responseTokenData.Access_token);
                                var serializedTokenList = JsonSerializer.Serialize(tokenList);
                                await cache.SetStringAsync(cacheTokenKey, serializedTokenList, cacheOptions, token: cancellationToken);
                            }
                        }

                        return Results.Ok(responseData);
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
                    logger.LogWarning("RegisterHandler: {Message}", ex.InnerException);
                    return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }

    public class RegisterResponse
    {
        public int ProfileId { get; set; }

        public string Email { get; set; }

        public long UserId { get; set; }

        public string UserName { get; set; }

        public object Vendor { get; set; }

        public object DefaultForwardingNumber { get; set; }

        public int Balance { get; set; }

        public object Currency { get; set; }

        public int OriginalAmount { get; set; }

        public int Amount { get; set; }

        public int RateInUsd { get; set; }

        public object GmailMsgId { get; set; }

        public int Rating { get; set; }

        public object DefaultCoutryName { get; set; }

        public object DefaultIso { get; set; }

        public object DefaultPrefix { get; set; }

        public object DefaultOperatorName { get; set; }

        public int FrozenBalance { get; set; }

        public object TokenApi { get; set; }
    }
}
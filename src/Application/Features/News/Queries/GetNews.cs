using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Server;
using MinimalApiArchitecture.Application.Helpers;
using MinimalApiArchitecture.Application.Model;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinimalApiArchitecture.Application.Features.Users.Queries;

public class GetNews : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getallnews", (IMediator mediator) =>
        {
            return mediator.Send(new GetNewsQuery { });
        })
        .WithName(nameof(GetNews));
    }

    public class GetNewsQuery : IRequest<IResult>
    {
    }

    public class GetNewsHandler(IDistributedCache cache, IHttpContextAccessor httpContextAccessor, ILogger<GetNewsHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetNewsQuery, IResult>
    {
        public async Task<IResult> Handle(GetNewsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var userName = configuration["usernamengraph"];
                var password = configuration["passwordgraph"];

                var cacheTokenKey = $"UserGraph-{userName}";

                var cacheOptions = new DistributedCacheEntryOptions()
                                                        .SetSlidingExpiration(TimeSpan.FromDays(366));

                var cachedToken = await cache.GetStringAsync(cacheTokenKey, cancellationToken);

                var httpClient = httpClientFactory.CreateClient("SimGraphClient");

                var httpTokenClient = httpClientFactory.CreateClient("SimTokenClient");

                using (httpTokenClient)
                {
                    if (string.IsNullOrEmpty(cachedToken))
                    {
                        var formData = new Dictionary<string, string>
                                        {
                                            { "grant_type", "password" },
                                            { "client_id", configuration["client_id"] },
                                            { "client_secret", configuration["client_secret"] },
                                            { "username", userName },
                                            { "password", password }
                                        };

                        var content = new FormUrlEncodedContent(formData);                        

                        using var responseToken = await httpTokenClient.PostAsync("token", content, cancellationToken);

                        if (responseToken.IsSuccessStatusCode)
                        {
                            cachedToken = (await responseToken.Content.ReadFromJsonAsync<ResponseTokenData>(cancellationToken)).Access_token;

                            await cache.SetStringAsync(cacheTokenKey, cachedToken, cacheOptions, cancellationToken);
                        }
                        
                    }

                    var payload = new
                    {
                        query = @"
                                query NewsQuery {
                                      news {
                                        content {
                                          html
                                        }
                                      }
                                    }"
                    };

                    using StringContent jsonContent = new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cachedToken);

                    using var response = await httpClient.PostAsync("", jsonContent, cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<Root>(cancellationToken);

                    return Results.Ok(responseData!.data.news);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("GetNews: {Message}", ex.InnerException);
                return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
            }

        }

    }

    public class Content
    {
        public string html { get; set; }
    }

    public class Data
    {
        public List<News> news { get; set; }
    }

    public class News
    {
        public Content content { get; set; }
    }

    public class Root
    {
        public Data data { get; set; }
    }
}
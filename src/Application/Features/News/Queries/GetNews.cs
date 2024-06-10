using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

    public class GetNewsHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetNewsHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetNewsQuery, IResult>
    {
        public async Task<IResult> Handle(GetNewsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("SimGraphClient");

                var httpContext = httpContextAccessor.HttpContext!;

                var tokenString = ValidateTokenHelper.ValidateAndExtractToken(httpContext, out IResult? validationResult);

                if (validationResult != null)
                {
                    return validationResult;
                }

                using (httpClient)
                {
                    var payload = new
                    {
                        query = @"
                                query NewsQuery {
                                      news {
                                        bodyContent {
                                          html
                                        }
                                        footerContent {
                                          html
                                        }
                                      }
                                    }"
                    };

                    using StringContent jsonContent = new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

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

    public class BodyContent
    {
        public string html { get; set; }
    }

    public class Data
    {
        public List<News> news { get; set; }
    }

    public class FooterContent
    {
        public string html { get; set; }
    }

    public class News
    {
        public BodyContent bodyContent { get; set; }
        public FooterContent footerContent { get; set; }
    }

    public class Root
    {
        public Data data { get; set; }
    }
}
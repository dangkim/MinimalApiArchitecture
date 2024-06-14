using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Helpers;


namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class GetApiKey : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getapikey", (IMediator mediator) =>
        {
            return mediator.Send(new GetApiKeyQuery { });
        })
        .WithName(nameof(GetApiKey));
    }

    public class GetApiKeyQuery : IRequest<IResult>
    {
    }   
    
    public class GetApiKeyHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetApiKeyHandler> logger, IHttpClientFactory httpClientFactory)
        : IRequestHandler<GetApiKeyQuery, IResult>
    {
        public async Task<IResult> Handle(GetApiKeyQuery request, CancellationToken cancellationToken)
        {
            var httpClient = httpClientFactory.CreateClient("SimApiClient");

            using (httpClient)
            {
                try
                {
                    var httpContext = httpContextAccessor.HttpContext!;

                    var tokenString = ValidateTokenHelper.ValidateAndExtractToken(httpContext, out IResult? validationResult);

                    if (validationResult != null)
                    {
                        return validationResult;
                    }

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

                    using var response = await httpClient.GetAsync("GetUserInfo", cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<UserInfoDto>(cancellationToken);

                    var userInfo = new
                    {
                        responseData!.UserId,
                        responseData!.Email,
                        ApiKey = tokenString
                    };

                    return Results.Ok(userInfo);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("GetApiKeyHandler: {Message}", ex.InnerException);
                    return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }
        }
    }

    public class UserInfoDto
    {
        public int UserId { get; set; }
        public string? Email { get; set; }
    }
}
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Helpers;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MinimalApiArchitecture.Application.Features.Users.Queries;

public class GetUserProfile : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getuserprofile", (IMediator mediator) =>
        {
            return mediator.Send(new GetUserProfileQuery { });
        })
        .WithName(nameof(GetUserProfile));
    }

    public class GetUserProfileQuery : IRequest<IResult>
    {
    }

    public class GetUserProfileHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetUserProfileHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetUserProfileQuery, IResult>
    {
        public async Task<IResult> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("SimApiClient");

                var httpContext = httpContextAccessor.HttpContext!;

                var tokenString = ValidateTokenHelper.ValidateAndExtractToken(httpContext, out IResult? validationResult);

                if (validationResult != null)
                {
                    return validationResult;
                }

                using (httpClient)
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

                    using var response = await httpClient.GetAsync("profile", cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<UserProfileResponse>(cancellationToken);

                    return Results.Ok(responseData);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("GetUserProfile: {Message}", ex.InnerException);
                return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
            }

        }

        public class UserProfileResponse
        {
            public decimal Balance { get; set; }
            public string? Email { get; set; }
            public long? UserId { get; set; }
        }
    }
}
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

public class GetEmailConfirmation : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/getemailconfirmation", (IMediator mediator, GetEmailConfirmationQuery query) =>
        {
            return mediator.Send(query);
        })
        .WithName(nameof(GetEmailConfirmation));
    }

    public class GetEmailConfirmationQuery : IRequest<IResult>
    {
        public string? UserId { get; set; }
        public string? Code { get; set; }
    }

    public class GetEmailConfirmationHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetEmailConfirmationHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetEmailConfirmationQuery, IResult>
    {
        public async Task<IResult> Handle(GetEmailConfirmationQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("SimApiClient");

                //var httpContext = httpContextAccessor.HttpContext!;

                //var tokenString = ValidateTokenHelper.ValidateAndExtractToken(httpContext, out IResult? validationResult);

                //if (validationResult != null)
                //{
                //    return validationResult;
                //}

                using (httpClient)
                {
                    var userId = request.UserId;
                    var code = Uri.EscapeDataString(request.Code);

                    var url = $"ConfirmEmailSim?userId={userId}&code={code}";
                    
                    using var response = await httpClient.GetAsync(url, cancellationToken);

                    var responseData = await response.Content.ReadAsStringAsync(cancellationToken);

                    return Results.Ok(responseData);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("GetEmailConfirmation: {Message}", ex.InnerException);
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
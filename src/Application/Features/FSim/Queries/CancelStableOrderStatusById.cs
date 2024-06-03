using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Helpers;
using MinimalApiArchitecture.Application.Model;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static MinimalApiArchitecture.Application.Features.Authentication.Queries.GetFSPrices;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class CancelStableOrderStatusById : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/cancelstableorderstatusbyid/{orderId}", (IMediator mediator, string? orderId) =>
        {
            return mediator.Send(new CancelStableOrderStatusByIdQuery { OrderId = orderId });
        })
        .WithName(nameof(CancelStableOrderStatusById));
    }

    public class CancelStableOrderStatusByIdQuery : IRequest<IResult>
    {
        public string? OrderId { get; set; }
    }

    public class CancelStableOrderStatusByIdHandler(IHttpContextAccessor httpContextAccessor, ILogger<CancelStableOrderStatusByIdHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<CancelStableOrderStatusByIdQuery, IResult>
    {
        public async Task<IResult> Handle(CancelStableOrderStatusByIdQuery request, CancellationToken cancellationToken)
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

                    var url = string.Format("cancelorder/{0}", request.OrderId);

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

                    using var response = await httpClient.GetAsync(url, cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<object>(cancellationToken);

                    return Results.Ok(responseData);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("CancelStableOrderStatusByIdHandler: {Message}", ex.InnerException);
                    return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }
}
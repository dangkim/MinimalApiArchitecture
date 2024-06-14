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
using static MinimalApiArchitecture.Application.Features.Authentication.Queries.GetFSPrices;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class GetStableOrders : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getstableorders/{product?}/{country?}", (IMediator mediator, string? product, string? country) =>
        {
            return mediator.Send(new GetStableQuery { Country = country, Product = product });
        })
        .WithName(nameof(GetStableOrders));
    }

    public class GetStableQuery : IRequest<IResult>
    {
        public string? Country { get; set; }
        public string? Product { get; set; }
    }

    public class GetStableHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetStableHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetStableQuery, IResult>
    {
        public async Task<IResult> Handle(GetStableQuery request, CancellationToken cancellationToken)
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

                    var url = string.Format("stableorderproductcountry/{0}/{1}/{2}", -1, request.Product, request.Country);

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

                    using var response = await httpClient.GetAsync(url, cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<object>(cancellationToken);

                    return Results.Ok(responseData);

                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("GetStable: {Message}", ex.InnerException);
                return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
            }

        }

    }
}
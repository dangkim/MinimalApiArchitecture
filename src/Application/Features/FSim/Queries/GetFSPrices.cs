using Azure.Core;
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

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class GetFSPrices : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getstableprices/{country?}/{product?}", (IMediator mediator, string? country, string? product) =>
        {
            return mediator.Send(new GetFSPricesQuery { Country = country, Product = product });
        })
        .WithName(nameof(GetFSPrices));
    }

    public class GetFSPricesQuery : IRequest<IResult>
    {
        public string? Country { get; set; }
        public string? Product { get; set; }
    }

    public class GetFSPricesHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetFSPricesHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetFSPricesQuery, IResult>
    {
        public async Task<IResult> Handle(GetFSPricesQuery request, CancellationToken cancellationToken)
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

                    var url = string.Format("pricesbycountryandproduct/{0}/{1}/{2}", request.Country ?? "", "any", request.Product ?? "");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

                    using var response = await httpClient.GetAsync(url, cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<object>(cancellationToken);

                    return Results.Ok(responseData);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("GetFSPricesHandler: {0}", ex.Message);
                    return Results.Problem(ex.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }
}
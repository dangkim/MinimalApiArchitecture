using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class GetFSProduct : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getstableproducts/{country}/{op}/{product?}", (IMediator mediator, string country, string op, string? product) =>
        {
            var paramProduct = product ?? "";
            return mediator.Send(new GetFSProductQuery { Country = country, Op = op, Product = paramProduct });
        })
        .WithName(nameof(GetFSProduct));
    }

    public class GetFSProductQuery : IRequest<IResult>
    {
        public string? Country { get; set; }
        public string? Op { get; set; }
        public string? Product { get; set; }
    }

    public class GetFSProductHandler(ILogger<GetFSProductHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetFSProductQuery, IResult>
    {
        public async Task<IResult> Handle(GetFSProductQuery request, CancellationToken cancellationToken)
        {
            // Use IHttpClientFactory to create an instance of HttpClient
            var httpClient = httpClientFactory.CreateClient("SimApiClient");

            // Create an instance of HttpClient
            using (httpClient)
            {
                try
                {
                    var url = string.Format("products/{0}/{1}/{2}", request.Country, request.Op, request.Product);
                    using var response = await httpClient.GetAsync(url, cancellationToken);

                    // Check if the request was successful
                    var responseData = await response.Content.ReadFromJsonAsync<Dictionary<string, FSProduct>>(cancellationToken);

                    return Results.Ok(responseData!.Count == 1 ? responseData.FirstOrDefault().Value : responseData);

                }
                catch (Exception ex)
                {
                    logger.LogWarning("GetFSProductHandler: {0}", ex.Message);
                    return Results.Problem(ex.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }

    public class FSProduct
    {
        public string? Category { get; set; }
        public int Qty { get; set; }
        public decimal Price { get; set; }
    }

}
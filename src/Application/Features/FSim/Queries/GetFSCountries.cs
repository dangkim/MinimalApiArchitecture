using Carter;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class GetFSCountries : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getstablecountries", (IMediator mediator) =>
        {
            return mediator.Send(new GetFSCountriesQuery());
        })
        .WithName(nameof(GetFSCountriesQuery));
    }

    public class GetFSCountriesQuery : IRequest<IResult>
    {
        public string? Country { get; set; }
        public string? Op { get; set; }
        public string? Product { get; set; }
    }

    public class GetFSCountriesHandler(ILogger<GetFSCountriesHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetFSCountriesQuery, IResult>
    {
        public async Task<IResult> Handle(GetFSCountriesQuery request, CancellationToken cancellationToken)
        {
            // Use IHttpClientFactory to create an instance of HttpClient
            var httpClient = httpClientFactory.CreateClient("SimApiClient");

            // Create an instance of HttpClient
            using (httpClient)
            {
                try
                {
                    var url = string.Format("getstablecountries");

                    using var response = await httpClient.GetAsync(url, cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<Dictionary<string, CountryInfo>>(cancellationToken);

                    var productObjects = new Dictionary<string, CountryObject>();

                    foreach (var country in responseData!)
                    {
                        productObjects.Add(country.Value.Text_En!, new CountryObject
                        {
                            Iso = country.Value.Iso!.Keys.FirstOrDefault(),
                            Country = country.Key,
                        });
                    }

                    return Results.Ok(productObjects);

                }
                catch (Exception ex)
                {
                    logger.LogWarning("GetFSCountriesHandler: {0}", ex.Message);
                    return Results.Problem(ex.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }


    public class CountryInfo
    {
        public Dictionary<string, int>? Iso { get; set; }

        public string? Text_En { get; set; }

    }

    public class CountryObject
    {
        public string? Iso { get; set; }

        public string? Country { get; set; }
    }

}
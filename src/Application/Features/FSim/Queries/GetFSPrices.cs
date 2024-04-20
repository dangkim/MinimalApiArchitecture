using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        app.MapGet("api/getstableprices", (IMediator mediator) =>
        {
            return mediator.Send(new GetFSPricesQuery());
        })
        .WithName(nameof(GetFSPrices));
    }

    public class GetFSPricesQuery : IRequest<IResult>
    {
    }

    public class GetFSPricesHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetFSPricesHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetFSPricesQuery, IResult>
    {
        public async Task<IResult> Handle(GetFSPricesQuery request, CancellationToken cancellationToken)
        {
            var httpClient = httpClientFactory.CreateClient("SimApiClient");

            var httpContext = httpContextAccessor.HttpContext;

            var token = httpContext!.Request.Cookies["stk"];

            //token = "CfDJ8A2DtcO3QABGi26y6dufApQZwBtVXhnw5lzVryEzImLmkGpblTZbut68icRaEiZYFdTy72T3LeKRdqy21_O1N-CTHUmAwK4T5tfurjuISpbJ5paEUUR-MzZ7koiFqt7n3rEuiKuhSNpevcO1h1KJaKNHfZl5VvSUicaIj2E16_9dv9yAcK9U94F8C8_4waTYFRU7EQXJTeZnnjKW2KglmdP4APaoAn4NCEMWE8NIwtI-Tn4hgI8wQtyYwzVJ1NpkqA8-JuK8xf_sDinlokHXYzfto2pc-XJYmtnrOB-g6v8vBLkVOvswQrrz_bSyGDzzxvNEenttp-44v8Tp_GLHT3yvlL3EPYW0If-JZyA81Tx9hrfj1s5V2bWKVFYEvXmEaFXfEZD42Inm9Y2z-a5boKcy-mSseju1lcogRFt3KU4FXbG35JRP7HBVdqYlS3kikMRzIUAT6jJ0wC4zX978pXwRAdTfkWmPoAgkgNgLUx3SAr1UD2CtwHgY1yELMLtQk8FRllBidMjy3aOdXESnu37qJnIf3_szzgtKgY_7CcEnVEHcjVptkY8zZFR3_Pr4axYkZNugzhxKuqvG0gCGovnXSP7DNZHZAkeAJk8tpe1iDye1UHzoe5_kHF9mwNRKZZXJSFdFWmsrYjVQ6pvj2EucUZUe9GDRQWPJNUlEOwhRXCeDGBPKti5HLTVzt5imjBcI6V3bATzw26dQ2aJxOuaD1AHCQEEiXjv9PcNvFNmXfwu9nFVzX7S-BZx8hO6rOEy4EPPAS_v-CFltWMyM2SiTXIMGzP3PaBGClAbxS0wbzLWxCYpwAm6OaH48R8DiQ7-RIP5Z_yiKjWdR5VvemSwAwA99PrXytZQz06n0vHbMGohufTkm3E56KOBD6ZaqW7OkpQFYgXS_0tXaQcMdzOtTXlGl1UuC_--X5mh8bXR0v7hUbMzXwU_EkqB17ARBTrhuCk4PG_mleAFJKBoD8IS-9SgUEAhGa2IZ5XyrpxAX7wEj7AhtKAxanvHiLkBXMzFjJ21frCVdxXwkay_fWZ2OH4ZoeaaDOKSxo91j_hzyQPeb2Uy06ED1Ve2hpPmzIzjqlA86JLHqbEET1Z6L0GyT42TDFFnTv9ncYtfyo0RivFnBAg-uYJWVK0ildAAJAm6qdG5y5J4lNRwKvTgFQkXvlIIROkls0pW0FZ-Il55D8WwyZGPrhKYhQCVtGsYooPZy7YZh_NNum7NlqlEmj6GVS_xwEw3pMD-22lwnBX6nc_Tma7vjqoSrgyj_aMB4m0BDn3HtBgROaGCBL0oAFZeaNNwPDBzOHEkkFxIOSF1__8CPSZzlD0C2HyniZwZf2TV5brkW67oHGAGzMOiF3On4d4eGgmE9eDx57SDaFmNj5W-VvekY6YId_8G7pFoifhyjAWCidwtwaSodGJqoW4tv_zYJMesdGYabQc9K91lxawvtSX6yYPqefS9aQXHOR-1T5FguDflaPpzyvam1pLXsunakhm62BNbydjmO7hG7G1v4Bs7_cojYJmnlk4546uBJ4T-dIZEUbUNovjSMItyOX1BWDwXO3GESnyuRvdEhqiAe0haF-WmueqSxi-GXrQ";
            
            using (httpClient)
            {
                try
                {
                    var url = string.Format("prices");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    
                    using var response = await httpClient.GetAsync(url, cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<object>(cancellationToken: cancellationToken);
                    //using var responseStream = await response.Content.ReadAsStreamAsync();

                    // Read response content as stream
                    //using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    // Deserialize the JSON stream into an object
                    //var responseObject = await JsonSerializer.DeserializeAsync<object>(stream, cancellationToken: cancellationToken);

                    // Use the deserialized object
                    //Console.WriteLine(responseObject.Property1);
                    //Console.WriteLine(responseObject.Property2);
                    // Access other properties as needed

                    return Results.Ok(responseData);

                    // Process or return the complete response content


                    //return Results.Ok(responseData);

                }
                catch (Exception ex)
                {
                    logger.LogWarning("GetFSPricesHandler: {0}", ex.Message);
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
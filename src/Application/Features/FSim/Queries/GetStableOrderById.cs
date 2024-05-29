using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

public class GetStableOrderById : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getstableorderbyid/{orderId}", (IMediator mediator, string? orderId) =>
        {
            return mediator.Send(new CheckOrderQuery { OrderId = orderId });
        })
        .WithName(nameof(GetStableOrderById));
    }

    public class CheckOrderQuery : IRequest<IResult>
    {
        public string? OrderId { get; set; }
    }

    public class GetStableOrderByIdHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetStableOrderByIdHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<CheckOrderQuery, IResult>
    {
        public async Task<IResult> Handle(CheckOrderQuery request, CancellationToken cancellationToken)
        {
            var httpClient = httpClientFactory.CreateClient("SimApiClient");

            var httpContext = httpContextAccessor.HttpContext;

            var tokenString = httpContext!.Request.Cookies["stk"];

            var tokenObject = JsonSerializer.Deserialize<Token>(tokenString!);

            using (httpClient)
            {
                try
                {
                    var url = string.Format("checkorder/{0}", request.OrderId);

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenObject!.access_token);

                    using var response = await httpClient.GetAsync(url, cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<object>(cancellationToken);

                    return Results.Ok(responseData);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("GetStableOrderByIdHandler: {Message}", ex.Message);
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
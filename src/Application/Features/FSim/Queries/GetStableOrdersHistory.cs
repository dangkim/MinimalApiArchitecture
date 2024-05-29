using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Model;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static MinimalApiArchitecture.Application.Features.Authentication.Queries.GetFSPrices;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class GetStableOrdersHistory : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getstableordershistory", async (IMediator mediator, [FromQuery] string date, [FromQuery] int limit, [FromQuery] int offset, [FromQuery] string order, [FromQuery] string phone, [FromQuery] bool reverse, [FromQuery] string status, [FromQuery] string product) =>
        {
            return await mediator.Send(new GetStableOrdersHistoryQuery
            {
                Date = date,
                Limit = limit,
                Offset = offset,
                Order = order,
                Phone = phone,
                Reverse = reverse,
                Status = status,
                Product = product
            });
        })
        .WithName(nameof(GetStableOrdersHistory));
    }

    public class GetStableOrdersHistoryQuery : IRequest<IResult>
    {
        public string? Date { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string? Order { get; set; }
        public string? Phone { get; set; }
        public bool? Reverse { get; set; }
        public string? Status { get; set; }
        public string? Product { get; set; }
    }

    public class GetStableOrdersHistoryHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetStableOrdersHistoryHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetStableOrdersHistoryQuery, IResult>
    {
        public async Task<IResult> Handle(GetStableOrdersHistoryQuery request, CancellationToken cancellationToken)
        {
            var httpClient = httpClientFactory.CreateClient("SimApiClient");

            var httpContext = httpContextAccessor.HttpContext;

            var tokenString = httpContext!.Request.Cookies["stk"];

            var tokenObject = JsonSerializer.Deserialize<Token>(tokenString!);

            using (httpClient)
            {
                try
                {
                    var url = string.Format("stableordershistory?date={0}&limit={1}&offset={2}&order={3}&reverse={4}&phone={5}&status={6}&product={7}"
                                            , request.Date
                                            , request.Limit
                                            , request.Offset
                                            , request.Order
                                            , request.Reverse
                                            , request.Phone
                                            , request.Status
                                            , request.Product);

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenObject!.access_token);

                    using var response = await httpClient.GetAsync(url, cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<object>(cancellationToken);

                    return Results.Ok(responseData);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("GetStableOrdersHistory: {Message}", ex.Message);
                    return Results.Problem(ex.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }
}
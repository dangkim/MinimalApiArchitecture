using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Helpers;
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

public class GetPaymentsHistory : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getpaymentshistory", async (IMediator mediator, [FromQuery] string date, [FromQuery] int limit, [FromQuery] int offset, [FromQuery] string order, [FromQuery] string paymentprovider, [FromQuery] bool reverse, [FromQuery] string paymenttype) =>
        {
            return await mediator.Send(new GetPaymentsHistoryQuery
            {
                Date = date,
                Limit = limit,
                Offset = offset,
                Order = order,
                PaymentProvider = paymentprovider,
                Reverse = reverse,
                PaymentType = paymenttype
            });
        })
        .WithName(nameof(GetPaymentsHistory));
    }

    public class GetPaymentsHistoryQuery : IRequest<IResult>
    {
        public string? Date { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string? Order { get; set; }
        public string? PaymentProvider { get; set; }
        public bool? Reverse { get; set; }
        public string? PaymentType { get; set; }
    }

    public class GetPaymentsHistoryHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetPaymentsHistoryHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetPaymentsHistoryQuery, IResult>
    {
        public async Task<IResult> Handle(GetPaymentsHistoryQuery request, CancellationToken cancellationToken)
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
                try
                {
                    var url = string.Format("getpayments?date={0}&limit={1}&offset={2}&order={3}&reverse={4}&payment_provider={5}&payment_type={6}"
                                            , request.Date
                                            , request.Limit
                                            , request.Offset
                                            , request.Order
                                            , request.Reverse
                                            , request.PaymentProvider
                                            , request.PaymentType);

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

                    using var response = await httpClient.GetAsync(url, cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<object>(cancellationToken);

                    return Results.Ok(responseData);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("GetPaymentsHistory: {Message}", ex.InnerException);
                    return Results.Problem("Connection Error", "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }
}
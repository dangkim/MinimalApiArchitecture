using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Domain.Entities;
using MinimalApiArchitecture.Application.Features.Products.EventHandlers;
using MinimalApiArchitecture.Application.Helpers;
using MinimalApiArchitecture.Application.Infrastructure.Persistence;
using MinimalApiArchitecture.Application.Model;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class UpdateBalanceByAdmin : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/updatebalancebyadmin", (IMediator mediator, UpdateBalanceByAdminQuery query) =>
        {
            return mediator.Send(query);
        })
        .WithName(nameof(UpdateBalanceByAdmin));
    }

    public class UpdateBalanceByAdminQuery : IRequest<IResult>
    {
        public int PaymentId { get; set; }
        public string? TypeName { get; set; }
        public string? ProviderName { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Email { get; set; }
        public long UserId { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? Method { get; set; }

    }

    public class UpdateBalanceByAdminHandler(IHttpContextAccessor httpContextAccessor, ILogger<UpdateBalanceByAdminHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<UpdateBalanceByAdminQuery, IResult>
    {
        public async Task<IResult> Handle(UpdateBalanceByAdminQuery request, CancellationToken cancellationToken)
        {
            var httpClient = httpClientFactory.CreateClient("SimApiClient");

            //var httpContext = httpContextAccessor.HttpContext;

            //var tokenString = httpContext!.Request.Cookies["stk"];

            //var tokenObject = JsonSerializer.Deserialize<Token>(tokenString!);

            // Serialize the request body to JSON
            var requestBodyJson = JsonSerializer.Serialize(request);

            var token = "CfDJ8DqZyODmnbREmJPA-xk0vsD3Ymge20sdMqhrPpzhgaLQj94eWGcc6-QDG6K-bZlOA0NGilIsQ78bytNqUHUT2rXZx0mvBlBiPnpLdf6_K2CfBdnnk6UVUAH7mBqYfUsOloLBknGv821hWynSvXv1YmHJrJUqdhfEV9diY58VIMa4ZunAazZAapOYwkWjx7vNeAWkBOHKrBz3RWySpLCqkJ9DfNNMh4aGxbTuZYPSYKr6BlFPVqhCTfCgQL5A8R3ziUCLX_B-ldQeFUsMkxI9sRIpJoLhygANHTbjCJmXAg2i69yDyrxBIUgyettqm-YOkR8lWMn1r4nT4MhJ35P90hJ5L1hbx6aoaxOgjAqmz4DRpmAbRs2AD3lray6-NxCcU6jHUoB27Fbt9gOdmSw2Sj__v3z7R_tkzfspVdRlqHANME36jVPdWg2CA7hjTF9EJ5AfkDDrpcUHV2CIoybnuszoCpoiAiSsVUAHM_l-4w450wQfw4sfmCnhGuKT211M0NBxlIADXCvu37b3VpEbEtHhM2NjGnwtRjvlmhGY4sRwmP0n5bckd_nxuTpvvfXVL2-9T-uhGDfmo2ri73zIHv_3KxMPgUTsv5UztvNDSD0ej39RbzR4d41eokfnx6LH92Z__9rx2Gi8N6CgY8wiBOpIBRo7FC7_tLWBhBVCEm9tvO87fIf8mayqDx8q7fgA28u9GTPmP6fT-ld8NEVRxqmukicKrfUqdZe6MEJJLt6tQ0H68b_euA-L52U92QLFiDti5wXd9dqRf3WYrV1nPwxwFVSqcox-W2QGbotoMi9uSBhi1j4Nf3bc-inyleL7yZQeggXAelE3bzCkDn-58fs3x4FuPrbQ5vqdwTn1U4eTU_xiAsQdGCjxd-mdGm6aZSt-yeT3SzRTQ98idk9rbgN5IxZFZeWFuHmJW9f92dVISoBeD66aG2CpOweWue_uf0FSSjfFwVGFsx2wwQLwl1ftcwwZ9toAQMLvAj2XNtog5CTKoJ2BJ7j_qzp1c73-YSX0gdA-caKwrHaXpSPpA2EcR08yfIBnW6nQg-YDN19LBDkyaVexof8Zl_FHgS9A1PNndVUWzeA0585oZY_Ct8RZSzQuHOYIhFzUtSScYB2lU3YVwaQTYHm9XLNo1FhslwQI3QpdWMwqB36ZbjvbnZ-7Y1X9UcmI_d0L9dJ-lDgD_BVJ2sALjePx-1qN7SbeAhuvmm9ciAmhswsvNKEjKYT7rBMxr_kt4YoHDeEP38qENDvnzQkxrxwCMP05fsTNuqqmcyoXwYTHVJ1YEO8r8fzQ9vN5nM85-ASAyMCsvZXImCSFvQmdHICDcWwAv6X_JlAT9MDbPc4S_kbgHSrEHczzxLuBQfpYY_PwCgpMd1KlrmTCX24ZOkRD1fp4C6vPKbN2zK-lywKEU8oeZPQwQKr1Q8qM38-MHZv127A4vWa18TIdbevEPCJkbwKANy1P2uktCa6WkWuezr44wlT6BmBHDnvTUKVa45qcFw3S92PgBJ-UdCeYYDU0FhqTLZnqJzjUI_73Ca92QakSy-RuBMSXbq9nrmRqjvq9njgQt4laExA3SbD8s0EAZt8pr5dVhOfO4AsWgixBFz8tlHKSinszt83fJDoKAYp8jaxBMG8QDiLeBvi7is-Be3_UPC5E9UAgJDSSoSmfNtKc5RtIONDHrLiFkUg0Tp_0gHtEHWRwMEFNmzYsqQLYwKKilqAxVDxeHzhxI2_A2GcmsA8p2ZtKUFfqcKwlZ1W4tMWzhUZ5nKGLpeNAwP5-BjxTRlMFRGB12YgXxGRewCrKZ7RrIky0GSOh1YZmgIdqmuIoEmWPGNMM4uL2WaVWdD_vZsr1F-lX3TrVdCkIjpSiM1Y29xYA8gdyvbk56zIdBU4qx4aMArVd8EOQWLxEHvo8_oI40Uc6wy3n6OzPXUdW-Er6KG532G2o1Il826dWpyBLZar85ABd-xrbgjJ-_Kxo6J9z3gT5zBhRzuwpGPLZwDZDAN2vxEiS2r32eabWQd2m9vEsMpPiDe-nn0aZNgLJcJTfX_0xXtPjW7qU46qTXkITb2rzjUNXQ5tnLurQ8IFL1ReX9bpyFOGUReLofV36gUpdF3sP6m12t6iuo5UfS2ca0FHfog8lo5jGHvjuS61vgMBhBjwW-ZbExFbXvsrG-Ff0a5DWaKgwYbdFG8SPp3G1Sy8ogmAMXTSPrPFj_IF0oN5umNR17cvfnJ-gVHM-DO4gN4mHXctxqih9TjrnfHWax_H4ZVzuNA-o0u0u3al3NLHeXaE6KtuouuYGabS0woh-Xj5DrxFowU4T_7KHHdAsvE5_PsZ8MqPsKWNx0WZVpZUtEYm1uFSFcY99sJ_jpz8byQ";

            // Create an instance of HttpClient
            using (httpClient)
            {
                try
                {
                    // Create an instance of HttpContent with the serialized request body
                    var httpContent = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    
                    // Send a POST request with the HttpContent
                    HttpResponseMessage response = await httpClient.PostAsync("UpdateBalanceByAdmin", httpContent, cancellationToken);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a string
                        var responseData = await response.Content.ReadFromJsonAsync<bool>(cancellationToken: cancellationToken);

                        return Results.Ok(responseData);
                    }
                    else
                    {
                        string responseData = await response.Content.ReadAsStringAsync(cancellationToken);
                        var responseObject = JsonSerializer.Deserialize<ProblemDetails>(responseData);
                        return Results.Problem(responseObject!);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning("UpdateBalanceByAdminHandler: {0}", ex.Message);
                    return Results.Problem(ex.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }
}
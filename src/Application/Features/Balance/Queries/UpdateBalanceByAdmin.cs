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

            var token = "CfDJ8DqZyODmnbREmJPA-xk0vsBt78X0KUdhobWPV5St5v1BgOf87j_QGwmC-6cYt5i8X-0M37dMD5wjHdbnZSAQF7qNEoatMvtTmpD32jwFvZEvnZW6lUl8fhJASvtWwzJlt3S3NDAh0JCWTM0TDL1x8wIY0UYePLyThZnc95aerPi7FweHaDOzmPXrz0D0Zt6crFe4WEJPLLv_dn7EwiiwJZkVUc7gUF7Q7yv8LFG2MAd2C15buskStLFiZqCZqpNDvbLX7n4r3BrAM4VxlSwa2XNrVYNPIyYFghxBOHEwb7yZFEabvOym1e8ckTLjCccGT0uuSagFyzdpb3cp_jQ0HLX5akKrqviS46WaylOK7vh8kWkhwHOOD9IYr7Ez6C4g5NN3dXzkwI0KcHU2ZXpxWXE_AtpnmPEinhDZxILlb0FPiIfeoM0yKND3j7hI3tQHDS1t76iQ9PwN_Zc209i350fBVUTntoQKE6AR5P460AJXqNG-JH4OwQs9Uj4ZR1NuDP4luBUCgEejH-GYyRm5MrAGcHzmyCSrk0mjhcAg5AdgjsikHHKUOlYK5oKvnz9ZYomCcRK81l7WT-tS34Lk9FK65CVxlMhuc7xcK-m6kf8TI3ij57QNim_QGqEUs2kg405GQWoxzMYq3ONAGjyIMuPpmrXC9n0mAIM0zpeCvh6HZDHWlQA91OiUVoqUD3mL1MI5tdCu31Tek7NP2VmdbgvozK4yHnS3mERC2uxfhHxqLFZ7aLx6yz843w9obkVabbtrDSDt1y_GitQd3E_er-U1gteEazJV8uzUg-_QXMSpuCd1799ev91yqIkboh_4tXCwTU-XO2u9LZH9BGDpqEgwBjG0KDt1KF4xVz7ddNOGyspOElSS4jCrahynnuV0fR7SXP6Zhsn5HZsaIkFkhzg99AN4ss-xcWjMYPdGRvfk8H2RZR1ooPJlinuLqaRLSIc45SiA54qE-h53GK2IbrRLSBHUyn_lXyuFhomMcP89nhBMZmel4X1Mt93tr2BcqOB3BgYlw3Jc5cBH1f_Fmu7uTXqqfDy_soYcrLz2VwV7BJHcfugfA3JTeP6_fXRCrIhyxKgWH9CC5C6VeD2_xhQZARV20bziioTxyAGNOJz5ENJ3aRkmy_nWkjx85sIl8KKwR9f3nN_qCw3mW48ll-zjrVdZ9HZPqpi6ACUnFBSafleZsWGXbFVMhLEiy8cK-QiQergY17F5QDBZ2eR6ZsN5ScYOvvgS-rWZBxbUqQEnmF8pIAtzMbhHSX6SGjbd6jYqOYh1YFNCEVmVcIrwVbnLT0F4wHEPmQORpXcFPtn35PvOPerzeoaNTX8X6pbYRHsg1UxmOqoKZlzVYIWcyrU0NaXrl0PzYiNEmLOsrBuVSA4DkMVGy66UsChEfkasohVli64fSNHRtYZ18bqoJryrbJg6pjxQI32HDoLHfhY0oTl8IyyOhmhqG9oTEEDiGmhzLYvsypACEeUj_RlN2L467tnv_vOzegwmrf6bdoO5OZWX9cx8jp-oBy1RFuJhGEgNYKBogBzjdAjZNGHmGW9LAWrFy24knctd7y0bW2YKB5tX8PLpKpz1QuC0QTT-UGvY2ykTOEV99I7-BAWU9yeo-jcFFJhkR8FBiqG5vPHd2DbswXNMj6agb82O4hdibqhVRWRQs6Ga8FIewxkggRHgRFdjwD8ZnPGxk-3MCHGPOVvTZAK9le7CKV_dc5uS2nbj0Y_tiXlF8aDdwq5O8yCoVUPIwLOTi_tzRrbmYAas_VOS3ZcskctqiwSp2GpNle6wpQxtIj37aX0GulYRj54cboUd7alSrgdKO79oVnh3S7Lk-nL5G6uR8uXwCLNcEelM8G8ll2LwISpx1U0xW-4yy6xMHdGaKooCNH1QhPocvZy1ucLBc25DxtPm8SRxF7eRFYPv79qfHyjQcHWShiQzvweHEUMUoZbCPmzo75FNsZfsjAPOc4pigXnyEkq6fD_wjG-an_k-90C-ANNU9_OjOjPv7U1enRLYFO4WXvj3QV29xuA1ov0pdf3chPRutI_XQLeFuuEpJHOUUIcR-8AwBbFWIYqxvmhmzaaOGPLN1Q4tiJGV0P914t41uOtv0AJna0T0pZCwTB4b-cxYXvBQlwHpHH5ma96VsKgVOrE8JAdq0DW17rZ-NVX9Eif-QS6EGNtvpN-ZJ41CRWyLpC0jR64fqeRLeLSGKsURc6WrJAc1qK08Lo9R4j4-T7p0JnhefPHst59tK4CUy1_9ZJGeOXfHvdq5nqEmfiWJ8wuzf3OoQhdz3KcC1WCmIp1fQTusboUoqZmtN30yy5W723D25OIIHKmerANJ__FyRdKqHrx_YF8ZYGr0_4tcD8M4eA";
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
                    logger.LogWarning("UpdateBalanceByAdminHandler: {0}", ex.InnerException);
                    return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }
}
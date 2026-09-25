using Deliver.Billing.Application;
using Deliver.Billing.Application.Features.GetInvoice;
using Deliver.Billing.Infrastructure;
using Deliver.ServiceDefaults;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("billing-service");
builder.Services.AddOpenApi();
builder.Services.AddBillingApplication();
builder.Services.AddBillingInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.MapOpenApi();
app.MapDefaultEndpoints();

var billing = app.MapGroup("/api/billing/shipments").WithTags("Billing");

// 404 right after creating a shipment is expected: the invoice appears once Billing has consumed ShipmentCreated.
billing.MapGet("/{shipmentId:guid}", async Task<Results<Ok<BillingSummary>, NotFound>> (
        Guid shipmentId, ISender sender, CancellationToken ct) =>
        await sender.Send(new GetInvoiceQuery(shipmentId), ct) is { } invoice ? TypedResults.Ok(invoice.ToSummary()) : TypedResults.NotFound())
    .WithName("GetBillingSummary");

billing.MapGet("/{shipmentId:guid}/invoice", async Task<Results<Ok<InvoiceDetails>, NotFound>> (
        Guid shipmentId, ISender sender, CancellationToken ct) =>
        await sender.Send(new GetInvoiceQuery(shipmentId), ct) is { } invoice ? TypedResults.Ok(invoice) : TypedResults.NotFound())
    .WithName("GetInvoice");

await app.Services.MigrateBillingDatabaseAsync();
await app.RunAsync();

public partial class Program;

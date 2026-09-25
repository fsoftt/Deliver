using Deliver.Billing.Domain.Invoices;
using Deliver.Contracts.Billing;
using Deliver.Messaging;
using Deliver.SharedKernel;

namespace Deliver.Billing.Application.IntegrationEvents;

public sealed class PublishBillingIntegrationEvents(IOutbox outbox) :
    IDomainEventHandler<DeliveryPriceCalculatedDomainEvent>,
    IDomainEventHandler<PaymentCapturedDomainEvent>,
    IDomainEventHandler<PaymentFailedDomainEvent>
{
    public Task HandleAsync(DeliveryPriceCalculatedDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new DeliveryPriceCalculatedIntegrationEvent(e.ShipmentId, e.InvoiceId, e.Price.Amount, e.Price.Currency, e.OccurredAt));
        return Task.CompletedTask;
    }

    public Task HandleAsync(PaymentCapturedDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new PaymentCapturedIntegrationEvent(e.ShipmentId, e.InvoiceId, e.CustomerId, e.Amount.Amount, e.Amount.Currency, e.OccurredAt));
        return Task.CompletedTask;
    }

    public Task HandleAsync(PaymentFailedDomainEvent e, CancellationToken cancellationToken)
    {
        outbox.Enqueue(new PaymentFailedIntegrationEvent(e.ShipmentId, e.InvoiceId, e.CustomerId, e.Amount.Amount, e.Amount.Currency, e.Reason, e.OccurredAt));
        return Task.CompletedTask;
    }
}

using Deliver.SharedKernel;

namespace Deliver.Billing.Domain.Invoices;

public enum InvoiceStatus
{
    /// <summary>Priced when the shipment was created; charged once it is delivered.</summary>
    Open,
    Paid,
    PaymentFailed,
    Voided,
}

public sealed record DeliveryPriceCalculatedDomainEvent(Guid InvoiceId, Guid ShipmentId, Money Price, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record PaymentCapturedDomainEvent(Guid InvoiceId, Guid ShipmentId, Guid CustomerId, Money Amount, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record PaymentFailedDomainEvent(Guid InvoiceId, Guid ShipmentId, Guid CustomerId, Money Amount, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Outcome reported by the payment provider.</summary>
public sealed record PaymentResult(bool Succeeded, string? ProviderReference, string? FailureReason)
{
    public static PaymentResult Success(string reference) => new(true, reference, null);

    public static PaymentResult Declined(string reason) => new(false, null, reason);
}

/// <summary>A charge attempt against the customer. Kept for auditing even when it fails.</summary>
public sealed class PaymentAttempt : Entity<Guid>
{
    private PaymentAttempt()
    {
    }

    public decimal Amount { get; private set; }
    public bool Succeeded { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset AttemptedAt { get; private set; }

    internal static PaymentAttempt From(Money amount, PaymentResult result, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        Amount = amount.Amount,
        Succeeded = result.Succeeded,
        ProviderReference = result.ProviderReference,
        FailureReason = result.FailureReason,
        AttemptedAt = now,
    };
}

/// <summary>
/// What the customer owes for one shipment. Core invariant: an invoice is charged successfully at
/// most once, so a duplicated ShipmentDelivered can never charge the customer twice.
/// </summary>
public sealed class Invoice : AggregateRoot<Guid>
{
    private readonly List<PaymentAttempt> _payments = [];

    private Invoice()
    {
    }

    public Guid ShipmentId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Money Price { get; private set; } = null!;
    public InvoiceStatus Status { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? VoidedAt { get; private set; }
    public IReadOnlyCollection<PaymentAttempt> Payments => _payments.AsReadOnly();
    public uint Version { get; private set; }

    public static Invoice Open(Guid shipmentId, Guid customerId, Money price, DateTimeOffset now)
    {
        var invoice = new Invoice
        {
            Id = Guid.CreateVersion7(now),
            ShipmentId = Ensure.NotEmpty(shipmentId, "Shipment id"),
            CustomerId = Ensure.NotEmpty(customerId, "Customer id"),
            Price = price,
            Status = InvoiceStatus.Open,
            IssuedAt = now,
        };
        invoice.Raise(new DeliveryPriceCalculatedDomainEvent(invoice.Id, shipmentId, price, now));
        return invoice;
    }

    /// <summary>Guard to call BEFORE contacting the payment provider.</summary>
    public void EnsureCanBeCharged()
    {
        if (Status != InvoiceStatus.Open)
            throw new DomainException($"Cannot charge an invoice that is {Status}.");
    }

    public void RecordPayment(PaymentResult result, DateTimeOffset now)
    {
        EnsureCanBeCharged();
        _payments.Add(PaymentAttempt.From(Price, result, now));

        if (result.Succeeded)
        {
            Status = InvoiceStatus.Paid;
            PaidAt = now;
            Raise(new PaymentCapturedDomainEvent(Id, ShipmentId, CustomerId, Price, now));
        }
        else
        {
            Status = InvoiceStatus.PaymentFailed;
            Raise(new PaymentFailedDomainEvent(Id, ShipmentId, CustomerId, Price, result.FailureReason ?? "Declined", now));
        }
    }

    /// <summary>Compensation for a cancelled shipment: nothing will be charged.</summary>
    public void Void(DateTimeOffset now)
    {
        if (Status == InvoiceStatus.Voided)
            return;
        if (Status == InvoiceStatus.Paid)
            throw new DomainException("A paid invoice cannot be voided; it needs a refund.");

        Status = InvoiceStatus.Voided;
        VoidedAt = now;
    }
}

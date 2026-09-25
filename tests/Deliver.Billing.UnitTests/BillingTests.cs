using Deliver.Billing.Domain.Invoices;
using Deliver.Billing.Domain.Pricing;
using Deliver.SharedKernel;

namespace Deliver.Billing.UnitTests;

public class BillingTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(1, 1, 5500)]      // 4000 + 1 kg
    [InlineData(5.25, 3, 14000)]  // 4000 + 6 started kg + 2 extra items
    [InlineData(0.1, 1, 5500)]    // every started kilogram is charged
    public void Price_is_base_fee_plus_started_kilograms_plus_extra_items(decimal kg, int items, decimal expected)
    {
        var price = DeliveryPricing.Calculate(kg, items, Tariff.Standard);

        price.ShouldBe(new Money(expected, "CLP"));
    }

    [Fact]
    public void Opening_an_invoice_publishes_the_calculated_price()
    {
        var invoice = Invoice.Open(Guid.NewGuid(), Guid.NewGuid(), new Money(12500, "CLP"), Now);

        invoice.Status.ShouldBe(InvoiceStatus.Open);
        invoice.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DeliveryPriceCalculatedDomainEvent>()
            .Price.Amount.ShouldBe(12500);
    }

    [Fact]
    public void A_successful_payment_marks_the_invoice_paid()
    {
        var invoice = Invoice.Open(Guid.NewGuid(), Guid.NewGuid(), new Money(12500, "CLP"), Now);
        invoice.ClearDomainEvents();

        invoice.RecordPayment(PaymentResult.Success("ref-1"), Now);

        invoice.Status.ShouldBe(InvoiceStatus.Paid);
        invoice.Payments.ShouldHaveSingleItem().Succeeded.ShouldBeTrue();
        invoice.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PaymentCapturedDomainEvent>();
    }

    [Fact]
    public void Customer_is_never_charged_twice()
    {
        var invoice = Invoice.Open(Guid.NewGuid(), Guid.NewGuid(), new Money(12500, "CLP"), Now);
        invoice.RecordPayment(PaymentResult.Success("ref-1"), Now);

        Should.Throw<DomainException>(() => invoice.EnsureCanBeCharged());
        Should.Throw<DomainException>(() => invoice.RecordPayment(PaymentResult.Success("ref-2"), Now));
        invoice.Payments.Count.ShouldBe(1);
    }

    [Fact]
    public void A_declined_payment_is_recorded_and_published()
    {
        var invoice = Invoice.Open(Guid.NewGuid(), Guid.NewGuid(), new Money(250000, "CLP"), Now);
        invoice.ClearDomainEvents();

        invoice.RecordPayment(PaymentResult.Declined("Card limit"), Now);

        invoice.Status.ShouldBe(InvoiceStatus.PaymentFailed);
        invoice.Payments.ShouldHaveSingleItem().FailureReason.ShouldBe("Card limit");
        invoice.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PaymentFailedDomainEvent>().Reason.ShouldBe("Card limit");
    }

    [Fact]
    public void Cancelled_shipments_void_their_open_invoice()
    {
        var invoice = Invoice.Open(Guid.NewGuid(), Guid.NewGuid(), new Money(12500, "CLP"), Now);

        invoice.Void(Now);
        invoice.Void(Now);

        invoice.Status.ShouldBe(InvoiceStatus.Voided);
        Should.Throw<DomainException>(() => invoice.EnsureCanBeCharged());
    }

    [Fact]
    public void A_paid_invoice_cannot_be_voided()
    {
        var invoice = Invoice.Open(Guid.NewGuid(), Guid.NewGuid(), new Money(12500, "CLP"), Now);
        invoice.RecordPayment(PaymentResult.Success("ref-1"), Now);

        Should.Throw<DomainException>(() => invoice.Void(Now));
    }
}

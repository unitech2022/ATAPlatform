using ATA.Domain.Common;

namespace ATA.Domain.Passengers;

/// <summary>Row of the <c>passengers</c> table.</summary>
public class PassengerProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public decimal RatingAvg { get; set; } = 5.00m;
    public int RatingCount { get; set; }
    public bool PreferFemaleDriver { get; set; }
    public PaymentMethodKind DefaultPaymentMethod { get; set; } = PaymentMethodKind.Cash;
    /// <summary>Default saved card when <see cref="DefaultPaymentMethod"/> is <c>card</c>.</summary>
    public Guid? DefaultPaymentMethodId { get; set; }

    public ICollection<SavedPlace> SavedPlaces { get; set; } = [];
}

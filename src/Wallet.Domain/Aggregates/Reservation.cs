using Wallet.Domain.Common;
using Wallet.Domain.Enums;

namespace Wallet.Domain.Aggregates;

public class Reservation : BaseEntity
{
    public long WalletId { get; private set; }
    public DomainWalletServiceType ServiceType { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime ExpireAt { get; private set; }
    public ReservationStatus Status { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpireAt;

    private Reservation() { }

    public Reservation(long walletId, DomainWalletServiceType serviceType, decimal amount, DateTime expireAt)
    {
        WalletId = walletId;
        ServiceType = serviceType;
        Amount = amount;
        ExpireAt = expireAt;
        Status = ReservationStatus.Created;
    }

    public bool CanConfirm()
        => Status == ReservationStatus.Created && !IsExpired;

    public bool CanCancel()
        => Status == ReservationStatus.Created || IsExpired;

    public void MarkConfirmed()
    {
        if (!CanConfirm())
            throw new InvalidOperationException("Reservation cannot be confirmed.");

        Status = ReservationStatus.Confirmed;
        
        MarkAsModified();
    }

    public void MarkCancelled()
    {
        if (!CanCancel())
            throw new InvalidOperationException("Reservation cannot be cancelled.");

        Status = ReservationStatus.Cancelled;
        
        MarkAsModified();
    }

    public void MarkExpired()
    {
        if (Status != ReservationStatus.Created)
            throw new InvalidOperationException("Only active reservations can expire.");

        Status = ReservationStatus.Expired;
        
        MarkAsModified();
    }
}

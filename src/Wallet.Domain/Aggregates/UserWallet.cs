using Wallet.Domain.Common;
using Wallet.Domain.Enums;
using Wallet.Domain.Events.Ledger;
using Wallet.Domain.Events.Promotion;
using Wallet.Domain.Events.Reservation;
using Wallet.Domain.Events.Wallet;

namespace Wallet.Domain.Aggregates;

public class UserWallet : AggregateRoot
{
    public Guid UserId { get; private set; }

    public decimal AvailableBalance { get; private set; }
    public decimal ReservedBalance { get; private set; }

    private readonly List<PromoGrant> _promoGrants = [];
    public IReadOnlyCollection<PromoGrant> PromoGrants => _promoGrants.AsReadOnly();

    private readonly List<Reservation> _reservations = [];
    public IReadOnlyCollection<Reservation> Reservations => _reservations.AsReadOnly();

    private readonly List<LedgerTransaction> _ledgerTx = [];
    public IReadOnlyCollection<LedgerTransaction> LedgerTransactions => _ledgerTx.AsReadOnly();

    private UserWallet()
    {
    }

    public UserWallet(Guid userId)
    {
        UserId = userId;
    }

    private void ApplyBalanceChange(decimal amount)
    {
        AvailableBalance += amount;
        AddDomainEvent(new WalletBalanceChanged(Wallet: this, AmountChanged: amount));
    }

    public LedgerTransaction TopUp(decimal amount, string idem)
    {
        ValidatePositive(v: amount);

        ApplyBalanceChange(amount: amount);

        var tx = LedgerTransaction.TopUp(walletId: Id, amount: amount, idem: idem);
        _ledgerTx.Add(tx);

        AddDomainEvent(new LedgerTransactionCreated(UserId: UserId, Transaction: tx));

        return tx;
    }

    public LedgerTransaction Pay(DomainWalletServiceType serviceType, decimal amount, string idem)
    {
        ValidatePositive(v: amount);

        if (AvailableBalance < amount)
            throw new InvalidOperationException("Insufficient balance.");

        ApplyBalanceChange(amount: -amount);

        var tx = LedgerTransaction.Payment(walletId: Id, serviceType: serviceType, amount: amount, idem: idem);
        _ledgerTx.Add(tx);

        AddDomainEvent(new LedgerTransactionCreated(UserId: UserId, Transaction: tx));

        return tx;
    }

    public Reservation CreateReservation(DomainWalletServiceType serviceType, decimal amount, DateTime expireAt, string idem)
    {
        ValidatePositive(v: amount);

        if (AvailableBalance < amount)
            throw new InvalidOperationException("Insufficient balance.");

        ApplyBalanceChange(amount: -amount);
        ReservedBalance += amount;

        var reservation = new Reservation(walletId: Id, serviceType: serviceType, amount: amount, expireAt: expireAt);
        _reservations.Add(reservation);

        var tx = LedgerTransaction.Hold(walletId: Id, reservationId: reservation.Id, serviceType: serviceType, amount: amount, idem: idem);
        _ledgerTx.Add(tx);

        AddDomainEvent(new ReservationCreated(UserId: UserId, WalletId: Id, Reservation: reservation));
        AddDomainEvent(new LedgerTransactionCreated(UserId: UserId, Transaction: tx));

        return reservation;
    }

    public LedgerTransaction ConfirmReservation(long reservationId, string idem)
    {
        var res = FindReservation(id: reservationId);

        if (!res.CanConfirm())
            throw new InvalidOperationException("Reservation cannot be confirmed.");

        ReservedBalance -= res.Amount;
        res.MarkConfirmed();

        var tx = LedgerTransaction.Capture(walletId: Id, reservationId: res.Id, serviceType: res.ServiceType, amount: res.Amount, idem: idem);
        _ledgerTx.Add(tx);

        AddDomainEvent(new ReservationConfirmed(UserId: UserId, WalletId: Id, ReservationId: res.Id));
        AddDomainEvent(new LedgerTransactionCreated(UserId: UserId, Transaction: tx));

        return tx;
    }

    public LedgerTransaction CancelReservation(long reservationId, string idem)
    {
        var res = FindReservation(id: reservationId);

        if (!res.CanCancel())
            throw new InvalidOperationException("Cannot cancel reservation.");

        ReservedBalance -= res.Amount;
        ApplyBalanceChange(amount: res.Amount);

        res.MarkCancelled();

        var tx = LedgerTransaction.Release(walletId: Id, reservationId: res.Id, serviceType: res.ServiceType, amount: res.Amount, idem: idem);
        _ledgerTx.Add(tx);

        AddDomainEvent(new ReservationCancelled(UserId: UserId, WalletId: Id, ReservationId: res.Id));
        AddDomainEvent(new LedgerTransactionCreated(UserId: UserId, Transaction: tx));

        return tx;
    }

    public LedgerTransaction ExpireReservation(long reservationId)
    {
        var res = FindReservation(id: reservationId);

        if (res.Status != ReservationStatus.Created || !res.IsExpired)
            throw new InvalidOperationException("Reservation not eligible for expiry.");

        ReservedBalance -= res.Amount;
        ApplyBalanceChange(amount: res.Amount);

        res.MarkExpired();

        var tx = LedgerTransaction.Release(walletId: Id, reservationId: res.Id, serviceType: res.ServiceType, amount: res.Amount, idem: $"expire-{res.Id}");
        _ledgerTx.Add(tx);

        AddDomainEvent(new ReservationExpired(UserId: UserId, WalletId: Id, ReservationId: res.Id));
        AddDomainEvent(new LedgerTransactionCreated(UserId: UserId, Transaction: tx));

        return tx;
    }

    public LedgerTransaction Refund(DomainWalletServiceType serviceType, decimal amount, string idem)
    {
        ValidatePositive(v: amount);

        ApplyBalanceChange(amount: amount);

        var tx = LedgerTransaction.Refund(walletId: Id, serviceType: serviceType, amount: amount, idem: idem);
        _ledgerTx.Add(tx);

        AddDomainEvent(new WalletRefunded(UserId: UserId, WalletId: Id, Amount: amount));
        AddDomainEvent(new LedgerTransactionCreated(UserId: UserId, Transaction: tx));

        return tx;
    }

    public PromoGrant AddPromoGrant(DomainWalletServiceType serviceType, decimal amount, DateTime expiresAt)
    {
        ValidatePositive(v: amount);

        var p = new PromoGrant(walletId: Id, serviceType: serviceType, amount: amount, expiresAt: expiresAt);
        _promoGrants.Add(p);

        AddDomainEvent(new PromoGrantAdded(UserId: UserId, WalletId: Id, PromoGrant: p));

        return p;
    }

    public LedgerTransaction ConsumePromo(DomainWalletServiceType serviceType, decimal amount, string idem)
    {
        ValidatePositive(v: amount);

        var activeGrants = _promoGrants
            .Where(x => x.ServiceType == serviceType && x.IsActive)
            .OrderBy(x => x.ExpiresAt)
            .ToArray();

        if (activeGrants.Sum(x => x.RemainingAmount) < amount)
            throw new InvalidOperationException("Insufficient promo credit.");

        var remaining = amount;

        foreach (var g in activeGrants)
        {
            var use = g.Consume(amount: remaining);
            remaining -= use;

            if (remaining == 0)
                break;
        }

        var tx = LedgerTransaction.PromoConsume(walletId: Id, serviceType: serviceType, amount: amount, idem: idem);
        _ledgerTx.Add(tx);

        AddDomainEvent(new PromoConsumed(UserId: UserId, WalletId: Id, ServiceType: serviceType, Amount: amount));
        AddDomainEvent(new LedgerTransactionCreated(UserId: UserId, Transaction: tx));

        return tx;
    }

    private Reservation FindReservation(long id)
        => _reservations.FirstOrDefault(x => x.Id == id)
           ?? throw new InvalidOperationException("Reservation not found.");

    private static void ValidatePositive(decimal v)
    {
        if (v <= 0) throw new InvalidOperationException("Amount must be positive.");
    }
}

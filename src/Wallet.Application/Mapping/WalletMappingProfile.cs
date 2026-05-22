using AutoMapper;
using Wallet.Contracts.Enums;
using Wallet.Contracts.Responses;
using Wallet.Domain.Aggregates;
using Wallet.Domain.Enums;

namespace Wallet.Application.Mapping;

public sealed class WalletMappingProfile : Profile
{
    public WalletMappingProfile()
    {
        CreateMap<DomainWalletServiceType, ContractWalletServiceType>()
            .ConvertUsing(serviceType => serviceType.ToContract());

        CreateMap<ContractWalletServiceType, DomainWalletServiceType>()
            .ConvertUsing(serviceType => serviceType.ToDomain());

        CreateMap<LedgerTransactionType, ContractLedgerTransactionType>()
            .ConvertUsing(transactionType => transactionType.ToContract());

        CreateMap<WalletTransactionMappingSource, WalletTransactionResultResponse>()
            .ForCtorParam("WalletId", opt => opt.MapFrom(src => src.Wallet.Id))
            .ForCtorParam("TransactionId", opt => opt.MapFrom(src => src.Transaction.Id))
            .ForCtorParam("ServiceType", opt => opt.MapFrom(src => src.Transaction.ServiceType))
            .ForCtorParam("TransactionType", opt => opt.MapFrom(src => src.Transaction.Type.ToString()))
            .ForCtorParam("Amount", opt => opt.MapFrom(src => src.Transaction.Amount))
            .ForCtorParam("AvailableBalance", opt => opt.MapFrom(src => src.Wallet.AvailableBalance))
            .ForCtorParam("ReservedBalance", opt => opt.MapFrom(src => src.Wallet.ReservedBalance))
            .PreserveReferences()
            .MaxDepth(3);

        CreateMap<ReservationOperationMappingSource, ReservationOperationResponse>()
            .ForCtorParam("WalletId", opt => opt.MapFrom(src => src.Wallet.Id))
            .ForCtorParam("ReservationId", opt => opt.MapFrom(src => src.Reservation.Id))
            .ForCtorParam("TransactionId", opt => opt.MapFrom(src => src.Transaction == null ? null : (long?)src.Transaction.Id))
            .ForCtorParam("ServiceType", opt => opt.MapFrom(src => src.Reservation.ServiceType))
            .ForCtorParam("Amount", opt => opt.MapFrom(src => src.Reservation.Amount))
            .ForCtorParam("ExpiresAt", opt => opt.MapFrom(src => src.Reservation.ExpireAt))
            .ForCtorParam("Status", opt => opt.MapFrom(src => src.Reservation.Status.ToString()))
            .ForCtorParam("AvailableBalance", opt => opt.MapFrom(src => src.Wallet.AvailableBalance))
            .ForCtorParam("ReservedBalance", opt => opt.MapFrom(src => src.Wallet.ReservedBalance))
            .PreserveReferences()
            .MaxDepth(3);

        CreateMap<PromoGrantMappingSource, PromoGrantOperationResponse>()
            .ForCtorParam("WalletId", opt => opt.MapFrom(src => src.Wallet.Id))
            .ForCtorParam("PromoGrantId", opt => opt.MapFrom(src => src.PromoGrant.Id))
            .ForCtorParam("ServiceType", opt => opt.MapFrom(src => src.PromoGrant.ServiceType))
            .ForCtorParam("OriginalAmount", opt => opt.MapFrom(src => src.PromoGrant.Amount))
            .ForCtorParam("RemainingAmount", opt => opt.MapFrom(src => src.PromoGrant.RemainingAmount))
            .ForCtorParam("ExpiresAt", opt => opt.MapFrom(src => src.PromoGrant.ExpiresAt))
            .PreserveReferences()
            .MaxDepth(3);
    }
}

public sealed record WalletTransactionMappingSource(UserWallet Wallet, LedgerTransaction Transaction);

public sealed record ReservationOperationMappingSource(
    UserWallet Wallet,
    Reservation Reservation,
    LedgerTransaction? Transaction);

public sealed record PromoGrantMappingSource(UserWallet Wallet, PromoGrant PromoGrant);

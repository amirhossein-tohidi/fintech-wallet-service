using Wallet.Contracts.Enums;
using Wallet.GrpcContracts;

namespace Wallet.Api.Grpc.Mapping;

public static class WalletServiceTypeGrpcMapping
{
    public static WalletServiceType ToGrpc(this ContractWalletServiceType serviceType)
    {
        return serviceType switch
        {
            ContractWalletServiceType.General => WalletServiceType.General,
            ContractWalletServiceType.Travel => WalletServiceType.Travel,
            ContractWalletServiceType.Food => WalletServiceType.Food,
            ContractWalletServiceType.Shop => WalletServiceType.Shop,
            _ => throw new NotImplementedException($"gRPC mapping is not implemented for contract wallet service type '{serviceType}'.")
        };
    }

    public static ContractWalletServiceType ToContract(this WalletServiceType serviceType)
    {
        return serviceType switch
        {
            WalletServiceType.General => ContractWalletServiceType.General,
            WalletServiceType.Travel => ContractWalletServiceType.Travel,
            WalletServiceType.Food => ContractWalletServiceType.Food,
            WalletServiceType.Shop => ContractWalletServiceType.Shop,
            _ => throw new NotImplementedException($"Contract mapping is not implemented for gRPC wallet service type '{serviceType}'.")
        };
    }
}

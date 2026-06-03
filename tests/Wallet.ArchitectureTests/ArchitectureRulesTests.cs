using System.Reflection;
using Wallet.Api.Constants;
using Wallet.Domain.Aggregates;
using Wallet.Worker.BackgroundJobs;

namespace Wallet.ArchitectureTests;

public class ArchitectureRulesTests
{
    private static readonly Assembly DomainAssembly = typeof(UserWallet).Assembly;
    private static readonly Assembly ContractsAssembly = typeof(Contracts.Responses.WalletBalanceResponse).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(HeaderNames).Assembly;
    private static readonly Assembly WorkerAssembly = typeof(ReservationExpiryWorker).Assembly;

    [Fact]
    public void Domain_should_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                ContractsAssembly.GetName().Name,
                ApplicationAssembly.GetName().Name,
                InfrastructureAssembly.GetName().Name,
                ApiAssembly.GetName().Name,
                WorkerAssembly.GetName().Name)
            .GetResult();

        AssertArchitectureRule(result);
    }

    [Fact]
    public void Contracts_should_not_depend_on_implementation_layers()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                DomainAssembly.GetName().Name,
                ApplicationAssembly.GetName().Name,
                InfrastructureAssembly.GetName().Name,
                ApiAssembly.GetName().Name,
                WorkerAssembly.GetName().Name)
            .GetResult();

        AssertArchitectureRule(result);
    }

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_hosts()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                InfrastructureAssembly.GetName().Name,
                ApiAssembly.GetName().Name,
                WorkerAssembly.GetName().Name)
            .GetResult();

        AssertArchitectureRule(result);
    }

    [Fact]
    public void Infrastructure_should_not_depend_on_host_projects()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                ApiAssembly.GetName().Name,
                WorkerAssembly.GetName().Name)
            .GetResult();

        AssertArchitectureRule(result);
    }

    [Fact]
    public void Host_projects_should_not_depend_on_each_other()
    {
        var apiResult = Types.InAssembly(ApiAssembly)
            .Should()
            .NotHaveDependencyOn(WorkerAssembly.GetName().Name)
            .GetResult();

        var workerResult = Types.InAssembly(WorkerAssembly)
            .Should()
            .NotHaveDependencyOn(ApiAssembly.GetName().Name)
            .GetResult();

        AssertArchitectureRule(apiResult);
        AssertArchitectureRule(workerResult);
    }

    private static void AssertArchitectureRule(TestResult result)
    {
        Assert.True(
            result.IsSuccessful,
            $"Architecture rule failed for: {string.Join(", ", result.FailingTypes?.Select(x => x.FullName) ?? [])}");
    }
}

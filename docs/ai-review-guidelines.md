# AI Review Guidelines and Coding Rules

This document explains the coding and review rules I use in this wallet project.

The project is a financial wallet system, so every code change must be reviewed carefully. A small mistake in balance, ledger, reservation, idempotency, or concurrency can create serious problems.

These rules are used for code review by both human reviewers and AI review tools.

The goal is not to generate code automatically. The goal is to check whether a code change is safe, clear, and aligned with the design of the project.

I use this document as a review checklist to make sure new changes do not break important parts of the system, such as:

- Clean Architecture boundaries
- Domain rules
- Double-entry ledger
- Wallet balance changes
- Reservation flow
- Idempotency
- Inbox/Outbox
- Concurrency control
- Tests
- Naming and project structure

The short version: every change should be easy to understand, safe for a financial system, tested, and consistent with the existing design.

## How AI Reviewers Should Use This Document

AI review tools should use this document as a checklist when reviewing pull requests or code changes.

The review should check:

- Does the change follow the existing architecture?
- Are business rules still inside the Domain layer?
- Does the change keep the ledger balanced?
- Does it protect idempotency and retry safety?
- Does it respect reservation and balance rules?
- Does it avoid unsafe concurrent changes?
- Are important cases covered by tests?
- Are names clear and easy to understand?
- Is the code simple and ready for real use?

If a change breaks one of these rules, the review should explain the problem clearly and suggest a safer solution.

## Engineering Mindset

Reviewers should check that changes follow this engineering mindset:

- The wallet is treated as a financial system first, not just a CRUD service.
- The design remains simple and explicit instead of relying on clever abstractions.
- Business rules stay visible in the domain model instead of being hidden in controllers, handlers, database code, or transport-specific layers.
- Architectural boundaries remain strong enough that the dependency graph explains the system.
- Consistency and traceability are preferred over short-term convenience.
- Failure modes are intentional. Retries, duplicate requests, concurrency, expired reservations, and external-service failures are normal cases in fintech systems.
- The code lets another engineer understand the business flow without reconstructing it from infrastructure details.
- New abstractions protect a boundary, remove real duplication, or make a business concept clearer.
- Changes stay focused and do not mix unrelated refactors with feature work.
- Tests protect rules and behavior instead of only mirroring implementation.

## Decision Priorities

When reviewing a trade-off, prefer this order:

1. Financial correctness
2. Idempotency and retry safety
3. Clear domain behavior
4. Clean Architecture dependency direction
5. Observability and operational reliability
6. Testability
7. Simplicity and readability
8. Performance, after correctness is protected

Performance matters, but not at the cost of losing money movement correctness, auditability, or safe retries.

## gRPC

- Name the gRPC contracts project `Wallet.GrpcContracts`.
- Keep gRPC contract files under a versioned folder such as `src/Wallet.GrpcContracts/Protos/v1`.
- Keep generated gRPC C# types in the `Wallet.GrpcContracts` namespace.
- Use the proto package `wallet.v1` for wallet gRPC contracts.
- Do not use an unversioned proto folder for new contracts.
- Keep `wallet_enums.proto` as the only shared non-message proto file.
- Only enum definitions may be centralized to avoid repetition.
- Do not create aggregated operation/query message proto files.
- Do not keep request/response messages inside the service proto file.
- Put every gRPC request or response message in its own proto file, for example:
  - `messages/top_up_wallet_grpc_request.proto`
  - `messages/wallet_transaction_result_grpc_response.proto`
- Keep the service RPC definitions in a dedicated service proto file such as `wallet_grpc.proto`.
- Do not use `wallet_service.proto`.
- Service proto files may import the individual request/response proto files they expose.
- Message proto files may import `wallet_enums.proto` when they need wallet enums.
- Use `option csharp_namespace = "Wallet.GrpcContracts";` for wallet gRPC proto files.
- Put enum definitions in `wallet_enums.proto`.
- In `Wallet.GrpcContracts.csproj`, include enum and message proto files with `GrpcServices="None"` and service proto files with `GrpcServices="Both"`.
- Prefer proto field names in snake_case and keep C# mapping explicit.
- Use `string` for money fields in proto contracts and parse/format them with `CultureInfo.InvariantCulture`.
- Use `google.protobuf.Timestamp` for date/time fields and normalize timestamps to UTC when mapping.
- Use `optional` for genuinely optional scalar values such as nullable ids.
- Use `repeated` response collections for query results.
- Keep the gRPC service implementation under `src/Wallet.Api/Grpc`.
- Arrange `src/Wallet.Api/Grpc` like `src/Wallet.Api/Endpoints`:
  - shared gRPC helpers under `Grpc/Common`
  - gRPC mapping extensions under `Grpc/Mapping`
  - wallet-specific RPC handlers under `Grpc/Wallet`
- Keep gRPC interceptors outside the `Grpc` service folder, under `src/Wallet.Api/Interceptors`.
- Register gRPC services through API service/application builder extensions, not directly scattered through `Program.cs`.
- Add `GrpcIdempotencyInterceptor` to gRPC options when registering gRPC.
- Map gRPC services in the API application pipeline next to the HTTP endpoint mapping.
- Keep gRPC mapping out of `Common`; put it under `Grpc/Mapping`.
- Do not keep all gRPC mappings in one large `GrpcMappingExtensions` file.
- Create one focused mapping class per mapped concept or response, for example:
  - `WalletServiceTypeGrpcMapping`
  - `WalletTransactionResultGrpcMapping`
  - `WalletBalanceGrpcMapping`
- Keep gRPC error/status mapping in `GrpcExceptionMapper`.
- Keep gRPC route metadata in a transport-specific `IRouteInfo` implementation such as `GrpcRouteInfo`.
- Do not keep all wallet RPC methods in one large `WalletGrpcService.cs` file.
- Keep `WalletGrpcService.cs` as the service shell, constructor dependencies, and shared helper methods.
- Split wallet RPC method implementations into focused partial files by capability, mirroring HTTP endpoint files:
  - `TopUpGrpcService.cs`
  - `PaymentGrpcService.cs`
  - `ReservationGrpcService.cs`
  - `PromoGrpcService.cs`
  - `WalletReportGrpcService.cs`

## Solution and Project Structure

- Keep the Clean Architecture project boundaries explicit:
  - `Wallet.Domain` contains business rules, aggregates, entities, enums, and domain events.
  - `Wallet.Application` contains CQRS commands, queries, handlers, abstractions, validation, mapping, and use-case orchestration.
  - `Wallet.Infrastructure` implements persistence, messaging, caching, distributed locks, and resilience.
  - `Wallet.Api` and `Wallet.Worker` are host/composition projects.
  - `Wallet.Contracts` contains public HTTP/integration contracts.
  - `Wallet.GrpcContracts` contains gRPC/proto contracts.
- Add a separate library/project when adding a new test category or contract surface. Do not mix architecture, property, integration, acceptance, and unit tests into one project.
- Keep test projects under `tests/` and production projects under `src/`.
- When adding a new project, add it to `FintechWalletService.sln` under the correct solution folder.
- Avoid unrelated solution-file churn such as extra `x64`/`x86` solution platforms or BOM-only changes.
- Keep project names predictable: `Wallet.<LayerOrConcern>` for production projects and `Wallet.<TestCategory>Tests` for test projects.
- Do not ignore `docs/`; project-specific engineering rules and design notes belong in version control.

## Clean Architecture Dependencies

- `Wallet.Domain` must not depend on contracts, application, infrastructure, API, or worker projects.
- `Wallet.Contracts` must not depend on implementation projects.
- `Wallet.Application` may depend on domain and contracts, but must not depend on infrastructure or host projects.
- `Wallet.Infrastructure` may depend on application, domain, and contracts, but must not depend on API or worker projects.
- `Wallet.Api` may reference `Wallet.GrpcContracts` because it hosts the gRPC endpoint.
- `Wallet.GrpcContracts` should remain a contract-only project and should not reference application, domain, infrastructure, API, or worker projects.
- Host projects must not depend on each other. `Wallet.Api` must not reference `Wallet.Worker`, and `Wallet.Worker` must not reference `Wallet.Api`.
- Protect these dependency rules with `tests/Wallet.ArchitectureTests`.
- If a dependency feels convenient but violates the direction above, introduce an application abstraction instead of referencing the outer layer.
- Composition belongs in host/infrastructure projects. Domain and application code should not know how HTTP, gRPC, Kafka, Redis, or SQL Server are wired.

## Domain and Financial Rules

- Keep wallet business decisions inside the domain model, especially balance changes, reservations, promo consumption, and ledger creation.
- Preserve financial correctness through double-entry ledger entries. Every ledger transaction should remain balanced.
- Do not use `double` or floating-point types for money. Use `decimal` in .NET and decimal-safe representations in external contracts.
- Use idempotency for financial write operations and preserve replay/conflict semantics across transports.
- Keep promo credit separate from real wallet balance.
- Avoid fragmented real wallet balances per service; carry service information through `ServiceType` for audit/reporting instead.
- Model cold transactions as reservations: available balance goes down, reserved balance goes up, then confirm/cancel/expiry completes the state transition.
- Domain methods should enforce invariants themselves. Do not rely on callers to remember critical checks such as positive amounts, sufficient balance, or valid reservation state.
- Use domain events to express meaningful business changes that other parts of the system may need to publish or project.
- Ledger entries are append-only audit facts. Do not treat them as temporary calculation helpers.

## Application and API Flow

- HTTP and gRPC endpoints that represent the same business operation should route through the same application commands and queries.
- Keep application handlers focused on orchestration. Domain rules should stay in domain entities/aggregates.
- Expected business failures should use the existing `Result` flow where applicable instead of leaking transport-specific exceptions.
- Keep command and query paths separate.
- Use named arguments for sensitive domain calls when the meaning of parameters is easy to confuse, especially money, ids, service types, and idempotency keys.
- Application handlers should coordinate repositories, unit of work, publishing abstractions, idempotency, and mapping. They should not reimplement domain decisions.
- Read models can be optimized for queries, but write-side correctness belongs to the aggregate and transaction boundary.
- Transport layers should translate protocol details into application calls and translate results back into protocol responses.
- Keep HTTP and gRPC parity intentional. If both expose the same operation, they should behave the same from a business point of view.
- Reuse the existing HTTP request validators for equivalent gRPC write operations by mapping gRPC input into the same request DTO shape.
- Parse and validate transport-specific input at the edge. For example, parse `UserId` and money strings in the gRPC service before sending application commands.
- Pass `CancellationToken` from the transport context into validators and mediator calls.
- Query RPCs do not need idempotency keys. Financial write RPCs do.

## Shared Constants

- Reuse existing shared constants instead of creating local duplicate literals.
- For idempotency headers, always use `HeaderNames.IdempotencyKey`.
- Do not introduce local constants such as `private const string IdempotencyKeyHeader = "x-idempotency-key";`.

## API Parity

- When adding gRPC endpoints equivalent to HTTP APIs, route them through the same application commands and queries used by HTTP endpoints.
- Preserve existing validation, idempotency, mapping, and error semantics as closely as possible.
- For monetary values in proto contracts, prefer decimal-safe representations. Do not use `double` for wallet amounts.
- Preserve idempotency behavior across HTTP and gRPC:
  - Missing idempotency key should be rejected before the command runs.
  - Reusing the same key with a different endpoint or request hash should be treated as a conflict.
  - Completed idempotent requests should replay the stored response.
  - In-progress or failed previous requests should return explicit conflict/error semantics.
- Store gRPC idempotency endpoints in a transport-aware form such as `GRPC {context.Method}`.
- For gRPC, serialize replayable protobuf responses deterministically enough to persist and replay them.
- Keep error codes visible to clients. gRPC errors should include the project error code in response trailers such as `x-error-code`.
- Map business/application failures to appropriate gRPC status codes instead of returning generic internal errors.
- Internal errors should be logged and mapped to a controlled `Internal` gRPC response.

## Cross-Cutting Code

- Put middleware in `src/Wallet.Api/Middlewares`.
- Put interceptors in `src/Wallet.Api/Interceptors`.
- Keep transport-specific mapping and service classes close to their transport implementation.
- Keep route/context abstractions in application and transport-specific implementations in the host project.
- Keep resilience mechanisms close to the infrastructure concern they protect.
- Do not duplicate cross-cutting behavior across transports. Prefer shared application behavior plus thin transport adapters.
- Infrastructure concerns should be replaceable behind application abstractions where the core use case does not care about the concrete technology.
- Idempotency middleware/interceptors are transport-level cross-cutting concerns, but they should reuse the same application persistence abstractions and idempotency policy.
- Use shared constants for metadata/header keys even across transports when the concept is the same.

## Testing

- Use behavior-oriented test names that describe the rule or scenario being protected.
- Put ordinary domain/application/infrastructure unit tests in `tests/Wallet.UnitTests`.
- Put FsCheck property tests in `tests/Wallet.PropertyTests`.
- Use property tests for invariants such as balanced ledger entries, balance conservation, reservation state transitions, and promo credit consumption.
- Put architecture dependency rules in `tests/Wallet.ArchitectureTests`.
- Put Testcontainers-backed API, persistence, Redis, Kafka/outbox/inbox, concurrency, and worker tests in `tests/Wallet.IntegrationTests`.
- Put business-readable Gherkin scenarios in `tests/Wallet.AcceptanceTests`.
- Prefer focused tests that protect real behavior over broad tests that only exercise implementation details.
- Add or update tests when changing financial behavior, idempotency, concurrency control, mapping, validation, architecture boundaries, or contracts.
- Use property tests for rules that should hold across many generated inputs, not for examples that are better expressed as ordinary unit tests.
- Use integration tests when correctness depends on real persistence, transactions, concurrency, outbox/inbox behavior, Redis, Kafka, or hosted workers.
- Use acceptance tests for flows that should be readable by someone thinking in business language.
- Test names should read like business or architecture rules, for example `Cancelled_reservation_restores_the_original_available_balance`.

## Commits

- Use conventional commit messages.
- Prefer formats such as:
  - `test: add property tests for wallet domain invariants`
  - `test: add architecture tests`
  - `test(integration): cover wallet API flows`
  - `refactor: clean up HttpContextRouteInfo and update test references`
- Keep commit messages lowercase after the type unless a proper noun requires otherwise.
- Use a scope when it clarifies the test category or subsystem, for example `test(integration): ...`.
- Keep commits coherent. A commit should tell one story.
- Do not hide formatting churn, solution-file noise, or unrelated cleanups inside a behavior change.
- If a commit message is corrected, amend it instead of creating a second cleanup commit.

## Code Style Preferences

- Prefer clear names over comments. Add comments only where they explain why a decision exists, not what the next line does.
- Prefer small methods that reveal business intent.
- Prefer records and immutable contracts where they naturally fit request/response or event shapes.
- Keep nullable reference types enabled and treat nullability as part of the design.
- Use collection expressions and modern C# syntax when it improves readability.
- Avoid magic strings and duplicated literals for shared protocol or header names.
- Keep mapping code explicit enough that contract/domain differences remain visible.
- Do not use silent defaults in enum mappings. Unknown enum values should throw `NotImplementedException` so contract drift is visible.
- gRPC interceptors should translate `NotImplementedException` from mapping gaps into a controlled `Unimplemented` gRPC response.
- Avoid broad utility classes that become dumping grounds.
- Do not introduce a new package or framework unless it solves a real problem better than the existing stack.

## Documentation

- Update README or docs when adding a new project, transport, test category, contract surface, or important operational behavior.
- Documentation should explain the why, not only the command to run.
- Keep docs consistent with the actual solution structure.
- Prefer examples that match the real wallet flows.

## Verification

- After structural or contract changes, run:
  - `dotnet build FintechWalletService.sln`
  - `dotnet test tests/Wallet.UnitTests/Wallet.UnitTests.csproj --no-build`
  - `dotnet test tests/Wallet.ArchitectureTests/Wallet.ArchitectureTests.csproj --no-build`
- After financial domain changes, also run property tests:
  - `dotnet test tests/Wallet.PropertyTests/Wallet.PropertyTests.csproj`
- After API, persistence, worker, outbox/inbox, Redis, Kafka, or concurrency changes, run the relevant integration tests.
- For documentation-only changes, tests are usually not required.

# Client Management

Covers creating a client and changing their name, address, or phone number. See
`00-architecture-overview.md` for the diagram style note and the overall system view.

## Domain model

`Client` is the aggregate root. `ClientName`, `Address`, and `PhoneNumber` are immutable
value objects (C# `record`s). `BankCard` is a child entity of `Client` — covered in
`02-bank-cards.md`, shown here only as a relationship.

```plantuml
@startuml
skinparam classAttributeIconSize 0
hide circle

class Client {
  - _clientName : ClientName
  - _address : Address
  - _phoneNumber : PhoneNumber
  - _accounts : List<Guid>
  - _bankCards : EntityList<BankCard>
  + static CreateNew(ClientName, Address, PhoneNumber) : Client
  + UpdateClientName(ClientName)
  + UpdatePhoneNumber(PhoneNumber)
  + ClientMoved(Address)
  + CreateNewAccount(accountName, accountNumber) : ActiveAccount
  + AssignNewBankCardForAccount(accountId)
  + GetBankCard(bankCardId) : IBankCard
}

class ClientName <<value object>> {
  + Name : string?
}
class Address <<value object>> {
  + Street : string?
  + StreetNumber : string?
  + PostalCode : string?
  + City : string?
}
class PhoneNumber <<value object>> {
  + Number : string?
}
class BankCard <<entity>>

Client "1" *-- "1" ClientName
Client "1" *-- "1" Address
Client "1" *-- "1" PhoneNumber
Client "1" o-- "*" BankCard : _bankCards
Client "1" o-- "*" ActiveAccount : _accounts (by id only)
@enduml
```

`_accounts` stores only account **ids** (`List<Guid>`) — the client doesn't hold account
aggregates, just enough to answer "does this account belong to this client" for the
`AssignNewBankCardForAccount` guard.

### Guard clauses

| Method | Guard | Exception |
|---|---|---|
| `UpdateClientName`, `UpdatePhoneNumber`, `ClientMoved`, `CreateNewAccount`, `AssignNewBankCardForAccount` | `Id == Guid.Empty` (client never created) | `NonExistingClientException("The Client is not created and no opperations can be executed on it")` |
| `AssignNewBankCardForAccount` | `accountId` not in `_accounts` | `NonExistingAccountException("Client does not have the requested account")` |
| `GetBankCard` | `bankCardId` not found in `_bankCards` | `NonExistingBankCardException("The requested bank card does not exist!")` |

## Commands, handlers, events

| Command | Handler | Aggregate method | Event raised |
|---|---|---|---|
| `CreateClientCommand` | `CreateClientCommandHandler` | `Client.CreateNew(...)` | `ClientCreatedEvent` |
| `ChangeClientNameCommand` | `ChangeClientNameCommandHandler` | `client.UpdateClientName(...)` | `ClientNameChangedEvent` |
| `ChangeClientPhoneNumberCommand` | `ChangeClientPhoneNumberCommandHandler` | `client.UpdatePhoneNumber(...)` | `ClientPhoneNumberChangedEvent` |
| `ClientIsMovingCommand` | `ClientIsMovingCommandHandler` | `client.ClientMoved(...)` | `ClientMovedEvent` |

Each event has a matching event handler that updates the read model (`ClientReport` /
`ClientDetailsReport`, see `08-reporting-read-models.md`):

| Event | Read-model effect |
|---|---|
| `ClientCreatedEvent` | Save `ClientReport` + `ClientDetailsReport` |
| `ClientNameChangedEvent` | Update `ClientReport.Name` + `ClientDetailsReport.ClientName` |
| `ClientPhoneNumberChangedEvent` | Update `ClientDetailsReport.PhoneNumber` |
| `ClientMovedEvent` | Update `ClientDetailsReport`'s address fields |

## Sequence: creating a new client

Both clients collect the same four fields (name, address, phone) but present them
differently: the WinForms wizard (`ClientDetailsPresenter`, see `09-winforms-ui.md`)
gathers them across three panels before sending anything, while Vue's `ClientCreate.vue`
is a single form submitted all at once — either way, exactly one `POST /api/clients` call
reaches `Fohjin.DDD.WebApi`, which is where everything below the HTTP line is unchanged
from before this system had a web front door at all.

```plantuml
@startuml
actor Employee
participant "ClientDetailsPresenter\n(WinForms) or ClientCreate.vue" as Client
participant "FohjinApiClient\n(generated, either client)" as ApiClient
participant "Fohjin.DDD.WebApi\nPOST /api/clients" as Endpoint
participant "IBus\n(DirectBus)" as Bus
participant "CreateClientCommandHandler" as Handler
participant "Client\n(aggregate)" as Aggregate
participant "Event Store" as Store
participant "ClientCreatedEventHandler" as EvtHandler
participant "Reporting Store" as Reporting

Employee -> Client : fill name, address, phone,\nsubmit (wizard step 3, or the one form)
Client -> ApiClient : CreateClientAsync(request)\n/ createClient(request)
ApiClient -> Endpoint : POST /api/clients\n(Authorization: Bearer <token from Sts login>)
Endpoint -> Bus : Publish(CreateClientCommand)
Endpoint -> Bus : CommitAsync()
Endpoint --> ApiClient : 202 Accepted
activate Bus
Bus -> Handler : RouteAsync -> ExecuteAsync(command)
Handler -> Aggregate : Client.CreateNew(name, address, phone)
Aggregate --> Aggregate : Apply(ClientCreatedEvent)
Handler -> Store : repository.Add(client)
Store -> Store : SaveAsync (persist ClientCreatedEvent)
Store -> Bus : Publish(ClientCreatedEvent)
deactivate Bus
Bus ->> EvtHandler : OnNext(ClientCreatedEvent) (Rx, async,\ndetached from the request above -\nsee 07-messaging-bus.md)
EvtHandler -> Reporting : SaveAsync(ClientReport)
EvtHandler -> Reporting : SaveAsync(ClientDetailsReport)
@enduml
```

The `202 Accepted` matters: it comes back as soon as `CommitAsync()` returns, which is
fire-and-forget (`07-messaging-bus.md`) — the HTTP response does **not** wait for
`ClientCreatedEventHandler` to run. Both clients handle this the same way conceptually:
WinForms' fixed-delay `ISystemTimer` refresh (`09-winforms-ui.md`) and Vue's
navigate-back-to-the-search-list-and-refetch are both just "poll again a bit later,"
because there's no id or confirmation to navigate straight to yet.

## Editing an existing client

`ChangeClientNameCommand`/`ClientIsMovingCommand`/`ChangeClientPhoneNumberCommand` follow
the same shape as each other and as create: one API call per edit (no batching across
wizard steps, unlike WinForms' create flow), then a refresh. One representative sequence
stands in for all three:

```plantuml
@startuml
actor Employee
participant "ClientDetailsPresenter\n(WinForms) or ClientDetails.vue" as Client
participant "FohjinApiClient" as ApiClient
participant "Fohjin.DDD.WebApi\nPOST /api/clients/{id}/name" as Endpoint
participant "IBus" as Bus
participant "ChangeClientNameCommandHandler" as Handler
participant "Client\n(aggregate)" as Aggregate

Employee -> Client : edit name, Save
Client -> ApiClient : ChangeClientNameAsync(id, request)
ApiClient -> Endpoint : POST /api/clients/{id}/name
Endpoint -> Bus : Publish(ChangeClientNameCommand)
Endpoint -> Bus : CommitAsync()
Endpoint --> ApiClient : 202 Accepted
Bus -> Handler : ExecuteAsync(command)
Handler -> Aggregate : GetByIdAsync(Id)
Handler -> Aggregate : client.UpdateClientName(new ClientName(...))
Aggregate --> Aggregate : Apply(ClientNameChangedEvent)
Client -> Client : refresh (WinForms: ISystemTimer.Trigger(LoadDataAsync, 1000ms);\nVue: re-fetch on navigation)
@enduml
```

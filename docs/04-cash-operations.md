# Cash Operations

Deposit and withdrawal — the two simplest mutators on `ActiveAccount` (see
`03-account-management.md` for the aggregate, guards, and the `Ledger` hierarchy these
operations append to).

## Commands, handlers, events

| Command | Handler | Aggregate call | Event | Ledger entry |
|---|---|---|---|---|
| `DepositCashCommand(accountId, amount)` | `DepositCashCommandHandler` | `activeAccount.Deposit(new Amount(amount))` | `CashDepositedEvent(newBalance, amount)` | `CreditMutation` |
| `WithdrawalCashCommand(accountId, amount)` | `WithdrawalCashCommandHandler` | `activeAccount.Withdrawal(new Amount(amount))` | `CashWithdrawnEvent(newBalance, amount)` | `DebitMutation` |

| Event | Read-model effect |
|---|---|
| `CashDepositedEvent` | Update `AccountDetailsReport.Balance`; save `LedgerReport("Deposit", amount)` |
| `CashWithdrawnEvent` | Update `AccountDetailsReport.Balance`; save `LedgerReport("Withdrawal", amount)` |

## Sequence: deposit (always succeeds if the account is open)

Both clients drive this through the same `POST /api/accounts/{id}/deposit` endpoint —
WinForms' `AccountDetailsPresenter.DepositMoney()` and Vue's `AccountDetails.vue` deposit
form both call it via their own generated `FohjinApiClient`, and everything from the
endpoint down is identical to how it worked before this system had an HTTP front door.

```plantuml
@startuml
actor Employee
participant "AccountDetailsPresenter\n(WinForms) or AccountDetails.vue" as Client
participant "FohjinApiClient" as ApiClient
participant "Fohjin.DDD.WebApi\nPOST /api/accounts/{id}/deposit" as Endpoint
participant "IBus" as Bus
participant "DepositCashCommandHandler" as Handler
participant "ActiveAccount" as Account
participant "CashDepositEventHandler" as EvtHandler
participant "Reporting Store" as Reporting

Employee -> Client : enter amount, deposit
Client -> ApiClient : DepositCashAsync(accountId, request)
ApiClient -> Endpoint : POST /api/accounts/{id}/deposit
Endpoint -> Bus : Publish(DepositCashCommand)
Endpoint -> Bus : CommitAsync()
Endpoint --> ApiClient : 202 Accepted
Bus -> Handler : ExecuteAsync(command)
Handler -> Account : GetByIdAsync(accountId)
Handler -> Account : Deposit(new Amount(amount))
Account -> Account : Guard()
Account -> Account : _balance = _balance.Deposit(amount)
Account --> Account : Apply(CashDepositedEvent)\nappends CreditMutation ledger entry
Bus ->> EvtHandler : OnNext(CashDepositedEvent)\n(detached from the request above)
EvtHandler -> Reporting : UpdateAsync<AccountDetailsReport>(Balance)
EvtHandler -> Reporting : SaveAsync(LedgerReport("Deposit", amount))
Client -> Client : refresh (WinForms: ISystemTimer.Trigger(LoadDataAsync, 2000ms);\nVue: re-fetch on navigation)
@enduml
```

## Sequence: withdrawal, both outcomes

Withdrawal is the same shape but has a real guard to show — insufficient balance throws
before any event is raised, so the account's state (and the read model) is left untouched.

```plantuml
@startuml
participant "WithdrawalCashCommandHandler" as Handler
participant "ActiveAccount" as Account

Handler -> Account : GetByIdAsync(accountId)
Handler -> Account : Withdrawal(new Amount(amount))
Account -> Account : Guard()

alt balance >= amount
    Account -> Account : IsBalanceHighEnough(amount) [ok]
    Account -> Account : _balance = _balance.Withdrawal(amount)
    Account --> Account : Apply(CashWithdrawnEvent)\nappends DebitMutation ledger entry
else balance < amount
    Account -> Account : IsBalanceHighEnough(amount)\nthrows AccountBalanceToLowException
    note right of Account : no event raised,\nno ledger entry,\nread model unchanged
end
@enduml
```

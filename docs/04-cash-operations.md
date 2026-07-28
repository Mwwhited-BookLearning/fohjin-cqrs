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

```plantuml
@startuml
actor Employee
participant "AccountDetailsPresenter" as Presenter
participant "IBus" as Bus
participant "DepositCashCommandHandler" as Handler
participant "ActiveAccount" as Account
participant "CashDepositEventHandler" as EvtHandler
participant "Reporting Store" as Reporting

Employee -> Presenter : enter amount, DepositMoney()
Presenter -> Bus : Publish(DepositCashCommand)
Presenter -> Bus : CommitAsync()
Bus -> Handler : ExecuteAsync(command)
Handler -> Account : GetByIdAsync(accountId)
Handler -> Account : Deposit(new Amount(amount))
Account -> Account : Guard()
Account -> Account : _balance = _balance.Deposit(amount)
Account --> Account : Apply(CashDepositedEvent)\nappends CreditMutation ledger entry
Bus ->> EvtHandler : OnNext(CashDepositedEvent)
EvtHandler -> Reporting : UpdateAsync<AccountDetailsReport>(Balance)
EvtHandler -> Reporting : SaveAsync(LedgerReport("Deposit", amount))
Presenter -> Presenter : ISystemTimer.Trigger(LoadDataAsync, 2000ms)
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

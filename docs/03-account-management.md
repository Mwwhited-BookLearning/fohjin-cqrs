# Account Management

Covers opening an account, closing it, and renaming it. Deposits, withdrawals, and
transfers each get their own doc (`04-cash-operations.md`, `05-money-transfers.md`)
because of how much routing/guard logic they carry, but they're all methods on the same
`ActiveAccount` aggregate described here.

## Domain model

> Aggregate root / value object explained from first principles: `patterns/ddd-building-blocks.md`.

`ActiveAccount` is the aggregate root while an account is open. Closing it produces a
separate, independent aggregate — `ClosedAccount` — that archives the ledger history.
Every balance-changing operation appends a `Ledger` entry; the `Ledger` subclasses are
pure markers (no extra fields) used only for their type name.

```plantuml
@startuml
skinparam classAttributeIconSize 0
hide circle

class ActiveAccount {
  - _clientId : Guid
  - _accountName : AccountName
  - _accountNumber : AccountNumber
  - _balance : Balance
  - _closed : bool
  - _ledgers : List<Ledger>
  + static CreateNew(clientId, name, number) : ActiveAccount
  + ChangeAccountName(AccountName)
  + Deposit(Amount)
  + Withdrawal(Amount)
  + SendTransferTo(AccountNumber, Amount)
  + ReceiveTransferFrom(AccountNumber, Amount)
  + PreviousTransferFailed(AccountNumber, Amount)
  + Close() : ClosedAccount
}

class ClosedAccount {
  - _originalAccountId : Guid
  - _clientId : Guid
  - _accountName : AccountName
  - _accountNumber : AccountNumber
  - _ledgers : List<Ledger>
}

class Balance <<value object>> {
  - _amount : Amount
  + Deposit(Amount) : Balance
  + Withdrawal(Amount) : Balance
  + WithdrawalWillResultInNegativeBalance(Amount) : bool
}
class Amount <<value object>> {
  - _decimalAmount : decimal
  + Add(Amount) : Amount
  + Substract(Amount) : Amount
  + IsNegative() : bool
}
class AccountName <<value object>> {
  + Name : string?
}
class AccountNumber <<value object>> {
  + Number : string?
}

abstract class Ledger {
  + Amount : Amount
  + Account : AccountNumber
}
class CreditMutation
class DebitMutation
class CreditTransfer
class DebitTransfer
class DebitTransferFailed

Ledger <|-- CreditMutation
Ledger <|-- DebitMutation
Ledger <|-- CreditTransfer
Ledger <|-- DebitTransfer
Ledger <|-- DebitTransferFailed

ActiveAccount "1" *-- "1" Balance
ActiveAccount "1" *-- "*" Ledger
ActiveAccount ..> ClosedAccount : Close() creates
ClosedAccount "1" *-- "*" Ledger : archived copy
@enduml
```

| Operation | Ledger entry created |
|---|---|
| Deposit | `CreditMutation` |
| Withdrawal | `DebitMutation` |
| SendTransferTo | `CreditTransfer` |
| ReceiveTransferFrom | `DebitTransfer` |
| PreviousTransferFailed (compensating refund) | `DebitTransferFailed` |

`ClosedAccountCreatedEventHandler.GetDescription` recognizes `"DebitTransferFailed"` —
matching what a failed transfer actually produces — so replaying a closed account's archived
ledger describes a failed-transfer entry correctly instead of throwing
`UnsupportedTransferTypeException`.

## Guard clauses

Every mutator except the factory calls `Guard()` first, which is `IsAccountNotCreated()` +
`IsAccountClosed()`.

| Guard | Condition | Exception |
|---|---|---|
| `IsAccountNotCreated` | `Id == Guid.Empty` | `NonExitsingAccountException("The ActiveAccount is not created and no operations can be executed on it")` |
| `IsAccountClosed` | `_closed == true` | `ClosedAccountException("The ActiveAccount is closed and no operations can be executed on it")` |
| `IsBalanceHighEnough` (Withdrawal, SendTransferTo) | `_balance.WithdrawalWillResultInNegativeBalance(amount)` | `AccountBalanceToLowException("The amount {0:C} is larger than your current balance {1:C}")` |
| `IsAccountBalanceZero` (Close only) | `_balance != 0.0M` | `AccountBalanceNotZeroException("The current balance is {0:C} this must first be transferred to an other account")` |

## State

```plantuml
@startuml
[*] --> Open : CreateNew()\n/ AccountOpenedEvent
Open --> Open : Deposit / Withdrawal / SendTransferTo /\nReceiveTransferFrom / PreviousTransferFailed /\nChangeAccountName
Open --> Closed : Close()\n[balance must be zero]\n/ AccountClosedEvent + ClosedAccountCreatedEvent
Closed --> Closed : any mutator\n[throws ClosedAccountException]
@enduml
```

`Close()` is the only transition, and it's one-way: it both marks the `ActiveAccount` as
`_closed = true` (so it rejects all further operations) and constructs an independent
`ClosedAccount` aggregate carrying a serialized copy of the ledger history.

## Commands, handlers, events

| Command | Handler | Aggregate call | Event(s) raised |
|---|---|---|---|
| `OpenNewAccountForClientCommand` | `OpenNewAccountForClientCommandHandler` | `client.CreateNewAccount(name, number)` | `AccountOpenedEvent` |
| `ChangeAccountNameCommand` | `ChangeAccountNameCommandHandler` | `activeAccount.ChangeAccountName(...)` | `AccountNameChangedEvent` |
| `CloseAccountCommand` | `CloseAccountCommandHandler` | `activeAccount.Close()` | `AccountClosedEvent` + `ClosedAccountCreatedEvent` |

| Event | Read-model effect |
|---|---|
| `AccountOpenedEvent` | Save `AccountReport` + `AccountDetailsReport` (Balance = 0) |
| `AccountNameChangedEvent` | Update `AccountReport`/`AccountDetailsReport`.AccountName |
| `AccountClosedEvent` | Update `AccountReport`/`AccountDetailsReport`.Status = "Closed" (same row, same id - `08-reporting-read-models.md`) |
| `ClosedAccountCreatedEvent` | none (no-op - the row was already marked closed in place above) |

## Sequence: open then close an account

```plantuml
@startuml
participant "OpenNewAccountForClientCommandHandler" as OpenHandler
participant "Client" as Client
participant "ActiveAccount" as Active
participant "CloseAccountCommandHandler" as CloseHandler
participant "ClosedAccount" as Closed

OpenHandler -> Client : GetByIdAsync(clientId)
OpenHandler -> Client : CreateNewAccount(name, number)
Client -> Active : ActiveAccount.CreateNew(clientId, name, number)
Active --> Active : Apply(AccountOpenedEvent)
Client --> Client : Apply(AccountToClientAssignedEvent)
OpenHandler -> Active : repository.Add(activeAccount)

== later, balance must be zero ==

CloseHandler -> Active : GetByIdAsync(accountId)
CloseHandler -> Active : Close()
Active -> Active : Guard(); IsAccountBalanceZero()
Active -> Closed : ClosedAccount.CreateNew(id, clientId, ledgers, name, number)
Closed --> Closed : Apply(ClosedAccountCreatedEvent)
Active --> Active : Apply(AccountClosedEvent)
CloseHandler -> Closed : repository.Add(closedAccount)
@enduml
```

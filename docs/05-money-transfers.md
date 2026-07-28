# Money Transfers

This is the most involved flow in the app: the bank simulates talking to other banks by
routing a transfer through one of three fake outcomes, entirely at random, against its own
single database. There is no real external bank anywhere in this codebase.

## The routing gag

`MoneyTransferService.Send` schedules `DoSendAsync` after a fake 5-second network delay
(`ISystemTimer.Trigger(..., 5000)`), then draws `ISystemRandom.Next(0, 9)` and looks the
result up in a 9-entry dictionary:

| Index | Route | Meaning |
|---|---|---|
| 0, 1, 5, 6 | Internal | Target account exists in *this* bank's own tables |
| 2, 3, 7, 8 | External (exists) | Routed through `IReceiveMoneyTransfers.Receive` as if it were a different bank — but `MoneyReceiveService` queries the same `AccountReport` table |
| 4 | External (missing) | Same as above, but the target account number is deliberately mangled first, guaranteeing the lookup fails |

The code's own comment: *"I didn't want to introduce an actual external bank, so that's
why you see this nice construct :)"*.

> **Bug found while documenting this**: the "mangle the account number" step is
> `moneyTransfer.TargetAccount?.Reverse().ToString()`. `Reverse()` on a `string` returns
> `IEnumerable<char>`, and calling `.ToString()` on *that* does not reverse the string — it
> just prints the enumerator's type name. It still produces something that isn't a valid
> account number (so the bug this line is trying to cause still happens), just not for the
> reason the code implies.

## Activity diagram: transfer routing

```plantuml
@startuml
start
:MoneyTransferSendEventHandler triggers
SendMoneyTransferFurtherEventHandler;
:MoneyTransferService.Send(transfer);
:wait ~5000ms (simulated network delay);
:draw random int 0-8;

if (index is 0, 1, 5, or 6?\n(internal)) then (yes)
  :look up AccountReport
  by TargetAccount number;
  if (found?) then (yes)
    :Publish ReceiveMoneyTransferCommand directly;
    :CommitAsync();
    :ok;
  else (no)
    :.First() throws;
    :failed;
  endif
elseif (index is 2, 3, 7, or 8?\n(external, exists)) then (yes)
  :MoneyReceiveService.Receive(transfer);
  :look up AccountReport by TargetAccount number
  (same table, different service);
  if (found?) then (yes)
    :Publish ReceiveMoneyTransferCommand;
    note right: no explicit CommitAsync on this path
    :ok;
  else (no)
    :throw UnknownAccountException;
    :failed;
  endif
else (index is 4\n(external, missing))
  :mangle TargetAccount number;
  :MoneyReceiveService.Receive(mangled transfer);
  :lookup always fails;
  :throw UnknownAccountException;
  :failed;
endif

if (failed?) then (yes)
  :catch in DoSendAsync;
  :look up AccountReport
  by SOURCE account number;
  :Publish MoneyTransferFailedCompensatingCommand;
  :ActiveAccount.PreviousTransferFailed
  (refund deposit + DebitTransferFailed ledger entry);
  stop
else (no)
  :ActiveAccount.ReceiveTransferFrom
  (target account credited);
  stop
endif
@enduml
```

## Commands, handlers, events

| Command | Handler | Aggregate call | Event |
|---|---|---|---|
| `SendMoneyTransferCommand` | `SendMoneyTransferCommandHandler` | `activeAccount.SendTransferTo(targetNumber, amount)` | `MoneyTransferSendEvent` |
| `ReceiveMoneyTransferCommand` | `ReceiveMoneyTransferCommandHandler` | `activeAccount.ReceiveTransferFrom(sourceNumber, amount)` | `MoneyTransferReceivedEvent` |
| `MoneyTransferFailedCompensatingCommand` | `MoneyTransferFailedCompensatingCommandHandler` | `activeAccount.PreviousTransferFailed(targetNumber, amount)` | `MoneyTransferFailedEvent` |

All three handlers null-conditional (`?.`) the aggregate call — if `GetByIdAsync` can't
find the account, the handler silently no-ops rather than throwing.

| Event | Read-model effect | Other handler |
|---|---|---|
| `MoneyTransferSendEvent` | Update balance; save `LedgerReport("Transfer to {target}")` | **`SendMoneyTransferFurtherEventHandler`** also subscribes — it's what actually calls `MoneyTransferService.Send`, restarting the routing pipeline above |
| `MoneyTransferReceivedEvent` | Update balance; save `LedgerReport("Transfer from {source}")` | — |
| `MoneyTransferFailedEvent` | Update balance (refund); save `LedgerReport("Transfer to {target} failed")` | — |

Two independent event handlers subscribe to the same `MoneyTransferSendEvent` — one
updates the read model, the other drives the simulated routing. Neither knows about the
other (see `07-messaging-bus.md` for how Rx fans a single event out to N handlers).

## Sequence: full send-to-external-account, success case

```plantuml
@startuml
participant "SendMoneyTransferCommandHandler" as SendHandler
participant "ActiveAccount\n(source)" as Source
participant "SendMoneyTransferFurtherEventHandler" as FurtherHandler
participant "MoneyTransferService" as TransferSvc
participant "MoneyReceiveService" as ReceiveSvc
participant "ReceiveMoneyTransferCommandHandler" as ReceiveHandler
participant "ActiveAccount\n(target)" as Target

SendHandler -> Source : SendTransferTo(targetNumber, amount)
Source -> Source : Guard(); IsBalanceHighEnough()
Source --> Source : Apply(MoneyTransferSendEvent)\nappends CreditTransfer ledger entry

Source ->> FurtherHandler : OnNext(MoneyTransferSendEvent) (Rx)
FurtherHandler -> TransferSvc : Send(transfer)
TransferSvc -> TransferSvc : wait 5000ms, draw random -> bucket "external, exists"
TransferSvc -> ReceiveSvc : Receive(transfer)
ReceiveSvc -> ReceiveSvc : look up AccountReport by TargetAccount
ReceiveSvc -> ReceiveHandler : Publish(ReceiveMoneyTransferCommand)
ReceiveHandler -> Target : ReceiveFrom(sourceNumber, amount)
Target --> Target : Apply(MoneyTransferReceivedEvent)\nappends DebitTransfer ledger entry
@enduml
```

## Sequence: compensating failure

```plantuml
@startuml
participant "MoneyTransferService" as TransferSvc
participant "MoneyReceiveService" as ReceiveSvc
participant "MoneyTransferFailedCompensatingCommandHandler" as CompHandler
participant "ActiveAccount\n(source)" as Source

TransferSvc -> TransferSvc : draw random -> bucket "external, missing"
TransferSvc -> ReceiveSvc : Receive(transfer with mangled target)
ReceiveSvc -> ReceiveSvc : lookup fails
ReceiveSvc --> TransferSvc : throw UnknownAccountException
TransferSvc -> TransferSvc : catch(Exception) in DoSendAsync
TransferSvc -> TransferSvc : look up AccountReport by SOURCE account
TransferSvc -> CompHandler : Publish(MoneyTransferFailedCompensatingCommand)
CompHandler -> Source : PreviousTransferFailed(targetNumber, amount)
Source --> Source : Apply(MoneyTransferFailedEvent)\nrefund deposit + DebitTransferFailed ledger entry
@enduml
```

`DebitTransferFailed` is the ledger entry type from the refund above — see
`03-account-management.md` for the known bug where `ClosedAccountCreatedEventHandler`
doesn't recognize this type name if the account is later closed with one of these entries
in its history.

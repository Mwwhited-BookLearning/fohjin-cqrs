# WinForms UI

The four screens, the Presenter/View pattern they're all built on, and the wizard-style
multi-step flow for creating a new client.

## Presenter/View wiring

`Presenter<TView>`'s constructor wires up every view event to a matching presenter method
purely by reflection and naming convention — no manual `+=` anywhere in a concrete
presenter:

```plantuml
@startuml
participant "Presenter<TView> ctor" as Ctor
participant "TView (reflection)" as View
participant "Presenter subclass (reflection)" as Sub

Ctor -> View : find declared `event Action OnXxx` members
Ctor -> Sub : find public method `Xxx`\n(strip the "On" prefix)
Ctor -> View : eventInfo.AddEventHandler(view, delegate-bound-to-Xxx)
note right : no match found for an event\n-> Debug.WriteLine warning, skipped\n(non-fatal)
@enduml
```

So `IClientDetailsView.OnSaveNewClientName` auto-binds to
`ClientDetailsPresenter.SaveNewClientName()` purely because the names match — this is what
`PresenterTest.cs` verifies.

## Components

```plantuml
@startuml
skinparam rectangle {
  BackgroundColor<<Component>> #85bbf0
  FontColor<<Component>> black
  BorderColor black
}
skinparam defaultTextAlignment center
skinparam wrapWidth 200

rectangle "ClientSearchFormPresenter\n<size:11><<Component>></size>" <<Component>> as p1
rectangle "ClientSearchForm\n<size:11><<WinForms Form>></size>" <<Component>> as v1
rectangle "ClientDetailsPresenter\n<size:11><<Component>></size>\nwizard state machine" <<Component>> as p2
rectangle "ClientDetails\n<size:11><<WinForms Form>></size>" <<Component>> as v2
rectangle "AccountDetailsPresenter\n<size:11><<Component>></size>" <<Component>> as p3
rectangle "AccountDetails\n<size:11><<WinForms Form>></size>" <<Component>> as v3
rectangle "PopupPresenter\n<size:11><<Component>></size>\nCatchPossibleException" <<Component>> as p4
rectangle "Popup\n<size:11><<WinForms Form>></size>" <<Component>> as v4

p1 --> v1 : hooks up via reflection
p2 --> v2 : hooks up via reflection
p3 --> v3 : hooks up via reflection
p4 --> v4 : hooks up via reflection
p1 --> p2 : SetClient() + Display()
p2 --> p3 : OpenSelectedAccount()
p1 --> p4 : wraps command-publishing\nblocks in CatchPossibleException
p2 --> p4 : "
p3 --> p4 : "
@enduml
```

`IAccountDetailsPresenter` has no bank-card methods — `AssignNewBankCardCommand` /
`CancelBankCardCommand` / `ReportStolenBankCardCommand` (see `02-bank-cards.md`) are not
wired to any button or menu anywhere in this UI.

## Screens

**Client Search** — the main window. A hidden single-tab `TabControl` hosts a `ListBox`
bound to `ClientReport`s; a menu item starts the "add new client" flow.

```plantuml
@startsalt
{
  { "Client" | "Help" }
  ==
  Existing clients
  {
    "Doe, John"
    "Smith, Jane"
    "Nijhof, Mark"
  }
}
@endsalt
```

**Client Details — create-new wizard.** Three steps, shown one panel at a time. Steps 1
and 2 only mutate an in-memory DTO (`_clientDetailsReport = ... with { ... }`) — nothing is
published to the bus until step 3.

```plantuml
@startsalt
{
  New Client - Step 1 of 3: Name
  ==
  Client Name | "________________"
  --
  [Save]  [Cancel]
}
@endsalt
```

```plantuml
@startsalt
{
  New Client - Step 2 of 3: Address
  ==
  Street        | "________________"
  Street Number | "____"
  Postal Code   | "______"
  City          | "________________"
  --
  [Save]  [Cancel]
}
@endsalt
```

```plantuml
@startsalt
{
  New Client - Step 3 of 3: Phone Number
  ==
  Phone Number | "________________"
  --
  [Save]  [Cancel]
}
@endsalt
```

**Client Details — editing an existing client.** Every field is visible at once; each
"initiate change" action opens the relevant panel and publishes its command immediately on
save (no batching, unlike create).

```plantuml
@startsalt
{
  Client Details - Overview
  ==
  Name    | "Mark Nijhof"           | [Change name]
  Address | "Welhavens gate 49b"    | [Client has moved]
  Phone   | "95009937"              | [Change phone number]
  --
  Accounts
  {
    "Checking - 123456 - $1,234.56"
  }
  [New account]
  --
  [Close]
}
@endsalt
```

**Account Details.**

```plantuml
@startsalt
{
  Account Details
  ==
  { Details | Deposit | Withdraw | Transfer | Rename }
  {
    Account Name   | "Checking"
    Account Number | "123456"
    Balance        | "$1,234.56"
    --
    Ledger
    {
      "Deposit         +$100.00"
      "Withdrawal      -$50.00"
      "Transfer to 987654  -$25.00"
    }
  }
  --
  [Close account]
}
@endsalt
```

**Popup** — the shared error dialog every presenter routes exceptions through.

```plantuml
@startsalt
{
  Error
  ==
  An error occurred:
  "NonExistingAccountException:
  Client does not have the requested account"
  --
  [OK]
}
@endsalt
```

## Sequence: create new client, full UI-to-refresh flow

```plantuml
@startuml
actor Employee
participant "ClientSearchFormPresenter" as Search
participant "ClientDetailsPresenter" as Details
participant "IBus" as Bus
participant "ClientCreatedEventHandler" as EvtHandler
participant "Reporting Store" as Reporting
participant "ClientSearchForm" as SearchView

Employee -> Search : Client > Add a new client
Search -> Details : SetClient(null); Display()
Details -> Details : _editStep=1, _createNewProcess=true
Details -> Details : show client-name panel (ShowDialog - modal)

Employee -> Details : fill name -> Save
Details -> Details : SaveNewClientName()\n(local only, no publish)
Employee -> Details : fill address -> Save
Details -> Details : SaveNewAddress()\n(local only, no publish)
Employee -> Details : fill phone -> Save
Details -> Bus : Publish(CreateClientCommand)
Details -> Bus : CommitAsync()
Details -> Details : dialog Close()

Bus ->> EvtHandler : OnNext(ClientCreatedEvent) (Rx, async,\ntiming decoupled from the above)
EvtHandler -> Reporting : SaveAsync(ClientReport + ClientDetailsReport)

Search -> Search : ISystemTimer.Trigger(LoadDataAsync, 2000ms)\n(started when the dialog was OPENED,\nnot when it closed)
Search -> Reporting : GetByExampleAsync<ClientReport>(null)
Search -> SearchView : Clients = results
@enduml
```

The 2-second timer is a blind fixed-delay poll, not correlated with when the command/event
pipeline actually finishes — on a slow event handler the new client may not appear yet, and
there's no retry. Historically this refresh also silently failed outright: `ISystemTimer`'s
production implementation ran the delayed callback via `Task.Run`, off the UI thread, and
setting a WinForms control's `DataSource` from a non-UI thread throws — invisibly, because
nothing observed the fire-and-forget task's exception. `SystemTimer.Trigger` now captures
the UI `SynchronizationContext` when scheduled and marshals the callback back onto it, so
the poll itself works again; the timing/correlation issue described above is unchanged.

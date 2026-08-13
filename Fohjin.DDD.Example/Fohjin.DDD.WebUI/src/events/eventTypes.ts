// Hand-maintained in step with Fohjin.DDD.Abstractions/Events/{Account,Client}/*.cs - no
// generated client covers SSE payloads (see eventBus.ts), so there's no automatic source of
// truth for this list. Grouped by domain area purely to mirror the backend's own folder split
// for readability in the Monitoring filter UI.
export const ACCOUNT_EVENT_TYPES = [
  "AccountClosedEvent",
  "AccountNameChangedEvent",
  "AccountOpenedEvent",
  "CashDepositedEvent",
  "CashWithdrawnEvent",
  "ClosedAccountCreatedEvent",
  "MoneyTransferFailedEvent",
  "MoneyTransferReceivedEvent",
  "MoneyTransferSendEvent",
] as const;

export const CLIENT_EVENT_TYPES = [
  "AccountToClientAssignedEvent",
  "BankCardWasCanceledByClientEvent",
  "BankCardWasReportedStolenEvent",
  "ClientCreatedEvent",
  "ClientMovedEvent",
  "ClientNameChangedEvent",
  "ClientPhoneNumberChangedEvent",
  "NewBankCardForAccountAsignedEvent",
] as const;

export const ALL_EVENT_TYPES = [...ACCOUNT_EVENT_TYPES, ...CLIENT_EVENT_TYPES] as const;

export type KnownEventType = (typeof ALL_EVENT_TYPES)[number];

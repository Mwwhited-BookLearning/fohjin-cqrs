import type { EventEnvelope } from "./eventBus";

// Which domain events, arriving on the shared event bus (eventBus.ts), should make each screen
// reload - extracted out of the views themselves so these rules can be unit-tested directly,
// without mounting a component.

// ClientSearch.vue: ClientCreatedEvent's AggregateId is the new client's own id (Client.CreateNew),
// not something already known before it happens, so this just reloads the whole list on any
// creation rather than trying to append a single row.
export function shouldRefreshClientSearch(event: EventEnvelope): boolean {
  return event.eventType === "ClientCreatedEvent";
}

// ClientDetails.vue: every event below is applied on the Client aggregate itself
// (AggregateId === clientId) except the two bank-card "disabled" events, which are applied on the
// BankCard entity instead (AggregateId === the card's own id, not the client's - see
// docs/02-bank-cards.md's note on this) - checked against the bank card ids already loaded for
// this client, since there's no other way to know which client a bare bank-card id belongs to
// without a second lookup.
const CLIENT_LEVEL_EVENTS = new Set([
  "ClientNameChangedEvent",
  "ClientMovedEvent",
  "ClientPhoneNumberChangedEvent",
  "AccountToClientAssignedEvent",
  "NewBankCardForAccountAsignedEvent",
]);
const BANK_CARD_EVENTS = new Set(["BankCardWasCanceledByClientEvent", "BankCardWasReportedStolenEvent"]);

export function shouldRefreshClientDetails(event: EventEnvelope, clientId: string, bankCardIds: readonly string[]): boolean {
  if (CLIENT_LEVEL_EVENTS.has(event.eventType) && event.aggregateId === clientId) return true;
  if (BANK_CARD_EVENTS.has(event.eventType) && bankCardIds.includes(event.aggregateId)) return true;
  return false;
}

// AccountDetails.vue: every one of these events is applied on the ActiveAccount aggregate itself,
// so AggregateId is always this account's own id regardless of whether this account initiated the
// change (deposit/withdrawal/rename/send) or is only the target of someone else's transfer
// (MoneyTransferReceivedEvent) or its refund (MoneyTransferFailedEvent) - no separate "is this
// about me" lookup needed, unlike ClientDetails.vue's bank-card events.
const ACCOUNT_RELEVANT_EVENTS = new Set([
  "AccountNameChangedEvent",
  "CashDepositedEvent",
  "CashWithdrawnEvent",
  "MoneyTransferSendEvent",
  "MoneyTransferReceivedEvent",
  "MoneyTransferFailedEvent",
]);

export function shouldRefreshAccountDetails(event: EventEnvelope, accountId: string): boolean {
  return ACCOUNT_RELEVANT_EVENTS.has(event.eventType) && event.aggregateId === accountId;
}

import { describe, expect, it } from "vitest";
import type { EventEnvelope } from "./eventBus";
import { shouldRefreshAccountDetails, shouldRefreshClientDetails, shouldRefreshClientSearch } from "./refreshRules";

function event(eventType: string, aggregateId = "irrelevant"): EventEnvelope {
  return { id: "e1", aggregateId, version: 1, eventType, occurredAt: "2026-01-01T00:00:00Z", payload: null };
}

describe("shouldRefreshClientSearch", () => {
  it("refreshes on ClientCreatedEvent", () => {
    expect(shouldRefreshClientSearch(event("ClientCreatedEvent"))).toBe(true);
  });

  it("ignores every other event type", () => {
    expect(shouldRefreshClientSearch(event("ClientNameChangedEvent"))).toBe(false);
    expect(shouldRefreshClientSearch(event("CashDepositedEvent"))).toBe(false);
  });
});

describe("shouldRefreshClientDetails", () => {
  const clientId = "client-1";
  const bankCardIds = ["card-1", "card-2"];

  it("refreshes on a client-level event addressed to this client", () => {
    expect(shouldRefreshClientDetails(event("ClientNameChangedEvent", clientId), clientId, bankCardIds)).toBe(true);
    expect(shouldRefreshClientDetails(event("ClientMovedEvent", clientId), clientId, bankCardIds)).toBe(true);
    expect(shouldRefreshClientDetails(event("ClientPhoneNumberChangedEvent", clientId), clientId, bankCardIds)).toBe(true);
    expect(shouldRefreshClientDetails(event("AccountToClientAssignedEvent", clientId), clientId, bankCardIds)).toBe(true);
    expect(shouldRefreshClientDetails(event("NewBankCardForAccountAsignedEvent", clientId), clientId, bankCardIds)).toBe(true);
  });

  it("ignores a client-level event addressed to a different client", () => {
    expect(shouldRefreshClientDetails(event("ClientNameChangedEvent", "someone-else"), clientId, bankCardIds)).toBe(false);
  });

  it("refreshes on a bank-card event whose aggregateId is one of this client's own cards", () => {
    expect(shouldRefreshClientDetails(event("BankCardWasCanceledByClientEvent", "card-1"), clientId, bankCardIds)).toBe(true);
    expect(shouldRefreshClientDetails(event("BankCardWasReportedStolenEvent", "card-2"), clientId, bankCardIds)).toBe(true);
  });

  it("ignores a bank-card event for a card that isn't one of this client's own", () => {
    expect(shouldRefreshClientDetails(event("BankCardWasCanceledByClientEvent", "someone-elses-card"), clientId, bankCardIds)).toBe(false);
  });

  it("ignores unrelated event types entirely", () => {
    expect(shouldRefreshClientDetails(event("CashDepositedEvent", clientId), clientId, bankCardIds)).toBe(false);
  });
});

describe("shouldRefreshAccountDetails", () => {
  const accountId = "account-1";

  it("refreshes on every relevant event addressed to this account", () => {
    for (const type of [
      "AccountNameChangedEvent",
      "CashDepositedEvent",
      "CashWithdrawnEvent",
      "MoneyTransferSendEvent",
      "MoneyTransferReceivedEvent",
      "MoneyTransferFailedEvent",
    ]) {
      expect(shouldRefreshAccountDetails(event(type, accountId), accountId)).toBe(true);
    }
  });

  it("ignores a relevant event type addressed to a different account", () => {
    expect(shouldRefreshAccountDetails(event("CashDepositedEvent", "other-account"), accountId)).toBe(false);
  });

  it("ignores an unrelated event type even when addressed to this account", () => {
    expect(shouldRefreshAccountDetails(event("ClientNameChangedEvent", accountId), accountId)).toBe(false);
  });
});

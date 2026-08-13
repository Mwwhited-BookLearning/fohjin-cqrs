import { describe, expect, it } from "vitest";
import { matchesEventTypeFilter } from "./Monitoring.config";

describe("matchesEventTypeFilter", () => {
  it("matches every event type when nothing is selected", () => {
    expect(matchesEventTypeFilter("ClientCreatedEvent", [])).toBe(true);
    expect(matchesEventTypeFilter("AccountOpenedEvent", [])).toBe(true);
  });

  it("matches only the selected event types", () => {
    const selected = ["ClientCreatedEvent", "AccountOpenedEvent"];
    expect(matchesEventTypeFilter("ClientCreatedEvent", selected)).toBe(true);
    expect(matchesEventTypeFilter("AccountOpenedEvent", selected)).toBe(true);
    expect(matchesEventTypeFilter("AccountClosedEvent", selected)).toBe(false);
  });

  it("matches exact-cased class names only", () => {
    expect(matchesEventTypeFilter("clientcreatedevent", ["ClientCreatedEvent"])).toBe(false);
  });
});

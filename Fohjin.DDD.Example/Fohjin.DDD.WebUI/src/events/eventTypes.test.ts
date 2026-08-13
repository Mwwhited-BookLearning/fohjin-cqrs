import { describe, expect, it } from "vitest";
import { ACCOUNT_EVENT_TYPES, ALL_EVENT_TYPES, CLIENT_EVENT_TYPES } from "./eventTypes";

describe("eventTypes", () => {
  it("has no duplicate entries", () => {
    expect(new Set(ALL_EVENT_TYPES).size).toBe(ALL_EVENT_TYPES.length);
  });

  it("matches the current count of concrete domain event classes (17: 9 Account + 8 Client)", () => {
    expect(ACCOUNT_EVENT_TYPES.length).toBe(9);
    expect(CLIENT_EVENT_TYPES.length).toBe(8);
    expect(ALL_EVENT_TYPES.length).toBe(17);
  });

  it("is exactly the union of the two group arrays, in order", () => {
    expect(ALL_EVENT_TYPES).toEqual([...ACCOUNT_EVENT_TYPES, ...CLIENT_EVENT_TYPES]);
  });
});

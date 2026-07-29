import { describe, expect, it } from "vitest";
import type { ClientReport } from "../api/generated-client";
import { matchesSearchTerm } from "./ClientSearch.config";

function client(name: string | undefined): ClientReport {
  return { name } as ClientReport;
}

describe("matchesSearchTerm", () => {
  it("matches everything when the term is empty or whitespace", () => {
    expect(matchesSearchTerm(client("Alice"), "")).toBe(true);
    expect(matchesSearchTerm(client("Alice"), "   ")).toBe(true);
  });

  it("matches a case-insensitive substring of the client's name", () => {
    expect(matchesSearchTerm(client("Alice Smith"), "smith")).toBe(true);
    expect(matchesSearchTerm(client("Alice Smith"), "ALICE")).toBe(true);
  });

  it("does not match an unrelated term", () => {
    expect(matchesSearchTerm(client("Alice Smith"), "Bob")).toBe(false);
  });

  it("does not match when the client has no name", () => {
    expect(matchesSearchTerm(client(undefined), "anything")).toBe(false);
  });
});

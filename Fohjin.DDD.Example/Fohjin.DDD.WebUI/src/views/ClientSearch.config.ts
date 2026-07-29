import type { ClientReport } from "../api/generated-client";

// Structure layer (docs/patterns/vue-architecture.md): the search box's matching rule,
// extracted so it's testable independent of the component - client-side only, since neither
// this screen nor any other client actually calls the /odata/Clients $filter endpoint today
// (docs/08-reporting-read-models.md).
export function matchesSearchTerm(client: ClientReport, term: string): boolean {
  const trimmed = term.trim().toLowerCase();
  if (!trimmed) return true;
  return client.name?.toLowerCase().includes(trimmed) ?? false;
}

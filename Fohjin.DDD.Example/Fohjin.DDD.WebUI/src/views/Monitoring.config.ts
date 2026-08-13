// Structure layer (docs/patterns/vue-architecture.md): how many events this screen keeps
// around before dropping the oldest - a display-shape decision, not business logic.
export const MAX_MONITORING_EVENTS = 200;

// Structure layer: the event-type filter's matching rule, extracted so it's testable
// independent of the component - same pattern as ClientSearch.config.ts's matchesSearchTerm.
// No selection means no filter at all (show everything), not "match nothing".
export function matchesEventTypeFilter(eventType: string, selectedEventTypes: readonly string[]): boolean {
  return selectedEventTypes.length === 0 || selectedEventTypes.includes(eventType);
}

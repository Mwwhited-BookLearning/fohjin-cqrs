import { defineStore } from "pinia";
import type { ConnectionStatus, EventEnvelope } from "../events/eventBus";

export const useMonitoringStore = defineStore("monitoring", {
  state: () => ({
    events: [] as EventEnvelope[],
    status: "disconnected" as ConnectionStatus,
    paused: false,
    // Empty array = no filter = show every event type (Monitoring.config.ts's matchesEventTypeFilter).
    filter: { eventTypes: [] as string[] },
  }),
});

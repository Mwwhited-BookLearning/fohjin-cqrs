import { defineStore } from "pinia";
import type { ConnectionStatus, EventEnvelope } from "../events/eventBus";

export const useMonitoringStore = defineStore("monitoring", {
  state: () => ({
    events: [] as EventEnvelope[],
    status: "disconnected" as ConnectionStatus,
    paused: false,
    filter: { eventType: "" },
  }),
});

import { storeToRefs } from "pinia";
import { useMonitoringStore } from "../stores/monitoring";
import { onStatusChange, subscribe } from "../events/eventBus";
import { MAX_MONITORING_EVENTS } from "../views/Monitoring.config";

export function useMonitoring() {
  const store = useMonitoringStore();
  const { events, status, paused, filter } = storeToRefs(store);

  // This is just a subscriber of the one shared app-wide connection (src/events/eventBus.ts) -
  // "paused" only stops appending to this screen's own list, it doesn't touch the underlying
  // connection, which stays open for every other screen using it too (docs/07-messaging-bus.md's
  // pattern applied client-side: one shared stream, N independent subscribers).
  function watchLiveEvents(): () => void {
    const unsubscribeStatus = onStatusChange((next) => (store.status = next));
    const unsubscribe = subscribe((envelope) => {
      if (store.paused) return;
      if (store.filter.eventType.trim() && envelope.eventType !== store.filter.eventType.trim()) return;

      store.events.unshift(envelope);
      if (store.events.length > MAX_MONITORING_EVENTS) store.events.length = MAX_MONITORING_EVENTS;
    });
    return () => {
      unsubscribe();
      unsubscribeStatus();
    };
  }

  function togglePaused() {
    store.paused = !store.paused;
  }

  function clear() {
    store.events = [];
  }

  return { events, status, paused, filter, watchLiveEvents, togglePaused, clear };
}

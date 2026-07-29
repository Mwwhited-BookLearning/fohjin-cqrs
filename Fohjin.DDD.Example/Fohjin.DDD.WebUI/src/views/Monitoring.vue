<script setup lang="ts">
import { onBeforeUnmount, onMounted, reactive, ref } from "vue";
import { onStatusChange, subscribe, type ConnectionStatus, type EventEnvelope } from "../events/eventBus";

const events = ref<EventEnvelope[]>([]);
const status = ref<ConnectionStatus>("disconnected");
const paused = ref(false);
const filter = reactive({ eventType: "" });

const MAX_EVENTS = 200;
let unsubscribe: (() => void) | null = null;
let unsubscribeStatus: (() => void) | null = null;

// This is now just a subscriber of the one shared app-wide connection (src/events/eventBus.ts)
// - "paused" only stops appending to this screen's own list, it doesn't touch the underlying
// connection, which stays open for every other screen using it too (docs/07-messaging-bus.md's
// pattern applied client-side: one shared stream, N independent subscribers).
onMounted(() => {
  unsubscribeStatus = onStatusChange((next) => (status.value = next));
  unsubscribe = subscribe((envelope) => {
    if (paused.value) return;
    if (filter.eventType.trim() && envelope.eventType !== filter.eventType.trim()) return;

    events.value.unshift(envelope);
    if (events.value.length > MAX_EVENTS) events.value.length = MAX_EVENTS;
  });
});

onBeforeUnmount(() => {
  unsubscribe?.();
  unsubscribeStatus?.();
});

function clear() {
  events.value = [];
}
</script>

<template>
  <div class="monitoring">
    <h1>Monitoring</h1>
    <div class="toolbar">
      <label>Filter by event type <input v-model="filter.eventType" placeholder="e.g. ClientCreatedEvent" /></label>
      <button @click="paused = !paused">{{ paused ? "Resume" : "Pause" }}</button>
      <button @click="clear">Clear</button>
      <span class="status" :class="status">{{ status === "connected" ? "Live" : status === "connecting" ? "Connecting..." : "Disconnected" }}</span>
    </div>

    <ul class="event-list">
      <li v-for="event in events" :key="event.id">
        <span class="event-type">{{ event.eventType }}</span>
        <span class="event-time">{{ new Date(event.occurredAt).toLocaleTimeString() }}</span>
        <span class="event-aggregate">aggregate {{ event.aggregateId }} v{{ event.version }}</span>
      </li>
      <li v-if="!events.length" class="empty">No events yet.</li>
    </ul>
  </div>
</template>

<style scoped>
.toolbar {
  display: flex;
  align-items: flex-end;
  gap: 0.75rem;
  margin-bottom: 1rem;
}
.toolbar label {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.status {
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  background: #eee;
}
.status.connected {
  background: #d4edda;
  color: #155724;
}
.status.connecting {
  background: #fff3cd;
  color: #856404;
}
.event-list {
  list-style: none;
  padding: 0;
}
.event-list li {
  display: flex;
  gap: 1rem;
  padding: 0.5rem;
  border-bottom: 1px solid #eee;
  font-family: monospace;
}
.event-type {
  font-weight: bold;
  min-width: 16rem;
}
</style>

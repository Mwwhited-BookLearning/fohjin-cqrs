<script setup lang="ts">
import { onBeforeUnmount, onMounted } from "vue";
import { useMonitoring } from "../composables/useMonitoring";

const { events, status, paused, filter, watchLiveEvents, togglePaused, clear } = useMonitoring();

let stopWatching: (() => void) | null = null;
onMounted(() => {
  stopWatching = watchLiveEvents();
});
onBeforeUnmount(() => stopWatching?.());
</script>

<template>
  <div class="monitoring">
    <h1>Monitoring</h1>
    <div class="toolbar">
      <label>Filter by event type <input v-model="filter.eventType" placeholder="e.g. ClientCreatedEvent" /></label>
      <button @click="togglePaused">{{ paused ? "Resume" : "Pause" }}</button>
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
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-lg);
}
.toolbar label {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}
.status {
  padding: var(--spacing-xs) var(--spacing-sm);
  border-radius: var(--radius-sm);
  background: var(--color-muted-bg);
}
.status.connected {
  background: var(--color-success-bg);
  color: var(--color-success-text);
}
.status.connecting {
  background: var(--color-warning-bg);
  color: var(--color-warning-text);
}
.event-list {
  list-style: none;
  padding: 0;
}
.event-list li {
  display: flex;
  gap: var(--spacing-lg);
  padding: var(--spacing-sm);
  border-bottom: 1px solid var(--color-border);
  font-family: monospace;
}
.event-type {
  font-weight: bold;
  min-width: 16rem;
}
</style>

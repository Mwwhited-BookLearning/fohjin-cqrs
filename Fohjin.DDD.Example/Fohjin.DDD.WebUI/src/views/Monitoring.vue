<script setup lang="ts">
import { onBeforeUnmount, onMounted } from "vue";
import { useMonitoring } from "../composables/useMonitoring";
import { ACCOUNT_EVENT_TYPES, ALL_EVENT_TYPES, CLIENT_EVENT_TYPES } from "../events/eventTypes";

const { events, status, paused, filter, watchLiveEvents, togglePaused, clear } = useMonitoring();

function selectAllEventTypes() {
  filter.value.eventTypes = [...ALL_EVENT_TYPES];
}

function clearEventTypeFilter() {
  filter.value.eventTypes = [];
}

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
      <fieldset class="event-filter">
        <legend>Filter by event type (none selected = show all)</legend>
        <div class="event-filter-actions">
          <button type="button" @click="selectAllEventTypes">Select all</button>
          <button type="button" @click="clearEventTypeFilter">Clear filter</button>
        </div>
        <div class="event-filter-groups">
          <div class="event-filter-group">
            <h3>Account</h3>
            <label v-for="type in ACCOUNT_EVENT_TYPES" :key="type">
              <input type="checkbox" :value="type" v-model="filter.eventTypes" />{{ type }}
            </label>
          </div>
          <div class="event-filter-group">
            <h3>Client</h3>
            <label v-for="type in CLIENT_EVENT_TYPES" :key="type">
              <input type="checkbox" :value="type" v-model="filter.eventTypes" />{{ type }}
            </label>
          </div>
        </div>
      </fieldset>
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
.event-filter {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  padding: var(--spacing-sm);
}
.event-filter-actions {
  display: flex;
  gap: var(--spacing-sm);
  margin-bottom: var(--spacing-xs);
}
.event-filter-groups {
  display: flex;
  gap: var(--spacing-lg);
}
.event-filter-group {
  max-height: 10rem;
  overflow-y: auto;
}
.event-filter-group h3 {
  margin: 0 0 var(--spacing-xs);
  font-size: 0.9em;
}
.event-filter-group label {
  display: block;
  font-weight: normal;
  white-space: nowrap;
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

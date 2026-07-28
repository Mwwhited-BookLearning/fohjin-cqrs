<script setup lang="ts">
import { onBeforeUnmount, reactive, ref } from "vue";
import { getAccessToken } from "../auth/authService";

interface EventEnvelope {
  id: string;
  aggregateId: string;
  version: number;
  eventType: string;
  occurredAt: string;
  payload: unknown;
}

const events = ref<EventEnvelope[]>([]);
const connected = ref(false);
const error = ref<string | null>(null);
const filter = reactive({ eventType: "" });

const MAX_EVENTS = 200;
let abortController: AbortController | null = null;

// The generated FohjinApiClient.streamEvents() (NSwag has no real SSE support - it just
// returns Promise<void>) can't be used here, and the browser's native EventSource can't
// attach an Authorization header - so this reads the stream by hand: fetch() with a bearer
// token, then parse the wire format Results.ServerSentEvents writes itself ("event: <type>",
// "data: <json>", blank line between records - no "id:" line since
// Fohjin.DDD.WebApi/Program.cs's SseItem is constructed without one).
async function connect() {
  if (connected.value) return;
  error.value = null;
  abortController = new AbortController();

  try {
    const token = await getAccessToken();
    const url = new URL("/api/events", import.meta.env.VITE_API_BASE_URL);
    if (filter.eventType.trim()) {
      url.search = `$filter=${encodeURIComponent(`EventType eq '${filter.eventType.trim()}'`)}`;
    }

    const response = await fetch(url, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      signal: abortController.signal,
    });
    if (!response.ok || !response.body) {
      throw new Error(`Failed to connect: HTTP ${response.status}`);
    }

    connected.value = true;
    const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
    let buffer = "";

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += value;
      let separatorIndex: number;
      while ((separatorIndex = buffer.indexOf("\n\n")) !== -1) {
        const record = buffer.slice(0, separatorIndex);
        buffer = buffer.slice(separatorIndex + 2);
        handleRecord(record);
      }
    }
  } catch (e) {
    if (!(e instanceof DOMException && e.name === "AbortError")) {
      error.value = e instanceof Error ? e.message : String(e);
    }
  } finally {
    connected.value = false;
  }
}

function handleRecord(record: string) {
  const dataLines = record
    .split("\n")
    .filter((line) => line.startsWith("data:"))
    .map((line) => line.slice(5).trimStart());
  if (!dataLines.length) return;

  try {
    const envelope = JSON.parse(dataLines.join("\n")) as EventEnvelope;
    // WebApi/Program.cs's Stream local function sends this the instant a subscriber attaches,
    // purely to flush response headers right away (see EventEnvelope.Connected) - not a real
    // domain event, so it doesn't belong in the visible log.
    if (envelope.eventType === "StreamConnected") return;
    events.value.unshift(envelope);
    if (events.value.length > MAX_EVENTS) events.value.length = MAX_EVENTS;
  } catch {
    // Ignore malformed/partial records rather than crashing the whole stream view.
  }
}

function disconnect() {
  abortController?.abort();
  abortController = null;
  connected.value = false;
}

function clear() {
  events.value = [];
}

onBeforeUnmount(disconnect);
</script>

<template>
  <div class="monitoring">
    <h1>Monitoring</h1>
    <div class="toolbar">
      <label>Filter by event type <input v-model="filter.eventType" :disabled="connected" placeholder="e.g. ClientCreatedEvent" /></label>
      <button v-if="!connected" @click="connect">Connect</button>
      <button v-else @click="disconnect">Disconnect</button>
      <button @click="clear">Clear</button>
      <span class="status" :class="{ connected }">{{ connected ? "Live" : "Disconnected" }}</span>
    </div>

    <p v-if="error" class="error">{{ error }}</p>

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

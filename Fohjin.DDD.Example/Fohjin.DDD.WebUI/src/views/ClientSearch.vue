<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { apiClient } from "../api/client";
import type { ClientReport } from "../api/generated-client";
import { onReconnect, subscribe } from "../events/eventBus";
import { shouldRefreshClientSearch } from "../events/refreshRules";

const router = useRouter();
const clients = ref<ClientReport[]>([]);
const search = ref("");
const loading = ref(true);
const error = ref<string | null>(null);

const filteredClients = computed(() => {
  const term = search.value.trim().toLowerCase();
  if (!term) return clients.value;
  return clients.value.filter((c) => c.name?.toLowerCase().includes(term));
});

async function load() {
  loading.value = true;
  error.value = null;
  try {
    clients.value = await apiClient.getClients();
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);

// Event-driven refresh instead of a poll (src/events/refreshRules.ts for the rule itself).
const unsubscribe = subscribe((event) => {
  if (shouldRefreshClientSearch(event)) load();
});
// Reconciliation: catches anything missed while the shared connection wasn't up yet (eventBus.ts).
const unsubscribeReconnect = onReconnect(load);
onBeforeUnmount(() => {
  unsubscribe();
  unsubscribeReconnect();
});

function openClient(client: ClientReport) {
  router.push({ name: "client-details", params: { id: client.id } });
}
</script>

<template>
  <div class="client-search">
    <div class="toolbar">
      <input v-model="search" type="search" placeholder="Search clients..." />
      <button @click="router.push({ name: 'client-create' })">New client</button>
      <button @click="load">Refresh</button>
    </div>

    <p v-if="loading">Loading...</p>
    <p v-else-if="error" class="error">{{ error }}</p>
    <ul v-else class="client-list">
      <li v-for="client in filteredClients" :key="client.id" @click="openClient(client)">
        {{ client.name }}
      </li>
      <li v-if="filteredClients.length === 0">No clients found.</li>
    </ul>
  </div>
</template>

<style scoped>
.toolbar {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
}
.client-list {
  list-style: none;
  padding: 0;
}
.client-list li {
  padding: 0.5rem;
  cursor: pointer;
  border-bottom: 1px solid #eee;
}
.client-list li:hover {
  background: #f5f5f5;
}
</style>

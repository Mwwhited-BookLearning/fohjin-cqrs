<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import type { ClientReport } from "../api/generated-client";
import { useClientSearch } from "../composables/useClientSearch";
import { matchesSearchTerm } from "./ClientSearch.config";

const router = useRouter();
const search = ref("");
const { clients, loading, error, load, watchLiveEvents } = useClientSearch();

const filteredClients = computed(() => clients.value.filter((c) => matchesSearchTerm(c, search.value)));

onMounted(load);
const stopWatching = watchLiveEvents();
onBeforeUnmount(stopWatching);

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
  gap: var(--spacing-sm);
  margin-bottom: var(--spacing-lg);
}
.client-list {
  list-style: none;
  padding: 0;
}
.client-list li {
  padding: var(--spacing-sm);
  cursor: pointer;
  border-bottom: 1px solid var(--color-border);
}
.client-list li:hover {
  background: var(--color-hover-bg);
}
</style>

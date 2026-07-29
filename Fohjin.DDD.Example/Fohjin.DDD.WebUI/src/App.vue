<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRoute } from "vue-router";
import { getUser, logout } from "./auth/authService";

const route = useRoute();
const userName = ref<string | null>(null);

async function refreshUser() {
  const user = await getUser();
  userName.value = user?.profile.preferred_username ?? user?.profile.email ?? null;
}

onMounted(refreshUser);
</script>

<template>
  <div class="app">
    <nav v-if="!route.meta.public" class="app-nav">
      <RouterLink :to="{ name: 'clients' }">Client Search</RouterLink>
      <RouterLink :to="{ name: 'monitoring' }">Monitoring</RouterLink>
      <span class="app-nav-spacer" />
      <span v-if="userName">{{ userName }}</span>
      <button @click="logout">Sign out</button>
    </nav>
    <main>
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.app-nav {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.5rem 1rem;
  border-bottom: 1px solid #ddd;
}
.app-nav-spacer {
  flex: 1;
}
</style>

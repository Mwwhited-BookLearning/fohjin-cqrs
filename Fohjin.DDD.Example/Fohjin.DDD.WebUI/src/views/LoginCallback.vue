<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { completeLogin } from "../auth/authService";

const error = ref<string | null>(null);
const router = useRouter();

onMounted(async () => {
  try {
    await completeLogin();
    await router.replace({ name: "clients" });
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  }
});
</script>

<template>
  <div class="login-callback">
    <p v-if="!error">Signing in...</p>
    <p v-else class="error">Sign-in failed: {{ error }}</p>
  </div>
</template>

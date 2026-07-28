<script setup lang="ts">
import { reactive, ref } from "vue";
import { useRouter } from "vue-router";
import { apiClient } from "../api/client";
import { CreateClientRequest } from "../api/generated-client";

const router = useRouter();
const submitting = ref(false);
const error = ref<string | null>(null);

const form = reactive({
  clientName: "",
  street: "",
  streetNumber: "",
  postalCode: "",
  city: "",
  phoneNumber: "",
});

async function submit() {
  submitting.value = true;
  error.value = null;
  try {
    await apiClient.createClient(new CreateClientRequest(form));
    // CreateClientCommand.Id isn't the persisted client's id (Fohjin.DDD.WebApi/Program.cs) and
    // DirectBus.CommitAsync() is fire-and-forget (docs/07-messaging-bus.md), so there's no id to
    // navigate straight to - go back to the search list, which will show the new client once
    // the command has actually been handled.
    await router.push({ name: "clients" });
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <div class="client-create">
    <h1>New client</h1>
    <form @submit.prevent="submit">
      <label>Name <input v-model="form.clientName" required /></label>
      <label>Street <input v-model="form.street" required /></label>
      <label>Street number <input v-model="form.streetNumber" required /></label>
      <label>Postal code <input v-model="form.postalCode" required /></label>
      <label>City <input v-model="form.city" required /></label>
      <label>Phone number <input v-model="form.phoneNumber" required /></label>

      <p v-if="error" class="error">{{ error }}</p>
      <button type="submit" :disabled="submitting">{{ submitting ? "Creating..." : "Create client" }}</button>
    </form>
  </div>
</template>

<style scoped>
form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  max-width: 24rem;
}
label {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
</style>

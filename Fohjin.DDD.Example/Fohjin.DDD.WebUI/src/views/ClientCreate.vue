<script setup lang="ts">
import { useRouter } from "vue-router";
import { useClientCreate } from "../composables/useClientCreate";

const router = useRouter();
const { clientName, street, streetNumber, postalCode, city, phoneNumber, submitting, error, submit } = useClientCreate();

async function onSubmit() {
  const created = await submit();
  if (!created) return;
  // CreateClientCommand.Id isn't the persisted client's id (Fohjin.DDD.WebApi/Program.cs) and
  // DirectBus.CommitAsync() is fire-and-forget (docs/07-messaging-bus.md), so there's no id to
  // navigate straight to - go back to the search list, which will show the new client once
  // the command has actually been handled.
  await router.push({ name: "clients" });
}
</script>

<template>
  <div class="client-create">
    <h1>New client</h1>
    <form @submit.prevent="onSubmit">
      <label>Name <input v-model="clientName" required /></label>
      <label>Street <input v-model="street" required /></label>
      <label>Street number <input v-model="streetNumber" required /></label>
      <label>Postal code <input v-model="postalCode" required /></label>
      <label>City <input v-model="city" required /></label>
      <label>Phone number <input v-model="phoneNumber" required /></label>

      <p v-if="error" class="error">{{ error }}</p>
      <button type="submit" :disabled="submitting">{{ submitting ? "Creating..." : "Create client" }}</button>
    </form>
  </div>
</template>

<style scoped>
form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
  max-width: var(--content-max-width);
}
label {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}
</style>

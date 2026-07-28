<script setup lang="ts">
import { reactive, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { apiClient } from "../api/client";
import {
  ChangeClientNameRequest,
  ChangeClientPhoneNumberRequest,
  ClientIsMovingRequest,
  OpenNewAccountForClientRequest,
  type ClientDetailsReport,
} from "../api/generated-client";

const props = defineProps<{ id: string }>();
const router = useRouter();

const details = ref<ClientDetailsReport | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);

const nameForm = reactive({ clientName: "" });
const addressForm = reactive({ street: "", streetNumber: "", postalCode: "", city: "" });
const phoneForm = reactive({ phoneNumber: "" });
const newAccountForm = reactive({ accountName: "" });

const savingName = ref(false);
const savingAddress = ref(false);
const savingPhoneNumber = ref(false);
const openingAccount = ref(false);

async function load() {
  loading.value = true;
  error.value = null;
  try {
    details.value = await apiClient.getClientDetailsById(props.id);
    nameForm.clientName = details.value.clientName ?? "";
    addressForm.street = details.value.street ?? "";
    addressForm.streetNumber = details.value.streetNumber ?? "";
    addressForm.postalCode = details.value.postalCode ?? "";
    addressForm.city = details.value.city ?? "";
    phoneForm.phoneNumber = details.value.phoneNumber ?? "";
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    loading.value = false;
  }
}

watch(() => props.id, load, { immediate: true });

// DirectBus.CommitAsync() is fire-and-forget (docs/07-messaging-bus.md), so the read model
// updates asynchronously - reload shortly after each command, same pattern as the WinForms
// ClientDetailsPresenter's SystemTimer.Trigger(LoadDataAsync, 1000).
function reloadShortly() {
  setTimeout(load, 1000);
}

async function saveName() {
  savingName.value = true;
  error.value = null;
  try {
    await apiClient.changeClientName(props.id, new ChangeClientNameRequest(nameForm));
    reloadShortly();
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    savingName.value = false;
  }
}

async function saveAddress() {
  savingAddress.value = true;
  error.value = null;
  try {
    await apiClient.changeClientAddress(props.id, new ClientIsMovingRequest(addressForm));
    reloadShortly();
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    savingAddress.value = false;
  }
}

async function savePhoneNumber() {
  savingPhoneNumber.value = true;
  error.value = null;
  try {
    await apiClient.changeClientPhoneNumber(props.id, new ChangeClientPhoneNumberRequest(phoneForm));
    reloadShortly();
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    savingPhoneNumber.value = false;
  }
}

async function openNewAccount() {
  openingAccount.value = true;
  error.value = null;
  try {
    await apiClient.openNewAccountForClient(props.id, new OpenNewAccountForClientRequest(newAccountForm));
    newAccountForm.accountName = "";
    reloadShortly();
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    openingAccount.value = false;
  }
}
</script>

<template>
  <div class="client-details">
    <p v-if="loading">Loading...</p>
    <template v-else-if="details">
      <h1>{{ details.clientName }}</h1>
      <p v-if="error" class="error">{{ error }}</p>

      <section>
        <h2>Name</h2>
        <form @submit.prevent="saveName">
          <label>Name <input v-model="nameForm.clientName" required /></label>
          <button type="submit" :disabled="savingName">{{ savingName ? "Saving..." : "Save name" }}</button>
        </form>
      </section>

      <section>
        <h2>Address</h2>
        <form @submit.prevent="saveAddress">
          <label>Street <input v-model="addressForm.street" required /></label>
          <label>Street number <input v-model="addressForm.streetNumber" required /></label>
          <label>Postal code <input v-model="addressForm.postalCode" required /></label>
          <label>City <input v-model="addressForm.city" required /></label>
          <button type="submit" :disabled="savingAddress">{{ savingAddress ? "Saving..." : "Save address" }}</button>
        </form>
      </section>

      <section>
        <h2>Phone number</h2>
        <form @submit.prevent="savePhoneNumber">
          <label>Phone number <input v-model="phoneForm.phoneNumber" required /></label>
          <button type="submit" :disabled="savingPhoneNumber">{{ savingPhoneNumber ? "Saving..." : "Save phone number" }}</button>
        </form>
      </section>

      <section>
        <h2>Accounts</h2>
        <ul class="account-list">
          <li v-for="account in details.accounts" :key="account.id" @click="router.push({ name: 'account-details', params: { id: account.id } })">
            {{ account.accountName }} ({{ account.accountNumber }})
          </li>
          <li v-if="!details.accounts?.length" class="empty">No open accounts.</li>
        </ul>

        <h3>Closed accounts</h3>
        <ul class="account-list">
          <li v-for="account in details.closedAccounts" :key="account.id">
            {{ account.accountName }} ({{ account.accountNumber }})
          </li>
          <li v-if="!details.closedAccounts?.length" class="empty">No closed accounts.</li>
        </ul>

        <form @submit.prevent="openNewAccount">
          <label>New account name <input v-model="newAccountForm.accountName" required /></label>
          <button type="submit" :disabled="openingAccount">{{ openingAccount ? "Opening..." : "Open account" }}</button>
        </form>
      </section>
    </template>
    <p v-else class="error">{{ error }}</p>
  </div>
</template>

<style scoped>
.client-details section {
  max-width: 24rem;
  margin-bottom: 1.5rem;
}
form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}
label {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.account-list {
  list-style: none;
  padding: 0;
  margin-bottom: 1rem;
}
.account-list li {
  padding: 0.5rem;
  border-bottom: 1px solid #eee;
}
.account-list li:not(.empty) {
  cursor: pointer;
}
.account-list li:not(.empty):hover {
  background: #f5f5f5;
}
</style>

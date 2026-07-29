<script setup lang="ts">
import { onBeforeUnmount, reactive, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { apiClient } from "../api/client";
import {
  AssignNewBankCardRequest,
  ChangeClientNameRequest,
  ChangeClientPhoneNumberRequest,
  ClientIsMovingRequest,
  OpenNewAccountForClientRequest,
  type ClientDetailsReport,
} from "../api/generated-client";
import { onReconnect, subscribe } from "../events/eventBus";

const props = defineProps<{ id: string }>();
const router = useRouter();

const details = ref<ClientDetailsReport | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);

const nameForm = reactive({ clientName: "" });
const addressForm = reactive({ street: "", streetNumber: "", postalCode: "", city: "" });
const phoneForm = reactive({ phoneNumber: "" });
const newAccountForm = reactive({ accountName: "" });
const newBankCardForm = reactive({ accountId: "" });

const savingName = ref(false);
const savingAddress = ref(false);
const savingPhoneNumber = ref(false);
const openingAccount = ref(false);
const assigningBankCard = ref(false);
// Per-bank-card-id busy flag, so clicking Cancel/Report stolen on one card doesn't disable
// every other card's buttons while its request is in flight.
const bankCardActionInFlight = reactive<Record<string, boolean>>({});

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

// Event-driven refresh instead of a poll (src/events/eventBus.ts). Every event below is applied
// on the Client aggregate itself (AggregateId === props.id) except the two bank-card "disabled"
// events, which are applied on the BankCard entity instead (AggregateId === the card's own id,
// not the client's - see docs/02-bank-cards.md's note on this) - checked against the bank card
// ids already loaded into `details`, since there's no other way to know which client a bare
// bank-card id belongs to without a second lookup.
const CLIENT_LEVEL_EVENTS = new Set([
  "ClientNameChangedEvent",
  "ClientMovedEvent",
  "ClientPhoneNumberChangedEvent",
  "AccountToClientAssignedEvent",
  "NewBankCardForAccountAsignedEvent",
]);
const BANK_CARD_EVENTS = new Set(["BankCardWasCanceledByClientEvent", "BankCardWasReportedStolenEvent"]);

const unsubscribe = subscribe((event) => {
  if (CLIENT_LEVEL_EVENTS.has(event.eventType) && event.aggregateId === props.id) {
    load();
  } else if (BANK_CARD_EVENTS.has(event.eventType) && details.value?.bankCards?.some((c) => c.id === event.aggregateId)) {
    load();
  }
});
// Reconciliation: catches anything missed while the shared connection wasn't up yet (eventBus.ts).
const unsubscribeReconnect = onReconnect(load);
onBeforeUnmount(() => {
  unsubscribe();
  unsubscribeReconnect();
});

async function saveName() {
  savingName.value = true;
  error.value = null;
  try {
    await apiClient.changeClientName(props.id, new ChangeClientNameRequest(nameForm));
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
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    openingAccount.value = false;
  }
}

function accountLabel(accountId: string): string {
  const account = details.value?.accounts?.find((a) => a.id === accountId);
  return account ? `${account.accountName} (${account.accountNumber})` : accountId;
}

// AssignNewBankCardForAccount (Fohjin.DDD.Domain/Client.cs) guards that the account belongs to
// this client - only open accounts are ever in Client's own _accounts list, so only those are
// offered here (docs/02-bank-cards.md: this whole feature has no read-model card number/type,
// just an id + linked account + status - nothing here is invented beyond what the domain has).
async function assignNewBankCard() {
  assigningBankCard.value = true;
  error.value = null;
  try {
    await apiClient.assignNewBankCard(props.id, new AssignNewBankCardRequest(newBankCardForm));
    newBankCardForm.accountId = "";
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    assigningBankCard.value = false;
  }
}

async function cancelBankCard(bankCardId: string) {
  bankCardActionInFlight[bankCardId] = true;
  error.value = null;
  try {
    await apiClient.cancelBankCard(props.id, bankCardId);
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    bankCardActionInFlight[bankCardId] = false;
  }
}

async function reportBankCardStolen(bankCardId: string) {
  bankCardActionInFlight[bankCardId] = true;
  error.value = null;
  try {
    await apiClient.reportStolenBankCard(props.id, bankCardId);
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    bankCardActionInFlight[bankCardId] = false;
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

      <section>
        <h2>Bank cards</h2>
        <ul class="bank-card-list">
          <li v-for="card in details.bankCards" :key="card.id">
            <span class="bank-card-account">{{ accountLabel(card.accountId!) }}</span>
            <span class="bank-card-status" :class="card.status?.toLowerCase()">{{ card.status }}</span>
            <span class="bank-card-actions" v-if="card.status === 'Active'">
              <button :disabled="bankCardActionInFlight[card.id!]" @click="cancelBankCard(card.id!)">Cancel</button>
              <button :disabled="bankCardActionInFlight[card.id!]" @click="reportBankCardStolen(card.id!)">Report stolen</button>
            </span>
          </li>
          <li v-if="!details.bankCards?.length" class="empty">No bank cards.</li>
        </ul>

        <form @submit.prevent="assignNewBankCard" v-if="details.accounts?.length">
          <label>
            Account
            <select v-model="newBankCardForm.accountId" required>
              <option value="" disabled>Select an account</option>
              <option v-for="account in details.accounts" :key="account.id" :value="account.id">
                {{ account.accountName }} ({{ account.accountNumber }})
              </option>
            </select>
          </label>
          <button type="submit" :disabled="assigningBankCard">{{ assigningBankCard ? "Assigning..." : "Assign new bank card" }}</button>
        </form>
        <p v-else class="empty">Open an account before assigning a bank card.</p>
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
.bank-card-list {
  list-style: none;
  padding: 0;
  margin-bottom: 1rem;
}
.bank-card-list li {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem;
  border-bottom: 1px solid #eee;
}
.bank-card-status {
  padding: 0.15rem 0.5rem;
  border-radius: 4px;
  background: #eee;
  font-size: 0.85rem;
}
.bank-card-status.active {
  background: #d4edda;
  color: #155724;
}
.bank-card-status.cancelled,
.bank-card-status.reportedstolen {
  background: #f8d7da;
  color: #721c24;
}
.bank-card-actions {
  margin-left: auto;
  display: flex;
  gap: 0.5rem;
}
</style>

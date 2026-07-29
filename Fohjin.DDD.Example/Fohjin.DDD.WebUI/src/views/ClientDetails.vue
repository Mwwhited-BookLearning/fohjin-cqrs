<script setup lang="ts">
import { onBeforeUnmount, watch } from "vue";
import { useRouter } from "vue-router";
import { useClientDetails } from "../composables/useClientDetails";

const props = defineProps<{ id: string }>();
const router = useRouter();

const {
  details, loading, error,
  nameForm, addressForm, phoneForm, newAccountForm, newBankCardForm,
  savingName, savingAddress, savingPhoneNumber, openingAccount, assigningBankCard, bankCardActionInFlight,
  load, watchLiveEvents, saveName, saveAddress, savePhoneNumber, openNewAccount,
  accountLabel, assignNewBankCard, cancelBankCard, reportBankCardStolen,
} = useClientDetails(() => props.id);

watch(() => props.id, load, { immediate: true });
const stopWatching = watchLiveEvents();
onBeforeUnmount(stopWatching);
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
  max-width: var(--content-max-width);
  margin-bottom: var(--spacing-xl);
}
form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}
label {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}
.account-list {
  list-style: none;
  padding: 0;
  margin-bottom: var(--spacing-lg);
}
.account-list li {
  padding: var(--spacing-sm);
  border-bottom: 1px solid var(--color-border);
}
.account-list li:not(.empty) {
  cursor: pointer;
}
.account-list li:not(.empty):hover {
  background: var(--color-hover-bg);
}
.bank-card-list {
  list-style: none;
  padding: 0;
  margin-bottom: var(--spacing-lg);
}
.bank-card-list li {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-sm);
  border-bottom: 1px solid var(--color-border);
}
.bank-card-status {
  padding: 0.15rem var(--spacing-sm);
  border-radius: var(--radius-sm);
  background: var(--color-muted-bg);
  font-size: 0.85rem;
}
.bank-card-status.active {
  background: var(--color-success-bg);
  color: var(--color-success-text);
}
.bank-card-status.cancelled,
.bank-card-status.reportedstolen {
  background: var(--color-danger-bg);
  color: var(--color-danger-text);
}
.bank-card-actions {
  margin-left: auto;
  display: flex;
  gap: var(--spacing-sm);
}
</style>

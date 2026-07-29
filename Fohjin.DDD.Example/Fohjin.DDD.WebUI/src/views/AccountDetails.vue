<script setup lang="ts">
import { onBeforeUnmount, watch } from "vue";
import { useAccountDetails } from "../composables/useAccountDetails";

const props = defineProps<{ id: string }>();

const {
  details, otherAccounts, loading, error,
  nameForm, depositForm, withdrawalForm, transferForm,
  savingName, depositing, withdrawing, transferring, closing,
  load, watchLiveEvents, saveName, deposit, withdraw, transfer, closeAccount,
} = useAccountDetails(() => props.id);

watch(() => props.id, load, { immediate: true });
const stopWatching = watchLiveEvents();
onBeforeUnmount(stopWatching);
</script>

<template>
  <div class="account-details">
    <p v-if="loading">Loading...</p>
    <template v-else-if="details">
      <h1>{{ details.accountName }}</h1>
      <p class="subtitle">Account number: {{ details.accountNumber }} &middot; Balance: {{ details.balance }}</p>
      <p v-if="error" class="error">{{ error }}</p>

      <section>
        <h2>Name</h2>
        <form @submit.prevent="saveName">
          <label>Name <input v-model="nameForm.accountName" required /></label>
          <button type="submit" :disabled="savingName">{{ savingName ? "Saving..." : "Save name" }}</button>
        </form>
      </section>

      <section>
        <h2>Deposit</h2>
        <form @submit.prevent="deposit">
          <label>Amount <input v-model.number="depositForm.amount" type="number" min="0.01" step="0.01" required /></label>
          <button type="submit" :disabled="depositing">{{ depositing ? "Depositing..." : "Deposit" }}</button>
        </form>
      </section>

      <section>
        <h2>Withdrawal</h2>
        <form @submit.prevent="withdraw">
          <label>Amount <input v-model.number="withdrawalForm.amount" type="number" min="0.01" step="0.01" required /></label>
          <button type="submit" :disabled="withdrawing">{{ withdrawing ? "Withdrawing..." : "Withdraw" }}</button>
        </form>
      </section>

      <section>
        <h2>Transfer</h2>
        <form @submit.prevent="transfer">
          <label>Amount <input v-model.number="transferForm.amount" type="number" min="0.01" step="0.01" required /></label>
          <label>
            To account
            <select v-model="transferForm.accountNumber" required>
              <option value="" disabled>Select an account</option>
              <option v-for="account in otherAccounts" :key="account.id" :value="account.accountNumber">
                {{ account.accountName }} ({{ account.accountNumber }})
              </option>
            </select>
          </label>
          <button type="submit" :disabled="transferring">{{ transferring ? "Transferring..." : "Transfer" }}</button>
        </form>
      </section>

      <section>
        <h2>Transaction history</h2>
        <ul class="ledger-list">
          <li v-for="ledger in details.ledgers" :key="ledger.id">
            {{ ledger.action }}: {{ ledger.amount }}
          </li>
          <li v-if="!details.ledgers?.length" class="empty">No transactions yet.</li>
        </ul>
      </section>

      <section>
        <button class="danger" :disabled="closing" @click="closeAccount">{{ closing ? "Closing..." : "Close account" }}</button>
      </section>
    </template>
    <p v-else class="error">{{ error }}</p>
  </div>
</template>

<style scoped>
.account-details section {
  max-width: var(--content-max-width);
  margin-bottom: var(--spacing-xl);
}
.subtitle {
  color: #666;
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
.ledger-list {
  list-style: none;
  padding: 0;
}
.ledger-list li {
  padding: var(--spacing-sm);
  border-bottom: 1px solid var(--color-border);
}
button.danger {
  background: var(--color-danger);
  color: var(--color-danger-contrast);
  border: none;
  padding: var(--spacing-sm) var(--spacing-lg);
  border-radius: var(--radius-sm);
  cursor: pointer;
}
</style>

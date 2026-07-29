<script setup lang="ts">
import { onBeforeUnmount, reactive, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { apiClient } from "../api/client";
import {
  ChangeAccountNameRequest,
  DepositCashRequest,
  SendMoneyTransferRequest,
  WithdrawalCashRequest,
  type AccountDetailsReport,
  type AccountReport,
} from "../api/generated-client";
import { onReconnect, subscribe } from "../events/eventBus";
import { shouldRefreshAccountDetails } from "../events/refreshRules";

const props = defineProps<{ id: string }>();
const router = useRouter();

const details = ref<AccountDetailsReport | null>(null);
const otherAccounts = ref<AccountReport[]>([]);
const loading = ref(true);
const error = ref<string | null>(null);

const nameForm = reactive({ accountName: "" });
const depositForm = reactive({ amount: 0 });
const withdrawalForm = reactive({ amount: 0 });
const transferForm = reactive({ amount: 0, accountNumber: "" });

const savingName = ref(false);
const depositing = ref(false);
const withdrawing = ref(false);
const transferring = ref(false);
const closing = ref(false);

async function load() {
  loading.value = true;
  error.value = null;
  try {
    details.value = await apiClient.getAccountDetailsById(props.id);
    nameForm.accountName = details.value.accountName ?? "";
    otherAccounts.value = (await apiClient.getAccounts()).filter((a) => a.id !== props.id);
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    loading.value = false;
  }
}

watch(() => props.id, load, { immediate: true });

// Event-driven refresh instead of a poll (rule itself lives in src/events/refreshRules.ts, unit
// tested there).
const unsubscribe = subscribe((event) => {
  if (shouldRefreshAccountDetails(event, props.id)) load();
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
    await apiClient.changeAccountName(props.id, new ChangeAccountNameRequest(nameForm));
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    savingName.value = false;
  }
}

async function deposit() {
  depositing.value = true;
  error.value = null;
  try {
    await apiClient.depositCash(props.id, new DepositCashRequest(depositForm));
    depositForm.amount = 0;
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    depositing.value = false;
  }
}

async function withdraw() {
  withdrawing.value = true;
  error.value = null;
  try {
    await apiClient.withdrawalCash(props.id, new WithdrawalCashRequest(withdrawalForm));
    withdrawalForm.amount = 0;
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    withdrawing.value = false;
  }
}

async function transfer() {
  transferring.value = true;
  error.value = null;
  try {
    await apiClient.sendMoneyTransfer(props.id, new SendMoneyTransferRequest(transferForm));
    transferForm.amount = 0;
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    transferring.value = false;
  }
}

async function closeAccount() {
  closing.value = true;
  error.value = null;
  try {
    await apiClient.closeAccount(props.id);
    await router.push({ name: "clients" });
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e);
  } finally {
    closing.value = false;
  }
}
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
  max-width: 24rem;
  margin-bottom: 1.5rem;
}
.subtitle {
  color: #666;
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
.ledger-list {
  list-style: none;
  padding: 0;
}
.ledger-list li {
  padding: 0.5rem;
  border-bottom: 1px solid #eee;
}
button.danger {
  background: #c0392b;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  cursor: pointer;
}
</style>

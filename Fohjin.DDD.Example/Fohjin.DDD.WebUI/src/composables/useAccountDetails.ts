import { storeToRefs } from "pinia";
import { useRouter } from "vue-router";
import { apiClient } from "../api/client";
import {
  ChangeAccountNameRequest,
  DepositCashRequest,
  SendMoneyTransferRequest,
  WithdrawalCashRequest,
} from "../api/generated-client";
import { useAccountDetailsStore } from "../stores/accountDetails";
import { onReconnect, subscribe } from "../events/eventBus";
import { shouldRefreshAccountDetails } from "../events/refreshRules";

export function useAccountDetails(accountId: () => string) {
  const router = useRouter();
  const store = useAccountDetailsStore();
  const {
    details, otherAccounts, loading, error,
    nameForm, depositForm, withdrawalForm, transferForm,
    savingName, depositing, withdrawing, transferring, closing,
  } = storeToRefs(store);

  async function load() {
    store.loading = true;
    store.error = null;
    try {
      store.details = await apiClient.getAccountDetailsById(accountId());
      store.nameForm.accountName = store.details.accountName ?? "";
      store.otherAccounts = (await apiClient.getAccounts()).filter((a) => a.id !== accountId());
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.loading = false;
    }
  }

  // See useClientDetails.ts's watchLiveEvents for why this reloads twice (a short-delay
  // reconciliation retry, since the SSE notification can race ahead of the read model's own
  // write - found live while verifying this refactor).
  function watchLiveEvents(): () => void {
    const unsubscribe = subscribe((event) => {
      if (shouldRefreshAccountDetails(event, accountId())) {
        load();
        setTimeout(load, 750);
      }
    });
    const unsubscribeReconnect = onReconnect(load);
    return () => {
      unsubscribe();
      unsubscribeReconnect();
    };
  }

  async function saveName() {
    store.savingName = true;
    store.error = null;
    try {
      await apiClient.changeAccountName(accountId(), new ChangeAccountNameRequest(store.nameForm));
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.savingName = false;
    }
  }

  async function deposit() {
    store.depositing = true;
    store.error = null;
    try {
      await apiClient.depositCash(accountId(), new DepositCashRequest(store.depositForm));
      store.depositForm.amount = 0;
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.depositing = false;
    }
  }

  async function withdraw() {
    store.withdrawing = true;
    store.error = null;
    try {
      await apiClient.withdrawalCash(accountId(), new WithdrawalCashRequest(store.withdrawalForm));
      store.withdrawalForm.amount = 0;
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.withdrawing = false;
    }
  }

  async function transfer() {
    store.transferring = true;
    store.error = null;
    try {
      await apiClient.sendMoneyTransfer(accountId(), new SendMoneyTransferRequest(store.transferForm));
      store.transferForm.amount = 0;
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.transferring = false;
    }
  }

  async function closeAccount() {
    store.closing = true;
    store.error = null;
    try {
      await apiClient.closeAccount(accountId());
      await router.push({ name: "clients" });
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.closing = false;
    }
  }

  return {
    details, otherAccounts, loading, error,
    nameForm, depositForm, withdrawalForm, transferForm,
    savingName, depositing, withdrawing, transferring, closing,
    load, watchLiveEvents, saveName, deposit, withdraw, transfer, closeAccount,
  };
}

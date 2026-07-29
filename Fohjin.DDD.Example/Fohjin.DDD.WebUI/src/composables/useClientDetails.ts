import { storeToRefs } from "pinia";
import { apiClient } from "../api/client";
import {
  AssignNewBankCardRequest,
  ChangeClientNameRequest,
  ChangeClientPhoneNumberRequest,
  ClientIsMovingRequest,
  OpenNewAccountForClientRequest,
} from "../api/generated-client";
import { useClientDetailsStore } from "../stores/clientDetails";
import { onReconnect, subscribe } from "../events/eventBus";
import { shouldRefreshClientDetails } from "../events/refreshRules";

export function useClientDetails(clientId: () => string) {
  const store = useClientDetailsStore();
  const {
    details, loading, error,
    nameForm, addressForm, phoneForm, newAccountForm, newBankCardForm,
    savingName, savingAddress, savingPhoneNumber, openingAccount, assigningBankCard, bankCardActionInFlight,
  } = storeToRefs(store);

  async function load() {
    store.loading = true;
    store.error = null;
    try {
      store.details = await apiClient.getClientDetailsById(clientId());
      store.nameForm.clientName = store.details.clientName ?? "";
      store.addressForm.street = store.details.street ?? "";
      store.addressForm.streetNumber = store.details.streetNumber ?? "";
      store.addressForm.postalCode = store.details.postalCode ?? "";
      store.addressForm.city = store.details.city ?? "";
      store.phoneForm.phoneNumber = store.details.phoneNumber ?? "";
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.loading = false;
    }
  }

  // Event-driven refresh instead of a poll (rule itself lives in src/events/refreshRules.ts,
  // unit tested there - this is just wiring it up to this screen's own client id and loaded
  // card ids). The SSE notification and the reporting-store's own DB write are independent Rx
  // subscriptions on the same domain event (07-messaging-bus.md) with no ordering guarantee
  // between them - found live (a rename's reload sometimes fired and re-fetched before the read
  // model had actually finished updating, silently showing the pre-rename name until a manual
  // page reload). The immediate reload is still worth doing (it's usually already caught up),
  // but a short follow-up reload is the same reconciliation idea `onReconnect` already uses,
  // just for "raced ahead of the read model" instead of "was disconnected."
  function watchLiveEvents(): () => void {
    const unsubscribe = subscribe((event) => {
      const bankCardIds = store.details?.bankCards?.map((c) => c.id!) ?? [];
      if (shouldRefreshClientDetails(event, clientId(), bankCardIds)) {
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
      await apiClient.changeClientName(clientId(), new ChangeClientNameRequest(store.nameForm));
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.savingName = false;
    }
  }

  async function saveAddress() {
    store.savingAddress = true;
    store.error = null;
    try {
      await apiClient.changeClientAddress(clientId(), new ClientIsMovingRequest(store.addressForm));
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.savingAddress = false;
    }
  }

  async function savePhoneNumber() {
    store.savingPhoneNumber = true;
    store.error = null;
    try {
      await apiClient.changeClientPhoneNumber(clientId(), new ChangeClientPhoneNumberRequest(store.phoneForm));
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.savingPhoneNumber = false;
    }
  }

  async function openNewAccount() {
    store.openingAccount = true;
    store.error = null;
    try {
      await apiClient.openNewAccountForClient(clientId(), new OpenNewAccountForClientRequest(store.newAccountForm));
      store.newAccountForm.accountName = "";
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.openingAccount = false;
    }
  }

  function accountLabel(accountId: string): string {
    const account = store.details?.accounts?.find((a) => a.id === accountId);
    return account ? `${account.accountName} (${account.accountNumber})` : accountId;
  }

  // AssignNewBankCardForAccount (Fohjin.DDD.Domain/Client.cs) guards that the account belongs to
  // this client - only open accounts are ever in Client's own _accounts list, so only those are
  // offered here (docs/02-bank-cards.md: this whole feature has no read-model card
  // number/type, just an id + linked account + status - nothing here is invented beyond what
  // the domain has).
  async function assignNewBankCard() {
    store.assigningBankCard = true;
    store.error = null;
    try {
      await apiClient.assignNewBankCard(clientId(), new AssignNewBankCardRequest(store.newBankCardForm));
      store.newBankCardForm.accountId = "";
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.assigningBankCard = false;
    }
  }

  async function cancelBankCard(bankCardId: string) {
    store.bankCardActionInFlight[bankCardId] = true;
    store.error = null;
    try {
      await apiClient.cancelBankCard(clientId(), bankCardId);
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.bankCardActionInFlight[bankCardId] = false;
    }
  }

  async function reportBankCardStolen(bankCardId: string) {
    store.bankCardActionInFlight[bankCardId] = true;
    store.error = null;
    try {
      await apiClient.reportStolenBankCard(clientId(), bankCardId);
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
    } finally {
      store.bankCardActionInFlight[bankCardId] = false;
    }
  }

  return {
    details, loading, error,
    nameForm, addressForm, phoneForm, newAccountForm, newBankCardForm,
    savingName, savingAddress, savingPhoneNumber, openingAccount, assigningBankCard, bankCardActionInFlight,
    load, watchLiveEvents, saveName, saveAddress, savePhoneNumber, openNewAccount,
    accountLabel, assignNewBankCard, cancelBankCard, reportBankCardStolen,
  };
}

import { storeToRefs } from "pinia";
import { apiClient } from "../api/client";
import { useClientSearchStore } from "../stores/clientSearch";
import { onReconnect, subscribe } from "../events/eventBus";
import { shouldRefreshClientSearch } from "../events/refreshRules";

export function useClientSearch() {
  const store = useClientSearchStore();
  const { clients, loading, error } = storeToRefs(store);

  async function load() {
    store.loading = true;
    store.error = null;
    try {
      store.clients = await apiClient.getClients();
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
      if (shouldRefreshClientSearch(event)) {
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

  return { clients, loading, error, load, watchLiveEvents };
}

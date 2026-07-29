import { storeToRefs } from "pinia";
import { apiClient } from "../api/client";
import { CreateClientRequest } from "../api/generated-client";
import { useClientCreateStore } from "../stores/clientCreate";

export function useClientCreate() {
  const store = useClientCreateStore();
  const { clientName, street, streetNumber, postalCode, city, phoneNumber, submitting, error } = storeToRefs(store);

  async function submit(): Promise<boolean> {
    store.submitting = true;
    store.error = null;
    try {
      await apiClient.createClient(new CreateClientRequest({
        clientName: store.clientName,
        street: store.street,
        streetNumber: store.streetNumber,
        postalCode: store.postalCode,
        city: store.city,
        phoneNumber: store.phoneNumber,
      }));
      return true;
    } catch (e) {
      store.error = e instanceof Error ? e.message : String(e);
      return false;
    } finally {
      store.submitting = false;
    }
  }

  return { clientName, street, streetNumber, postalCode, city, phoneNumber, submitting, error, submit };
}

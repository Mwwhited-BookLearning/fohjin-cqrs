import { defineStore } from "pinia";
import type { ClientDetailsReport } from "../api/generated-client";

export const useClientDetailsStore = defineStore("clientDetails", {
  state: () => ({
    details: null as ClientDetailsReport | null,
    loading: true,
    error: null as string | null,

    nameForm: { clientName: "" },
    addressForm: { street: "", streetNumber: "", postalCode: "", city: "" },
    phoneForm: { phoneNumber: "" },
    newAccountForm: { accountName: "" },
    newBankCardForm: { accountId: "" },

    savingName: false,
    savingAddress: false,
    savingPhoneNumber: false,
    openingAccount: false,
    assigningBankCard: false,
    // Per-bank-card-id busy flag, so clicking Cancel/Report stolen on one card doesn't disable
    // every other card's buttons while its request is in flight.
    bankCardActionInFlight: {} as Record<string, boolean>,
  }),
});

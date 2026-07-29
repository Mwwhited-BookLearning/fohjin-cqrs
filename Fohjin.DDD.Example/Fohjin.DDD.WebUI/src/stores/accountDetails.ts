import { defineStore } from "pinia";
import type { AccountDetailsReport, AccountReport } from "../api/generated-client";

export const useAccountDetailsStore = defineStore("accountDetails", {
  state: () => ({
    details: null as AccountDetailsReport | null,
    otherAccounts: [] as AccountReport[],
    loading: true,
    error: null as string | null,

    nameForm: { accountName: "" },
    depositForm: { amount: 0 },
    withdrawalForm: { amount: 0 },
    transferForm: { amount: 0, accountNumber: "" },

    savingName: false,
    depositing: false,
    withdrawing: false,
    transferring: false,
    closing: false,
  }),
});

import { defineStore } from "pinia";
import type { ClientReport } from "../api/generated-client";

export const useClientSearchStore = defineStore("clientSearch", {
  state: () => ({
    clients: [] as ClientReport[],
    loading: true,
    error: null as string | null,
  }),
});

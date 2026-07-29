import { defineStore } from "pinia";

export const useClientCreateStore = defineStore("clientCreate", {
  state: () => ({
    clientName: "",
    street: "",
    streetNumber: "",
    postalCode: "",
    city: "",
    phoneNumber: "",
    submitting: false,
    error: null as string | null,
  }),
});

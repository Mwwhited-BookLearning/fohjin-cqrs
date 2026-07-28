import { createRouter, createWebHistory } from "vue-router";
import { getUser } from "../auth/authService";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: "/login", name: "login", component: () => import("../views/Login.vue"), meta: { public: true } },
    { path: "/callback", name: "callback", component: () => import("../views/LoginCallback.vue"), meta: { public: true } },
    { path: "/", name: "clients", component: () => import("../views/ClientSearch.vue") },
    { path: "/clients/new", name: "client-create", component: () => import("../views/ClientCreate.vue") },
    { path: "/clients/:id", name: "client-details", component: () => import("../views/ClientDetails.vue"), props: true },
    { path: "/accounts/:id", name: "account-details", component: () => import("../views/AccountDetails.vue"), props: true },
    { path: "/monitoring", name: "monitoring", component: () => import("../views/Monitoring.vue") },
  ],
});

router.beforeEach(async (to) => {
  if (to.meta.public) return true;

  const user = await getUser();
  if (user && !user.expired) return true;

  return { name: "login", query: { returnTo: to.fullPath } };
});

export default router;

import { User, UserManager } from "oidc-client-ts";

// Phase 5's dev STS (Fohjin.DDD.Sts) as an OIDC provider: authorization code + PKCE
// (oidc-client-ts uses PKCE automatically for response_type "code"), against the one
// seeded dev-client/dev user. Swapping to a real IdP later is changing these three env
// values, not this file - see docs/supporting/oidc-sts-openiddict-vs-duende.md.
const userManager = new UserManager({
  authority: import.meta.env.VITE_STS_AUTHORITY,
  client_id: import.meta.env.VITE_STS_CLIENT_ID,
  redirect_uri: `${window.location.origin}/callback`,
  post_logout_redirect_uri: window.location.origin,
  response_type: "code",
  scope: "openid profile email",
});

export function login(): Promise<void> {
  return userManager.signinRedirect();
}

export function logout(): Promise<void> {
  return userManager.signoutRedirect();
}

export function completeLogin(): Promise<User> {
  return userManager.signinRedirectCallback();
}

export function getUser(): Promise<User | null> {
  return userManager.getUser();
}

export async function getAccessToken(): Promise<string | null> {
  const user = await getUser();
  return user?.access_token ?? null;
}

export { userManager };

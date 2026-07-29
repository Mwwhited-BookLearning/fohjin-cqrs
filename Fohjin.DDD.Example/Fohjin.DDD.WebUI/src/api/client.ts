import { FohjinApiClient } from "./generated-client";
import { getAccessToken } from "../auth/authService";

// Attaches the signed-in user's access token to every request the generated client makes -
// the constructor's second parameter lets it use a custom fetch instead of the global one.
async function authenticatedFetch(url: RequestInfo, init?: RequestInit): Promise<Response> {
  const token = await getAccessToken();
  const headers = new Headers(init?.headers);
  if (token) headers.set("Authorization", `Bearer ${token}`);
  return fetch(url, { ...init, headers });
}

export const apiClient = new FohjinApiClient(import.meta.env.VITE_API_BASE_URL, { fetch: authenticatedFetch });

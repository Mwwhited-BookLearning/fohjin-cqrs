import { getAccessToken } from "../auth/authService";

// One shared GET /api/events (SSE) connection for the whole app, opened once at startup
// (main.ts calls start()) and kept open for the session - every screen that wants
// event-driven refresh subscribes here instead of opening its own connection or falling back
// to a blind setTimeout poll. Mirrors Fohjin.DDD.Bus.Direct.DirectBus's own shape server-side
// (docs/07-messaging-bus.md): one shared stream, N independent subscriptions, each filtering
// for what it cares about - just the client-side half of the same pattern.
//
// The generated FohjinApiClient.streamEvents() can't be used here (NSwag has no real SSE
// support - it just returns Promise<void>) and the browser's native EventSource can't attach
// an Authorization header, so this reads the stream by hand: fetch() with a bearer token, then
// a hand-parsed reader over Results.ServerSentEvents' wire format ("event:"/"data:" lines,
// blank line between records - no "id:" line since Fohjin.DDD.WebApi/Program.cs's SseItem is
// constructed without one). Filtering is done here, client-side, over the one unfiltered
// stream, rather than per-connection server-side $filter - there's only one connection shared
// by every subscriber, each of which may want a different filter.
export interface EventEnvelope {
  id: string;
  aggregateId: string;
  version: number;
  eventType: string;
  occurredAt: string;
  payload: unknown;
}

export type ConnectionStatus = "connecting" | "connected" | "disconnected";

type Listener = (event: EventEnvelope) => void;
type StatusListener = (status: ConnectionStatus) => void;

const listeners = new Set<Listener>();
const statusListeners = new Set<StatusListener>();
let status: ConnectionStatus = "disconnected";
let started = false;

export function subscribe(handler: Listener): () => void {
  listeners.add(handler);
  return () => listeners.delete(handler);
}

export function onStatusChange(handler: StatusListener): () => void {
  statusListeners.add(handler);
  handler(status);
  return () => statusListeners.delete(handler);
}

// Fires every time the shared connection reaches "connected" - including the very first time,
// and again after any reconnect. There's no queue/replay on this stream, so anything that
// happened while disconnected (the initial connect race right after login, a dropped network
// connection, a laptop waking from sleep) is otherwise gone for good; subscribing here and
// reloading once per (re)connect is the reconciliation step that closes that gap, without
// reintroducing a continuous poll - screens still rely on live events the rest of the time.
export function onReconnect(handler: () => void): () => void {
  let wasConnected = false;
  return onStatusChange((next) => {
    if (next === "connected" && !wasConnected) handler();
    wasConnected = next === "connected";
  });
}

export function start(): void {
  if (started) return;
  started = true;
  void runForever();
}

function setStatus(next: ConnectionStatus) {
  status = next;
  statusListeners.forEach((listener) => listener(status));
}

// Retries forever - matches the WinForms side's EventStreamClient, which hits the exact same
// "connection can drop independently of the rest of the app" problem (docs/09-winforms-ui.md).
// Two different backoffs depending on *why* the last attempt failed: "no token yet" (not signed
// in, or oidc-client-ts's async token exchange right after the OIDC redirect hasn't finished)
// retries almost immediately, since this isn't a real failure and every extra second here is a
// window where a domain event could fire and be silently missed forever (no queue/replay on this
// stream - an event that happens while nobody's connected just never gets delivered). A real
// connection failure (network error, non-401 HTTP error, stream closed by the server) gets the
// slower 5s backoff, since retrying instantly against a genuinely-down server is just noise.
async function runForever() {
  while (true) {
    const token = await getAccessToken();
    if (!token) {
      setStatus("connecting");
      await new Promise((resolve) => setTimeout(resolve, 500));
      continue;
    }

    try {
      await connectOnce(token);
    } catch {
      // Falls through to the backoff below regardless of why (network error, stream closed by
      // the server, token rejected) - every case is handled the same way: wait, then retry.
    }
    setStatus("disconnected");
    await new Promise((resolve) => setTimeout(resolve, 5000));
  }
}

async function connectOnce(token: string): Promise<void> {
  setStatus("connecting");
  const url = new URL("/api/events", import.meta.env.VITE_API_BASE_URL);

  const response = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
  if (!response.ok || !response.body) {
    throw new Error(`Failed to connect: HTTP ${response.status}`);
  }

  setStatus("connected");
  const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += value;
    let separatorIndex: number;
    while ((separatorIndex = buffer.indexOf("\n\n")) !== -1) {
      const record = buffer.slice(0, separatorIndex);
      buffer = buffer.slice(separatorIndex + 2);
      handleRecord(record);
    }
  }

  throw new Error("Event stream closed by server");
}

function handleRecord(record: string) {
  const dataLines = record
    .split("\n")
    .filter((line) => line.startsWith("data:"))
    .map((line) => line.slice(5).trimStart());
  if (!dataLines.length) return;

  try {
    const envelope = JSON.parse(dataLines.join("\n")) as EventEnvelope;
    // WebApi/Program.cs's Stream local function sends this the instant a subscriber attaches,
    // purely to flush response headers right away (see EventEnvelope.Connected) - not a real
    // domain event, so subscribers never see it.
    if (envelope.eventType === "StreamConnected") return;
    listeners.forEach((listener) => listener(envelope));
  } catch {
    // Ignore malformed/partial records rather than crashing the shared stream for every subscriber.
  }
}

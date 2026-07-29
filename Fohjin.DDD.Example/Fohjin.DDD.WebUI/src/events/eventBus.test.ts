import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const { getAccessTokenMock } = vi.hoisted(() => ({ getAccessTokenMock: vi.fn() }));
vi.mock("../auth/authService", () => ({ getAccessToken: getAccessTokenMock }));

// Builds a ReadableStream<Uint8Array> that yields the given text chunks one read() at a time,
// then either stays open forever (no more chunks, read() never resolves again - simulates a live
// connection) or closes (simulates the server ending the stream).
function streamOf(chunks: string[], { closeAfter = false } = {}) {
  const encoder = new TextEncoder();
  let i = 0;
  return new ReadableStream<Uint8Array>({
    pull(controller) {
      if (i < chunks.length) {
        controller.enqueue(encoder.encode(chunks[i]));
        i++;
        return;
      }
      if (closeAfter) {
        controller.close();
        return;
      }
      // Nothing left to enqueue and the connection is still "open": return a promise that never
      // settles, like a real fetch stream waiting on the network. Returning undefined instead
      // would make the stream controller call pull() again immediately forever (a tight
      // microtask loop that starves fake timers and hangs the test).
      return new Promise<void>(() => {});
    },
  });
}

function sseRecord(data: unknown): string {
  return `event: message\ndata: ${JSON.stringify(data)}\n\n`;
}

// A stream the test drives by hand - push()/close() one chunk at a time, rather than one whose
// timing is decided by an automatic pull() loop - for tests that need to assert on state between
// two chunks arriving.
function manualStream() {
  const encoder = new TextEncoder();
  let controller!: ReadableStreamDefaultController<Uint8Array>;
  const stream = new ReadableStream<Uint8Array>({
    start(c) {
      controller = c;
    },
  });
  return {
    stream,
    push: (chunk: string) => controller.enqueue(encoder.encode(chunk)),
    close: () => controller.close(),
  };
}

describe("eventBus", () => {
  beforeEach(() => {
    vi.resetModules();
    vi.useFakeTimers();
    vi.stubEnv("VITE_API_BASE_URL", "http://webapi.test");
    getAccessTokenMock.mockReset();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
    vi.useRealTimers();
  });

  it("delivers a real event and silently drops the StreamConnected marker", async () => {
    const body = streamOf([sseRecord({ eventType: "StreamConnected" }), sseRecord({ eventType: "ClientCreatedEvent", aggregateId: "c1" })]);
    getAccessTokenMock.mockResolvedValue("token-1");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, status: 200, body }));

    const { start, subscribe } = await import("./eventBus");
    const received: unknown[] = [];
    subscribe((e) => received.push(e));
    start();

    await vi.waitFor(() => expect(received).toHaveLength(1));
    expect(received[0]).toMatchObject({ eventType: "ClientCreatedEvent", aggregateId: "c1" });
  });

  it("assembles a record split across multiple stream reads", async () => {
    const full = sseRecord({ eventType: "CashDepositedEvent", aggregateId: "a1" });
    const splitPoint = Math.floor(full.length / 2);
    const body = streamOf([full.slice(0, splitPoint), full.slice(splitPoint)]);
    getAccessTokenMock.mockResolvedValue("token-1");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, status: 200, body }));

    const { start, subscribe } = await import("./eventBus");
    const received: unknown[] = [];
    subscribe((e) => received.push(e));
    start();

    await vi.waitFor(() => expect(received).toHaveLength(1));
    expect(received[0]).toMatchObject({ eventType: "CashDepositedEvent", aggregateId: "a1" });
  });

  it("ignores a malformed record without dropping subsequent ones", async () => {
    const body = streamOf(["event: message\ndata: {not json\n\n", sseRecord({ eventType: "CashWithdrawnEvent", aggregateId: "a2" })]);
    getAccessTokenMock.mockResolvedValue("token-1");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, status: 200, body }));

    const { start, subscribe } = await import("./eventBus");
    const received: unknown[] = [];
    subscribe((e) => received.push(e));
    start();

    await vi.waitFor(() => expect(received).toHaveLength(1));
    expect(received[0]).toMatchObject({ eventType: "CashWithdrawnEvent" });
  });

  it("retries almost immediately when there's no token yet, not on the slow backoff", async () => {
    getAccessTokenMock.mockResolvedValue(null);
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);

    const { start } = await import("./eventBus");
    start();

    await vi.advanceTimersByTimeAsync(0);
    expect(getAccessTokenMock).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(500);
    expect(getAccessTokenMock).toHaveBeenCalledTimes(2);
    await vi.advanceTimersByTimeAsync(500);
    expect(getAccessTokenMock).toHaveBeenCalledTimes(3);

    // Never calls fetch at all while there's no token - this is a "not signed in yet" state,
    // not a connection failure, so it shouldn't even attempt to connect.
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("backs off 5s (not 500ms) after a real connection failure", async () => {
    getAccessTokenMock.mockResolvedValue("token-1");
    const fetchMock = vi.fn().mockResolvedValue({ ok: false, status: 500, body: null });
    vi.stubGlobal("fetch", fetchMock);

    const { start } = await import("./eventBus");
    start();

    await vi.advanceTimersByTimeAsync(0);
    expect(fetchMock).toHaveBeenCalledTimes(1);

    // Well past the fast no-token retry interval, but before the 5s backoff - still just 1 attempt.
    await vi.advanceTimersByTimeAsync(2000);
    expect(fetchMock).toHaveBeenCalledTimes(1);

    await vi.advanceTimersByTimeAsync(3000);
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("treats the stream closing (done:true) the same as a connection failure", async () => {
    getAccessTokenMock.mockResolvedValue("token-1");
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, status: 200, body: streamOf([sseRecord({ eventType: "X" })], { closeAfter: true }) });
    vi.stubGlobal("fetch", fetchMock);

    const { start, onStatusChange } = await import("./eventBus");
    const statuses: string[] = [];
    onStatusChange((s) => statuses.push(s));
    start();

    await vi.waitFor(() => expect(statuses).toContain("disconnected"));
    await vi.advanceTimersByTimeAsync(5000);
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("onStatusChange fires immediately with the current status, then on every transition", async () => {
    getAccessTokenMock.mockResolvedValue(null);
    vi.stubGlobal("fetch", vi.fn());

    const { start, onStatusChange } = await import("./eventBus");
    const statuses: string[] = [];
    onStatusChange((s) => statuses.push(s));
    expect(statuses).toEqual(["disconnected"]);

    start();
    await vi.advanceTimersByTimeAsync(0);
    expect(statuses).toEqual(["disconnected", "connecting"]);
  });

  it("onReconnect fires on the initial connect and again after a reconnect, but not while staying connected", async () => {
    getAccessTokenMock.mockResolvedValue("token-1");
    let call = 0;
    const fetchMock = vi.fn().mockImplementation(async () => {
      call++;
      if (call === 1) {
        // First connection: fails immediately (closed stream) to force one reconnect cycle.
        return { ok: true, status: 200, body: streamOf([], { closeAfter: true }) };
      }
      // Second connection: stays open.
      return { ok: true, status: 200, body: streamOf([]) };
    });
    vi.stubGlobal("fetch", fetchMock);

    const { start, onReconnect } = await import("./eventBus");
    const reconnects: number[] = [];
    let n = 0;
    onReconnect(() => reconnects.push(++n));
    start();

    await vi.waitFor(() => expect(reconnects).toHaveLength(1));
    await vi.advanceTimersByTimeAsync(5000);
    await vi.waitFor(() => expect(reconnects).toHaveLength(2));
  });

  it("unsubscribe stops delivering further events to that listener", async () => {
    const { stream, push } = manualStream();
    getAccessTokenMock.mockResolvedValue("token-1");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, status: 200, body: stream }));

    const { start, subscribe } = await import("./eventBus");
    const received: unknown[] = [];
    const unsubscribe = subscribe((e) => received.push(e));
    start();
    await vi.advanceTimersByTimeAsync(0);

    push(sseRecord({ eventType: "A" }));
    await vi.waitFor(() => expect(received).toHaveLength(1));

    unsubscribe();
    push(sseRecord({ eventType: "B" }));
    await vi.advanceTimersByTimeAsync(0);
    expect(received).toHaveLength(1);
  });
});

import { metrics } from "@opentelemetry/api";
import { WebTracerProvider, BatchSpanProcessor } from "@opentelemetry/sdk-trace-web";
import { MeterProvider, PeriodicExportingMetricReader } from "@opentelemetry/sdk-metrics";
import { ZoneContextManager } from "@opentelemetry/context-zone";
import { resourceFromAttributes } from "@opentelemetry/resources";
import { registerInstrumentations } from "@opentelemetry/instrumentation";
import { DocumentLoadInstrumentation } from "@opentelemetry/instrumentation-document-load";
import { FetchInstrumentation } from "@opentelemetry/instrumentation-fetch";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-proto";
import { OTLPMetricExporter } from "@opentelemetry/exporter-metrics-otlp-proto";

// Sends browser traces + metrics to the Aspire dashboard's OTLP/HTTP endpoint, mirroring the
// OTel instrumentation ServiceDefaults already wires up for Sts/WebApi
// (docs/00-architecture-overview.md). Only wired up when running under the AppHost
// (Fohjin.DDD.AppHost/AppHost.cs sets these VITE_* vars from the dashboard's own
// ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL / AppHost:OtlpApiKey) - a plain `npm run dev` outside
// Aspire has neither, so this silently no-ops rather than failing.
export function startTelemetry(): void {
  const endpointUrl = import.meta.env.VITE_OTLP_TRACE_ENDPOINT_URL;
  if (!endpointUrl) return;

  const headers = parseHeaders(import.meta.env.VITE_OTLP_HEADERS);
  const resource = resourceFromAttributes({ "service.name": "webui" });

  // The Sts/WebApi origins are what's worth instrumenting from the browser's side (they're the
  // ones already emitting server-side spans/metrics via ServiceDefaults); everything else
  // (Vite's own dev-server asset requests, etc.) would just be noise.
  const trackedOrigins = [import.meta.env.VITE_API_BASE_URL, import.meta.env.VITE_STS_AUTHORITY];

  startTracing(endpointUrl, headers, resource, trackedOrigins);
  startMetrics(endpointUrl, headers, resource, trackedOrigins);
}

function startTracing(
  endpointUrl: string,
  headers: Record<string, string>,
  resource: ReturnType<typeof resourceFromAttributes>,
  trackedOrigins: string[],
): void {
  const provider = new WebTracerProvider({
    resource,
    spanProcessors: [
      new BatchSpanProcessor(
        new OTLPTraceExporter({
          url: `${endpointUrl}/v1/traces`,
          headers,
        }),
      ),
    ],
  });

  provider.register({
    contextManager: new ZoneContextManager(),
  });

  registerInstrumentations({
    instrumentations: [
      new DocumentLoadInstrumentation(),
      new FetchInstrumentation({
        propagateTraceHeaderCorsUrls: trackedOrigins.map((origin) => new RegExp(`^${escapeRegExp(origin)}`)),
      }),
    ],
  });
}

function startMetrics(
  endpointUrl: string,
  headers: Record<string, string>,
  resource: ReturnType<typeof resourceFromAttributes>,
  trackedOrigins: string[],
): void {
  const provider = new MeterProvider({
    resource,
    readers: [
      new PeriodicExportingMetricReader({
        exporter: new OTLPMetricExporter({
          url: `${endpointUrl}/v1/metrics`,
          headers,
        }),
        exportIntervalMillis: 10_000,
      }),
    ],
  });
  metrics.setGlobalMeterProvider(provider);

  const meter = metrics.getMeter("webui");

  // Same navigation-timing data DocumentLoadInstrumentation already turns into a trace span
  // (real Navigation Timing API data, not invented), also recorded as a metric so it shows up
  // on the dashboard's Metrics page alongside webapi/sts, not just its Traces page.
  const documentLoadDuration = meter.createHistogram("webui.document_load.duration", {
    description: "Time from navigation start to the load event",
    unit: "s",
  });
  window.addEventListener("load", () => {
    const [nav] = performance.getEntriesByType("navigation");
    if (nav) documentLoadDuration.record(nav.duration / 1000);
  });

  // Mirrors the "http.client.request.duration" histogram ServiceDefaults' HttpClientInstrumentation
  // already records server-side, but for outgoing fetch() calls made from the browser itself -
  // Resource Timing entries are the browser's own record of exactly this, filtered to the
  // Sts/WebApi origins the same way the trace propagation allow-list above is.
  const fetchDuration = meter.createHistogram("http.client.request.duration", {
    description: "Duration of outgoing fetch() calls from webui to Sts/WebApi",
    unit: "s",
  });
  const resourceObserver = new PerformanceObserver((list) => {
    for (const entry of list.getEntries() as PerformanceResourceTiming[]) {
      if (entry.initiatorType !== "fetch") continue;
      const url = new URL(entry.name);
      if (!trackedOrigins.some((origin) => url.origin === new URL(origin).origin)) continue;
      fetchDuration.record(entry.duration / 1000, {
        "server.address": url.hostname,
        "server.port": url.port,
        "url.scheme": url.protocol.replace(":", ""),
      });
    }
  });
  resourceObserver.observe({ type: "resource", buffered: true });
}

function parseHeaders(raw: string | undefined): Record<string, string> {
  const headers: Record<string, string> = {};
  if (!raw) return headers;
  for (const pair of raw.split(",")) {
    const [key, value] = pair.split("=");
    if (key && value) headers[key.trim()] = value.trim();
  }
  return headers;
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

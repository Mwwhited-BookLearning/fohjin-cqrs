import { WebTracerProvider, BatchSpanProcessor } from "@opentelemetry/sdk-trace-web";
import { ZoneContextManager } from "@opentelemetry/context-zone";
import { resourceFromAttributes } from "@opentelemetry/resources";
import { registerInstrumentations } from "@opentelemetry/instrumentation";
import { DocumentLoadInstrumentation } from "@opentelemetry/instrumentation-document-load";
import { FetchInstrumentation } from "@opentelemetry/instrumentation-fetch";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-proto";

// Sends browser traces to the Aspire dashboard's OTLP/HTTP endpoint, mirroring the OTel
// instrumentation ServiceDefaults already wires up for Sts/WebApi (docs/00-architecture-overview.md).
// Only wired up when running under the AppHost (Fohjin.DDD.AppHost/AppHost.cs sets these two
// VITE_* vars from the dashboard's own ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL / AppHost:OtlpApiKey) -
// a plain `npm run dev` outside Aspire has neither, so this silently no-ops rather than failing.
export function startTelemetry(): void {
  const endpointUrl = import.meta.env.VITE_OTLP_TRACE_ENDPOINT_URL;
  if (!endpointUrl) return;

  const headers = parseHeaders(import.meta.env.VITE_OTLP_HEADERS);

  const provider = new WebTracerProvider({
    resource: resourceFromAttributes({
      "service.name": "webui",
    }),
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
        // The Sts/WebApi origins are what's worth tracing from the browser's side (they're the
        // ones already emitting server-side spans via ServiceDefaults); everything else (Vite's
        // own dev-server asset requests, etc.) would just be noise.
        propagateTraceHeaderCorsUrls: [
          new RegExp(`^${escapeRegExp(import.meta.env.VITE_API_BASE_URL)}`),
          new RegExp(`^${escapeRegExp(import.meta.env.VITE_STS_AUTHORITY)}`),
        ],
      }),
    ],
  });
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

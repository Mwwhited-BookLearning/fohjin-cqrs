import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import router from './router'
import { start as startEventBus } from './events/eventBus'
import { startTelemetry } from './telemetry'

// Reports browser traces to the Aspire dashboard when running under the AppHost (src/telemetry.ts
// no-ops otherwise). Registered before anything else so document-load and the earliest fetch calls
// get captured.
startTelemetry()

// One shared SSE connection for the whole session (src/events/eventBus.ts) - starts trying
// immediately; if there's no signed-in user yet, connectOnce()'s 401 is just another retryable
// failure, so this doesn't need to wait for login to complete first.
startEventBus()

createApp(App).use(router).mount('#app')

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    // host.docker.internal: how a Playwright container sees this dev server when driving a
    // real headless browser through the login flow for manual/E2E verification (this repo has
    // no native Node.js install, so the whole toolchain - including E2E checks - runs in
    // Docker containers; see docs/11-migration-plan.md Phase 6).
    allowedHosts: ['host.docker.internal'],
  },
})

import { defineConfig, devices } from '@playwright/test'

/**
 * End-to-end tests of the UI against the REAL system (docker compose): browser → nginx → gateway →
 * services → RabbitMQ → services. Run with `npm run e2e` while `docker compose up` is running.
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 120_000,
  expect: { timeout: 30_000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:3000',
    viewport: { width: 1440, height: 900 },
    colorScheme: 'light',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } } }],
})

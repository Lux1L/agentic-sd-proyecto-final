import { defineConfig } from '@playwright/test'

// Requiere API (http://localhost:5080) y Vite (http://localhost:5173) ya levantados.
// Usa el Google Chrome instalado en el sistema (channel 'chrome'), no Chromium descargado.
export default defineConfig({
  testDir: 'e2e',
  globalSetup: './e2e/global-setup.ts',
  workers: 1,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5173',
    channel: 'chrome',
    storageState: '.auth/state.json',
    locale: 'es-PE',
    timezoneId: 'America/Lima',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
})

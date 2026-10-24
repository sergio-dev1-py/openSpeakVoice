import { defineConfig } from "vite";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));

// Multi-page build: one HTML entry per Tauri window.
export default defineConfig({
  clearScreen: false,
  server: {
    port: 1420,
    strictPort: true,
  },
  build: {
    target: "es2022",
    outDir: "dist",
    emptyOutDir: true,
    rollupOptions: {
      input: {
        pill: resolve(__dirname, "pill.html"),
        menu: resolve(__dirname, "menu.html"),
        settings: resolve(__dirname, "settings.html"),
        keys: resolve(__dirname, "keys.html"),
        models: resolve(__dirname, "models.html"),
      },
    },
  },
});

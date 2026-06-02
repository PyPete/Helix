import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174,
    strictPort: true,
    proxy: {
      // WS entry MUST precede generic /api (first-match wins in Vite proxy)
      "/api/trends/ws": { target: "ws://localhost:56523", ws: true, changeOrigin: true },
      "/api": { target: "http://localhost:56523", changeOrigin: true },
    },
  },
  envPrefix: ["VITE_"],
  build: { target: "es2020", minify: "esbuild", sourcemap: false },
});

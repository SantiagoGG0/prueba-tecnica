import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

// mismo proxy que nginx, para desarrollo local
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', '');
  const apiTarget = env.ORDERS_API_URL || 'http://localhost:5000';

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        '/api': { target: apiTarget, rewrite: (path) => path.replace(/^\/api/, '') },
        '/hubs': { target: apiTarget, ws: true },
      },
    },
  };
});

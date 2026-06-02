import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: 3000,
    proxy: {
      // Все запросы, начинающиеся с /api, будут перенаправлены
      '/api': {
        target: 'http://localhost:5000', // Адрес твоего бэкенда в Rider
        changeOrigin: true,
        secure: false,      
        configure: (proxy, _options) => {
          proxy.on('error', (err, _req, _res) => {
            console.log('proxy error', err);
          });
        },
      },
      '/gamehub': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        ws: true,
        configure: (proxy, _options) => {
          proxy.on('error', (err, _req, _res) => {
            console.log('proxy error', err);
          });
        },
      },
      '/uploads': {
        target: 'http://localhost:5000', // Адрес твоего бэкенда в Rider
        changeOrigin: true,
        secure: false,      
        configure: (proxy, _options) => {
          proxy.on('error', (err, _req, _res) => {
            console.log('proxy error', err);
          });
        },
      }
    }
  }
})

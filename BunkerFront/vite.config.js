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
        // Если твой бекенд НЕ ожидает приставку /api (например, в контроллере напиёсано [Route("users")]), 
        // а фронт шлет /api/users, можно её отрезать:
        // rewrite: (path) => path.replace(/^\/api/, '')
      }
    }
  }
})

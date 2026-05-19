import {defineConfig} from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    }
  },
  server: {
    proxy: {
      // Tell Vite to send any request starting with /api to the C# backend at port 7125
      '/api': {
        target: 'https://localhost:7168',
        secure: false,
        changeOrigin: true
      }
    }
  }
})

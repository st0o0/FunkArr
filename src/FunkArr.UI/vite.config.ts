import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  server: {
    proxy: {
      '/api': 'http://localhost:6969',
      '/index/api': 'http://localhost:6969',
      '/download/api': 'http://localhost:6969',
    },
  },
})

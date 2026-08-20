<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()

const tabs = [
  { path: '/', label: 'Queue' },
  { path: '/history', label: 'History' },
  { path: '/rulesets', label: 'Rulesets' },
  { path: '/matches', label: 'Matches' },
  { path: '/settings', label: 'Settings' },
]

function isActive(tabPath: string): boolean {
  if (tabPath === '/') return route.path === '/'
  return route.path.startsWith(tabPath)
}
</script>

<template>
  <div class="min-h-screen bg-white text-neutral-900 dark:bg-neutral-900 dark:text-neutral-100">
    <header
      v-if="route.path !== '/setup'"
      class="border-b border-neutral-200 dark:border-neutral-700"
    >
      <div class="mx-auto flex max-w-5xl items-center gap-6 px-4 py-3">
        <span class="font-mono text-sm font-bold tracking-tight">FunkArr</span>
        <nav class="flex gap-1">
          <router-link
            v-for="tab in tabs"
            :key="tab.path"
            :to="tab.path"
            class="rounded px-3 py-1.5 text-sm transition-colors"
            :class="
              isActive(tab.path)
                ? 'bg-neutral-100 font-medium dark:bg-neutral-800'
                : 'text-neutral-500 hover:text-neutral-900 dark:text-neutral-400 dark:hover:text-neutral-100'
            "
          >
            {{ tab.label }}
          </router-link>
        </nav>
      </div>
    </header>
    <main class="mx-auto max-w-5xl px-4 py-6">
      <router-view />
    </main>
  </div>
</template>

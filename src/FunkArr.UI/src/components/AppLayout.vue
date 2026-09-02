<template>
  <div class="min-h-screen grid grid-cols-[64px_1fr] md:grid-cols-[64px_1fr]" :class="collapsed ? 'grid-cols-[48px_1fr]!' : ''">
    <aside class="bg-surface-raised border-r border-border-default flex flex-col h-screen sticky top-0">
      <div class="px-3 py-4 flex items-center justify-center md:justify-start">
        <router-link to="/" class="text-brand-500 font-bold tracking-tight text-lg">
          <span class="hidden md:inline">FunkArr</span>
          <span class="md:hidden text-sm">FA</span>
        </router-link>
      </div>

      <div class="border-t border-border-default" />

      <nav class="flex-1 flex flex-col gap-1 px-2 py-3">
        <router-link
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          custom
          v-slot="{ navigate, isActive, isExactActive }"
        >
          <button
            @click="navigate"
            class="w-full flex items-center gap-3 px-2 py-2 rounded-md text-sm transition-colors"
            :class="isRouteActive(item, isActive, isExactActive)
              ? 'border-l-2 border-brand-500 bg-brand-900/20 text-text-primary'
              : 'border-l-2 border-transparent text-text-secondary hover:bg-surface-elevated hover:text-text-body'"
          >
            <svg class="w-4 h-4 shrink-0" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" v-html="item.icon" />
            <span class="hidden md:inline truncate">{{ item.label }}</span>
            <span class="sr-only md:hidden">{{ item.label }}</span>
          </button>
        </router-link>
      </nav>

      <div class="border-t border-border-default" />
      <div class="px-3 py-3 text-text-muted text-xs text-center">
        <span class="hidden md:inline">v0.1.0</span>
      </div>
    </aside>

    <main class="min-h-screen bg-surface-base">
      <div class="max-w-7xl mx-auto w-full p-6">
        <slot />
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()

const navItems = [
  {
    to: '/',
    label: 'Dashboard',
    exact: true,
    icon: '<rect x="2" y="2" width="5" height="5" rx="1"/><rect x="9" y="2" width="5" height="5" rx="1"/><rect x="2" y="9" width="5" height="5" rx="1"/><rect x="9" y="9" width="5" height="5" rx="1"/>',
  },
  {
    to: '/rulesets',
    label: 'RuleSets',
    exact: false,
    icon: '<path d="M2 4h12M2 8h12M2 12h8"/><circle cx="13" cy="12" r="1.5"/>',
  },
  {
    to: '/setup',
    label: 'Setup',
    exact: false,
    icon: '<circle cx="8" cy="8" r="3"/><path d="M8 1v2M8 13v2M1 8h2M13 8h2M3.05 3.05l1.41 1.41M11.54 11.54l1.41 1.41M3.05 12.95l1.41-1.41M11.54 4.46l1.41-1.41"/>',
  },
]

const collapsed = false

function isRouteActive(item: typeof navItems[number], isActive: boolean, isExactActive: boolean): boolean {
  if (item.exact) return isExactActive
  return isActive || route.path.startsWith(item.to)
}
</script>

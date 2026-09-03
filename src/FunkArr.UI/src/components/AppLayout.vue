<template>
  <div class="min-h-screen grid grid-cols-[200px_1fr]">
    <aside class="bg-surface-raised flex flex-col h-screen sticky top-0 border-r border-border-default">
      <div class="px-4 py-5">
        <router-link to="/" class="flex items-center gap-2.5">
          <div class="w-8 h-8 rounded-lg bg-brand-600 flex items-center justify-center">
            <svg class="w-4 h-4 text-white" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M3 2v12M8 5v9M13 3v11" />
            </svg>
          </div>
          <span class="text-text-primary font-semibold tracking-tight">FunkArr</span>
        </router-link>
      </div>

      <nav class="flex-1 flex flex-col gap-0.5 px-2 py-1">
        <div class="px-3 pt-3 pb-1.5 text-[10px] font-semibold uppercase tracking-widest text-text-muted">
          Media
        </div>
        <router-link
          v-for="item in primaryNav"
          :key="item.to"
          :to="item.to"
          custom
          v-slot="{ navigate, isActive, isExactActive }"
        >
          <button
            @click="navigate"
            class="w-full flex items-center gap-2.5 px-3 py-2 rounded-md text-[13px] font-medium transition-colors"
            :class="isRouteActive(item, isActive, isExactActive)
              ? 'bg-brand-600/15 text-brand-400'
              : 'text-text-secondary hover:bg-surface-elevated hover:text-text-body'"
          >
            <svg class="w-[18px] h-[18px] shrink-0" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" v-html="item.icon" />
            <span class="truncate">{{ item.label }}</span>
          </button>
        </router-link>

        <div class="px-3 pt-5 pb-1.5 text-[10px] font-semibold uppercase tracking-widest text-text-muted">
          System
        </div>
        <router-link
          v-for="item in systemNav"
          :key="item.to"
          :to="item.to"
          custom
          v-slot="{ navigate, isActive, isExactActive }"
        >
          <button
            @click="navigate"
            class="w-full flex items-center gap-2.5 px-3 py-2 rounded-md text-[13px] font-medium transition-colors"
            :class="isRouteActive(item, isActive, isExactActive)
              ? 'bg-brand-600/15 text-brand-400'
              : 'text-text-secondary hover:bg-surface-elevated hover:text-text-body'"
          >
            <svg class="w-[18px] h-[18px] shrink-0" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" v-html="item.icon" />
            <span class="truncate">{{ item.label }}</span>
          </button>
        </router-link>
      </nav>

      <div class="border-t border-border-default px-4 py-3">
        <div class="text-[11px] text-text-muted">v0.1.0</div>
      </div>
    </aside>

    <main class="min-h-screen">
      <div class="max-w-6xl mx-auto w-full px-8 py-6">
        <slot />
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()

interface NavItem {
  to: string
  label: string
  exact: boolean
  icon: string
}

const primaryNav: NavItem[] = [
  {
    to: '/',
    label: 'Dashboard',
    exact: true,
    icon: '<rect x="2" y="2" width="5" height="5" rx="1"/><rect x="9" y="2" width="5" height="5" rx="1"/><rect x="2" y="9" width="5" height="5" rx="1"/><rect x="9" y="9" width="5" height="5" rx="1"/>',
  },
  {
    to: '/queue',
    label: 'Downloads',
    exact: false,
    icon: '<path d="M8 2v8M5 7l3 3 3-3"/><path d="M2 12h12"/>',
  },
  {
    to: '/history',
    label: 'History',
    exact: false,
    icon: '<circle cx="8" cy="8" r="6"/><path d="M8 4v4l2.5 2.5"/>',
  },
]

const systemNav: NavItem[] = [
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

function isRouteActive(item: NavItem, isActive: boolean, isExactActive: boolean): boolean {
  if (item.exact) return isExactActive
  return isActive || route.path.startsWith(item.to)
}
</script>

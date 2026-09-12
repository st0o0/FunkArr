<template>
  <div
    class="min-h-screen grid transition-[grid-template-columns] duration-200 ease-in-out"
    :class="collapsed ? 'grid-cols-[52px_1fr]' : 'grid-cols-[192px_1fr]'"
  >
    <aside class="bg-surface-raised flex flex-col h-screen sticky top-0 border-r border-border-default overflow-hidden">
      <div class="py-4" :class="collapsed ? 'px-0 flex justify-center' : 'px-4'">
        <router-link to="/" class="flex items-center" :class="collapsed ? 'justify-center' : 'gap-2'">
          <div class="w-7 h-7 rounded-md bg-accent-dim flex items-center justify-center shrink-0">
            <svg class="w-3.5 h-3.5 text-white" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M3 2v12M8 5v9M13 3v11" />
            </svg>
          </div>
          <span v-if="!collapsed" class="text-text-primary font-medium text-sm tracking-tight">FunkArr</span>
        </router-link>
      </div>

      <nav class="flex-1 flex flex-col gap-px px-2 py-1">
        <router-link
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          custom
          v-slot="{ navigate, isActive, isExactActive }"
        >
          <button
            @click="navigate"
            class="w-full flex items-center rounded-md text-sm transition-colors"
            :class="[
              collapsed ? 'justify-center px-0 py-2' : 'gap-2.5 px-2.5 py-2',
              isRouteActive(item, isActive, isExactActive)
                ? 'bg-surface-elevated text-text-primary border-l-2 border-accent'
                : 'text-text-body hover:bg-surface-elevated/50 hover:text-text-primary border-l-2 border-transparent'
            ]"
            :title="collapsed ? item.label : undefined"
          >
            <svg class="w-4 h-4 shrink-0" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" v-html="item.icon" />
            <span v-if="!collapsed" class="truncate">{{ item.label }}</span>
          </button>
        </router-link>
      </nav>

      <div class="border-t border-border-default px-2 py-2 space-y-px">
        <router-link to="/setup" custom v-slot="{ navigate, isActive }">
          <button
            @click="navigate"
            class="w-full flex items-center rounded-md text-[13px] transition-colors"
            :class="[
              collapsed ? 'justify-center px-0 py-2' : 'gap-2.5 px-2.5 py-1.5',
              isActive
                ? 'bg-surface-elevated text-text-primary'
                : 'text-text-muted hover:bg-surface-elevated/50 hover:text-text-secondary'
            ]"
            :title="collapsed ? 'Setup' : undefined"
          >
            <svg class="w-4 h-4 shrink-0" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="8" cy="8" r="3"/><path d="M8 1v2M8 13v2M1 8h2M13 8h2M3.05 3.05l1.41 1.41M11.54 11.54l1.41 1.41M3.05 12.95l1.41-1.41M11.54 4.46l1.41-1.41"/>
            </svg>
            <span v-if="!collapsed" class="truncate">Setup</span>
          </button>
        </router-link>
        <button
          @click="toggle"
          class="w-full flex items-center rounded-md text-text-muted hover:text-text-secondary hover:bg-surface-elevated/50 transition-colors"
          :class="collapsed ? 'justify-center py-2' : 'gap-2.5 px-2.5 py-1.5'"
          :title="collapsed ? 'Expand' : 'Collapse'"
        >
          <svg
            class="w-4 h-4 transition-transform duration-200"
            :class="collapsed ? 'rotate-180' : ''"
            viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"
          >
            <path d="M10 4l-4 4 4 4" />
          </svg>
          <span v-if="!collapsed" class="text-xs">Collapse</span>
        </button>
      </div>
    </aside>

    <main class="min-h-screen">
      <div class="w-full px-6 py-5">
        <slot />
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

const collapsed = ref(false)

onMounted(() => {
  const stored = localStorage.getItem('funkarr-sidebar')
  if (stored === 'collapsed') collapsed.value = true
})

function toggle() {
  collapsed.value = !collapsed.value
  localStorage.setItem('funkarr-sidebar', collapsed.value ? 'collapsed' : 'expanded')
}

interface NavItem {
  to: string
  label: string
  exact: boolean
  icon: string
}

const navItems: NavItem[] = [
  {
    to: '/',
    label: 'Overview',
    exact: true,
    icon: '<rect x="2" y="2" width="5" height="5" rx="1"/><rect x="9" y="2" width="5" height="5" rx="1"/><rect x="2" y="9" width="5" height="5" rx="1"/><rect x="9" y="9" width="5" height="5" rx="1"/>',
  },
  {
    to: '/activity',
    label: 'Activity',
    exact: false,
    icon: '<path d="M8 2v8M5 7l3 3 3-3"/><path d="M2 12h12"/>',
  },
  {
    to: '/rulesets',
    label: 'RuleSets',
    exact: false,
    icon: '<path d="M2 4h12M2 8h12M2 12h8"/><circle cx="13" cy="12" r="1.5"/>',
  },
]

function isRouteActive(item: NavItem, isActive: boolean, isExactActive: boolean): boolean {
  if (item.exact) return isExactActive
  return isActive || route.path.startsWith(item.to)
}
</script>

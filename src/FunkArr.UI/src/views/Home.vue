<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-text-primary tracking-tight">Dashboard</h1>
      <p class="text-sm text-text-secondary mt-1">German public broadcaster media library integration for the *arr ecosystem.</p>
    </div>

    <div class="grid gap-5 mb-6 lg:grid-cols-[1fr_1fr]">
      <HealthWidget />
      <ActiveDownloads />
    </div>

    <router-link
      to="/rulesets"
      class="inline-flex items-center gap-2 px-4 py-2 bg-surface-raised border border-border-default rounded-lg hover:border-brand-500/40 hover:bg-surface-elevated text-sm text-text-body transition-colors"
    >
      <svg class="w-4 h-4 text-text-muted" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
        <path d="M2 4h12M2 8h12M2 12h8"/><circle cx="13" cy="12" r="1.5"/>
      </svg>
      View RuleSets
    </router-link>
  </div>
</template>

<script setup lang="ts">
import HealthWidget from '../components/HealthWidget.vue'
import ActiveDownloads from '../components/ActiveDownloads.vue'
</script>

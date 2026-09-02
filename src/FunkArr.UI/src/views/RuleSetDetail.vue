<template>
  <div>
    <div class="mb-4 text-sm text-text-muted">
      <router-link to="/rulesets" class="hover:text-text-secondary">RuleSets</router-link>
      <span class="mx-1">&gt;</span>
      <span class="text-text-secondary">{{ id }}</span>
    </div>

    <div v-if="loading" class="text-text-muted">Loading...</div>
    <div v-else-if="error" class="text-status-fail">{{ error }}</div>
    <div v-else-if="detail">
      <h1 class="text-xl font-bold text-text-primary mb-6">{{ detail.identity.topic }}</h1>

      <!-- Identity -->
      <section class="mb-6">
        <h2 class="text-base font-semibold mb-2 text-text-secondary">Identity</h2>
        <div class="bg-surface-raised rounded-lg border border-border-default p-4 text-sm">
          <div class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
            <span class="text-text-muted">RuleSet ID</span>
            <span class="font-mono text-brand-400">{{ detail.ruleSetId }}</span>
            <span class="text-text-muted">Topic</span>
            <span class="text-text-body">{{ detail.identity.topic }}</span>
            <span class="text-text-muted">Aliases</span>
            <span class="text-text-body">{{ detail.identity.aliases.length > 0 ? detail.identity.aliases.join(', ') : '—' }}</span>
            <span class="text-text-muted">TVDB</span>
            <span class="text-text-body">{{ detail.identity.tvdbId ?? '—' }}</span>
            <span class="text-text-muted">IMDB</span>
            <span class="text-text-body">{{ detail.identity.imdbId ?? '—' }}</span>
            <span class="text-text-muted">TMDB</span>
            <span class="text-text-body">{{ detail.identity.tmdbId ?? '—' }}</span>
          </div>
        </div>
      </section>

      <!-- Source -->
      <section class="mb-6">
        <h2 class="text-base font-semibold mb-2 text-text-secondary">Source</h2>
        <div class="bg-surface-raised rounded-lg border border-border-default p-4 text-sm">
          <div class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
            <span class="text-text-muted">Community</span>
            <span v-if="detail.source.communityPath" class="font-mono text-xs text-text-body">
              {{ detail.source.communityPath }}
              <span v-if="detail.source.communityModified" class="text-text-muted ml-2">({{ formatDate(detail.source.communityModified) }})</span>
            </span>
            <span v-else class="text-text-muted">—</span>
            <span class="text-text-muted">Local</span>
            <span v-if="detail.source.localPath" class="font-mono text-xs text-text-body">
              {{ detail.source.localPath }}
              <span v-if="detail.source.localModified" class="text-text-muted ml-2">({{ formatDate(detail.source.localModified) }})</span>
            </span>
            <span v-else class="text-text-muted">—</span>
            <span class="text-text-muted">Mode</span>
            <span class="text-text-body">{{ mergeMode }}</span>
          </div>
        </div>
      </section>

      <!-- Matching Rules -->
      <section class="mb-6">
        <h2 class="text-base font-semibold mb-2 text-text-secondary">Matching Rules</h2>
        <div class="text-sm text-text-muted mb-3">Default confidence: {{ detail.defaultConfidence }}</div>

        <div v-if="detail.rules.length === 0" class="text-text-muted text-sm">No rules defined.</div>

        <div v-else class="grid gap-3">
          <div
            v-for="rule in detail.rules"
            :key="rule.id"
            class="bg-surface-raised rounded-lg border border-border-default p-4 text-sm"
          >
            <div class="flex items-baseline gap-3 mb-2">
              <span class="font-mono font-semibold text-brand-400">{{ rule.id }}</span>
              <span class="text-text-muted">prio {{ rule.priority }}</span>
              <span v-if="rule.confidence != null" class="text-text-muted">conf {{ rule.confidence }}</span>
            </div>
            <div class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-xs">
              <span class="text-text-muted">Strategy</span>
              <span class="text-text-body">{{ rule.strategy }}</span>
              <template v-if="rule.seasonPattern">
                <span class="text-text-muted">Season</span>
                <span class="font-mono text-text-body">{{ rule.seasonPattern }}</span>
              </template>
              <template v-if="rule.episodePattern">
                <span class="text-text-muted">Episode</span>
                <span class="font-mono text-text-body">{{ rule.episodePattern }}</span>
              </template>
              <template v-if="rule.matchMode">
                <span class="text-text-muted">Match Mode</span>
                <span class="text-text-body">{{ rule.matchMode }}</span>
              </template>
              <template v-if="rule.titleParts && rule.titleParts.length > 0">
                <span class="text-text-muted">Title Parts</span>
                <span class="text-text-body">{{ rule.titleParts.join(' + ') }}</span>
              </template>
              <template v-if="rule.filterSummary">
                <span class="text-text-muted">Filters</span>
                <span class="text-text-body">{{ rule.filterSummary }}</span>
              </template>
            </div>
          </div>
        </div>
      </section>

      <!-- Scoring History link -->
      <router-link
        :to="`/rulesets/${id}/history`"
        class="inline-block px-4 py-2 bg-brand-600 text-white rounded-md hover:bg-brand-500 text-sm"
      >
        Scoring History
      </router-link>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getRuleSetDetail, type RuleSetDetail } from '../api/rulesets'

const route = useRoute()
const id = route.params.id as string

const detail = ref<RuleSetDetail | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)

const mergeMode = computed(() => {
  if (!detail.value) return ''
  const hasCommunity = detail.value.source.communityPath != null
  const hasLocal = detail.value.source.localPath != null
  if (hasCommunity && hasLocal) return 'merged'
  if (hasCommunity) return 'community only'
  return 'local only'
})

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString()
}

onMounted(async () => {
  try {
    detail.value = await getRuleSetDetail(id)
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load ruleset'
  } finally {
    loading.value = false
  }
})
</script>

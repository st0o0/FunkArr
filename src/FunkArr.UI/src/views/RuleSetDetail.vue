<template>
  <div>
    <div class="mb-4 text-sm text-gray-500">
      <router-link to="/rulesets" class="hover:text-gray-700">RuleSets</router-link>
      <span class="mx-1">&gt;</span>
      <span>{{ id }}</span>
    </div>

    <div v-if="loading" class="text-gray-500">Loading...</div>
    <div v-else-if="error" class="text-red-600">{{ error }}</div>
    <div v-else-if="detail">
      <h1 class="text-2xl font-bold mb-6">{{ detail.identity.topic }}</h1>

      <!-- Identity -->
      <section class="mb-6">
        <h2 class="text-lg font-semibold mb-2 text-gray-700">Identity</h2>
        <div class="bg-white rounded border border-gray-200 p-4 text-sm">
          <div class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
            <span class="text-gray-500">RuleSet ID</span>
            <span class="font-mono">{{ detail.ruleSetId }}</span>
            <span class="text-gray-500">Topic</span>
            <span>{{ detail.identity.topic }}</span>
            <span class="text-gray-500">Aliases</span>
            <span>{{ detail.identity.aliases.length > 0 ? detail.identity.aliases.join(', ') : '—' }}</span>
            <span class="text-gray-500">TVDB</span>
            <span>{{ detail.identity.tvdbId ?? '—' }}</span>
            <span class="text-gray-500">IMDB</span>
            <span>{{ detail.identity.imdbId ?? '—' }}</span>
            <span class="text-gray-500">TMDB</span>
            <span>{{ detail.identity.tmdbId ?? '—' }}</span>
          </div>
        </div>
      </section>

      <!-- Source -->
      <section class="mb-6">
        <h2 class="text-lg font-semibold mb-2 text-gray-700">Source</h2>
        <div class="bg-white rounded border border-gray-200 p-4 text-sm">
          <div class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
            <span class="text-gray-500">Community</span>
            <span v-if="detail.source.communityPath" class="font-mono text-xs">
              {{ detail.source.communityPath }}
              <span v-if="detail.source.communityModified" class="text-gray-400 ml-2">({{ formatDate(detail.source.communityModified) }})</span>
            </span>
            <span v-else class="text-gray-400">—</span>
            <span class="text-gray-500">Local</span>
            <span v-if="detail.source.localPath" class="font-mono text-xs">
              {{ detail.source.localPath }}
              <span v-if="detail.source.localModified" class="text-gray-400 ml-2">({{ formatDate(detail.source.localModified) }})</span>
            </span>
            <span v-else class="text-gray-400">—</span>
            <span class="text-gray-500">Mode</span>
            <span>{{ mergeMode }}</span>
          </div>
        </div>
      </section>

      <!-- Matching Rules -->
      <section class="mb-6">
        <h2 class="text-lg font-semibold mb-2 text-gray-700">Matching Rules</h2>
        <div class="text-sm text-gray-600 mb-3">Default confidence: {{ detail.defaultConfidence }}</div>

        <div v-if="detail.rules.length === 0" class="text-gray-500 text-sm">No rules defined.</div>

        <div v-else class="grid gap-3">
          <div
            v-for="rule in detail.rules"
            :key="rule.id"
            class="bg-white rounded border border-gray-200 p-4 text-sm"
          >
            <div class="flex items-baseline gap-3 mb-2">
              <span class="font-mono font-semibold">{{ rule.id }}</span>
              <span class="text-gray-500">prio {{ rule.priority }}</span>
              <span v-if="rule.confidence != null" class="text-gray-500">conf {{ rule.confidence }}</span>
            </div>
            <div class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-xs">
              <span class="text-gray-500">Strategy</span>
              <span>{{ rule.strategy }}</span>
              <template v-if="rule.seasonPattern">
                <span class="text-gray-500">Season</span>
                <span class="font-mono">{{ rule.seasonPattern }}</span>
              </template>
              <template v-if="rule.episodePattern">
                <span class="text-gray-500">Episode</span>
                <span class="font-mono">{{ rule.episodePattern }}</span>
              </template>
              <template v-if="rule.matchMode">
                <span class="text-gray-500">Match Mode</span>
                <span>{{ rule.matchMode }}</span>
              </template>
              <template v-if="rule.titleParts && rule.titleParts.length > 0">
                <span class="text-gray-500">Title Parts</span>
                <span>{{ rule.titleParts.join(' + ') }}</span>
              </template>
              <template v-if="rule.filterSummary">
                <span class="text-gray-500">Filters</span>
                <span>{{ rule.filterSummary }}</span>
              </template>
            </div>
          </div>
        </div>
      </section>

      <!-- Scoring History link -->
      <router-link
        :to="`/rulesets/${id}/history`"
        class="inline-block px-4 py-2 bg-gray-900 text-white rounded hover:bg-gray-700 text-sm"
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

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '@/api/client'

interface RulesetSummary {
  topic: string
  source: string
  ruleCount: number
  media: { name: string; tvdbId?: number; type: string }
  aliases: string[]
  matchRate?: number
}

const router = useRouter()
const rulesets = ref<RulesetSummary[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const search = ref('')

const filtered = computed(() => {
  const q = search.value.toLowerCase()
  if (!q) return rulesets.value
  return rulesets.value.filter((r) => r.topic.toLowerCase().includes(q))
})

async function fetchRulesets() {
  loading.value = true
  error.value = null
  try {
    rulesets.value = await api<RulesetSummary[]>('/api/rulesets')
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  } finally {
    loading.value = false
  }
}

function sourceBadgeClass(source: string): string {
  switch (source) {
    case 'generated':
      return 'bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300'
    case 'local':
      return 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300'
    default:
      return 'bg-neutral-100 text-neutral-700 dark:bg-neutral-700 dark:text-neutral-300'
  }
}

function formatMatchRate(rate?: number): string {
  if (rate == null) return '--'
  return `${Math.round(rate * 100)}%`
}

function isLowMatchRate(rate?: number): boolean {
  return rate != null && rate < 0.75
}

onMounted(fetchRulesets)
</script>

<template>
  <div>
    <div class="flex items-center justify-between gap-4 mb-4">
      <input
        v-model="search"
        type="text"
        placeholder="Search topics..."
        class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-64"
      />
      <button
        class="bg-blue-600 text-white px-3 py-1.5 rounded text-sm"
        @click="router.push('/rulesets/new')"
      >
        New Ruleset
      </button>
    </div>

    <p v-if="loading" class="text-center text-neutral-500 py-12">Loading...</p>

    <p v-else-if="error" class="text-center text-red-600 dark:text-red-400 py-12">{{ error }}</p>

    <p v-else-if="filtered.length === 0" class="text-center text-neutral-500 py-12">
      No rulesets found
    </p>

    <table v-else class="w-full text-sm">
      <thead>
        <tr class="border-b border-neutral-200 dark:border-neutral-700 text-left text-neutral-500 dark:text-neutral-400">
          <th class="pb-2 font-medium">Topic</th>
          <th class="pb-2 font-medium">Source</th>
          <th class="pb-2 font-medium text-right">Rules</th>
          <th class="pb-2 font-medium text-right">Match Rate</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="rs in filtered"
          :key="rs.topic"
          class="border-b border-neutral-100 dark:border-neutral-800 cursor-pointer hover:bg-neutral-50 dark:hover:bg-neutral-800/50"
          @click="router.push(`/rulesets/${encodeURIComponent(rs.topic)}`)"
        >
          <td class="py-2">
            <div class="flex items-center gap-2">
              <span class="font-medium">{{ rs.topic }}</span>
              <span
                v-if="rs.source === 'local'"
                class="text-neutral-400 dark:text-neutral-500"
                title="Local override"
              >
                &#9998;
              </span>
            </div>
            <div v-if="rs.aliases.length" class="text-xs text-neutral-500 mt-0.5">
              {{ rs.aliases.join(', ') }}
            </div>
          </td>
          <td class="py-2">
            <span
              class="text-xs font-medium px-2 py-0.5 rounded-full"
              :class="sourceBadgeClass(rs.source)"
            >
              {{ rs.source }}
            </span>
          </td>
          <td class="py-2 text-right font-mono">{{ rs.ruleCount }}</td>
          <td class="py-2 text-right font-mono" :class="isLowMatchRate(rs.matchRate) ? 'text-amber-600 dark:text-amber-400' : ''">
            {{ formatMatchRate(rs.matchRate) }}
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<template>
  <div class="max-w-3xl mx-auto">
    <AppBreadcrumb :items="[{ label: 'RuleSets', to: '/rulesets' }, { label: id }]" />

    <div v-if="loading" class="space-y-4">
      <SkeletonCard />
      <SkeletonCard />
      <SkeletonCard />
    </div>
    <div v-else-if="error" class="text-status-fail">{{ error }}</div>
    <div v-else-if="detail">
      <div class="flex items-center justify-between mb-5">
        <h1 class="text-xl font-semibold text-text-primary tracking-tight">{{ detail.identity.topic }}</h1>
        <div class="flex items-center gap-2">
          <router-link
            :to="`/rulesets/${id}/history`"
            class="px-2.5 py-1 text-xs bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-secondary transition-colors"
          >
            Scoring History
          </router-link>
          <router-link
            :to="`/rulesets/${id}/edit`"
            class="px-2.5 py-1 text-xs bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-secondary transition-colors"
          >
            Edit
          </router-link>
        </div>
      </div>

      <section class="mb-4">
        <h2 class="text-sm font-semibold mb-2 text-text-secondary">Identity</h2>
        <div class="bg-surface-raised rounded-lg border border-border-default p-4 text-sm">
          <div class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2">
            <span class="text-text-secondary">RuleSet ID</span>
            <span class="font-mono text-text-body">{{ detail.ruleSetId }}</span>
            <span class="text-text-secondary">Topic</span>
            <span class="text-text-primary font-medium">{{ detail.identity.topic }}</span>
            <span class="text-text-secondary">Aliases</span>
            <span class="text-text-body">{{ detail.identity.aliases.length > 0 ? detail.identity.aliases.join(', ') : '-' }}</span>
            <span class="text-text-secondary">TVDB</span>
            <span class="font-mono text-text-body">{{ detail.identity.tvdbId ?? '-' }}</span>
            <span class="text-text-secondary">IMDB</span>
            <span class="font-mono text-text-body">{{ detail.identity.imdbId ?? '-' }}</span>
            <span class="text-text-secondary">TMDB</span>
            <span class="font-mono text-text-body">{{ detail.identity.tmdbId ?? '-' }}</span>
          </div>
        </div>
      </section>

      <section class="mb-4">
        <h2 class="text-sm font-semibold mb-2 text-text-secondary">Source</h2>
        <div class="bg-surface-raised rounded-lg border border-border-default p-4 text-sm">
          <div class="flex items-center gap-3">
            <span
              class="px-2 py-0.5 rounded text-xs"
              :class="mergeMode === 'merged'
                ? 'bg-surface-elevated text-text-body'
                : mergeMode === 'community only'
                  ? 'bg-surface-elevated text-status-ok'
                  : 'bg-surface-elevated text-text-secondary'"
            >
              {{ mergeMode === 'community only' ? 'Community' : mergeMode === 'local only' ? 'Local' : 'Community + Local' }}
            </span>
            <span v-if="detail.source.communityModified" class="text-xs text-text-muted">
              Updated {{ formatDate(detail.source.communityModified) }}
            </span>
          </div>
        </div>
      </section>

      <section class="mb-5">
        <h2 class="text-sm font-semibold mb-2 text-text-secondary">Matching Rules</h2>
        <div class="text-sm text-text-muted mb-2">Default confidence: {{ detail.defaultConfidence }}</div>

        <div v-if="detail.rules.length === 0" class="text-text-muted text-sm">No rules defined.</div>

        <div v-else class="grid gap-2">
          <div
            v-for="rule in detail.rules"
            :key="rule.id"
            class="bg-surface-raised rounded-lg border border-border-default p-4 text-sm"
          >
            <div class="flex items-center justify-between mb-2">
              <div class="flex items-baseline gap-2">
                <span class="font-mono font-medium text-text-primary">{{ rule.id }}</span>
                <span class="text-[11px] px-1.5 py-0.5 rounded bg-surface-elevated text-text-body">{{ rule.strategy }}</span>
              </div>
              <div class="flex items-center gap-2 text-xs text-text-muted">
                <span>prio {{ rule.priority }}</span>
                <span v-if="rule.confidence != null">conf {{ rule.confidence }}</span>
              </div>
            </div>
            <div class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-1.5 text-xs">
              <template v-if="rule.seasonPattern">
                <span class="text-text-secondary">Season</span>
                <span class="font-mono text-text-body">{{ rule.seasonPattern }}</span>
              </template>
              <template v-if="rule.episodePattern">
                <span class="text-text-secondary">Episode</span>
                <span class="font-mono text-text-body">{{ rule.episodePattern }}</span>
              </template>
              <template v-if="rule.matchMode">
                <span class="text-text-secondary">Match Mode</span>
                <span class="text-text-body">{{ rule.matchMode }}</span>
              </template>
              <template v-if="rule.titleParts && rule.titleParts.length > 0">
                <span class="text-text-secondary">Title Parts</span>
                <span class="text-text-body">{{ rule.titleParts.join(' + ') }}</span>
              </template>
              <template v-if="rule.filterSummary">
                <span class="text-text-secondary">Filters</span>
                <span class="text-text-body">{{ rule.filterSummary }}</span>
              </template>
            </div>
          </div>
        </div>
      </section>

      <div class="flex items-center gap-2">
        <button
          v-if="detail.source.localPath"
          class="px-3 py-1.5 text-xs text-status-fail border border-border-default rounded-md hover:bg-status-fail/10 transition-colors"
          @click="showDeleteConfirm = true"
        >
          Delete Local
        </button>
      </div>

      <div v-if="showDeleteConfirm" class="mt-3 p-4 bg-surface-raised rounded-lg border border-border-default">
        <p class="text-sm text-text-body mb-3">Delete local overlay? This cannot be undone.</p>
        <div class="flex items-center gap-2">
          <button
            class="px-3 py-1.5 text-xs text-status-fail border border-border-default rounded-md hover:bg-status-fail/10 transition-colors"
            :disabled="deleting"
            @click="handleDelete"
          >
            {{ deleting ? 'Deleting...' : 'Confirm' }}
          </button>
          <button
            class="px-3 py-1.5 text-xs text-text-secondary border border-border-default rounded-md hover:bg-surface-elevated transition-colors"
            :disabled="deleting"
            @click="showDeleteConfirm = false"
          >
            Cancel
          </button>
        </div>
        <div v-if="deleteError" class="text-status-fail text-sm mt-2">{{ deleteError }}</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getRuleSetDetail, deleteRuleSet, type RuleSetDetail } from '../api/rulesets'
import { useToast } from '../composables/useToast'

const { toast } = useToast()
import SkeletonCard from '../components/SkeletonCard.vue'
import AppBreadcrumb from '../components/AppBreadcrumb.vue'

const route = useRoute()
const router = useRouter()
const id = route.params.id as string

const detail = ref<RuleSetDetail | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const showDeleteConfirm = ref(false)
const deleting = ref(false)
const deleteError = ref<string | null>(null)

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

async function handleDelete() {
  deleting.value = true
  deleteError.value = null
  try {
    await deleteRuleSet(id)
    toast('Local overlay deleted')
    const hasCommunity = detail.value?.source.communityPath != null
    if (hasCommunity) {
      detail.value = await getRuleSetDetail(id)
      showDeleteConfirm.value = false
    } else {
      router.push('/rulesets')
    }
  } catch (e) {
    deleteError.value = e instanceof Error ? e.message : 'Failed to delete'
    toast(deleteError.value ?? 'Delete failed', 'error')
  } finally {
    deleting.value = false
  }
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

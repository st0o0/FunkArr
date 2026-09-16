<template>
  <div class="max-w-3xl mx-auto">
    <AppBreadcrumb :items="[{ label: $t('rulesets.title'), to: '/rulesets' }, { label: id }]" />

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
            class="px-2.5 py-1 text-xs bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-body transition-colors"
          >
            {{ $t('detail.scoringHistory') }}
          </router-link>
          <router-link
            :to="`/rulesets/${id}/edit`"
            class="px-2.5 py-1 text-xs bg-surface-elevated border border-border-default rounded-md hover:bg-surface-overlay text-text-body transition-colors"
          >
            {{ $t('detail.edit') }}
          </router-link>
        </div>
      </div>

      <section class="mb-4">
        <h2 class="text-sm font-semibold mb-2 text-text-body">{{ $t('detail.identity') }}</h2>
        <div class="bg-surface-raised rounded-lg border border-border-default p-4 text-sm">
          <div class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2">
            <span class="text-text-secondary">{{ $t('detail.ruleSetId') }}</span>
            <span class="font-mono text-text-body">{{ detail.ruleSetId }}</span>
            <span class="text-text-secondary">{{ $t('detail.topic') }}</span>
            <span class="text-text-primary font-medium">{{ detail.identity.topic }}</span>
            <span class="text-text-secondary">{{ $t('detail.aliases') }}</span>
            <span class="text-text-body">{{ detail.identity.aliases.length > 0 ? detail.identity.aliases.join(', ') : '-' }}</span>
            <span class="text-text-secondary">{{ $t('detail.tvdb') }}</span>
            <span class="font-mono text-text-body">{{ detail.identity.tvdbId ?? '-' }}</span>
            <span class="text-text-secondary">{{ $t('detail.imdb') }}</span>
            <span class="font-mono text-text-body">{{ detail.identity.imdbId ?? '-' }}</span>
            <span class="text-text-secondary">{{ $t('detail.tmdb') }}</span>
            <span class="font-mono text-text-body">{{ detail.identity.tmdbId ?? '-' }}</span>
          </div>
        </div>
      </section>

      <section class="mb-4">
        <h2 class="text-sm font-semibold mb-2 text-text-body">{{ $t('detail.source') }}</h2>
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
              {{ mergeMode === 'community only' ? $t('detail.communityLabel') : mergeMode === 'local only' ? $t('detail.localLabel') : $t('detail.communityLocalLabel') }}
            </span>
            <span v-if="detail.source.communityModified" class="text-xs text-text-secondary">
              {{ $t('detail.updated', { date: formatDate(detail.source.communityModified) }) }}
            </span>
          </div>
        </div>
      </section>

      <section class="mb-5">
        <h2 class="text-sm font-semibold mb-2 text-text-body">{{ $t('detail.matchingRules') }}</h2>
        <div class="text-sm text-text-secondary mb-2">{{ $t('detail.defaultConfidence') }}: {{ detail.defaultConfidence }}</div>

        <div v-if="detail.rules.length === 0" class="text-text-secondary text-sm">{{ $t('detail.noRulesDefined') }}</div>

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
              <div class="flex items-center gap-2 text-xs text-text-secondary">
                <span>prio {{ rule.priority }}</span>
                <span v-if="rule.confidence != null">conf {{ rule.confidence }}</span>
              </div>
            </div>
            <div class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-1.5 text-xs">
              <template v-if="rule.seasonPattern">
                <span class="text-text-secondary">{{ $t('detail.season') }}</span>
                <span class="font-mono text-text-body">{{ rule.seasonPattern }}</span>
              </template>
              <template v-if="rule.episodePattern">
                <span class="text-text-secondary">{{ $t('detail.episode') }}</span>
                <span class="font-mono text-text-body">{{ rule.episodePattern }}</span>
              </template>
              <template v-if="rule.matchMode">
                <span class="text-text-secondary">{{ $t('detail.matchMode') }}</span>
                <span class="text-text-body">{{ rule.matchMode }}</span>
              </template>
              <template v-if="rule.titleParts && rule.titleParts.length > 0">
                <span class="text-text-secondary">{{ $t('detail.titleParts') }}</span>
                <span class="text-text-body">{{ rule.titleParts.join(' + ') }}</span>
              </template>
              <template v-if="rule.filterSummary">
                <span class="text-text-secondary">{{ $t('detail.filters') }}</span>
                <span class="text-text-body">{{ rule.filterSummary }}</span>
              </template>
            </div>
          </div>
        </div>
      </section>

      <div class="flex items-center gap-2">
        <button
          v-if="detail.source.localPath"
          class="px-3 py-1.5 text-xs text-text-body border border-border-default rounded-md hover:bg-surface-elevated transition-colors"
          :disabled="exporting"
          @click="handleExport"
        >
          {{ exporting ? $t('detail.exporting') : $t('detail.exportForCommunity') }}
        </button>
        <button
          v-if="detail.source.localPath"
          class="px-3 py-1.5 text-xs text-status-fail border border-border-default rounded-md hover:bg-status-fail/10 transition-colors"
          @click="showDeleteConfirm = true"
        >
          {{ $t('detail.deleteLocal') }}
        </button>
      </div>

      <div v-if="showDeleteConfirm" class="mt-3 p-4 bg-surface-raised rounded-lg border border-border-default">
        <p class="text-sm text-text-body mb-3">{{ $t('detail.deleteConfirm') }}</p>
        <div class="flex items-center gap-2">
          <button
            class="px-3 py-1.5 text-xs text-status-fail border border-border-default rounded-md hover:bg-status-fail/10 transition-colors"
            :disabled="deleting"
            @click="handleDelete"
          >
            {{ deleting ? $t('detail.deleting') : $t('detail.confirm') }}
          </button>
          <button
            class="px-3 py-1.5 text-xs text-text-secondary border border-border-default rounded-md hover:bg-surface-elevated transition-colors"
            :disabled="deleting"
            @click="showDeleteConfirm = false"
          >
            {{ $t('detail.cancel') }}
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
import { useI18n } from 'vue-i18n'
import { getRuleSetDetail, deleteRuleSet, exportRuleSet, ValidationFailedError, type RuleSetDetail } from '../api/rulesets'
import { useToast } from '../composables/useToast'

const { t } = useI18n()
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
const exporting = ref(false)

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

async function handleExport() {
  exporting.value = true
  try {
    await exportRuleSet(id)
    toast(t('detail.exportedSuccessfully'))
  } catch (e) {
    if (e instanceof ValidationFailedError) {
      const msgs = e.errors.map(err => `${err.field}: ${err.message}`).join('\n')
      toast(`${t('detail.exportValidationFailed')}\n${msgs}`, 'error')
    } else {
      toast(e instanceof Error ? e.message : t('detail.exportFailed'), 'error')
    }
  } finally {
    exporting.value = false
  }
}

async function handleDelete() {
  deleting.value = true
  deleteError.value = null
  try {
    await deleteRuleSet(id)
    toast(t('detail.localOverlayDeleted'))
    const hasCommunity = detail.value?.source.communityPath != null
    if (hasCommunity) {
      detail.value = await getRuleSetDetail(id)
      showDeleteConfirm.value = false
    } else {
      router.push('/rulesets')
    }
  } catch (e) {
    deleteError.value = e instanceof Error ? e.message : t('detail.failedToDelete')
    toast(deleteError.value ?? t('detail.deleteFailed'), 'error')
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

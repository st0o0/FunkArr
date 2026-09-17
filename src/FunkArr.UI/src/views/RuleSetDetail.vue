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
            <span class="text-text-secondary">{{ $t('detail.aliases') }}</span>
            <span class="text-text-body">{{ detail.identity.aliases.length > 0 ? detail.identity.aliases.join(', ') : '-' }}</span>
            <span class="text-text-secondary">{{ $t('detail.tvdb') }}</span>
            <span class="font-mono text-text-body">{{ detail.identity.tvdbId ?? '-' }}</span>
            <span class="text-text-secondary">{{ $t('detail.imdb') }}</span>
            <span class="font-mono text-text-body">{{ detail.identity.imdbId ?? '-' }}</span>
            <span class="text-text-secondary">{{ $t('detail.tmdb') }}</span>
            <span class="font-mono text-text-body">{{ detail.identity.tmdbId ?? '-' }}</span>
            <span class="text-text-secondary">{{ $t('detail.source') }}</span>
            <div class="flex items-center gap-2">
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
        </div>
      </section>

      <section class="mb-5">
        <h2 class="text-sm font-semibold mb-2 text-text-body">{{ $t('detail.matchingRules') }}</h2>
        <div class="text-sm text-text-secondary mb-2">{{ $t('detail.defaultConfidence') }}: {{ detail.defaultConfidence }}</div>

        <EmptyState
          v-if="detail.rules.length === 0"
          icon='<path d="M2 4h12M2 8h12M2 12h8"/><circle cx="13" cy="12" r="1.5"/>'
          :title="$t('detail.noRulesDefined')"
          :description="$t('detail.noRulesDescription')"
        />

        <div v-else class="grid gap-2">
          <div
            v-for="(rule, rIdx) in detail.rules"
            :key="rule.id"
            class="bg-surface-raised rounded-lg border border-border-default overflow-hidden text-sm"
          >
            <div
              class="flex items-center justify-between px-4 py-3 cursor-pointer hover:bg-surface-elevated/30 transition-colors"
              @click="toggleRule(rule.id)"
            >
              <div class="flex items-baseline gap-2">
                <span class="font-mono font-medium text-text-primary">{{ rule.id }}</span>
                <span class="text-[11px] px-1.5 py-0.5 rounded bg-surface-elevated text-text-body">{{ strategyLabel(rule.strategy, t) }}</span>
              </div>
              <div class="flex items-center gap-2 text-xs text-text-secondary">
                <span v-if="rule.titleRules && rule.titleRules.length > 0" class="text-text-muted">{{ rule.titleRules.length }} {{ $t('detail.titlePartsCount') }}</span>
                <span v-if="rule.titleRules && rule.titleRules.length > 0 && filterCount(rule) > 0" class="text-text-muted">·</span>
                <span v-if="filterCount(rule) > 0" class="text-text-muted">{{ filterCount(rule) }} Filter</span>
                <span>prio {{ rule.priority }}</span>
                <span v-if="rule.confidence != null">conf {{ rule.confidence }}</span>
                <svg class="w-4 h-4 text-text-secondary transition-transform" :class="isExpanded(rule.id, rIdx) ? 'rotate-180' : ''" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M4 6l4 4 4-4"/></svg>
              </div>
            </div>
            <div v-if="isExpanded(rule.id, rIdx)" class="px-4 pb-4 border-t border-border-default pt-3">
              <div class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-1.5 text-xs">
                <template v-if="rule.seasonRegex">
                  <span class="text-text-secondary">{{ $t('detail.season') }}</span>
                  <span class="font-mono text-text-body">{{ rule.seasonRegex }}</span>
                </template>
                <template v-if="rule.episodeRegex">
                  <span class="text-text-secondary">{{ $t('detail.episode') }}</span>
                  <span class="font-mono text-text-body">{{ rule.episodeRegex }}</span>
                </template>
                <template v-if="rule.captureGroup != null">
                  <span class="text-text-secondary">{{ $t('builder.captureGroup') }}</span>
                  <span class="font-mono text-text-body">{{ rule.captureGroup }}</span>
                </template>
                <template v-if="rule.titleRules && rule.titleRules.length > 0">
                  <span class="text-text-secondary">{{ $t('detail.titleParts') }}</span>
                  <TitleRuleDisplay :title-rules="rule.titleRules" />
                </template>
                <template v-if="rule.filters">
                  <span class="text-text-secondary">{{ $t('detail.filters') }}</span>
                  <FilterConditionDisplay :filters="rule.filters" />
                </template>
              </div>
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
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { getRuleSetDetail, deleteRuleSet, exportRuleSet, ValidationFailedError, type RuleSetDetail, type RuleSetDetailRule } from '../api/rulesets'
import { strategyLabel } from '../utils/strategy'
import { useToast } from '../composables/useToast'
import FilterConditionDisplay from '../components/FilterConditionDisplay.vue'
import TitleRuleDisplay from '../components/TitleRuleDisplay.vue'

const { t } = useI18n()
const { toast } = useToast()
import EmptyState from '../components/EmptyState.vue'
import SkeletonCard from '../components/SkeletonCard.vue'
import AppBreadcrumb from '../components/AppBreadcrumb.vue'

const route = useRoute()
const router = useRouter()
const id = route.params.id as string

const detail = ref<RuleSetDetail | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const showDeleteConfirm = ref(false)
const expandedRules = reactive<Record<string, boolean>>({})

function toggleRule(ruleId: string) {
  expandedRules[ruleId] = !expandedRules[ruleId]
}

function isExpanded(ruleId: string, idx: number): boolean {
  return expandedRules[ruleId] ?? idx === 0
}

function filterCount(rule: RuleSetDetailRule): number {
  if (!rule.filters) return 0
  return (rule.filters.all?.length ?? 0) + (rule.filters.any?.length ?? 0) + (rule.filters.not?.length ?? 0)
}
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

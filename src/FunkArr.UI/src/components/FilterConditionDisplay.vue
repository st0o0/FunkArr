<template>
  <div class="space-y-1.5 text-xs">
    <template v-for="section in sections" :key="section.key">
      <div v-if="section.conditions && section.conditions.length > 0" class="rounded border border-border-subtle bg-surface-base px-2.5 py-1.5">
        <div class="text-text-secondary font-medium text-[11px] mb-1">{{ section.label }}</div>
        <div class="space-y-0.5">
          <div v-for="(cond, idx) in section.conditions" :key="idx" class="flex items-center gap-1.5 py-0.5">
            <span class="text-text-body">{{ fieldLabel(cond.field, t) }}</span>
            <span class="font-mono text-text-secondary">{{ opSymbol(cond.op) }}</span>
            <span class="font-mono text-text-body">{{ cond.value }}</span>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { FilterGroupOutput } from '../api/rulesets'
import { opSymbol, groupLabel, fieldLabel } from '../utils/ruleVocabulary'

const { t } = useI18n()

const props = defineProps<{
  filters: FilterGroupOutput
}>()

const sections = computed(() => [
  { key: 'all', label: groupLabel('all', t), conditions: props.filters.all },
  { key: 'any', label: groupLabel('any', t), conditions: props.filters.any },
  { key: 'not', label: groupLabel('not', t), conditions: props.filters.not },
])
</script>

<template>
  <div class="space-y-1 text-xs">
    <div v-for="(rule, idx) in titleRules" :key="idx" class="flex items-start gap-2 py-0.5">
      <span class="text-[10px] px-1.5 py-0.5 rounded font-medium shrink-0"
        :class="rule.type === 'static' ? 'bg-surface-elevated text-text-secondary' : 'bg-accent/10 text-accent'"
      >{{ titlePartLabel(rule.type, t) }}</span>
      <template v-if="rule.type === 'static'">
        <span class="text-text-body">"{{ rule.value }}"</span>
      </template>
      <template v-else>
        <span class="text-text-body">{{ fieldLabelLocal(rule.field ?? '') }}</span>
        <span class="text-text-secondary">→</span>
        <span class="font-mono text-text-body">/{{ rule.pattern }}/</span>
        <span v-if="rule.captureGroup != null" class="text-text-secondary">({{ rule.captureGroup }})</span>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { TitleRuleOutput } from '../api/rulesets'
import { titlePartLabel, fieldLabel } from '../utils/ruleVocabulary'

const { t } = useI18n()

defineProps<{
  titleRules: TitleRuleOutput[]
}>()

function fieldLabelLocal(field: string): string {
  return fieldLabel(field, t)
}
</script>

<template>
  <div class="text-xs">
    <div class="text-text-secondary font-medium mb-0.5">{{ groupLabelLocal(group.operator) }}
      <span :class="group.passed ? 'text-status-ok' : 'text-status-fail'">
        {{ group.passed ? $t('filter.passed') : $t('filter.failed') }}
      </span>
    </div>
    <div class="ml-2 space-y-0.5">
      <div
        v-for="(node, ni) in group.nodes"
        :key="ni"
        class="flex items-start gap-1.5 py-0.5 px-1.5 rounded"
        :class="ni % 2 === 0 ? 'bg-surface-base' : 'bg-surface-raised'"
      >
        <!-- Nested group -->
        <template v-if="node.group">
          <FilterGroupTraceView :group="node.group" />
        </template>

        <!-- Condition -->
        <template v-else>
          <span v-if="node.skipped" class="text-text-muted">&mdash;</span>
          <span v-else-if="node.passed" class="text-status-ok">&check;</span>
          <span v-else class="text-status-fail">&cross;</span>

          <template v-if="node.skipped">
            <span class="text-text-muted">{{ fieldLabelLocal(node.field) }}</span>
            <span class="text-text-muted font-mono">{{ opSymbolLocal(node.op) }}</span>
            <span class="text-text-muted font-mono">{{ node.expectedValue }}</span>
            <span class="text-text-muted italic ml-1">{{ $t('filter.skipped') }}</span>
          </template>
          <template v-else>
            <span class="text-text-secondary">{{ fieldLabelLocal(node.field) }}</span>
            <span class="font-mono text-text-secondary">{{ opSymbolLocal(node.op) }}</span>
            <span class="font-mono text-text-secondary">{{ node.expectedValue }}</span>
            <span class="text-text-secondary mx-0.5">&rarr;</span>
            <span class="font-mono" :class="node.passed ? 'text-status-ok' : 'text-status-fail'">{{ node.actualValue ?? 'null' }}</span>
          </template>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { FilterGroupTrace } from '../api/rulesets'
import { opSymbol, groupLabel, fieldLabel } from '../utils/ruleVocabulary'

const { t } = useI18n()

defineProps<{
  group: FilterGroupTrace
}>()

function groupLabelLocal(group: string): string {
  return groupLabel(group, t)
}

function fieldLabelLocal(field: number | null): string {
  return fieldLabel(field ?? 0, t)
}

function opSymbolLocal(op: number | null): string {
  return opSymbol(op ?? 0)
}
</script>

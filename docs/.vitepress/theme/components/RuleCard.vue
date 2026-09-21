<script setup lang="ts">
import { ref } from 'vue'
import type { Rule, ValidationMessage } from './RulesetBuilder.vue'
import StrategyFields from './StrategyFields.vue'
import FilterBuilder from './FilterBuilder.vue'

const props = defineProps<{
  rule: Rule
  index: number
  validation: ValidationMessage[]
}>()

defineEmits<{
  remove: []
}>()

const collapsed = ref(false)

function ruleFieldError(field: string): string | undefined {
  return props.validation.find(v =>
    v.level === 'error' && v.ruleIndex === props.index && v.field === field
  )?.message
}

const hasErrors = () => props.validation.some(v =>
  v.level === 'error' && v.ruleIndex === props.index
)
</script>

<template>
  <div class="rule-card" :class="{ 'has-errors': hasErrors() }">
    <div class="rule-header" @click="collapsed = !collapsed">
      <span class="rule-toggle">{{ collapsed ? '▶' : '▼' }}</span>
      <span class="rule-title">
        Rule: <strong>{{ rule.id || '(unnamed)' }}</strong>
        <span v-if="rule.strategy" class="rule-strategy-badge">{{ rule.strategy }}</span>
      </span>
      <button class="btn-icon" @click.stop="$emit('remove')" title="Remove rule">&times;</button>
    </div>

    <div v-if="!collapsed" class="rule-body">
      <div class="form-row">
        <div class="form-group">
          <label>Rule ID <span class="required">*</span></label>
          <input v-model="rule.id" type="text" placeholder="e.g. airdate" class="mono-input"
            :class="{ invalid: ruleFieldError('id') }" />
          <span v-if="ruleFieldError('id')" class="field-error">{{ ruleFieldError('id') }}</span>
        </div>
        <div class="form-group" style="max-width: 100px">
          <label>Priority</label>
          <input v-model.number="rule.priority" type="number" min="0" />
        </div>
        <div class="form-group" style="max-width: 100px">
          <label>Confidence</label>
          <input v-model.number="rule.confidence" type="number" min="0" max="1" step="0.1" placeholder="—" />
        </div>
      </div>

      <StrategyFields
        :rule="rule"
        :rule-index="index"
        :validation="validation"
      />

      <FilterBuilder :filters="rule.filters" />
    </div>
  </div>
</template>

<script setup lang="ts">
import type { TitleRule, ValidationMessage } from './RulesetBuilder.vue'

const props = defineProps<{
  titleRules: TitleRule[]
  ruleIndex: number
  validation: ValidationMessage[]
}>()

const filterFields = [
  { value: 'title', label: 'title' },
  { value: 'topic', label: 'topic' },
  { value: 'channel', label: 'channel' },
  { value: 'description', label: 'description' },
]

function addStatic() {
  props.titleRules.push({ type: 'static', value: '' })
}

function addRegex() {
  props.titleRules.push({ type: 'regex', field: 'title', pattern: '' })
}

function remove(index: number) {
  props.titleRules.splice(index, 1)
}

function hasRegexError(): boolean {
  return props.validation.some(v =>
    v.level === 'error' && v.ruleIndex === props.ruleIndex && v.field === 'titleRules'
  )
}
</script>

<template>
  <div class="title-rules">
    <h4>Title Rules</h4>
    <div v-if="titleRules.length === 0" class="empty-state-sm">
      No title parts. Add static text or regex extraction parts.
    </div>
    <div v-for="(part, i) in titleRules" :key="i" class="title-part">
      <span class="part-badge" :class="part.type">{{ part.type }}</span>
      <template v-if="part.type === 'static'">
        <input v-model="part.value" type="text" placeholder="Literal text (e.g. ' - ')" />
      </template>
      <template v-else>
        <select v-model="part.field">
          <option v-for="f in filterFields" :key="f.value" :value="f.value">{{ f.label }}</option>
        </select>
        <input v-model="part.pattern" type="text" placeholder="Regex pattern" class="mono-input" />
        <input v-model.number="part.captureGroup" type="number" min="0" placeholder="Group" style="max-width: 70px" title="Capture group (0 = full match)" />
      </template>
      <button class="btn-icon" @click="remove(i)">&times;</button>
    </div>
    <span v-if="hasRegexError()" class="field-error">
      {{ validation.find(v => v.level === 'error' && v.ruleIndex === ruleIndex && v.field === 'titleRules')?.message }}
    </span>
    <div class="title-actions">
      <button class="btn btn-secondary btn-xs" @click="addStatic">+ Static</button>
      <button class="btn btn-secondary btn-xs" @click="addRegex">+ Regex</button>
    </div>
  </div>
</template>

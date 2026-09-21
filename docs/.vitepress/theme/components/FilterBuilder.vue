<script setup lang="ts">
import { ref } from 'vue'
import type { FilterCondition } from './RulesetBuilder.vue'

const props = defineProps<{
  filters: {
    all: FilterCondition[]
    any: FilterCondition[]
    not: FilterCondition[]
  }
}>()

const fields = [
  { value: 'title', label: 'title' },
  { value: 'topic', label: 'topic' },
  { value: 'channel', label: 'channel' },
  { value: 'description', label: 'description' },
  { value: 'duration', label: 'duration' },
  { value: 'timestamp', label: 'timestamp' },
]

const operators = [
  { value: 'eq', label: '=' },
  { value: 'contains', label: 'contains' },
  { value: 'notContains', label: 'notContains' },
  { value: 'greaterThan', label: '>' },
  { value: 'lessThan', label: '<' },
  { value: 'regex', label: 'regex' },
]

const anyOpen = ref(false)
const notOpen = ref(false)

function addCondition(group: FilterCondition[]) {
  group.push({ field: '', op: '', value: '' })
}

function removeCondition(group: FilterCondition[], index: number) {
  group.splice(index, 1)
}
</script>

<template>
  <div class="filter-builder">
    <h4>Filters</h4>

    <div class="filter-section">
      <div class="filter-section-header">
        <span class="filter-label">ALL (every condition must match)</span>
        <button class="btn btn-secondary btn-xs" @click="addCondition(filters.all)">+ Add</button>
      </div>
      <div v-for="(cond, i) in filters.all" :key="'all-' + i" class="condition-row">
        <select v-model="cond.field">
          <option value="" disabled>Field...</option>
          <option v-for="f in fields" :key="f.value" :value="f.value">{{ f.label }}</option>
        </select>
        <select v-model="cond.op">
          <option value="" disabled>Op...</option>
          <option v-for="o in operators" :key="o.value" :value="o.value">{{ o.label }}</option>
        </select>
        <input v-model="cond.value" type="text" placeholder="Value" />
        <button class="btn-icon" @click="removeCondition(filters.all, i)">&times;</button>
      </div>
    </div>

    <div class="filter-section">
      <div class="filter-section-header" @click="anyOpen = !anyOpen" style="cursor: pointer">
        <span class="filter-toggle">{{ anyOpen ? '▼' : '▶' }}</span>
        <span class="filter-label">ANY (at least one must match)</span>
        <button v-if="anyOpen" class="btn btn-secondary btn-xs" @click.stop="addCondition(filters.any)">+ Add</button>
      </div>
      <template v-if="anyOpen">
        <div v-for="(cond, i) in filters.any" :key="'any-' + i" class="condition-row">
          <select v-model="cond.field">
            <option value="" disabled>Field...</option>
            <option v-for="f in fields" :key="f.value" :value="f.value">{{ f.label }}</option>
          </select>
          <select v-model="cond.op">
            <option value="" disabled>Op...</option>
            <option v-for="o in operators" :key="o.value" :value="o.value">{{ o.label }}</option>
          </select>
          <input v-model="cond.value" type="text" placeholder="Value" />
          <button class="btn-icon" @click="removeCondition(filters.any, i)">&times;</button>
        </div>
      </template>
    </div>

    <div class="filter-section">
      <div class="filter-section-header" @click="notOpen = !notOpen" style="cursor: pointer">
        <span class="filter-toggle">{{ notOpen ? '▼' : '▶' }}</span>
        <span class="filter-label">NOT (none may match)</span>
        <button v-if="notOpen" class="btn btn-secondary btn-xs" @click.stop="addCondition(filters.not)">+ Add</button>
      </div>
      <template v-if="notOpen">
        <div v-for="(cond, i) in filters.not" :key="'not-' + i" class="condition-row">
          <select v-model="cond.field">
            <option value="" disabled>Field...</option>
            <option v-for="f in fields" :key="f.value" :value="f.value">{{ f.label }}</option>
          </select>
          <select v-model="cond.op">
            <option value="" disabled>Op...</option>
            <option v-for="o in operators" :key="o.value" :value="o.value">{{ o.label }}</option>
          </select>
          <input v-model="cond.value" type="text" placeholder="Value" />
          <button class="btn-icon" @click="removeCondition(filters.not, i)">&times;</button>
        </div>
      </template>
    </div>
  </div>
</template>

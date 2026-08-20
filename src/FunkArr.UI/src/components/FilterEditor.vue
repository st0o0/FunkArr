<script setup lang="ts">
interface Filter {
  field: string
  op: string
  value: string
}

interface FilterGroup {
  all: FilterNode[]
  any: FilterNode[]
  not: FilterNode[]
}

type FilterNode = Filter | FilterGroup

const props = defineProps<{
  modelValue: FilterGroup
}>()

const emit = defineEmits<{
  'update:modelValue': [value: FilterGroup]
}>()

const fields = ['duration', 'title', 'description', 'topic', 'channel']
const ops = ['GreaterThan', 'LessThan', 'ExactMatch', 'Contains', 'Regex', 'Eq', 'NotContains']

function update(group: FilterGroup) {
  emit('update:modelValue', { ...group })
}

function addFilter(section: 'all' | 'any' | 'not') {
  const newGroup = {
    all: [...props.modelValue.all],
    any: [...props.modelValue.any],
    not: [...props.modelValue.not],
  }
  newGroup[section] = [
    ...newGroup[section],
    { field: 'title', op: 'Contains', value: '' } as Filter,
  ]
  update(newGroup)
}

function removeFilter(section: 'all' | 'any' | 'not', index: number) {
  const newGroup = {
    all: [...props.modelValue.all],
    any: [...props.modelValue.any],
    not: [...props.modelValue.not],
  }
  newGroup[section] = newGroup[section].filter((_, i) => i !== index)
  update(newGroup)
}

function updateFilter(section: 'all' | 'any' | 'not', index: number, key: keyof Filter, value: string) {
  const newGroup = {
    all: [...props.modelValue.all],
    any: [...props.modelValue.any],
    not: [...props.modelValue.not],
  }
  const node = newGroup[section][index]
  if ('field' in node) {
    newGroup[section] = newGroup[section].map((n, i) =>
      i === index ? { ...n as Filter, [key]: value } : n,
    )
  }
  update(newGroup)
}

function isFilter(node: FilterNode): node is Filter {
  return 'field' in node && 'op' in node && 'value' in node
}
</script>

<template>
  <div class="space-y-3">
    <div v-for="section in (['all', 'any', 'not'] as const)" :key="section">
      <div class="flex items-center gap-2 mb-1">
        <span class="text-xs font-medium uppercase text-neutral-500">{{ section }}</span>
        <button
          type="button"
          class="text-xs text-blue-600 dark:text-blue-400 hover:underline"
          @click="addFilter(section)"
        >
          + Add Filter
        </button>
      </div>
      <div
        v-for="(node, i) in props.modelValue[section]"
        :key="i"
        class="flex items-center gap-2 mb-1"
      >
        <template v-if="isFilter(node)">
          <select
            :value="(node as Filter).field"
            class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm"
            @change="updateFilter(section, i, 'field', ($event.target as HTMLSelectElement).value)"
          >
            <option v-for="f in fields" :key="f" :value="f">{{ f }}</option>
          </select>
          <select
            :value="(node as Filter).op"
            class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm"
            @change="updateFilter(section, i, 'op', ($event.target as HTMLSelectElement).value)"
          >
            <option v-for="o in ops" :key="o" :value="o">{{ o }}</option>
          </select>
          <input
            :value="(node as Filter).value"
            type="text"
            class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm flex-1"
            placeholder="Value"
            @input="updateFilter(section, i, 'value', ($event.target as HTMLInputElement).value)"
          />
          <button
            type="button"
            class="text-red-600 dark:text-red-400 text-sm hover:underline"
            @click="removeFilter(section, i)"
          >
            Remove
          </button>
        </template>
      </div>
    </div>
  </div>
</template>

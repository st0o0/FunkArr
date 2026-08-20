<script setup lang="ts">
interface TitleRule {
  type: string
  field?: string
  pattern?: string
  captureGroup?: number
  value?: string
}

const props = defineProps<{
  modelValue: TitleRule[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: TitleRule[]]
}>()

const titleFields = ['title', 'topic', 'description']

function addRule() {
  emit('update:modelValue', [...props.modelValue, { type: 'Regex', field: 'title', pattern: '' }])
}

function removeRule(index: number) {
  emit('update:modelValue', props.modelValue.filter((_, i) => i !== index))
}

function updateRule(index: number, updates: Partial<TitleRule>) {
  emit(
    'update:modelValue',
    props.modelValue.map((r, i) => (i === index ? { ...r, ...updates } : r)),
  )
}

function toggleType(index: number) {
  const rule = props.modelValue[index]
  if (rule.type === 'Regex') {
    updateRule(index, { type: 'Static', value: '', field: undefined, pattern: undefined, captureGroup: undefined })
  } else {
    updateRule(index, { type: 'Regex', field: 'title', pattern: '', value: undefined })
  }
}
</script>

<template>
  <div class="space-y-2">
    <div class="flex items-center gap-2 mb-1">
      <span class="text-xs font-medium text-neutral-500">Title Rules</span>
      <button
        type="button"
        class="text-xs text-blue-600 dark:text-blue-400 hover:underline"
        @click="addRule"
      >
        + Add
      </button>
    </div>

    <div
      v-for="(rule, i) in props.modelValue"
      :key="i"
      class="flex items-center gap-2"
    >
      <button
        type="button"
        class="text-xs font-medium px-2 py-0.5 rounded border border-neutral-300 dark:border-neutral-600"
        @click="toggleType(i)"
      >
        {{ rule.type }}
      </button>

      <template v-if="rule.type === 'Regex'">
        <select
          :value="rule.field ?? 'title'"
          class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm"
          @change="updateRule(i, { field: ($event.target as HTMLSelectElement).value })"
        >
          <option v-for="f in titleFields" :key="f" :value="f">{{ f }}</option>
        </select>
        <input
          :value="rule.pattern ?? ''"
          type="text"
          class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm font-mono flex-1"
          placeholder="Pattern"
          @input="updateRule(i, { pattern: ($event.target as HTMLInputElement).value })"
        />
        <input
          :value="rule.captureGroup ?? ''"
          type="number"
          class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm w-16"
          placeholder="Group"
          @input="updateRule(i, { captureGroup: ($event.target as HTMLInputElement).value ? Number(($event.target as HTMLInputElement).value) : undefined })"
        />
      </template>

      <template v-if="rule.type === 'Static'">
        <input
          :value="rule.value ?? ''"
          type="text"
          class="border border-neutral-300 dark:border-neutral-600 dark:bg-neutral-800 rounded px-2 py-1.5 text-sm flex-1"
          placeholder="Static value"
          @input="updateRule(i, { value: ($event.target as HTMLInputElement).value })"
        />
      </template>

      <button
        type="button"
        class="text-red-600 dark:text-red-400 text-sm hover:underline"
        @click="removeRule(i)"
      >
        Remove
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import type { ValidationMessage } from './RulesetBuilder.vue'

const props = defineProps<{
  json: string
  topicSlug: string
  errors: ValidationMessage[]
  warnings: ValidationMessage[]
}>()

const copied = ref(false)

function highlight(json: string): string {
  return json
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/("(?:\\.|[^"\\])*")\s*:/g, '<span class="json-key">$1</span>:')
    .replace(/:\s*("(?:\\.|[^"\\])*")/g, ': <span class="json-string">$1</span>')
    .replace(/:\s*(\d+\.?\d*)/g, ': <span class="json-number">$1</span>')
    .replace(/:\s*(true|false|null)/g, ': <span class="json-bool">$1</span>')
}

const highlighted = computed(() => highlight(props.json))

async function copyToClipboard() {
  await navigator.clipboard.writeText(props.json)
  copied.value = true
  setTimeout(() => copied.value = false, 2000)
}

function download() {
  const blob = new Blob([props.json], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${props.topicSlug}.json`
  a.click()
  URL.revokeObjectURL(url)
}
</script>

<template>
  <div class="json-preview">
    <div class="preview-header">
      <h3>JSON Output</h3>
      <div class="preview-buttons">
        <button class="btn btn-secondary btn-sm" @click="copyToClipboard">
          {{ copied ? 'Copied!' : 'Copy' }}
        </button>
        <button class="btn btn-primary btn-sm" @click="download">Download</button>
      </div>
    </div>
    <pre class="json-code"><code v-html="highlighted"></code></pre>

    <div v-if="errors.length > 0 || warnings.length > 0" class="validation-summary">
      <div v-for="(err, i) in errors" :key="'e-' + i" class="validation-msg validation-error">
        {{ err.message }}
      </div>
      <div v-for="(warn, i) in warnings" :key="'w-' + i" class="validation-msg validation-warning">
        {{ warn.message }}
      </div>
    </div>
  </div>
</template>

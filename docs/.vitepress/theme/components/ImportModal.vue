<script setup lang="ts">
import { ref } from 'vue'

const emit = defineEmits<{
  import: [json: string]
  close: []
}>()

const input = ref('')
const error = ref('')

function tryImport() {
  error.value = ''
  try {
    JSON.parse(input.value)
    emit('import', input.value)
  } catch (e) {
    error.value = `Invalid JSON: ${(e as Error).message}`
  }
}
</script>

<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <div class="modal-header">
        <h3>Import JSON</h3>
        <button class="btn-icon" @click="$emit('close')">&times;</button>
      </div>
      <p>Paste an existing ruleset JSON to edit it in the builder.</p>
      <textarea
        v-model="input"
        rows="12"
        placeholder='{"topic": "...", "media": {...}, "rules": [...]}'
        class="mono-input"
      ></textarea>
      <div v-if="error" class="field-error">{{ error }}</div>
      <div class="modal-actions">
        <button class="btn btn-secondary" @click="$emit('close')">Cancel</button>
        <button class="btn btn-primary" @click="tryImport" :disabled="!input.trim()">Import</button>
      </div>
    </div>
  </div>
</template>

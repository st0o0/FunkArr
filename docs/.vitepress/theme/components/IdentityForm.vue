<script setup lang="ts">
import type { RulesetState, ValidationMessage } from './RulesetBuilder.vue'

const props = defineProps<{
  state: RulesetState
  validation: ValidationMessage[]
}>()

const emit = defineEmits<{
  'media-name-input': []
}>()

function fieldError(field: string): string | undefined {
  return props.validation.find(v => v.level === 'error' && v.field === field)?.message
}

function addAlias() {
  props.state.aliases.push('')
}

function removeAlias(index: number) {
  props.state.aliases.splice(index, 1)
}

function parseIntOrNull(val: string): number | null {
  const n = parseInt(val, 10)
  return isNaN(n) ? null : n
}
</script>

<template>
  <div class="builder-section">
    <h3>Identity</h3>

    <div class="form-group">
      <label>Topic <span class="required">*</span></label>
      <input v-model="state.topic" type="text" placeholder="e.g. Tatort" :class="{ invalid: fieldError('topic') }" />
      <span v-if="fieldError('topic')" class="field-error">{{ fieldError('topic') }}</span>
    </div>

    <div class="form-group">
      <label>Aliases</label>
      <div v-for="(_, i) in state.aliases" :key="i" class="alias-row">
        <input v-model="state.aliases[i]" type="text" placeholder="Alternative topic name" />
        <button class="btn-icon" @click="removeAlias(i)" title="Remove alias">&times;</button>
      </div>
      <button class="btn btn-secondary btn-sm" @click="addAlias">+ Add Alias</button>
    </div>

    <h3>Media</h3>

    <div class="form-group">
      <label>Name <span class="required">*</span></label>
      <input
        :value="state.media.name"
        @input="(e) => { state.media.name = (e.target as HTMLInputElement).value; emit('media-name-input') }"
        type="text"
        placeholder="Display name for Sonarr/Radarr"
        :class="{ invalid: fieldError('media.name') }"
      />
      <span v-if="fieldError('media.name')" class="field-error">{{ fieldError('media.name') }}</span>
    </div>

    <div class="form-row">
      <div class="form-group">
        <label>Type</label>
        <select v-model="state.media.type">
          <option value="show">Show</option>
          <option value="movie">Movie</option>
        </select>
      </div>
      <div class="form-group">
        <label>Confidence</label>
        <input v-model.number="state.confidence" type="number" min="0" max="1" step="0.1" />
      </div>
    </div>

    <div class="form-row">
      <div class="form-group">
        <label>TVDB ID</label>
        <input
          :value="state.media.tvdbId ?? ''"
          @input="(e) => state.media.tvdbId = parseIntOrNull((e.target as HTMLInputElement).value)"
          type="number"
          placeholder="e.g. 12345"
        />
      </div>
      <div class="form-group">
        <label>IMDB ID</label>
        <input v-model="state.media.imdbId" type="text" placeholder="e.g. tt0806910" />
      </div>
      <div class="form-group">
        <label>TMDB ID</label>
        <input
          :value="state.media.tmdbId ?? ''"
          @input="(e) => state.media.tmdbId = parseIntOrNull((e.target as HTMLInputElement).value)"
          type="number"
          placeholder="e.g. 67890"
        />
      </div>
    </div>
  </div>
</template>

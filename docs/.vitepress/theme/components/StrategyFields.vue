<script setup lang="ts">
import type { Rule, ValidationMessage } from './RulesetBuilder.vue'
import TitleRuleList from './TitleRuleList.vue'

const props = defineProps<{
  rule: Rule
  ruleIndex: number
  validation: ValidationMessage[]
}>()

const strategies = [
  { value: 'seasonAndEpisodeNumber', label: 'Season & Episode Number' },
  { value: 'byAbsoluteEpisodeNumber', label: 'Absolute Episode Number' },
  { value: 'itemTitleExact', label: 'Title Exact' },
  { value: 'itemTitleIncludes', label: 'Title Includes' },
  { value: 'itemTitleEqualsAirdate', label: 'Title Equals Airdate' },
]

const isRegexStrategy = (s: string) =>
  s === 'seasonAndEpisodeNumber' || s === 'byAbsoluteEpisodeNumber'

const isTitleStrategy = (s: string) =>
  s === 'itemTitleExact' || s === 'itemTitleIncludes' || s === 'itemTitleEqualsAirdate'

const showSeasonRegex = (s: string) => s === 'seasonAndEpisodeNumber'

function ruleFieldError(field: string): string | undefined {
  return props.validation.find(v =>
    v.level === 'error' && v.ruleIndex === props.ruleIndex && v.field === field
  )?.message
}
</script>

<template>
  <div class="strategy-fields">
    <div class="form-group">
      <label>Strategy <span class="required">*</span></label>
      <select v-model="rule.strategy" :class="{ invalid: ruleFieldError('strategy') }">
        <option value="" disabled>Select strategy...</option>
        <option v-for="s in strategies" :key="s.value" :value="s.value">{{ s.label }}</option>
      </select>
      <span v-if="ruleFieldError('strategy')" class="field-error">{{ ruleFieldError('strategy') }}</span>
    </div>

    <template v-if="isRegexStrategy(rule.strategy)">
      <div v-if="showSeasonRegex(rule.strategy)" class="form-group">
        <label>Season Regex</label>
        <input v-model="rule.seasonRegex" type="text" placeholder="e.g. S(\d{2})" class="mono-input"
          :class="{ invalid: ruleFieldError('seasonRegex') }" />
        <span v-if="ruleFieldError('seasonRegex')" class="field-error">{{ ruleFieldError('seasonRegex') }}</span>
      </div>
      <div class="form-group">
        <label>Episode Regex</label>
        <input v-model="rule.episodeRegex" type="text" placeholder="e.g. E(\d{2})" class="mono-input"
          :class="{ invalid: ruleFieldError('episodeRegex') }" />
        <span v-if="ruleFieldError('episodeRegex')" class="field-error">{{ ruleFieldError('episodeRegex') }}</span>
      </div>
      <div class="form-group">
        <label>Capture Group</label>
        <input v-model.number="rule.captureGroup" type="number" min="0" placeholder="1 (default)" />
      </div>
    </template>

    <template v-if="isTitleStrategy(rule.strategy)">
      <TitleRuleList
        :title-rules="rule.titleRules"
        :rule-index="ruleIndex"
        :validation="validation"
      />
    </template>
  </div>
</template>

<script setup lang="ts">
import { reactive, computed, ref, watch } from 'vue'
import IdentityForm from './IdentityForm.vue'
import RuleCard from './RuleCard.vue'
import JsonPreview from './JsonPreview.vue'
import ImportModal from './ImportModal.vue'

export interface TitleRule {
  type: 'static' | 'regex'
  value?: string
  field?: string
  pattern?: string
  captureGroup?: number
}

export interface FilterCondition {
  field: string
  op: string
  value: string
}

export interface Rule {
  id: string
  priority: number
  strategy: string
  confidence?: number
  seasonRegex?: string
  episodeRegex?: string
  captureGroup?: number
  filters: {
    all: FilterCondition[]
    any: FilterCondition[]
    not: FilterCondition[]
  }
  titleRules: TitleRule[]
}

export interface RulesetState {
  topic: string
  aliases: string[]
  media: {
    name: string
    type: 'show' | 'movie'
    tvdbId: number | null
    imdbId: string
    tmdbId: number | null
  }
  confidence: number
  rules: Rule[]
}

const state = reactive<RulesetState>({
  topic: '',
  aliases: [],
  media: {
    name: '',
    type: 'show',
    tvdbId: null,
    imdbId: '',
    tmdbId: null,
  },
  confidence: 1.0,
  rules: [],
})

const mediaNameManuallySet = ref(false)

watch(() => state.topic, (newTopic) => {
  if (!mediaNameManuallySet.value) {
    state.media.name = newTopic
  }
})

function onMediaNameInput() {
  mediaNameManuallySet.value = true
}

const showImport = ref(false)

function createRule(): Rule {
  const idx = state.rules.length + 1
  return {
    id: `rule-${idx}`,
    priority: 0,
    strategy: '',
    filters: { all: [], any: [], not: [] },
    titleRules: [],
  }
}

function addRule() {
  state.rules.push(createRule())
}

function removeRule(index: number) {
  state.rules.splice(index, 1)
}

export interface ValidationMessage {
  level: 'error' | 'warning' | 'info'
  field?: string
  ruleIndex?: number
  message: string
}

const validationMessages = computed<ValidationMessage[]>(() => {
  const msgs: ValidationMessage[] = []

  if (!state.topic.trim()) {
    msgs.push({ level: 'error', field: 'topic', message: 'Topic is required' })
  }
  if (!state.media.name.trim()) {
    msgs.push({ level: 'error', field: 'media.name', message: 'Media name is required' })
  }
  if (state.rules.length === 0) {
    msgs.push({ level: 'warning', message: 'No rules defined' })
  }

  if (!state.media.tvdbId && !state.media.imdbId && !state.media.tmdbId) {
    msgs.push({ level: 'warning', message: 'No external ID set (tvdbId, imdbId, or tmdbId recommended)' })
  }

  const ruleIds = new Set<string>()
  state.rules.forEach((rule, i) => {
    if (!rule.id.trim()) {
      msgs.push({ level: 'error', ruleIndex: i, field: 'id', message: 'Rule ID is required' })
    } else if (!/^[a-z0-9][a-z0-9-]{1,}[a-z0-9]$/.test(rule.id)) {
      msgs.push({ level: 'error', ruleIndex: i, field: 'id', message: 'Rule ID must be kebab-case, at least 3 characters' })
    } else if (ruleIds.has(rule.id)) {
      msgs.push({ level: 'error', ruleIndex: i, field: 'id', message: `Duplicate rule ID "${rule.id}"` })
    }
    ruleIds.add(rule.id)

    if (!rule.strategy) {
      msgs.push({ level: 'error', ruleIndex: i, field: 'strategy', message: 'Strategy is required' })
    }

    const regexFields = [
      { name: 'seasonRegex', val: rule.seasonRegex },
      { name: 'episodeRegex', val: rule.episodeRegex },
    ]
    for (const rf of regexFields) {
      if (rf.val) {
        try { new RegExp(rf.val) }
        catch { msgs.push({ level: 'error', ruleIndex: i, field: rf.name, message: `Invalid regex: ${rf.val}` }) }
      }
    }

    for (const tr of rule.titleRules) {
      if (tr.type === 'regex' && tr.pattern) {
        try { new RegExp(tr.pattern) }
        catch { msgs.push({ level: 'error', ruleIndex: i, field: 'titleRules', message: `Invalid regex pattern: ${tr.pattern}` }) }
      }
    }
  })

  return msgs
})

const errors = computed(() => validationMessages.value.filter(m => m.level === 'error'))
const warnings = computed(() => validationMessages.value.filter(m => m.level === 'warning'))

function cleanOutput(obj: any): any {
  if (Array.isArray(obj)) {
    const cleaned = obj.map(cleanOutput).filter(v => v !== undefined)
    return cleaned.length > 0 ? cleaned : undefined
  }
  if (obj !== null && typeof obj === 'object') {
    const result: any = {}
    for (const [key, value] of Object.entries(obj)) {
      const cleaned = cleanOutput(value)
      if (cleaned !== undefined) result[key] = cleaned
    }
    return Object.keys(result).length > 0 ? result : undefined
  }
  if (obj === '' || obj === null || obj === undefined) return undefined
  if (typeof obj === 'number' && obj === 0 && obj !== 0) return undefined
  return obj
}

function buildRuleOutput(rule: Rule): any {
  const out: any = {
    id: rule.id,
    priority: rule.priority,
    strategy: rule.strategy || undefined,
  }
  if (rule.confidence !== undefined && rule.confidence !== null) {
    out.confidence = rule.confidence
  }

  const isRegexStrategy = rule.strategy === 'seasonAndEpisodeNumber' || rule.strategy === 'byAbsoluteEpisodeNumber'
  const isTitleStrategy = rule.strategy === 'itemTitleExact' || rule.strategy === 'itemTitleIncludes' || rule.strategy === 'itemTitleEqualsAirdate'

  if (isRegexStrategy) {
    if (rule.strategy === 'seasonAndEpisodeNumber' && rule.seasonRegex) {
      out.seasonRegex = rule.seasonRegex
    }
    if (rule.episodeRegex) out.episodeRegex = rule.episodeRegex
    if (rule.captureGroup !== undefined && rule.captureGroup !== null && rule.captureGroup !== 1) {
      out.captureGroup = rule.captureGroup
    }
  }

  if (isTitleStrategy && rule.titleRules.length > 0) {
    out.titleRules = rule.titleRules.map(tr => {
      const part: any = { type: tr.type }
      if (tr.type === 'static') {
        part.value = tr.value ?? ''
      } else {
        if (tr.field) part.field = tr.field
        if (tr.pattern) part.pattern = tr.pattern
        if (tr.captureGroup !== undefined && tr.captureGroup !== null && tr.captureGroup !== 0) {
          part.captureGroup = tr.captureGroup
        }
      }
      return part
    })
  }

  const filters: any = {}
  if (rule.filters.all.length > 0) filters.all = rule.filters.all.filter(c => c.field && c.op)
  if (rule.filters.any.length > 0) filters.any = rule.filters.any.filter(c => c.field && c.op)
  if (rule.filters.not.length > 0) filters.not = rule.filters.not.filter(c => c.field && c.op)
  if (Object.keys(filters).length > 0) out.filters = filters

  return out
}

const jsonOutput = computed(() => {
  const out: any = {
    topic: state.topic || undefined,
  }

  if (state.aliases.length > 0) {
    out.aliases = state.aliases.filter(a => a.trim())
  }

  const media: any = {
    name: state.media.name || undefined,
    type: state.media.type,
  }
  if (state.media.tvdbId) media.tvdbId = state.media.tvdbId
  if (state.media.imdbId) media.imdbId = state.media.imdbId
  if (state.media.tmdbId) media.tmdbId = state.media.tmdbId
  out.media = media

  if (state.confidence !== undefined && state.confidence !== null) {
    out.confidence = state.confidence
  }

  if (state.rules.length > 0) {
    out.rules = state.rules.map(buildRuleOutput)
  }

  return out
})

const jsonString = computed(() => JSON.stringify(jsonOutput.value, null, 2))

function importJson(json: string) {
  const parsed = JSON.parse(json)
  state.topic = parsed.topic ?? ''
  state.aliases = parsed.aliases ?? []
  state.media.name = parsed.media?.name ?? ''
  state.media.type = parsed.media?.type ?? 'show'
  state.media.tvdbId = parsed.media?.tvdbId ?? null
  state.media.imdbId = parsed.media?.imdbId ?? ''
  state.media.tmdbId = parsed.media?.tmdbId ?? null
  state.confidence = parsed.confidence ?? 1.0
  mediaNameManuallySet.value = !!(parsed.media?.name && parsed.media.name !== parsed.topic)

  state.rules = (parsed.rules ?? []).map((r: any) => ({
    id: r.id ?? '',
    priority: r.priority ?? 0,
    strategy: r.strategy ?? '',
    confidence: r.confidence ?? undefined,
    seasonRegex: r.seasonRegex ?? '',
    episodeRegex: r.episodeRegex ?? '',
    captureGroup: r.captureGroup ?? undefined,
    filters: {
      all: r.filters?.all ?? [],
      any: r.filters?.any ?? [],
      not: r.filters?.not ?? [],
    },
    titleRules: (r.titleRules ?? []).map((tr: any) => ({
      type: tr.type ?? 'static',
      value: tr.value ?? '',
      field: tr.field ?? '',
      pattern: tr.pattern ?? '',
      captureGroup: tr.captureGroup ?? undefined,
    })),
  }))

  showImport.value = false
}

function getTopicSlug(): string {
  return (state.topic || 'ruleset').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
}
</script>

<template>
  <div class="builder-layout">
    <div class="builder-form">
      <IdentityForm
        :state="state"
        :validation="validationMessages"
        @media-name-input="onMediaNameInput"
      />

      <div class="builder-section">
        <div class="section-header">
          <h3>Rules</h3>
          <button class="btn btn-primary btn-sm" @click="addRule">+ Add Rule</button>
        </div>
        <div v-if="state.rules.length === 0" class="empty-state">
          No rules yet. Click "Add Rule" to get started.
        </div>
        <RuleCard
          v-for="(rule, index) in state.rules"
          :key="index"
          :rule="rule"
          :index="index"
          :validation="validationMessages"
          @remove="removeRule(index)"
        />
      </div>
    </div>

    <div class="builder-preview">
      <JsonPreview
        :json="jsonString"
        :topic-slug="getTopicSlug()"
        :errors="errors"
        :warnings="warnings"
      />
      <div class="preview-actions">
        <button class="btn btn-secondary btn-sm" @click="showImport = true">Import JSON</button>
      </div>
    </div>
  </div>

  <ImportModal
    v-if="showImport"
    @import="importJson"
    @close="showImport = false"
  />
</template>

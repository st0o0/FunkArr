<template>
  <div>
    <AppBreadcrumb :items="breadcrumbItems" />

    <div v-if="loadingDetail" class="space-y-5">
      <SkeletonCard />
      <SkeletonCard />
    </div>
    <div v-else-if="loadError" class="text-status-fail text-sm">{{ loadError }}</div>

    <div v-else class="grid grid-cols-[1fr_380px] gap-6">
      <!-- Left pane: Builder form -->
      <div class="space-y-5">
        <h1 class="text-2xl font-bold text-text-primary tracking-tight">{{ isEditMode ? 'Edit RuleSet' : 'New RuleSet' }}</h1>

        <!-- Identity Section -->
        <section>
          <h2 class="text-xs font-semibold uppercase tracking-wider mb-2 text-text-muted">Identity</h2>
          <div class="bg-surface-raised rounded-xl border border-border-default p-4 space-y-3">
            <div>
              <label class="block text-xs text-text-muted mb-1">RuleSet ID</label>
              <input
                v-model="form.ruleSetId"
                :disabled="isEditMode"
                type="text"
                placeholder="my-show"
                class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50 disabled:opacity-50"
              />
              <div v-if="ruleSetIdError" class="text-status-fail text-xs mt-1">{{ ruleSetIdError }}</div>
            </div>
            <div>
              <label class="block text-xs text-text-muted mb-1">Topic</label>
              <input
                v-model="form.topic"
                type="text"
                placeholder="Show Name"
                class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50"
              />
            </div>
            <div>
              <label class="block text-xs text-text-muted mb-1">Aliases</label>
              <div class="space-y-1.5">
                <div v-for="(_, idx) in form.aliases" :key="idx" class="flex items-center gap-2">
                  <input
                    v-model="form.aliases[idx]"
                    type="text"
                    placeholder="Alias"
                    class="flex-1 bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50"
                  />
                  <button class="text-status-fail/60 hover:text-status-fail text-sm transition-colors" @click="form.aliases.splice(idx, 1)">×</button>
                </div>
              </div>
              <button class="text-xs text-brand-400 hover:text-brand-300 mt-1.5 transition-colors" @click="form.aliases.push('')">+ Add Alias</button>
            </div>
            <div class="grid grid-cols-3 gap-3">
              <div>
                <label class="block text-xs text-text-muted mb-1">TVDB ID</label>
                <input
                  v-model.number="form.tvdbId"
                  type="number"
                  placeholder="—"
                  class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50"
                />
              </div>
              <div>
                <label class="block text-xs text-text-muted mb-1">IMDB ID</label>
                <input
                  v-model="form.imdbId"
                  type="text"
                  placeholder="tt..."
                  class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50"
                />
              </div>
              <div>
                <label class="block text-xs text-text-muted mb-1">TMDB ID</label>
                <input
                  v-model.number="form.tmdbId"
                  type="number"
                  placeholder="—"
                  class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50"
                />
              </div>
            </div>
          </div>
        </section>

        <!-- Default Confidence -->
        <section>
          <h2 class="text-xs font-semibold uppercase tracking-wider mb-2 text-text-muted">Default Confidence</h2>
          <div class="bg-surface-raised rounded-xl border border-border-default p-4">
            <input
              v-model.number="form.confidence"
              type="number"
              min="0"
              max="1"
              step="0.01"
              class="w-32 bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body focus:outline-none focus:border-brand-500/50"
            />
          </div>
        </section>

        <!-- Rules Section -->
        <section>
          <div class="flex items-center justify-between mb-2">
            <h2 class="text-xs font-semibold uppercase tracking-wider text-text-muted">Matching Rules</h2>
            <button class="text-xs text-brand-400 hover:text-brand-300 transition-colors" @click="addRule">+ Add Rule</button>
          </div>

          <div v-if="form.rules.length === 0" class="text-text-muted text-sm">No rules defined.</div>

          <div v-else class="space-y-2.5">
            <div
              v-for="(rule, rIdx) in form.rules"
              :key="rIdx"
              class="bg-surface-raised rounded-xl border border-border-default overflow-hidden"
              :class="rule.expanded ? 'border-l-2 border-l-brand-500/30' : ''"
            >
              <!-- Rule header (click to toggle) -->
              <div
                class="flex items-center justify-between px-4 py-3 cursor-pointer hover:bg-surface-elevated/30 transition-colors"
                @click="rule.expanded = !rule.expanded"
              >
                <div class="flex items-baseline gap-3">
                  <span class="font-mono text-sm font-semibold text-brand-400">{{ rule.id || '(no id)' }}</span>
                  <span class="text-text-muted text-xs">{{ strategyLabel(rule.strategy) }}</span>
                  <span class="text-text-muted text-xs">prio {{ rule.priority }}</span>
                </div>
                <div class="flex items-center gap-2">
                  <button class="text-status-fail/60 hover:text-status-fail text-sm transition-colors" @click.stop="form.rules.splice(rIdx, 1)">×</button>
                  <svg class="w-4 h-4 text-text-muted transition-transform" :class="rule.expanded ? 'rotate-180' : ''" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M4 6l4 4 4-4"/></svg>
                </div>
              </div>

              <!-- Rule body (expanded) -->
              <div v-if="rule.expanded" class="px-4 pb-4 space-y-3 border-t border-border-default pt-3">
                <div class="grid grid-cols-3 gap-3">
                  <div>
                    <label class="block text-xs text-text-muted mb-1">Rule ID</label>
                    <input v-model="rule.id" type="text" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body font-mono focus:outline-none focus:border-brand-500/50" />
                  </div>
                  <div>
                    <label class="block text-xs text-text-muted mb-1">Priority</label>
                    <input v-model.number="rule.priority" type="number" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body focus:outline-none focus:border-brand-500/50" />
                  </div>
                  <div>
                    <label class="block text-xs text-text-muted mb-1">Confidence</label>
                    <input v-model.number="rule.confidence" type="number" min="0" max="1" step="0.01" placeholder="default" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
                  </div>
                </div>

                <!-- Strategy picker -->
                <div>
                  <label class="block text-xs text-text-muted mb-1">Strategy</label>
                  <select
                    v-model="rule.strategy"
                    class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body focus:outline-none focus:border-brand-500/50"
                    @change="onStrategyChange(rule)"
                  >
                    <option value="">— select —</option>
                    <option value="seasonAndEpisodeNumber">Season & Episode Number</option>
                    <option value="byAbsoluteEpisodeNumber">Absolute Episode Number</option>
                    <option value="itemTitleExact">Title Exact Match</option>
                    <option value="itemTitleIncludes">Title Includes</option>
                    <option value="itemTitleEqualsAirdate">Title Equals Airdate</option>
                  </select>
                </div>

                <!-- Strategy-specific fields: RegexCapture -->
                <div v-if="rule.strategy === 'seasonAndEpisodeNumber' || rule.strategy === 'byAbsoluteEpisodeNumber'" class="space-y-2">
                  <div v-if="rule.strategy === 'seasonAndEpisodeNumber'">
                    <label class="block text-xs text-text-muted mb-1">Season Regex</label>
                    <input v-model="rule.seasonRegex" type="text" placeholder="(?<=S)(\d{2,4})(?=/E)" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body font-mono placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
                  </div>
                  <div>
                    <label class="block text-xs text-text-muted mb-1">Episode Regex</label>
                    <input v-model="rule.episodeRegex" type="text" placeholder="(?<=E)(\d{2,4})(?=\))" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body font-mono placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
                  </div>
                  <div>
                    <label class="block text-xs text-text-muted mb-1">Capture Group (optional)</label>
                    <input v-model.number="rule.captureGroup" type="number" placeholder="auto" class="w-32 bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
                  </div>
                </div>

                <!-- Strategy-specific fields: TitleConstruction -->
                <div v-if="rule.strategy === 'itemTitleExact' || rule.strategy === 'itemTitleIncludes'" class="space-y-2">
                  <label class="block text-xs text-text-muted mb-1">Title Rules</label>
                  <div v-for="(tp, tIdx) in rule.titleRules" :key="tIdx" class="flex items-start gap-2 p-2 bg-surface-elevated/50 rounded-lg">
                    <select v-model="tp.type" class="bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body focus:outline-none focus:border-brand-500/50">
                      <option value="static">static</option>
                      <option value="regex">regex</option>
                    </select>
                    <template v-if="tp.type === 'static'">
                      <input v-model="tp.value" type="text" placeholder="static text" class="flex-1 bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
                    </template>
                    <template v-else>
                      <select v-model="tp.field" class="bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body focus:outline-none focus:border-brand-500/50">
                        <option value="title">title</option>
                        <option value="topic">topic</option>
                        <option value="channel">channel</option>
                        <option value="description">description</option>
                      </select>
                      <input v-model="tp.pattern" type="text" placeholder="regex pattern" class="flex-1 bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body font-mono placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
                      <input v-model.number="tp.captureGroup" type="number" placeholder="grp" class="w-14 bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
                    </template>
                    <button class="text-status-fail/60 hover:text-status-fail text-sm transition-colors" @click="rule.titleRules.splice(tIdx, 1)">×</button>
                  </div>
                  <button class="text-xs text-brand-400 hover:text-brand-300 transition-colors" @click="addTitleRule(rule)">+ Add Title Part</button>
                </div>

                <!-- Filter builder -->
                <div class="space-y-2">
                  <label class="block text-xs text-text-muted mb-1">Filters</label>
                  <div v-for="section in (['all', 'any', 'not'] as const)" :key="section" class="space-y-1">
                    <div class="flex items-center justify-between">
                      <span class="text-xs font-semibold uppercase tracking-wider text-text-muted">{{ section }}</span>
                      <button class="text-xs text-brand-400 hover:text-brand-300 transition-colors" @click="addFilterCondition(rule, section)">+ Add</button>
                    </div>
                    <div v-if="rule.filters[section].length === 0" class="text-text-muted text-xs pl-2">(no conditions)</div>
                    <div v-for="(cond, cIdx) in rule.filters[section]" :key="cIdx" class="flex items-center gap-1.5">
                      <select v-model="cond.field" class="bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body focus:outline-none focus:border-brand-500/50">
                        <option value="title">title</option>
                        <option value="topic">topic</option>
                        <option value="channel">channel</option>
                        <option value="description">description</option>
                        <option value="duration">duration</option>
                        <option value="timestamp">timestamp</option>
                      </select>
                      <select v-model="cond.op" class="bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body focus:outline-none focus:border-brand-500/50">
                        <option value="eq">eq</option>
                        <option value="contains">contains</option>
                        <option value="notContains">notContains</option>
                        <option value="greaterThan">greaterThan</option>
                        <option value="lessThan">lessThan</option>
                        <option value="regex">regex</option>
                      </select>
                      <input v-model="cond.value" type="text" placeholder="value" class="flex-1 bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body placeholder-text-muted focus:outline-none focus:border-brand-500/50" />
                      <button class="text-status-fail/60 hover:text-status-fail text-xs transition-colors" @click="rule.filters[section].splice(cIdx, 1)">×</button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- Save button -->
        <div class="flex items-center gap-3">
          <button
            class="px-4 py-2 bg-brand-600 text-white rounded-lg hover:bg-brand-500 text-sm transition-colors disabled:opacity-50 active:scale-[0.98]"
            :disabled="saving"
            @click="handleSave"
          >
            {{ saving ? 'Saving...' : 'Save' }}
          </button>
          <router-link
            :to="isEditMode ? `/rulesets/${editId}` : '/rulesets'"
            class="px-4 py-2 bg-surface-elevated text-text-body rounded-lg hover:border-brand-500/40 text-sm transition-colors border border-border-default active:scale-[0.98]"
          >
            Cancel
          </router-link>
        </div>
        <div v-if="saveError" class="text-status-fail text-sm">{{ saveError }}</div>
      </div>

      <!-- Right pane: Debugger -->
      <div class="overflow-y-auto">
        <DebuggerPanel :builder-state="debuggerState" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  getRuleSetRaw, createRuleSet, updateRuleSet,
  type RuleSetWriteRequest, type RuleSetWriteRule, type FilterConditionInput, type TitleRuleInput,
} from '../api/rulesets'
import DebuggerPanel from '../components/DebuggerPanel.vue'
import SkeletonCard from '../components/SkeletonCard.vue'
import AppBreadcrumb from '../components/AppBreadcrumb.vue'
import { useToast } from '../composables/useToast'

const { toast } = useToast()

interface FormFilterCondition {
  field: string
  op: string
  value: string
}

interface FormTitleRule {
  type: string
  field: string
  pattern: string
  captureGroup: number | null
  value: string
}

interface FormRule {
  id: string
  priority: number
  confidence: number | null
  strategy: string
  seasonRegex: string
  episodeRegex: string
  captureGroup: number | null
  filters: {
    all: FormFilterCondition[]
    any: FormFilterCondition[]
    not: FormFilterCondition[]
  }
  titleRules: FormTitleRule[]
  expanded: boolean
}

const route = useRoute()
const router = useRouter()

const editId = route.params.id as string | undefined
const isEditMode = computed(() => !!editId)

const breadcrumbItems = computed(() => {
  if (isEditMode.value) {
    return [
      { label: 'RuleSets', to: '/rulesets' },
      { label: editId!, to: `/rulesets/${editId}` },
      { label: 'Edit' },
    ]
  }
  return [
    { label: 'RuleSets', to: '/rulesets' },
    { label: 'New' },
  ]
})

const loadingDetail = ref(false)
const loadError = ref<string | null>(null)
const saving = ref(false)
const saveError = ref<string | null>(null)

const form = reactive({
  ruleSetId: '',
  topic: '',
  aliases: [] as string[],
  tvdbId: null as number | null,
  imdbId: '',
  tmdbId: null as number | null,
  confidence: 0.8,
  standalone: false,
  disable: [] as string[],
  rules: [] as FormRule[],
})

const ruleSetIdError = computed(() => {
  if (!form.ruleSetId && !isEditMode.value) return ''
  if (form.ruleSetId && !/^[a-z0-9]+(-[a-z0-9]+)*$/.test(form.ruleSetId)) return 'Must be kebab-case (lowercase, numbers, hyphens)'
  return ''
})

const debuggerState = computed(() => ({
  confidence: form.confidence,
  rules: form.rules,
}))

function createEmptyRule(): FormRule {
  return {
    id: `rule-${form.rules.length + 1}`,
    priority: form.rules.length * 10,
    confidence: null,
    strategy: '',
    seasonRegex: '',
    episodeRegex: '',
    captureGroup: null,
    filters: { all: [], any: [], not: [] },
    titleRules: [],
    expanded: true,
  }
}

function addRule() {
  form.rules.push(createEmptyRule())
}

function onStrategyChange(rule: FormRule) {
  rule.seasonRegex = ''
  rule.episodeRegex = ''
  rule.captureGroup = null
  rule.titleRules = []
}

function addTitleRule(rule: FormRule) {
  rule.titleRules.push({ type: 'regex', field: 'title', pattern: '', captureGroup: null, value: '' })
}

function addFilterCondition(rule: FormRule, section: 'all' | 'any' | 'not') {
  rule.filters[section].push({ field: 'title', op: 'contains', value: '' })
}

function strategyLabel(strategy: string): string {
  switch (strategy) {
    case 'seasonAndEpisodeNumber': return 'Season & Episode'
    case 'byAbsoluteEpisodeNumber': return 'Absolute Episode'
    case 'itemTitleExact': return 'Title Exact'
    case 'itemTitleIncludes': return 'Title Includes'
    case 'itemTitleEqualsAirdate': return 'Airdate'
    default: return strategy || '(none)'
  }
}

function serializeForm(): RuleSetWriteRequest {
  const rules: RuleSetWriteRule[] = form.rules.map(r => {
    const filters: RuleSetWriteRule['filters'] = {} as any
    if (r.filters.all.length > 0) (filters as any).all = r.filters.all.map(c => ({ field: c.field, op: c.op, value: c.value } as FilterConditionInput))
    if (r.filters.any.length > 0) (filters as any).any = r.filters.any.map(c => ({ field: c.field, op: c.op, value: c.value } as FilterConditionInput))
    if (r.filters.not.length > 0) (filters as any).not = r.filters.not.map(c => ({ field: c.field, op: c.op, value: c.value } as FilterConditionInput))
    const hasFilters = r.filters.all.length > 0 || r.filters.any.length > 0 || r.filters.not.length > 0

    const titleRules: TitleRuleInput[] | null = r.titleRules.length > 0
      ? r.titleRules.map(tp => ({
          type: tp.type,
          field: tp.type === 'regex' ? tp.field : undefined,
          pattern: tp.type === 'regex' ? tp.pattern : undefined,
          captureGroup: tp.type === 'regex' ? tp.captureGroup : undefined,
          value: tp.type === 'static' ? tp.value : undefined,
        } as TitleRuleInput))
      : null

    return {
      id: r.id,
      priority: r.priority,
      confidence: r.confidence,
      strategy: r.strategy,
      seasonRegex: r.seasonRegex || null,
      episodeRegex: r.episodeRegex || null,
      captureGroup: r.captureGroup,
      filters: hasFilters ? filters : null,
      titleRules,
    }
  })

  const media = (form.tvdbId || form.imdbId || form.tmdbId)
    ? { tvdbId: form.tvdbId, imdbId: form.imdbId || null, tmdbId: form.tmdbId }
    : undefined

  return {
    ruleSetId: isEditMode.value ? undefined : form.ruleSetId,
    topic: form.topic,
    aliases: form.aliases.filter(a => a.trim() !== ''),
    media,
    confidence: form.confidence,
    standalone: form.standalone || undefined,
    disable: form.disable.length > 0 ? form.disable : undefined,
    rules,
  }
}

async function handleSave() {
  saveError.value = null

  if (!isEditMode.value && !form.ruleSetId) {
    saveError.value = 'RuleSet ID is required'
    return
  }
  if (ruleSetIdError.value) {
    saveError.value = ruleSetIdError.value
    return
  }
  if (!form.topic.trim()) {
    saveError.value = 'Topic is required'
    return
  }

  saving.value = true
  try {
    const data = serializeForm()
    if (isEditMode.value) {
      await updateRuleSet(editId!, data)
      toast('RuleSet saved')
      router.push(`/rulesets/${editId}`)
    } else {
      await createRuleSet(data)
      toast('RuleSet saved')
      router.push(`/rulesets/${form.ruleSetId}`)
    }
  } catch (e) {
    saveError.value = e instanceof Error ? e.message : 'Failed to save'
    toast(saveError.value!, 'error')
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  if (!editId) return

  loadingDetail.value = true
  try {
    const raw = await getRuleSetRaw(editId)
    form.ruleSetId = editId
    form.topic = raw.topic || ''
    form.aliases = raw.aliases ? [...raw.aliases] : []
    form.tvdbId = raw.media?.tvdbId ?? null
    form.imdbId = raw.media?.imdbId ?? ''
    form.tmdbId = raw.media?.tmdbId ?? null
    form.confidence = raw.confidence ?? 0.8

    form.rules = (raw.rules || []).map((r: RuleSetWriteRule, idx: number) => ({
      id: r.id || `rule-${idx + 1}`,
      priority: r.priority ?? idx * 10,
      confidence: r.confidence ?? null,
      strategy: r.strategy || '',
      seasonRegex: r.seasonRegex ?? '',
      episodeRegex: r.episodeRegex ?? '',
      captureGroup: r.captureGroup ?? null,
      filters: {
        all: (r.filters?.all || []).map(f => ({ field: f.field || '', op: f.op || '', value: f.value || '' })),
        any: (r.filters?.any || []).map(f => ({ field: f.field || '', op: f.op || '', value: f.value || '' })),
        not: (r.filters?.not || []).map(f => ({ field: f.field || '', op: f.op || '', value: f.value || '' })),
      },
      titleRules: (r.titleRules || []).map(tr => ({
        type: tr.type || 'static',
        field: tr.field ?? 'title',
        pattern: tr.pattern ?? '',
        captureGroup: tr.captureGroup ?? null,
        value: tr.value ?? '',
      })),
      expanded: idx === 0,
    }))
  } catch (e) {
    loadError.value = e instanceof Error ? e.message : 'Failed to load ruleset'
  } finally {
    loadingDetail.value = false
  }
})
</script>

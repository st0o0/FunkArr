<template>
  <div class="max-w-6xl mx-auto">
    <AppBreadcrumb :items="breadcrumbItems" />

    <div v-if="loadingDetail" class="space-y-5">
      <SkeletonCard />
      <SkeletonCard />
    </div>
    <div v-else-if="loadError" class="text-status-fail text-sm">{{ loadError }}</div>

    <div v-else class="grid grid-cols-[1fr_420px] gap-6">
      <!-- Left pane: Builder form -->
      <div class="space-y-4">
        <h1 class="text-xl font-semibold text-text-primary tracking-tight">{{ isEditMode ? $t('builder.editRuleSet') : $t('builder.newRuleSet') }}</h1>

        <div v-if="validationErrors.length > 0" class="bg-status-fail/10 border border-status-fail/30 rounded-lg p-4">
          <p class="text-sm font-medium text-status-fail mb-2">{{ $t('builder.validationErrorCount', { count: validationErrors.length }) }}:</p>
          <ul class="space-y-1">
            <li v-for="(err, idx) in validationErrors" :key="idx" class="text-sm text-text-body">
              <span class="font-mono text-xs text-text-secondary">{{ err.field }}</span>
              <span class="mx-1 text-text-muted">—</span>
              <span>{{ err.message }}</span>
            </li>
          </ul>
        </div>

        <!-- Identity Section -->
        <section>
          <h2 class="text-sm font-semibold mb-1 text-text-body">{{ $t('builder.identity') }}</h2>
          <p class="text-xs text-text-secondary mb-2">{{ $t('builder.identityDescription') }}</p>
          <div class="bg-surface-raised rounded-lg border border-border-default p-4 space-y-3">
            <div>
              <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.ruleSetId') }}</label>
              <input
                v-model="form.ruleSetId"
                :disabled="isEditMode"
                type="text"
                :placeholder="$t('builder.ruleSetIdPlaceholder')"
                class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus disabled:opacity-50"
              />
              <div v-if="ruleSetIdError" class="text-status-fail text-xs mt-1">{{ ruleSetIdError }}</div>
            </div>
            <div>
              <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.topic') }}</label>
              <input
                v-model="form.topic"
                type="text"
                :placeholder="$t('builder.topicPlaceholder')"
                class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
              />
            </div>
            <div>
              <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.aliasLabel') }}</label>
              <div class="space-y-1.5">
                <div v-for="(_, idx) in form.aliases" :key="idx" class="flex items-center gap-2">
                  <input
                    v-model="form.aliases[idx]"
                    type="text"
                    :placeholder="$t('builder.aliasPlaceholder')"
                    class="flex-1 bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
                  />
                  <button class="text-status-fail/60 hover:text-status-fail text-sm transition-colors" @click="form.aliases.splice(idx, 1)">×</button>
                </div>
              </div>
              <button class="text-xs text-text-secondary hover:text-text-body mt-1.5 transition-colors" @click="form.aliases.push('')">{{ $t('builder.addAlias') }}</button>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.mediaType') }}</label>
                <select
                  v-model="form.mediaType"
                  class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:outline-none focus:border-border-focus"
                >
                  <option value="show">{{ $t('builder.show') }}</option>
                  <option value="movie">{{ $t('builder.movie') }}</option>
                </select>
              </div>
              <div>
                <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.mediaName') }}</label>
                <input
                  v-model="form.mediaName"
                  type="text"
                  :placeholder="$t('builder.mediaNamePlaceholder')"
                  class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
                  @input="mediaNameEdited = true"
                />
                <p class="text-xs text-text-muted mt-0.5">{{ $t('builder.mediaNameHint') }}</p>
              </div>
            </div>
            <div class="grid grid-cols-3 gap-3">
              <div>
                <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.tvdbId') }}</label>
                <input
                  v-model.number="form.tvdbId"
                  type="number"
                  placeholder="-"
                  class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
                />
              </div>
              <div>
                <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.imdbId') }}</label>
                <input
                  v-model="form.imdbId"
                  type="text"
                  placeholder="tt..."
                  class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
                />
              </div>
              <div>
                <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.tmdbId') }}</label>
                <input
                  v-model.number="form.tmdbId"
                  type="number"
                  placeholder="-"
                  class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus"
                />
              </div>
            </div>
          </div>
        </section>

        <!-- Default Confidence -->
        <section>
          <h2 class="text-sm font-semibold mb-1 text-text-body">{{ $t('builder.defaultConfidence') }}</h2>
          <p class="text-xs text-text-secondary mb-2">{{ $t('builder.defaultConfidenceDescription') }}</p>
          <div class="bg-surface-raised rounded-lg border border-border-default p-4">
            <input
              v-model.number="form.confidence"
              type="number"
              min="0"
              max="1"
              step="0.01"
              class="w-32 bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body focus:outline-none focus:border-border-focus"
            />
          </div>
        </section>

        <!-- Enrichment Section -->
        <section>
          <h2 class="text-sm font-semibold mb-1 text-text-body">{{ $t('builder.enrichment') }}</h2>
          <p class="text-xs text-text-secondary mb-2">{{ $t('builder.enrichmentDescription') }}</p>
          <div class="bg-surface-raised rounded-lg border border-border-default p-4 space-y-3">
            <div class="flex items-center justify-between">
              <label class="text-xs text-text-body font-medium">{{ $t('builder.enrichmentEnabled') }}</label>
              <button
                type="button"
                class="relative inline-flex h-5 w-9 items-center rounded-full transition-colors"
                :class="form.enrichment.enabled ? 'bg-accent' : 'bg-surface-elevated border border-border-default'"
                @click="form.enrichment.enabled = !form.enrichment.enabled"
              >
                <span
                  class="inline-block h-3.5 w-3.5 rounded-full bg-white transition-transform"
                  :class="form.enrichment.enabled ? 'translate-x-4' : 'translate-x-0.5'"
                />
              </button>
            </div>

            <div :class="{ 'opacity-40 pointer-events-none': !form.enrichment.enabled }">
              <div class="mb-3">
                <label class="block text-xs text-text-body mb-1.5 font-medium">{{ $t('builder.enrichmentMethods') }}</label>
                <div class="flex flex-wrap gap-2">
                  <label v-for="m in enrichmentMethodOptions" :key="m.value" class="flex items-center gap-1.5 text-xs text-text-body cursor-pointer">
                    <input
                      type="checkbox"
                      :checked="form.enrichment.methods.includes(m.value)"
                      class="rounded border-border-default text-accent focus:ring-accent"
                      @change="toggleMethod(m.value)"
                    />
                    {{ m.label }}
                  </label>
                </div>
              </div>

              <div class="grid grid-cols-2 gap-3">
                <div v-if="form.enrichment.methods.includes(EnrichmentMethod.Title)">
                  <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.titleThreshold') }}</label>
                  <input v-model.number="form.enrichment.titleThreshold" type="number" min="0" max="1" step="0.05" class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:outline-none focus:border-border-focus" />
                </div>
                <div v-if="form.enrichment.methods.includes(EnrichmentMethod.Airdate)">
                  <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.airdateTolerance') }}</label>
                  <input v-model.number="form.enrichment.airdateTolerance" type="number" min="0" step="1" class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:outline-none focus:border-border-focus" />
                </div>
                <div v-if="form.enrichment.methods.includes(RUNTIME_METHOD)">
                  <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.runtimeTolerance') }}</label>
                  <input v-model.number="form.enrichment.runtimeTolerance" type="number" min="0" max="1" step="0.05" class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:outline-none focus:border-border-focus" />
                </div>
                <div v-if="form.enrichment.methods.includes(RUNTIME_METHOD)">
                  <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.runtimeMode') }}</label>
                  <select v-model.number="form.enrichment.runtimeMode" class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:outline-none focus:border-border-focus">
                    <option :value="RuntimeMode.Tiebreaker">{{ $t('builder.runtimeModeTiebreaker') }}</option>
                    <option :value="RuntimeMode.Filter">{{ $t('builder.runtimeModeFilter') }}</option>
                  </select>
                </div>
                <div v-if="form.enrichment.methods.includes(YEAR_METHOD)">
                  <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.yearTolerance') }}</label>
                  <input v-model.number="form.enrichment.yearTolerance" type="number" min="0" step="1" class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body focus:outline-none focus:border-border-focus" />
                </div>
              </div>

              <div v-if="enrichmentHint" class="mt-2 text-xs text-amber-500 flex items-center gap-1.5">
                <svg class="w-3.5 h-3.5 shrink-0" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M8 5v3m0 2.5v.5"/><circle cx="8" cy="8" r="6.5"/></svg>
                {{ enrichmentHint }}
              </div>
            </div>
          </div>
        </section>

        <!-- Rules Section -->
        <section>
          <div class="flex items-center justify-between mb-2">
            <div>
              <h2 class="text-sm font-semibold text-text-body">{{ $t('builder.matchingRules') }}</h2>
              <p class="text-xs text-text-secondary mt-0.5">{{ $t('builder.matchingRulesDescription') }}</p>
            </div>
            <button class="text-xs text-text-secondary hover:text-text-body transition-colors" @click="addRule">{{ $t('builder.addRule') }}</button>
          </div>

          <div v-if="form.rules.length === 0" class="text-text-secondary text-sm">{{ $t('builder.noRulesDefined') }}</div>

          <div v-else class="space-y-2.5">
            <div
              v-for="(rule, rIdx) in form.rules"
              :key="rIdx"
              class="bg-surface-raised rounded-lg border border-border-default overflow-hidden"
            >
              <!-- Rule header (click to toggle) -->
              <div
                class="flex items-center justify-between px-4 py-3 cursor-pointer hover:bg-surface-elevated/30 transition-colors"
                @click="rule.expanded = !rule.expanded"
              >
                <div class="flex items-baseline gap-3">
                  <span class="font-mono text-sm font-medium text-text-primary">{{ rule.id || $t('builder.noId') }}</span>
                  <span class="text-text-secondary text-xs">{{ strategyLabel(rule.strategy, t) }}</span>
                  <span class="text-text-secondary text-xs">prio {{ rule.priority }}</span>
                </div>
                <div class="flex items-center gap-2">
                  <button class="text-status-fail/60 hover:text-status-fail text-sm transition-colors" @click.stop="form.rules.splice(rIdx, 1)">×</button>
                  <svg class="w-4 h-4 text-text-secondary transition-transform" :class="rule.expanded ? 'rotate-180' : ''" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M4 6l4 4 4-4"/></svg>
                </div>
              </div>

              <!-- Rule body (expanded) -->
              <div v-if="rule.expanded" class="px-4 pb-4 space-y-3 border-t border-border-default pt-3">
                <div class="grid grid-cols-3 gap-3">
                  <div>
                    <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.ruleId') }}</label>
                    <input v-model="rule.id" type="text" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body font-mono focus:outline-none focus:border-border-focus" />
                  </div>
                  <div>
                    <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.priority') }}</label>
                    <input v-model.number="rule.priority" type="number" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body focus:outline-none focus:border-border-focus" />
                  </div>
                  <div>
                    <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.confidence') }}</label>
                    <input v-model.number="rule.confidence" type="number" min="0" max="1" step="0.01" placeholder="default" class="w-full bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus" />
                  </div>
                </div>

                <!-- Strategy picker -->
                <div>
                  <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.strategy') }}</label>
                  <select
                    v-model.number="rule.strategy"
                    class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body focus:outline-none focus:border-border-focus"
                    @change="onStrategyChange(rule)"
                  >
                    <option :value="-1">{{ $t('builder.selectStrategy') }}</option>
                    <option :value="Strategy.SeasonAndEpisodeNumber">{{ $t('builder.seasonEpisodeNumber') }}</option>
                    <option :value="Strategy.AbsoluteEpisodeNumber">{{ $t('builder.absoluteEpisodeNumber') }}</option>
                    <option :value="Strategy.TitleExact">{{ $t('builder.titleExactMatch') }}</option>
                    <option :value="Strategy.TitleIncludes">{{ $t('builder.titleIncludes') }}</option>
                    <option :value="Strategy.AirdateExtraction">{{ $t('builder.titleEqualsAirdate') }}</option>
                  </select>
                </div>

                <!-- Strategy-specific fields: RegexCapture -->
                <div v-if="rule.strategy === Strategy.SeasonAndEpisodeNumber || rule.strategy === Strategy.AbsoluteEpisodeNumber" class="space-y-2">
                  <div v-if="rule.strategy === Strategy.SeasonAndEpisodeNumber">
                    <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.seasonRegex') }}</label>
                    <input v-model="rule.seasonRegex" type="text" placeholder="(?<=S)(\d{2,4})(?=/E)" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body font-mono placeholder-text-muted focus:outline-none focus:border-border-focus" />
                  </div>
                  <div>
                    <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.episodeRegex') }}</label>
                    <input v-model="rule.episodeRegex" type="text" placeholder="(?<=E)(\d{2,4})(?=\))" class="w-full bg-surface-elevated border border-border-default rounded-lg px-3 py-2 text-sm text-text-body font-mono placeholder-text-muted focus:outline-none focus:border-border-focus" />
                  </div>
                  <div>
                    <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.captureGroup') }}</label>
                    <input v-model.number="rule.captureGroup" type="number" placeholder="auto" class="w-32 bg-surface-elevated border border-border-default rounded-md px-3 py-1.5 text-sm text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus" />
                  </div>
                </div>

                <!-- Strategy-specific fields: TitleConstruction -->
                <div v-if="rule.strategy === Strategy.TitleExact || rule.strategy === Strategy.TitleIncludes" class="space-y-2">
                  <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.titleRules') }}</label>
                  <div v-for="(tp, tIdx) in rule.titleRules" :key="tIdx" class="flex items-start gap-2 p-2 bg-surface-elevated/50 rounded-lg">
                    <select v-model.number="tp.type" class="bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body focus:outline-none focus:border-border-focus">
                      <option :value="TitlePartType.Static">{{ titlePartLabel(TitlePartType.Static, t) }}</option>
                      <option :value="TitlePartType.Regex">{{ titlePartLabel(TitlePartType.Regex, t) }}</option>
                    </select>
                    <template v-if="tp.type === TitlePartType.Static">
                      <input v-model="tp.value" type="text" placeholder="static text" class="flex-1 bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus" />
                    </template>
                    <template v-else>
                      <select v-model.number="tp.field" class="bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body focus:outline-none focus:border-border-focus">
                        <option :value="FF.Title">{{ fieldLabel(FF.Title, t) }}</option>
                        <option :value="FF.Topic">{{ fieldLabel(FF.Topic, t) }}</option>
                        <option :value="FF.Channel">{{ fieldLabel(FF.Channel, t) }}</option>
                        <option :value="FF.Description">{{ fieldLabel(FF.Description, t) }}</option>
                      </select>
                      <input v-model="tp.pattern" type="text" placeholder="regex pattern" class="flex-1 bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body font-mono placeholder-text-muted focus:outline-none focus:border-border-focus" />
                      <input v-model.number="tp.captureGroup" type="number" placeholder="grp" class="w-14 bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus" />
                    </template>
                    <button class="text-status-fail/60 hover:text-status-fail text-sm transition-colors" @click="rule.titleRules.splice(tIdx, 1)">×</button>
                  </div>
                  <button class="text-xs text-text-secondary hover:text-text-body transition-colors" @click="addTitleRule(rule)">{{ $t('builder.addTitlePart') }}</button>
                </div>

                <!-- Filter builder -->
                <div class="space-y-2">
                  <label class="block text-xs text-text-body mb-1 font-medium">{{ $t('builder.filtersLabel') }}</label>
                  <div v-for="section in (['all', 'any', 'not'] as const)" :key="section" class="space-y-1">
                    <div class="flex items-center justify-between">
                      <span class="text-sm font-semibold text-text-body">{{ groupLabel(section, t) }}</span>
                      <button class="text-xs text-text-secondary hover:text-text-body transition-colors" @click="addFilterCondition(rule, section)">{{ $t('builder.addFilter') }}</button>
                    </div>
                    <div v-if="rule.filters[section].length === 0 && section === 'all'" class="text-text-secondary text-xs pl-2">{{ $t('builder.noConditions') }}</div>
                    <div v-for="(cond, cIdx) in rule.filters[section]" :key="cIdx" class="flex items-center gap-1.5">
                      <select v-model.number="cond.field" class="bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body focus:outline-none focus:border-border-focus">
                        <option :value="FF.Title">{{ fieldLabel(FF.Title, t) }}</option>
                        <option :value="FF.Topic">{{ fieldLabel(FF.Topic, t) }}</option>
                        <option :value="FF.Channel">{{ fieldLabel(FF.Channel, t) }}</option>
                        <option :value="FF.Description">{{ fieldLabel(FF.Description, t) }}</option>
                        <option :value="FF.Duration">{{ fieldLabel(FF.Duration, t) }}</option>
                        <option :value="FF.Timestamp">{{ fieldLabel(FF.Timestamp, t) }}</option>
                      </select>
                      <select v-model.number="cond.op" class="bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body focus:outline-none focus:border-border-focus">
                        <option :value="FO.Eq">{{ opLabel(FO.Eq, t) }}</option>
                        <option :value="FO.Contains">{{ opLabel(FO.Contains, t) }}</option>
                        <option :value="FO.NotContains">{{ opLabel(FO.NotContains, t) }}</option>
                        <option :value="FO.GreaterThan">{{ opLabel(FO.GreaterThan, t) }}</option>
                        <option :value="FO.LessThan">{{ opLabel(FO.LessThan, t) }}</option>
                        <option :value="FO.Regex">{{ opLabel(FO.Regex, t) }}</option>
                      </select>
                      <input v-model="cond.value" type="text" placeholder="value" class="flex-1 bg-surface-elevated border border-border-default rounded-lg px-2 py-1.5 text-xs text-text-body placeholder-text-muted focus:outline-none focus:border-border-focus" />
                      <button class="text-status-fail/60 hover:text-status-fail text-xs transition-colors" @click="rule.filters[section].splice(cIdx, 1)">×</button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <div v-if="saveError" class="text-status-fail text-sm">{{ saveError }}</div>

        <!-- Save bar -->
        <div class="sticky bottom-0 bg-surface-base/95 backdrop-blur-sm border-t border-border-default py-3 flex items-center gap-3 z-10">
          <button
            class="px-3 py-1.5 bg-accent text-black font-medium rounded-md hover:bg-accent-dim text-sm transition-colors disabled:opacity-50"
            :disabled="saving"
            @click="handleSave"
          >
            {{ saving ? $t('builder.saving') : $t('builder.save') }}
          </button>
          <router-link
            :to="isEditMode ? `/rulesets/${editId}` : '/rulesets'"
            class="px-3 py-1.5 bg-surface-elevated text-text-body rounded-md hover:bg-surface-overlay text-sm transition-colors border border-border-default"
          >
            {{ $t('builder.cancelButton') }}
          </router-link>
        </div>
      </div>

      <!-- Right pane: Debugger -->
      <div class="sticky top-5 self-start">
        <div class="bg-surface-raised rounded-lg border border-border-default p-3">
          <LiveMatchPreview :topic="form.topic" :builder-state="debuggerState" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import {
  getRuleSetDetail, createRuleSet, updateRuleSet, ValidationFailedError,
  type RuleSetWriteRequest, type RuleSetWriteRule, type FilterConditionInput, type TitleRuleInput,
  type ValidationError,
} from '../api/rulesets'
import {
  Strategy, TitlePartType, FilterField, FilterOp, EnrichmentMethod, RuntimeMode,
} from '../api/enumMaps'
import { strategyLabel } from '../utils/strategy'
import { opLabel, groupLabel, fieldLabel, titlePartLabel } from '../utils/ruleVocabulary'
import LiveMatchPreview from '../components/LiveMatchPreview.vue'
import SkeletonCard from '../components/SkeletonCard.vue'
import AppBreadcrumb from '../components/AppBreadcrumb.vue'
import { useToast } from '../composables/useToast'

const FF = FilterField
const FO = FilterOp

const RUNTIME_METHOD = 2
const YEAR_METHOD = 3

const { t } = useI18n()
const { toast } = useToast()

interface FormFilterCondition {
  field: number
  op: number
  value: string
}

interface FormTitleRule {
  type: number
  field: number
  pattern: string
  captureGroup: number | null
  value: string
}

interface FormRule {
  id: string
  priority: number
  confidence: number | null
  strategy: number
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
      { label: t('rulesets.title'), to: '/rulesets' },
      { label: editId!, to: `/rulesets/${editId}` },
      { label: t('detail.edit') },
    ]
  }
  return [
    { label: t('rulesets.title'), to: '/rulesets' },
    { label: t('builder.newRuleSet') },
  ]
})

const loadingDetail = ref(false)
const loadError = ref<string | null>(null)
const saving = ref(false)
const saveError = ref<string | null>(null)
const validationErrors = ref<ValidationError[]>([])

interface FormEnrichment {
  enabled: boolean
  methods: number[]
  titleThreshold: number
  airdateTolerance: number
  runtimeTolerance: number
  runtimeMode: number
  yearTolerance: number
}

const form = reactive({
  ruleSetId: '',
  topic: '',
  aliases: [] as string[],
  mediaType: 'show',
  mediaName: '',
  tvdbId: null as number | null,
  imdbId: '',
  tmdbId: null as number | null,
  confidence: 0.8,
  standalone: false,
  disable: [] as string[],
  rules: [] as FormRule[],
  enrichment: {
    enabled: true,
    methods: [EnrichmentMethod.Title, EnrichmentMethod.Airdate],
    titleThreshold: 0.7,
    airdateTolerance: 7,
    runtimeTolerance: 0.35,
    runtimeMode: RuntimeMode.Tiebreaker,
    yearTolerance: 1,
  } as FormEnrichment,
})

const enrichmentMethodOptions = computed(() => [
  { value: EnrichmentMethod.Title, label: t('builder.methodTitle') },
  { value: EnrichmentMethod.Airdate, label: t('builder.methodAirdate') },
  { value: RUNTIME_METHOD, label: t('builder.methodRuntime') },
  { value: YEAR_METHOD, label: t('builder.methodYear') },
])

const mediaNameEdited = ref(false)
const isCommunitySource = ref(false)

watch(() => form.topic, (newTopic) => {
  if (!mediaNameEdited.value) {
    form.mediaName = newTopic
  }
})

const ruleSetIdError = computed(() => {
  if (!form.ruleSetId && !isEditMode.value) return ''
  if (form.ruleSetId && !/^[a-z0-9]+(-[a-z0-9]+)*$/.test(form.ruleSetId)) return t('builder.ruleSetIdKebabError')
  return ''
})

const enrichmentHint = computed(() => {
  if (!form.enrichment.enabled) return ''
  if (form.mediaType === 'show' && !form.tvdbId) return t('builder.enrichmentHintShow')
  if (form.mediaType === 'movie' && !form.tmdbId && !form.imdbId) return t('builder.enrichmentHintMovie')
  return ''
})

function toggleMethod(method: number) {
  const idx = form.enrichment.methods.indexOf(method)
  if (idx >= 0) {
    form.enrichment.methods.splice(idx, 1)
  } else {
    form.enrichment.methods.push(method)
  }
}

const debuggerState = computed(() => ({
  confidence: form.confidence,
  rules: form.rules,
  enrichment: form.enrichment,
  tvdbId: form.tvdbId,
  tmdbId: form.tmdbId,
  imdbId: form.imdbId,
  mediaType: form.mediaType,
}))

function createEmptyRule(): FormRule {
  return {
    id: `rule-${form.rules.length + 1}`,
    priority: form.rules.length * 10,
    confidence: null,
    strategy: -1,
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
  rule.titleRules.push({ type: TitlePartType.Regex, field: FilterField.Title, pattern: '', captureGroup: null, value: '' })
}

function addFilterCondition(rule: FormRule, section: 'all' | 'any' | 'not') {
  rule.filters[section].push({ field: FilterField.Title, op: FilterOp.Contains, value: '' })
}

function serializeForm(): RuleSetWriteRequest {
  const rules: RuleSetWriteRule[] = form.rules.map(r => {
    const filters: RuleSetWriteRule['filters'] = {} as any
    if (r.filters.all.length > 0) (filters as any).all = r.filters.all.map(c => ({ field: c.field, op: c.op, value: c.value } as FilterConditionInput))
    if (r.filters.any.length > 0) (filters as any).any = r.filters.any.map(c => ({ field: c.field, op: c.op, value: c.value } as FilterConditionInput))
    if (r.filters.not.length > 0) (filters as any).not = r.filters.not.map(c => ({ field: c.field, op: c.op, value: c.value } as FilterConditionInput))
    const hasFilters = r.filters.all.length > 0 || r.filters.any.length > 0 || r.filters.not.length > 0

    const titleRules: TitleRuleInput[] | undefined = r.titleRules.length > 0
      ? r.titleRules.map(tp => {
          const rule: TitleRuleInput = { type: tp.type }
          if (tp.type === TitlePartType.Regex) {
            rule.field = tp.field
            if (tp.pattern) rule.pattern = tp.pattern
            if (tp.captureGroup != null) rule.captureGroup = tp.captureGroup
          } else {
            if (tp.value) rule.value = tp.value
          }
          return rule
        })
      : undefined

    const rule: RuleSetWriteRule = {
      id: r.id,
      priority: r.priority,
      strategy: r.strategy,
    }
    if (r.confidence != null) rule.confidence = r.confidence
    if (r.seasonRegex) rule.seasonRegex = r.seasonRegex
    if (r.episodeRegex) rule.episodeRegex = r.episodeRegex
    if (r.captureGroup != null) rule.captureGroup = r.captureGroup
    if (hasFilters) rule.filters = filters
    if (titleRules) rule.titleRules = titleRules
    return rule
  })

  const media: RuleSetWriteRequest['media'] = {
    name: form.mediaName || form.topic,
    type: form.mediaType,
  }
  if (form.tvdbId) media.tvdbId = form.tvdbId
  if (form.imdbId) media.imdbId = form.imdbId
  if (form.tmdbId) media.tmdbId = form.tmdbId

  const enrichment: RuleSetWriteRequest['enrichment'] = {
    enabled: form.enrichment.enabled,
    methods: form.enrichment.methods,
    title: { threshold: form.enrichment.titleThreshold },
    airdate: { tolerance: form.enrichment.airdateTolerance },
    runtime: { tolerance: form.enrichment.runtimeTolerance, mode: form.enrichment.runtimeMode },
    year: { tolerance: form.enrichment.yearTolerance },
  }

  const result: RuleSetWriteRequest = {
    topic: form.topic,
    aliases: form.aliases.filter(a => a.trim() !== ''),
    media,
    confidence: form.confidence,
    rules,
    enrichment,
  }
  if (!isEditMode.value) result.ruleSetId = form.ruleSetId
  if (isEditMode.value && isCommunitySource.value) result.standalone = true
  if (form.disable.length > 0) result.disable = form.disable
  return result
}

async function handleSave() {
  saveError.value = null
  validationErrors.value = []

  if (!isEditMode.value && !form.ruleSetId) {
    saveError.value = t('builder.ruleSetIdRequired')
    return
  }
  if (ruleSetIdError.value) {
    saveError.value = ruleSetIdError.value
    return
  }
  if (!form.topic.trim()) {
    saveError.value = t('builder.topicRequired')
    return
  }

  saving.value = true
  try {
    const data = serializeForm()
    if (isEditMode.value) {
      await updateRuleSet(editId!, data)
      toast(t('builder.ruleSetSaved'))
      router.push(`/rulesets/${editId}`)
    } else {
      await createRuleSet(data)
      toast(t('builder.ruleSetSaved'))
      router.push(`/rulesets/${form.ruleSetId}`)
    }
  } catch (e) {
    if (e instanceof ValidationFailedError) {
      validationErrors.value = e.errors
      saveError.value = t('builder.validationErrorCount', { count: e.errors.length })
    } else {
      saveError.value = e instanceof Error ? e.message : t('builder.failedToSave')
    }
    toast(saveError.value!, 'error')
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  if (!editId) return

  loadingDetail.value = true
  try {
    const detail = await getRuleSetDetail(editId)
    form.ruleSetId = editId
    form.topic = detail.identity.topic || ''
    form.aliases = detail.identity.aliases ? [...detail.identity.aliases] : []
    form.mediaType = 'show'
    form.mediaName = detail.identity.topic || ''
    mediaNameEdited.value = false
    isCommunitySource.value = detail.source.communityPath != null
    form.tvdbId = detail.identity.tvdbId ?? null
    form.imdbId = detail.identity.imdbId ?? ''
    form.tmdbId = detail.identity.tmdbId ?? null
    form.confidence = detail.defaultConfidence ?? 0.8

    if (detail.enrichment) {
      form.enrichment.enabled = detail.enrichment.enabled
      form.enrichment.methods = [...detail.enrichment.methods]
      form.enrichment.titleThreshold = detail.enrichment.title.threshold
      form.enrichment.airdateTolerance = detail.enrichment.airdate.tolerance
      form.enrichment.runtimeTolerance = detail.enrichment.runtime.tolerance
      form.enrichment.runtimeMode = detail.enrichment.runtime.mode
      form.enrichment.yearTolerance = detail.enrichment.year.tolerance
    }

    form.rules = (detail.rules || []).map((r, idx) => ({
      id: r.id || `rule-${idx + 1}`,
      priority: r.priority ?? idx * 10,
      confidence: r.confidence ?? null,
      strategy: r.strategy,
      seasonRegex: r.seasonRegex ?? '',
      episodeRegex: r.episodeRegex ?? '',
      captureGroup: r.captureGroup ?? null,
      filters: {
        all: (r.filters?.all || []).map(f => ({ field: f.field, op: f.op, value: f.value ?? '' })),
        any: (r.filters?.any || []).map(f => ({ field: f.field, op: f.op, value: f.value ?? '' })),
        not: (r.filters?.not || []).map(f => ({ field: f.field, op: f.op, value: f.value ?? '' })),
      },
      titleRules: (r.titleRules || []).map(tr => ({
        type: tr.type,
        field: tr.field ?? FilterField.Title,
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

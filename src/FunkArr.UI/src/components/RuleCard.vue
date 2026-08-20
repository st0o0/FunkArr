<script setup lang="ts">
import { ref } from 'vue'

interface FilterGroup {
  all: FilterNode[]
  any: FilterNode[]
  not: FilterNode[]
}

type FilterNode = Filter | FilterGroup

interface Filter {
  field: string
  op: string
  value: string
}

interface TitleRule {
  type: string
  field?: string
  pattern?: string
  captureGroup?: number
  value?: string
}

interface Rule {
  priority: number
  filters: FilterGroup
  strategy: string
  confidence?: number
  seasonRegex?: string
  episodeRegex?: string
  captureGroup?: number
  titleRules: TitleRule[]
}

const props = defineProps<{
  rule: Rule
  index: number
}>()

const expanded = ref(true)

function isFilter(node: FilterNode): node is Filter {
  return 'field' in node && 'op' in node && 'value' in node
}

function hasFilters(group: FilterGroup): boolean {
  return group.all.length > 0 || group.any.length > 0 || group.not.length > 0
}
</script>

<template>
  <div class="border border-neutral-200 dark:border-neutral-700 rounded">
    <button
      class="w-full flex items-center justify-between p-4 text-left"
      @click="expanded = !expanded"
    >
      <span class="text-sm font-bold">
        #{{ props.index }} &middot; priority:{{ props.rule.priority }} &middot;
        <span class="font-mono">{{ props.rule.strategy }}</span>
      </span>
      <span class="text-xs text-neutral-400">{{ expanded ? '&#9650;' : '&#9660;' }}</span>
    </button>

    <div v-if="expanded" class="px-4 pb-4 space-y-3">
      <!-- Filters -->
      <div v-if="hasFilters(props.rule.filters)">
        <div class="text-xs font-medium text-neutral-500 mb-1">Filters</div>
        <template v-for="(groupName) in (['all', 'any', 'not'] as const)" :key="groupName">
          <div
            v-if="props.rule.filters[groupName].length > 0"
            class="ml-2 mb-1"
          >
            <span class="text-xs font-medium uppercase text-neutral-400">{{ groupName }}</span>
            <div
              v-for="(node, ni) in props.rule.filters[groupName]"
              :key="ni"
              class="ml-3 text-sm"
            >
              <template v-if="isFilter(node)">
                <span class="font-mono text-xs">
                  {{ (node as Filter).field }}
                  <span class="text-neutral-500">{{ (node as Filter).op }}</span>
                  {{ (node as Filter).value }}
                </span>
              </template>
              <template v-else>
                <span class="text-xs text-neutral-400">[nested group]</span>
              </template>
            </div>
          </div>
        </template>
      </div>

      <!-- Regex patterns -->
      <div v-if="props.rule.seasonRegex || props.rule.episodeRegex" class="space-y-1">
        <div class="text-xs font-medium text-neutral-500">Regex Patterns</div>
        <div v-if="props.rule.seasonRegex" class="text-sm">
          <span class="text-neutral-500">Season:</span>
          <code class="font-mono text-xs ml-1 bg-neutral-100 dark:bg-neutral-800 px-1 rounded">{{ props.rule.seasonRegex }}</code>
        </div>
        <div v-if="props.rule.episodeRegex" class="text-sm">
          <span class="text-neutral-500">Episode:</span>
          <code class="font-mono text-xs ml-1 bg-neutral-100 dark:bg-neutral-800 px-1 rounded">{{ props.rule.episodeRegex }}</code>
        </div>
        <div v-if="props.rule.captureGroup != null" class="text-xs text-neutral-500">
          Capture group: {{ props.rule.captureGroup }}
        </div>
      </div>

      <!-- Title Rules -->
      <div v-if="props.rule.titleRules.length > 0">
        <div class="text-xs font-medium text-neutral-500 mb-1">Title Rules</div>
        <div
          v-for="(tr, ti) in props.rule.titleRules"
          :key="ti"
          class="ml-2 text-sm"
        >
          <span class="text-xs font-medium text-neutral-400">{{ tr.type }}</span>
          <template v-if="tr.type === 'Regex'">
            <span class="ml-1 text-neutral-500">{{ tr.field }}:</span>
            <code class="font-mono text-xs ml-1 bg-neutral-100 dark:bg-neutral-800 px-1 rounded">{{ tr.pattern }}</code>
            <span v-if="tr.captureGroup != null" class="text-xs text-neutral-500 ml-1">
              group:{{ tr.captureGroup }}
            </span>
          </template>
          <template v-if="tr.type === 'Static'">
            <span class="ml-1 font-mono text-xs">{{ tr.value }}</span>
          </template>
        </div>
      </div>

      <!-- Confidence -->
      <div v-if="props.rule.confidence != null" class="text-xs text-neutral-500">
        Confidence: {{ Math.round(props.rule.confidence * 100) }}%
      </div>
    </div>
  </div>
</template>

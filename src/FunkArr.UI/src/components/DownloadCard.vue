<script setup lang="ts">
defineProps<{
  title: string
  status: string
  progressPercent?: number
  downloadedBytes?: number
  totalBytes?: number
  errorMessage?: string | null
  completedAt?: string | null
}>()

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${units[i]}`
}

const badgeClasses: Record<string, string> = {
  Queued: 'bg-neutral-100 text-neutral-700 dark:bg-neutral-700 dark:text-neutral-300',
  Downloading: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
  Muxing: 'bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300',
  Completed: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
  Failed: 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
}
</script>

<template>
  <div class="border border-neutral-200 rounded p-4 dark:border-neutral-700">
    <div class="flex items-center justify-between gap-4">
      <span class="font-bold text-sm truncate">{{ title }}</span>
      <span
        class="text-xs font-medium px-2 py-0.5 rounded-full shrink-0"
        :class="badgeClasses[status] ?? badgeClasses.Queued"
      >
        {{ status }}
      </span>
    </div>

    <div
      v-if="status === 'Downloading' && progressPercent != null"
      class="mt-2"
    >
      <div class="h-1.5 bg-neutral-200 rounded-full dark:bg-neutral-700">
        <div
          class="h-1.5 bg-blue-600 rounded-full transition-all"
          :style="{ width: `${progressPercent}%` }"
        />
      </div>
    </div>

    <div
      v-if="downloadedBytes != null && totalBytes != null"
      class="mt-2 font-mono text-sm text-neutral-500"
    >
      {{ formatBytes(downloadedBytes) }} / {{ formatBytes(totalBytes) }}
    </div>

    <div
      v-if="completedAt"
      class="mt-1 text-xs text-neutral-500"
    >
      {{ new Date(completedAt).toLocaleString() }}
    </div>

    <div
      v-if="status === 'Failed' && errorMessage"
      class="mt-2 text-sm text-red-600 dark:text-red-400"
    >
      {{ errorMessage }}
    </div>
  </div>
</template>

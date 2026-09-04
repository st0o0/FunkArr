<template>
  <Teleport to="body">
    <div class="fixed bottom-4 right-4 z-50 flex flex-col gap-2 pointer-events-none">
      <TransitionGroup name="toast">
        <div
          v-for="t in toasts"
          :key="t.id"
          class="pointer-events-auto flex items-center gap-2 px-4 py-2.5 rounded-lg shadow-lg text-sm font-medium backdrop-blur-sm"
          :class="variantClass(t.variant)"
        >
          <span class="shrink-0">{{ variantIcon(t.variant) }}</span>
          <span>{{ t.message }}</span>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { useToast, type ToastVariant } from '../composables/useToast'

const { toasts } = useToast()

function variantClass(variant: ToastVariant): string {
  switch (variant) {
    case 'success': return 'bg-status-ok/90 text-white'
    case 'error': return 'bg-status-fail/90 text-white'
    case 'info': return 'bg-brand-500/90 text-surface-base'
  }
}

function variantIcon(variant: ToastVariant): string {
  switch (variant) {
    case 'success': return '✓'
    case 'error': return '✗'
    case 'info': return 'ℹ'
  }
}
</script>

<style scoped>
.toast-enter-active {
  transition: all 200ms ease-out;
}
.toast-leave-active {
  transition: all 150ms ease-in;
}
.toast-enter-from {
  opacity: 0;
  transform: translateX(1rem);
}
.toast-leave-to {
  opacity: 0;
  transform: translateX(1rem);
}
</style>

<template>
  <Teleport to="body">
    <div class="fixed bottom-4 right-4 z-50 flex flex-col gap-2 pointer-events-none">
      <TransitionGroup name="toast">
        <div
          v-for="t in toasts"
          :key="t.id"
          class="pointer-events-auto flex items-center gap-2 px-3 py-2 rounded-md text-sm bg-surface-overlay border border-border-default"
          :class="variantClass(t.variant)"
        >
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
    case 'success': return 'text-status-ok'
    case 'error': return 'text-status-fail'
    case 'info': return 'text-text-body'
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

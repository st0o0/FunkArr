<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useData } from 'vitepress'

const props = withDefaults(defineProps<{
  view: string
  variant?: 'diagram' | 'sequence'
  height?: string
}>(), {
  variant: 'diagram',
  height: '520px'
})

const { isDark } = useData()
const ready = ref(false)

onMounted(async () => {
  if (!customElements.get('likec4-view')) {
    const { defineWebcomponent } = await import('likec4:webcomponents/funkarr')
    defineWebcomponent('likec4-view')
  }
  ready.value = true
})
</script>

<template>
  <div class="architecture-view" :style="{ height: props.height }">
    <likec4-view
      v-if="ready"
      :view-id="props.view"
      :dynamic-variant="props.variant"
      :color-scheme="isDark ? 'dark' : 'light'"
      browser="true"
    />
    <div v-else class="architecture-view__loading">Loading diagram…</div>
  </div>
</template>

<style scoped>
.architecture-view {
  margin: 16px 0;
  border: 1px solid var(--vp-c-divider);
  border-radius: 8px;
  overflow: hidden;
}

.architecture-view likec4-view {
  display: block;
  width: 100%;
  height: 100%;
}

.architecture-view__loading {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--vp-c-text-2);
  font-size: 14px;
}
</style>

<template>
  <AppLayout>
    <router-view v-slot="{ Component }">
      <Transition name="page" mode="out-in">
        <component :is="Component" :key="$route.path" />
      </Transition>
    </router-view>
  </AppLayout>
  <ToastContainer />
</template>

<script setup lang="ts">
import AppLayout from './components/AppLayout.vue'
import ToastContainer from './components/ToastContainer.vue'
</script>

<style>
.page-enter-active {
  transition: opacity 150ms ease, transform 150ms ease;
}
.page-leave-active {
  transition: opacity 100ms ease;
}
.page-enter-from {
  opacity: 0;
  transform: translateY(6px);
}
.page-leave-to {
  opacity: 0;
}
</style>

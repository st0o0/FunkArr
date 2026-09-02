<template>
  <div>
    <h1 class="text-xl font-bold text-text-primary mb-2">FunkArr</h1>
    <p class="text-text-secondary mb-6">German public broadcaster media library integration for the *arr ecosystem.</p>

    <HealthWidget class="mb-6" />

    <router-link to="/rulesets" class="inline-block px-4 py-2 bg-brand-600 text-white rounded-md hover:bg-brand-500 text-sm">
      View RuleSets
    </router-link>
  </div>
</template>

<script setup lang="ts">
import HealthWidget from '../components/HealthWidget.vue'
</script>

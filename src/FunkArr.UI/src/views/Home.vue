<template>
  <div>
    <h1 class="text-3xl font-bold mb-2">FunkArr</h1>
    <p class="text-gray-600 mb-6">German public broadcaster media library integration for the *arr ecosystem.</p>

    <HealthWidget class="mb-6" />

    <router-link to="/rulesets" class="inline-block px-4 py-2 bg-gray-900 text-white rounded hover:bg-gray-700 text-sm">
      View RuleSets
    </router-link>
  </div>
</template>

<script setup lang="ts">
import HealthWidget from '../components/HealthWidget.vue'
</script>

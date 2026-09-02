<template>
  <div class="min-h-screen flex flex-col bg-gray-50 text-gray-900">
    <header class="bg-gray-900 text-white px-6 py-3 flex items-center gap-6">
      <router-link to="/" class="text-xl font-bold tracking-tight hover:text-gray-300">FunkArr</router-link>
      <nav class="flex gap-4 text-sm">
        <router-link to="/" class="hover:text-gray-300" active-class="text-white font-semibold" exact-active-class="underline underline-offset-4">Dashboard</router-link>
        <router-link to="/setup" class="hover:text-gray-300" active-class="text-white font-semibold">Setup</router-link>
        <router-link to="/rulesets" class="hover:text-gray-300" active-class="text-white font-semibold">RuleSets</router-link>
      </nav>
    </header>
    <main class="flex-1 p-6 max-w-7xl mx-auto w-full">
      <slot />
    </main>
  </div>
</template>

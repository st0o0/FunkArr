import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'
import './style.css'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: () => import('./views/Home.vue') },
    { path: '/queue', component: () => import('./views/Queue.vue'), meta: { title: 'Downloads' } },
    { path: '/history', component: () => import('./views/History.vue'), meta: { title: 'History' } },
    { path: '/setup', component: () => import('./views/Setup.vue'), meta: { title: 'Setup' } },
    { path: '/rulesets', component: () => import('./views/RuleSetList.vue'), meta: { title: 'RuleSets' } },
    { path: '/rulesets/new', component: () => import('./views/RuleSetBuilder.vue'), meta: { title: 'New RuleSet' } },
    { path: '/rulesets/:id', component: () => import('./views/RuleSetDetail.vue'), meta: { title: 'RuleSet' } },
    { path: '/rulesets/:id/edit', component: () => import('./views/RuleSetBuilder.vue'), meta: { title: 'Edit RuleSet' } },
    { path: '/rulesets/:id/history', component: () => import('./views/ScoringHistory.vue'), meta: { title: 'Scoring History' } },
    { path: '/rulesets/:id/history/:requestId', component: () => import('./views/ScoringDetail.vue'), meta: { title: 'Scoring Detail' } },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
})

router.afterEach((to) => {
  const title = to.meta.title as string | undefined
  document.title = title ? `${title} - FunkArr` : 'FunkArr'
})

createApp(App).use(router).mount('#app')

import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'
import './style.css'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: () => import('./views/Home.vue') },
    { path: '/setup', component: () => import('./views/Setup.vue') },
    { path: '/rulesets', component: () => import('./views/RuleSetList.vue') },
    { path: '/rulesets/:id', component: () => import('./views/RuleSetDetail.vue') },
    { path: '/rulesets/:id/history', component: () => import('./views/ScoringHistory.vue') },
    { path: '/rulesets/:id/history/:requestId', component: () => import('./views/ScoringDetail.vue') },
  ],
})

createApp(App).use(router).mount('#app')

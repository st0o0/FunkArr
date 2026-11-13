import { fileURLToPath } from 'node:url'
import { LikeC4VitePlugin } from 'likec4/vite-plugin'
import { defineConfig } from 'vitepress'
import { applyHookIdFilters } from './likec4-vite5'

const repo = 'https://github.com/st0o0/funkarr'
const editPattern = `${repo}/edit/main/docs/:path`

export default defineConfig({
  base: '/',
  title: 'FunkArr',
  vite: {
    plugins: [
      applyHookIdFilters(LikeC4VitePlugin({
        workspace: fileURLToPath(new URL('../architecture', import.meta.url)),
        ai: 'disabled'
      }))
    ]
  },
  vue: {
    template: {
      compilerOptions: {
        isCustomElement: (tag) => tag.startsWith('likec4-')
      }
    }
  },
  head: [
    ['link', { rel: 'icon', href: '/logo.svg' }]
  ],
  locales: {
    root: {
      label: 'Deutsch',
      lang: 'de',
      description: 'Integration für deutsche öffentlich-rechtliche Mediatheken im *arr-Ökosystem',
      themeConfig: {
        nav: [
          { text: 'Anleitung', link: '/getting-started' },
          { text: 'Konfiguration', link: '/configuration' },
          { text: 'Regelwerke', link: '/rulesets/' },
          { text: 'Vergleich', link: '/comparison' },
          { text: 'GitHub', link: repo }
        ],
        sidebar: [
          {
            text: 'Anleitung',
            items: [
              { text: 'Erste Schritte', link: '/getting-started' },
              { text: 'Konfiguration', link: '/configuration' },
              { text: 'So funktioniert FunkArr', link: '/how-it-works' },
              { text: 'Web-Oberfläche', link: '/web-ui' },
              { text: 'Observability', link: '/observability' },
              { text: 'Fehlerbehebung', link: '/troubleshooting' },
              { text: 'Vergleich mit Alternativen', link: '/comparison' }
            ]
          },
          {
            text: 'Regelwerke',
            items: [
              { text: 'Übersicht', link: '/rulesets/' },
              { text: 'Community-Katalog', link: '/rulesets/catalog' },
              { text: 'Eigene Regelwerke', link: '/rulesets/custom' },
              { text: 'Feld-Referenz', link: '/rulesets/field-reference' },
              { text: 'Strategien', link: '/rulesets/strategies' },
              { text: 'Filter', link: '/rulesets/filters' },
              { text: 'Regelwerk-Builder', link: '/rulesets/builder' }
            ]
          },
          {
            text: 'Für Entwickler',
            collapsed: true,
            items: [
              { text: 'Mitwirken', link: '/develop/' },
              { text: 'Architektur', link: '/develop/architecture' },
              { text: 'Repository-Aufbau', link: '/develop/layout' },
              { text: 'Bauen und testen', link: '/develop/building' },
              { text: 'Aktoren', link: '/develop/actors' },
              { text: 'Abläufe', link: '/develop/flows' },
              { text: 'APIs', link: '/develop/apis' },
              { text: 'Web-Oberfläche', link: '/develop/web-ui' },
              { text: 'Lokale Entwicklung', link: '/develop/local-development' },
              { text: 'Entwicklungshilfen', link: '/develop/dev-aids' },
              { text: 'Regelwerke', link: '/develop/rulesets' },
              { text: 'Releases', link: '/develop/releasing' }
            ]
          }
        ],
        editLink: {
          pattern: editPattern,
          text: 'Diese Seite auf GitHub bearbeiten'
        },
        docFooter: { prev: 'Vorherige Seite', next: 'Nächste Seite' },
        outline: { label: 'Auf dieser Seite' },
      }
    },
    en: {
      label: 'English',
      lang: 'en',
      description: 'German public broadcaster Mediathek integration for the *arr ecosystem',
      themeConfig: {
        nav: [
          { text: 'Guide', link: '/en/getting-started' },
          { text: 'Config', link: '/en/configuration' },
          { text: 'Rulesets', link: '/en/rulesets/' },
          { text: 'Comparison', link: '/en/comparison' },
          { text: 'GitHub', link: repo }
        ],
        sidebar: [
          {
            text: 'Guide',
            items: [
              { text: 'Getting Started', link: '/en/getting-started' },
              { text: 'Configuration', link: '/en/configuration' },
              { text: 'How FunkArr Works', link: '/en/how-it-works' },
              { text: 'Web UI', link: '/en/web-ui' },
              { text: 'Observability', link: '/en/observability' },
              { text: 'Troubleshooting', link: '/en/troubleshooting' },
              { text: 'Comparison', link: '/en/comparison' }
            ]
          },
          {
            text: 'Rulesets',
            items: [
              { text: 'Overview', link: '/en/rulesets/' },
              { text: 'Community Catalog', link: '/en/rulesets/catalog' },
              { text: 'Custom Rulesets', link: '/en/rulesets/custom' },
              { text: 'Field Reference', link: '/en/rulesets/field-reference' },
              { text: 'Strategies', link: '/en/rulesets/strategies' },
              { text: 'Filters', link: '/en/rulesets/filters' },
              { text: 'Builder', link: '/en/rulesets/builder' }
            ]
          },
          {
            text: 'For developers',
            collapsed: true,
            items: [
              { text: 'Contributing', link: '/en/develop/' },
              { text: 'Architecture', link: '/en/develop/architecture' },
              { text: 'Repository layout', link: '/en/develop/layout' },
              { text: 'Building and testing', link: '/en/develop/building' },
              { text: 'Actors', link: '/en/develop/actors' },
              { text: 'Request flows', link: '/en/develop/flows' },
              { text: 'APIs', link: '/en/develop/apis' },
              { text: 'Web UI', link: '/en/develop/web-ui' },
              { text: 'Local development', link: '/en/develop/local-development' },
              { text: 'Development aids', link: '/en/develop/dev-aids' },
              { text: 'Rulesets', link: '/en/develop/rulesets' },
              { text: 'Releasing', link: '/en/develop/releasing' }
            ]
          }
        ],
        editLink: {
          pattern: editPattern,
          text: 'Edit this page on GitHub'
        },
      }
    }
  },
  themeConfig: {
    logo: '/logo.svg',
    socialLinks: [
      { icon: 'github', link: repo }
    ],
    search: {
      provider: 'local'
    }
  }
})

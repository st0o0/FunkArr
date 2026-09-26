import { defineConfig } from 'vitepress'

export default defineConfig({
  base: '/',
  title: 'FunkArr',
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
          { text: 'Vergleich', link: '/comparison' }
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
          }
        ],
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
          { text: 'Comparison', link: '/en/comparison' }
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
          }
        ],
      }
    }
  },
  themeConfig: {
    logo: '/logo.svg',
    socialLinks: [
      { icon: 'github', link: 'https://github.com/st0o0/funkarr' }
    ],
    search: {
      provider: 'local'
    }
  }
})

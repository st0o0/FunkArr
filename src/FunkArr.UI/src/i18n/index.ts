import { createI18n } from 'vue-i18n'
import en from './locales/en.json'
import de from './locales/de.json'
import deAT from './locales/de-AT.json'
import deCH from './locales/de-CH.json'

const STORAGE_KEY = 'funkarr-locale'
const SUPPORTED_LOCALES = ['en', 'de', 'de-AT', 'de-CH'] as const

function detectLocale(): string {
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored && (SUPPORTED_LOCALES as readonly string[]).includes(stored)) {
    return stored
  }

  const browserLang = navigator.language
  if (browserLang.startsWith('de-AT')) return 'de-AT'
  if (browserLang.startsWith('de-CH')) return 'de-CH'
  if (browserLang.startsWith('de')) return 'de'
  if (browserLang.startsWith('en')) return 'en'

  return 'de'
}

export const i18n = createI18n({
  legacy: false,
  locale: detectLocale(),
  fallbackLocale: {
    'de-AT': ['de', 'en'],
    'de-CH': ['de', 'en'],
    'de': ['en'],
    'default': ['en'],
  },
  messages: { en, de, 'de-AT': deAT, 'de-CH': deCH },
})

export function setLocale(locale: string) {
  if (!(SUPPORTED_LOCALES as readonly string[]).includes(locale)) return
  i18n.global.locale.value = locale as typeof SUPPORTED_LOCALES[number]
  localStorage.setItem(STORAGE_KEY, locale)
  document.documentElement.lang = locale
}

export { SUPPORTED_LOCALES }

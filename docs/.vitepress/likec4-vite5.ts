// LikeC4's Vite plugin declares hook filters (`load: { filter: { id }, handler }`), a Vite 6.3+
// feature. VitePress 1.x runs on Vite 5, which ignores `filter` and calls the handler for every
// module - in dev the icon loader then replaces VitePress's own app entry with an empty stub and
// the site renders blank. Apply the id filters ourselves. Remove once VitePress runs on Vite 6.3+.

type Hook = { filter?: { id?: unknown }, handler: (...args: unknown[]) => unknown, [key: string]: unknown }
type PluginLike = { [hook: string]: unknown }

const filteredHooks = ['resolveId', 'load', 'transform']

export function applyHookIdFilters<T>(option: T): T {
  if (Array.isArray(option)) {
    return option.map(applyHookIdFilters) as T
  }
  if (option instanceof Promise) {
    return option.then(applyHookIdFilters) as T
  }
  if (!option || typeof option !== 'object') {
    return option
  }

  const plugin = option as PluginLike
  for (const name of filteredHooks) {
    const hook = plugin[name] as Hook | undefined
    const pattern = hook?.filter?.id
    if (!hook || typeof hook !== 'object' || !(pattern instanceof RegExp)) {
      continue
    }

    const { filter: _, handler, ...rest } = hook
    plugin[name] = {
      ...rest,
      handler(this: unknown, id: string, ...args: unknown[]) {
        pattern.lastIndex = 0
        return pattern.test(id) ? handler.call(this, id, ...args) : null
      }
    }
  }
  return option
}

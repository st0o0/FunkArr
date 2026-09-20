import { ref } from 'vue'

const isSecureContext = window.isSecureContext && typeof navigator.clipboard?.writeText === 'function'

async function writeToClipboard(text: string): Promise<void> {
  if (isSecureContext) {
    await navigator.clipboard.writeText(text)
    return
  }

  const selection = document.getSelection()
  const range = document.createRange()
  const node = document.createElement('span')
  node.textContent = text
  node.style.whiteSpace = 'pre'
  node.style.position = 'absolute'
  node.style.clip = 'rect(0, 0, 0, 0)'
  document.body.appendChild(node)
  range.selectNodeContents(node)
  selection?.removeAllRanges()
  selection?.addRange(range)
  document.execCommand('copy')
  selection?.removeAllRanges()
  document.body.removeChild(node)
}

export function useClipboard() {
  const copied = ref<string | null>(null)
  let timer: ReturnType<typeof setTimeout> | undefined

  async function copy(text: string): Promise<boolean> {
    try {
      await writeToClipboard(text)
      copied.value = text
      clearTimeout(timer)
      timer = setTimeout(() => { copied.value = null }, 2000)
      return true
    } catch {
      return false
    }
  }

  function hasCopied(text: string): boolean {
    return copied.value === text
  }

  return { copy, copied, hasCopied }
}

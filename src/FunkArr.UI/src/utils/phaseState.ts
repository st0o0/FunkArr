export type DownloadState = 'active' | 'completed' | 'failed' | 'unknown'

export function isStepDone(key: number, state: DownloadState, currentPhase: number): boolean {
  if (state === 'completed') return true
  return state === 'active' && key < currentPhase
}

export function isStepFailed(key: number, state: DownloadState): boolean {
  return state === 'failed' && key === 0
}

export function isStepCurrent(key: number, state: DownloadState, currentPhase: number): boolean {
  return state === 'active' && key === currentPhase
}

export function stepClass(key: number, state: DownloadState, currentPhase: number): string {
  if (isStepDone(key, state, currentPhase)) return 'bg-green-500/20 text-green-400'
  if (isStepFailed(key, state)) return 'bg-red-500/20 text-red-400'
  if (isStepCurrent(key, state, currentPhase)) return 'bg-accent text-white'
  return 'bg-surface-elevated text-text-secondary'
}

export function stepLabelClass(key: number, state: DownloadState, currentPhase: number): string {
  if (isStepDone(key, state, currentPhase)) return 'text-green-400'
  if (isStepFailed(key, state)) return 'text-red-400'
  if (isStepCurrent(key, state, currentPhase)) return 'text-accent font-medium'
  return 'text-text-secondary'
}

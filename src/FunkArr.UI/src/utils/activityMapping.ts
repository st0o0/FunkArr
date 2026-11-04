import type { QueueItem, HistoryItem } from '../api/downloads'
import { QueueStatus, QueuePriority, HistoryStatus } from '../api/enums'
import { formatSize } from './format'

export type RowType = 'active' | 'queued' | 'completed' | 'failed'

export interface TableRow {
  id: string
  position: number | null
  title: string
  channel: string
  type: RowType
  statusLabel: string
  statusDot: string
  phaseLabel: string
  percentage: number
  speed: number
  priorityLabel: string
  priority: number
  size: string
  failMessage: string | null
  isQueue: boolean
}

const phaseNames = ['Downloading', 'Remuxing', 'Moving'] as const

export function priorityLabel(p: number, t: (key: string) => string): string {
  if (p === QueuePriority.High) return t('queue.priorityHigh')
  if (p === QueuePriority.Low) return t('queue.priorityLow')
  return t('queue.priorityNormal')
}

export function queueItemToRow(item: QueueItem, idx: number, t: (key: string) => string): TableRow {
  const isActive = item.status === QueueStatus.Processing
  return {
    id: item.downloadId,
    position: idx + 1,
    title: item.title,
    channel: item.channel,
    type: isActive ? 'active' : 'queued',
    statusLabel: isActive ? (phaseNames[item.phase] ?? 'Processing') : t('queue.queuedStatus'),
    statusDot: isActive ? 'bg-accent' : 'bg-text-muted',
    phaseLabel: phaseNames[item.phase] ?? 'Processing',
    percentage: item.percentage,
    speed: item.speed,
    priorityLabel: priorityLabel(item.priority, t),
    priority: item.priority,
    size: item.totalBytes > 0 ? formatSize(item.totalBytes) : '-',
    failMessage: null,
    isQueue: true,
  }
}

export function historyItemToRow(item: HistoryItem): TableRow {
  const isFailed = item.status === HistoryStatus.Failed
  return {
    id: item.downloadId,
    position: null,
    title: item.title,
    channel: '',
    type: isFailed ? 'failed' : 'completed',
    statusLabel: isFailed ? 'Failed' : 'Completed',
    statusDot: isFailed ? 'bg-status-fail' : 'bg-status-ok',
    phaseLabel: '',
    percentage: 0,
    speed: 0,
    priorityLabel: '',
    priority: 0,
    size: item.totalBytes > 0 ? formatSize(item.totalBytes) : '-',
    failMessage: item.failMessage,
    isQueue: false,
  }
}

export const QueueStatus = { Processing: 0, Queued: 1 } as const
export const HistoryStatus = { Completed: 0, Failed: 1 } as const
export const SourceType = { Community: 0, Local: 1, Merged: 2 } as const
export const MediaType = { Show: 0, Movie: 1 } as const
export const CheckStatus = { Ok: 0, Warn: 1, Fail: 2 } as const
export const MediathekVariant = { Standard: 0, AudioDescription: 1, SignLanguage: 2, PlainLanguage: 3, OriginalVersion: 4 } as const
export const VariantPolicy = { Exclude: 0, Allow: 1 } as const

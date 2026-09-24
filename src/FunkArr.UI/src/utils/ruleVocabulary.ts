import { FilterOp, filterFieldName, filterOpName, titlePartTypeName } from '../api/enumMaps'

export function opSymbol(op: number): string {
  switch (op) {
    case FilterOp.Eq: return '='
    case FilterOp.Contains: return '∋'
    case FilterOp.NotContains: return '∌'
    case FilterOp.GreaterThan: return '>'
    case FilterOp.LessThan: return '<'
    case FilterOp.Regex: return '≈'
    default: return String(op)
  }
}

export function opLabel(op: number, t: (key: string) => string): string {
  const s = filterOpName(op)
  const key = `rule.op.${s}`
  const result = t(key)
  return result !== key ? result : s
}

export function groupLabel(group: string, t: (key: string) => string): string {
  const key = `rule.group.${group}`
  const result = t(key)
  return result !== key ? result : group
}

export function fieldLabel(field: number, t: (key: string) => string): string {
  const s = filterFieldName(field)
  const key = `rule.field.${s}`
  const result = t(key)
  return result !== key ? result : s
}

export function titlePartLabel(type: number, t: (key: string) => string): string {
  const s = titlePartTypeName(type)
  const key = `rule.titlePart.${s}`
  const result = t(key)
  return result !== key ? result : s
}

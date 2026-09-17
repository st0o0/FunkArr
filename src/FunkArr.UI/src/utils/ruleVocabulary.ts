export function opSymbol(op: string): string {
  switch (op) {
    case 'eq': return '='
    case 'contains': return '∋'
    case 'notContains': return '∌'
    case 'greaterThan': return '>'
    case 'lessThan': return '<'
    case 'regex': return '≈'
    default: return op
  }
}

export function opLabel(op: string, t: (key: string) => string): string {
  const key = `rule.op.${op}`
  const result = t(key)
  return result !== key ? result : op
}

export function groupLabel(group: string, t: (key: string) => string): string {
  const key = `rule.group.${group}`
  const result = t(key)
  return result !== key ? result : group
}

export function fieldLabel(field: string, t: (key: string) => string): string {
  const key = `rule.field.${field}`
  const result = t(key)
  return result !== key ? result : field
}

export function titlePartLabel(type: string, t: (key: string) => string): string {
  const key = `rule.titlePart.${type}`
  const result = t(key)
  return result !== key ? result : type
}

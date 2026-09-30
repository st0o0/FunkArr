## Context

Collapsed rule cards currently show `rule-id | Strategy Badge | prio N`. When multiple rules share the same strategy (common for title-exact rulesets like Bibi Blocksberg with 4 identical-looking collapsed headers), there's no way to tell them apart without expanding.

## Goals / Non-Goals

**Goals:**
- Collapsed headers hint at rule content (filter/title-rule counts)
- Button label is intuitive in German
- Identity + Source merge into a more compact layout

**Non-Goals:**
- Changing the expanded rule content (already improved in previous changes)
- Adding new data to the detail API response

# UdonSharp Project Guardrails

This file contains project-specific guardrails for VRChat UdonSharp development.

## Guardrails Addendum v3 — Canonical Sync Attribute (DO NOT BREAK)

**Canonical form (mandatory in all UdonSharp scripts):**
```csharp
[UdonSharp.UdonBehaviourSyncMode(VRC.Udon.Common.Enums.BehaviourSyncMode.Manual)]
```

**Never use:**
- `VRC.Udon.Common.Interfaces.BehaviourSyncMode.*`
- Unqualified `BehaviourSyncMode.*`
- Bare `[UdonBehaviourSyncMode(...)]` without `UdonSharp.` prefix

**Pre-commit validations (reject the change if any match):**

**Regex ban (wrong namespace):**
```
VRC\.Udon\.Common\.Interfaces\.BehaviourSyncMode
```

**Regex ban (unqualified attribute):**
```
\[UdonBehaviourSyncMode\s*\(BehaviourSyncMode\.
```

**Regex allow-only (must match at least one):**
```
\[UdonSharp\.UdonBehaviourSyncMode\s*\(VRC\.Udon\.Common\.Enums\.BehaviourSyncMode\.Manual\)\]
```

**Rationale:** Unity compile errors CS0234 arise if `Interfaces` is used. UdonSharp requires a fully qualified enum under `VRC.Udon.Common.Enums` and the `UdonSharp.` attribute prefix to ensure consistent codegen.

---
name: doc-sync
description: Keep documentation in sync with code changes. Use when making changes that affect documented facts.
---

# Documentation Sync

This skill covers keeping documentation synchronized with code changes.

## Core Rule

> **Documentation must stay in sync with code changes. This is NOT optional.**

When you change code that affects documented facts, update ALL relevant documentation in the same PR.

---

## What Triggers Doc Updates

| Change Type | Documentation to Update |
|-------------|------------------------|
| Version changes | All docs with version refs, setup scripts, MCP tools |
| Architecture changes | architecture.md, repository-map.md, repo-info.ts |
| New features | project-overview.md, relevant guides |
| Config changes | quick-start.md, setup scripts |
| API changes | API docs, DTOs, endpoint documentation |
| New patterns | AGENTS.md, relevant skills |

---

## Documentation Locations

### Primary Docs

| File | Purpose |
|------|---------|
| `AGENTS.md` | Per-repo instructions for AI agents |
| `README.md` | Project overview and quick start |
| `mcp-server/docs/` | MCP server documentation |
| `docs/` | Project-specific guides |

### MCP Server Docs

```
mcp-server/docs/
├── architecture.md        # System architecture
├── project-overview.md    # High-level project info
├── repository-map.md      # Repo structure
├── statistical-targets.md # NFL targets
└── interactive/
    └── quick-start.md     # Setup guide
```

### Setup Scripts

```
project-root/
├── setup.sh               # Linux/Mac setup
├── setup.ps1              # Windows PowerShell setup
└── LOCAL_DEVELOPMENT_SETUP.md  # Manual setup guide
```

---

## Sync Workflow

### Step 1: Identify Affected Docs

After making code changes, search for affected documentation:

```bash
# Search for old version numbers
grep -r "old-version" --include="*.md" --include="*.ts"

# Search for renamed components
grep -r "OldName" --include="*.md"

# Search for changed paths
grep -r "old/path" --include="*.md"
```

### Step 2: Update Documentation

Update each affected file:

1. **Version changes**: Search and replace all occurrences
2. **Architecture changes**: Update diagrams and descriptions
3. **New features**: Add to relevant docs
4. **Removed features**: Remove from docs (don't leave stale references)

### Step 3: Update MCP Tool Outputs

If your change affects what MCP tools return:

```typescript
// mcp-server/src/tools/repo-info.ts
// mcp-server/src/tools/tech-stack.ts
```

Update these to return accurate information.

### Step 4: Verify Sync

```bash
# Check for stale references
grep -r "TODO\|FIXME\|outdated\|deprecated" --include="*.md"

# Verify version consistency
grep -r "\.NET [0-9]" --include="*.md" | sort -u
grep -r "Node [0-9]" --include="*.md" | sort -u
```

---

## Common Sync Scenarios

### Version Upgrade

When upgrading .NET, Node, or package versions:

```bash
# Find all version references
grep -rn "\.NET 9" --include="*.md" --include="*.json" --include="*.ts"

# Replace with new version
# Then verify no old references remain
grep -rn "\.NET 9" --include="*.md" --include="*.json" --include="*.ts"
```

Files typically affected:
- `README.md`
- `AGENTS.md`
- `setup.sh`, `setup.ps1`
- `mcp-server/docs/architecture.md`
- `mcp-server/src/tools/tech-stack.ts`

### New Entity/Feature

When adding a new domain entity:

1. Update architecture docs if it's significant
2. Add API documentation for endpoints
3. Update setup docs if new config needed
4. Add to AGENTS.md if special handling required

### Renamed Component

When renaming a class, service, or file:

```bash
# Find all references to old name
grep -rn "OldServiceName" --include="*.md"

# Update each occurrence
# Verify no old references remain
```

---

## Validation Script

Run before completing any PR:

```bash
#!/bin/bash
# validate-docs.sh

echo "Checking for potential doc issues..."

# Check for TODO/FIXME in docs
echo "=== Incomplete docs ==="
grep -rn "TODO\|FIXME" --include="*.md" docs/ mcp-server/docs/ || echo "None found"

# Check for version consistency
echo ""
echo "=== .NET versions mentioned ==="
grep -rhn "\.NET [0-9]" --include="*.md" | sort -u

echo ""
echo "=== Node versions mentioned ==="
grep -rhn "Node.*[0-9]" --include="*.md" | sort -u

echo ""
echo "=== Package versions in docs ==="
grep -rhn "Version.*[0-9]" --include="*.md" | head -20
```

---

## Checklist for Changes

- [ ] Searched for affected documentation
- [ ] Updated all version references
- [ ] Updated architecture docs if structure changed
- [ ] Updated MCP tool outputs if they return changed info
- [ ] Updated setup scripts if requirements changed
- [ ] Verified no stale references remain
- [ ] Included doc updates in same PR as code changes

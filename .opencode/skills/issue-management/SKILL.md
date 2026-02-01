# Issue Management Skill

Skill for creating and managing GitHub issues across multi-repo projects using a hierarchical structure.

## Issue Hierarchy

```
Central Project (meta repo)
└── INITIATIVE (label: initiative, title: "Initiative: ...")
    └── EPIC (label: epic, title: "Epic: ...", sub-issue of INITIATIVE)

Repo-Specific Projects
└── Issue (sub-issue of EPIC, in repo's own project ONLY)
    └── PR (mentions "Closes owner/repo#N" in body)
```

---

## Core Principles

1. **INITIATIVES & EPICS in central repo only** - The central project should ONLY contain Initiative and Epic issues. No other issue types.

2. **Label AND title prefix required** - INITIATIVES must have label `initiative` AND title starting with "Initiative: ". EPICS must have label `epic` AND title starting with "Epic: ".

3. **Sub-issues via GraphQL API** - Child issues MUST be attached using GitHub's sub-issue API. Never use markdown task lists or body mentions as substitutes.

4. **One project per repo** - Each issue belongs ONLY to its repository's project. Issues must NOT appear in multiple projects.

5. **Every issue has a parent EPIC** - All regular issues (features, bugs, tasks) must be sub-issues of an EPIC.

6. **Full references required** - ALWAYS use `owner/repo#number` syntax, never bare `#number`.

7. **PRs close issues via body** - PR bodies must include `Closes owner/repo#N` for auto-close on merge.

8. **Branch naming ties to issues** - Branches must follow `{type}/{issue-number}-short-description` pattern.

---

## Branch Naming Convention

All branches must reference their issue number for traceability:

```
{type}/{issue-number}-short-description
```

| Type | Use |
|------|-----|
| `feature/` | New functionality |
| `fix/` | Bug fixes |
| `chore/` | Maintenance, refactoring, docs |

**Examples:**
```bash
feature/265-auth-api-endpoints
fix/142-login-redirect-loop
chore/300-update-dependencies
```

---

## Label Standardization

### Required Labels (all repos)

| Label | Color | Use |
|-------|-------|-----|
| `initiative` | `#6B21A8` (purple) | Top-level goals (meta only) |
| `epic` | `#7C3AED` (violet) | Deliverable chunks (meta only) |
| `bug` | `#DC2626` (red) | Something broken |
| `feature` | `#2563EB` (blue) | New functionality |
| `chore` | `#6B7280` (gray) | Maintenance, refactoring |
| `blocked` | `#F59E0B` (amber) | Waiting on something |
| `priority:high` | `#EF4444` (red) | Needs attention soon |
| `priority:low` | `#10B981` (green) | Can wait |

### Create Labels Script

```bash
# Run in each repo to standardize labels
for repo in your-repos; do
  gh label create bug --repo your-org/$repo --color DC2626 --description "Something broken" --force
  gh label create feature --repo your-org/$repo --color 2563EB --description "New functionality" --force
  gh label create chore --repo your-org/$repo --color 6B7280 --description "Maintenance, refactoring" --force
  gh label create blocked --repo your-org/$repo --color F59E0B --description "Waiting on something" --force
  gh label create "priority:high" --repo your-org/$repo --color EF4444 --description "Needs attention soon" --force
  gh label create "priority:low" --repo your-org/$repo --color 10B981 --description "Can wait" --force
done

# Meta repo gets initiative and epic labels
gh label create initiative --repo bobum/hexgame --color 6B21A8 --description "Top-level goal" --force
gh label create epic --repo bobum/hexgame --color 7C3AED --description "Deliverable chunk under initiative" --force
```

---

## INITIATIVE & EPIC Lifecycle

### When to Close

| Type | Close When |
|------|------------|
| **INITIATIVE** | All child EPICs are closed AND goals achieved |
| **EPIC** | All child issues are closed AND feature complete |

### Closure Checklist

Before closing an EPIC:
- [ ] All sub-issues closed
- [ ] Feature tested and working
- [ ] No open PRs referencing this EPIC
- [ ] Documentation updated if needed

Before closing an INITIATIVE:
- [ ] All child EPICs closed
- [ ] Success criteria from Initiative body met
- [ ] Stakeholder sign-off (if applicable)

---

## Critical: Issue vs PR References

> **GitHub uses the same `#` numbering for BOTH issues AND pull requests.**

### Always Use Full References

```markdown
# WRONG - ambiguous, might link to a PR with the same number
Part of Epic #50

# RIGHT - explicit and verifiable
Part of Epic bobum/hexgame#50
```

---

## Initiative Creation Workflow

```bash
# 1. Create Initiative
gh issue create \
  --repo bobum/hexgame \
  --title "Initiative: [Major Goal Name]" \
  --label "initiative" \
  --body "## Vision
[High-level description]

## Success Criteria
- [ ] Criterion 1
- [ ] Criterion 2"
```

## Epic Creation Workflow

```bash
# 1. Create Epic
gh issue create \
  --repo bobum/hexgame \
  --title "Epic: [Feature Name]" \
  --label "epic" \
  --body "## Overview
[Description]

## Scope
- [ ] Component 1
- [ ] Component 2"

# 2. Attach Epic to Parent Initiative
EPIC_ID=$(gh api repos/bobum/hexgame/issues/{epic_number} --jq '.id')
gh api repos/bobum/hexgame/issues/{initiative_number}/sub_issues \
  -X POST \
  -F sub_issue_id=$EPIC_ID
```

## Sub-Issue Creation Workflow

```bash
# 1. Create sub-issue in appropriate repo
gh issue create \
  --repo bobum/hexgame \
  --title "[Feature]: API endpoints for X" \
  --body "Part of Epic bobum/hexgame#XX"

# 2. Attach to Epic
SUB_ISSUE_ID=$(gh api repos/bobum/hexgame/issues/{number} --jq '.id')
gh api repos/bobum/hexgame/issues/{epic_number}/sub_issues \
  -X POST \
  -F sub_issue_id=$SUB_ISSUE_ID

# 3. Add to repo's project
gh project item-add {project_number} --owner your-org \
  --url https://github.com/bobum/hexgame/issues/{number}
```

---

## PR Workflow

PRs must be linked to issues so GitHub auto-closes them on merge.

### PR Body Template

```markdown
## Summary
- What was done

## Test Plan
- How to verify

Closes your-org/{repo}#{issue_number}
```

### Keywords That Auto-Close

- `Closes your-org/repo#123`
- `Fixes your-org/repo#123`
- `Resolves your-org/repo#123`

**Always use full reference** (`your-org/repo#N`), never bare `#N`.

---

## Hard Rules

1. **NEVER create non-Initiative/Epic issues in meta repo** - This repo is ONLY for Initiatives and Epics.

2. **NEVER use markdown task lists as sub-issues** - Always use the sub-issue API for proper tracking.

3. **NEVER put issues in multiple projects** - Each issue appears ONLY in its repo's project.

4. **ALWAYS use label AND title prefix** for Initiatives and Epics.

5. **ALWAYS attach sub-issues via API** - Sub-issues should appear in both:
   - The parent's progress tracking (via sub-issue API)
   - Their repository's project board (via project item-add)

6. **ALWAYS link PRs to issues** - PR body must include `Closes owner/repo#N`.

7. **EVERY regular issue must have a parent EPIC** - No orphan issues.

---

## Common Mistakes to Avoid

| Mistake | Why It's Wrong | Correct Approach |
|---------|----------------|------------------|
| Using bare `#123` | Ambiguous - might link to a PR | Use `owner/repo#123` |
| Using `- [ ] #123` in Epic body | Not a real sub-issue | Use sub-issue API |
| Mentioning "Part of Epic #123" in body only | Creates reference, not parent-child | Use sub-issue API |
| Missing label OR title prefix | Can't filter/identify issue type | Must have BOTH |
| PR without Closes reference | Issue stays open after merge | Add `Closes owner/repo#N` |
| Issue in multiple projects | Confuses tracking | Each issue in ONLY its repo's project |

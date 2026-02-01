---
name: pr-workflow
description: Standardized PR creation workflow with proper issue linking. Use when creating pull requests across any any repo.
---

# Pull Request Workflow

This skill covers the complete workflow for creating PRs with proper issue references.

## Critical: Issue Linking

> **NEVER use bare `#123` references.** GitHub uses the same numbering for issues AND PRs, so `#123` could link to a closed PR instead of the intended issue.

### Correct Syntax

```markdown
# WRONG - ambiguous, might link to a PR
Closes #123

# RIGHT - explicit repo, verifiable
Closes bobum/hexgame#123

# RIGHT - cross-repo reference
Closes bobum/hexgame-web#45
Closes bobum/hexgame-meta#12
```

### Verification Before Linking

**ALWAYS verify the reference is an issue (not a PR) before including it:**

```bash
# Verify it's an issue and check its state
gh issue view 123 --repo bobum/hexgame --json number,title,state,url

# If this fails with "not found" or shows a PR, DO NOT use it
# PRs are accessed via: gh pr view 123 --repo ...

# Get the full URL for the PR body
gh issue view 123 --repo bobum/hexgame --json url --jq '.url'
```

### What to Check

1. **It's an issue, not a PR** - `gh issue view` succeeds
2. **It's open** - `state` is `OPEN`, not `CLOSED`
3. **It's in the correct repo** - matches the work being done
4. **It's the right issue** - title matches the work

---

## PR Creation Steps

### Step 1: Verify Branch State

```bash
# Check you're on the correct feature branch
git branch --show-current

# Verify commits are ready
git log --oneline -5

# Check nothing uncommitted
git status
```

### Step 2: Push Branch

```bash
# Push with upstream tracking
git push -u origin feature/your-branch-name
```

### Step 3: Verify Issue Reference

```bash
# Check the issue exists and is open
gh issue view 123 --repo bobum/hexgame --json number,title,state

# Expected output:
# {
#   "number": 123,
#   "title": "Add user profile page",
#   "state": "OPEN"
# }

# If you see "state": "CLOSED" or the command fails, DO NOT reference this issue
```

### Step 4: Create PR

```bash
gh pr create \
  --repo bobum/hexgame \
  --base main \
  --title "Add user profile page" \
  --body "$(cat <<'EOF'
## Summary

- Implemented profile page with user stats
- Added avatar upload functionality
- Created responsive layout for mobile

## Test Plan

- [ ] Verify profile loads with user data
- [ ] Test avatar upload with various file sizes
- [ ] Check mobile layout on different viewports

Closes bobum/hexgame#123
EOF
)"
```

---

## PR Body Template

```markdown
## Summary

- [Bullet point describing main change]
- [Additional changes]

## Test Plan

- [ ] [How to verify the change works]
- [ ] [Additional test scenarios]

Closes your-org/{repo}#{issue_number}
```

---

## Cross-Repo PRs

When a PR in one repo relates to issues in another:

```bash
# Verify each issue first
gh issue view 50 --repo bobum/hexgame-meta --json title,state
gh issue view 265 --repo bobum/hexgame --json title,state

# In PR body, use full references
Part of Epic bobum/hexgame-meta#50
Closes bobum/hexgame#265
```

---

## Linking Keywords

| Keyword | Effect |
|---------|--------|
| `Closes` | Closes the issue when PR merges |
| `Fixes` | Same as Closes |
| `Resolves` | Same as Closes |
| `Part of` | Links without closing (for sub-issues of epics) |
| `Related to` | Links without closing |

**Always use `Closes` for the primary issue being addressed.**

---

## Common Mistakes

| Mistake | Problem | Fix |
|---------|---------|-----|
| `Closes #123` | Ambiguous - might link to PR #123 | Use `your-org/repo#123` |
| Not verifying first | Links to wrong item | Run `gh issue view` first |
| Linking to closed issue | Confusing, no effect | Check `state` field |
| Wrong repo | Links to wrong project | Verify repo in `gh issue view` |
| Linking to PR instead of issue | Confusing, won't auto-close | Use `gh issue view` not `gh pr view` |

---

## Repository Reference Guide

| Repo | Full Reference |
|------|----------------|
| API backend | `bobum/hexgame#N` |
| Frontend | `bobum/hexgame-web#N` |
| Simulation engine | `bobum/hexgame-engine#N` |
| Injury engine | `bobum/hexgame-injury#N` |
| Meta/Epics | `bobum/hexgame-meta#N` |

---

## Verification Script

Run this before creating any PR with issue references:

```bash
#!/bin/bash
# verify-issue.sh REPO ISSUE_NUMBER

REPO=$1
ISSUE=$2

echo "Checking your-org/$REPO#$ISSUE..."

result=$(gh issue view $ISSUE --repo your-org/$REPO --json number,title,state,url 2>&1)

if echo "$result" | grep -q "Could not resolve"; then
    echo "ERROR: Issue #$ISSUE not found in $REPO"
    echo "This might be a PR number, not an issue."
    exit 1
fi

state=$(echo "$result" | jq -r '.state')
title=$(echo "$result" | jq -r '.title')
url=$(echo "$result" | jq -r '.url')

if [ "$state" != "OPEN" ]; then
    echo "WARNING: Issue is $state, not OPEN"
fi

echo "Found: $title"
echo "State: $state"
echo "URL: $url"
echo ""
echo "Use in PR body: Closes your-org/$REPO#$ISSUE"
```

---

## Checklist Before Creating PR

- [ ] On correct feature branch
- [ ] All changes committed
- [ ] Branch pushed to origin
- [ ] Issue verified with `gh issue view`
- [ ] Issue is OPEN (not closed)
- [ ] Issue is in the correct repo
- [ ] Using full `owner/repo#number` syntax
- [ ] PR title is clear and concise
- [ ] PR body includes Summary, Test Plan, and issue reference

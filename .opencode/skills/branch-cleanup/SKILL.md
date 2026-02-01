---
name: branch-cleanup
description: Post-merge cleanup workflow for branches. Use after PRs are merged to clean up local and remote branches.
---

# Branch Cleanup Workflow

This skill covers post-merge cleanup for feature branches.

## When to Use

Run this workflow when:
- A PR has been merged
- The user says "merged", "PR merged", or "{number} merged"
- After completing a feature that was merged

---

## Single Repo Cleanup

### Step 1: Switch to Main

```bash
cd /path/to/repo
git checkout main
git pull origin main
```

### Step 2: Delete Local Branch

```bash
# Delete the merged feature branch
git branch -d feature/your-branch-name

# If the branch has unmerged changes (force delete)
git branch -D feature/your-branch-name
```

### Step 3: Delete Remote Branch (if still exists)

```bash
# Check if remote branch exists
git branch -r | grep feature/your-branch-name

# Delete if it exists
git push origin --delete feature/your-branch-name
```

### Step 4: Prune Stale Remote References

```bash
# Remove stale remote-tracking branches
git fetch --prune
```

---

## Full Cleanup Command

One-liner for complete cleanup:

```bash
git checkout main && git pull && git branch -d feature/your-branch-name && git push origin --delete feature/your-branch-name 2>/dev/null; git fetch --prune
```

---

## Multi-Repo Cleanup

For cross-repo features, clean up all affected repos:

```bash
# Clean up each repo (customize repo list and path)
for repo in repo-api repo-engine repo-web; do
    echo "=== Cleaning $repo ==="
    cd /path/to/monorepo/$repo

    # Switch to main and pull
    git checkout main
    git pull

    # Delete local branch if it exists
    git branch -d feature/your-feature 2>/dev/null

    # Delete remote branch if it exists
    git push origin --delete feature/your-feature 2>/dev/null

    # Prune
    git fetch --prune

    cd ..
done
```

---

## Verify Cleanup

```bash
# List remaining local branches
git branch

# List remaining remote branches
git branch -r

# Should only see main/master and origin/main
```

---

## Handling Errors

### "branch not found"

Branch already deleted - safe to ignore.

### "branch not fully merged"

Use `-D` (force delete) if you're sure the PR was merged:

```bash
git branch -D feature/your-branch-name
```

### "remote ref does not exist"

Remote branch already deleted (GitHub auto-deletes on merge) - safe to ignore.

---

## Automated Cleanup Script

```bash
#!/bin/bash
# cleanup-merged.sh BRANCH_NAME

BRANCH=$1

if [ -z "$BRANCH" ]; then
    echo "Usage: cleanup-merged.sh BRANCH_NAME"
    exit 1
fi

echo "Cleaning up branch: $BRANCH"

# Switch to main
git checkout main || exit 1
git pull || exit 1

# Delete local
if git branch | grep -q "$BRANCH"; then
    git branch -d "$BRANCH" && echo "Deleted local branch"
else
    echo "Local branch already gone"
fi

# Delete remote
if git branch -r | grep -q "origin/$BRANCH"; then
    git push origin --delete "$BRANCH" && echo "Deleted remote branch"
else
    echo "Remote branch already gone"
fi

# Prune
git fetch --prune

echo "Cleanup complete"
```

---

## Batch Cleanup of Merged Branches

To clean up ALL merged branches at once:

```bash
# List merged branches (excluding main/master)
git branch --merged | grep -v "main\|master\|\*"

# Delete all merged local branches
git branch --merged | grep -v "main\|master\|\*" | xargs -r git branch -d

# Delete all merged remote branches
git branch -r --merged | grep -v "main\|master\|HEAD" | sed 's/origin\///' | xargs -r -I {} git push origin --delete {}
```

---

## Checklist After Merge

- [ ] Switched to main/master
- [ ] Pulled latest changes
- [ ] Deleted local feature branch
- [ ] Deleted remote feature branch (if exists)
- [ ] Ran `git fetch --prune`
- [ ] Verified with `git branch` and `git branch -r`

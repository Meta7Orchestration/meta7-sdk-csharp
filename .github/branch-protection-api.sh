#!/bin/bash
# ══════════════════════════════════════════════════════════════
# META7 — Branch Protection Setup via GitHub API
# รัน: bash .github/branch-protection-api.sh
# ต้องการ: GitHub Token ที่มีสิทธิ์ admin:repo
# ══════════════════════════════════════════════════════════════

set -e

echo "╔══════════════════════════════════════════════════════════╗"
echo "║   META7 — Branch Protection Setup                       ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo ""

read -p "GitHub Username: " GH_USER
read -p "GitHub Token (ghp_...): " GH_TOKEN
read -p "Repo name [meta7-sdk-csharp]: " GH_REPO
GH_REPO="${GH_REPO:-meta7-sdk-csharp}"

API="https://api.github.com/repos/$GH_USER/$GH_REPO"
AUTH="Authorization: token $GH_TOKEN"

echo ""
echo "🔒 Setting up Branch Protection Rules..."

# ── main branch ──────────────────────────────────────────────
echo ""
echo "  📌 Protecting 'main' branch..."

curl -s -X PUT "$API/branches/main/protection" \
  -H "$AUTH" \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  -d '{
    "required_status_checks": {
      "strict": true,
      "contexts": ["build-and-test"]
    },
    "enforce_admins": true,
    "required_pull_request_reviews": {
      "required_approving_review_count": 1,
      "dismiss_stale_reviews": true,
      "require_code_owner_reviews": false
    },
    "restrictions": null,
    "required_conversation_resolution": true,
    "allow_force_pushes": false,
    "allow_deletions": false
  }' | python3 -c "
import sys, json
data = json.load(sys.stdin)
if 'url' in data:
    print('  ✅ main branch protected')
elif 'message' in data:
    print(f'  ❌ Error: {data[\"message\"]}')
else:
    print('  ⚠️  Unexpected response')
"

# ── develop branch ───────────────────────────────────────────
echo ""
echo "  📌 Protecting 'develop' branch..."

# First create develop branch if it doesn't exist
MAIN_SHA=$(curl -s "$API/git/refs/heads/main" \
  -H "$AUTH" \
  -H "Accept: application/vnd.github+json" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(data.get('object', {}).get('sha', ''))
" 2>/dev/null)

if [ -n "$MAIN_SHA" ]; then
  # Create develop branch from main
  curl -s -X POST "$API/git/refs" \
    -H "$AUTH" \
    -H "Accept: application/vnd.github+json" \
    -d "{\"ref\": \"refs/heads/develop\", \"sha\": \"$MAIN_SHA\"}" \
    > /dev/null 2>&1 || true
  echo "  ✅ develop branch created (or already exists)"
fi

curl -s -X PUT "$API/branches/develop/protection" \
  -H "$AUTH" \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  -d '{
    "required_status_checks": {
      "strict": true,
      "contexts": ["build-and-test"]
    },
    "enforce_admins": false,
    "required_pull_request_reviews": {
      "required_approving_review_count": 0,
      "dismiss_stale_reviews": false
    },
    "restrictions": null,
    "required_conversation_resolution": false,
    "allow_force_pushes": false,
    "allow_deletions": false
  }' | python3 -c "
import sys, json
data = json.load(sys.stdin)
if 'url' in data:
    print('  ✅ develop branch protected')
elif 'message' in data:
    print(f'  ❌ Error: {data[\"message\"]}')
else:
    print('  ⚠️  Unexpected response')
"

echo ""
echo "╔══════════════════════════════════════════════════════════╗"
echo "║   ✅ Branch Protection Setup Complete!                   ║"
echo "╠══════════════════════════════════════════════════════════╣"
echo "║   main:    PR required + 1 reviewer + CI must pass      ║"
echo "║   develop: PR required + CI must pass                   ║"
echo "╠══════════════════════════════════════════════════════════╣"
echo "║   Verify: https://github.com/$GH_USER/$GH_REPO/settings/branches"
echo "╚══════════════════════════════════════════════════════════╝"
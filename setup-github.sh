#!/bin/bash
# ══════════════════════════════════════════════════════════════
# META7 Captain M7A SDK — GitHub Setup Script
# รัน: bash setup-github.sh
# ══════════════════════════════════════════════════════════════

set -e

echo "╔══════════════════════════════════════════════════════════╗"
echo "║   META7 Captain M7A SDK — GitHub Setup                  ║"
echo "║   จดจำไว้ แล้วไปด้วยกัน                                ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo ""

# ── ถามข้อมูลจากผู้ใช้ ──────────────────────────────────────
read -p "GitHub Username: " GH_USER
read -p "GitHub Token (ghp_...): " GH_TOKEN
read -p "GitHub Repo name [meta7-sdk-csharp]: " GH_REPO
GH_REPO="${GH_REPO:-meta7-sdk-csharp}"

echo ""
echo "📋 ข้อมูลที่จะใช้:"
echo "   User: $GH_USER"
echo "   Repo: $GH_REPO"
echo "   URL:  https://github.com/$GH_USER/$GH_REPO"
echo ""
read -p "ยืนยัน? (y/n): " CONFIRM
if [[ "$CONFIRM" != "y" && "$CONFIRM" != "Y" ]]; then
    echo "ยกเลิก"
    exit 0
fi

# ── Git Init ─────────────────────────────────────────────────
echo ""
echo "🔧 ตั้งค่า Git..."

git init 2>/dev/null || true
git config user.name "$GH_USER"
git config user.email "meta7@hopecplus.com"

# ── Remote ───────────────────────────────────────────────────
REMOTE_URL="https://${GH_TOKEN}@github.com/${GH_USER}/${GH_REPO}.git"

if git remote get-url origin &>/dev/null; then
    git remote set-url origin "$REMOTE_URL"
    echo "✅ Updated remote origin"
else
    git remote add origin "$REMOTE_URL"
    echo "✅ Added remote origin"
fi

# ── Stage & Commit ───────────────────────────────────────────
echo ""
echo "📦 Staging files..."
git add .

# Check if there's anything to commit
if git diff --cached --quiet; then
    echo "ℹ️  Nothing new to commit"
else
    git commit -m "🛡️ META7 Captain M7A SDK v2.0

- 15 Layers: Core Types → Command Simulation
- Canonical Event Contract + Meaning Stone
- 20 Demos — all passing
- CI/CD Pipeline (GitHub Actions)
- จดจำไว้ แล้วไปด้วยกัน"
    echo "✅ Committed"
fi

# ── Push ─────────────────────────────────────────────────────
echo ""
echo "🚀 Pushing to GitHub..."

# Rename branch to main if needed
CURRENT_BRANCH=$(git branch --show-current 2>/dev/null || echo "master")
if [[ "$CURRENT_BRANCH" == "master" ]]; then
    git branch -M main
fi

git push -u origin main --force-with-lease 2>&1 || \
git push -u origin main --force 2>&1

echo ""
echo "╔══════════════════════════════════════════════════════════╗"
echo "║   ✅ SUCCESS!                                            ║"
echo "╠══════════════════════════════════════════════════════════╣"
echo "║   Repo: https://github.com/$GH_USER/$GH_REPO"
echo "║   Actions: https://github.com/$GH_USER/$GH_REPO/actions"
echo "╠══════════════════════════════════════════════════════════╣"
echo "║   ⚠️  ขั้นตอนถัดไป:                                     ║"
echo "║   1. ไปที่ repo → Settings → Secrets → Actions          ║"
echo "║   2. เพิ่ม FTP_HOST = web3.vpsthai.net                  ║"
echo "║   3. เพิ่ม FTP_USERNAME = kjyxwyac                      ║"
echo "║   4. เพิ่ม FTP_PASSWORD = (รหัสผ่านใหม่)               ║"
echo "╚══════════════════════════════════════════════════════════╝"
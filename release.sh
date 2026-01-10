#!/bin/bash

# 自动发布新版本脚本
# 用法: ./release.sh [version]  例如: ./release.sh 1.0.30

set -e

cd "$(dirname "$0")"

# 获取当前版本号
CURRENT_VERSION=$(grep '"version"' version.json | cut -d'"' -f4)
echo "当前版本: $CURRENT_VERSION"

# 如果没有指定版本号，自动递增 patch 版本
if [ -z "$1" ]; then
    # 提取版本号并递增最后一位
    MAJOR=$(echo $CURRENT_VERSION | cut -d. -f1)
    MINOR=$(echo $CURRENT_VERSION | cut -d. -f2)
    PATCH=$(echo $CURRENT_VERSION | cut -d. -f3)
    NEW_PATCH=$((PATCH + 1))
    NEW_VERSION="$MAJOR.$MINOR.$NEW_PATCH"
else
    NEW_VERSION="$1"
fi

echo "新版本: $NEW_VERSION-preview"

# 更新版本号
sed -i '' "s/\"version\": \"$CURRENT_VERSION\"/\"version\": \"$NEW_VERSION\"/" version.json

# 提交
git add version.json
git commit -m "🔖 bump: 版本 v$NEW_VERSION"
git push

# 创建标签
git tag "v$NEW_VERSION-preview"
git push origin "v$NEW_VERSION-preview"

echo "✅ 发布完成: v$NEW_VERSION-preview"

#!/bin/bash
# 版本同步脚本 - 从 Git 标签更新 version.json

set -e

# 获取当前标签，去掉 v 前缀
TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "")

if [ -z "$TAG" ]; then
    echo "No git tag found, skipping version update"
    exit 0
fi

# 去掉 v 前缀
VERSION=${TAG#v}

# 提取主版本号（去掉 -preview 等后缀用于 version.json 中的 version 字段）
MAIN_VERSION=$(echo "$VERSION" | sed 's/[-+].*//')

# 检查是否安装了 jq
if command -v jq &> /dev/null; then
    # 使用 jq 更新 version.json
    jq ".version = \"$MAIN_VERSION\"" version.json > version.json.tmp
    mv version.json.tmp version.json
    echo "✓ Updated version.json to $MAIN_VERSION (from tag $TAG)"
else
    # jq 未安装，使用 sed
    sed -i '' "s/\"version\": \"[^\"]*\"/\"version\": \"$MAIN_VERSION\"/" version.json
    echo "✓ Updated version.json to $MAIN_VERSION (from tag $TAG) [using sed]"
fi

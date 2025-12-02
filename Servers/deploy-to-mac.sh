#!/bin/bash

# Mac 서버 배포 스크립트
# 사용법: ./deploy-to-mac.sh <mac-user> <mac-ip> <target-path> [intel|arm]

echo "========================================"
echo "  Interplanetary Server - Mac Deploy"
echo "========================================"
echo ""

# 인자 확인
if [ $# -lt 3 ]; then
    echo "Usage: $0 <mac-user> <mac-ip> <target-path> [intel|arm]"
    echo "Example: $0 admin 192.168.1.100 /home/admin/server arm"
    exit 1
fi

MAC_USER=$1
MAC_IP=$2
TARGET_PATH=$3
ARCH=${4:-arm}  # 기본값: arm (Apple Silicon)

if [ "$ARCH" != "intel" ] && [ "$ARCH" != "arm" ]; then
    echo "Error: Architecture must be 'intel' or 'arm'"
    exit 1
fi

# 빌드 디렉토리 설정
if [ "$ARCH" == "intel" ]; then
    BUILD_DIR="./BaseServer/publish/osx-x64"
else
    BUILD_DIR="./BaseServer/publish/osx-arm64"
fi

# 빌드 디렉토리 확인
if [ ! -d "$BUILD_DIR" ]; then
    echo "Error: Build directory not found: $BUILD_DIR"
    echo "Please run build-mac.bat first"
    exit 1
fi

echo "Deploying to: $MAC_USER@$MAC_IP:$TARGET_PATH"
echo "Architecture: $ARCH"
echo "Source: $BUILD_DIR"
echo ""

# Mac 서버에 디렉토리 생성
echo "[1/4] Creating target directory on Mac server..."
ssh $MAC_USER@$MAC_IP "mkdir -p $TARGET_PATH"

# 파일 전송
echo "[2/4] Transferring files..."
scp -r $BUILD_DIR/* $MAC_USER@$MAC_IP:$TARGET_PATH/

# 실행 권한 부여
echo "[3/4] Setting execute permission..."
ssh $MAC_USER@$MAC_IP "chmod +x $TARGET_PATH/BaseServer"

# appsettings.json 확인
echo "[4/4] Checking appsettings.json..."
# ssh $MAC_USER@$MAC_IP "if [ ! -f $TARGET_PATH/appsettings.json ]; then cp $TARGET_PATH/appsettings.example.json $TARGET_PATH/appsettings.json; echo 'Created appsettings.json from example'; else echo 'appsettings.json already exists'; fi"

echo ""
echo "========================================"
echo "  Deployment Completed!"
echo "========================================"
echo ""
echo "Next steps:"
echo "1. SSH to Mac: ssh $MAC_USER@$MAC_IP"
echo "2. Edit config: nano $TARGET_PATH/appsettings.json"
echo "3. Run server: cd $TARGET_PATH && ./BaseServer"
echo ""

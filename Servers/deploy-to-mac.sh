#!/bin/bash

################################################################################
#
#  Intel Mac 서버 배포 스크립트
#
#  기능:
#    1. .NET 프로젝트 빌드 (osx-x64)
#    2. 빌드 결과물을 인텔 맥 서버로 전송
#    3. 기존 인스턴스 종료
#    4. 새 터미널 윈도우에서 서버 실행
#
#  사용법:
#    ./deploy-to-mac.sh <mac-user> <mac-ip> <target-path> [--no-build]
#
#  예시:
#    ./deploy-to-mac.sh boseong 125.137.73.37 /opt/interplanetary-server
#    ./deploy-to-mac.sh admin 192.168.1.100 /home/admin/server --no-build
#
################################################################################

set -e

# 색상 정의
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 로깅 함수
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_ok() {
    echo -e "${GREEN}[OK]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 헤더 출력
print_header() {
    echo ""
    echo "================================================================================"
    echo "  Interplanetary Server - Intel Mac Deployment"
    echo "================================================================================"
    echo ""
}

# 에러 핸들링
error_exit() {
    log_error "$1"
    exit 1
}

# 정리 함수 (필요시 사용)
cleanup() {
    # SSH 연결 정리 (필요하면 추가)
    true
}

trap cleanup EXIT

# ============================================================================
# 1. 인자 검증
# ============================================================================

print_header

if [ $# -lt 3 ]; then
    log_error "Not enough arguments"
    echo ""
    echo "Usage: $0 <mac-user> <mac-ip> <target-path> [--no-build]"
    echo ""
    echo "Arguments:"
    echo "  <mac-user>       Mac server username (e.g., boseong)"
    echo "  <mac-ip>         Mac server IP or hostname (e.g., 125.137.73.37)"
    echo "  <target-path>    Deployment path on Mac (e.g., /opt/interplanetary-server)"
    echo "  --no-build       Skip build step (optional)"
    echo ""
    exit 1
fi

MAC_USER=$1
MAC_IP=$2
TARGET_PATH=$3
SKIP_BUILD=false

if [ "$4" == "--no-build" ]; then
    SKIP_BUILD=true
fi

# 스크립트 디렉토리 (이 스크립트가 위치한 디렉토리)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# 빌드 및 배포 경로
PROJECT_FILE="BaseServer/BaseServer.csproj"
BUILD_OUTPUT="BaseServer/bin/Release/net8.0/osx-x64/publish"
CONFIG_FILE="BaseServer/appsettings.json"

# SSH 설정 (간단한 비밀번호 인증 사용)
SSH_OPTS="-o StrictHostKeyChecking=accept-new -o PubkeyAuthentication=no -o PasswordAuthentication=yes"

# 설정 출력
log_info "Configuration:"
echo "  Working Directory: $SCRIPT_DIR"
echo "  Project: $PROJECT_FILE"
echo "  Build Output: $BUILD_OUTPUT"
echo "  Mac Server: $MAC_USER@$MAC_IP"
echo "  Target Path: $TARGET_PATH"
echo "  Skip Build: $SKIP_BUILD"
echo ""

# ============================================================================
# 2. 빌드 (--no-build 옵션이 없을 경우)
# ============================================================================

if [ "$SKIP_BUILD" = false ]; then
    log_info "Starting .NET build process..."
    echo ""

    if [ ! -f "$PROJECT_FILE" ]; then
        error_exit "Project file not found: $PROJECT_FILE"
    fi

    # 빌드 폴더 경로
    BUILD_DIR="$(dirname "$BUILD_OUTPUT")"

    # 빌드 폴더 생성 (없으면)
    if [ ! -d "$BUILD_DIR" ]; then
        log_info "Creating build directory: $BUILD_DIR"
        mkdir -p "$BUILD_DIR"
    fi

    # 기존 빌드 출력 정리
    if [ -d "$BUILD_OUTPUT" ]; then
        log_info "Cleaning previous build output..."
        rm -rf "$BUILD_OUTPUT"
    fi

    # 빌드 실행
    echo "[1/3] Building project for Intel Mac (osx-x64)..."
    if dotnet publish "$PROJECT_FILE" \
        -c Release \
        -r osx-x64 \
        --self-contained \
        -p:DebugType=none \
        -p:DebugSymbols=false \
        -p:TrimMode=partial; then
        log_ok "Build completed successfully"
    else
        error_exit "Build failed - check .NET SDK and project configuration"
    fi
    echo ""
else
    log_warn "Skipping build step (--no-build flag set)"
    echo ""
fi

# ============================================================================
# 3. 빌드 결과 검증
# ============================================================================

log_info "Validating build output..."
if [ ! -d "$BUILD_OUTPUT" ]; then
    error_exit "Build output directory not found: $BUILD_OUTPUT"
fi

if [ ! -f "$BUILD_OUTPUT/BaseServer" ]; then
    error_exit "Executable not found: $BUILD_OUTPUT/BaseServer"
fi

# 빌드된 파일 목록 확인
FILE_COUNT=$(find "$BUILD_OUTPUT" -type f | wc -l)
log_ok "Build output validated ($FILE_COUNT files)"
echo ""

# ============================================================================
# 4. SSH 연결 테스트
# ============================================================================

log_info "Testing SSH connection to $MAC_USER@$MAC_IP..."
echo ""

# SSH 연결 테스트
if ssh $SSH_OPTS $MAC_USER@$MAC_IP "echo 'SSH connection OK'" > /dev/null 2>&1; then
    log_ok "SSH connection successful"
else
    error_exit "SSH connection failed (check username, password, or network)"
fi
echo ""

# ============================================================================
# 5. 맥 서버에 배포
# ============================================================================

log_info "Deploying to Mac server..."
echo ""

# SSH 명령어 (비밀번호 인증 사용)
SSH_CMD="ssh $SSH_OPTS"

# 5.1 디렉토리 생성 및 확인
echo "[1/5] Creating/verifying target directory..."
if $SSH_CMD $MAC_USER@$MAC_IP "
    if [ -d '$TARGET_PATH' ]; then
        echo 'Target directory already exists'
        exit 0
    else
        echo 'Creating new target directory'
        mkdir -p '$TARGET_PATH' && chmod 755 '$TARGET_PATH'
    fi
" > /dev/null 2>&1; then
    log_ok "Target directory is ready"
else
    error_exit "Failed to create/verify directory"
fi
echo ""

# 5.2 기존 서버 프로세스 종료
echo "[2/5] Stopping existing server process..."
$SSH_CMD $MAC_USER@$MAC_IP "pkill -f 'BaseServer' || true; sleep 1" > /dev/null 2>&1
log_ok "Old process terminated"
echo ""

# 5.3 파일 전송
echo "[3/5] Transferring files..."

if command -v rsync &> /dev/null; then
    # rsync 사용 (빠르고 효율적)
    log_info "Using rsync for file transfer..."
    rsync -avz --progress \
        -e "ssh $SSH_OPTS" \
        --exclude='appsettings.json' \
        --exclude='appsettings.*.json' \
        --exclude='*.log' \
        --exclude='nohup.out' \
        --delete \
        "$BUILD_OUTPUT/" "$MAC_USER@$MAC_IP:$TARGET_PATH/" || error_exit "rsync transfer failed"
else
    # tar + SSH 사용 (rsync가 없을 경우)
    log_info "rsync not found, using tar+ssh..."
    tar --exclude='appsettings.json' \
        --exclude='appsettings.*.json' \
        --exclude='*.log' \
        --exclude='nohup.out' \
        -czf - -C "$(dirname "$BUILD_OUTPUT")" "$(basename "$BUILD_OUTPUT")" | \
    $SSH_CMD $MAC_USER@$MAC_IP "tar -xzf - -C '$TARGET_PATH' --strip-components=1" || error_exit "tar transfer failed"
fi

log_ok "Files transferred successfully"
echo ""

# 5.4 권한 설정
echo "[4/5] Setting executable permissions..."
$SSH_CMD $MAC_USER@$MAC_IP "
    chmod +x '$TARGET_PATH/BaseServer'
    find '$TARGET_PATH' -type f \( -name '*.so' -o -name '*.dylib' \) -exec chmod +x {} \;
    chmod 755 '$TARGET_PATH'
" > /dev/null 2>&1 || error_exit "Failed to set permissions"

log_ok "Permissions set"
echo ""

# 5.5 설정 파일 처리
echo "[5/5] Configuring application..."
if [ -f "$CONFIG_FILE" ]; then
    log_info "Uploading local appsettings.json..."
    scp $SSH_OPTS "$CONFIG_FILE" "$MAC_USER@$MAC_IP:$TARGET_PATH/" > /dev/null 2>&1 || log_warn "Failed to upload appsettings.json"
else
    log_warn "Local appsettings.json not found"
fi

$SSH_CMD $MAC_USER@$MAC_IP "
    if [ ! -f '$TARGET_PATH/appsettings.json' ]; then
        if [ -f '$TARGET_PATH/appsettings.example.json' ]; then
            cp '$TARGET_PATH/appsettings.example.json' '$TARGET_PATH/appsettings.json'
        fi
    fi
" > /dev/null 2>&1

log_ok "Configuration processed"
echo ""


# ============================================================================
# 7. 완료 메시지
# ============================================================================

echo "================================================================================"
echo "  ✓ Build & Deployment Completed Successfully!"
echo "================================================================================"
echo ""
echo "Deployment Information:"
echo "  Server: $MAC_USER@$MAC_IP"
echo "  Location: $TARGET_PATH"
echo "  Executable: $TARGET_PATH/BaseServer"
echo ""
echo "To run the server manually:"
echo "  ssh $MAC_USER@$MAC_IP"
echo "  cd $TARGET_PATH"
echo "  ./BaseServer"
echo ""
echo "================================================================================"
echo ""

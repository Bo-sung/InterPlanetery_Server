# Interplanetary Online 서버 문서

## 📑 문서 구조

```
Docs/
├── README.md                          # 📍 이 문서 (네비게이션)
├── interplanetary_system_spec.md     # ⭐ 메인 기획서 (시작점)
├── Guides/                            # 📚 개발 가이드 모음
└── Archives/                          # 📦 참고 문서 보관소
```

---

## ⭐ 시작하기

### 메인 기획서
**👉 [interplanetary_system_spec.md](interplanetary_system_spec.md)**

Interplanetary 게임 서버의 전체 시스템 설계 문서입니다.

**포함 내용:**
- 📋 시스템 아키텍처
- 🎮 게임 로직 설계 (Planet, Fleet, Player)
- 🗂️ 데이터 구조
- 🌐 네트워크 프로토콜
- 🛠️ CLI 테스트 환경
- 📅 구현 로드맵 (Phase 1~4)

---

## 📚 개발 가이드

개발 시 참고할 가이드 문서들입니다.

| 문서 | 설명 |
|------|------|
| [CodingStyle.md](Guides/CodingStyle.md) | C# 코딩 컨벤션 및 스타일 가이드 |
| [ProtocolSpecification.md](Guides/ProtocolSpecification.md) | 네트워크 프로토콜 명세 |
| [ProtocolUsageGuide.md](Guides/ProtocolUsageGuide.md) | Protocol 클래스 사용법 |
| [Graph 자료구조 사용 가이드.md](Guides/Graph%20자료구조%20사용%20가이드.md) | 행성 연결 그래프 자료구조 가이드 |
| [ChatClientWPF - MVP 패턴 채팅 클라이언트.md](Guides/ChatClientWPF%20-%20MVP%20패턴%20채팅%20클라이언트.md) | WPF 클라이언트 아키텍처 |
| [ServerArchitecture.md](Guides/ServerArchitecture.md) | 기존 채팅 서버 아키텍처 (참고용) |

---

## 📦 보관 문서 (Archives)

과거 버전 또는 참고용 문서들입니다.

| 문서 | 설명 |
|------|------|
| [INTEGRATED_DESIGN_DOCUMENT.md](Archives/INTEGRATED_DESIGN_DOCUMENT.md) | 이전 통합 설계 문서 (Phase별 상세 구현 포함) |

---

## 🎯 개발 흐름

### 1단계: 문서 읽기
```
interplanetary_system_spec.md 읽기
    ↓
Phase별 목표 파악
    ↓
데이터 구조 및 프로토콜 이해
```

### 2단계: 개발 준비
```
Guides/CodingStyle.md 숙지
    ↓
Protocol 클래스 사용법 학습
    ↓
개발 환경 세팅
```

### 3단계: 구현
```
Phase 1: CLI 게임 로직 (4주)
    ↓
Phase 2: 네트워크 레이어 (3주)
    ↓
Phase 3: Unity 클라이언트 연동 (4주)
    ↓
Phase 4: 멀티플레이어 확장 (진행 중)
```

---

## 🔧 기술 스택

- **언어**: C# (.NET 8.0)
- **통신**: TCP, WebSocket
- **데이터베이스**: MySQL
- **테스트**: CLI 콘솔 애플리케이션
- **프로토콜**: 커스텀 바이너리 + JSON

---

## 📌 빠른 참조

### 자주 찾는 섹션

| 항목 | 위치 |
|------|------|
| 게임 엔티티 정의 | [interplanetary_system_spec.md](interplanetary_system_spec.md#31-핵심-엔티티-정의) |
| 프로토콜 명세 | [Guides/ProtocolSpecification.md](Guides/ProtocolSpecification.md) |
| 코딩 컨벤션 | [Guides/CodingStyle.md](Guides/CodingStyle.md) |
| 데이터베이스 설계 | [interplanetary_system_spec.md](interplanetary_system_spec.md#3-데이터-구조-설계) |
| 로드맵 | [interplanetary_system_spec.md](interplanetary_system_spec.md#7-구현-로드맵) |

---

## 📝 문서 업데이트 이력

| 날짜 | 변경 내용 |
|------|----------|
| 2025-10-15 | 문서 구조 재정리, README 작성 |
| 2025-10-14 | 통합 설계 문서 작성 |
| 2025-10-13 | interplanetary_system_spec.md 업데이트 |

---

## 💡 도움말

### 문서를 찾을 수 없나요?
- **메인 기획서**: `interplanetary_system_spec.md`
- **가이드**: `Guides/` 폴더
- **보관 문서**: `Archives/` 폴더

### 문서가 최신 버전인지 확인하려면?
```bash
git log --oneline Docs/
```

### 새 문서를 추가하려면?
1. 적절한 폴더 선택 (루트, Guides, Archives)
2. 문서 작성
3. 이 README에 링크 추가

---

**프로젝트**: InterplanetaryOnline Game Server
**팀**: Team Interplanetary
**최종 업데이트**: 2025-10-15

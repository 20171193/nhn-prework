# develop.md — 개발 환경 정보

> **이 문서는 실제 코드 작업(엔진 버전, 저장소, 브랜치, 폴더 구조, 빌드 방법)에 대한 정보입니다.**
> 게임 설계/기획 관련 내용은 `projectinfo.md`, 대회 개요는 `contestinfo.md`를 참고하세요.
> 클로드 코드는 이 파일을 `CLAUDE.md`에서 `@develop.md`로 불러옵니다. 클로드 앱/제미나이에는 이 파일을 그대로 프로젝트 지식 베이스에 업로드하세요.
> 최종 수정: 2026.08.01

---

## 0. 문서 관리 정책
- **이 문서는 팀 공통 규약입니다. 두 개발자 모두 임의로 혼자 수정하지 않기로 합니다.**
- 내용을 바꿔야 할 일이 생기면 반드시 서로 얘기해서 합의한 뒤에만 수정합니다.
- `CLAUDE.md`는 이 문서와 반대로 **의도적으로 버전관리 대상에서 제외**되어 있습니다. 각자 로컬에서 자유롭게 수정해서 쓰는 개인 설정 파일이며, `CLAUDE.md.example`이 그 시작 템플릿입니다.

## 1. 저장소
- **GitHub 주소**: https://github.com/20171193/nhn-prework
- **공개 범위**: 공개(Public)
- **기본 브랜치**: master
- **작업 브랜치**: `feature/combat` (개발1), `feature/augment` (개발2)
- **씬 파일 규칙**: `.unity` 씬 파일은 담당자 한 명만 수정 (병합 충돌 방지)

## 2. 엔진 & 네트워킹
- **Unity 버전**: 2022.3.62f2 (Photon PUN2 Asset Store 명시 지원 버전과 일치)
- **렌더 파이프라인**: URP (Universal Render Pipeline) + 2D Renderer
  - 이유: 2D Light2D로 조명 효과(발사체 글로우, 피격 이펙트 등) 저비용 구현 가능, WebGL/APK 최적화 유리
  - 주의: 외부 2D 애셋이 Built-in 셰이더 기반이면 핑크색으로 보일 수 있음 → `Edit > Rendering > Materials > Convert Selected Materials to URP`로 변환
- **네트워킹**: Photon PUN2
  - 전투 단계: 클라이언트 권위 히트 판정 (쏘는 쪽 클라이언트가 판정)
  - 증강 선택 단계: Room Custom Properties + RPC
  - 발사체: 클라이언트 사이드 예측 + 주기적 위치 보정

## 3. 폴더/버전관리 구조
- Unity 공식 `.gitignore` 적용 (Library/, Temp/, obj/, Build/, Logs/, UserSettings/ 등 제외)
- Git LFS 추적 대상: `*.png`, `*.psd`, `*.fbx` (GitHub 무료 LFS 용량 월 1GB 제한 — 아트 용량 커지면 압축본 별도 관리)
- 커밋 메시지 컨벤션: `feat:`, `fix:`, `wip:`

## 4. 빌드 & 배포
- **빌드 타겟**: WebGL (1순위, GitHub Pages 배포), APK (검토 중)
- **WebGL 배포 경로**: `gh-pages` 브랜치 (root), https://20171193.github.io/nhn-prework/
- **사전 과제 제출용 플레이 링크**: 위 GitHub Pages URL과 동일 (2026-08-09 최종 완성 빌드로 재배포 완료)

## 5. 확정된 기술 관련 규칙 (projectinfo.md와 연동)
- 점수 계산 로직은 별도 함수/모듈로 분리 (v1 단순 승패 점수제 → 추후 개편 대비)
- 증강 효과는 라운드 패배해도 누적 유지되므로, 상태 저장 구조는 초기화가 아니라 누적 방식으로 설계
- 무기는 1종류 고정이므로 무기 관련 확장 포인트는 최소한으로만 열어둘 것 (본행사 때 확장 대비 정도만)

## 6. 미정 / 확인 필요
- [ ] 저장소 주소 채워넣기
- [ ] WebGL + Photon PUN2 연결 사전 테스트 결과
- [ ] GitHub Pages 배포 URL 확정
- [ ] 전투 조작 방식 확정 후 관련 스크립트 구조 여기에 추가

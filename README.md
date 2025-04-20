# 🎮 Unity Large-Scale Data Transmission Project  
### *(Unity + FastAPI + DuckDB)*

Unity 기반의 **대용량 데이터 전송 및 3D 시각화 프로젝트**

FastAPI와 DuckDB 메모리 매핑을 활용해 **최대 100,000개 이상의 데이터를 초고속 처리**

기존 15초 이상 걸리던 방식 대비 **2~3초로 속도 개선**

---

## 📂 프로젝트 구조

| 폴더명            | 설명                            |
|------------------|---------------------------------|
| `FastAPI-DuckDB` | 서버 (FastAPI + DuckDB)         |
| `LargeDataProject` | 클라이언트 (Unity 프로젝트)     |

---

## 🍏 주요 특징

🔹 **초경량 데이터 전송**  
  `&`로 구분된 커스텀 포맷 기반의 텍스트 데이터 전송

🔹 **대규모 객체 렌더링**  
  최대 100,000건 이상의 Unity 3D 객체 처리

🔹 **실시간 데이터 반영**  
  서버 데이터 변경 시 Unity와 자동 동기화 *(Socket.IO)*

🔹 **고속 데이터 쿼리**  
  DuckDB를 사용한 빠른 데이터 조회

🔹 **RESTful API 제공**  
  FastAPI 기반의 CRUD API 인터페이스

---

## 🍏 FastAPI 서버 실행 방법

```
cd FastAPI-DuckDB
python -m uvicorn app.main:app --reload --port=8001
```

+) 포트 번호는 Unity의 `Var` 스크립트에서 변경 가능 (기본값: `8001`)

---

## 🍏 Unity 주요 스크립트

| 파일명           | 설명 |
|------------------|------|
| `CubeLoader.cs`  | 메인 데이터 수신 처리  → `LoadCubesFromCustom()` 및 `ParseCustomFormat()` 함수 |
| `SocketClient.cs`| 서버 변경 사항을 Unity에 반영 (현재 비활성화 상태) |

---

## 🍏 데이터 포맷 예시

```
UR,6,0,7_F,43&1&2025-02-05 10:00:00&2025-02-05 11:00:00&신선식품-과일-제주위미귤-1kg&3
```

| 필드명        | 설명                   |
|---------------|------------------------|
| object_id     | 객체 ID                |
| now_status    | 상태값                 |
| receiving_dt  | 입고일                 |
| shipping_dt   | 출고일                 |
| remark        | 비고                   |
| cur_qty       | 현재 수량              |

---

## 🍏 API 명세

| Method | Endpoint              | 설명 |
|--------|------------------------|------|
| GET    | `/api/cubes_custom`   | 모든 객체 데이터 (텍스트 `&` 형식) |
| GET    | `/api/cubes`          | 모든 객체 데이터 (JSON 배열) |
| GET    | `/api/cube`           | 단일 객체 조회 (`object_id` 쿼리) |
| POST   | `/api/cube`           | 객체 생성 + Socket.IO emit |
| PUT    | `/api/cube/{id}`      | 객체 수정 + Socket.IO emit |
| DELETE | `/api/cube/{id}`      | 객체 삭제 + Socket.IO emit |

---

## 🍏 .env 설정 예시 (`FastAPI-DuckDB/routes/.env`)

```
METANET_DB_HOST=  
METANET_DB_PORT=
METANET_DB_USERNAME=
METANET_DB_PSW=
METANET_DB_NAME=
```

---

## 🍏 브랜치 설명

| 브랜치명                  | 설명 |
|---------------------------|------|
| `main`                    | 성능 최적화 버전 (약 3초 내 데이터 수신) |
| `enhance/change-form`     | 상태 시각화 및 포맷 변경 포함 |
| `refactor/drop-unused-things` | 최종 추천 버전 (불필요 코드 제거 + 커스텀 포맷 적용) |

---

## 🍏 주의 사항

현재는 테스트용 테이블을 사용 중입니다.  

---

## 🛠 개발 환경

- Python 3.10+
- FastAPI
- DuckDB
- Unity (C#)
- Socket.IO
- VS Code / Rider



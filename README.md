<Unity Large-Scale Data Transmission Project (Unity + FastAPI + DuckDB)>

Unity 기반의 대용량 데이터 전송 및 3D 시각화 프로젝트

FastAPI와 DuckDB를 활용해 최대 100,000개 이상의 대규모 데이터를 초고속 처리

폴더 FastAPI-DuckDB는 서버 역할, LargeDataProject는 Unity 기반 클라이언트입니다.




  🍏 주요 특징

🔷 커스텀 포맷(&로 구분된 텍스트) 기반의 초경량 데이터 전송

🔷 최대 10만 건 이상 대규모 객체 렌더링

🔷 실시간 데이터 변경 시 Unity 동기화

🔷 DuckDB를 통한 고속 쿼리 처리

🔷 FastAPI 백엔드로 REST API 지원


  🍏 FastAPI 서버 실행 방법
  
cd FastAPI-DuckDB

python -m uvicorn app.main:app --reload --port=8001

+) Unity의 Var 스크립트에서 포트번호(현재는 8001) 변경 가능


  🍏 Unity 주요 스크립트 설명
  
CubeLoader.cs	= 메인 데이터 수신 스크립트. LoadCubesFromCustom() 및 ParseCustomFormat()가 주요 함수

SocketClient.cs	= 서버 데이터 변경 시 Unity에 반영 (현재 비활성화 상태)



  🍏 데이터 포맷 예시
  
UR,6,0,7_F,43&1&2025-02-05 10:00:00&2025-02-05 11:00:00&신선식품-과일-제주위미귤-1kg&3

+) 필드 구성: object_id & now_status & receiving_dt & shipping_dt & remark & cur_qty


  🍏 API 명세
  
Method	Endpoint	설명

GET	/api/cubes_custom	모든 객체 데이터 (텍스트 & 형식)

GET	/api/cubes	모든 객체 데이터 (JSON 배열)

GET	/api/cube	단일 객체 조회 (쿼리 파라미터 object_id)

POST	/api/cube	객체 생성 + Socket.IO 이벤트

PUT	/api/cube/{id}	객체 수정 + Socket.IO 이벤트

DELETE	/api/cube/{id}	객체 삭제 + Socket.IO 이벤트



  🍏 .env 설정 예시 (FastAPI-DuckDB/routes/.env)
  
METANET_DB_HOST=

METANET_DB_PORT=

METANET_DB_USERNAME=

METANET_DB_PSW=

METANET_DB_NAME=



  🍏 브랜치 설명
  
main                             성능 최적화 버전. 약 3초 내 데이터 수신

enhance/change-form              데이터 상태 시각화 포함 + 포맷 변경

refactor/drop-unused-things	     최종 추천 버전-불필요 코드 제거 + 커스텀 포맷 적용


  🍏 주의 사항
  
현재 테스트용 테이블 사용중


  🍏 개발환경
  
Python 3.10+

FastAPI

DuckDB

Unity (C#)

Socket.IO

VS Code / Rider


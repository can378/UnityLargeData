# routes/cube_routes.py
import os
import duckdb
import mysql.connector
import pandas as pd
from fastapi import APIRouter, Query, HTTPException
from app.models.cube import Cube
from dotenv import load_dotenv
from socketio import AsyncServer
import json
  
# load_dotenv()
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
load_dotenv(os.path.join(BASE_DIR, ".env"), override=True)#환경변수말고 .env를 쓰겠다.

# env_path = os.path.join(BASE_DIR, ".env")
# if os.path.exists(env_path): 
#     print(f".env 파일 존재함: {env_path}")
# else:
#     print(f".env 파일 없음: {env_path}")

  
TABLE_NAME = "TB_WEB_RACK_MST_TEST"
VIEW_NAME="V_WEB_RACK_MST"


# 외부에서 socketio 서버 주입 받음
router = APIRouter()
#duck_db!!!
con = duckdb.connect(database=':memory:')


def create_view():
    con.execute(f"""
        CREATE OR REPLACE VIEW {VIEW_NAME} AS
        SELECT
            object_id,
            CASE
                WHEN DATEDIFF('day',receiving_dt, shipping_dt) <= 5 THEN 1
                WHEN DATEDIFF('day',receiving_dt, shipping_dt) <= 10 THEN 2
                ELSE 3
            END AS now_status,
            receiving_dt,
            shipping_dt,
            remark,
            cur_qty
        FROM {TABLE_NAME}
    """)



def load_data_from_mariadb():
    conn = mysql.connector.connect(
        host=os.getenv("METANET_DB_HOST"),
        port=int(os.getenv("METANET_DB_PORT")),
        user=os.getenv("METANET_DB_USERNAME"),
        password=os.getenv("METANET_DB_PSW"),
        database=os.getenv("METANET_DB_NAME")
    )

 
    cursor = conn.cursor(dictionary=True)
    
    # 테이블 데이터
    cursor.execute(f"SELECT * FROM {TABLE_NAME}")
    df_table = pd.DataFrame(cursor.fetchall())
    con.execute(f"CREATE OR REPLACE TABLE {TABLE_NAME} AS SELECT * FROM df_table")

    # 뷰 결과도 로딩
    cursor.execute(f"SELECT * FROM {VIEW_NAME}")
    df_view = pd.DataFrame(cursor.fetchall())
    create_view()

    #close
    cursor.close()
    conn.close()

load_data_from_mariadb()





# 외부에서 sio 주입 예정
sio: AsyncServer = None

def init_routes(socket_io: AsyncServer):
    global sio
    sio = socket_io




def get_cube_from_view(object_id: str):
    result = con.execute(f"""
        SELECT * FROM {VIEW_NAME} WHERE object_id = ?
    """, (object_id,)).fetchdf()

    if result.empty:
        return None

    # 첫 행만 추출
    cube = result.to_dict(orient="records")[0]

    # datetime → str 변환
    for key in ["receiving_dt", "shipping_dt"]:
        if key in cube and hasattr(cube[key], "isoformat"):
            cube[key] = cube[key].isoformat()

    return cube





@router.get("/api/cubes")
def get_cubes():
    result = con.execute(f"SELECT * FROM {VIEW_NAME} limit 1000").fetchdf()
    return result.to_dict(orient="records")

@router.get("/api/cube")
def get_one_cube(object_id: str = Query(...)):
    result = con.execute(f"SELECT * FROM {VIEW_NAME} WHERE object_id = ?", (object_id,)).fetchdf()
    if result.empty:
        return {"message": f"Cube {object_id} not found."}
    return result.to_dict(orient="records")[0]

@router.post("/api/cube")
async def create_cube(cube: Cube):
    con.execute(f"""
        INSERT INTO {TABLE_NAME} 
        (object_id, receiving_dt, shipping_dt, remark, cur_qty) 
        VALUES (?, ?, ?, ?, ?)
        """, (cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty)
    )

    cube_with_status = get_cube_from_view(cube.object_id)
    await sio.emit("cube_updated", cube_with_status)
    return {"message": "box created"}


@router.put("/api/cube/{object_id}")
async def update_cube(object_id: str, cube: Cube):
    result = con.execute(f"SELECT * FROM {TABLE_NAME} WHERE object_id = ?", (object_id,)).fetchdf()
    
    if result.empty:
        raise HTTPException(status_code=404, detail="Cube not found")
    
    con.execute(f"UPDATE {TABLE_NAME} SET object_id=?, receiving_dt=?, shipping_dt=?, remark=?, cur_qty=? WHERE object_id=?", (
        cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty, object_id
    ))

    
    cube_with_status = get_cube_from_view(cube.object_id)
    
    print("cube_with_status type:", type(cube_with_status))
    print("update data=" + json.dumps(cube_with_status, indent=2))
    
    await sio.emit("cube_updated", cube_with_status)
    return {"message": "box updated"}


@router.delete("/api/cube/{object_id}")
async def delete_cube(object_id: str):
    con.execute(f"DELETE FROM {TABLE_NAME} WHERE object_id = ?", (object_id,))

    await sio.emit("cube_deleted", {"object_id": object_id})
    return {"message": "Cube deleted"}





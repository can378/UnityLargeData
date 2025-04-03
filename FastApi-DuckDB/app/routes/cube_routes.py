import os
import mysql.connector
from fastapi import APIRouter, Query, HTTPException, Response
from app.models.cube import Cube
from dotenv import load_dotenv
import pandas as pd

# 환경 변수 불러오기
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
load_dotenv(os.path.join(BASE_DIR, ".env"), override=True)

TABLE_NAME = "TB_WEB_RACK_MST_TEST"
router = APIRouter()

# MariaDB 연결 함수
def get_mariadb_conn():
    return mysql.connector.connect(
        host=os.getenv("METANET_DB_HOST"),
        port=int(os.getenv("METANET_DB_PORT")),
        user=os.getenv("METANET_DB_USERNAME"),
        password=os.getenv("METANET_DB_PSW"),
        database=os.getenv("METANET_DB_NAME")
    )
 
CUBE_SELECT_QUERY = f"""
    SELECT
        ROW_NUMBER() OVER () AS row_index,
        object_id,
        CASE
            WHEN DATEDIFF(receiving_dt, shipping_dt) <= 5 THEN 1
            WHEN DATEDIFF(receiving_dt, shipping_dt) <= 10 THEN 2
            ELSE 3
        END AS now_status,
        receiving_dt,
        shipping_dt,
        remark,
        cur_qty
    FROM {TABLE_NAME}
"""

# get all cubes - text form
@router.get("/api/cubes_custom")
def get_cubes_custom():
    conn = get_mariadb_conn()
    df = pd.read_sql(CUBE_SELECT_QUERY, conn)
    conn.close()
    lines = df.apply(lambda row: "&".join([str(v) for v in row]), axis=1)
    return Response("\n".join(lines), media_type="text/plain")

# insert cube
@router.post("/api/cube")
async def create_cube(cube: Cube):
    conn = get_mariadb_conn()
    cursor = conn.cursor()
    cursor.execute(f"""
        INSERT INTO {TABLE_NAME} 
        (object_id, receiving_dt, shipping_dt, remark, cur_qty) 
        VALUES (%s, %s, %s, %s, %s)
    """, (cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty))
    conn.commit()
    cursor.close()
    conn.close()
    return {"message": "object created"}

# update cube
@router.put("/api/cube/{object_id}")
async def update_cube(object_id: str, cube: Cube):
    conn = get_mariadb_conn()
    cursor = conn.cursor(dictionary=True)
    cursor.execute(f"SELECT * FROM {TABLE_NAME} WHERE object_id = %s", (object_id,))
    result = cursor.fetchone()

    if result is None:
        cursor.close()
        conn.close()
        raise HTTPException(status_code=404, detail="Object not found")

    cursor.execute(f"""
        UPDATE {TABLE_NAME} 
        SET object_id=%s, receiving_dt=%s, shipping_dt=%s, remark=%s, cur_qty=%s 
        WHERE object_id=%s
    """, (cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty, object_id))
    conn.commit()
    cursor.close()
    conn.close()
    return {"message": "object updated"}

# delete cube
@router.delete("/api/cube/{object_id}")
async def delete_cube(object_id: str):
    conn = get_mariadb_conn()
    cursor = conn.cursor()
    cursor.execute(f"SELECT * FROM {TABLE_NAME} WHERE object_id = %s", (object_id,))
    result = cursor.fetchone()

    if result is None:
        cursor.close()
        conn.close()
        raise HTTPException(status_code=404, detail="object not found")

    cursor.execute(f"DELETE FROM {TABLE_NAME} WHERE object_id = %s", (object_id,))
    conn.commit()
    cursor.close()
    conn.close()
    return {"message": "object deleted"}

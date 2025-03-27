import os
import duckdb
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
con = duckdb.connect(database=':memory:')

# load data
def load_data_from_mariadb():
    conn = mysql.connector.connect(
        host=os.getenv("METANET_DB_HOST"),
        port=int(os.getenv("METANET_DB_PORT")),
        user=os.getenv("METANET_DB_USERNAME"),
        password=os.getenv("METANET_DB_PSW"),
        database=os.getenv("METANET_DB_NAME")
    )

    cursor = conn.cursor(dictionary=True)
    cursor.execute(f"SELECT * FROM {TABLE_NAME}")
    rows = cursor.fetchall()
    df_table = pd.DataFrame(rows)
    con.execute(f"CREATE OR REPLACE TABLE {TABLE_NAME} AS SELECT * FROM df_table")

    cursor.close()
    conn.close()

load_data_from_mariadb()




CUBE_SELECT_QUERY = f"""
    SELECT
        ROW_NUMBER() OVER () AS row_index,
        object_id,
        CASE
            WHEN DATEDIFF('day', receiving_dt, shipping_dt) <= 5 THEN 1
            WHEN DATEDIFF('day', receiving_dt, shipping_dt) <= 10 THEN 2
            ELSE 3
        END AS now_status,
        receiving_dt,
        shipping_dt,
        remark,
        cur_qty
    FROM {TABLE_NAME}
"""









#get all cubes - text form
@router.get("/api/cubes_custom")
def get_cubes_custom():
    df = con.execute(CUBE_SELECT_QUERY).fetchdf()
    lines = df.apply(lambda row: "&".join([str(v) for v in row]), axis=1)
    return Response("\n".join(lines), media_type="text/plain")


#insert cube
@router.post("/api/cube")
async def create_cube(cube: Cube):
    con.execute(f"""
        INSERT INTO {TABLE_NAME} 
        (object_id, receiving_dt, shipping_dt, remark, cur_qty) 
        VALUES (?, ?, ?, ?, ?)
    """, (cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty))

    # cube_with_status = get_cube_with_status(cube.object_id)
    
    return {"message": "object created"}

#update cube
@router.put("/api/cube/{object_id}")
async def update_cube(object_id: str, cube: Cube):
    result = con.execute(f"SELECT * FROM {TABLE_NAME} WHERE object_id = ?", (object_id,)).fetchdf()
    
    if result.empty:
        raise HTTPException(status_code=404, detail="Cube not found")

    con.execute(f"""
        UPDATE {TABLE_NAME} 
        SET object_id=?, receiving_dt=?, shipping_dt=?, remark=?, cur_qty=? 
        WHERE object_id=?
    """, (cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty, object_id))

    # cube_with_status = get_cube_with_status(cube.object_id)
    
    return {"message": "object updated"}

#delete cube
@router.delete("/api/cube/{object_id}")
async def delete_cube(object_id: str):
    con.execute(f"DELETE FROM {TABLE_NAME} WHERE object_id = ?", (object_id,))
    
    return {"message": "object deleted"}



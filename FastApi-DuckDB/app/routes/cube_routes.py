import os
import duckdb
import mysql.connector
import pandas as pd
from fastapi import APIRouter, HTTPException, Response
from app.models.cube import Cube
from dotenv import load_dotenv
from fastapi.exceptions import HTTPException as FastAPIHTTPException

# 환경 변수 설정
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
load_dotenv(os.path.join(BASE_DIR, ".env"), override=True)

# 상수
TABLE_NAME = "TB_WEB_RACK_MST_TEST"

#DuckDB 연결
router = APIRouter()
con = duckdb.connect(database=':memory:')


# 공통 DB 유틸
def get_mariadb_connection():
    return mysql.connector.connect(
        host=os.getenv("METANET_DB_HOST"),
        port=int(os.getenv("METANET_DB_PORT")),
        user=os.getenv("METANET_DB_USERNAME"),
        password=os.getenv("METANET_DB_PSW"),
        database=os.getenv("METANET_DB_NAME")
    )

def execute_mariadb_query(query: str, params: tuple) -> int:
    conn = get_mariadb_connection()
    cursor = conn.cursor()
    cursor.execute(query, params)
    conn.commit()
    rowcount = cursor.rowcount
    cursor.close()
    conn.close()
    return rowcount

def get_duckdb_row_by_seq(seq: str):
    return con.execute(
        f"SELECT * FROM {TABLE_NAME} WHERE CAST(seq AS VARCHAR) = ?", (seq.strip(),)
    ).fetchdf()


# DuckDB 초기 로딩
def load_data_from_mariadb():
    conn = get_mariadb_connection()
    cursor = conn.cursor(dictionary=True)
    cursor.execute(f"SELECT * FROM {TABLE_NAME}")
    df = pd.DataFrame(cursor.fetchall())
    con.execute(f"CREATE OR REPLACE TABLE {TABLE_NAME} AS SELECT * FROM df")
    cursor.close()
    conn.close()

load_data_from_mariadb()








#전체 조회
@router.get("/api/cubes_custom")
def get_cubes_custom():
    query = f"""
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
    df = con.execute(query).fetchdf()
    return Response("\n".join(df.apply(lambda row: "&".join(map(str, row)), axis=1)), media_type="text/plain")




# 등록
@router.post("/api/cube")
async def create_cube(cube: Cube):

    # insert in DuckDB
    con.execute(f"""
        INSERT INTO {TABLE_NAME}
        (seq, object_id, receiving_dt, shipping_dt, remark, cur_qty)
        VALUES (?, ?, ?, ?, ?, ?)
    """, (cube.seq, cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty))

    # insert in DuckDB
    insert_sql = f"""
        INSERT INTO {TABLE_NAME}
        (seq, object_id, receiving_dt, shipping_dt, remark, cur_qty)
        VALUES (%s, %s, %s, %s, %s, %s)
    """
    params = (cube.seq, cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty)
    execute_mariadb_query(insert_sql, params)

    return {"message": "object created"}




#수정
@router.put("/api/cube/{seq}")
async def update_cube(seq: str, cube: Cube):

    #공백 제거
    seq = seq.strip()

    try:
        #duckdb에 있는지 조회
        if get_duckdb_row_by_seq(seq).empty:
            raise FastAPIHTTPException(status_code=404, detail="Object not found in DuckDB")

        #DuckDB에서 수정
        con.execute(f"""
            UPDATE {TABLE_NAME}
            SET object_id=?, receiving_dt=?, shipping_dt=?, remark=?, cur_qty=?
            WHERE CAST(seq AS VARCHAR) = ?
        """, (cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty, seq))
    except FastAPIHTTPException as e:
        raise e
    except Exception as duck_err:
        raise HTTPException(status_code=500, detail=f"DuckDB Error: {str(duck_err)}")


    #MariaDB에서 수정
    update_sql = f"""
        UPDATE {TABLE_NAME}
        SET object_id=%s, receiving_dt=%s, shipping_dt=%s, remark=%s, cur_qty=%s
        WHERE seq=%s
    """
    params = (cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty, seq)

    #변경 여부 확인
    rowcount = execute_mariadb_query(update_sql, params)
    if rowcount == 0:
        raise FastAPIHTTPException(status_code=404, detail="Object not found in MariaDB")

    return {"message": f"Object with seq={seq} updated successfully"}




#삭제
@router.delete("/api/cube/{seq}")
async def delete_cube(seq: str):
    #공백제거 
    seq = seq.strip()

    #Dubckdb에 있는지 조회
    if get_duckdb_row_by_seq(seq).empty:
        raise HTTPException(status_code=404, detail="Object not found in DuckDB")

    #DuckDB에서 삭제
    con.execute(f"DELETE FROM {TABLE_NAME} WHERE CAST(seq AS VARCHAR) = ?", (seq,))

    #MariaDB에서 삭제
    delete_sql = f"DELETE FROM {TABLE_NAME} WHERE seq = %s"
    rowcount = execute_mariadb_query(delete_sql, (seq,))

    if rowcount == 0:
        raise HTTPException(status_code=404, detail="Object not found in MariaDB")

    return {"message": f"object seq={seq} deleted successfully"}

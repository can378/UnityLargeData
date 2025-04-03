import os
import duckdb
import mysql.connector
from fastapi import APIRouter, Query, HTTPException, Response
from app.models.cube import Cube
from dotenv import load_dotenv
import pandas as pd
from fastapi.exceptions import HTTPException as FastAPIHTTPException


# 환경 변수 불러오기
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
load_dotenv(os.path.join(BASE_DIR, ".env"), override=True)

TABLE_NAME = "TB_WEB_RACK_MST_TEST"
router = APIRouter()
con = duckdb.connect(database=':memory:')

# DuckDB 초기 로딩
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

# 모든 cube 조회 - 텍스트 포맷
@router.get("/api/cubes_custom")
def get_cubes_custom():
    df = con.execute(CUBE_SELECT_QUERY).fetchdf()
    lines = df.apply(lambda row: "&".join([str(v) for v in row]), axis=1)
    return Response("\n".join(lines), media_type="text/plain")

# cube 삽입
@router.post("/api/cube")
async def create_cube(cube: Cube):
    # DuckDB 반영
    con.execute(f"""
        INSERT INTO {TABLE_NAME} 
        (seq, object_id, receiving_dt, shipping_dt, remark, cur_qty) 
        VALUES (?, ?, ?, ?, ?, ?)
    """, (cube.seq, cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty))

    # MariaDB 반영
    conn = mysql.connector.connect(
        host=os.getenv("METANET_DB_HOST"),
        port=int(os.getenv("METANET_DB_PORT")),
        user=os.getenv("METANET_DB_USERNAME"),
        password=os.getenv("METANET_DB_PSW"),
        database=os.getenv("METANET_DB_NAME")
    )
    cursor = conn.cursor()
    cursor.execute(f"""
        INSERT INTO {TABLE_NAME} 
        (seq, object_id, receiving_dt, shipping_dt, remark, cur_qty) 
        VALUES (%s, %s, %s, %s, %s, %s)
    """, (cube.seq, cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty))

    conn.commit()
    cursor.close()
    conn.close()

    return {"message": "object created"}






#수정
@router.put("/api/cube/{seq}")
async def update_cube(seq: str, cube: Cube):
    seq = seq.strip() 

    # DuckDB 처리
    try:
        print(">>> [PUT] seq from path param:", seq)

        duck_result = con.execute(
            f"SELECT * FROM {TABLE_NAME} WHERE CAST(seq AS VARCHAR) = ?", (seq,)
        ).fetchdf()

        print(">>> [PUT] DuckDB result row count:", len(duck_result))

        if duck_result.empty:
            raise FastAPIHTTPException(status_code=404, detail="Object not found in DuckDB")

        con.execute(f"""
            UPDATE {TABLE_NAME} 
            SET object_id=?, receiving_dt=?, shipping_dt=?, remark=?, cur_qty=? 
            WHERE CAST(seq AS VARCHAR) = ?
        """, (
            cube.object_id,
            cube.receiving_dt,
            cube.shipping_dt,
            cube.remark,
            cube.cur_qty,
            seq
        ))

    except FastAPIHTTPException as e:
        raise e
    except Exception as duck_err:
        print(">>> [DuckDB Error]", duck_err)
        raise HTTPException(status_code=500, detail=f"DuckDB Error: {str(duck_err)}")

    # MariaDB 처리
    try:
        conn = mysql.connector.connect(
            host=os.getenv("METANET_DB_HOST"),
            port=int(os.getenv("METANET_DB_PORT")),
            user=os.getenv("METANET_DB_USERNAME"),
            password=os.getenv("METANET_DB_PSW"),
            database=os.getenv("METANET_DB_NAME")
        )
        cursor = conn.cursor()

        cursor.execute(f"""
            UPDATE {TABLE_NAME} 
            SET object_id=%s, receiving_dt=%s, shipping_dt=%s, remark=%s, cur_qty=%s 
            WHERE seq=%s
        """, (
            cube.object_id,
            cube.receiving_dt,
            cube.shipping_dt,
            cube.remark,
            cube.cur_qty,
            seq
        ))
        conn.commit()

        if cursor.rowcount == 0:
            raise FastAPIHTTPException(status_code=404, detail="Object not found in MariaDB")

        cursor.close()
        conn.close()

    except FastAPIHTTPException as e:
        raise e
    except mysql.connector.Error as err:
        print(">>> [MariaDB Error]", err)
        raise HTTPException(status_code=500, detail=f"MySQL Error: {str(err)}")

    return {"message": f"Object with seq={seq} updated successfully"}



 
# cube 삭제
@router.delete("/api/cube/{seq}")
async def delete_cube(seq: str):
    seq = seq.strip()  # 공백 제거

    # DuckDB 존재 여부 확인
    duck_result = con.execute(
        f"SELECT * FROM {TABLE_NAME} WHERE CAST(seq AS VARCHAR) = ?", (seq,)
    ).fetchdf()

    if duck_result.empty:
        raise HTTPException(status_code=404, detail="Object not found in DuckDB")

    #DuckDB 삭제
    con.execute(
        f"DELETE FROM {TABLE_NAME} WHERE CAST(seq AS VARCHAR) = ?", (seq,)
    )

    #MariaDB 삭제
    try:
        conn = mysql.connector.connect(
            host=os.getenv("METANET_DB_HOST"),
            port=int(os.getenv("METANET_DB_PORT")),
            user=os.getenv("METANET_DB_USERNAME"),
            password=os.getenv("METANET_DB_PSW"),
            database=os.getenv("METANET_DB_NAME")
        )
        cursor = conn.cursor()

        print(">>> [MariaDB] Deleting row with seq:", repr(seq))

        cursor.execute(
            f"DELETE FROM {TABLE_NAME} WHERE seq = %s", (seq,)
        )
        conn.commit()

        if cursor.rowcount == 0:
            raise HTTPException(status_code=404, detail="Object not found in MariaDB")

        cursor.close()
        conn.close()

    except mysql.connector.Error as err:
        print(">>> [MariaDB Error]", err)
        raise HTTPException(status_code=500, detail=f"MySQL Error: {str(err)}")

    return {"message": f"Object with seq={seq} deleted successfully"}

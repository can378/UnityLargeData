# routes/cube_routes.py
import os
import duckdb
import mysql.connector
from fastapi import APIRouter, Query, HTTPException, Response
from app.models.cube import Cube
from dotenv import load_dotenv
from socketio import AsyncServer
import json
import io
import pandas as pd

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
load_dotenv(os.path.join(BASE_DIR, ".env"), override=True)

TABLE_NAME = "TB_WEB_RACK_MST_TEST"

router = APIRouter()
con = duckdb.connect(database=':memory:')

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

sio: AsyncServer = None

def init_routes(socket_io: AsyncServer):
    global sio
    sio = socket_io

def get_cube_with_status(object_id: str):
    query = f"""
        SELECT
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
        WHERE object_id = ?
    """
    result = con.execute(query, (object_id,)).fetchdf()
    if result.empty:
        return None

    cube = result.to_dict(orient="records")[0]
    for key in ["receiving_dt", "shipping_dt"]:
        if key in cube and hasattr(cube[key], "isoformat"):
            cube[key] = cube[key].isoformat()
    return cube

@router.get("/api/cubes")
def get_cubes():
    query = f"""
        SELECT
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
    result = con.execute(query).fetchdf()
    return result.to_dict(orient="records")

@router.get("/api/cube")
def get_one_cube(object_id: str = Query(...)):
    result = get_cube_with_status(object_id)
    if result is None:
        return {"message": f"Cube {object_id} not found."}
    return result

@router.post("/api/cube")
async def create_cube(cube: Cube):
    con.execute(f"""
        INSERT INTO {TABLE_NAME} 
        (object_id, receiving_dt, shipping_dt, remark, cur_qty) 
        VALUES (?, ?, ?, ?, ?)
    """, (cube.object_id, cube.receiving_dt, cube.shipping_dt, cube.remark, cube.cur_qty))

    cube_with_status = get_cube_with_status(cube.object_id)
    await sio.emit("cube_updated", cube_with_status)
    return {"message": "box created"}

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

    cube_with_status = get_cube_with_status(cube.object_id)
    # print("cube_with_status type:", type(cube_with_status))
    # print("update data=" + json.dumps(cube_with_status, indent=2))

    await sio.emit("cube_updated", cube_with_status)
    return {"message": "box updated"}

@router.delete("/api/cube/{object_id}")
async def delete_cube(object_id: str):
    con.execute(f"DELETE FROM {TABLE_NAME} WHERE object_id = ?", (object_id,))
    await sio.emit("cube_deleted", {"object_id": object_id})
    return {"message": "Cube deleted"}



@router.get("/api/cubes_csv")
def get_cubes_csv():
    query = f"""
        SELECT
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
    buffer = io.StringIO()
    df.to_csv(buffer, index=False)
    return Response(content=buffer.getvalue(), media_type="text/csv")

@router.get("/api/cubes_custom")
def get_cubes_custom():
    query = f"""
        SELECT
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
    lines = df.apply(lambda row: "&".join([str(v) for v in row]), axis=1)
    return Response("\n".join(lines), media_type="text/plain")
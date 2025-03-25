# routes/cube_routes.py
import os
import duckdb
import mysql.connector
import pandas as pd
from fastapi import APIRouter, Query, HTTPException
from models.cube import Cube
from dotenv import load_dotenv
from socketio import AsyncServer

load_dotenv()

# 외부에서 socketio 서버 주입 받음
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
    cursor.execute("SELECT * FROM largedata")
    rows = cursor.fetchall()
    df = pd.DataFrame(rows)
    con.execute("CREATE OR REPLACE TABLE largedata AS SELECT * FROM df")
    cursor.close()
    conn.close()

load_data_from_mariadb()

# 외부에서 sio 주입 예정
sio: AsyncServer = None

def init_routes(socket_io: AsyncServer):
    global sio
    sio = socket_io

@router.get("/api/cubes")
def get_cubes():
    result = con.execute("SELECT seq, column5 FROM largedata limit 1000").fetchdf()
    return result.to_dict(orient="records")

@router.get("/api/cube")
def get_one_cube(seq: int = Query(...)):
    result = con.execute("SELECT * FROM largedata WHERE seq = ?", (seq,)).fetchdf()
    if result.empty:
        return {"message": f"Cube {seq} not found."}
    return result.to_dict(orient="records")[0]

@router.post("/api/cube")
async def create_cube(cube: Cube):
    con.execute("INSERT INTO largedata VALUES (?, ?, ?, ?, ?, ?)", (
        cube.seq, cube.column1, cube.column2, cube.column3, cube.column4, cube.column5
    ))
    await sio.emit("cube_updated", cube.dict())
    return {"message": "Cube created"}

@router.put("/api/cube/{seq}")
async def update_cube(seq: int, cube: Cube):
    result = con.execute("SELECT * FROM largedata WHERE seq = ?", (seq,)).fetchdf()
    if result.empty:
        raise HTTPException(status_code=404, detail="Cube not found")
    
    con.execute("UPDATE largedata SET column1=?, column2=?, column3=?, column4=?, column5=? WHERE seq=?", (
        cube.column1, cube.column2, cube.column3, cube.column4, cube.column5, seq
    ))
    
    await sio.emit("cube_updated", cube.dict())
    return {"message": "Cube updated"}

@router.delete("/api/cube/{seq}")
async def delete_cube(seq: int):
    result = con.execute("SELECT * FROM largedata WHERE seq = ?", (seq,)).fetchdf()
    if result.empty:
        raise HTTPException(status_code=404, detail="Cube not found")
    con.execute("DELETE FROM largedata WHERE seq = ?", (seq,))
    await sio.emit("cube_deleted", {"seq": seq})
    return {"message": "Cube deleted"}

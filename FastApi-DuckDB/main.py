import os
import socketio
from fastapi import FastAPI, Query, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from dotenv import load_dotenv
import mysql.connector
import duckdb
import pandas as pd
from pydantic import BaseModel

# Load .env
load_dotenv()

# Create Socket.IO server
sio = socketio.AsyncServer(async_mode='asgi', cors_allowed_origins='*')
fastapi_app = FastAPI()
app = socketio.ASGIApp(sio, other_asgi_app=fastapi_app)

# CORS for Unity
fastapi_app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# Pydantic model for cube
class Cube(BaseModel):
    seq: int
    column1: str
    column2: int
    column3: int
    column4: str
    column5: str

# DuckDB connection
con = duckdb.connect(database=':memory:')

# Load from MariaDB to DuckDB
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

# FastAPI endpoints
@fastapi_app.get("/api/cubes")
def get_cubes():
    result = con.execute("SELECT seq, column5 FROM largedata limit 1000").fetchdf()
    return result.to_dict(orient="records")

@fastapi_app.get("/api/cube")
def get_one_cube(seq: int = Query(...)):
    result = con.execute("SELECT * FROM largedata WHERE seq = ?", (seq,)).fetchdf()
    if result.empty:
        return {"message": f"Cube {seq} not found."}
    return result.to_dict(orient="records")[0]

@fastapi_app.post("/api/cube")
async def create_cube(cube: Cube):
    con.execute("INSERT INTO largedata VALUES (?, ?, ?, ?, ?, ?)", (
        cube.seq, cube.column1, cube.column2, cube.column3, cube.column4, cube.column5
    ))
    await sio.emit("cube_updated", cube.dict())
    return {"message": "Cube created"}

@fastapi_app.put("/api/cube/{seq}")
async def update_cube(seq: int, cube: Cube):
    result = con.execute("SELECT * FROM largedata WHERE seq = ?", (seq,)).fetchdf()
    if result.empty:
        raise HTTPException(status_code=404, detail="Cube not found")
    
    con.execute("UPDATE largedata SET column1=?, column2=?, column3=?, column4=?, column5=? WHERE seq=?", (
        cube.column1, cube.column2, cube.column3, cube.column4, cube.column5, seq
    ))
    
    # 전체 데이터 JSON으로 보내기
    await sio.emit("cube_updated", cube.dict())
    
    return {"message": "Cube updated"}

@fastapi_app.delete("/api/cube/{seq}")
async def delete_cube(seq: int):
    result = con.execute("SELECT * FROM largedata WHERE seq = ?", (seq,)).fetchdf()
    if result.empty:
        raise HTTPException(status_code=404, detail="Cube not found")
    con.execute("DELETE FROM largedata WHERE seq = ?", (seq,))
    await sio.emit("cube_deleted", {"seq": seq})
    return {"message": "Cube deleted"}

# Socket.IO events
@sio.event
async def connect(sid, environ):
    print(f"[Socket.IO] Unity connected: {sid}")

@sio.event
async def disconnect(sid):
    print(f"[Socket.IO] Unity disconnected: {sid}")

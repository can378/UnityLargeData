from fastapi import FastAPI
from fastapi import Query
from fastapi.middleware.cors import CORSMiddleware
import duckdb
import pandas as pd
import mysql.connector

app = FastAPI()

# CORS 설정 (Unity에서 요청 허용)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# DuckDB 인메모리 연결
con = duckdb.connect(database=':memory:')

def load_data_from_mariadb():
    conn = mysql.connector.connect(
        host="localhost",
        port=3305,
        user="test",
        password="test",
        database="test"
    )

    cursor = conn.cursor(dictionary=True)
    cursor.execute("SELECT * FROM largedata")
    rows = cursor.fetchall()
    df = pd.DataFrame(rows)
    con.execute("CREATE OR REPLACE TABLE largedata AS SELECT * FROM df")
    cursor.close()
    conn.close()

load_data_from_mariadb()

@app.get("/api/cubes")
def get_cubes():
    result = con.execute("SELECT seq, column5 FROM largedata").fetchdf()
    return result.to_dict(orient="records")


@app.get("/api/cube")
def get_one_cube(seq: int = Query(..., description="조회할 cube의 seq 값")):
    result = con.execute("SELECT * FROM largedata WHERE seq = ?", (seq,)).fetchdf()
    if result.empty:
        return {"message": f"Cube with seq={seq} not found."}
    return result.to_dict(orient="records")[0]  # 단일 row만 반환
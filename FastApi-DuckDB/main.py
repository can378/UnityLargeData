from fastapi import FastAPI
from fastapi import Query
from fastapi.middleware.cors import CORSMiddleware
import duckdb
import pandas as pd
import mysql.connector
from dotenv import load_dotenv
import os

load_dotenv()
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
    print("Loaded DB username:", os.getenv("METANET_DB_USERNAME"))

    try:
        conn = mysql.connector.connect(
            host=os.getenv("METANET_DB_HOST"),
            port=int(os.getenv("METANET_DB_PORT")),
            user=os.getenv("METANET_DB_USERNAME"),
            password=os.getenv("METANET_DB_PSW"),
            database=os.getenv("METANET_DB_NAME")
        )
    except KeyError as e:
        raise RuntimeError(f"환경변수 에러: {e}")


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
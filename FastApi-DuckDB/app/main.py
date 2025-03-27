from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.routes import cube_routes
from fastapi.middleware.gzip import GZipMiddleware

app = FastAPI()
app.add_middleware(GZipMiddleware, minimum_size=1000) #g zip사용 속도때매

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(cube_routes.router)

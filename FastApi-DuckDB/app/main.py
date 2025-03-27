# main.py
import socketio
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.routes import cube_routes
from fastapi.middleware.gzip import GZipMiddleware#g cip


# Create Socket.IO server
sio = socketio.AsyncServer(async_mode='asgi', cors_allowed_origins='*')
fastapi_app = FastAPI()
fastapi_app.add_middleware(GZipMiddleware, minimum_size=1000)#GZip 적용. 속도 때문에
app = socketio.ASGIApp(sio, other_asgi_app=fastapi_app)

# CORS for Unity
fastapi_app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# 라우터 초기화 및 등록
cube_routes.init_routes(sio)
fastapi_app.include_router(cube_routes.router)

# Socket.IO events
@sio.event
async def connect(sid, environ):
    print(f"[Socket.IO] Unity connected: {sid}")

@sio.event
async def disconnect(sid):
    print(f"[Socket.IO] Unity disconnected: {sid}")

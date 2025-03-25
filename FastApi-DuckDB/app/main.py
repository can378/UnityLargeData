# main.py
import socketio
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.routes import cube_routes

# Load .env
from dotenv import load_dotenv
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

from typing import Optional
from pydantic import BaseModel
from datetime import datetime

class Cube(BaseModel):
    seq: str
    object_id: str
    now_status: Optional[int] = None
    receiving_dt: str
    shipping_dt:str
    remark: Optional[str]
    cur_qty: int

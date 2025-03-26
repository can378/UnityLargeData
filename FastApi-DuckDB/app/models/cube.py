from pydantic import BaseModel

# Pydantic model for cube
class Cube(BaseModel):
    object_id: str
    receiving_dt: str
    shipping_dt: str
    remark: str
    cur_qty: int
    
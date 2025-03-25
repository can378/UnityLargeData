from pydantic import BaseModel

# Pydantic model for cube
class Cube(BaseModel):
    seq: int
    column1: str
    column2: int
    column3: int
    column4: str
    column5: str
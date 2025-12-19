# token_server.py
from fastapi import FastAPI, HTTPException, Body
from pydantic import BaseModel
from tokenizers import Tokenizer
from typing import List, Optional
import os
import logging

# Thiết lập logging
log = logging.getLogger("uvicorn.error")

# Cấu hình
TOKENIZER_PATH = os.environ.get("TOKENIZER_PATH", "tokenizer.json")
SERVER_TITLE = "Fast Token Server"
# Giới hạn số lượng item trong 1 batch để tránh treo server
MAX_BATCH_SIZE = int(os.environ.get("MAX_BATCH_SIZE", 1000))

app = FastAPI(title=SERVER_TITLE)

# Tải tokenizer một lần duy nhất khi khởi động
try:
    tokenizer = Tokenizer.from_file(TOKENIZER_PATH)
    log.info(f"Loaded tokenizer from {TOKENIZER_PATH}")
except Exception as e:
    log.error(f"Failed to load tokenizer: {e}")
    raise RuntimeError(f"Failed to load tokenizer at '{TOKENIZER_PATH}': {e}")

# Pydantic models
class CountRequest(BaseModel):
    text: str
    return_tokens: Optional[bool] = False

class CountResponse(BaseModel):
    token_count: int
    token_ids: Optional[List[int]] = None

class BatchCountRequest(BaseModel):
    texts: List[str]
    return_tokens: Optional[bool] = False

class BatchCountItem(BaseModel):
    token_count: int
    token_ids: Optional[List[int]] = None

class BatchCountResponse(BaseModel):
    results: List[BatchCountItem]

# Helpers
def guard_text(text: str):
    if not isinstance(text, str):
        raise HTTPException(status_code=400, detail="text must be a string")

# Endpoints
@app.post("/count", response_model=CountResponse)
async def count(req: CountRequest = Body(...)):
    guard_text(req.text)
    
    # Mã hóa trực tiếp không qua cache
    encoding = tokenizer.encode(req.text)
    
    return CountResponse(
        token_count=len(encoding.ids),
        token_ids=encoding.ids if req.return_tokens else None
    )

@app.post("/batch_count", response_model=BatchCountResponse)
async def batch_count(req: BatchCountRequest = Body(...)):
    if not isinstance(req.texts, list):
        raise HTTPException(status_code=400, detail="texts must be a list of strings")
    if len(req.texts) == 0:
        raise HTTPException(status_code=400, detail="texts list is empty")
    if len(req.texts) > MAX_BATCH_SIZE:
        raise HTTPException(status_code=413, detail=f"too many texts, max {MAX_BATCH_SIZE}")

    results = []
    for t in req.texts:
        guard_text(t)
        enc = tokenizer.encode(t)
        results.append(BatchCountItem(
            token_count=len(enc.ids),
            token_ids=enc.ids if req.return_tokens else None
        ))
        
    return BatchCountResponse(results=results)

@app.get("/health")
async def health():
    return {
        "status": "ok", 
        "tokenizer_path": TOKENIZER_PATH,
        "cache_enabled": False
    }
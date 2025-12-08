# token_server.py
from fastapi import FastAPI, HTTPException, Body
from pydantic import BaseModel
from tokenizers import Tokenizer
from typing import List, Optional
import hashlib
import time
import os
import json
import asyncio
import logging

# Optional Redis async client
import redis.asyncio as aioredis
from redis.exceptions import RedisError

log = logging.getLogger("uvicorn.error")

# Configuration
TOKENIZER_PATH = os.environ.get("TOKENIZER_PATH", "tokenizer.json")
SERVER_TITLE = "Fast Token Server"
MAX_TEXT_LENGTH = int(os.environ.get("MAX_TEXT_LENGTH", 200_000))
CACHE_TTL_SECONDS = int(os.environ.get("CACHE_TTL_SECONDS", 60 * 5))
REDIS_URL = os.environ.get("REDIS_URL", None)  # e.g. redis://redis:6379/0
USE_REDIS = bool(REDIS_URL)

# Simple in-memory cache (synchronous)
class SimpleCache:
    def __init__(self):
        self._store = {}

    def get(self, key):
        item = self._store.get(key)
        if not item:
            return None
        value, expiry = item
        if expiry < time.time():
            del self._store[key]
            return None
        return value

    def set(self, key, value, ttl=CACHE_TTL_SECONDS):
        self._store[key] = (value, time.time() + ttl)

# Async Redis-backed cache wrapper (JSON-serializes values)
class RedisCache:
    def __init__(self, redis_client, ttl=CACHE_TTL_SECONDS):
        self.redis = redis_client
        self.ttl = ttl

    async def get(self, key):
        try:
            raw = await self.redis.get(key)
            if raw is None:
                return None
            # stored as JSON bytes
            return json.loads(raw)
        except RedisError as e:
            log.error("Redis get error: %s", e)
            return None

    async def set(self, key, value, ttl=None):
        try:
            t = ttl or self.ttl
            await self.redis.set(key, json.dumps(value, ensure_ascii=False), ex=t)
        except RedisError as e:
            log.error("Redis set error: %s", e)

# Instantiate caches (in-memory always available; redis optional)
memory_cache = SimpleCache()
redis_cache = None  # will be RedisCache instance if REDIS_URL set

app = FastAPI(title=SERVER_TITLE)

# Load tokenizer
try:
    tokenizer = Tokenizer.from_file(TOKENIZER_PATH)
except Exception as e:
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
def text_hash(text: str) -> str:
    return hashlib.sha256(text.encode("utf-8")).hexdigest()

def guard_text(text: str):
    if not isinstance(text, str):
        raise HTTPException(status_code=400, detail="text must be a string")
    # if len(text) > MAX_TEXT_LENGTH:
    #     raise HTTPException(status_code=413, detail=f"text too long, max {MAX_TEXT_LENGTH} chars")

async def cache_get(key):
    """
    Try Redis if available, otherwise in-memory cache.
    If Redis present but fails, will fallback to memory.
    """
    if redis_cache:
        try:
            res = await redis_cache.get(key)
            if res is not None:
                return res
        except Exception as e:
            log.error("Redis get fallback error: %s", e)
    return memory_cache.get(key)

async def cache_set(key, value, ttl=CACHE_TTL_SECONDS):
    # store in both redis (if present) and memory cache for fast local hits
    memory_cache.set(key, value, ttl)
    if redis_cache:
        try:
            await redis_cache.set(key, value, ttl)
        except Exception as e:
            log.error("Redis set fallback error: %s", e)

# Startup / shutdown events to connect/disconnect redis
@app.on_event("startup")
async def on_startup():
    global redis_cache
    if USE_REDIS:
        try:
            r = aioredis.from_url(REDIS_URL, encoding="utf-8", decode_responses=False)
            # quick ping to ensure connection
            await r.ping()
            redis_cache = RedisCache(r, ttl=CACHE_TTL_SECONDS)
            log.info("Connected to Redis at %s", REDIS_URL)
        except Exception as e:
            # if cannot connect, keep redis_cache = None and use memory cache
            log.error("Failed to connect to Redis (%s): %s. Falling back to in-memory cache.", REDIS_URL, e)
            redis_cache = None

@app.on_event("shutdown")
async def on_shutdown():
    global redis_cache
    if redis_cache:
        try:
            await redis_cache.redis.close()
            await redis_cache.redis.connection_pool.disconnect()
            log.info("Redis connection closed")
        except Exception:
            pass

# Endpoints
@app.post("/count", response_model=CountResponse)
async def count(req: CountRequest = Body(...)):
    guard_text(req.text)
    key = f"count:{text_hash(req.text)}:{req.return_tokens}"
    cached = await cache_get(key)
    if cached is not None:
        # cached is a dict like {"token_count": N, "token_ids": [...] or None}
        return CountResponse(token_count=cached["token_count"], token_ids=cached.get("token_ids"))

    # encode with tokenizers
    encoding = tokenizer.encode(req.text)
    token_ids = encoding.ids if req.return_tokens else None
    resp = {"token_count": len(encoding.ids), "token_ids": token_ids}
    # set cache (async)
    await cache_set(key, resp)
    return CountResponse(token_count=resp["token_count"], token_ids=resp["token_ids"])

@app.post("/batch_count", response_model=BatchCountResponse)
async def batch_count(req: BatchCountRequest = Body(...)):
    if not isinstance(req.texts, list):
        raise HTTPException(status_code=400, detail="texts must be a list of strings")
    if len(req.texts) == 0:
        raise HTTPException(status_code=400, detail="texts list is empty")
    if len(req.texts) > MAX_TEXT_LENGTH:
        raise HTTPException(status_code=413, detail="too many texts in one batch")

    results = []
    # We will process sequentially here; for large batches you may parallelize carefully
    for t in req.texts:
        guard_text(t)
        key = f"count:{text_hash(t)}:{req.return_tokens}"
        cached = await cache_get(key)
        if cached is not None:
            results.append(BatchCountItem(token_count=cached["token_count"], token_ids=cached.get("token_ids")))
            continue
        enc = tokenizer.encode(t)
        item = {"token_count": len(enc.ids), "token_ids": enc.ids if req.return_tokens else None}
        await cache_set(key, item)
        results.append(BatchCountItem(token_count=item["token_count"], token_ids=item["token_ids"]))
    return BatchCountResponse(results=results)

@app.get("/health")
async def health():
    return {"status": "ok", "tokenizer_path": TOKENIZER_PATH, "use_redis": bool(redis_cache)}

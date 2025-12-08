
1. Prepare tokenizer.json
   - Download the tokenizer.json for the model you need (from HuggingFace or model repo).
   - Example: place it next to the code as `tokenizer.json`.

2. Local run (no Docker)

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
TOKENIZER_PATH=./tokenizer.json uvicorn token_server:app --host 0.0.0.0 --port 8000
```

3. Docker

```bash
# build
docker build -t fast-token-server:latest .
# run, mount tokenizer.json
docker run --rm -p 8000:8000 -v $(pwd)/tokenizer.json:/app/tokenizer.json \
  -e TOKENIZER_PATH=/app/tokenizer.json fast-token-server:latest
```

4. Example HTTP request (curl)

```bash
curl -X POST http://localhost:8000/count -H 'Content-Type: application/json' \
  -d '{"text": "Xin chao the gioi", "return_tokens": false}'

# Batch
curl -X POST http://localhost:8000/batch_count -H 'Content-Type: application/json' \
  -d '{"texts": ["Hello world", "Xin chào"], "return_tokens": false}'
```

5. C# client example (HttpClient)

```csharp
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class TokenClientExample
{
    static async Task Main()
    {
        var http = new HttpClient { BaseAddress = new Uri("http://localhost:8000") };
        var payload = new { text = "Xin chào thế giới", return_tokens = false };
        var resp = await http.PostAsync("/count", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        Console.WriteLine(json);
    }
}
```

---

# Production notes & best practices

- **Use Redis** to replace the in-memory cache for multi-instance scaling.
- **Run multiple uvicorn workers** behind a process manager (Gunicorn with Uvicorn workers) or container orchestrator. For tokenizers, single-process with concurrency is usually enough because encoding is CPU-bound.
- **Batching**: use `/batch_count` to reduce overhead when measuring many small texts.
- **Rate-limiting**: protect endpoints (e.g., nginx + rate limiting) to avoid misuse.
- **Security**: validate payload size and sanitize inputs. Do not accept arbitrary files.
- **Monitoring**: export metrics (Prometheus) for request latency and counts.

---

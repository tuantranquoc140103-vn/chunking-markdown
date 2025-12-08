
# File: Dockerfile
# Small Dockerfile using Python slim base. The resulting image is compact because we don't install torch.

FROM python:3.11-slim

# Create app dir
WORKDIR /app

# Install build deps for tokenizers wheel if needed. On many images tokenizers has wheels prebuilt.
# Only include minimal packages
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    curl \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY token_server.py .
# COPY token_cli.py .

# Expect tokenizer.json to be mounted or copied into /app/tokenizer.json
ENV TOKENIZER_PATH=/app/tokenizer.json

EXPOSE 8000

CMD ["uvicorn", "token_server:app", "--host", "0.0.0.0", "--port", "8000", "--workers", "1"]

# Document Management System

Central document repository for the application ecosystem. **Port: 7009**.

## Phase 1 — Foundation
- ASP.NET Core MVC on .NET 10
- EF Core + SQLite metadata database
- Docker Compose deployment
- Persistent volume for database and document binaries
- Health endpoint at `/health`

## Phase 2 — Document repository
- Upload, list, search, details, preview and download
- Title, description and category metadata
- Extension and file-signature validation
- Generated storage keys; files stored outside web root
- SHA-256 integrity hash
- Soft-delete metadata with physical cleanup attempt

## Run
```bash
docker compose up --build -d
```
Open http://localhost:7009 and check http://localhost:7009/health.

## Storage
- SQLite: `/app/data/documents.db`
- Files: `/app/data/documents/{generated-key}`
- Docker volume: `document_management_data`

Supported uploads: PDF, DOCX, XLSX, PPTX, TXT, CSV, PNG, JPG, JPEG. Default maximum: 50 MB.

## Security notes
Files are not served from the static web root. Uploads use generated storage names and basic signature checks. This initial release is intended for a trusted LAN only; do not expose it publicly. Authentication/authorization, malware scanning, rate limiting, durable backup/restore, and formal EF migrations are required before production exposure. Office file checks currently verify the ZIP container signature, not package safety.

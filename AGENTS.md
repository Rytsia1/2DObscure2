# AGENTS.md — Agent Security & Git Commit Guidelines

Dokumen ini merupakan panduan instruksi dan aturan mutlak (**Strict Guidelines**) bagi seluruh AI Agent (Antigravity, Claude, Copilot, Cursor, dan tools agen otomatis lainnya) yang beroperasi pada repositori proyek **2DObscure**.

---

## 1. Aturan Mutlak: Larangan Commit Berkas Sensitif & Internal

Setiap agen **DILARANG KERAS** melakukan staging atau commit terhadap berkas-berkas yang termasuk dalam kategori di bawah ini:

### A. Agent Skills, Workspace, & Internal AI Configs
- Direktori konfigurasi agen: `.gemini/`, `.claude/`, `.cursor/`, `.windsurf/`, `.antigravity/`, `.agent/`, `.agents/`
- Folder skill agen atau custom skills (contoh: `skills/`, `custom_skills/`, `*skill*`)
- File metadata proyek agen internal: `project.yaml`, `.system_generated/`, workspace cache, scratchpad scripts/files.
- File percakapan, transcript, atau prompt scratch files.

### B. Environment, Kunci Rahasia, & API Keys
- File environment: `.env`, `.env.*`, `*.env`, `env.json`
- API keys, token autentikasi, access tokens, dan bearer tokens.
- Berkas kredensial & secrets: `credentials.json`, `secrets.json`, `service-account*.json`, `jwt*.key`
- Private keys & certificates: `*.pem`, `*.key`, `*.cert`, `*.pfx`, `*.keystore`

### C. Dependencies & Package Caches
- Node.js: `node_modules/`, `package-lock.json` (jika tidak diminta spesifik)
- Python: `__pycache__/`, `*.pyc`, `*.pyo`, `.venv/`, `venv/`, `env/`, `.pytest_cache/`
- Unity & C# build artifacts: `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `bin/`, `obj/`
- Package managers local caches: `.nuget/`, `packages/`

### D. File Uji Coba, Log, & Temporary Artifacts
- Test runner output: `TestResults.txt`, `TestResults.xml`, `TestResults/`
- Crash reports, profiling cache, dan debug logs: `*.log`, `sysinfo.txt`
- OS metadata files: `Thumbs.db`, `.DS_Store`, `desktop.ini`

---

## 2. Prosedur Git Workflow untuk Agen

Sebelum menjalankan operasi git (`git add` dan `git commit`), agen **WAJIB** mengikuti prosedur berikut:

1. **Dilarang Menggunakan Wildcard Add:**
   - **JANGAN PERNAH** menjalankan `git add .` atau `git add -A`.
   - Hanya lakukan staging file secara eksplisit dan selektif satu per satu (contoh: `git add Assets/Scripts/Run/RunState.cs`).

2. **Periksa Status Repositori:**
   - Selalu jalankan `git status` sebelum melakukan commit.
   - Pastikan tidak ada file berkategori sensitif atau file temporary (seperti `.claude/`, `TestResults.txt`, `project.yaml`) yang masuk dalam staging area.

3. **Verifikasi Diff (Pre-Commit Review):**
   - Lakukan peninjauan `git diff --cached` untuk memastikan:
     - Tidak ada hardcoded API key, token, URL berotentikasi, atau password yang tertulis dalam source code.
     - Hanya perubahan terkait tugas pengguna yang di-stage.

4. **Tindakan Jika Menemukan File Sensitif:**
   - Jika terdapat file sensitif yang tidak sengaja ter-stage, segera unstage menggunakan:
     ```bash
     git restore --staged <path-to-file>
     ```
   - Masukkan file/pola tersebut ke `.gitignore` agar tidak terdeteksi kembali.

---

## 3. Sinkronisasi dengan `.gitignore`

Aturan pada dokumen ini didukung langsung oleh berkas `.gitignore` di root proyek. Jika ada format file konfigurasi agen, dependency baru, atau file lingkungan baru yang ditambahkan ke proyek, agen harus memastikan polanya telah tercantum pada `.gitignore`.


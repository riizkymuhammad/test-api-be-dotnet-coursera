# Sistem Perpustakaan — ASP.NET Core API

API untuk mengelola anggota perpustakaan, katalog buku, peminjaman, dan pengembalian. Dibuat untuk proyek **Back-End Development with .NET** di Coursera. Anggota menggunakan endpoint `/api/users` agar CRUD pengguna yang diminta rubrik tetap terlihat jelas.

## Teknologi

- .NET 10 / ASP.NET Core Web API dengan controllers.
- Entity Framework Core dan SQLite; data bertahan setelah aplikasi dimatikan.
- Data Annotations untuk validasi otomatis dengan respons HTTP 400.
- Middleware pencatatan request dan penanganan exception.
- xUnit dan `WebApplicationFactory` untuk pengujian integrasi menggunakan SQLite terpisah pada setiap pengujian.
- OpenAPI dan GitHub Actions untuk dokumentasi serta pemeriksaan build/test.

## Menjalankan proyek

Pasang [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), kemudian dari direktori repository:

```bash
dotnet restore Library.slnx
dotnet build Library.slnx --configuration Release --no-restore
dotnet test Library.slnx --configuration Release --no-build
dotnet run --project Library.Api
```

Alamat lokal: **http://localhost:5080**. Dokumen OpenAPI tersedia di **http://localhost:5080/openapi/v1.json**, dan pemeriksaan proses tersedia di `/health`.

Database `library.db` dibuat otomatis pada direktori kerja aplikasi ketika pertama kali dijalankan. Database mulai kosong. File database, hasil build, dan hasil pengujian diabaikan Git. Untuk database di lokasi lain, atur connection string melalui environment variable:

```bash
ConnectionStrings__Library='Data Source=/tmp/perpustakaan.db;Foreign Keys=True' dotnet run --project Library.Api
```

Proyek ini menggunakan `EnsureCreated` untuk inisialisasi skema tugas. Perubahan skema pada deployment yang sudah menyimpan data memerlukan EF Core migrations. API ini adalah demo lokal tanpa autentikasi; gunakan data contoh selama penilaian.

## Endpoint

| Metode | Endpoint | Fungsi | Respons berhasil |
| --- | --- | --- | --- |
| GET | `/api/users` | Daftar anggota | 200 |
| GET | `/api/users/{id}` | Detail anggota | 200 |
| POST | `/api/users` | Tambah anggota | 201 + Location |
| PUT | `/api/users/{id}` | Ubah anggota | 200 |
| DELETE | `/api/users/{id}` | Hapus anggota tanpa riwayat pinjaman | 204 |
| GET | `/api/books` | Daftar buku beserta stok tersedia | 200 |
| GET | `/api/books/{id}` | Detail buku | 200 |
| POST | `/api/books` | Tambah buku | 201 + Location |
| PUT | `/api/books/{id}` | Ubah buku dan jumlah salinan | 200 |
| DELETE | `/api/books/{id}` | Hapus buku tanpa riwayat pinjaman | 204 |
| GET | `/api/loans` | Daftar riwayat peminjaman | 200 |
| GET | `/api/loans/{id}` | Detail peminjaman | 200 |
| POST | `/api/loans` | Pinjam buku | 201 + Location |
| PUT | `/api/loans/{id}/return` | Kembalikan buku | 200 |

Data tidak ditemukan menghasilkan **404**. Email/ISBN duplikat dan konflik aturan peminjaman menghasilkan **409**. Body tidak valid atau JSON rusak menghasilkan **400**. Error menggunakan format `application/problem+json`.

### Contoh alur peminjaman

Jalankan pada database kosong. Gunakan ID dari respons jika database sudah berisi data.

```bash
curl -i -X POST http://localhost:5080/api/users \
  -H 'Content-Type: application/json' \
  -d '{"fullName":"Budi Santoso","email":"budi@example.com"}'

curl -i -X POST http://localhost:5080/api/books \
  -H 'Content-Type: application/json' \
  -d '{"title":"Clean Code","author":"Robert C. Martin","isbn":"9780132350884","totalCopies":2}'

curl -i -X POST http://localhost:5080/api/loans \
  -H 'Content-Type: application/json' \
  -d '{"userId":1,"bookId":1,"durationDays":14}'

curl -i -X PUT http://localhost:5080/api/loans/1/return
```

Contoh CRUD, validasi gagal, dan konflik juga tersedia di [Library.Api.http](Library.Api/Library.Api.http). Buka dengan REST Client di VS Code atau salin request ke Postman.

## Validasi dan aturan bisnis

- Nama anggota wajib diisi, 2–100 karakter setelah spasi luar dibuang. Email harus valid, maksimal 254 karakter, disimpan sebagai huruf kecil, dan unik tanpa membedakan huruf besar/kecil.
- Judul dan penulis wajib diisi, masing-masing maksimal 200 dan 150 karakter. ISBN unik dan berupa 10 atau 13 digit; validasi ini memeriksa format, bukan checksum ISBN.
- Jumlah salinan 1–10.000. Jumlah tersebut tidak boleh dikurangi di bawah jumlah pinjaman aktif.
- ID anggota/buku harus positif dan merujuk data yang ada. Durasi pinjaman 1–30 hari, default 14 hari.
- Anggota tidak dapat meminjam buku yang sama dua kali bersamaan atau meminjam ketika stok habis.
- Peminjaman dan perubahan stok menggunakan transaksi SQLite agar pemeriksaan stok dan penyimpanan tetap konsisten pada request bersamaan.
- Stok tersedia dihitung dari jumlah salinan dikurangi pinjaman aktif. Pengembalian kedua ditolak agar stok tidak bertambah keliru.
- Riwayat pinjaman tetap disimpan. Anggota dan buku dengan riwayat peminjaman tidak dapat dihapus, termasuk setelah pengembalian.
- Timestamp dan perhitungan tanggal jatuh tempo menggunakan UTC. `isOverdue` hanya berlaku untuk pinjaman aktif yang melewati tanggal jatuh tempo.

## Middleware

`RequestLoggingMiddleware` mencatat metode HTTP, path, status akhir, durasi, dan request ID. Setiap respons memiliki header `X-Request-ID`. Isi body dan query string tidak dicatat.

`ApiExceptionHandler` menangani exception terpusat. Respons 500 tidak membocorkan stack trace atau detail database. Konflik constraint menghasilkan 409; database yang sedang terkunci menghasilkan 503 dengan `Retry-After`. Error response menyertakan `traceId` untuk dicocokkan dengan log.

## Pengujian

```bash
dotnet test Library.slnx --configuration Release
```

Pengujian mencakup CRUD, respons Location, validasi input, email/ISBN duplikat, data yang tidak ditemukan, stok buku, pengembalian berulang, riwayat pinjaman, transaksi peminjaman bersamaan, keterlambatan, JSON rusak, request ID, penanganan exception, persistensi SQLite, dan dokumen OpenAPI. Setiap pengujian menggunakan file database sementara sendiri dan membersihkannya setelah selesai.

Verifikasi lokal pada 8 November 2026: build Release tanpa warning/error, **35 pengujian lulus**.

## Pemetaan rubrik Coursera

| Kriteria | Bukti / tindakan |
| --- | --- |
| Repository GitHub (5 poin) | Repository `riizkymuhammad/test-api-be-dotnet-coursera` sudah tersedia dan terverifikasi **public**. Pastikan kode sudah di-push sebelum mengirim URL. |
| CRUD pengguna (5 poin) | `Library.Api/Controllers/UsersController.cs`: GET, POST, PUT, DELETE. |
| Debugging dengan Copilot (5 poin) | Ikuti [panduan debugging](docs/COPILOT_DEBUGGING.md) dan lengkapi catatan berdasarkan penggunaan GitHub Copilot yang benar-benar dilakukan. |
| Validasi (5 poin) | `Library.Api/Contracts/Requests.cs`, `[ApiController]`, pemeriksaan konflik, constraint SQLite, dan pengujian integrasi. |
| Middleware (5 poin) | `Library.Api/Middleware/RequestLoggingMiddleware.cs`, `ApiExceptionHandler.cs`, dan registrasi di `Program.cs`. |

Lihat [checklist pengumpulan](docs/SUBMISSION.md). Kode dan dokumen proyek ini dibantu Codex; hal tersebut tidak dengan sendirinya membuktikan penggunaan GitHub Copilot.

## Referensi

- [ASP.NET Core Web API](https://learn.microsoft.com/aspnet/core/web-api/?view=aspnetcore-10.0)
- [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)
- [GitHub: membuat repository](https://docs.github.com/en/repositories/creating-and-managing-repositories/quickstart-for-repositories?tool=webui)

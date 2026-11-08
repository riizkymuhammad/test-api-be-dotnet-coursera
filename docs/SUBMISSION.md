# Checklist pengumpulan

- [x] Build Release berhasil tanpa warning/error; seluruh 35 pengujian integrasi lulus pada 8 November 2026.
- [ ] Jalankan API dan coba CRUD `/api/users` menggunakan file `.http` atau Postman.
- [ ] Tunjukkan HTTP 400 pada input tidak valid dan header `X-Request-ID` pada respons.
- [ ] Gunakan GitHub Copilot untuk debugging/review dan lengkapi `COPILOT_DEBUGGING.md` dengan bukti sebenarnya.
- [ ] Commit dan push seluruh kode ke GitHub.
- [x] Repository tujuan terverifikasi **public** melalui API GitHub tanpa autentikasi.
- [ ] Periksa hasil GitHub Actions setelah push.
- [ ] Kirim URL repository pada halaman peer review Coursera.

Remote yang dikonfigurasi pada workspace:

```text
git@github.com:riizkymuhammad/test-api-be-dotnet-coursera.git
```

URL yang akan dikumpulkan setelah upload dan akses publik dikonfirmasi:

```text
https://github.com/riizkymuhammad/test-api-be-dotnet-coursera
```

Jika belum di-commit, jalankan dari direktori proyek:

```bash
git add .
git commit -m "Initial commit: library management API"
git push -u origin main
```

Jika Git meminta identitas author, atur `git config user.name` dan `git config user.email` menggunakan identitas Anda sebelum commit. Pastikan repository di GitHub sudah dibuat. Jika perlu mengubah visibilitas, gunakan pengaturan repository sesuai [dokumentasi GitHub](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/managing-repository-settings/setting-repository-visibility).

Deskripsi singkat untuk peer review:

> This library management API uses ASP.NET Core and SQLite. It includes full user CRUD endpoints, book CRUD, borrowing and returns, input validation, duplicate email and ISBN checks, request logging middleware, centralized exception handling, and integration tests. Please see README.md for setup instructions and rubric mapping. Evidence of GitHub Copilot debugging is documented separately once completed.

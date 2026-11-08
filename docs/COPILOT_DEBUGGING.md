# Bukti debugging menggunakan GitHub Copilot

Bagian rubrik ini menilai penggunaan **GitHub Copilot**, sehingga tidak cukup hanya memiliki kode yang lulus pengujian. Catatan ini adalah panduan dan template; belum merupakan bukti bahwa Copilot telah dipakai.

## Langkah yang dapat dilakukan

1. Buka repository di VS Code dengan GitHub Copilot aktif.
2. Jalankan `dotnet test Library.slnx` untuk melihat perilaku awal.
3. Pilih kode dan pengujian yang berkaitan dengan masalah yang ingin ditinjau. Misalnya validasi nama setelah trim, konflik email berbeda kapital, atau stok buku pada request bersamaan.
4. Minta Copilot menjelaskan kemungkinan bug dan saran perbaikannya. Contoh prompt:

   > Review UsersController, UserRequest, dan pengujian validasi anggota. Apakah nama yang berisi spasi atau email yang berbeda kapital dapat melewati aturan? Jelaskan kasus reproduksi dan usulkan perbaikan hanya jika ada masalah nyata.

   > Review transaksi dalam LoansController. Apakah dua anggota dapat meminjam satu salinan terakhir secara bersamaan? Jelaskan bukti dari pengujian Concurrent_borrowing_never_overbooks_the_last_copy dan usulkan perbaikan jika diperlukan.

5. Tinjau saran Copilot. Terapkan hanya perbaikan yang diperlukan; jangan menyatakan masalah yang tidak benar-benar ditemukan.
6. Jalankan ulang pengujian terkait, lalu seluruh suite. Simpan percakapan atau screenshot sesuai ketentuan kelas.
7. Isi catatan berikut dengan hasil sebenarnya. Jika tidak ditemukan bug, catat hasil review tersebut secara jujur dan gunakan aktivitas debugging sebelumnya jika tersedia.

## Catatan yang harus diisi peserta

- **Tanggal penggunaan Copilot:** belum diisi.
- **File dan masalah yang ditinjau:** belum diisi.
- **Prompt yang diberikan:** belum diisi.
- **Gejala / kasus reproduksi:** belum diisi.
- **Penjelasan dan saran Copilot:** belum diisi.
- **Perbaikan yang diterapkan dan alasannya:** belum diisi.
- **Perintah pengujian dan hasil sesudah perbaikan:** belum diisi.
- **Bukti percakapan / screenshot / commit terkait:** belum diisi.

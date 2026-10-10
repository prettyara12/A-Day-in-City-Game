# 🏙️ Sunny Sidewalk (Little Town 3D Exploration)
> **Tugas Praktikum / Proyek Mata Kuliah: Introduction to Game Development**  
> Proyek simulasi dan eksplorasi lingkungan kota 3D dengan karakter pemain sudut pandang orang ketiga (*Third-Person*), NPC otonom mandiri dengan kecerdasan buatan (*AI Obstacle Avoidance*), serta sistem balon dialog dan subtitle beresolusi tinggi.

---

## 📌 Daftar Isi
1. [Tentang Proyek](#-tentang-proyek)
2. [Spesifikasi Teknis](#-spesifikasi-teknis)
3. [Fitur Utama](#-fitur-utama)
4. [Panduan Kontrol Pemain](#-panduan-kontrol-pemain)
5. [Daftar Karakter & Perilaku NPC](#-daftar-karakter--perilaku-npc)
6. [Arsitektur Kode & Struktur Skrip](#-arsitektur-kode--struktur-skrip)
7. [Sistem UI & Ketajaman Teks Dialog](#-sistem-ui--ketajaman-teks-dialog)
8. [Panduan Menjalankan Proyek (Getting Started)](#-panduan-menjalankan-proyek-getting-started)
9. [Menu Editor & Otomasi Setup](#-menu-editor--otomasi-setup)
10. [Troubleshooting & Optimasi](#-troubleshooting--optimasi)

---

## 📖 Tentang Proyek
Proyek ini dikembangkan sebagai bagian dari pembelajaran mata kuliah **Introduction to Game Development**. Fokus utama proyek ini meliputi:
- **Pengembangan Karakter Utama (Player Controller):** Kendali pergerakan 3D yang halus mencakup berjalan, berlari, melompat, serta duduk di fasilitas kota (bangku taman).
- **Kamera Cerdas (Third-Person Follow Camera):** Kamera dinamis yang mengikuti arah belakang pemain (*rear follow*), mendukung rotasi bebas dengan mouse, zoom dinamis dengan scroll wheel, serta fitur pendeteksi tabrakan dinding (*collision obstruction damping*).
- **Kecerdasan Buatan NPC (Autonomous AI):** NPC yang beraktivitas secara otonom di trotoar kota tanpa campur tangan pemain, dilengkapi sensor *raycast* anti-tabrakan (*obstacle avoidance*) dan kemampuan interaksi responsif saat pemain mendekat.
- **Sistem Dialog Modern (Screen-Space Dialogue Overlay):** Dialog dwitampilan berupa balon percakapan melayang yang mengikuti kepala karakter secara dinamis di layar (*3D-to-2D projection*) dan panel subtitle sinematik di bawah layar dengan tipografi vektor tebal (*Roboto-Bold*) beresolusi tinggi.

---

## ⚙️ Spesifikasi Teknis

| Parameter | Spesifikasi |
|---|---|
| **Game Engine** | Unity 6 (`6000.6.2f1`) |
| **Bahasa Pemrograman** | C# (.NET Standard / Unity Core) |
| **Render Pipeline** | Universal Render Pipeline (URP) / Standard 3D |
| **Gaya Visual** | Low-Poly Casual 3D Urban Environment |
| **Target Platform** | PC / Desktop (Windows) |
| **Scene Utama** | `Assets/Module 2.unity` |
| **Asset Pack** | Synty Studios - *SimpleWorld Vol. 1 / SimplePeople* |
| **Font Utama** | `Roboto-Bold.ttf` (dengan *fallback* dinamis OS font & LegacyRuntime) |

---

## ✨ Fitur Utama

### 1. 🎮 Player Controller Fisika Lengkap
- **Movement Physics:** Menggunakan komponen `Rigidbody` dan `CapsuleCollider` dengan penanganan lereng, gravitasi stabil, dan deteksi daratan (*ground check*).
- **Animasi Fleksibel:** Terintegrasi penuh dengan Animator Controller (`SimpleCharacter_5.0.controller`), mendukung transisi mulus antara status *Idle*, *Walk*, *Run*, *Jump*, dan *Sitting*.
- **Interaksi Lingkungan:** Pemain dapat duduk dan berdiri kembali di bangku taman kota dengan menekan tombol **[C]** atau **[X]**.

### 2. 🎥 Third-Person Camera dengan Rear-Follow Dinamis
- **Auto Rear Alignment:** Kamera secara dinamis menyelaraskan sudut pandangnya di belakang punggung pemain saat bergerak maju, memberikan orientasi navigasi yang alami.
- **Mouse Orbit & Clamp:** Kontrol pandangan bebas 360° secara horizontal dan pembatasan sudut elevasi vertikal (-20° hingga +60°) agar kamera tidak menembus aspal atau tanah.
- **Obstacle Avoidance Zoom:** Jarak kamera otomatis menyesuaikan (*smooth zoom-in*) jika terdapat bangunan atau objek yang menghalangi pandangan antara kamera dan pemain.
- **Toggle Mode Kamera:** Tombol **[V]** memungkinkan pemain berpindah instan antara Kamera Mengikuti Pemain (*Third-Person Follow*) dan Kamera Pemandangan Kota (*City Overview*).

### 3. 🤖 NPC Cerdas & Mandiri (Autonomous AI)
- **Sensor Anti-Tabrakan (*Obstacle Avoidance*):** NPC pejalan kaki dilengkapi detektor *Raycast* dan *SphereCast* tiga arah (depan, serong kiri, serong kanan) untuk menghindari tabrakan dengan pohon, tiang, bangku, atau pejalan kaki lain.
- **Interaksi Sapaan Ramah:** Saat pemain mendekat dalam radius interaksi (1.8m), NPC akan berhenti, melambaikan tangan (*Wave animation*), menghadap ke arah pemain, dan memunculkan dialog sapaan.
- **Dukungan Berbagai Mode Navigasi:**
  - *Stationary Jumping:* Berdiri melompat di tempat secara berkala untuk olahraga.
  - *Stationary Look Around:* Berdiri di depan pertokoan sambil menengok kiri-kanan secara alami.
  - *Directional / Waypoint Walk:* Berjalan menelusuri trotoar berlawanan arah dengan arah pemain.

### 4. 💬 Sistem Dialog Resolusi Tinggi (*High-Definition Overlay*)
- **Screen-Space Projection:** Mengatasi kelemahan *World-Space Canvas* yang sering buram atau berbayang di sudut 3D.
- **Balon Kepala Melayang (*Overhead Speech Bubble*):** Menghitung proyeksi posisi kepala karakter di dunia 3D ke koordinat layar 2D secara *real-time*, melayang tepat di atas kepala tanpa menutupi wajah.
- **Kartu Subtitle Sinematik (*Bottom Dialogue Bar*):** Panel gelap elegan di bawah layar dengan aksen biru *cyan*, nama pembicara berwarna emas bercahaya (`◆ Nama Pembicara`), dan teks dialog putih berbayang kontras tinggi.
- **Supersampling 2.0x:** Dikonfigurasi dengan `dynamicPixelsPerUnit = 2f` pada resolusi kanvas referensi `1280 × 720` sehingga glif teks selalu tajam kristal tanpa efek pikselasi.
- **Resilient Lifecycle:** Tahan terhadap opsi *Fast Enter Play Mode* (Reload Domain / Reload Scene disabled) tanpa menyebabkan kebocoran memori (*memory leak*).

---

## ⌨️ Panduan Kontrol Pemain

| Tombol / Input | Fungsi |
|---|---|
| **W, A, S, D** / **Tombol Panah** | Menggerakkan karakter (Maju, Mundur, Kiri, Kanan) |
| **Shift Kiri** (Tahan) | Berlari cepat (*Sprint*) |
| **Spasi** | Melompat (*Jump*) |
| **C** atau **X** | Duduk di bangku terdekat / Berdiri kembali |
| **Geser Mouse** | Memutar sudut pandang kamera (*Orbit Camera*) |
| **Scroll Wheel Mouse** | Memperbesar / Memperkecil jarak kamera (*Zoom In / Zoom Out*) |
| **V** | Beralih antara Kamera Mengikuti Pemain dan Kamera Kota (*Overview*) |

---

## 👥 Daftar Karakter & Perilaku NPC

| Karakter | Lokasi di Scene | Perilaku & Interaksi | Teks Dialog |
|---|---|---|---|
| **Player (Trucker)** | Trotoar Kota | Dikendalikan pemain dengan keyboard dan mouse. | - |
| **Wanita Melompat** | Trotoar Timur (dekat air mancur) | Melompat di tempat secara terus-menerus (*Jumping in Place*). Menyapa saat pemain mendekat. | *"Halo! Lihat, aku melompat di tempat! Asyik sekali olahraga di trotoar ini!"* |
| **Pria Pejalan Kaki** | Trotoar Barat | Berjalan kaki otonom melawan arah pemain (ke arah Selatan) dengan sensor anti-tabrakan. Berhenti & melambai saat didekati. | *"Halo bro! Selamat beraktivitas di kota. Tetap semangat!"* |
| **Pria Barber Shop** | Depan Barber Shop | Berdiri diam di tempat sambil menengok kiri-kanan secara alami. Menyapa jika didekati. | *"Halo! Saya sedang menunggu antrean potong rambut di Barber Shop ini."* |

---

## 📂 Arsitektur Kode & Struktur Skrip

Seluruh skrip diletakkan di direktori [`Assets/Scripts/`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20%281%29/Assets/Scripts):

```text
Assets/Scripts/
├── PlayerController.cs        # Logika fisika pergerakan, lompatan, duduk, dan kontrol animasi Player
├── ThirdPersonCamera.cs       # Sistem kamera orang ketiga, mouse look, rear alignment, dan zoom
├── NPCWalker.cs               # AI NPC otonom: navigasi jalan, melompat, menengok, sensor rintangan
├── NPCDialogueOverlay.cs      # Sistem kanvas overlay dialog beresolusi tinggi (Overhead & Subtitle)
├── NPCWaypointPath.cs         # Komponen rute waypoint visual di scene untuk jalur patroli NPC
├── PlayerBootstrap.cs         # Inisialisasi otomatis scene saat Play, setup collider, & perbaikan material
└── Editor/
    ├── NPCSetupWizard.cs      # Wizard Editor Unity untuk konfigurasi NPC otomatis
    └── NPCWalkerEditor.cs     # Custom Inspector editor untuk visualisasi NPC & waypoint
```

### Rincian Fungsi Utama Tiap Skrip:
- **[`PlayerController.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20%281%29/Assets/Scripts/PlayerController.cs):**
  Mengatur kalkulasi vektor pergerakan berdasarkan orientasi kamera, lerp rotasi karakter, eksekusi lompatan berbasis kurva fisika, serta transisi animasi parameter integer `Animation_type`.
- **[`ThirdPersonCamera.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20%281%29/Assets/Scripts/ThirdPersonCamera.cs):**
  Menggunakan `Vector3.SmoothDamp` dan `Quaternion.Slerp` untuk rotasi mulus bebas *jitter*. Mengintegrasikan `followRearAngle` yang secara cerdas mendeteksi arah input gerak pemain untuk mengarahkan kamera ke belakang punggung.
- **[`NPCWalker.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20%281%29/Assets/Scripts/NPCWalker.cs):**
  Memiliki arsitektur state machine (`NPCState`: *Idle*, *Walking*, *Sitting*, *InteractingWithPlayer*). Dilengkapi coroutine kalkulasi parabola loncatan (`JumpInPlaceRoutine`) dan pemindaian rintangan raycast (`CheckObstacles`).
- **[`NPCDialogueOverlay.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20%281%29/Assets/Scripts/NPCDialogueOverlay.cs):**
  Membangun antarmuka secara programatis melalui kode (*code-driven UI*) dengan komponen `CanvasScaler`, `Image`, `Text`, `Outline`, dan `Shadow`. Menggunakan metode `Camera.WorldToScreenPoint` untuk menempatkan balon tepat di atas kepala target.
- **[`PlayerBootstrap.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20%281%29/Assets/Scripts/PlayerBootstrap.cs):**
  Titik masuk otomatis melalui `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]`. Mengidentifikasi karakter pemain dan NPC di dalam scene, mengaitkan material tekstur yang hilang, serta memasang collider pada bangunan dan trotoar.

---

## 🎨 Sistem UI & Ketajaman Teks Dialog

Sebelumnya teks dialog 3D di Unity sering mengalami masalah buram (*blurry / pixelated*) dan ukuran yang tidak konsisten saat kamera bergerak menjauh atau mendekat. Masalah ini diselesaikan melalui implementasi berikut pada [`NPCDialogueOverlay.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/My%20project%20%281%29/Assets/Scripts/NPCDialogueOverlay.cs):

1. **Resolusi Dasar Kanvas Proporsional:**
   Menggunakan resolusi referensi `1280 × 720` pada `CanvasScaler` dengan mode `ScaleWithScreenSize` dan bobot pembagi seimbang `0.5f` (lebar vs tinggi).
2. **Font Vektor Tebal Berkualitas Tinggi:**
   Menggunakan aset font TrueType `Roboto-Bold.ttf` yang terbukti tajam pada rendering UI modern.
3. **Supersampling 2.0x (`dynamicPixelsPerUnit`):**
   Rasterizer glif Unity dipaksa merender tekstur huruf pada ketajaman 200%, membuat setiap huruf terlihat jernih dan bebas gerigi tanpa membebani performa GPU.
4. **Ukuran yang Proporsional:**
   - **Balon Kepala:** `440 × 88 px` dengan teks `19 pt Bold`.
   - **Kotak Subtitle:** `740 × 85 px` dengan teks nama pembicara `18 pt Bold` (kuning keemasan) dan isi pesan `18 pt Bold` (putih bersih + *drop shadow* pekat).

---

## 🚀 Panduan Menjalankan Proyek (Getting Started)

### Prasyarat
- **Unity Hub** terpasang di komputer Anda.
- **Unity Editor Versi 6000.6.2f1** (Unity 6) atau versi yang kompatibel.

### Langkah-langkah Membuka Proyek
1. Buka **Unity Hub**.
2. Klik tombol **Add** > **Add project from disk**.
3. Pilih direktori root proyek:
   ```text
   d:\Mata Kuliah\Introduction to Game Development\My project (1)
   ```
4. Pastikan versi editor terpilih adalah **Unity 6000.6.2f1**.
5. Buka proyek dan tunggu proses impor aset selesai.
6. Pada panel **Project**, buka scene utama di:
   ```text
   Assets / Module 2.unity
   ```
7. Klik tombol **Play (▶)** di bagian atas editor untuk langsung memainkan game!

---

## 🛠️ Menu Editor & Otomasi Setup

Proyek ini dilengkapi menu kustom di bilah menu Unity (`Tools/`) untuk memudahkan pengujian dan otomasi:

- **`Tools` > `⭐ Setup Game (1 Player WASD + Minimal 2 NPC)`**  
  Menjalankan konfigurasi penuh secara instan: menyiapkan Player Trucker, menyiapkan NPC Wanita Melompat dan NPC Pria Berjalan, memasang material karakter, dan memastikan seluruh collider aktif.
- **`Tools` > `👥 Setup 2 NPC (Wanita Melompat di Tempat & Pria Berjalan Melawan Arah)`**  
  Mengonfigurasi ulang kedua NPC utama di trotoar timur dan barat.
- **`Tools` > `💈 Setup NPC Barber Shop (Menengok Kiri-Kanan)`**  
  Mengonfigurasi NPC penjaga di depan bangunan Barber Shop.
- **`Tools` > `🛍️ Pindahkan Player ke Area Pertokoan (Shopping Street)`**  
  Memindahkan posisi pemain langsung ke trotoar pusat pertokoan untuk pengujian cepat.

---

## 🔧 Troubleshooting & Optimasi

- **Teks Dialog Tidak Muncul Setelah Keluar-Masuk Play Mode:**  
  Jika Unity dikonfigurasi dengan opsi *Enter Play Mode Options (Reload Domain disabled)*, sistem otomatis mereset seluruh variabel statis melalui hook `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` sehingga dialog selalu berfungsi normal setiap kali tombol Play ditekan.
- **Karakter Menembus Tanah / Lantai:**  
  Pastikan `PlayerController` memiliki ground check offset yang pas (0.05m) dan scene memiliki komponen MeshCollider atau BoxCollider pada bidang tanah/trotoar.
- **Kamera Tertutup Objek Bangunan:**  
  Kamera secara otomatis melakukan *smooth dampening* mendekat ke arah pemain jika mendeteksi dinding bangunan di antara posisi kamera dan kepala pemain.

---

## 👨‍💻 Kontributor & Lisensi
- **Mata Kuliah:** Introduction to Game Development
- **Institusi / Jurusan:** Game Development / Ilmu Komputer / Teknik Informatika
- **Lisensi Aset:** Aset 3D perkotaan dan karakter menggunakan paket resmi Synty Studios (*SimpleWorld / SimplePeople*) untuk keperluan edukasi dan praktikum akademik.

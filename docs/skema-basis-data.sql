-- ============================================================
-- TRASHURY — Skema Basis Data
-- Modul 5: Pembuatan Basis Data
--
-- DBMS   : SQLite 3
-- Sumber : dihasilkan Entity Framework Core 8 dari migrasi
--          Trashury.Core/Data/Migrations/20260921090656_InitialCreate.cs
--
-- Berkas ini adalah salinan skema untuk keperluan dokumentasi.
-- Skema sesungguhnya dibuat otomatis lewat migrasi, bukan dengan
-- menjalankan berkas ini secara manual.
-- ============================================================

CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);
CREATE TABLE "KategoriSampah" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_KategoriSampah" PRIMARY KEY AUTOINCREMENT,
    "Nama" TEXT NOT NULL,
    "HargaPerKg" TEXT NOT NULL,
    "FaktorEmisiCO2e" TEXT NOT NULL
);
CREATE TABLE "Nasabah" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Nasabah" PRIMARY KEY AUTOINCREMENT,
    "Nama" TEXT NOT NULL,
    "Saldo" TEXT NOT NULL
);
CREATE TABLE "Transaksi" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Transaksi" PRIMARY KEY AUTOINCREMENT,
    "NasabahId" INTEGER NOT NULL,
    "Tanggal" TEXT NOT NULL,
    "Nominal" TEXT NOT NULL,
    "JenisTransaksi" TEXT NOT NULL,
    "JumlahTarik" TEXT NULL,
    CONSTRAINT "FK_Transaksi_Nasabah_NasabahId" FOREIGN KEY ("NasabahId") REFERENCES "Nasabah" ("Id") ON DELETE RESTRICT
);
CREATE TABLE "DetailSetoran" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DetailSetoran" PRIMARY KEY AUTOINCREMENT,
    "SetoranSampahId" INTEGER NOT NULL,
    "KategoriId" INTEGER NOT NULL,
    "BeratKg" TEXT NOT NULL,
    CONSTRAINT "FK_DetailSetoran_KategoriSampah_KategoriId" FOREIGN KEY ("KategoriId") REFERENCES "KategoriSampah" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_DetailSetoran_Transaksi_SetoranSampahId" FOREIGN KEY ("SetoranSampahId") REFERENCES "Transaksi" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_DetailSetoran_KategoriId" ON "DetailSetoran" ("KategoriId");
CREATE INDEX "IX_DetailSetoran_SetoranSampahId" ON "DetailSetoran" ("SetoranSampahId");
CREATE UNIQUE INDEX "IX_KategoriSampah_Nama" ON "KategoriSampah" ("Nama");
CREATE INDEX "IX_Transaksi_NasabahId" ON "Transaksi" ("NasabahId");
CREATE INDEX "IX_Transaksi_Tanggal" ON "Transaksi" ("Tanggal");

-- ------------------------------------------------------------
-- Data awal (seed) kategori sampah
-- ------------------------------------------------------------

INSERT INTO KategoriSampah (Id, Nama, HargaPerKg, FaktorEmisiCO2e) VALUES (1, 'Plastik PET', 4000.0, 1.5);
INSERT INTO KategoriSampah (Id, Nama, HargaPerKg, FaktorEmisiCO2e) VALUES (2, 'Kertas & Kardus', 2000.0, 0.9);
INSERT INTO KategoriSampah (Id, Nama, HargaPerKg, FaktorEmisiCO2e) VALUES (3, 'Kaleng Aluminium', 15000.0, 8.1);
INSERT INTO KategoriSampah (Id, Nama, HargaPerKg, FaktorEmisiCO2e) VALUES (4, 'Kaca', 500.0, 0.3);
INSERT INTO KategoriSampah (Id, Nama, HargaPerKg, FaktorEmisiCO2e) VALUES (5, 'Organik', 300.0, 0.25);

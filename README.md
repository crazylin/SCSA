# SCSA – Signal Capture & Spectrum Analysis

## Overview
SCSA is a cross-platform signal-acquisition and analysis toolkit built with .NET 8 + Avalonia. It offers:

* Real-time oscilloscope / spectrum viewer (time, frequency, I-Q Lissajous).
* Flexible device interface (TCP/Modbus), supports trigger & filter configuration.
* On-disk data logging – **Binary (`.bin`), WAV (`.wav`), and Universal File Format 58 / 58b (`.uff`)**.
* Post-processing helpers (FFT, windowing, DC removal, peak detect, filtering).
* Built-in reader for generated UFF files for playback / offline analysis.

---

## Device packet protocol
SCSA communicates with DAQ devices over TCP using **PipelineNetDataPackage** frames:
```
Offset  Size  Field
0       4     Magic   (0x4E5A4353 "S C Z N")
4       1     Version
5       1     Device-Cmd
6       2     Cmd-Id (little endian)
8       4     BodyLength (N)
12      N     Payload (parameters / raw samples)
…       4     CRC-32 (IEEE 802.3)
```
See *通讯协议.docx* for full details.

---

## Build & Run
```bash
# Windows / Linux / macOS
> dotnet build -c Release
> dotnet run --project SCSA/SCSA.csproj
```
Self-contained binaries are placed under `SCSA/bin/Release/*`.

---

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| **Avalonia** / **Avalonia.ReactiveUI** | 11.2.x | Cross-platform UI framework (desktop) & ReactiveUI integration |
| **ReactiveUI** | 19.x | MVVM framework with powerful reactive extensions |
| **ReactiveUI.Fody** + **Fody** | 19.x / 6.x | Compile-time weaving – transforms `[Reactive]` auto-properties into fast `INotifyPropertyChanged` code |
| **FluentAvaloniaUI** | 2.x | Fluent Design System controls & styles for Avalonia |
| **OxyPlot.AvaloniaCore** | 2.1.x | High-performance charting / plotting engine |
| **MathNet.Numerics** | 5.x | Numerical algorithms (FFT, window functions, filters) |
| **Microsoft.Extensions.DependencyInjection** | 9.x | Lightweight DI container used throughout the app |

> All packages are referenced via *PackageReference* in the individual project `.csproj` files – no manual installation steps are required.

---
Copyright © 2025 SCSA contributors. Licensed under MIT.
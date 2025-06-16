# SCSA – Signal Capture & Spectrum Analysis

## Overview
SCSA is a cross-platform signal-acquisition and analysis toolkit built with .NET 8 + Avalonia. It offers:

* Real-time oscilloscope / spectrum viewer (time, frequency, I-Q Lissajous).
* Flexible device interface (TCP/Modbus), supports trigger & filter configuration.
* On-disk data logging – **Binary (`.bin`), WAV (`.wav`), and Universal File Format 58 / 58b (`.uff`)**.
* Post-processing helpers (FFT, windowing, DC removal, peak detect, filtering).
* Built-in reader for generated UFF files for playback / offline analysis.

---

## Universal File Format support
SCSA writes Dataset 58 (Function at Nodal DOF) in two flavours:

| Mode | Precision | Description |
|------|-----------|-------------|
| ASCII 58  | 64-bit | 80-column card image, header + `4E20.12` rows |
| Binary 58b| 64-bit or **32-bit** (*optional*) | Same header, binary little-endian samples |

Highlights
1. **Multiple datasets per file** – I/Q are stored sequentially in one file.
2. **Units** – ordinate axis unit (V, mm/s, µm, m/s²) written to Record 11.
3. **Float32 compression** – enable `useFloat32=true` to shrink 58b files ~50 %.
4. Verified with LMS Test.Lab, IDEAS, pyUFF.

### Minimal header layout
```
-1                     (Record 1 start)
58 / 58b …             (Record 2 ID)
<5× ID lines>
Record 6 – DOF (node 1 +X)
Record 7 – DataForm (type, N, even, dt)
Record 8 – Abscissa (Specific 17 = time)
Record 9 – Ordinate (Specific 12 + unit label)
Records 10-11 – zeros
<DATA>
-1                     (end marker)
```

### Reading UFF
```csharp
var series = UFFReader.Read("file.uff");
foreach (var ts in series)
    Console.WriteLine($"Fs={ts.SampleRate}Hz unit={ts.Unit} N={ts.Samples.Length}");
```

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
Copyright © 2025 SCSA contributors. Licensed under MIT.
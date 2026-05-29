# deekta

[![build](https://github.com/JordiCamps/deekta/actions/workflows/build.yml/badge.svg)](https://github.com/JordiCamps/deekta/actions/workflows/build.yml)
![license](https://img.shields.io/badge/license-MIT-blue)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6)
![.NET](https://img.shields.io/badge/.NET-8-512BD4)

**Voice dictation into any Windows app.** A lightweight system‑tray app: press a global
shortcut, speak, press again — the text is transcribed with OpenAI and typed where your cursor is.
The dictation language follows your active keyboard (CA, ES, IT…). UI in English, Catalan,
Spanish, French, Italian and German.

> _Screenshots / demo GIF: TODO._

## Features

- 🎙️ Global hotkey (default **Ctrl + Alt + D**), single‑instance, lives in the tray.
- ⌨️ Types the text directly (Unicode `SendInput`) — works even in apps that ignore `Ctrl+V`.
- 🌐 Transcription language auto‑detected from the active keyboard layout.
- 🪟 Multi‑language UI; modern status overlay with a clickable Settings link.
- 🔒 API key stored encrypted with Windows DPAPI; never in plain text.

## Install

**Option A — download (recommended):** grab the latest from
[Releases](https://github.com/JordiCamps/deekta/releases):
- `deekta-setup.exe` — installer (Start‑menu shortcut, uninstaller), or
- `deekta.exe` — single self‑contained file (no install, no .NET required).

**Option B — build from source:** see [Build from source](#build-from-source).

## How to get an OpenAI API key (step by step)

1. Go to **https://platform.openai.com/api-keys** and sign in (or create an account).
2. Add a payment method under **Billing** — transcription is pay‑as‑you‑go (see pricing below).
3. Click **“Create new secret key”**, give it a name, and **copy** the key (`sk-…`).
   ⚠️ It's shown only once.
4. Open **deekta → Settings**, paste the key, and **Save**.

There's a **“How do I get an OpenAI key?”** link inside the Settings window too.

## Usage

1. Put the cursor where you want to write (Notepad, browser, Word, VS Code…).
2. Press the shortcut (default **Ctrl + Alt + D**) — a red “Recording…” bar appears.
3. Speak.
4. Press it again — the text is transcribed and typed for you.

> If the focused window runs **as administrator**, Windows blocks synthetic typing; deekta then
> leaves the text on the clipboard so you can paste it with `Ctrl+V` (it tells you).

## Configuration

Open Settings from the tray icon, by re‑running `deekta.exe`, or by clicking the overlay's
**⚙ Settings** link.

| Option | Default |
|---|---|
| Model | `gpt-4o-mini-transcribe` |
| Language | Automatic (follows the active keyboard) |
| Shortcut | Ctrl + Alt + D |
| Type automatically | On |
| Start with Windows | Off |
| Sound feedback | On |

- **Settings & logs:** `%AppData%\deekta` · **Temp audio:** `%TEMP%\deekta` (deleted after each use).

## Pricing

Transcription is billed by **audio length** (silence included), not by the resulting text.
Approximate, **set by OpenAI and subject to change** — see
[OpenAI pricing](https://openai.com/api/pricing):

| Model | ≈ price | Notes |
|---|---|---|
| `gpt-4o-mini-transcribe` | ~$0.003 / min | Fast & cheap — default |
| `gpt-4o-transcribe` | ~$0.006 / min | Highest accuracy |
| `whisper-1` | ~$0.006 / min | Classic, broad compatibility |

## Build from source

Requires the [.NET 8 SDK](https://aka.ms/dotnet/download).

```powershell
dotnet build -c Release          # build
dotnet run -c Release            # run

# self-contained single-file exe:
dotnet publish deekta.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -o publish
```

The installer is built from `installer/deekta.iss` with [Inno Setup](https://jrsoftware.org/isinfo.php)
(`iscc installer\deekta.iss`). CI builds both artifacts on each `v*` tag (see `.github/workflows`).

## Roadmap

See [TODO.md](TODO.md) — packaging (winget/Scoop), a landing page, and streaming transcription.

## License

[MIT](LICENSE) © 2026 **Jordee** ([@JordiCamps](https://github.com/JordiCamps)).

# deekta

App lleugera de **dictat per veu per a Windows** que viu a la *system tray*. Prems una drecera
global, parles, tornes a prémer i el text transcrit (via OpenAI Speech-to-Text) s'escriu
automàticament a l'aplicació activa. L'idioma de transcripció **segueix el teclat actiu** (CA, IT…).

## Requisits

- Windows 10/11
- [.NET 8 SDK](https://aka.ms/dotnet/download) (per compilar)
- Una clau d'API d'OpenAI

## Compilar i executar

```powershell
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

L'executable queda a `bin\Release\net8.0-windows\deekta.exe`.

## Com obrir l'app

- **Primer cop / configuració:** executa `deekta.exe`. Apareix la icona de micròfon a la *system tray*.
  Si encara no has posat la clau d'API, **la finestra de Configuració s'obre sola**.
- **Tornar a obrir Configuració:** torna a executar `deekta.exe` (per exemple des d'una drecera
  a l'escriptori o el menú Inici). Com que és *single-instance*, no n'obre una segona còpia: porta
  la Configuració de la instància que ja corre al davant.
- També pots fer doble clic a la icona de la safata, o clicar el globus de notificació.

## Ús

1. Posa el cursor on vulguis escriure (Notepad, navegador, Word, VS Code…).
2. Prem la drecera (per defecte **Ctrl + Alt + D**) — apareix la barra vermella "Gravant…".
3. Parla.
4. Torna a prémer-la — el text es transcriu i **s'escriu sol** on hi havia el cursor.

> Si la finestra activa té permisos d'administrador, Windows bloqueja l'escriptura automàtica;
> en aquest cas el text es deixa al porta-retalls i l'enganxes amb Ctrl+V (deekta t'avisa).

## Configuració

| Opció | Per defecte |
|---|---|
| Model | `gpt-4o-mini-transcribe` |
| Idioma | Automàtic (segueix el teclat actiu) |
| Drecera | Ctrl + Alt + D |
| Escriure automàticament | Activat |
| Arrencar amb Windows | Desactivat |
| So (beep) | Activat |

- **Configuració i logs:** `%AppData%\deekta`
- **Àudio temporal:** `%TEMP%\deekta` (s'esborra després de cada transcripció)
- La clau d'API es desa xifrada amb **Windows DPAPI** (scope d'usuari); mai en text pla.

## Límits (MVP)

Gravació màxima 120 s · timeout d'API 60 s · clips < 1 s s'ignoren · sense selecció de micròfon,
overlay complet, postprocessat amb LLM ni restauració del porta-retalls.

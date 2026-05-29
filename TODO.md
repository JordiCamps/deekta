# deekta — TODO / Idees

Llista de millores pendents i idees a valorar. No tot s'ha de fer; serveix per no perdre-ho.

## Fet ✅
- [x] **Llicència** → **MIT** (© 2026 Jordee).
- [x] **Autoria** al codi (`deekta.csproj`) + crèdit a la finestra.
- [x] **Finestra a dues columnes** (marca / "com funciona" / enllaços / llicència | settings).
- [x] **Guia de claus d'OpenAI**: README + enllaç dins Settings.
- [x] **Informació de preu** a la UI (per minut) + disclaimer + enllaç.
- [x] **Distribució**: `.exe` autònom (single-file) + **instal·lador** Inno Setup.
- [x] **CI/Release** amb GitHub Actions (`build.yml` + `release.yml`).
- [x] **Repo públic** amb descripció, homepage i topics.
- [x] **Primera Release `v1.0.1`** publicada amb `deekta.exe` + `deekta-setup.exe`.
- [x] **Historial net** (correus fora; commits com a Jordee + noreply).

## Publicació — pendent
- [ ] **Captures de pantalla / GIF** de demostració al README (placeholders posats).
- [ ] **GitHub Pages (landing)**: pàgina de màrqueting amb botó de descàrrega que apunti a
      l'instal·lador de la **Release** (GitHub Releases ja és un host conegut i fiable — no cal cap
      web externa de tercers). Enllaçar-hi eventualment la Microsoft Store.
- [ ] **Microsoft Store**: empaquetar com a **MSIX** i publicar (compte de developer ~19 $ one-time;
      la Store gestiona la signatura). Millora molt la **confiança** i la descobribilitat per a públic
      no tècnic. Enllaçar-la des del README/landing.
- [ ] **winget** i/o **Scoop** (un cop la Release sigui estable).
- [ ] (Opcional) **Signatura de codi** (certificat) per evitar avisos de SmartScreen al `.exe`/instal·lador
      distribuïts fora de la Store.

## Estadístiques d'ús i cost
- [ ] Investigar si l'API retorna el **consum de tokens** (camp `usage`) a la resposta de transcripció
      dels models `gpt-4o[-mini]-transcribe`. Si no el retorna, **estimar el cost** a partir de la
      **durada de l'àudio × preu/min** (la durada ja la tenim a `AudioRecorder`).
- [ ] Desar un **registre local** per transcripció: data, hora, durada, tokens (si n'hi ha) i € estimats
      (a `%AppData%\deekta`, sense desar mai el text transcrit).
- [ ] **Pantalla d'estadístiques**: cost del dia/mes, minuts dictats, nombre de transcripcions… (queda xulo).

## Transcripció en streaming (important)
Canvia molt l'experiència (el text apareix mentre parles). Hi ha enfocaments coneguts i fiables.
- [ ] Investigar `stream=true` a `/v1/audio/transcriptions` (`gpt-4o[-mini]-transcribe`) i/o la
      **Realtime API**; confirmar preu i latència.
- [ ] El streaming pot **revisar resultats enrere**. Enfocaments segurs (sense sorpreses):
      - (a) mostrar el parcial **només a l'overlay** i inserir **només el text final**;
      - (b) inserir per **segments "estables"** (commit) i **no tocar mai** el text ja escrit.
- [ ] Recomanació: començar per (a)/(b) sense esborrar text ja inserit, per evitar conflictes amb el cursor.

## Millores de producte — pendent
- [ ] Retallar **silencis** abans d'enviar (estalvi de cost; els silencis es facturen).

## Monetització — llicència de pagament (futur)
Idea: la funció **"Arrencar amb Windows"** queda **desactivada** per defecte i s'habilita
**per sempre** amb una compra única (POC ~1 €). Hauria de ser **raonablement difícil de hackejar**
(assumint que cap app local és 100 % inviolable).

- [ ] **Gate de funció:** bloquejar el toggle "Arrencar amb Windows" (marcar-lo "Premium") fins que
      hi hagi llicència vàlida; botó "Desbloquejar (1 €)".
- [ ] **Validació offline amb signatura asimètrica:** l'app incrusta una **clau pública** (Ed25519/RSA);
      la llicència és un *token signat* amb la teva **clau privada** → verificació offline. Opcional:
      lligar-la a un **ID de màquina** per evitar compartir-la (amb fricció en canvis de hardware).
- [ ] **Pagament:** *merchant of record* que gestioni l'IVA UE i emeti claus — **Lemon Squeezy**,
      **Paddle** o **Gumroad** (millor que Stripe sol per a imports petits).
- [ ] **Flux d'activació:** camp "Introduir llicència" a Configuració → validar → habilitar.
- [ ] **Anti-tamper (moderat):** ofuscació + comprovacions en més d'un punt; objectiu "prou difícil",
      no impossible. Comprovació online opcional si algun dia cal més.

## Notes
- **Com es factura:** es paga per la **durada de l'àudio** (silencis inclosos), no pel text.
  deekta ja ignora clips < 1 s i talla als 120 s, però no retalla pauses internes.
- **Compte de Google Play developer:** **no serveix** per a deekta — és per a apps **Android**, no per a
  Windows desktop. Per a Windows: GitHub Releases (fet), Microsoft Store (MSIX), winget/Scoop.
- **Avalonia?** Valorat i **descartat de moment**: deekta és molt específic de Windows (hotkey global,
  SendInput, tray, DPAPI, registre, NAudio). La UI portable aportaria poc i la integració amb l'SO
  s'hauria de reescriure igualment per plataforma. WinForms és suficient i lleuger.

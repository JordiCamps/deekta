# deekta — TODO / Idees

Llista de millores pendents i idees a valorar. No tot s'ha de fer; serveix per no perdre-ho.

## Fet ✅
- [x] **Llicència** aclarida → **MIT** (© 2026 Jordee).
- [x] **Autoria** al codi: `<Authors>/<Company>/<Copyright>` al `deekta.csproj` + crèdit a la finestra.
- [x] **Finestra a dues columnes** (esquerra: marca, "com funciona", enllaços, llicència; dreta: settings).
- [x] **Guia de claus d'OpenAI**: secció al README + enllaç dins Settings.
- [x] **Informació de preu** a la UI (per minut) + disclaimer + enllaç a OpenAI pricing.
- [x] **Distribució**: `.exe` autònom (single-file) + **instal·lador** Inno Setup (`installer/deekta.iss`).
- [x] **CI/Release** amb GitHub Actions (`build.yml` + `release.yml` en etiqueta `v*`).
- [x] L'idioma ja no es mostra a la barra de gravació; primera línia d'instruccions visible.

## Publicació — pendent
- [ ] Crear la primera **GitHub Release** (push d'etiqueta `v1.0.0` → CI genera exe + instal·lador).
- [ ] Afegir **topics** al repo (windows, dictation, speech-to-text, openai, whisper, catalan…).
- [ ] **Captures de pantalla / GIF** de demostració al README (placeholders posats).
- [ ] **winget** (manifest a microsoft/winget-pkgs) i/o **Scoop** — millor un cop hi hagi Release estable.
- [ ] Opcional: **landing page** (GitHub Pages) amb màrqueting i enllaç de descàrrega.

## Millores de producte — pendent
- [ ] Valorar **mode fosc** coherent per a la finestra.
- [ ] (Opcional) Comptador local aproximat de minuts/cost de la sessió.
- [ ] Retallar **silencis** abans d'enviar (estalvi de cost; els silencis es facturen).

## Transcripció en streaming (ajornat)
- [ ] Investigar si el model/endpoint suporta **streaming** (`stream=true` a `/v1/audio/transcriptions`
      per als `gpt-4o[-mini]-transcribe`, o la **Realtime API**) i si el **preu** és acceptable.
- [ ] Consideració clau: el streaming retorna **resultats parcials que es poden revisar enrere**.
      Opcions: (a) mostrar el parcial només a l'overlay i **inserir només el text final**;
      (b) inserir progressivament només segments "estables". Decidir l'experiència abans d'implementar.

## Notes
- **Com es factura:** es paga per la **durada de l'àudio** (silencis inclosos), no pel text.
  deekta ja ignora clips < 1 s i talla als 120 s, però no retalla pauses internes.
- **Avalonia?** Valorat i **descartat de moment**: deekta és molt específic de Windows (hotkey global,
  SendInput, tray, DPAPI, registre, NAudio). La UI portable aportaria poc i la integració amb l'SO
  s'hauria de reescriure igualment per plataforma. WinForms és suficient i lleuger.

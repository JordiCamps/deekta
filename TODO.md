# deekta — TODO / Idees

Llista de millores pendents i idees a valorar. No tot s'ha de fer; serveix per no perdre-ho.

## Llicència i autoria
- [ ] **Aclarir l'ús de la llicència.** Ara mateix el repo té GPL‑3 (heretada del primer commit).
      Decidir si volem GPL‑3 (copyleft fort), MIT/Apache‑2.0 (permissiva) o una altra. Implicacions:
      GPL obliga a publicar el codi de qualsevol derivat; MIT/Apache permet ús tancat.
- [ ] **Posar el nom de l'autor al codi** (ex. "Jordee"): `<Authors>` i `<Company>` al `deekta.csproj`,
      capçalera/crèdit a la finestra de Configuració i a un "Quant a…".

## Publicació / descobribilitat (obert i lliure, fàcil de trobar)
- [ ] Crear una **GitHub Release** amb binari (`deekta.exe` o instal·lador) i notes de versió.
- [ ] Afegir **topics** al repo (windows, dictation, speech-to-text, openai, whisper, catalan…) i una bona descripció.
- [ ] `README` amb captures de pantalla i GIF de demostració.
- [ ] Valorar **winget** (manifest a microsoft/winget-pkgs) i/o **Scoop** per instal·lació fàcil.
- [ ] Opcional: petita **landing page** (GitHub Pages) amb el màrqueting i enllaç de descàrrega.

## Onboarding / claus d'OpenAI "for dummies"
- [ ] Guia pas a pas (amb captures) per obtenir una clau:
      1. Anar a https://platform.openai.com/api-keys
      2. Iniciar sessió / crear compte i afegir un mètode de pagament (la transcripció és de pagament per ús).
      3. "Create new secret key" → copiar la clau `sk-…` (només es mostra un cop).
      4. Enganxar-la a deekta → Configuració.
- [ ] Enllaç directe "Com obtenir la clau?" dins la finestra de Configuració.
- [ ] Nota sobre costos aproximats per minut segons model.

## UI / primera execució
- [ ] **Pantalla inicial millor** quan s'obre per primera vegada. Idea preferida:
      convertir la finestra en **dues columnes** →
      - **Esquerra:** zona visual/màrqueting (logo, nom, eslògan, com funciona en 3 passos,
        enllaç per obtenir la clau) + **llicència/crèdits**.
      - **Dreta:** els settings actuals (compte, model, drecera, opcions).
- [ ] Polir tipografia, espaiats i icones; mode clar coherent (valorar mode fosc).

## Instal·lador
- [ ] Valorar un **instal·lador** (Inno Setup o WiX/MSI) que:
      - copiï `deekta.exe` a Program Files o `%LocalAppData%`,
      - creï drecera al menú Inici i (opcional) "arrencar amb Windows",
      - inclogui desinstal·lador.
- [ ] Alternativa lleugera: publicar `deekta.exe` *self-contained* (un sol fitxer) sense instal·lador.

## Transcripció en streaming (a valorar)
- [ ] Investigar si el model/endpoint suporta **streaming** (`stream=true` a `/v1/audio/transcriptions`
      per als models `gpt-4o[-mini]-transcribe`, o la **Realtime API**) i si el **preu** és acceptable.
- [ ] Consideració clau: el streaming retorna **resultats parcials que es poden revisar enrere**
      (un fragment ja "escrit" pot canviar quan arriba més context). Implicacions d'implementació:
      - Escriure en viu i després **corregir** implicaria esborrar (Backspace) i reescriure → fràgil
        si l'usuari ja ha mogut el cursor o escrit a sobre.
      - Opcions: (a) mostrar el parcial només a l'overlay i **inserir només el text final** en acabar;
        (b) inserir progressivament només quan un segment es marca com a "estable".
      - Decidir l'experiència abans d'implementar.

## Bugs / petits ajustos
- [x] La primera línia d'instruccions de Configuració no es veia sencera. *(arreglat)*

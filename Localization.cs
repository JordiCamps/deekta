using System.Globalization;

namespace Deekta;

/// <summary>Localizable UI string keys.</summary>
internal enum Tr
{
    MenuSettings, MenuStartup, MenuExit,
    TipIdle, TipRecording, TipTranscribing,
    Recording, StopHint, Transcribing,
    TextInserted, CopiedClipboard, CopiedPasteManually, PasteBlockedBalloon,
    NoText, TooShort, MicError, TranscribeError, NeedApiKey,
    StoppedAtLimit, HotkeyRegisterFailed, SaveSettingsError, StartupChangeError,
    // OpenAI client
    ErrApiKeyNotSet, ErrTimeout, ErrConnect, ErrUnexpectedAudio,
    ErrApiKeyInvalid, ErrRateLimit, ErrApiWithMsg, ErrApiCode, ErrUnrecognized,
    // Settings form
    WinTitle, LblApiKey, LblModel, LblLanguage, LangNote, LblHotkey,
    LblAutoInsert, LblStartup, LblBeep, BtnSave, BtnCancel, HotkeyPrompt,
    ErrModelEmpty, ErrHotkeyModifier,
    // Overlay
    OpenSettingsLink,
    // Settings groups + richer controls
    GrpApi, GrpModel, GrpShortcut, GrpOptions,
    ApiKeyConfigured, BtnChange, ApiKeyPlaceholder, ApiKeyOnlyOpenAi,
    LblKeyChar, ShortcutPreview,
    ModelMiniDesc, ModelFullDesc, ModelWhisperDesc,
    AutoInsertHint, StartupHint, BeepHint, SettingsIntro,
}

/// <summary>
/// Tiny built-in localization. The active UI language is chosen once from the Windows display
/// language (CurrentUICulture); supported languages are English (default), Catalan, Spanish,
/// French, Italian and German. Each string is an array indexed by <see cref="Lang"/>.
/// </summary>
internal static class Localization
{
    // Array index order for every entry below.
    private enum Lang { En = 0, Ca = 1, Es = 2, Fr = 3, It = 4, De = 5 }

    private static Lang _lang = Lang.En;

    public static void Init()
    {
        _lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant() switch
        {
            "ca" => Lang.Ca,
            "es" => Lang.Es,
            "fr" => Lang.Fr,
            "it" => Lang.It,
            "de" => Lang.De,
            _ => Lang.En,
        };
    }

    public static string Get(Tr key, params object[] args)
    {
        string[] row = Table[key];
        int i = (int)_lang;
        string text = (i < row.Length && !string.IsNullOrEmpty(row[i])) ? row[i] : row[0];
        return args.Length == 0 ? text : string.Format(text, args);
    }

    //                                   En, Ca, Es, Fr, It, De
    private static readonly Dictionary<Tr, string[]> Table = new()
    {
        [Tr.MenuSettings] = new[] { "Settings…", "Configuració…", "Configuración…", "Paramètres…", "Impostazioni…", "Einstellungen…" },
        [Tr.MenuStartup] = new[] { "Start with Windows", "Arrencar amb Windows", "Iniciar con Windows", "Démarrer avec Windows", "Avvia con Windows", "Mit Windows starten" },
        [Tr.MenuExit] = new[] { "Exit", "Sortir", "Salir", "Quitter", "Esci", "Beenden" },

        [Tr.TipIdle] = new[] { "deekta", "deekta", "deekta", "deekta", "deekta", "deekta" },
        [Tr.TipRecording] = new[] { "deekta — Recording…", "deekta — Gravant…", "deekta — Grabando…", "deekta — Enregistrement…", "deekta — Registrazione…", "deekta — Aufnahme…" },
        [Tr.TipTranscribing] = new[] { "deekta — Transcribing…", "deekta — Transcrivint…", "deekta — Transcribiendo…", "deekta — Transcription…", "deekta — Trascrizione…", "deekta — Transkription…" },

        [Tr.Recording] = new[] { "Recording…", "Gravant…", "Grabando…", "Enregistrement…", "Registrazione…", "Aufnahme…" },
        [Tr.StopHint] = new[] { "press again to stop", "torna a prémer per aturar", "pulsa de nuevo para parar", "appuyez à nouveau pour arrêter", "premi di nuovo per fermare", "erneut drücken zum Stoppen" },
        [Tr.Transcribing] = new[] { "Transcribing…", "Transcrivint…", "Transcribiendo…", "Transcription…", "Trascrizione…", "Transkription…" },

        [Tr.TextInserted] = new[] { "Text inserted", "Text inserit", "Texto insertado", "Texte inséré", "Testo inserito", "Text eingefügt" },
        [Tr.CopiedClipboard] = new[] { "Copied to clipboard (Ctrl+V)", "Copiat al porta-retalls (Ctrl+V)", "Copiado al portapapeles (Ctrl+V)", "Copié dans le presse-papiers (Ctrl+V)", "Copiato negli appunti (Ctrl+V)", "In Zwischenablage kopiert (Strg+V)" },
        [Tr.CopiedPasteManually] = new[] { "Copied — paste with Ctrl+V (protected window)", "Copiat — enganxa amb Ctrl+V (finestra protegida)", "Copiado — pega con Ctrl+V (ventana protegida)", "Copié — collez avec Ctrl+V (fenêtre protégée)", "Copiato — incolla con Ctrl+V (finestra protetta)", "Kopiert — mit Strg+V einfügen (geschütztes Fenster)" },
        [Tr.PasteBlockedBalloon] = new[]
        {
            "Couldn't type automatically (the window runs with elevated privileges). The text is on the clipboard: paste it with Ctrl+V.",
            "No s'ha pogut escriure automàticament (finestra amb permisos elevats). El text és al porta-retalls: enganxa'l amb Ctrl+V.",
            "No se pudo escribir automáticamente (ventana con permisos elevados). El texto está en el portapapeles: pégalo con Ctrl+V.",
            "Saisie automatique impossible (fenêtre avec privilèges élevés). Le texte est dans le presse-papiers : collez-le avec Ctrl+V.",
            "Digitazione automatica non riuscita (finestra con privilegi elevati). Il testo è negli appunti: incollalo con Ctrl+V.",
            "Automatisches Tippen nicht möglich (Fenster mit erhöhten Rechten). Der Text ist in der Zwischenablage: mit Strg+V einfügen.",
        },

        [Tr.NoText] = new[] { "No text detected", "Sense text detectat", "No se detectó texto", "Aucun texte détecté", "Nessun testo rilevato", "Kein Text erkannt" },
        [Tr.TooShort] = new[] { "Too short — try again", "Massa curt — torna-ho a provar", "Demasiado corto — inténtalo de nuevo", "Trop court — réessayez", "Troppo corto — riprova", "Zu kurz — bitte erneut" },
        [Tr.MicError] = new[] { "Couldn't access the microphone", "No s'ha pogut accedir al micròfon", "No se pudo acceder al micrófono", "Impossible d'accéder au microphone", "Impossibile accedere al microfono", "Kein Zugriff auf das Mikrofon" },
        [Tr.TranscribeError] = new[] { "Error while transcribing", "Error en transcriure", "Error al transcribir", "Erreur lors de la transcription", "Errore durante la trascrizione", "Fehler bei der Transkription" },
        [Tr.NeedApiKey] = new[] { "Set your OpenAI API key first.", "Cal configurar la clau d'API d'OpenAI.", "Configura primero tu clave de API de OpenAI.", "Configurez d'abord votre clé API OpenAI.", "Configura prima la chiave API di OpenAI.", "Zuerst den OpenAI-API-Schlüssel einrichten." },

        [Tr.StoppedAtLimit] = new[] { "Recording stopped at the {0}s limit.", "Gravació aturada al límit de {0}s.", "Grabación detenida en el límite de {0}s.", "Enregistrement arrêté à la limite de {0}s.", "Registrazione interrotta al limite di {0}s.", "Aufnahme beim {0}s-Limit gestoppt." },
        [Tr.HotkeyRegisterFailed] = new[] { "Couldn't register the shortcut {0}. Choose another in Settings.", "No s'ha pogut registrar la drecera {0}. Tria'n una altra a Configuració.", "No se pudo registrar el atajo {0}. Elige otro en Configuración.", "Impossible d'enregistrer le raccourci {0}. Choisissez-en un autre dans les Paramètres.", "Impossibile registrare la scorciatoia {0}. Scegline un'altra nelle Impostazioni.", "Verknüpfung {0} konnte nicht registriert werden. Wähle in den Einstellungen eine andere." },
        [Tr.SaveSettingsError] = new[] { "Couldn't save the settings.", "No s'ha pogut desar la configuració.", "No se pudo guardar la configuración.", "Impossible d'enregistrer les paramètres.", "Impossibile salvare le impostazioni.", "Einstellungen konnten nicht gespeichert werden." },
        [Tr.StartupChangeError] = new[] { "Couldn't change the start-with-Windows setting.", "No s'ha pogut canviar l'arrencada amb Windows.", "No se pudo cambiar el inicio con Windows.", "Impossible de modifier le démarrage avec Windows.", "Impossibile modificare l'avvio con Windows.", "Start mit Windows konnte nicht geändert werden." },

        [Tr.ErrApiKeyNotSet] = new[] { "The OpenAI API key is not configured.", "No s'ha configurat la clau d'API d'OpenAI.", "La clave de API de OpenAI no está configurada.", "La clé API OpenAI n'est pas configurée.", "La chiave API di OpenAI non è configurata.", "Der OpenAI-API-Schlüssel ist nicht konfiguriert." },
        [Tr.ErrTimeout] = new[] { "The OpenAI request timed out.", "La petició a OpenAI ha excedit el temps d'espera.", "La solicitud a OpenAI superó el tiempo de espera.", "La requête OpenAI a expiré.", "La richiesta a OpenAI è scaduta.", "Die OpenAI-Anfrage hat das Zeitlimit überschritten." },
        [Tr.ErrConnect] = new[] { "Couldn't connect to OpenAI. Check your connection.", "No s'ha pogut connectar amb OpenAI. Comprova la connexió.", "No se pudo conectar con OpenAI. Comprueba la conexión.", "Impossible de se connecter à OpenAI. Vérifiez votre connexion.", "Impossibile connettersi a OpenAI. Controlla la connessione.", "Verbindung zu OpenAI fehlgeschlagen. Überprüfe deine Verbindung." },
        [Tr.ErrUnexpectedAudio] = new[] { "Unexpected error while transcribing the audio.", "Error inesperat en transcriure l'àudio.", "Error inesperado al transcribir el audio.", "Erreur inattendue lors de la transcription de l'audio.", "Errore imprevisto durante la trascrizione dell'audio.", "Unerwarteter Fehler bei der Audiotranskription." },
        [Tr.ErrApiKeyInvalid] = new[] { "Invalid API key or insufficient permissions.", "Clau d'API incorrecta o sense permisos.", "Clave de API incorrecta o sin permisos.", "Clé API non valide ou autorisations insuffisantes.", "Chiave API non valida o permessi insufficienti.", "Ungültiger API-Schlüssel oder fehlende Berechtigungen." },
        [Tr.ErrRateLimit] = new[] { "OpenAI rate limit reached. Try again.", "Límit de peticions d'OpenAI superat. Torna-ho a provar.", "Límite de solicitudes de OpenAI alcanzado. Inténtalo de nuevo.", "Limite de requêtes OpenAI atteinte. Réessayez.", "Limite di richieste OpenAI raggiunto. Riprova.", "OpenAI-Anfragelimit erreicht. Bitte erneut versuchen." },
        [Tr.ErrApiWithMsg] = new[] { "OpenAI returned an error: {0}", "OpenAI ha retornat un error: {0}", "OpenAI devolvió un error: {0}", "OpenAI a renvoyé une erreur : {0}", "OpenAI ha restituito un errore: {0}", "OpenAI hat einen Fehler zurückgegeben: {0}" },
        [Tr.ErrApiCode] = new[] { "OpenAI returned an error ({0}).", "OpenAI ha retornat un error ({0}).", "OpenAI devolvió un error ({0}).", "OpenAI a renvoyé une erreur ({0}).", "OpenAI ha restituito un errore ({0}).", "OpenAI hat einen Fehler zurückgegeben ({0})." },
        [Tr.ErrUnrecognized] = new[] { "Unrecognised OpenAI response.", "Resposta d'OpenAI no reconeguda.", "Respuesta de OpenAI no reconocida.", "Réponse OpenAI non reconnue.", "Risposta di OpenAI non riconosciuta.", "Unbekannte OpenAI-Antwort." },

        [Tr.WinTitle] = new[] { "deekta — Settings", "deekta — Configuració", "deekta — Configuración", "deekta — Paramètres", "deekta — Impostazioni", "deekta — Einstellungen" },
        [Tr.LblApiKey] = new[] { "OpenAI API key", "Clau d'API d'OpenAI", "Clave de API de OpenAI", "Clé API OpenAI", "Chiave API OpenAI", "OpenAI-API-Schlüssel" },
        [Tr.LblModel] = new[] { "Model:", "Model:", "Modelo:", "Modèle :", "Modello:", "Modell:" },
        [Tr.LblLanguage] = new[] { "Language", "Idioma", "Idioma", "Langue", "Lingua", "Sprache" },
        [Tr.LangNote] = new[] { "Automatic — follows the active keyboard (EN, IT…)", "Automàtic — segueix el teclat actiu (CA, IT…)", "Automático — sigue el teclado activo (ES, IT…)", "Automatique — suit le clavier actif (FR, IT…)", "Automatico — segue la tastiera attiva (IT, EN…)", "Automatisch — folgt der aktiven Tastatur (DE, IT…)" },
        [Tr.LblHotkey] = new[] { "Shortcut:", "Drecera:", "Atajo:", "Raccourci :", "Scorciatoia:", "Tastenkürzel:" },
        [Tr.LblAutoInsert] = new[] { "Type the text automatically", "Escriure el text automàticament", "Escribir el texto automáticamente", "Saisir le texte automatiquement", "Digita il testo automaticamente", "Text automatisch eintippen" },
        [Tr.LblStartup] = new[] { "Start deekta with Windows", "Arrencar deekta amb Windows", "Iniciar deekta con Windows", "Démarrer deekta avec Windows", "Avvia deekta con Windows", "deekta mit Windows starten" },
        [Tr.LblBeep] = new[] { "Sound when recording starts/stops", "So en començar/aturar la gravació", "Sonido al empezar/parar la grabación", "Son au début/à la fin de l'enregistrement", "Suono all'inizio/fine della registrazione", "Ton bei Aufnahmestart/-stopp" },
        [Tr.BtnSave] = new[] { "Save", "Desar", "Guardar", "Enregistrer", "Salva", "Speichern" },
        [Tr.BtnCancel] = new[] { "Cancel", "Cancel·lar", "Cancelar", "Annuler", "Annulla", "Abbrechen" },
        [Tr.HotkeyPrompt] = new[] { "Press the combination…", "Prem la combinació…", "Pulsa la combinación…", "Appuyez sur la combinaison…", "Premi la combinazione…", "Tastenkombination drücken…" },
        [Tr.ErrModelEmpty] = new[] { "The model can't be empty.", "El model no pot estar buit.", "El modelo no puede estar vacío.", "Le modèle ne peut pas être vide.", "Il modello non può essere vuoto.", "Das Modell darf nicht leer sein." },
        [Tr.ErrHotkeyModifier] = new[] { "The shortcut must include at least one modifier (Ctrl/Alt/Shift/Win).", "La drecera ha d'incloure almenys un modificador (Ctrl/Alt/Shift/Win).", "El atajo debe incluir al menos un modificador (Ctrl/Alt/Shift/Win).", "Le raccourci doit inclure au moins un modificateur (Ctrl/Alt/Maj/Win).", "La scorciatoia deve includere almeno un modificatore (Ctrl/Alt/Shift/Win).", "Das Kürzel muss mindestens einen Modifikator enthalten (Strg/Alt/Umschalt/Win)." },

        [Tr.OpenSettingsLink] = new[] { "⚙ Settings", "⚙ Configuració", "⚙ Configuración", "⚙ Paramètres", "⚙ Impostazioni", "⚙ Einstellungen" },

        [Tr.GrpApi] = new[] { "OpenAI account", "Compte d'OpenAI", "Cuenta de OpenAI", "Compte OpenAI", "Account OpenAI", "OpenAI-Konto" },
        [Tr.GrpModel] = new[] { "Transcription model", "Model de transcripció", "Modelo de transcripción", "Modèle de transcription", "Modello di trascrizione", "Transkriptionsmodell" },
        [Tr.GrpShortcut] = new[] { "Global shortcut", "Drecera global", "Atajo global", "Raccourci global", "Scorciatoia globale", "Globales Tastenkürzel" },
        [Tr.GrpOptions] = new[] { "Options", "Opcions", "Opciones", "Options", "Opzioni", "Optionen" },

        [Tr.ApiKeyConfigured] = new[] { "✓ API key configured", "✓ Clau d'API configurada", "✓ Clave de API configurada", "✓ Clé API configurée", "✓ Chiave API configurata", "✓ API-Schlüssel konfiguriert" },
        [Tr.BtnChange] = new[] { "Change…", "Canviar…", "Cambiar…", "Modifier…", "Cambia…", "Ändern…" },
        [Tr.ApiKeyPlaceholder] = new[] { "Paste your OpenAI key — starts with “sk-”", "Enganxa la teva clau d'OpenAI — comença per «sk-»", "Pega tu clave de OpenAI — empieza por «sk-»", "Collez votre clé OpenAI — commence par « sk- »", "Incolla la tua chiave OpenAI — inizia con «sk-»", "Füge deinen OpenAI-Schlüssel ein — beginnt mit „sk-“" },
        [Tr.ApiKeyOnlyOpenAi] = new[] { "Only OpenAI keys work with deekta.", "Només funcionen claus d'OpenAI amb deekta.", "Solo funcionan claves de OpenAI con deekta.", "Seules les clés OpenAI fonctionnent avec deekta.", "Con deekta funzionano solo chiavi OpenAI.", "Mit deekta funktionieren nur OpenAI-Schlüssel." },

        [Tr.LblKeyChar] = new[] { "Key:", "Tecla:", "Tecla:", "Touche :", "Tasto:", "Taste:" },
        [Tr.ShortcutPreview] = new[] { "Shortcut: {0}", "Drecera: {0}", "Atajo: {0}", "Raccourci : {0}", "Scorciatoia: {0}", "Tastenkürzel: {0}" },

        [Tr.ModelMiniDesc] = new[] { "Fast and inexpensive. Best default for everyday dictation.", "Ràpid i econòmic. La millor opció per defecte per al dictat diari.", "Rápido y económico. La mejor opción por defecto para el dictado diario.", "Rapide et économique. Le meilleur choix par défaut au quotidien.", "Veloce ed economico. La scelta predefinita ideale per l'uso quotidiano.", "Schnell und günstig. Beste Voreinstellung für den Alltag." },
        [Tr.ModelFullDesc] = new[] { "Highest accuracy. Slower and more expensive.", "Màxima precisió. Més lent i més car.", "Máxima precisión. Más lento y más caro.", "Précision maximale. Plus lent et plus cher.", "Massima precisione. Più lento e più costoso.", "Höchste Genauigkeit. Langsamer und teurer." },
        [Tr.ModelWhisperDesc] = new[] { "Classic Whisper. Broad compatibility, good for long audio.", "Whisper clàssic. Àmplia compatibilitat, bo per a àudios llargs.", "Whisper clásico. Amplia compatibilidad, bueno para audios largos.", "Whisper classique. Large compatibilité, idéal pour les longs audios.", "Whisper classico. Ampia compatibilità, adatto agli audio lunghi.", "Klassisches Whisper. Breite Kompatibilität, gut für lange Audios." },

        [Tr.AutoInsertHint] = new[] { "If off, the text is only copied to the clipboard.", "Si està desactivat, el text només es copia al porta-retalls.", "Si está desactivado, el texto solo se copia al portapapeles.", "Si désactivé, le texte est seulement copié dans le presse-papiers.", "Se disattivato, il testo viene solo copiato negli appunti.", "Wenn aus, wird der Text nur in die Zwischenablage kopiert." },
        [Tr.StartupHint] = new[] { "Launches automatically when you sign in.", "S'inicia automàticament en iniciar sessió.", "Se inicia automáticamente al iniciar sesión.", "Se lance automatiquement à la connexion.", "Si avvia automaticamente all'accesso.", "Startet automatisch bei der Anmeldung." },
        [Tr.BeepHint] = new[] { "A short sound confirms start and stop.", "Un so curt confirma l'inici i l'aturada.", "Un sonido corto confirma inicio y parada.", "Un son court confirme le début et l'arrêt.", "Un suono breve conferma avvio e arresto.", "Ein kurzer Ton bestätigt Start und Stopp." },
        [Tr.SettingsIntro] = new[] { "Press your shortcut, speak, press again — the text is typed where your cursor is.", "Prem la drecera, parla, torna a prémer — el text s'escriu on tens el cursor.", "Pulsa el atajo, habla, vuelve a pulsar — el texto se escribe donde está el cursor.", "Appuyez sur le raccourci, parlez, ré-appuyez — le texte s'écrit où est le curseur.", "Premi la scorciatoia, parla, premi di nuovo — il testo viene scritto dove c'è il cursore.", "Kürzel drücken, sprechen, erneut drücken — der Text wird an der Cursorposition eingefügt." },
    };
}

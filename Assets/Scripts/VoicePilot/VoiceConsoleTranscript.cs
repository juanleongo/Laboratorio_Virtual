using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Oculus.Voice;
using Meta.WitAi.Json;

public class VoiceConsoleTranscript : MonoBehaviour
{
    [Header("Voice SDK")]
    [SerializeField] private AppVoiceExperience appVoiceExperience;

    [Header("UI de la consola")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text transcriptText;

    private void Awake()
    {
        if (appVoiceExperience == null)
        {
            appVoiceExperience = FindFirstObjectByType<AppVoiceExperience>();
        }

        if (statusText != null)
        {
            statusText.text = "Presiona ESPACIO para hablar";
        }

        if (transcriptText != null)
        {
            transcriptText.text = "";
        }
    }

    private void OnEnable()
    {
        if (appVoiceExperience == null) return;

        appVoiceExperience.VoiceEvents.OnStartListening.AddListener(OnStartListening);
        appVoiceExperience.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
        appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        appVoiceExperience.VoiceEvents.OnResponse.AddListener(OnWitResponse);
        appVoiceExperience.VoiceEvents.OnError.AddListener(OnWitError);
    }

    private void OnDisable()
    {
        if (appVoiceExperience == null) return;

        appVoiceExperience.VoiceEvents.OnStartListening.RemoveListener(OnStartListening);
        appVoiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
        appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
        appVoiceExperience.VoiceEvents.OnResponse.RemoveListener(OnWitResponse);
        appVoiceExperience.VoiceEvents.OnError.RemoveListener(OnWitError);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartListening();
        }
    }

    public void StartListening()
    {
        if (appVoiceExperience == null)
        {
            SetStatus("Error: no se encontró App Voice Experience");
            return;
        }

        SetStatus("Activando micrófono...");
        SetTranscript("");
        appVoiceExperience.Activate();
    }

    private void OnStartListening()
    {
        SetStatus("Escuchando...");
        SetTranscript("Habla ahora...");
    }

    private void OnStoppedListening()
    {
        SetStatus("Procesando voz...");
    }

    private void OnPartialTranscription(string transcription)
    {
        SetStatus("Escuchando...");
        SetTranscript("> " + transcription);
        Debug.Log("[TRANSCRIPCIÓN PARCIAL] " + transcription);
    }

    private void OnFullTranscription(string transcription)
    {
        SetStatus("Frase capturada");
        SetTranscript("> " + transcription);
        Debug.Log("[TRANSCRIPCIÓN FINAL] " + transcription);
    }

    private void OnWitResponse(WitResponseNode response)
    {
        SetStatus("Respuesta recibida de Wit.ai");

        if (response == null)
        {
            SetTranscript("Respuesta Wit.ai: NULL");
            Debug.LogWarning("[WIT RESPONSE] NULL");
            return;
        }

        string json = response.ToString();
        Debug.Log("[WIT RESPONSE RAW] " + json);

        string text = response["text"] != null ? response["text"].Value : "";

        string intentName = "sin_intent";
        float confidence = 0f;
        string intentSource = "wit";

        // Primero intentamos usar el intent que venga desde Wit.ai
        if (response["intents"] != null && response["intents"].Count > 0)
        {
            intentName = response["intents"][0]["name"].Value;
            confidence = response["intents"][0]["confidence"].AsFloat;
        }
        else
        {
            // Si Wit.ai no devuelve intent, usamos reglas locales en Unity
            intentName = ResolveIntentFallback(text);
            confidence = intentName == "sin_intent" ? 0f : 1f;
            intentSource = "fallback_local";
        }

        SetTranscript(
            "> " + text +
            "\nIntent: " + intentName +
            "\nConfianza: " + confidence +
            "\nFuente: " + intentSource
        );

        Debug.Log("[INTENT RESUELTO] " + intentName + " | Fuente: " + intentSource);
    }
    private string ResolveIntentFallback(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "sin_intent";
        }

        string t = text.Trim().ToLowerInvariant();

        // AYUDA
        if (t.Contains("ayuda") ||
            t.Contains("ayúdame") ||
            t.Contains("que puedo hacer") ||
            t.Contains("qué puedo hacer") ||
            t.Contains("mostrar ayuda") ||
            t.Contains("muestra la ayuda"))
        {
            return "show_help";
        }

        // LISTAR ARCHIVOS
        if (t.Contains("ver archivos") ||
            t.Contains("listar archivos") ||
            t.Contains("lista los archivos") ||
            t.Contains("mostrar directorio") ||
            t.Contains("muestra el directorio") ||
            t.Contains("enseña el contenido") ||
            t.Contains("contenido de la carpeta") ||
            t.Contains("que archivos hay") ||
            t.Contains("qué archivos hay"))
        {
            return "listar_archivos";
        }

        // LIMPIAR PANTALLA
        if (t.Contains("limpia la pantalla") ||
            t.Contains("limpiar pantalla") ||
            t.Contains("limpia la consola") ||
            t.Contains("borra la terminal") ||
            t.Contains("vacía la pantalla") ||
            t.Contains("vacia la pantalla") ||
            t.Contains("quita todo"))
        {
            return "clear_screen";
        }

        // CAMBIAR DIRECTORIO / ABRIR CARPETA
        if (t.Contains("abrir carpeta") ||
            t.Contains("abre la carpeta") ||
            t.Contains("entrar a carpeta") ||
            t.Contains("entra a la carpeta") ||
            t.Contains("cambiar directorio") ||
            t.Contains("cambiar carpeta") ||
            t.Contains("ir a carpeta") ||
            t.Contains("ve a la carpeta"))
        {
            return "cambiar_directorio";
        }

        return "sin_intent";
    }


    private void OnWitError(string error, string message)
    {
        SetStatus("Error en Wit.ai");
        SetTranscript("Error: " + error + "\n" + message);
        Debug.LogError("[WIT ERROR] " + error + " - " + message);
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }

        Debug.Log("[VOICE STATUS] " + text);
    }

    private void SetTranscript(string text)
    {
        if (transcriptText != null)
        {
            transcriptText.text = text;
        }
    }
}
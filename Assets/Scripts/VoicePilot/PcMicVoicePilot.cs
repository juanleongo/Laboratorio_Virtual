using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Oculus.Voice;
using Meta.WitAi.Json;

public class PcMicVoicePilot : MonoBehaviour
{
    [Header("Voice SDK")]
    [SerializeField] private AppVoiceExperience voiceExperience;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text transcriptionText;
    [SerializeField] private TMP_Text responseText;

    private void Awake()
    {
        if (voiceExperience == null)
        {
            voiceExperience = GetComponent<AppVoiceExperience>();
        }

        PrintAvailableMicrophones();
        SetStatus("Listo. Presiona ESPACIO o el botón Hablar.");
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ActivateVoice();
        }
    }

    public void ActivateVoice()
    {
        if (voiceExperience == null)
        {
            SetStatus("Error: no se encontró AppVoiceExperience.");
            return;
        }

        SetStatus("Activando micrófono...");
        voiceExperience.Activate();
    }

    public void DeactivateVoice()
    {
        if (voiceExperience == null)
        {
            return;
        }

        SetStatus("Enviando audio a Wit.ai...");
        voiceExperience.Deactivate();
    }

    public void OnStartListening()
    {
        SetStatus("Escuchando...");
    }

    public void OnStoppedListening()
    {
        SetStatus("Procesando...");
    }

    public void OnPartialTranscription(string text)
    {
        SetText(transcriptionText, "Parcial: " + text);
    }

    public void OnFullTranscription(string text)
    {
        SetText(transcriptionText, "Final: " + text);
    }

    public void OnWitResponse(WitResponseNode response)
    {
        SetStatus("Respuesta recibida desde Wit.ai.");

        string json = response != null ? response.ToString() : "Respuesta vacía";
        SetText(responseText, json);

        Debug.Log("[WIT RESPONSE] " + json);
    }

    public void OnWitError(string error, string message)
    {
        SetStatus("Error de Wit.ai.");
        Debug.LogError("[WIT ERROR] " + error + " - " + message);
    }

    private void PrintAvailableMicrophones()
    {
        Debug.Log("=== MICRÓFONOS DISPONIBLES ===");

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No se detectaron micrófonos en Unity.");
            return;
        }

        foreach (string device in Microphone.devices)
        {
            Debug.Log("Micrófono detectado: " + device);
        }
    }

    private void SetStatus(string value)
    {
        SetText(statusText, value);
        Debug.Log("[VOICE STATUS] " + value);
    }

    private void SetText(TMP_Text label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }
}
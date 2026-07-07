using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Responsible for showing the current wave number and animating the wave transition popup (syncs across all clients).
/// </summary>
public class WaveUIController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text currentWaveText; // Reference to the text element that displays the current wave number.
    [SerializeField] private TMP_Text popUpWaveText; // Reference to the popup text element used for the wave transition announcement.

    [Header("Settings")]
    [SerializeField] private PopUpTextConfigurations popUpConfigs; // Configuration values for the popup fade-in, display, and fade-out timing.
    
    /// <summary>
    /// Starts the wave transition sequence and broadcasts it to all clients.
    /// </summary>
    /// <param name="nextWaveNum">
    /// The wave number to display in the UI.
    /// </param>
    /// <returns>
    /// An IEnumerator used by Unity's coroutine system.
    /// </returns>
    public IEnumerator TransitionToNextWave(int nextWaveNum)
    {
        // All clients see the pop up
        TransitionToNextWaveClientRpc(nextWaveNum);

        // Stop coroutine when our display has ended
        yield return new WaitForSeconds(popUpConfigs.fadeInDuration + popUpConfigs.displayDuration + popUpConfigs.fadeOutDuration);
    }


    // ===================== HIDDEN FUNCTIONALITY =====================
    /// <summary>
    /// Updates the wave text on all clients and starts the popup animation.
    /// </summary>
    /// <param name="waveNum">
    /// The wave number to display.
    /// </param>
    [ClientRpc] 
    private void TransitionToNextWaveClientRpc(int waveNum)
    {
        UpdateWaveText(waveNum);
        StartCoroutine(DisplayPopUpWaveText());
    }

    /// <summary>
    /// Animates the wave popup text through fade-in, display, and fade-out phases.
    /// </summary>
    /// <returns>
    /// An IEnumerator used by Unity's coroutine system.
    /// </returns>
    private IEnumerator DisplayPopUpWaveText()
    {
        float waitPerCall = 0.01f;

        // Fade in
        while (popUpWaveText.alpha < 1)
        {
            popUpWaveText.alpha += 1 / popUpConfigs.fadeInDuration * waitPerCall;
            yield return new WaitForSeconds(waitPerCall);
        }
        
        // On screen
        yield return new WaitForSeconds(popUpConfigs.displayDuration);

        // Fade out
        while (popUpWaveText.alpha > 0)
        {
            popUpWaveText.alpha -= 1 / popUpConfigs.fadeOutDuration * waitPerCall;
            yield return new WaitForSeconds(waitPerCall);
        }
    }

    /// <summary>
    /// Updates the current wave text and the popup wave text with the provided wave number.
    /// </summary>
    /// <param name="currWave">
    /// The wave number to display.
    /// </param>
    private void UpdateWaveText(int currWave)
    {
        currentWaveText.text = $"Wave {currWave}";
        popUpWaveText.text = $"Wave {currWave}";
    }
}

/// <summary>
/// Stores timing values for the wave transition popup animation.
/// </summary>
[System.Serializable] 
public struct PopUpTextConfigurations
{
    public float fadeInDuration; // The duration of the popup fade-in animation.
    public float displayDuration; // The duration that the popup remains fully visible.
    public float fadeOutDuration; // The duration of the popup fade-out animation.
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WaveUIController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text currentWaveText;
    [SerializeField] private TMP_Text popUpWaveText;

    [Header("Settings")]
    [SerializeField] private PopUpTextConfigurations popUpConfigs;
    

    public IEnumerator TransitionToNextWave(int nextWaveNum)
    {
        // All clients see the pop up
        TransitionToNextWaveClientRpc(nextWaveNum);

        // Stop coroutine when our display has ended
        yield return new WaitForSeconds(popUpConfigs.fadeInDuration + popUpConfigs.displayDuration + popUpConfigs.fadeOutDuration);
    }


    // ===================== HIDDEN FUNCTIONALITY =====================
    [ClientRpc] 
    private void TransitionToNextWaveClientRpc(int waveNum)
    {
        UpdateWaveText(waveNum);
        StartCoroutine(DisplayPopUpWaveText());
    }

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

    private void UpdateWaveText(int currWave)
    {
        currentWaveText.text = $"Wave {currWave}";
        popUpWaveText.text = $"Wave {currWave}";
    }
}

[System.Serializable] 
public struct PopUpTextConfigurations
{
    public float fadeInDuration;
    public float displayDuration;
    public float fadeOutDuration; 
}

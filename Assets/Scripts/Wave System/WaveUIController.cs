using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WaveUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text currentWaveText;
    [SerializeField] private TMP_Text popUpWaveText;

    [Header("Settings")]
    [SerializeField] private PopUpTextConfigurations popUpConfigs;
    

    public void UpdateWaveText(int currWave)
    {
        currentWaveText.text = $"Wave {currWave}";
        popUpWaveText.text = $"Wave {currWave}";
    }

    public IEnumerator DisplayPopUpWaveText()
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
}

[System.Serializable] 
public struct PopUpTextConfigurations
{
    public float fadeInDuration;
    public float displayDuration;
    public float fadeOutDuration; 
}

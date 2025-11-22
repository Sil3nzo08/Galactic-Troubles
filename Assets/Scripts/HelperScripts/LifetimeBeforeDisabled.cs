using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifetimeBeforeDisabled : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifeTime;


    private void OnEnable()
    {
        StartCoroutine(disableGameObject());
    }

    private void OnDisable()
    {
        StopCoroutine(disableGameObject());
    }

    private IEnumerator disableGameObject()
    {
        yield return new WaitForSeconds(lifeTime);

        gameObject.SetActive(false);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages switching between multiple sprite GameObjects attached to a single object.
/// </summary>
/// <remarks>
/// This controller keeps track of an initial sprite and a collection of alternate sprites,
/// allowing simple runtime sprite swaps by name.
/// </remarks>
public class SpriteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject startingSprite; // The initial sprite that is active when the object is first initialized.

    [Header("Settings")]
    [SerializeField] private List<SpriteObject> spriteObjects; // A list of named sprite mappings used to switch between available visuals.
    


    // =============== SPRITE FUNCTIONALITY BELOW ===============
    private Dictionary<string, GameObject> sprites = new Dictionary<string, GameObject>();
    private GameObject currentSprite;

    /// <summary>Activates the sprite associated with the provided name and deactivates the current one. If name doesn't exist, nothing happens.</summary>
    /// <param name="name">The name of the sprite to switch to.  </param>
    public void SwitchSprite(string name)
    {
        if (!sprites.ContainsKey(name)) { return; }   
        
        // Set previous sprite/image to be invisible
        currentSprite.SetActive(false);

        // Get new sprite and make it appear
        GameObject spriteObject = sprites[name];
        spriteObject.SetActive(true);
        currentSprite = spriteObject;
    }

    // =============== RUNTIME METHODS ===============
    /// <summary>Initializes the current sprite and builds the sprite lookup from serialized data.</summary>
    private void Awake()
    {
        currentSprite = startingSprite;

        foreach (SpriteObject so in spriteObjects)
        {
            sprites.Add(so.name, so.spriteObject);
        }
    }
}

/// <summary>Represents a named sprite mapping for use by the sprite controller.</summary>
[System.Serializable]
public struct SpriteObject
{
    public string name; // The identifier used to look up the sprite.
    public GameObject spriteObject; // The GameObject associated with the sprite.
}

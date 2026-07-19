using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages runtime switching between multiple sprite GameObjects attached to a single object.
/// </summary>
/// <remarks>
/// The controller keeps track of an initial sprite and a collection of alternate sprite mappings,
/// allowing simple sprite swaps by name and synchronizing the change across clients through RPCs.
/// </remarks>
public class SpriteController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject startingSprite; // The sprite that is active by default when the object is initialized.

    [Header("Settings")]
    [SerializeField] private List<SpriteObject> spriteObjects; // A list of named sprite mappings used to switch between available visuals at runtime.
    


    // =============== SPRITE FUNCTIONALITY BELOW ===============
    private Dictionary<string, GameObject> sprites = new Dictionary<string, GameObject>();
    private GameObject currentSprite;

    /// <summary>
    /// Activates the sprite associated with the provided name and deactivates the currently active sprite.
    /// </summary>
    /// <param name="name">The identifier of the sprite to switch to.</param>
    /// <remarks>
    /// If the requested sprite name does not exist in the mapping, the method exits without changing the current sprite.
    /// </remarks>
    public void SwitchSprite(string name)
    {
        if (!sprites.ContainsKey(name)) { return; }   

        // Change the sprite on the host already so it feels "fast" for them
        GameObject spriteObject = sprites[name];
        currentSprite.SetActive(false);
        spriteObject.SetActive(true);

        // Update current sprite reference
        currentSprite = spriteObject;

        // Apply sprite switch to all other clients
        SwitchSpriteServerRpc(name);
    }

    // =================== Hidden Implementation ===================
    /// <summary>
    /// Sends a request to switch the active sprite on the server (doesn't affect the host though).
    /// </summary>
    /// <param name="name">The identifier of the sprite to display.</param>
    [ServerRpc]
    private void SwitchSpriteServerRpc(string name)
    {
        SwitchSpriteClientRpc(name);
    }
    
    /// <summary>
    /// Applies the sprite switch on clients after the server has sent the update (not on the host though).
    /// </summary>
    /// <param name="name">The identifier of the sprite to display.</param>
    [ClientRpc]
    private void SwitchSpriteClientRpc(string name)
    {
        if (IsHost) { return; }

        GameObject spriteObject = sprites[name];
        
        currentSprite.SetActive(false);
        spriteObject.SetActive(true);

        currentSprite = spriteObject;   
    }

    // =============== RUNTIME METHODS ===============
    /// <summary>
    /// Initializes the current sprite and builds the lookup table from serialized sprite data.
    /// </summary>
    private void Awake()
    {
        currentSprite = startingSprite;

        foreach (SpriteObject so in spriteObjects)
        {
            sprites.Add(so.name, so.spriteObject);
        }
    }
}

/// <summary>
/// Represents a named sprite mapping for use by the sprite controller.
/// </summary>
[System.Serializable]
public struct SpriteObject
{
    public string name; // The identifier used to look up the sprite.
    public GameObject spriteObject; // The GameObject associated with the sprite.
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    [Header("Card Settings")]
    public float flipSpeed = 500f;
    private bool isFlipping = false;
    private bool isCompleted = false;

    private GameManager gameManagerRef;

    // this will act as a bool that prevents users from clikcing the card whne the game starts
    // and for the long run maybe when the game is paused as well
    public bool GameLock = true;

    private Guid cardId = Guid.Empty;
    public Guid GetCardId() { return cardId; }
    
    public void Initialize(Texture2D texture, Color color, Guid Id, GameManager gameManager)
    {
        //TODO:: Find a better way to get the child object. getting by name is not entirely reliable.
        Transform planeTransform = transform.Find("CardPlane");
        if (planeTransform != null)
        {
            Renderer planeRenderer = planeTransform.GetComponent<Renderer>();
            if (planeRenderer != null)
            {
                Material mat = planeRenderer.material;
                mat.SetColor("_Color", color);
                
                if (texture != null)
                {
                    mat.SetTexture("_Texture", texture);
                }
            }
        }
        gameManagerRef = gameManager;
        cardId = Id;
    }

    public void FlipCard()
    {
        StartCoroutine(Flip());
    }

    private void OnMouseDown()
    {
        // checks if items should be clickable
        if (GameLock) return;
        
        if (gameManagerRef.CanFlipCard())  // Check BEFORE flipping
        {
            StartCoroutine(FlipAndNotify());
        }
    }

    private IEnumerator FlipAndNotify()
    {
        yield return StartCoroutine(Flip());
        gameManagerRef.OnCardFlipped(this);
    }

    IEnumerator Flip()
    {
        isFlipping = true;
        var rotated = 0f;
        while (rotated < 180f)
        {
            // this line makes sure that the card doesnt over flip
            var step = Mathf.Min(flipSpeed * Time.deltaTime, 180f - rotated);
            transform.Rotate(0, 0, step);
            rotated += step;
            yield return null;
        }
        
        isFlipping = false;
    }
    
    public void SetCompleted(bool completed)
    {
        isCompleted = completed;
    }

    public bool IsCompleted()
    {
        return isCompleted;
    }

}

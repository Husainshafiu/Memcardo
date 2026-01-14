using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum CardFaceState
{
    FaceDown,
    FaceUp
}

public class Card : MonoBehaviour
{
    [Header("Card Settings")]
    public float flipSpeed = 500f;
    private bool isFlipping = false;
    private bool isCompleted = false;
    private CardFaceState faceState = CardFaceState.FaceDown;

    [Header("Audio")]
    public AudioClip[] flipSounds;
    private AudioSource audioSource;

    private GameManager gameManagerRef;

    // this will act as a bool that prevents users from clikcing the card whne the game starts
    // and for the long run maybe when the game is paused as well
    public bool GameLock = true;

    private Guid cardId = Guid.Empty;
    private Color cardColor;
    public Guid GetCardId() { return cardId; }
    public Color GetCardColor() { return cardColor; }
    public CardFaceState GetFaceState() { return faceState; }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = 0.3f;
        audioSource.pitch = 1.5f;
    }

    
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
        cardColor = color;
        faceState = CardFaceState.FaceUp;
    }

    public void FlipCard(Action onComplete = null)
    {
        StartCoroutine(Flip(onComplete));
    }
    
    public void EnsureFaceDown(Action onComplete = null)
    {
        if (faceState == CardFaceState.FaceDown)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(Flip(onComplete));
    }
    
    public void EnsureFaceUp(Action onComplete = null)
    {
        if (faceState == CardFaceState.FaceUp)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(Flip(onComplete));
    }

    private void OnMouseDown()
    {
        // checks if items should be clickable
        if (GameLock) return;
        
        // Don't allow flipping if already flipping or completed
        if (isFlipping || isCompleted) return;
        
        // Check with GameManager if this specific card can be flipped
        if (gameManagerRef.CanFlipCard(this))
        {
            StartCoroutine(FlipAndNotify());
        }
    }

    private IEnumerator FlipAndNotify()
    {
        yield return StartCoroutine(Flip());
        gameManagerRef.OnCardFlipped(this);
    }

    IEnumerator Flip(Action onComplete = null)
    {
        isFlipping = true;
        if (flipSounds != null && flipSounds.Length > 0 && audioSource != null)
        {
            int randomIndex = Random.Range(0, flipSounds.Length);
            audioSource.PlayOneShot(flipSounds[randomIndex]);
        }

        var rotated = 0f;
        while (rotated < 180f)
        {
            // this line makes sure that the card doesnt over flip
            var step = Mathf.Min(flipSpeed * Time.deltaTime, 180f - rotated);
            transform.Rotate(0, 0, step);
            rotated += step;
            yield return null;
        }
        
        // Toggle face state after flip completes
        faceState = faceState == CardFaceState.FaceDown ? CardFaceState.FaceUp : CardFaceState.FaceDown;
        
        isFlipping = false;
        onComplete?.Invoke();
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

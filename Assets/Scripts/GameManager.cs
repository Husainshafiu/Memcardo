using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public float previewTime = 0.1f;
    public float preGameDuration = 2f;
    
    [Header("Grid Settings")]
    [SerializeField] private int gridX = 4;
    [SerializeField] private int gridY = 4;
    [SerializeField] private float cardSpacing = 1f;

    [Header("Card Assets")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Texture2D[] cardTextures;
    [SerializeField] private Color[] cardColors;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraPadding = 2f;
    [SerializeField] private Vector2 cameraOffset = Vector2.zero;

    private Card cardA = null;
    private Card cardB = null;
    private List<(Card cardA, Card cardB)> processingQueue = new List<(Card, Card)>();
    
    private int pairsNeededToWin = -1;
    private int currentMatchedPairs = -1;
    private Vector2 lastViewportSize;
    
    private TextMeshProUGUI scoreText;
    private int score = 0;
    
    private List<Card> allCards = new List<Card>();
    private List<int> cardTextureIndices = new List<int>();

    public TMP_InputField GridXInput;
    public TMP_InputField GridYInput;
    public TextMeshProUGUI completedText;
    
    [Header("Audio")]
    public AudioClip[] matchSounds;
    public AudioClip[] mismatchSounds;
    public AudioClip[] completeSounds;
    private AudioSource audioSource;
    
    
    private void OnValidate()
    {
        if (gridX * gridY % 2 != 0)
        {
            Debug.LogError("Grid size must be even (total cards must be divisible by 2)");
        }
    }

    private void Start()
    {
        if (cardPrefab == null)
        {
            Debug.LogError("Card prefab is not assigned");
            return;
        }

        scoreText = GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>();
        
        if (completedText)
            completedText.gameObject.SetActive(false);
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.volume = 0.3f;
        audioSource.pitch = 1.5f;
        
        // Check if save exists
        if (SaveManager.SaveExists())
        {
            LoadGame();
        }
        else
        {
            StartNewGame();
        }
        
        // fit the camera to the grid - on update check for screen size changes to retrigger
        lastViewportSize = new Vector2(Screen.width, Screen.height);
        FitCameraToGrid();
    }
    
    private void StartNewGame()
    {
        int totalCards = gridX * gridY;
        if (totalCards % 2 != 0)
        {
            Debug.LogError("Cannot create memory game with an odd number of cards. total number of cards must be even");
            return;
        }

        var cardsData = CreateAndShuffleCardData(totalCards / 2);
        SpawnCards(cardsData);
        
        // Flip all cards after the game duration to begin the game
        Invoke(nameof(CallAllCardsToFlip), preGameDuration);

        // this sets the points needed to win and resets the current matched pairs for starting the game
        pairsNeededToWin = totalCards / 2;
        currentMatchedPairs = 0;
        score = 0;
        
        UpdateUI();
        FitCameraToGrid();
    }

    void CallAllCardsToFlip()
    {
        var cards = GetComponentsInChildren<Card>();
        foreach (var card in cards)
        {
            card.FlipCard();
            card.GameLock = false;
        }
    }

    void Update()
    {
        // instead of doing every frame check changes in screen size to trigger camera size changes
        var currentSize = new Vector2(Screen.width, Screen.height);
        if (currentSize != lastViewportSize)
        {
            FitCameraToGrid();
            lastViewportSize = currentSize;
        }
    }

    private List<CardData> CreateAndShuffleCardData(int pairCount)
    {
        var data = new List<CardData>(pairCount * 2);
        cardTextureIndices.Clear();

        for (int i = 0; i < pairCount; i++)
        {
            int textureIndex = i % (cardTextures != null && cardTextures.Length > 0 ? cardTextures.Length : 1);
            var texture = GetTexture(i);
            var color = GetColor(i);
            var id = Guid.NewGuid();

            data.Add(new CardData(texture, color, id, textureIndex));
            data.Add(new CardData(texture, color, id, textureIndex));
            
            cardTextureIndices.Add(textureIndex);
            cardTextureIndices.Add(textureIndex);
        }
        
        // Shuffle the cards around
        for (int i = 0; i < data.Count; i++)
        {
            int randomIndex = Random.Range(i, data.Count);
            CardData temp = data[i];
            data[i] = data[randomIndex];
            data[randomIndex] = temp;
            
            int tempIndex = cardTextureIndices[i];
            cardTextureIndices[i] = cardTextureIndices[randomIndex];
            cardTextureIndices[randomIndex] = tempIndex;
        }

        return data;
    }

    private Texture2D GetTexture(int index) =>
        cardTextures != null && cardTextures.Length > 0
            ? cardTextures[index % cardTextures.Length]
            : null;

    private Color GetColor(int index) =>
        cardColors != null && index < cardColors.Length
            ? cardColors[index]
            : Random.ColorHSV();

    private void SpawnCards(IReadOnlyList<CardData> cardsData)
    {
        float centerX = (gridX - 1) * cardSpacing * 0.5f;
        float centerZ = (gridY - 1) * cardSpacing * 0.5f;

        int index = 0;
        allCards.Clear();

        for (int x = 0; x < gridX; x++)
        for (int z = 0; z < gridY; z++)
        {
            Vector3 pos = transform.position + new Vector3(
                x * cardSpacing - centerX,
                0f,
                z * cardSpacing - centerZ);

            var cardObj = Instantiate(cardPrefab, pos, Quaternion.Euler(0, 180, 0), transform);
            var card = cardObj.GetComponent<Card>();

            if (card != null)
            {
                card.Initialize(cardsData[index].Texture, cardsData[index].Color, cardsData[index].Id, this);
                allCards.Add(card);
            }
            index++;
        }
    }

    private void FitCameraToGrid()
    {
        var cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No camera found or assigned.");
            return;
        }

        float width  = gridX * cardSpacing;
        float height = gridY * cardSpacing;
        
        float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
        float aspectRatio = cam.aspect;
        
        float distanceForHeight = (height / 2f) / Mathf.Tan(fovRad / 2f);
        float distanceForWidth = (width / 2f) / (Mathf.Tan(fovRad / 2f) * aspectRatio);
        
        float requiredDistance = Mathf.Max(distanceForHeight, distanceForWidth) + cameraPadding;

        Vector3 center = transform.position + new Vector3(cameraOffset.x, 0f, cameraOffset.y);

        cam.transform.SetPositionAndRotation(
            center + Vector3.up * requiredDistance,
            Quaternion.LookRotation(center - (center + Vector3.up * requiredDistance))
        );
    }
   
    public void OnCardFlipped(Card card)
    {
        if (IsCardInQueue(card)) return;
        
        if (cardA == null)
        {
            cardA = card;
            SaveGame(); // Save when first card is flipped
        }
        else if (card == cardA)
        {
            // Same card clicked twice - deselect it by flipping back
            cardA.EnsureFaceDown();
            cardA = null;
            SaveGame(); // Save when card is deselected
        }
        else if (cardB == null)
        {
            cardB = card;
            
            // Add pair to queue and start processing
            processingQueue.Add((cardA, cardB));
            StartCoroutine(CheckMatch(cardA, cardB));
            
            // Reset for next pair immediately
            cardA = null;
            cardB = null;
        }
    }
    
    private bool IsCardInQueue(Card card)
    {
        foreach (var pair in processingQueue)
        {
            if (pair.cardA == card || pair.cardB == card)
                return true;
        }
        return false;
    }
    
    private IEnumerator CheckMatch(Card pairA, Card pairB)
    {
        yield return new WaitForSeconds(previewTime);
        
        // Check if the cards match using their IDs
        if (pairA.GetCardId() == pairB.GetCardId())
        {
            // Play match sound
            if (matchSounds != null && matchSounds.Length > 0 && audioSource != null)
            {
                int randomIndex = Random.Range(0, matchSounds.Length);
                audioSource.PlayOneShot(matchSounds[randomIndex]);
            }
            
            pairA.EnsureFaceUp();
            pairB.EnsureFaceUp();
            pairA.SetCompleted(true);
            pairB.SetCompleted(true);
            
            currentMatchedPairs++;
            AddScore(10);
            SaveGame(); 
            CheckGameComplete();
        }
        else
        {
            // Play mismatch sound
            if (mismatchSounds != null && mismatchSounds.Length > 0 && audioSource != null)
            {
                int randomIndex = Random.Range(0, mismatchSounds.Length);
                audioSource.PlayOneShot(mismatchSounds[randomIndex]);
            }
            
            bool flipComplete = false;
            int flipsCompleted = 0;
            
            // Ensure both cards flip back to face down state
            pairA.EnsureFaceDown(() => {
                flipsCompleted++;
                if (flipsCompleted == 2) flipComplete = true;
            });
            
            pairB.EnsureFaceDown(() => {
                flipsCompleted++;
                if (flipsCompleted == 2) flipComplete = true;
            });
            
            // Wait until both flips complete
            yield return new WaitUntil(() => flipComplete);
            SaveGame();
        }
        
        // Remove from queue
        processingQueue.Remove((pairA, pairB));
    }

    private void CheckGameComplete()
    {
        if (currentMatchedPairs == pairsNeededToWin)
            ProcessGameCompletion();
    }

    private void ProcessGameCompletion()
    {
        Debug.Log("Game Completed! All pairs matched.");
        
        // Play completion sound
        if (completeSounds != null && completeSounds.Length > 0 && audioSource != null)
        {
            int randomIndex = Random.Range(0, completeSounds.Length);
            audioSource.PlayOneShot(completeSounds[randomIndex]);
        }
        
        SaveManager.DeleteSave();
        StartCoroutine(ShowCompletionAndRestart());
    }
    
    private IEnumerator ShowCompletionAndRestart()
    {
        if (completedText)
            completedText.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(1f);
        
        if (completedText)
            completedText.gameObject.SetActive(false);
        
        NewGame();
    }

    public bool CanFlipCard(Card card)
    {
        // Dont allow flipping cards that are in the processing queue
        return !IsCardInQueue(card);
    }
    
    public void NewGame()
    {
        // only set the new grid fields if they are valid
        if (GridXInput &&  GridYInput)
        {
            Debug.Log("New Game Started!" + GridXInput.text + "x" + GridYInput.text);
            var gridXValue = int.TryParse(GridXInput.text, out gridX) ? gridX : -1;
            if (gridXValue != -1)
                gridX = gridXValue;
            
            var gridYValue = int.TryParse(GridYInput.text, out gridY) ? gridY : -1;
            if (gridYValue != -1)
                gridY = gridYValue;
        }
        
        // Clear existing cards
        foreach (var card in allCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        allCards.Clear();
        
        // Delete save and start new game
        SaveManager.DeleteSave();
        StartNewGame();
    }
    
    private void SaveGame()
    {
        GameSaveData saveData = new GameSaveData
        {
            score = score,
            gridX = gridX,
            gridY = gridY,
            cardSpacing = cardSpacing,
            currentMatchedPairs = currentMatchedPairs
        };

        for (int i = 0; i < allCards.Count; i++)
        {
            Card card = allCards[i];
            CardSaveData cardData = new CardSaveData
            {
                guid = card.GetCardId().ToString(),
                textureIndex = cardTextureIndices[i],
                color = card.GetCardColor(),
                position = card.transform.position,
                isCompleted = card.IsCompleted(),
                isFaceUp = card.GetFaceState() == CardFaceState.FaceUp
            };
            saveData.cards.Add(cardData);
        }

        SaveManager.SaveGame(saveData);
    }
    
    private void LoadGame()
    {
        GameSaveData saveData = SaveManager.LoadGame();
        if (saveData == null)
        {
            StartNewGame();
            return;
        }

        // Load grid settings
        gridX = saveData.gridX;
        gridY = saveData.gridY;
        cardSpacing = saveData.cardSpacing;
        score = saveData.score;
        currentMatchedPairs = saveData.currentMatchedPairs;
        pairsNeededToWin = (gridX * gridY) / 2;

        // Spawn cards from save data all cards start face-up for preview
        allCards.Clear();
        cardTextureIndices.Clear();

        foreach (CardSaveData cardData in saveData.cards)
        {
            Guid cardGuid = Guid.Parse(cardData.guid);
            Texture2D texture = cardTextures != null && cardData.textureIndex < cardTextures.Length 
                ? cardTextures[cardData.textureIndex] 
                : null;

            // Spawn all cards faceup initially for preview
            var cardObj = Instantiate(cardPrefab, cardData.position, 
                Quaternion.Euler(0, 180, 0), transform);
            var card = cardObj.GetComponent<Card>();

            if (card != null)
            {
                card.Initialize(texture, cardData.color, cardGuid, this);
                card.SetCompleted(cardData.isCompleted);
                card.GameLock = true; // Lock cards during preview
                allCards.Add(card);
                cardTextureIndices.Add(cardData.textureIndex);
                Debug.Log($"Loaded card with GUID: {cardGuid}");
            }
        }

        // After preview duration, flip noncompleted cards back to facedown
        Invoke(nameof(FlipNonCompletedCardsDown), preGameDuration);

        UpdateUI();
        Debug.Log("Game loaded successfully!");
    }
    
    private void FlipNonCompletedCardsDown()
    {
        foreach (var card in allCards)
        {
            if (!card.IsCompleted())
            {
                card.FlipCard();
            }
            card.GameLock = false; // Unlock all cards for play
        }
    }

    private readonly struct CardData
    {
        public readonly Texture2D Texture;
        public readonly Color Color;
        public readonly Guid Id;
        public readonly int TextureIndex;

        public CardData(Texture2D texture, Color color, Guid id, int textureIndex)
        {
            Texture = texture;
            Color = color;
            Id = id;
            TextureIndex = textureIndex;
        }
    }

    #region Score

    private void AddScore(int pointsToAdd)
    {
        score += pointsToAdd;
        UpdateUI();
        StartCoroutine(ScoreBounceAnimation(scoreText));
    }

    private void UpdateUI()
    {
        if (scoreText)
        {
            scoreText.SetText(score.ToString());
        }
    }
    
    private IEnumerator ScoreBounceAnimation(TextMeshProUGUI text)
    {
        if (!text) yield break;
        
        Transform textTransform = text.transform;
        Vector3 originalScale = textTransform.localScale;
        Quaternion originalRotation = textTransform.localRotation;
        
        float duration = 0.2f;
        float elapsed = 0f;
        
        // Random rotation angle
        float randomRotation = Random.Range(-15f, 15f);
        
        // Scale up and rotate
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float bounce = 1f + (Mathf.Sin(t * Mathf.PI) * 1f);
            textTransform.localScale = originalScale * bounce;
            textTransform.localRotation = Quaternion.Euler(0, 0, randomRotation * (1f - t));
            yield return null;
        }
        
        textTransform.localScale = originalScale;
        textTransform.localRotation = originalRotation;
    }

    #endregion
    
    public void BackToMenu()
    {
        if (SceneManager.GetSceneByName("MainMenuScene").IsValid() || Application.CanStreamedLevelBeLoaded("MainMenuScene"))
        {
            SceneManager.LoadScene("MainMenuScene");
        }
        else
        {
            Debug.LogError("Scene 'MainMenuScene' not found in Build Settings!");
        }
    }
}
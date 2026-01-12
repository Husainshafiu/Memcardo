using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
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

        int totalCards = gridX * gridY;
        if (totalCards % 2 != 0)
        {
            Debug.LogError("Cannot create memory game with an odd number of cards. total number of cards must be even");
            return;
        }

        var cardsData = CreateAndShuffleCardData(totalCards / 2);
        SpawnCards(cardsData);
        FitCameraToGrid();
    }

    void Update()
    {
        FitCameraToGrid();
    }

    private List<CardData> CreateAndShuffleCardData(int pairCount)
    {
        var data = new List<CardData>(pairCount * 2);

        for (int i = 0; i < pairCount; i++)
        {
            Texture2D texture = GetTexture(i);
            Color color = GetColor(i);

            data.Add(new CardData(texture, color));
            data.Add(new CardData(texture, color));
        }

        // Shuffle the cards around
        for (int i = 0; i < data.Count; i++)
        {
            int randomIndex = Random.Range(i, data.Count);
            CardData temp = data[i];
            data[i] = data[randomIndex];
            data[randomIndex] = temp;
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

        for (int x = 0; x < gridX; x++)
        for (int z = 0; z < gridY; z++)
        {
            Vector3 pos = transform.position + new Vector3(
                x * cardSpacing - centerX,
                0f,
                z * cardSpacing - centerZ);

            var cardObj = Instantiate(cardPrefab, pos, Quaternion.identity, transform);
            var card = cardObj.GetComponent<Card>();

            if (card != null)
            {
                card.Initialize(cardsData[index].Texture, cardsData[index].Color);
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
        float maxSize = Mathf.Max(width, height);

        Vector3 center = transform.position + new Vector3(cameraOffset.x, 0f, cameraOffset.y);

        cam.transform.SetPositionAndRotation(
            center + Vector3.up * (maxSize + cameraPadding),
            Quaternion.LookRotation(center - (center + Vector3.up * (maxSize + cameraPadding)))
        );
    }
   
    private readonly struct CardData
    {
        public readonly Texture2D Texture;
        public readonly Color Color;

        public CardData(Texture2D texture, Color color)
        {
            Texture = texture;
            Color = color;
        }
    }
}
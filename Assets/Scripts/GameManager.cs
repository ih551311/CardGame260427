using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core References")]
    public GameObject cardPrefab;           // Card prefab (Button + Image + Card.cs)
    public Transform gridParent;            // Parent with GridLayoutGroup
    public TextMeshProUGUI statusText;      // "Matched: 0/6"

    [Header("Game Settings - Inspector에서 여기만 조절하세요!")]
    [Range(1, 12)]
    public int numberOfPairs = 6;           // 페어 개수만 조절하면 카드 수가 자동으로 바뀝니다
    public float matchDelay = 0.7f;         // 불일치 시 뒤집히기 전 대기 시간

    [Header("Card Sprites")]
    public Sprite cardBackSprite;
    public List<Sprite> cardFrontSprites = new List<Sprite>(); // 12개 이상 추천

    // Internal
    private List<Card> allCards = new List<Card>();
    private Card firstSelected;
    private Card secondSelected;
    public bool isProcessing { get; private set; } = false;
    private int matchedPairs = 0;
    private int totalPairs = 0;
    private GridLayoutGroup gridLayout;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        gridLayout = gridParent.GetComponent<GridLayoutGroup>();
        GenerateGame();
    }

    /// <summary>
    /// 인스펙터에서 numberOfPairs 변경 후 Play 버튼 누르면 자동 실행
    /// </summary>
    public void GenerateGame()
    {
        // 이전 카드 모두 삭제
        ClearGrid();

        totalPairs = Mathf.Clamp(numberOfPairs, 1, cardFrontSprites.Count);
        int totalCards = totalPairs * 2;

        // 그리드 자동 계산 (대략 정사각형에 가깝게)
        int columns = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(totalCards)), 2, 8);
        int rows = Mathf.CeilToInt(totalCards / (float)columns);

        SetupGridLayout(rows, columns);
        CreateCards(totalPairs);

        // 상태 초기화
        matchedPairs = 0;
        firstSelected = null;
        secondSelected = null;
        isProcessing = false;

        UpdateStatus();
    }

    private void SetupGridLayout(int rows, int cols)
    {
        if (gridLayout == null) return;

        RectTransform parentRect = gridParent.GetComponent<RectTransform>();
        float availableWidth = parentRect.rect.width;
        float availableHeight = parentRect.rect.height;

        float cellWidth = (availableWidth - (cols - 1) * gridLayout.spacing.x) / cols;
        float cellHeight = (availableHeight - (rows - 1) * gridLayout.spacing.y) / rows;
        float cellSize = Mathf.Min(cellWidth, cellHeight) * 0.98f;

        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = cols;
    }

    private void CreateCards(int pairs)
    {
        // 페어 ID 리스트 생성 (0 ~ pairs-1) × 2
        List<int> pairIDs = new List<int>();
        for (int i = 0; i < pairs; i++)
        {
            pairIDs.Add(i);
            pairIDs.Add(i);
        }
        pairIDs = pairIDs.OrderBy(x => Random.value).ToList(); // 셔플

        allCards.Clear();

        for (int i = 0; i < pairIDs.Count; i++)
        {
            GameObject cardGO = Instantiate(cardPrefab, gridParent);
            Card card = cardGO.GetComponent<Card>();

            if (card == null)
            {
                Debug.LogError("Card Prefab에 Card.cs가 없습니다!");
                continue;
            }

            int pairIndex = pairIDs[i];
            Sprite front = cardFrontSprites[pairIndex % cardFrontSprites.Count];

            card.Initialize(cardBackSprite, front, pairIndex);
            allCards.Add(card);
        }
    }

    private void ClearGrid()
    {
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);

        allCards.Clear();
    }

    public void OnCardSelected(Card card)
    {
        if (isProcessing || card.isMatched || card.isFlipped)
            return;

        card.Flip(true);

        if (firstSelected == null)
        {
            firstSelected = card;
        }
        else if (secondSelected == null && card != firstSelected)
        {
            secondSelected = card;
            StartCoroutine(CheckForMatch());
        }
    }

    private IEnumerator CheckForMatch()
    {
        isProcessing = true;
        yield return new WaitForSeconds(matchDelay);

        if (firstSelected.pairID == secondSelected.pairID)
        {
            firstSelected.SetMatched();
            secondSelected.SetMatched();
            matchedPairs++;
            UpdateStatus();

            // 모든 페어 완성 시 (간단히 로그만)
            if (matchedPairs >= totalPairs)
            {
                Debug.Log("모든 카드 매치 완료!");
                // 원하면 여기에 간단한 텍스트 변경 가능
                if (statusText != null)
                    statusText.text = "완료! 모든 짝을 찾았습니다 🎉";
            }
        }
        else
        {
            firstSelected.Flip(false);
            secondSelected.Flip(false);
        }

        firstSelected = null;
        secondSelected = null;
        isProcessing = false;
    }

    private void UpdateStatus()
    {
        if (statusText != null)
        {
            statusText.text = $"Matched: {matchedPairs} / {totalPairs}";
        }
    }

    // 인스펙터에서 numberOfPairs를 바꾼 후 "Generate Game" 버튼을 만들고 싶다면
    // 이 메서드를 Button.onClick에 연결하세요 (선택사항)
    public void RegenerateWithCurrentPairs()
    {
        GenerateGame();
    }
}
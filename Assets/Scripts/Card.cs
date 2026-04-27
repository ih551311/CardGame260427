using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Card : MonoBehaviour
{
    [Header("References")]
    public Image cardImage;           // The UI Image component
    public Button cardButton;         // The Button for click detection

    [Header("Sprites")]
    public Sprite backSprite;         // Assigned by GameManager
    public Sprite frontSprite;        // Assigned by GameManager

    [Header("Animation Settings")]
    public float flipDuration = 0.25f;
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // State
    [HideInInspector] public int pairID;
    [HideInInspector] public bool isFlipped = false;
    [HideInInspector] public bool isMatched = false;

    private GameManager gameManager;
    private RectTransform rectTransform;
    private Coroutine flipCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (cardButton == null)
            cardButton = GetComponent<Button>();
        
        if (cardImage == null)
            cardImage = GetComponent<Image>();

        // Find GameManager (singleton pattern)
        gameManager = GameManager.Instance;
        
        // Setup button listener
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardClicked);
        }
    }

    public void Initialize(Sprite back, Sprite front, int id)
    {
        backSprite = back;
        frontSprite = front;
        pairID = id;
        
        // Start face down (back)
        cardImage.sprite = backSprite;
        isFlipped = false;
        isMatched = false;
        
        // Reset scale and rotation
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        
        // Enable button
        if (cardButton != null)
            cardButton.interactable = true;
    }

    private void OnCardClicked()
    {
        if (isMatched || isFlipped || gameManager == null || gameManager.isProcessing)
            return;

        gameManager.OnCardSelected(this);
    }

    public void Flip(bool faceUp, bool instant = false)
    {
        if (flipCoroutine != null)
            StopCoroutine(flipCoroutine);

        if (instant)
        {
            cardImage.sprite = faceUp ? frontSprite : backSprite;
            isFlipped = faceUp;
            rectTransform.localScale = Vector3.one;
            return;
        }

        flipCoroutine = StartCoroutine(FlipAnimation(faceUp));
    }

    private IEnumerator FlipAnimation(bool faceUp)
    {
        float elapsed = 0f;
        Vector3 startScale = rectTransform.localScale;
        Vector3 targetScale = new Vector3(0f, 1f, 1f); // Squeeze to 0 width

        // First half: shrink to 0
        while (elapsed < flipDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (flipDuration * 0.5f);
            float curvedT = flipCurve.Evaluate(t);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, curvedT);
            yield return null;
        }

        // Switch sprite at the middle (when width is 0)
        cardImage.sprite = faceUp ? frontSprite : backSprite;
        isFlipped = faceUp;

        // Second half: expand back
        elapsed = 0f;
        startScale = rectTransform.localScale; // Should be ~ (0,1,1)
        
        while (elapsed < flipDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (flipDuration * 0.5f);
            float curvedT = flipCurve.Evaluate(t);
            rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one, curvedT);
            yield return null;
        }

        rectTransform.localScale = Vector3.one;
        flipCoroutine = null;
    }

    public void SetMatched()
    {
        isMatched = true;
        if (cardButton != null)
            cardButton.interactable = false;
        
        // Optional: add a little "pop" animation or highlight
        StartCoroutine(MatchedPopAnimation());
    }

    private IEnumerator MatchedPopAnimation()
    {
        Vector3 originalScale = rectTransform.localScale;
        float popDuration = 0.15f;
        
        // Scale up
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            rectTransform.localScale = Vector3.Lerp(originalScale, originalScale * 1.15f, t);
            yield return null;
        }
        
        // Scale back
        elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            rectTransform.localScale = Vector3.Lerp(originalScale * 1.15f, originalScale, t);
            yield return null;
        }
        
        rectTransform.localScale = originalScale;
    }

    public void ResetCard()
    {
        isFlipped = false;
        isMatched = false;
        cardImage.sprite = backSprite;
        rectTransform.localScale = Vector3.one;
        
        if (cardButton != null)
            cardButton.interactable = true;
    }
}
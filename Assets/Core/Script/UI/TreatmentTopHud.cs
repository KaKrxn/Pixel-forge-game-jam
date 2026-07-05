using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TreatmentTopHud : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Patient")]
    [SerializeField] private TMP_Text patientNameText;

    [Header("Character Visual")]
    [SerializeField] private GameObject characterVisualRoot;
    [SerializeField] private Image characterVisualImage;
    [SerializeField] private Sprite characterVisualSprite;
    [SerializeField] private bool useCustomerSpriteWhenEmpty;

    [Header("Sanity")]
    [SerializeField] private Slider sanitySlider;
    [SerializeField] private TMP_Text sanityValueText;
    [SerializeField] private Image sanityFillImage;
    [SerializeField] private Color stableColor = new Color(0.65f, 0.12f, 0.16f);
    [SerializeField] private Color warningColor = new Color(0.9f, 0.58f, 0.14f);
    [SerializeField] private Color criticalColor = new Color(0.95f, 0.12f, 0.08f);
    [SerializeField] private Color transformedColor = new Color(0.45f, 0f, 0f);

    [Header("Treatment Dialogue")]
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text lineText;
    [SerializeField, TextArea(1, 3)] private string defaultTreatmentLine = "Please be careful... it really stings.";

    private CustomerAgent customer;
    private Sanity sanity;
    private TreatmentCaseData caseData;
    private DialogData dialogData;
    private SanityState lastDialogueState = SanityState.Stable;
    private bool subscribed;

    private void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshPatient();
        RefreshSanity();
    }

    public void Bind(CustomerAgent nextCustomer)
    {
        Unsubscribe();

        customer = nextCustomer;
        sanity = customer != null ? customer.GetComponent<Sanity>() : null;

        CustomerCaseProvider caseProvider = customer != null ? customer.GetComponent<CustomerCaseProvider>() : null;
        caseData = caseProvider != null ? caseProvider.CaseData : null;
        dialogData = caseData != null ? caseData.DialogData : null;

        SetVisible(customer != null);
        Subscribe();
        RefreshPatient();
        RefreshSanity();
        SetCharacterVisualVisible(false);
        ShowDefaultTreatmentLine();
    }

    public void Clear()
    {
        Unsubscribe();

        customer = null;
        sanity = null;
        caseData = null;
        dialogData = null;

        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        EnsureRoot();

        if (root != null)
        {
            root.SetActive(visible);
        }
    }

    public void ShowLine(DialogSpeaker speaker, string text)
    {
        if (speakerText != null)
        {
            speakerText.text = ResolveSpeakerName(speaker);
        }

        if (lineText != null)
        {
            lineText.text = string.IsNullOrWhiteSpace(text) ? defaultTreatmentLine : text;
        }
    }

    public void SetCharacterVisualVisible(bool visible)
    {
        if (characterVisualRoot != null)
        {
            characterVisualRoot.SetActive(visible);
        }

        if (visible)
        {
            RefreshCharacterVisual();
        }
    }

    public void ShowPainLine(float sanityAmount)
    {
        if (dialogData == null)
        {
            ShowDefaultTreatmentLine();
            return;
        }

        IReadOnlyList<DialogLine> candidates = sanityAmount >= 65f
            ? dialogData.PainDialogue.Severe
            : sanityAmount >= 35f
                ? dialogData.PainDialogue.Moderate
                : dialogData.PainDialogue.Minor;

        ShowRandomLine(candidates, DialogSpeaker.Customer, defaultTreatmentLine);
    }

    private void Subscribe()
    {
        if (subscribed || sanity == null)
        {
            return;
        }

        sanity.SanityChanged += HandleSanityChanged;
        sanity.StateChanged += HandleSanityStateChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || sanity == null)
        {
            return;
        }

        sanity.SanityChanged -= HandleSanityChanged;
        sanity.StateChanged -= HandleSanityStateChanged;
        subscribed = false;
    }

    private void EnsureRoot()
    {
        if (root == null)
        {
            root = gameObject;
        }
    }

    private void RefreshPatient()
    {
        if (patientNameText != null)
        {
            patientNameText.text = ResolvePatientName();
        }

        RefreshCharacterVisual();
    }

    private void RefreshCharacterVisual()
    {
        if (characterVisualImage != null)
        {
            Sprite portrait = ResolvePortrait();
            characterVisualImage.sprite = portrait;
            characterVisualImage.enabled = portrait != null;

            if (portrait != null && characterVisualImage.color.a <= 0f)
            {
                Color color = characterVisualImage.color;
                color.a = 1f;
                characterVisualImage.color = color;
            }
        }
    }

    private void RefreshSanity()
    {
        if (sanity == null)
        {
            HandleSanityChanged(0f, 100f);
            HandleSanityStateChanged(SanityState.Stable);
            return;
        }

        HandleSanityChanged(sanity.CurrentSanity, sanity.MaxSanity);
        HandleSanityStateChanged(sanity.CurrentState);
    }

    private void HandleSanityChanged(float current, float max)
    {
        float normalized = max <= 0f ? 0f : Mathf.Clamp01(current / max);

        if (sanitySlider != null)
        {
            sanitySlider.value = normalized;
        }

        if (sanityValueText != null)
        {
            sanityValueText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
        }
    }

    private void HandleSanityStateChanged(SanityState state)
    {
        if (sanityFillImage != null)
        {
            sanityFillImage.color = ResolveSanityColor(state);
        }

        if (state != lastDialogueState)
        {
            lastDialogueState = state;
            ShowSanityLine(state);
        }
    }

    private void ShowSanityLine(SanityState state)
    {
        if (dialogData == null)
        {
            return;
        }

        IReadOnlyList<DialogLine> candidates = state switch
        {
            SanityState.Warning => dialogData.SanityDialogue.Above50,
            SanityState.Critical => dialogData.SanityDialogue.Above80,
            SanityState.Transformed => dialogData.SanityDialogue.Above80,
            _ => dialogData.SanityDialogue.Above20
        };

        ShowRandomLine(candidates, DialogSpeaker.Customer, defaultTreatmentLine);
    }

    private void ShowDefaultTreatmentLine()
    {
        IReadOnlyList<DialogLine> candidates = dialogData != null
            ? dialogData.PainDialogue.Minor
            : null;

        ShowRandomLine(candidates, DialogSpeaker.Customer, defaultTreatmentLine);
    }

    private void ShowRandomLine(IReadOnlyList<DialogLine> candidates, DialogSpeaker fallbackSpeaker, string fallbackText)
    {
        DialogLine line = PickLine(candidates);
        if (line != null)
        {
            ShowLine(line.Speaker, line.Text);
            return;
        }

        ShowLine(fallbackSpeaker, fallbackText);
    }

    private DialogLine PickLine(IReadOnlyList<DialogLine> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        int guard = 0;
        while (guard < candidates.Count)
        {
            DialogLine candidate = candidates[Random.Range(0, candidates.Count)];
            if (candidate != null && !string.IsNullOrWhiteSpace(candidate.Text))
            {
                return candidate;
            }

            guard++;
        }

        return null;
    }

    private string ResolvePatientName()
    {
        if (caseData != null && !string.IsNullOrWhiteSpace(caseData.CustomerDisplayName))
        {
            return caseData.CustomerDisplayName;
        }

        if (dialogData != null && !string.IsNullOrWhiteSpace(dialogData.CustomerDisplayName))
        {
            return dialogData.CustomerDisplayName;
        }

        return customer != null ? customer.name : "Patient";
    }

    private string ResolveSpeakerName(DialogSpeaker speaker)
    {
        if (dialogData != null)
        {
            return dialogData.GetDisplayName(speaker);
        }

        return speaker == DialogSpeaker.Customer ? ResolvePatientName() : "Doctor";
    }

    private Sprite ResolvePortrait()
    {
        if (characterVisualSprite != null)
        {
            return characterVisualSprite;
        }

        if (characterVisualImage != null && characterVisualImage.sprite != null)
        {
            return characterVisualImage.sprite;
        }

        if (useCustomerSpriteWhenEmpty && customer != null)
        {
            SpriteRenderer spriteRenderer = customer.GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                return spriteRenderer.sprite;
            }
        }

        return null;
    }

    private Color ResolveSanityColor(SanityState state)
    {
        return state switch
        {
            SanityState.Warning => warningColor,
            SanityState.Critical => criticalColor,
            SanityState.Transformed => transformedColor,
            _ => stableColor
        };
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class Treatment : MonoBehaviour
{
    [SerializeField] private GameFlow flow;
    [SerializeField] private RoomTransition roomTransition;
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button completeButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private TongsMiniGame tongsMiniGame;

    private CustomerAgent activeCustomer;
    private bool isAtCounter;

    private void Awake()
    {
        if (completeButton != null)
        {
            completeButton.onClick.RemoveListener(CompletePlaceholderTreatment);
            completeButton.onClick.AddListener(CompletePlaceholderTreatment);
        }

        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(ReturnToCounter);
            returnButton.onClick.AddListener(ReturnToCounter);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeTreatment);
            resumeButton.onClick.AddListener(ResumeTreatment);
        }

        Hide();
    }

    private void OnEnable()
    {
        if (tongsMiniGame != null)
        {
            tongsMiniGame.MiniGameCompleted -= CompletePlaceholderTreatment;
            tongsMiniGame.MiniGameCompleted += CompletePlaceholderTreatment;
        }
    }

    private void OnDisable()
    {
        if (tongsMiniGame != null)
        {
            tongsMiniGame.MiniGameCompleted -= CompletePlaceholderTreatment;
        }
    }

    public void Begin(CustomerAgent customer)
    {
        activeCustomer = customer;
        isAtCounter = false;
        Hide();

        if (roomTransition != null)
        {
            roomTransition.ShowTreatmentRoom(() =>
            {
                Show();
                RefreshText();
                BeginTreatmentContent();
            });
            return;
        }

        Show();
        RefreshText();
        BeginTreatmentContent();
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
        }
    }

    private void ReturnToCounter()
    {
        isAtCounter = true;
        flow?.SetTreatmentStress(false);
        tongsMiniGame?.Pause();
        Hide();

        if (roomTransition != null)
        {
            roomTransition.ShowCounterRoom(() =>
            {
                Show();
                RefreshText();
            });
            return;
        }

        Show();
        RefreshText();
    }

    private void ResumeTreatment()
    {
        isAtCounter = false;
        Hide();

        if (roomTransition != null)
        {
            roomTransition.ShowTreatmentRoom(() =>
            {
                Show();
                RefreshText();
                ResumeTreatmentContent();
            });
            return;
        }

        Show();
        RefreshText();
        ResumeTreatmentContent();
    }

    private void CompletePlaceholderTreatment()
    {
        flow?.SetTreatmentStress(false);
        tongsMiniGame?.Stop();
        Hide();
        flow?.CompleteTreatment(activeCustomer);
    }

    private void BeginTreatmentContent()
    {
        if (tongsMiniGame != null)
        {
            tongsMiniGame.Begin(activeCustomer);
        }
    }

    private void ResumeTreatmentContent()
    {
        if (tongsMiniGame != null)
        {
            tongsMiniGame.Resume();
        }
    }

    private void RefreshText()
    {
        if (titleText != null)
        {
            titleText.text = isAtCounter ? "Counter Room" : "Treatment Room";
        }

        if (bodyText != null)
        {
            bodyText.text = isAtCounter
                ? "Placeholder return state. Refill candle will be connected here later. Press Resume to go back to treatment."
                : tongsMiniGame != null
                    ? "Use the tongs to extract every parasite. Release to let pain drain before it spikes Sanity."
                    : "Placeholder treatment state. Minigame is not designed yet, so this screen only proves the room transition and cure flow.";
        }

        if (completeButton != null)
        {
            completeButton.gameObject.SetActive(!isAtCounter && tongsMiniGame == null);
        }

        if (returnButton != null)
        {
            returnButton.gameObject.SetActive(!isAtCounter);
        }

        if (resumeButton != null)
        {
            resumeButton.gameObject.SetActive(isAtCounter);
        }
    }
}

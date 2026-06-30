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
    [SerializeField] private AnatomyController anatomyController;
    [SerializeField] private TongsMiniGame tongsMiniGame;

    private CustomerAgent activeCustomer;
    private bool isAtCounter;

    private void Awake()
    {
        if (completeButton != null)
        {
            completeButton.onClick.RemoveListener(CompleteTreatmentCase);
            completeButton.onClick.AddListener(CompleteTreatmentCase);
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
        if (anatomyController != null)
        {
            anatomyController.TreatmentCompleted -= CompleteTreatmentCase;
            anatomyController.TreatmentCompleted += CompleteTreatmentCase;
            anatomyController.NavigationStateChanged -= RefreshText;
            anatomyController.NavigationStateChanged += RefreshText;
        }

        if (anatomyController == null && tongsMiniGame != null)
        {
            tongsMiniGame.MiniGameCompleted -= CompleteTreatmentCase;
            tongsMiniGame.MiniGameCompleted += CompleteTreatmentCase;
        }
    }

    private void OnDisable()
    {
        if (anatomyController != null)
        {
            anatomyController.TreatmentCompleted -= CompleteTreatmentCase;
            anatomyController.NavigationStateChanged -= RefreshText;
        }

        if (tongsMiniGame != null)
        {
            tongsMiniGame.MiniGameCompleted -= CompleteTreatmentCase;
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
        if (anatomyController != null && !anatomyController.CanReturnToCounter)
        {
            return;
        }

        isAtCounter = true;
        flow?.SetTreatmentStress(false);
        tongsMiniGame?.Pause();
        anatomyController?.PauseForCounter();
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

    private void CompleteTreatmentCase()
    {
        flow?.SetTreatmentStress(false);
        anatomyController?.Stop();
        tongsMiniGame?.Stop();
        Hide();
        flow?.CompleteTreatment(activeCustomer);
    }

    private void BeginTreatmentContent()
    {
        if (anatomyController != null)
        {
            anatomyController.Begin(activeCustomer);
            return;
        }

        if (tongsMiniGame != null)
        {
            tongsMiniGame.Begin(activeCustomer);
        }
    }

    private void ResumeTreatmentContent()
    {
        if (anatomyController != null)
        {
            anatomyController.ResumeAtAnatomyLevel();
            return;
        }

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
                : anatomyController != null
                    ? "Select the body part to inspect. The screen gives no infection hints, so use the customer's dialog clues."
                    : tongsMiniGame != null
                    ? "Use the tongs to extract every parasite. Release to let pain drain before it spikes Sanity."
                    : "Placeholder treatment state. Minigame is not designed yet, so this screen only proves the room transition and cure flow.";
        }

        if (completeButton != null)
        {
            completeButton.gameObject.SetActive(!isAtCounter && anatomyController == null && tongsMiniGame == null);
        }

        if (returnButton != null)
        {
            bool canReturn = anatomyController == null || anatomyController.CanReturnToCounter;
            returnButton.gameObject.SetActive(!isAtCounter && canReturn);
        }

        if (resumeButton != null)
        {
            resumeButton.gameObject.SetActive(isAtCounter);
        }
    }
}

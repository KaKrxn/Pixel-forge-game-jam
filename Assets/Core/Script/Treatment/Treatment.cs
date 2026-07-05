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
    [SerializeField] private TreatmentPatientPresenter patientPresenter;
    [SerializeField] private TreatmentTopHud topHud;
    [Header("Debug")]
    [SerializeField] private bool debugTreatmentFlow = true;

    private CustomerAgent activeCustomer;
    private bool isAtCounter;

    private void Awake()
    {
        ResolveSceneReferences();

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

        topHud?.Clear();
        Hide();
    }

    private void OnEnable()
    {
        if (anatomyController != null)
        {
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
            anatomyController.NavigationStateChanged -= RefreshText;
        }

        if (tongsMiniGame != null)
        {
            tongsMiniGame.MiniGameCompleted -= CompleteTreatmentCase;
        }
    }

    public void Begin(CustomerAgent customer)
    {
        LogTreatmentFlow($"Begin customer={DescribeObject(customer)} anatomy={DescribeObject(anatomyController)} legacyTongs={DescribeObject(tongsMiniGame)} roomTransition={DescribeObject(roomTransition)}");
        activeCustomer = customer;
        isAtCounter = false;
        flow?.SetCandleAtCounter(false);
        Hide();

        if (roomTransition != null)
        {
            roomTransition.ShowTreatmentRoom(() =>
            {
                Show();
                topHud?.Bind(activeCustomer);
                RefreshText();
                RefreshTopHudVisualState();
                BeginTreatmentContent();
            });
            return;
        }

        Show();
        topHud?.Bind(activeCustomer);
        RefreshText();
        RefreshTopHudVisualState();
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
        LogTreatmentFlow($"ReturnToCounter canReturn={(anatomyController == null || anatomyController.CanReturnToCounter)} anatomy={DescribeObject(anatomyController)}");
        if (anatomyController != null && !anatomyController.CanReturnToCounter)
        {
            return;
        }

        isAtCounter = true;
        flow?.SetTreatmentStress(false);
        tongsMiniGame?.Pause();
        anatomyController?.PauseForCounter();
        topHud?.SetVisible(false);
        Hide();

        if (roomTransition != null)
        {
            roomTransition.ShowCounterRoom(() =>
            {
                flow?.SetCandleAtCounter(true);
                Show();
                RefreshText();
                RefreshTopHudVisualState();
            });
            return;
        }

        flow?.SetCandleAtCounter(true);
        Show();
        RefreshText();
    }

    private void ResumeTreatment()
    {
        LogTreatmentFlow($"ResumeTreatment anatomy={DescribeObject(anatomyController)} legacyTongs={DescribeObject(tongsMiniGame)}");
        isAtCounter = false;
        flow?.SetCandleAtCounter(false);
        Hide();

        if (roomTransition != null)
        {
            roomTransition.ShowTreatmentRoom(() =>
            {
                Show();
                topHud?.SetVisible(activeCustomer != null);
                RefreshText();
                RefreshTopHudVisualState();
                ResumeTreatmentContent();
            });
            return;
        }

        Show();
        topHud?.SetVisible(activeCustomer != null);
        RefreshText();
        RefreshTopHudVisualState();
        ResumeTreatmentContent();
    }

    private void CompleteTreatmentCase()
    {
        LogTreatmentFlow($"CompleteTreatmentCase activeCustomer={DescribeObject(activeCustomer)} anatomy={DescribeObject(anatomyController)} legacyTongs={DescribeObject(tongsMiniGame)}");
        flow?.SetTreatmentStress(false);
        flow?.SetCandleAtCounter(false);
        anatomyController?.Stop();
        tongsMiniGame?.Stop();
        topHud?.Clear();
        Hide();
        patientPresenter?.ReturnToCounterForExit(activeCustomer);
        flow?.CompleteTreatment(activeCustomer);
    }

    private void BeginTreatmentContent()
    {
        LogTreatmentFlow($"BeginTreatmentContent anatomy={DescribeObject(anatomyController)} legacyTongs={DescribeObject(tongsMiniGame)}");
        patientPresenter?.MoveToTreatment(activeCustomer);
        if (anatomyController != null)
        {
            LogTreatmentFlow("BeginTreatmentContent route=AnatomyController.Begin");
            anatomyController.Begin(activeCustomer);
            return;
        }

        if (tongsMiniGame != null)
        {
            LogTreatmentFlow("BeginTreatmentContent route=LegacyTongsMiniGame.Begin");
            tongsMiniGame.Begin(activeCustomer);
        }
    }

    private void ResumeTreatmentContent()
    {
        LogTreatmentFlow($"ResumeTreatmentContent anatomy={DescribeObject(anatomyController)} legacyTongs={DescribeObject(tongsMiniGame)}");
        if (anatomyController != null)
        {
            LogTreatmentFlow("ResumeTreatmentContent route=AnatomyController.ResumeAtAnatomyLevel");
            anatomyController.ResumeAtAnatomyLevel();
            return;
        }

        if (tongsMiniGame != null)
        {
            LogTreatmentFlow("ResumeTreatmentContent route=LegacyTongsMiniGame.Resume");
            tongsMiniGame.Resume();
        }
    }

    private void RefreshText()
    {
        LogTreatmentFlow($"RefreshText isAtCounter={isAtCounter} anatomyCanComplete={(anatomyController != null && anatomyController.CanCompleteTreatment)} anatomyCanReturn={(anatomyController == null || anatomyController.CanReturnToCounter)}");
        if (titleText != null)
        {
            titleText.text = isAtCounter ? "Counter Room" : "Treatment Room";
        }

        if (bodyText != null)
        {
            bodyText.text = isAtCounter
                ? "Refill the candle at the counter, then press Resume to return to treatment."
                : anatomyController != null
                    ? anatomyController.CanCompleteTreatment
                        ? "All required treatment areas are cured. Press Complete to finish the case."
                        : "Select the body part to inspect. The screen gives no infection hints, so use the customer's dialog clues."
                    : tongsMiniGame != null
                    ? "Use the tongs to extract every parasite. Release to let pain drain before it spikes Sanity."
                    : "Placeholder treatment state. Minigame is not designed yet, so this screen only proves the room transition and cure flow.";
        }

        if (completeButton != null)
        {
            bool canCompleteFromAnatomy = anatomyController != null && anatomyController.CanCompleteTreatment;
            completeButton.gameObject.SetActive(!isAtCounter && (canCompleteFromAnatomy || (anatomyController == null && tongsMiniGame == null)));
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

        RefreshTopHudVisualState();
    }

    private void RefreshTopHudVisualState()
    {
        // Show the patient avatar throughout treatment (anatomy select + mini game), hidden at the counter.
        bool showCharacterVisual = !isAtCounter;
        topHud?.SetCharacterVisualVisible(showCharacterVisual);
    }

    private void ResolveSceneReferences()
    {
        if (topHud == null)
        {
            topHud = FindFirstObjectByType<TreatmentTopHud>(FindObjectsInactive.Include);
        }
    }

    private void LogTreatmentFlow(string message)
    {
        if (debugTreatmentFlow)
        {
            Debug.Log($"[TreatmentFlow][Treatment] {message}", this);
        }
    }

    private static string DescribeObject(Object target)
    {
        if (target == null)
        {
            return "null";
        }

        return $"{target.name} ({target.GetType().Name})";
    }
}

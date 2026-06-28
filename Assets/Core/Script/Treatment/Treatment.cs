using UnityEngine;
using UnityEngine.UI;

public sealed class Treatment : MonoBehaviour
{
    [SerializeField] private GameFlow flow;
    [SerializeField] private RoomTransition roomTransition;
    [SerializeField] private GameObject root;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Button completeButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private Button resumeButton;

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

    public void Begin(CustomerAgent customer)
    {
        activeCustomer = customer;
        isAtCounter = false;
        roomTransition?.ShowTreatmentRoom();
        Show();
        RefreshText();
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
        roomTransition?.ShowCounterRoom();
        RefreshText();
    }

    private void ResumeTreatment()
    {
        isAtCounter = false;
        roomTransition?.ShowTreatmentRoom();
        RefreshText();
    }

    private void CompletePlaceholderTreatment()
    {
        Hide();
        flow?.CompleteTreatment(activeCustomer);
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
                : "Placeholder treatment state. Minigame is not designed yet, so this screen only proves the room transition and cure flow.";
        }

        if (completeButton != null)
        {
            completeButton.gameObject.SetActive(!isAtCounter);
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

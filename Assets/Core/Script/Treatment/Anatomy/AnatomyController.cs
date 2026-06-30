using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AnatomyController : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject bodyRoot;
    [SerializeField] private GameObject partMessageRoot;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text partMessageText;
    [SerializeField] private Button exitPartButton;

    [Header("Infection Setup")]
    [SerializeField] private InfectionMode infectionMode = InfectionMode.Manual;
    [SerializeField] private bool headInfected;
    [SerializeField] private bool torsoInfected;
    [SerializeField] private bool armInfected = true;
    [SerializeField] private bool legInfected;
    [SerializeField, Min(1)] private int minInfected = 1;
    [SerializeField, Min(1)] private int maxInfected = 4;

    [Header("Parts")]
    [SerializeField] private List<BodyPartButton> partButtons = new List<BodyPartButton>();

    [Header("Mini Games")]
    [SerializeField] private BodyArea tongsArea = BodyArea.Arm;
    [SerializeField] private TongsMiniGame tongsMiniGame;
    [SerializeField] private bool autoTreatInfectedAreasWithoutMiniGame = true;

    private readonly Dictionary<BodyArea, PartState> areaStates = new Dictionary<BodyArea, PartState>();
    private CustomerAgent activeCustomer;
    private BodyArea activeArea;
    private bool isInsidePart;
    private bool isMiniGameRunning;
    private bool hasBegun;

    public bool CanReturnToCounter => hasBegun && !isInsidePart;
    public bool IsInsidePart => isInsidePart;

    public event Action NavigationStateChanged;
    public event Action TreatmentCompleted;

    private void Awake()
    {
        CachePartButtons();

        if (exitPartButton != null)
        {
            exitPartButton.onClick.RemoveListener(ExitCurrentPart);
            exitPartButton.onClick.AddListener(ExitCurrentPart);
        }

        SetPartMessageVisible(false);
    }

    private void OnEnable()
    {
        SubscribeMiniGames();
    }

    private void OnDisable()
    {
        UnsubscribeMiniGames();
    }

    private void OnValidate()
    {
        if (maxInfected < minInfected)
        {
            maxInfected = minInfected;
        }
    }

    public void Begin(CustomerAgent customer)
    {
        activeCustomer = customer;
        activeArea = BodyArea.Arm;
        isInsidePart = false;
        isMiniGameRunning = false;
        hasBegun = true;

        CachePartButtons();
        ResolveInfection();
        RefreshAllPartVisuals();
        ShowAnatomyLevel("Select the treatment area from the patient's symptoms.");
        SetRootVisible(true);
    }

    public void ResumeAtAnatomyLevel()
    {
        if (!hasBegun)
        {
            Begin(activeCustomer);
            return;
        }

        isInsidePart = false;
        isMiniGameRunning = false;
        ShowAnatomyLevel("Select the treatment area from the patient's symptoms.");
        SetRootVisible(true);
    }

    public void PauseForCounter()
    {
        if (isMiniGameRunning)
        {
            tongsMiniGame?.Pause();
        }

        SetRootVisible(false);
    }

    public void Stop()
    {
        hasBegun = false;
        isInsidePart = false;
        isMiniGameRunning = false;
        tongsMiniGame?.Stop();
        SetRootVisible(false);
        SetPartMessageVisible(false);
        NavigationStateChanged?.Invoke();
    }

    public bool CanSelectArea(BodyArea area)
    {
        return hasBegun && !isInsidePart && GetState(area) != PartState.Treated;
    }

    public void EnterArea(BodyArea area)
    {
        if (!CanSelectArea(area))
        {
            return;
        }

        activeArea = area;
        isInsidePart = true;
        NavigationStateChanged?.Invoke();

        PartState state = GetState(area);
        if (state == PartState.Infected)
        {
            EnterInfectedArea(area);
            return;
        }

        MarkAreaState(area, PartState.Healthy);
        ShowHealthyArea(area);
    }

    public void ExitCurrentPart()
    {
        if (!isInsidePart || isMiniGameRunning)
        {
            return;
        }

        ShowAnatomyLevel("Select another treatment area.");
    }

    public void MarkAreaTreated(BodyArea area)
    {
        MarkAreaState(area, PartState.Treated);
        RefreshAllPartVisuals();
        ShowAnatomyLevel($"{FormatArea(area)} treatment complete.");

        if (AreAllInfectedAreasTreated())
        {
            TreatmentCompleted?.Invoke();
        }
    }

    private void EnterInfectedArea(BodyArea area)
    {
        SetBodyVisible(false);
        SetPartMessageVisible(false);

        if (area == tongsArea && tongsMiniGame != null)
        {
            isMiniGameRunning = true;
            tongsMiniGame.Begin(activeCustomer);
            SetStatusText($"Treating {FormatArea(area)}.");
            return;
        }

        Debug.LogWarning($"No mini game is assigned for infected area {area}.");
        if (autoTreatInfectedAreasWithoutMiniGame)
        {
            MarkAreaTreated(area);
            return;
        }

        ShowPartMessage($"No mini game is assigned for {FormatArea(area)} yet.");
    }

    private void ShowHealthyArea(BodyArea area)
    {
        SetBodyVisible(false);
        ShowPartMessage($"{FormatArea(area)} looks clean. Nothing to treat here.");
    }

    private void ShowAnatomyLevel(string message)
    {
        isInsidePart = false;
        isMiniGameRunning = false;
        tongsMiniGame?.Stop();
        SetBodyVisible(true);
        SetPartMessageVisible(false);
        SetStatusText(message);
        NavigationStateChanged?.Invoke();
    }

    private void ResolveInfection()
    {
        areaStates.Clear();
        foreach (BodyArea area in Enum.GetValues(typeof(BodyArea)))
        {
            areaStates[area] = PartState.Untouched;
        }

        if (infectionMode == InfectionMode.Manual)
        {
            ApplyManualInfection();
            return;
        }

        ApplyRandomInfection();
    }

    private void ApplyManualInfection()
    {
        if (headInfected)
        {
            areaStates[BodyArea.Head] = PartState.Infected;
        }

        if (torsoInfected)
        {
            areaStates[BodyArea.Torso] = PartState.Infected;
        }

        if (armInfected)
        {
            areaStates[BodyArea.Arm] = PartState.Infected;
        }

        if (legInfected)
        {
            areaStates[BodyArea.Leg] = PartState.Infected;
        }
    }

    private void ApplyRandomInfection()
    {
        List<BodyArea> areas = new List<BodyArea>
        {
            BodyArea.Head,
            BodyArea.Torso,
            BodyArea.Arm,
            BodyArea.Leg
        };

        int low = Mathf.Clamp(minInfected, 1, areas.Count);
        int high = Mathf.Clamp(maxInfected, low, areas.Count);
        int count = UnityEngine.Random.Range(low, high + 1);

        for (int i = 0; i < count && areas.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, areas.Count);
            areaStates[areas[index]] = PartState.Infected;
            areas.RemoveAt(index);
        }
    }

    private void HandleTongsCompleted()
    {
        if (!isMiniGameRunning || activeArea != tongsArea)
        {
            return;
        }

        isMiniGameRunning = false;
        MarkAreaTreated(activeArea);
    }

    private bool AreAllInfectedAreasTreated()
    {
        bool hasAnyTreated = false;
        foreach (KeyValuePair<BodyArea, PartState> pair in areaStates)
        {
            if (pair.Value == PartState.Infected)
            {
                return false;
            }

            if (pair.Value == PartState.Treated)
            {
                hasAnyTreated = true;
            }
        }

        return hasAnyTreated;
    }

    private PartState GetState(BodyArea area)
    {
        return areaStates.TryGetValue(area, out PartState state) ? state : PartState.Untouched;
    }

    private void MarkAreaState(BodyArea area, PartState state)
    {
        areaStates[area] = state;
        RefreshAllPartVisuals();
    }

    private void RefreshAllPartVisuals()
    {
        for (int i = 0; i < partButtons.Count; i++)
        {
            if (partButtons[i] != null)
            {
                partButtons[i].Bind(this);
                partButtons[i].SetState(GetState(partButtons[i].Area));
            }
        }
    }

    private void CachePartButtons()
    {
        if (partButtons.Count == 0)
        {
            GetComponentsInChildren(includeInactive: true, partButtons);
        }

        for (int i = 0; i < partButtons.Count; i++)
        {
            if (partButtons[i] != null)
            {
                partButtons[i].Bind(this);
            }
        }
    }

    private void SubscribeMiniGames()
    {
        if (tongsMiniGame != null)
        {
            tongsMiniGame.MiniGameCompleted -= HandleTongsCompleted;
            tongsMiniGame.MiniGameCompleted += HandleTongsCompleted;
        }
    }

    private void UnsubscribeMiniGames()
    {
        if (tongsMiniGame != null)
        {
            tongsMiniGame.MiniGameCompleted -= HandleTongsCompleted;
        }
    }

    private void SetRootVisible(bool visible)
    {
        GameObject target = root != null ? root : gameObject;
        target.SetActive(visible);
    }

    private void SetBodyVisible(bool visible)
    {
        if (bodyRoot != null)
        {
            bodyRoot.SetActive(visible);
        }
    }

    private void SetPartMessageVisible(bool visible)
    {
        if (partMessageRoot != null)
        {
            partMessageRoot.SetActive(visible);
        }

        if (exitPartButton != null)
        {
            exitPartButton.gameObject.SetActive(visible && !isMiniGameRunning);
        }
    }

    private void ShowPartMessage(string message)
    {
        SetStatusText("Area checked.");
        if (partMessageText != null)
        {
            partMessageText.text = message;
        }

        SetPartMessageVisible(true);
        NavigationStateChanged?.Invoke();
    }

    private void SetStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private static string FormatArea(BodyArea area)
    {
        return area.ToString();
    }
}

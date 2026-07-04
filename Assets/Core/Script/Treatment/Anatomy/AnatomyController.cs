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

    [Header("Case Source")]
    [SerializeField] private TreatmentCaseSource caseSource = TreatmentCaseSource.CustomerCase;
    [SerializeField] private bool fallbackToManualIfCustomerCaseMissing = true;

    [Header("Fallback Infection Setup")]
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
    [SerializeField] private KnifeMiniGame knifeMiniGame;
    [SerializeField] private NeedleMiniGame needleMiniGame;
    [SerializeField] private TreatmentBodyPrefabSpawner bodyPrefabSpawner;
    [SerializeField] private GameFlow miniGameFlow;
    [SerializeField] private Camera miniGameInputCamera;
    [SerializeField] private MiniGameOverlay miniGameOverlay;
    [SerializeField] private List<BodyArea> knifeAreas = new List<BodyArea>
    {
        BodyArea.Head,
        BodyArea.Torso,
        BodyArea.Leg
    };
    [SerializeField] private List<BodyArea> needleAreas = new List<BodyArea>();
    [SerializeField] private bool autoTreatInfectedAreasWithoutMiniGame = true;
    [Header("Debug")]
    [SerializeField] private bool debugTreatmentFlow = true;

    private readonly Dictionary<BodyArea, PartState> areaStates = new Dictionary<BodyArea, PartState>();
    private CustomerAgent activeCustomer;
    private TreatmentCaseRuntime activeCaseRuntime;
    private BodyArea activeArea;
    private TreatmentMiniGameType activeMiniGameType = TreatmentMiniGameType.None;
    private bool isInsidePart;
    private bool isMiniGameRunning;
    private bool hasBegun;

    public bool CanReturnToCounter => hasBegun && !isInsidePart;
    public bool IsInsidePart => isInsidePart;
    public bool CanCompleteTreatment => hasBegun && !isInsidePart && AreAllInfectedAreasTreated();

    public event Action NavigationStateChanged;

    private void Awake()
    {
        CachePartButtons();
        ResolveBodyPrefabSpawner();

        if (exitPartButton != null)
        {
            exitPartButton.onClick.RemoveListener(ExitCurrentPart);
            exitPartButton.onClick.AddListener(ExitCurrentPart);
        }

        SetPartMessageVisible(false);

        // Subscribe here (not in OnEnable) so the mini-game completion callback survives
        // this GameObject being deactivated while the Anatomy screen is folded away during
        // a running mini game. See EnterInfectedArea / ShowAnatomyLevel.
        SubscribeMiniGames();
    }

    private void OnDestroy()
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
        LogTreatmentFlow($"Begin customer={DescribeObject(customer)} caseSource={caseSource} fallback={fallbackToManualIfCustomerCaseMissing} infectionMode={infectionMode}");
        activeCustomer = customer;
        activeArea = BodyArea.Arm;
        activeMiniGameType = TreatmentMiniGameType.None;
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
        LogTreatmentFlow($"ResumeAtAnatomyLevel hasBegun={hasBegun} activeCustomer={DescribeObject(activeCustomer)}");
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
        LogTreatmentFlow($"PauseForCounter isMiniGameRunning={isMiniGameRunning} activeMiniGameType={activeMiniGameType}");
        if (isMiniGameRunning)
        {
            if (tongsMiniGame != null)
            {
                tongsMiniGame.Pause();
            }

            if (knifeMiniGame != null)
            {
                knifeMiniGame.Pause();
            }

            if (needleMiniGame != null)
            {
                needleMiniGame.Pause();
            }
        }

        SetRootVisible(false);
    }

    public void Stop()
    {
        LogTreatmentFlow($"Stop activeArea={activeArea} activeMiniGameType={activeMiniGameType} activeBody={DescribeBody(bodyPrefabSpawner != null ? bodyPrefabSpawner.ActiveBody : null)}");
        hasBegun = false;
        isInsidePart = false;
        isMiniGameRunning = false;
        activeMiniGameType = TreatmentMiniGameType.None;
        activeCaseRuntime = null;
        StopActiveMiniGames();
        ClearRuntimeMiniGamesIfOwnedByActiveBody();
        if (bodyPrefabSpawner != null)
        {
            bodyPrefabSpawner.ClearActiveBody();
        }
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
        LogTreatmentFlow($"EnterArea requested area={area} canSelect={CanSelectArea(area)} currentState={GetState(area)} activeCase={DescribeActiveCase()}");
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
        LogTreatmentFlow($"ExitCurrentPart isInsidePart={isInsidePart} isMiniGameRunning={isMiniGameRunning}");
        if (!isInsidePart || isMiniGameRunning)
        {
            return;
        }

        ShowAnatomyLevel("Select another treatment area.");
    }

    public void MarkAreaTreated(BodyArea area)
    {
        LogTreatmentFlow($"MarkAreaTreated area={area}");
        activeCaseRuntime?.MarkTreated(area);
        MarkAreaState(area, PartState.Treated);
        RefreshAllPartVisuals();

        string message = AreAllInfectedAreasTreated()
            ? "All required treatments are complete. Press Complete when ready."
            : $"{FormatArea(area)} treatment complete. Select another treatment area.";

        ShowAnatomyLevel(message);
    }

    private void EnterInfectedArea(BodyArea area)
    {
        TreatmentMiniGameType miniGameType = GetMiniGameTypeForArea(area);
        LogTreatmentFlow($"EnterInfectedArea area={area} resolvedMiniGameType={miniGameType} tongsRef={DescribeObject(tongsMiniGame)} knifeRef={DescribeObject(knifeMiniGame)} needleRef={DescribeObject(needleMiniGame)}");
        switch (miniGameType)
        {
            case TreatmentMiniGameType.Tongs:
                TreatmentBodyPrefab tongsBody = SpawnBodyPrefabForArea(TreatmentMiniGameType.Tongs, area, tongsMiniGame);
                TongsMiniGame resolvedTongsMiniGame = ResolveTongsMiniGame(tongsBody);
                LogTreatmentFlow($"Tongs route area={area} body={DescribeBody(tongsBody)} resolvedMiniGame={DescribeObject(resolvedTongsMiniGame)}");
                if (resolvedTongsMiniGame == null)
                {
                    break;
                }

                BindTongsMiniGame(resolvedTongsMiniGame);
                ConfigureTongsMiniGame(resolvedTongsMiniGame);
                StartMiniGame(TreatmentMiniGameType.Tongs);
                resolvedTongsMiniGame.ApplyBodyPrefab(tongsBody);
                resolvedTongsMiniGame.Begin(activeCustomer);
                FoldForMiniGame();
                return;

            case TreatmentMiniGameType.Knife:
                TreatmentBodyPrefab knifeBody = SpawnBodyPrefabForArea(TreatmentMiniGameType.Knife, area, knifeMiniGame);
                KnifeMiniGame resolvedKnifeMiniGame = ResolveKnifeMiniGame(knifeBody);
                LogTreatmentFlow($"Knife route area={area} body={DescribeBody(knifeBody)} resolvedMiniGame={DescribeObject(resolvedKnifeMiniGame)}");
                if (resolvedKnifeMiniGame == null)
                {
                    break;
                }

                BindKnifeMiniGame(resolvedKnifeMiniGame);
                ConfigureKnifeMiniGame(resolvedKnifeMiniGame);
                StartMiniGame(TreatmentMiniGameType.Knife);
                resolvedKnifeMiniGame.ApplyBodyPrefab(knifeBody);
                resolvedKnifeMiniGame.Begin(activeCustomer);
                FoldForMiniGame();
                return;

            case TreatmentMiniGameType.Needle:
                TreatmentBodyPrefab needleBody = SpawnBodyPrefabForArea(TreatmentMiniGameType.Needle, area, needleMiniGame);
                NeedleMiniGame resolvedNeedleMiniGame = ResolveNeedleMiniGame(needleBody);
                LogTreatmentFlow($"Needle route area={area} body={DescribeBody(needleBody)} resolvedMiniGame={DescribeObject(resolvedNeedleMiniGame)}");
                if (resolvedNeedleMiniGame == null)
                {
                    break;
                }

                BindNeedleMiniGame(resolvedNeedleMiniGame);
                ConfigureNeedleMiniGame(resolvedNeedleMiniGame);
                StartMiniGame(TreatmentMiniGameType.Needle);
                resolvedNeedleMiniGame.ApplyBodyPrefab(needleBody);
                resolvedNeedleMiniGame.Begin(activeCustomer);
                FoldForMiniGame();
                return;
        }

        SetBodyVisible(false);
        SetPartMessageVisible(false);

        Debug.LogWarning($"No mini game is assigned for infected area {area}. Requested mini game: {miniGameType}.");
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

    // Fold the entire Anatomy screen away while a world-space mini game plays.
    // The controller keeps its completion subscription (see Awake/OnDestroy), so the
    // mini game still calls back here even though this GameObject is inactive.
    private void FoldForMiniGame()
    {
        LogTreatmentFlow($"FoldForMiniGame activeArea={activeArea} activeMiniGameType={activeMiniGameType}");
        SetPartMessageVisible(false);
        SetRootVisible(false);
    }

    private void ShowAnatomyLevel(string message)
    {
        LogTreatmentFlow($"ShowAnatomyLevel message='{message}' activeBodyBeforeClear={DescribeBody(bodyPrefabSpawner != null ? bodyPrefabSpawner.ActiveBody : null)}");
        isInsidePart = false;
        isMiniGameRunning = false;
        activeMiniGameType = TreatmentMiniGameType.None;
        StopActiveMiniGames();
        ClearRuntimeMiniGamesIfOwnedByActiveBody();
        if (bodyPrefabSpawner != null)
        {
            bodyPrefabSpawner.ClearActiveBody();
        }
        SetRootVisible(true);       // un-fold the screen when returning to the anatomy level
        SetBodyVisible(true);
        SetPartMessageVisible(false);
        SetStatusText(message);
        NavigationStateChanged?.Invoke();
    }

    private void ResolveInfection()
    {
        LogTreatmentFlow("ResolveInfection begin");
        areaStates.Clear();
        activeCaseRuntime = null;
        foreach (BodyArea area in Enum.GetValues(typeof(BodyArea)))
        {
            areaStates[area] = PartState.Untouched;
        }

        if (caseSource == TreatmentCaseSource.CustomerCase && TryApplyCustomerCase())
        {
            LogTreatmentFlow($"ResolveInfection used customer case {DescribeActiveCase()}");
            return;
        }

        if (caseSource == TreatmentCaseSource.CustomerCase && !fallbackToManualIfCustomerCaseMissing)
        {
            Debug.LogWarning("No TreatmentCaseData was found on the active customer. AnatomyController will start with no infected areas.");
            LogTreatmentFlow("ResolveInfection stopped because customer case is missing and fallback is disabled.");
            return;
        }

        if (caseSource == TreatmentCaseSource.Random || (caseSource == TreatmentCaseSource.CustomerCase && infectionMode == InfectionMode.Random))
        {
            ApplyRandomInfection();
            LogTreatmentFlow("ResolveInfection used random infection fallback.");
            return;
        }

        if (caseSource == TreatmentCaseSource.Manual || caseSource == TreatmentCaseSource.CustomerCase || infectionMode == InfectionMode.Manual)
        {
            ApplyManualInfection();
            LogTreatmentFlow("ResolveInfection used manual infection fallback.");
            return;
        }
    }

    private bool TryApplyCustomerCase()
    {
        CustomerCaseProvider caseProvider = activeCustomer != null ? activeCustomer.GetComponent<CustomerCaseProvider>() : null;
        TreatmentCaseData caseData = caseProvider != null ? caseProvider.CaseData : null;
        LogTreatmentFlow($"TryApplyCustomerCase activeCustomer={DescribeObject(activeCustomer)} caseProvider={DescribeObject(caseProvider)} caseData={DescribeObject(caseData)} requiredCount={(caseData != null ? caseData.RequiredTreatmentCount : 0)}");
        if (caseData == null || caseData.RequiredTreatmentCount == 0)
        {
            return false;
        }

        activeCaseRuntime = new TreatmentCaseRuntime(caseData);
        foreach (BodyArea area in activeCaseRuntime.GetRequiredAreas())
        {
            activeCaseRuntime.TryGetMiniGameType(area, out TreatmentMiniGameType miniGameType);
            LogTreatmentFlow($"Customer case requirement area={area} miniGameType={miniGameType}");
            areaStates[area] = PartState.Infected;
        }

        return activeCaseRuntime.HasRequirements;
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
        if (!isMiniGameRunning || activeMiniGameType != TreatmentMiniGameType.Tongs)
        {
            return;
        }

        CompleteActiveMiniGameArea();
    }

    private void HandleKnifeCompleted()
    {
        if (!isMiniGameRunning || activeMiniGameType != TreatmentMiniGameType.Knife)
        {
            return;
        }

        CompleteActiveMiniGameArea();
    }

    private void HandleNeedleCompleted()
    {
        if (!isMiniGameRunning || activeMiniGameType != TreatmentMiniGameType.Needle)
        {
            return;
        }

        CompleteActiveMiniGameArea();
    }

    private void StartMiniGame(TreatmentMiniGameType miniGameType)
    {
        LogTreatmentFlow($"StartMiniGame miniGameType={miniGameType} activeArea={activeArea}");
        activeMiniGameType = miniGameType;
        isMiniGameRunning = true;
    }

    private void CompleteActiveMiniGameArea()
    {
        LogTreatmentFlow($"CompleteActiveMiniGameArea activeArea={activeArea} activeMiniGameType={activeMiniGameType}");
        isMiniGameRunning = false;
        activeMiniGameType = TreatmentMiniGameType.None;
        MarkAreaTreated(activeArea);
    }

    private TreatmentMiniGameType GetMiniGameTypeForArea(BodyArea area)
    {
        if (activeCaseRuntime != null && activeCaseRuntime.TryGetMiniGameType(area, out TreatmentMiniGameType miniGameType))
        {
            LogTreatmentFlow($"GetMiniGameTypeForArea area={area} source=CustomerCase miniGameType={miniGameType}");
            return miniGameType;
        }

        if (area == tongsArea)
        {
            LogTreatmentFlow($"GetMiniGameTypeForArea area={area} source=FallbackTongs tongsArea={tongsArea}");
            return TreatmentMiniGameType.Tongs;
        }

        if (knifeAreas != null && knifeAreas.Contains(area))
        {
            LogTreatmentFlow($"GetMiniGameTypeForArea area={area} source=FallbackKnife knifeAreas={FormatAreaList(knifeAreas)}");
            return TreatmentMiniGameType.Knife;
        }

        if (needleAreas != null && needleAreas.Contains(area))
        {
            LogTreatmentFlow($"GetMiniGameTypeForArea area={area} source=FallbackNeedle needleAreas={FormatAreaList(needleAreas)}");
            return TreatmentMiniGameType.Needle;
        }

        LogTreatmentFlow($"GetMiniGameTypeForArea area={area} source=None");
        return TreatmentMiniGameType.None;
    }

    private TreatmentBodyPrefab SpawnBodyPrefabForArea(TreatmentMiniGameType miniGameType, BodyArea area, Component miniGame)
    {
        LogTreatmentFlow($"SpawnBodyPrefabForArea request miniGameType={miniGameType} area={area} miniGameRef={DescribeObject(miniGame)}");
        TreatmentBodyPrefab existingBody = miniGame != null ? miniGame.GetComponentInParent<TreatmentBodyPrefab>(true) : null;
        if (existingBody != null)
        {
            LogTreatmentFlow($"SpawnBodyPrefabForArea existing parent body={DescribeBody(existingBody)} matches={existingBody.Matches(miniGameType, area)}");
            if (existingBody.Matches(miniGameType, area))
            {
                return existingBody;
            }

            Debug.LogWarning($"Mini game {miniGame.name} is already inside TreatmentBodyPrefab {existingBody.name}, but it does not match {miniGameType}/{area}. Spawning the matching catalog body instead.");
        }

        ResolveBodyPrefabSpawner();
        if (bodyPrefabSpawner != null && bodyPrefabSpawner.TrySpawn(miniGameType, area, out TreatmentBodyPrefab body))
        {
            LogTreatmentFlow($"SpawnBodyPrefabForArea spawner returned body={DescribeBody(body)}");
            return body;
        }

        LogTreatmentFlow($"SpawnBodyPrefabForArea failed for miniGameType={miniGameType} area={area} spawner={DescribeObject(bodyPrefabSpawner)}");
        return null;
    }

    private TongsMiniGame ResolveTongsMiniGame(TreatmentBodyPrefab body)
    {
        LogTreatmentFlow($"ResolveTongsMiniGame body={DescribeBody(body)}");
        if (body != null && body.TryGetComponent(out TongsMiniGame bodyMiniGame))
        {
            LogTreatmentFlow($"ResolveTongsMiniGame found on body root {DescribeObject(bodyMiniGame)}");
            return bodyMiniGame;
        }

        if (body != null)
        {
            bodyMiniGame = body.GetComponentInChildren<TongsMiniGame>(true);
            if (bodyMiniGame != null)
            {
                LogTreatmentFlow($"ResolveTongsMiniGame found in body children {DescribeObject(bodyMiniGame)}");
                return bodyMiniGame;
            }
        }

        LogTreatmentFlow($"ResolveTongsMiniGame using inspector fallback {DescribeObject(tongsMiniGame)}");
        return tongsMiniGame != null ? tongsMiniGame : null;
    }

    private KnifeMiniGame ResolveKnifeMiniGame(TreatmentBodyPrefab body)
    {
        LogTreatmentFlow($"ResolveKnifeMiniGame body={DescribeBody(body)}");
        if (body != null && body.TryGetComponent(out KnifeMiniGame bodyMiniGame))
        {
            LogTreatmentFlow($"ResolveKnifeMiniGame found on body root {DescribeObject(bodyMiniGame)}");
            return bodyMiniGame;
        }

        if (body != null)
        {
            bodyMiniGame = body.GetComponentInChildren<KnifeMiniGame>(true);
            if (bodyMiniGame != null)
            {
                LogTreatmentFlow($"ResolveKnifeMiniGame found in body children {DescribeObject(bodyMiniGame)}");
                return bodyMiniGame;
            }
        }

        LogTreatmentFlow($"ResolveKnifeMiniGame using inspector fallback {DescribeObject(knifeMiniGame)}");
        return knifeMiniGame != null ? knifeMiniGame : null;
    }

    private NeedleMiniGame ResolveNeedleMiniGame(TreatmentBodyPrefab body)
    {
        LogTreatmentFlow($"ResolveNeedleMiniGame body={DescribeBody(body)}");
        if (body != null && body.TryGetComponent(out NeedleMiniGame bodyMiniGame))
        {
            LogTreatmentFlow($"ResolveNeedleMiniGame found on body root {DescribeObject(bodyMiniGame)}");
            return bodyMiniGame;
        }

        if (body != null)
        {
            bodyMiniGame = body.GetComponentInChildren<NeedleMiniGame>(true);
            if (bodyMiniGame != null)
            {
                LogTreatmentFlow($"ResolveNeedleMiniGame found in body children {DescribeObject(bodyMiniGame)}");
                return bodyMiniGame;
            }
        }

        LogTreatmentFlow($"ResolveNeedleMiniGame using inspector fallback {DescribeObject(needleMiniGame)}");
        return needleMiniGame != null ? needleMiniGame : null;
    }

    private void BindTongsMiniGame(TongsMiniGame resolvedMiniGame)
    {
        LogTreatmentFlow($"BindTongsMiniGame current={DescribeObject(tongsMiniGame)} resolved={DescribeObject(resolvedMiniGame)}");
        if (resolvedMiniGame == null || tongsMiniGame == resolvedMiniGame)
        {
            return;
        }

        if (tongsMiniGame != null)
        {
            tongsMiniGame.MiniGameCompleted -= HandleTongsCompleted;
        }

        tongsMiniGame = resolvedMiniGame;
        tongsMiniGame.MiniGameCompleted -= HandleTongsCompleted;
        tongsMiniGame.MiniGameCompleted += HandleTongsCompleted;
    }

    private void BindKnifeMiniGame(KnifeMiniGame resolvedMiniGame)
    {
        LogTreatmentFlow($"BindKnifeMiniGame current={DescribeObject(knifeMiniGame)} resolved={DescribeObject(resolvedMiniGame)}");
        if (resolvedMiniGame == null || knifeMiniGame == resolvedMiniGame)
        {
            return;
        }

        if (knifeMiniGame != null)
        {
            knifeMiniGame.MiniGameCompleted -= HandleKnifeCompleted;
        }

        knifeMiniGame = resolvedMiniGame;
        knifeMiniGame.MiniGameCompleted -= HandleKnifeCompleted;
        knifeMiniGame.MiniGameCompleted += HandleKnifeCompleted;
    }

    private void BindNeedleMiniGame(NeedleMiniGame resolvedMiniGame)
    {
        LogTreatmentFlow($"BindNeedleMiniGame current={DescribeObject(needleMiniGame)} resolved={DescribeObject(resolvedMiniGame)}");
        if (resolvedMiniGame == null || needleMiniGame == resolvedMiniGame)
        {
            return;
        }

        if (needleMiniGame != null)
        {
            needleMiniGame.MiniGameCompleted -= HandleNeedleCompleted;
        }

        needleMiniGame = resolvedMiniGame;
        needleMiniGame.MiniGameCompleted -= HandleNeedleCompleted;
        needleMiniGame.MiniGameCompleted += HandleNeedleCompleted;
    }

    private void ConfigureTongsMiniGame(TongsMiniGame resolvedMiniGame)
    {
        LogTreatmentFlow($"ConfigureTongsMiniGame target={DescribeObject(resolvedMiniGame)} flow={DescribeObject(miniGameFlow)} camera={DescribeObject(miniGameInputCamera)} overlay={DescribeObject(miniGameOverlay)}");
        if (resolvedMiniGame != null)
        {
            resolvedMiniGame.ConfigureRuntimeContext(miniGameFlow, miniGameInputCamera, miniGameOverlay);
        }
    }

    private void ConfigureKnifeMiniGame(KnifeMiniGame resolvedMiniGame)
    {
        LogTreatmentFlow($"ConfigureKnifeMiniGame target={DescribeObject(resolvedMiniGame)} flow={DescribeObject(miniGameFlow)} camera={DescribeObject(miniGameInputCamera)} overlay={DescribeObject(miniGameOverlay)}");
        if (resolvedMiniGame != null)
        {
            resolvedMiniGame.ConfigureRuntimeContext(miniGameFlow, miniGameInputCamera, miniGameOverlay);
        }
    }

    private void ConfigureNeedleMiniGame(NeedleMiniGame resolvedMiniGame)
    {
        LogTreatmentFlow($"ConfigureNeedleMiniGame target={DescribeObject(resolvedMiniGame)} flow={DescribeObject(miniGameFlow)} camera={DescribeObject(miniGameInputCamera)} overlay={DescribeObject(miniGameOverlay)}");
        if (resolvedMiniGame != null)
        {
            resolvedMiniGame.ConfigureRuntimeContext(miniGameFlow, miniGameInputCamera, miniGameOverlay);
        }
    }

    private void StopActiveMiniGames()
    {
        LogTreatmentFlow($"StopActiveMiniGames tongs={DescribeObject(tongsMiniGame)} knife={DescribeObject(knifeMiniGame)} needle={DescribeObject(needleMiniGame)}");
        if (tongsMiniGame != null)
        {
            tongsMiniGame.Stop();
        }

        if (knifeMiniGame != null)
        {
            knifeMiniGame.Stop();
        }

        if (needleMiniGame != null)
        {
            needleMiniGame.Stop();
        }
    }

    private void ClearRuntimeMiniGamesIfOwnedByActiveBody()
    {
        LogTreatmentFlow($"ClearRuntimeMiniGamesIfOwnedByActiveBody activeBody={DescribeBody(bodyPrefabSpawner != null ? bodyPrefabSpawner.ActiveBody : null)}");
        if (bodyPrefabSpawner == null || bodyPrefabSpawner.ActiveBody == null)
        {
            return;
        }

        TreatmentBodyPrefab activeBody = bodyPrefabSpawner.ActiveBody;
        if (tongsMiniGame != null && tongsMiniGame.GetComponentInParent<TreatmentBodyPrefab>(true) == activeBody)
        {
            LogTreatmentFlow($"Clearing runtime tongs mini game {DescribeObject(tongsMiniGame)} owned by {DescribeBody(activeBody)}");
            tongsMiniGame.MiniGameCompleted -= HandleTongsCompleted;
            tongsMiniGame = null;
        }

        if (knifeMiniGame != null && knifeMiniGame.GetComponentInParent<TreatmentBodyPrefab>(true) == activeBody)
        {
            LogTreatmentFlow($"Clearing runtime knife mini game {DescribeObject(knifeMiniGame)} owned by {DescribeBody(activeBody)}");
            knifeMiniGame.MiniGameCompleted -= HandleKnifeCompleted;
            knifeMiniGame = null;
        }

        if (needleMiniGame != null && needleMiniGame.GetComponentInParent<TreatmentBodyPrefab>(true) == activeBody)
        {
            LogTreatmentFlow($"Clearing runtime needle mini game {DescribeObject(needleMiniGame)} owned by {DescribeBody(activeBody)}");
            needleMiniGame.MiniGameCompleted -= HandleNeedleCompleted;
            needleMiniGame = null;
        }
    }

    private void LogTreatmentFlow(string message)
    {
        if (debugTreatmentFlow)
        {
            Debug.Log($"[TreatmentFlow][Anatomy] {message}", this);
        }
    }

    private static string DescribeObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return "null";
        }

        return $"{target.name} ({target.GetType().Name})";
    }

    private static string DescribeBody(TreatmentBodyPrefab body)
    {
        if (body == null)
        {
            return "null";
        }

        return $"{body.name} ({body.MiniGameType}/{body.Area}, active={body.gameObject.activeSelf})";
    }

    private string DescribeActiveCase()
    {
        if (activeCaseRuntime == null)
        {
            return "none";
        }

        List<string> requirements = new List<string>();
        foreach (BodyArea area in activeCaseRuntime.GetRequiredAreas())
        {
            activeCaseRuntime.TryGetMiniGameType(area, out TreatmentMiniGameType miniGameType);
            requirements.Add($"{area}:{miniGameType}");
        }

        return string.Join(", ", requirements);
    }

    private static string FormatAreaList(List<BodyArea> areas)
    {
        return areas == null || areas.Count == 0 ? "[]" : $"[{string.Join(", ", areas)}]";
    }

    private void ResolveBodyPrefabSpawner()
    {
        if (bodyPrefabSpawner == null)
        {
            bodyPrefabSpawner = GetComponentInParent<TreatmentBodyPrefabSpawner>(true);
        }
    }

    private bool AreAllInfectedAreasTreated()
    {
        if (activeCaseRuntime != null)
        {
            return activeCaseRuntime.AreAllRequiredAreasTreated();
        }

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

        if (knifeMiniGame != null)
        {
            knifeMiniGame.MiniGameCompleted -= HandleKnifeCompleted;
            knifeMiniGame.MiniGameCompleted += HandleKnifeCompleted;
        }

        if (needleMiniGame != null)
        {
            needleMiniGame.MiniGameCompleted -= HandleNeedleCompleted;
            needleMiniGame.MiniGameCompleted += HandleNeedleCompleted;
        }
    }

    private void UnsubscribeMiniGames()
    {
        if (tongsMiniGame != null)
        {
            tongsMiniGame.MiniGameCompleted -= HandleTongsCompleted;
        }

        if (knifeMiniGame != null)
        {
            knifeMiniGame.MiniGameCompleted -= HandleKnifeCompleted;
        }

        if (needleMiniGame != null)
        {
            needleMiniGame.MiniGameCompleted -= HandleNeedleCompleted;
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

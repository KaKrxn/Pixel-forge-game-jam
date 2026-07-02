using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Shared HUD for every treatment mini game. One canvas holds a single progress slider, pain
/// slider, complete button, and a row of tool buttons (the tool tray). A mini game calls
/// <see cref="Activate"/> when it starts (passing its complete handler + a tool-selected handler),
/// drives the meters through <see cref="SetMeters"/>, and calls <see cref="Deactivate"/> when it stops.
///
/// Tools are toggles: turning one on selects that tool id, turning it off clears the held tool.
/// Adding a new mini game only needs its own tool logic plus one entry in <c>toolButtons</c> — the
/// sliders, complete button, and canvas are reused as-is.
/// </summary>
public sealed class MiniGameOverlay : MonoBehaviour
{
    [Serializable]
    private sealed class ToolButton
    {
        [SerializeField] private string id;
        [SerializeField] private Toggle toggle;
        [SerializeField] private Button button;
        [SerializeField] private GameObject selectedIndicator;

        public string Id => id;
        public Toggle Toggle => toggle;
        public Button Button => button;
        public GameObject SelectedIndicator => selectedIndicator;
        public bool HasControl => toggle != null || button != null;

        private UnityAction<bool> toggleListener;
        private UnityAction buttonListener;

        public void ResolveMissingReferences(Transform searchRoot)
        {
            if (searchRoot == null || string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            Transform controlTransform = toggle != null ? toggle.transform : button != null ? button.transform : FindDeepChild(searchRoot, $"{id}ToolButton");
            if (controlTransform == null)
            {
                controlTransform = FindDeepChild(searchRoot, id);
            }

            if (toggle == null && controlTransform != null)
            {
                toggle = controlTransform.GetComponent<Toggle>();
            }

            if (button == null && controlTransform != null)
            {
                button = controlTransform.GetComponent<Button>();
            }

            if (selectedIndicator == null && controlTransform != null)
            {
                Transform indicator = FindDeepChild(controlTransform, "SelectedIndicator");
                selectedIndicator = indicator != null ? indicator.gameObject : null;
            }
        }

        public void Bind(MiniGameOverlay overlay)
        {
            if (overlay == null || string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (toggle != null)
            {
                UnbindToggle();
                string capturedId = id;
                toggleListener = isOn => overlay.HandleToolToggleChanged(capturedId, isOn);
                toggle.onValueChanged.AddListener(toggleListener);
                return;
            }

            if (button != null)
            {
                UnbindButton();
                string capturedId = id;
                buttonListener = () => overlay.ToggleToolSelection(capturedId);
                button.onClick.AddListener(buttonListener);
            }
        }

        public void Unbind()
        {
            UnbindToggle();
            UnbindButton();
        }

        public void SetIsOnWithoutNotify(bool isOn)
        {
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(isOn);
            }
        }

        public void SetAvailable(bool available)
        {
            if (toggle != null)
            {
                toggle.interactable = available;
                toggle.gameObject.SetActive(available);
            }

            if (button != null)
            {
                button.interactable = available;
                if (toggle == null || button.gameObject != toggle.gameObject)
                {
                    button.gameObject.SetActive(available);
                }
            }

            if (!available && selectedIndicator != null)
            {
                selectedIndicator.SetActive(false);
            }
        }

        private void UnbindToggle()
        {
            if (toggle != null && toggleListener != null)
            {
                toggle.onValueChanged.RemoveListener(toggleListener);
                toggleListener = null;
            }
        }

        private void UnbindButton()
        {
            if (button != null && buttonListener != null)
            {
                button.onClick.RemoveListener(buttonListener);
                buttonListener = null;
            }
        }
    }

    [Header("Content")]
    [Tooltip("Root object shown while a mini game is active. Must NOT be this GameObject.")]
    [SerializeField] private GameObject content;
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private GraphicRaycaster overlayRaycaster;

    [Header("Shared Meters")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Slider painSlider;
    [SerializeField] private Button completeButton;

    [Header("Tools (one toggle per tool)")]
    [SerializeField] private List<ToolButton> toolButtons = new List<ToolButton>();

    private Action activeCompleteHandler;
    private Action<string> activeToolSelectedHandler;
    private string selectedToolId;

    /// <summary>Currently selected tool id, or null when nothing is selected.</summary>
    public string SelectedToolId => selectedToolId;

    private void Awake()
    {
        ResolveCanvasReferences();
        ResolveMissingToolReferences();

        if (completeButton != null)
        {
            completeButton.onClick.RemoveListener(HandleCompleteClicked);
            completeButton.onClick.AddListener(HandleCompleteClicked);
        }

        for (int i = 0; i < toolButtons.Count; i++)
        {
            ToolButton tool = toolButtons[i];
            if (tool == null || !tool.HasControl)
            {
                continue;
            }

            tool.Bind(this);
        }

        selectedToolId = null;
        UpdateToolHighlights();
        SetOverlayVisible(false);
        SetCompleteVisible(false);
    }

    private void OnDestroy()
    {
        if (completeButton != null)
        {
            completeButton.onClick.RemoveListener(HandleCompleteClicked);
        }

        for (int i = 0; i < toolButtons.Count; i++)
        {
            toolButtons[i]?.Unbind();
        }
    }

    /// <summary>Called by a mini game when it becomes the active one. Starts with no tool selected.</summary>
    public void Activate(Action completeHandler, Action<string> toolSelectedHandler)
    {
        Activate(completeHandler, toolSelectedHandler, null);
    }

    /// <summary>Called by a mini game when it becomes active. Only listed tools are shown.</summary>
    public void Activate(Action completeHandler, Action<string> toolSelectedHandler, params string[] allowedToolIds)
    {
        activeCompleteHandler = completeHandler;
        activeToolSelectedHandler = toolSelectedHandler;
        SetAllToolsAvailable(true);
        SetOverlayVisible(true);
        SetMeters(0f, 0f);
        SetCompleteVisible(false);
        ClearToolSelection();
    }

    /// <summary>
    /// Called by a mini game when it stops/pauses. Only clears the overlay if the caller still owns
    /// it, so a late Deactivate cannot wipe a mini game that already took over.
    /// </summary>
    public void Deactivate(Action completeHandler)
    {
        if (completeHandler != null && activeCompleteHandler != completeHandler)
        {
            return;
        }

        activeCompleteHandler = null;
        activeToolSelectedHandler = null;
        selectedToolId = null;
        SetAllToolsAvailable(true);
        UpdateToolHighlights();
        SetCompleteVisible(false);
        SetMeters(0f, 0f);
        SetOverlayVisible(false);
    }

    /// <summary>Selects a tool by id and notifies the active mini game. Wired to each tool toggle.</summary>
    public void SelectTool(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            ClearToolSelection();
            return;
        }

        selectedToolId = id;
        UpdateToolHighlights();
        activeToolSelectedHandler?.Invoke(id);
    }

    public void SetMeters(float progress, float pain)
    {
        if (progressSlider != null)
        {
            progressSlider.value = progress;
        }

        if (painSlider != null)
        {
            painSlider.value = pain;
        }
    }

    public void SetCompleteVisible(bool visible)
    {
        if (completeButton != null)
        {
            completeButton.gameObject.SetActive(visible);
        }
    }

    private void ClearToolSelection()
    {
        selectedToolId = null;
        UpdateToolHighlights();
        activeToolSelectedHandler?.Invoke(null);
    }

    private void ToggleToolSelection(string id)
    {
        if (selectedToolId == id)
        {
            ClearToolSelection();
            return;
        }

        SelectTool(id);
    }

    private void HandleToolToggleChanged(string id, bool isOn)
    {
        if (isOn)
        {
            SelectTool(id);
            return;
        }

        if (selectedToolId == id)
        {
            ClearToolSelection();
        }
    }

    private void UpdateToolHighlights()
    {
        for (int i = 0; i < toolButtons.Count; i++)
        {
            ToolButton tool = toolButtons[i];
            if (tool == null)
            {
                continue;
            }

            bool selected = tool.Id == selectedToolId;
            tool.SetIsOnWithoutNotify(selected);

            if (tool.SelectedIndicator != null)
            {
                tool.SelectedIndicator.SetActive(selected);
            }
        }
    }

    private void HandleCompleteClicked()
    {
        activeCompleteHandler?.Invoke();
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayCanvas != null)
        {
            overlayCanvas.enabled = visible;
        }

        if (overlayRaycaster != null)
        {
            overlayRaycaster.enabled = visible;
        }

        SetContentVisible(visible);
    }

    private void SetContentVisible(bool visible)
    {
        GameObject target = content != null ? content : gameObject;
        if (target == gameObject)
        {
            // Never disable the object this component lives on, or Awake/OnDestroy wiring breaks.
            return;
        }

        target.SetActive(visible);
    }

    private void ResolveCanvasReferences()
    {
        if (overlayCanvas == null)
        {
            overlayCanvas = GetComponent<Canvas>();
        }

        if (overlayRaycaster == null)
        {
            overlayRaycaster = GetComponent<GraphicRaycaster>();
        }
    }

    private void SetAllToolsAvailable(bool available)
    {
        for (int i = 0; i < toolButtons.Count; i++)
        {
            ToolButton tool = toolButtons[i];
            if (tool == null)
            {
                continue;
            }

            tool.SetAvailable(available);
        }
    }

    private void ResolveMissingToolReferences()
    {
        Transform searchRoot = content != null ? content.transform : transform;
        for (int i = 0; i < toolButtons.Count; i++)
        {
            ToolButton tool = toolButtons[i];
            if (tool == null)
            {
                continue;
            }

            tool.ResolveMissingReferences(searchRoot);
        }
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        if (string.Equals(root.name, childName, StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeepChild(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}

using System;
using UnityEngine;

/// <summary>
/// Shared software cursor for every treatment mini game. When a tool is equipped (Knife / Tongs / Needle)
/// the cursor shows that tool's sprite and follows the focus point the active mini game pushes each frame —
/// the jittering tool tip while acting, or the raw pointer while hovering. Each tool visual is authored with
/// its pivot/origin exactly at the tool's tip (Option A), so placing this root on the focus point lines the
/// tip up with it precisely. Optionally hides the OS cursor so the tool tip is the single focus point.
///
/// One instance lives in the treatment scene; mini games reach it through <see cref="Instance"/> and never
/// need a direct reference. Safe to be absent (all calls are null-guarded on the caller side).
/// </summary>
public sealed class TreatmentToolCursor : MonoBehaviour
{
    [Serializable]
    private sealed class ToolVisual
    {
        [SerializeField] private string toolId = "Knife";
        [Tooltip("The tool sprite root that gets shown/hidden.")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Point on the visual that should sit on the focus point (e.g. the blade/needle tip). " +
                 "Assign the 'pivot' child. If empty, the visual root's own origin is used.")]
        [SerializeField] private Transform tip;

        public string ToolId => toolId;
        public Transform VisualRoot => visualRoot;
        public Transform Tip => tip != null ? tip : visualRoot;
        public bool Matches(string id) => !string.IsNullOrEmpty(toolId) && toolId == id;
        public bool IsValid => !string.IsNullOrEmpty(toolId) && visualRoot != null;
    }

    [SerializeField] private ToolVisual[] tools = new ToolVisual[0];
    [Tooltip("Hide the OS cursor while a tool is shown, so the tool tip is the only focus point.")]
    [SerializeField] private bool hideSystemCursor = true;

    private string currentToolId;
    private ToolVisual activeTool;
    private bool active;

    public static TreatmentToolCursor Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        HideAllVisuals();
        active = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        RestoreSystemCursor();
    }

    /// <summary>Show the cursor for the given tool id (from the overlay). An empty/unknown id hides it.</summary>
    public void Show(string toolId)
    {
        if (string.IsNullOrEmpty(toolId) || !HasTool(toolId))
        {
            Hide();
            return;
        }

        currentToolId = toolId;
        activeTool = FindTool(toolId);
        active = true;
        ApplyVisual();
        ApplySystemCursor();
    }

    /// <summary>Move the active tool so its tip sits exactly on this world point.</summary>
    public void SetFocusPoint(Vector3 worldPoint)
    {
        if (!active || activeTool == null || activeTool.VisualRoot == null)
        {
            return;
        }

        Transform tip = activeTool.Tip;
        Vector3 current = tip != null ? tip.position : activeTool.VisualRoot.position;
        Vector3 target = new Vector3(worldPoint.x, worldPoint.y, current.z);
        activeTool.VisualRoot.position += target - current;
    }

    public void Hide()
    {
        active = false;
        currentToolId = null;
        activeTool = null;
        HideAllVisuals();
        RestoreSystemCursor();
    }

    private ToolVisual FindTool(string toolId)
    {
        for (int i = 0; i < tools.Length; i++)
        {
            if (tools[i] != null && tools[i].IsValid && tools[i].Matches(toolId))
            {
                return tools[i];
            }
        }

        return null;
    }

    private bool HasTool(string toolId)
    {
        for (int i = 0; i < tools.Length; i++)
        {
            if (tools[i] != null && tools[i].IsValid && tools[i].Matches(toolId))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyVisual()
    {
        for (int i = 0; i < tools.Length; i++)
        {
            ToolVisual tool = tools[i];
            if (tool == null || tool.VisualRoot == null)
            {
                continue;
            }

            tool.VisualRoot.gameObject.SetActive(active && tool.Matches(currentToolId));
        }
    }

    private void HideAllVisuals()
    {
        for (int i = 0; i < tools.Length; i++)
        {
            if (tools[i] != null && tools[i].VisualRoot != null)
            {
                tools[i].VisualRoot.gameObject.SetActive(false);
            }
        }
    }

    private void ApplySystemCursor()
    {
        if (hideSystemCursor)
        {
            Cursor.visible = !active;
        }
    }

    private void RestoreSystemCursor()
    {
        Cursor.visible = true;
    }
}

using UnityEngine;
using UnityEngine.UI;

public sealed class TongsTool : MonoBehaviour
{
    [SerializeField] private TongsMiniGame miniGame;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private bool toggleOnClick = true;

    private bool selected;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        SetSelected(false);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void Select()
    {
        SetSelected(true);
    }

    public void Deselect()
    {
        SetSelected(false);
    }

    private void HandleClick()
    {
        SetSelected(toggleOnClick ? !selected : true);
    }

    private void SetSelected(bool value)
    {
        selected = value;

        if (selected)
        {
            miniGame?.EquipTongs();
        }
        else
        {
            miniGame?.UnequipTongs();
        }

        if (selectedIndicator != null)
        {
            selectedIndicator.SetActive(selected);
        }
    }
}

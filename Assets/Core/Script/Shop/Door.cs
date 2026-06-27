using UnityEngine;

public sealed class Door : MonoBehaviour
{
    [SerializeField] private GameObject openVisual;
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private bool startClosed = true;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (startClosed)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        IsOpen = true;
        SetVisuals(true);
    }

    public void Close()
    {
        IsOpen = false;
        SetVisuals(false);
    }

    private void SetVisuals(bool open)
    {
        if (openVisual != null)
        {
            openVisual.SetActive(open);
        }

        if (closedVisual != null)
        {
            closedVisual.SetActive(!open);
        }
    }
}

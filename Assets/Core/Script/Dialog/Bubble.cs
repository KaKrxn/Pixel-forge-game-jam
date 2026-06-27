using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class Bubble : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button button;
    [SerializeField] private float clickRadius = 0.8f;

    private Action clickAction;
    private bool IsVisible => root != null ? root.activeInHierarchy : gameObject.activeInHierarchy;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(Click);
        }
    }

    public void Show(Action onClick)
    {
        clickAction = onClick;

        if (root != null)
        {
            root.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Click();
    }

    private void Update()
    {
        if (!IsVisible)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Click();
            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || Camera.main == null)
        {
            return;
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        if (Vector2.Distance(transform.position, worldPosition) <= clickRadius)
        {
            Click();
        }
    }

    private void OnMouseDown()
    {
        Click();
    }

    private void Click()
    {
        clickAction?.Invoke();
    }
}

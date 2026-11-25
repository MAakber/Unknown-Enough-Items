using System;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class UeiEntryClickHandler : MonoBehaviour, IPointerClickHandler
{
    private Action<UeiEntry>? _leftClick;
    private Action<UeiEntry>? _rightClick;
    internal UeiEntry? Entry { get; private set; }

    public void Init(Action<UeiEntry> leftClick, Action<UeiEntry> rightClick)
    {
        _leftClick = leftClick;
        _rightClick = rightClick;
    }

    internal void Bind(UeiEntry? entry) => Entry = entry;

    public void OnPointerClick(PointerEventData eventData)
    {
        UeiEntry? entry = Entry;
        if (entry == null) return;
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _rightClick?.Invoke(entry);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            _leftClick?.Invoke(entry);
        }
    }
}

internal sealed class UeiTooltipRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private string _tipName = string.Empty;
    private string _tipDesc = string.Empty;
    private bool _hovering;

    public void Init(string tipName, string tipDesc)
    {
        tipName ??= string.Empty;
        tipDesc ??= string.Empty;
        if (_tipName == tipName && _tipDesc == tipDesc) return;
        _tipName = tipName;
        _tipDesc = tipDesc;
        if (_hovering) PushTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovering = true;
        PushTooltip();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (_hovering) PushTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
        UeiPanel.ClearNativeTooltip();
    }

    private void OnDisable()
    {
        if (!_hovering) return;
        _hovering = false;
        UeiPanel.ClearNativeTooltip();
    }

    private void PushTooltip() => UeiPanel.SetNativeTooltip(_tipName, _tipDesc);
}

namespace UnityEngine.EventSystems;

public sealed class PointerEventData
{
    public enum InputButton { Left, Right, Middle }
    public InputButton button;
}
public interface IPointerClickHandler { void OnPointerClick(PointerEventData eventData); }
public interface IPointerEnterHandler { void OnPointerEnter(PointerEventData eventData); }
public interface IPointerExitHandler { void OnPointerExit(PointerEventData eventData); }
public interface IPointerMoveHandler { void OnPointerMove(PointerEventData eventData); }

using UnityEngine;
using UnityEngine.EventSystems;

namespace DanceTap
{
    /// <summary>Invisible UI half of the screen. Event-driven input (EventSystem), no polling in Update.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class TapZone : MonoBehaviour, IPointerDownHandler
    {
        [Tooltip("0 = left, 1 = right")]
        [SerializeField] int side;

        public void OnPointerDown(PointerEventData eventData) => GameEvents.RaiseTap(side);
    }
}

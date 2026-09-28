using UnityEngine;
using UnityEngine.EventSystems;

namespace Oilpuz
{
    public sealed class OilInputSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        public OilGameController controller;
        int? activePointer;
        public void OnInitializePotentialDrag(PointerEventData e) { e.useDragThreshold=false; }
        Vector2 Point(PointerEventData e)
        {
            var rect=(RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,e.position,e.pressEventCamera,out Vector2 p);
            return p/(rect.rect.width*.5f);
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(activePointer.HasValue||controller==null||!controller.CanPlay)return;
            if(controller.Simulation.Grab(Point(e)))activePointer=e.pointerId;
        }
        public void OnDrag(PointerEventData e) { if(activePointer==e.pointerId&&controller.CanPlay)controller.Simulation.Move(Point(e)); }
        public void OnPointerUp(PointerEventData e) { if(activePointer==e.pointerId)Cancel(); }
        public void Cancel() { activePointer=null;if(controller!=null)controller.Simulation?.Release(); }
        void OnDisable() { Cancel(); }
    }
}

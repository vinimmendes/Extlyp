using UnityEngine;
using UnityEngine.EventSystems;
using CyberExtraction.Players;

namespace CyberExtraction.UI
{
    public sealed class UguiHudPointer : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IDragHandler
    {
        public HudControl Control;
        bool captured,editing;
        int pointer;
        Vector2 offset;
        public void OnPointerDown(PointerEventData data)
        {
            if(captured) return;
            editing=HudLayoutEditor.IsOpen;
            if(!editing && Control!=HudControl.Move && Control!=HudControl.Aim && Control!=HudControl.Fire && Control!=HudControl.Sprint) return;
            captured=true; pointer=data.pointerId;
            if(editing) { HudLayoutEditor.Selected=Control; offset=UiLayout.Point(data.position)-HudLayout.Bounds(Control).position; }
            else TouchControls.Instance?.BeginPointer(Control,pointer,data.position);
        }
        public void OnDrag(PointerEventData data)
        {
            if(!captured || data.pointerId!=pointer) return;
            if(!editing) { TouchControls.Instance?.MovePointer(pointer,data.position); return; }
            if(!HudLayoutEditor.IsOpen) { captured=false; return; }
            var position=UiLayout.Point(data.position)-offset;
            HudLayout.MoveForEditing(Control,position);
        }
        public void OnPointerUp(PointerEventData data)
        { if(captured && data.pointerId==pointer) Release(); }
        void OnDisable()=>Release();
        void Release() { if(captured && !editing) TouchControls.Instance?.EndPointer(pointer); captured=false; }
    }
}

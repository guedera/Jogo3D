using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SkyRacers
{
    public sealed class MenuOptionFocus : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public Action focused;
        public void OnSelect(BaseEventData data) {focused?.Invoke();}
        public void OnPointerEnter(PointerEventData data) {focused?.Invoke();}
    }
}

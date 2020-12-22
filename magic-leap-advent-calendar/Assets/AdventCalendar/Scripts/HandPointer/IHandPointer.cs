using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;

namespace AdventCalendar.HandPointer
{
    public interface IHandPointer
    {
        float PointerRayDistance { get; set; }
        bool IsShow { get; }


        MLHandTracking.HandKeyPose SelectKeyPose { get; set; }
        MLHandTracking.HandKeyPose RayDrawKeyPose { get; set; }
        void RegisterOnSelectHandler(UnityAction<HandPointerSelect> handler);
        void RegisterOnSelectContinueHandler(UnityAction<HandPointerSelect> handler);
        void Show();
        void Hide();
    }
}
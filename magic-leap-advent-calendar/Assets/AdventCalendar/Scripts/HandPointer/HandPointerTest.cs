using System;
using UnityEngine.XR.MagicLeap;
using MagicLeapTools;
using UnityEngine;

namespace AdventCalendar.HandPointer
{
    /// <summary>
    /// テスト用のスクリプト.
    /// </summary>
    public class HandPointerTest : MonoBehaviour
    {
        [SerializeField] HandPointer pointer;
        [SerializeField] Transform targetObj;


        void Start()
        {
            pointer = GetComponent<HandPointer>();
            if (pointer != null)
            {
                pointer.RegisterOnSelectHandler(OnSelectHandler);
                pointer.RegisterOnSelectContinueHandler(OnSelectContinueHandler);
            }

        }


        private void LateUpdate()
        {
            if (pointer == null)
            {
                return;
            }
        }


        private void OnSelectHandler(
            HandPointerSelect target)
        {
            Debug.Log($"target : {target.Object.name}");
            targetObj.position = target.Position;
        }


        private void OnSelectContinueHandler(
            HandPointerSelect target)
        {
            Debug.Log($"target : {target.Object.name}");
            targetObj.position = target.Position;
        }

    }
}


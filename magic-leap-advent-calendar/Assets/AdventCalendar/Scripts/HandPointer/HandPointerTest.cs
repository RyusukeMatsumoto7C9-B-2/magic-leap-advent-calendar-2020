using UnityEngine;

namespace AdventCalendar.HandPointer
{
    /// <summary>
    /// テスト用のスクリプト.
    /// </summary>
    public class HandPointerTest : MonoBehaviour
    {
        [SerializeField] private HandPointer pointer;
        [SerializeField] private Transform targetObj;


        private void Start()
        {
            if (pointer != null)
            {
                pointer.RegisterOnSelectHandler(OnSelectHandler);
                pointer.RegisterOnSelectContinueHandler(OnSelectContinueHandler);
            }
            pointer.Show();

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


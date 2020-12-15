using UnityEngine;


namespace AdventCalendar.HandPointer
{
    /// <summary>
    /// HandPointerで選択したものをカプセル化したクラス.
    /// </summary>
    public class HandPointerSelect
    {
        public RaycastHit Hit { get; }
        public Vector3 Position => Hit.point;
        public GameObject Object => Hit.collider.gameObject;

        public HandPointerSelect(
            RaycastHit _hit)
        {
            Hit = _hit;
        }
    }
}
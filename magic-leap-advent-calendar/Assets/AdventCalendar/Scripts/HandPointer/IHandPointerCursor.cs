using UnityEngine;

namespace AdventCalendar.HandPointer
{
    public interface IHandPointerCursor
    {
        void Update(HandPointer.HandPointerState state, Vector3 startPosition, Vector3 endPosition);
        void Hide();
        void Show();
    }
}
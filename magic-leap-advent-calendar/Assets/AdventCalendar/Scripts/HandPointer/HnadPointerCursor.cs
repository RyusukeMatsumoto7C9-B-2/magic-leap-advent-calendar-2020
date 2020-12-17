using UnityEngine;

namespace AdventCalendar.HandPointer
{
    /// <summary>
    /// HandPointerのカーソル.
    /// </summary>
    public class HandPointerCursor : IHandPointerCursor
    {

        private LineRenderer lineRenderer;
        private GameObject cursor = null;

        private bool IsValid => lineRenderer != null || cursor != null;
        
        
        
        public HandPointerCursor(
            LineRenderer _lineRenderer,
            GameObject _cursor)
        {
            lineRenderer = _lineRenderer;
            cursor = _cursor;
            lineRenderer.material = cursor.GetComponent<MeshRenderer>().material;
        }


        public void Update(
            HandPointer.HandPointerState state,
            Vector3 startPosition,
            Vector3 endPosition)
        {
            if (!IsValid) return;
            
            if (state == HandPointer.HandPointerState.None)
            {
                Hide();
                return;
            }
            Show();

            RaycastHit hit;
            var ray = new Ray(startPosition, endPosition - startPosition);
            if (Physics.Raycast(ray, out hit, Vector3.Distance(startPosition, endPosition)))
                endPosition = hit.point;
    
            lineRenderer.SetPositions(new []{startPosition, endPosition});
            cursor.SetActive(true);
            cursor.transform.SetPositionAndRotation(lineRenderer.GetPosition(lineRenderer.positionCount - 1), cursor.transform.rotation);
        }

            
        public void Hide()
        {
            if (!IsValid) return;

            lineRenderer.enabled = false;
            cursor.SetActive(false);
        }


        public void Show()
        {
            if (!IsValid) return;
            lineRenderer.enabled = true;
            cursor.SetActive(true);
        }
    }
}
using UnityEngine;

namespace AdventCalendar
{
    public class Sample : MonoBehaviour
    {

        [SerializeField] private Transform camera;
        [SerializeField] private HandController handController;
        [SerializeField] private GameObject objA;
        [SerializeField] private GameObject objB;
        [SerializeField] private GameObject objC;
        
        private void Start()
        {
            handController.RegisterCustomGesture(1f, HandController.HandPose.RFist, HandController.HandPose.ROpenHand, SpawnObjA);
            handController.RegisterCustomGesture(1f, HandController.HandPose.LFist, HandController.HandPose.LOpenHand, SpawnObjB);
            handController.RegisterCustomGesture(1f, HandController.HandPose.LOk, HandController.HandPose.ROk, SpawnObjC);
        }


        private void SpawnObjA()
        {
            GameObject obj = Instantiate(objA);
            obj.transform.position = camera.position + (camera.forward * 0.5f);
        }

        
        private void SpawnObjB()
        {
            GameObject obj = Instantiate(objB);
            obj.transform.position = camera.position + (camera.forward * 0.5f);
        }
        
        
        private void SpawnObjC()
        {
            GameObject obj = Instantiate(objC);
            obj.transform.position = camera.position + (camera.forward * 0.5f);
        }

    }
}


using System;
using MagicLeapTools;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using Debug = UnityEngine.Debug;

using Cysharp.Threading.Tasks;
using UniRx;


namespace AdventCalendar
{
    /// <summary>
    /// ハンドコントローラ.
    /// </summary>
    public class HandController : MonoBehaviour
    {

        struct KeyInfo
        {
            public HandPose pose;
            public float time;
        }

        
        public enum HandPose
        {
            LFinger,
            RFinger,
            LFist,
            RFist,
            LPinch,
            RPinch,
            LThumb,
            RThumb,
            LL,
            RL,
            LOpenHand,
            ROpenHand,
            LOk,
            ROk,
            LC,
            RC,
            LNoPose,
            RNoPose,
            LNoHand,
            RNoHand
        }


        HandPose handPose;
        ManagedHand rHand;
        ManagedHand lHand;
        [SerializeField] GameObject handVisualizer;
        [field: SerializeField] bool IsGestureLogOutput { get; set; } = false;

        

        
        private async void Start()
        {
            await Setup();            
            SwitchHandVisualize();
        }


        private async UniTask Setup()
        {
            await UniTask.WaitUntil(() => HandInput.Ready);
            rHand = HandInput.Right;
            lHand = HandInput.Left;
            rHand.Gesture.OnKeyPoseChanged += OnHandGesturePoseChanged;
            lHand.Gesture.OnKeyPoseChanged += OnHandGesturePoseChanged;

            CreateGestureCommandObserver(1.0f, HandPose.RFist, HandPose.ROpenHand)
                .Subscribe(e => Debug.Log("R Jejeje"))
                .AddTo(this);

            CreateGestureCommandObserver(1.0f, HandPose.LFist, HandPose.LOpenHand)
                .Subscribe(e => Debug.Log("L Jejeje"))
                .AddTo(this);
                
            this.ObserveEveryValueChanged(_ => handPose).Subscribe(e =>
            {
                if (IsGestureLogOutput)
                    Debug.Log($"Key {e}");
            });
        }


        private void SwitchHandVisualize()
        {
#if UNITY_EDITOR
            handVisualizer.SetActive(true);
#elif !UNITY_EDITOR || UNITY_LUMIN
            handVisualizer.SetActive(false);
#endif
        }


        private void OnHandGesturePoseChanged(
            ManagedHand hand,
            MLHandTracking.HandKeyPose pose)
        {
            bool isLeft = hand.Hand.Type == MLHandTracking.HandType.Left;
            switch (pose)
            {
                case MLHandTracking.HandKeyPose.C: handPose = isLeft ? HandPose.LC : HandPose.RC; break;
                case MLHandTracking.HandKeyPose.Finger: handPose = isLeft ? HandPose.LFinger : HandPose.RFinger; break;
                case MLHandTracking.HandKeyPose.Fist: handPose = isLeft ? HandPose.LFist : HandPose.RFist; break;
                case MLHandTracking.HandKeyPose.L: handPose = isLeft ? HandPose.LL : HandPose.RL; break;
                case MLHandTracking.HandKeyPose.Ok: handPose = isLeft ?  HandPose.LOk : HandPose.ROk; break;
                case MLHandTracking.HandKeyPose.Pinch: handPose = isLeft ? HandPose.LPinch : HandPose.RPinch; break;
                case MLHandTracking.HandKeyPose.Thumb: handPose = isLeft ? HandPose.LThumb : HandPose.RThumb; break;
                case MLHandTracking.HandKeyPose.NoHand: handPose = isLeft ? HandPose.LNoHand : HandPose.RNoHand; break;
                case MLHandTracking.HandKeyPose.NoPose: handPose = isLeft ? HandPose.LNoPose : HandPose.RNoPose; break;
                case MLHandTracking.HandKeyPose.OpenHand: handPose = isLeft ? HandPose.LOpenHand : HandPose.ROpenHand; break;
            }
        }


        private IObservable<KeyInfo> CreateGestureCommandObserver(
            float time,
            HandPose poseA,
            HandPose poseB,
            Func<bool> option = null)
        {
            // 指定したキーの判定を通知するObserverを返す.
            IObservable<KeyInfo> GetInputObserver(HandPose pose)
            {
                return this.ObserveEveryValueChanged(_ => handPose)
                    .Where(k => k == pose)
                    .Select(k => new KeyInfo{pose = k, time = Time.realtimeSinceStartup});
            }

            var observer = GetInputObserver(poseA);
            observer = observer.Merge(GetInputObserver(poseB))
                .Buffer(2, 1)
                .Where(b => b[1].time - b[0].time < time)
                .Where(b =>
                {
                    bool ret = b[0].pose == poseA && b[1].pose == poseB;
                    return option == null ? ret : option.Invoke() && ret;
                })
                .Select(b => b[1]);

            return observer;
        }

    }
}
using MagicLeapTools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;

namespace AdventCalendar.HandPointer
{
    /// <summary>
    /// ハンドトラッキングでのポインター.
    /// こいつだけで両手分の処理を行いたい.
    /// </summary>
    public class HandPointer : MonoBehaviour, IHandPointer
    {

        #region --- class OnSelectEvent ---
        class OnSelectEvent : UnityEvent<HandPointerSelect> { }
        #endregion --- class OnSelectEvent ---

        
        #region --- class PointerPosition ---
        class PointerPosition
        {
            public Vector3 Target { get; private set; } = Vector3.zero;
            public Vector3 LastTarget { get; private set; } = Vector3.zero;

            // TODO : 新しいパラメータこっちに差し替える.
            public Vector3 Start { get; private set; } = Vector3.zero;
            public Vector3 LastStart { get; private set; } = Vector3.zero;

            
            public void SetTarget(
                Vector3 position)
            {
                LastTarget = Target;
                Target = position;
            }


            public void SetStartPosition(
                Vector3 position)
            {
                LastStart = Start;
                Start = position;
            }

        }
        #endregion --- class PointerPosition ---

        
        // Pointerのステート.
        public enum HandPointerState
        {
            None,
            NoSelected,
            Selected,
        }


        [SerializeField] Transform mainCamera;
        [SerializeField] float speed = 1f;
        [SerializeField] GameObject cursorPrefab; // ポインターの先端に配置するカーソルのプレハブ,設定されていなければ利用しない.

        public float PointerRayDistance { get; set; } = 2f;
        public MLHandTracking.HandKeyPose SelectKeyPose { get; set; } = MLHandTracking.HandKeyPose.Pinch;
        public MLHandTracking.HandKeyPose RayDrawKeyPose { get; set; } = MLHandTracking.HandKeyPose.OpenHand;
        public HandPointerState LeftHandState { get; private set; } = HandPointerState.None;
        public HandPointerState RightHandState { get; private set; } = HandPointerState.None;

        OnSelectEvent onSelect = new OnSelectEvent();
        OnSelectEvent onSelectContinue = new OnSelectEvent();

        private PointerPosition leftPointerPosition;
        private PointerPosition rightPointerPosition;
        IHandPointerCursor leftCursor;
        IHandPointerCursor rightCursor;

        // TODO : デバッグ用パラメータ.
        public Transform fuga;
        public Transform hoge;
        public float shoulderWidth = 0.2f;
        public float late = 0.3f;
        // =========================
        

        /// <summary>
        /// Eyeトラッキングが有効か否か.
        /// </summary>
        bool IsEyeTrackingValid => MLEyes.IsStarted && MLEyes.CalibrationStatus == MLEyes.Calibration.Good;

        /// <summary>
        /// 描画しているか否か.
        /// </summary>
        public bool IsShow { get; private set; } = false;


        void Start()
        {
            if (HandInput.Ready)
            {
                HandInput.Left.Gesture.OnKeyPoseChanged += OnHandGesturePoseChange;
                HandInput.Right.Gesture.OnKeyPoseChanged += OnHandGesturePoseChange;
            }
            else
            {
                HandInput.OnReady += () =>
                {
                    HandInput.Left.Gesture.OnKeyPoseChanged += OnHandGesturePoseChange;
                    HandInput.Right.Gesture.OnKeyPoseChanged += OnHandGesturePoseChange;
                };
            }

            MLEyes.Start();

            leftCursor = new HandPointerCursor(CreateLineRenderer("LeftLineRenderer"), CreateCursor("LeftHandCursor"));
            rightCursor = new HandPointerCursor(CreateLineRenderer("RightLineRenderer"), CreateCursor("RightHandCursor"));
            
            leftPointerPosition = new PointerPosition();
            rightPointerPosition = new PointerPosition();
        }

        
        void Update()
        {
            DrawRay();
            
            if (LeftHandState == HandPointerState.Selected)
            {
                var result = GetSelect(MLHandTracking.HandType.Left);
                if (result.Item1)
                    onSelectContinue?.Invoke(result.Item2);
            }
            
            if (RightHandState == HandPointerState.Selected)
            {
                var result = GetSelect(MLHandTracking.HandType.Right);
                if (result.Item1)
                    onSelectContinue?.Invoke(result.Item2);
            }
        }


        /// <summary>
        /// HandPointerのカーソル生成.
        /// </summary>
        GameObject CreateCursor(
            string name)
        {
            if (cursorPrefab == null) return null;
            GameObject cursor = Instantiate(cursorPrefab, transform);
            cursor.name = name;
            return cursor;
        }


        void DrawRay()
        {
            if (!HandInput.Ready || !IsShow)
            {
                LeftHandState = RightHandState = HandPointerState.None;
                leftCursor.Hide();
                rightCursor.Hide();
                return;
            }
            LeftHandState = HandInput.Left.Visible ? LeftHandState: HandPointerState.None;
            RightHandState = HandInput.Right.Visible ? RightHandState: HandPointerState.None;
            leftCursor.Show();
            rightCursor.Show();
            
            Vector3 tempTargetPosition = Vector3.zero;
            (bool isValid, Vector3 pos) eyeTrackingTarget = GetEytTrackingTargetPos();
            if (eyeTrackingTarget.isValid)
            {
                tempTargetPosition = eyeTrackingTarget.pos;
            }
            else
            {
                (bool isValid, Vector3 pos) result = GetHeadTrackingTargetPos();
                if (result.isValid)
                {
                    tempTargetPosition = result.pos;
                }
                else
                {
                    LeftHandState = RightHandState = HandPointerState.None;
                }
            }

            // TODO : ポインターのスタート位置の計算はPointerPosition内に収めるべき.
            // Rayのスタート位置計算.
            leftPointerPosition.SetStartPosition(Vector3.Lerp(leftPointerPosition.LastStart, GetRayStartPosition(HandInput.Left), 0.5f));
            rightPointerPosition.SetStartPosition(Vector3.Lerp(rightPointerPosition.LastStart, GetRayStartPosition(HandInput.Right), 0.5f));
            
            // ここで肩から手までのベクトルを求める.
            Vector3 leftShoulderPosition = GetShoulderPosition(MLHandTracking.HandType.Left);
            Vector3 leftHandTarget = leftPointerPosition.Start +
                                     (leftPointerPosition.Start - leftShoulderPosition).normalized * PointerRayDistance;
            leftHandTarget = Vector3.Lerp(leftHandTarget, tempTargetPosition, late);
            leftPointerPosition.SetTarget(Vector3.Lerp(leftPointerPosition.LastTarget, leftHandTarget, Time.deltaTime * speed));
            leftCursor.Update(LeftHandState, leftPointerPosition.Start, leftPointerPosition.Target);

            Vector3 rightShoulderPosition = GetShoulderPosition(MLHandTracking.HandType.Right);
            Vector3 rightHandTarget = rightPointerPosition.Start +
                                      (rightPointerPosition.Start - rightShoulderPosition).normalized * PointerRayDistance;
            rightHandTarget = Vector3.Lerp(rightHandTarget, tempTargetPosition, late);
            rightPointerPosition.SetTarget(Vector3.Lerp(rightPointerPosition.LastTarget, rightHandTarget, Time.deltaTime * speed));
            rightCursor.Update(RightHandState, rightPointerPosition.Start, rightPointerPosition.Target);
        }


        /// <summary>
        /// RaycastHitしたターゲットを返す、ヒットしない場合は Item2 はnullになる.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        (bool, HandPointerSelect) GetRayCastHitTarget(
            Ray ray,
            float maxDistance)
        {
            RaycastHit hit;
            return Physics.Raycast(ray, out hit, maxDistance) ? (true, new HandPointerSelect(hit)) : (false, null);
        }

        
        /// <summary>
        /// 選択したターゲットを取得する,選択できていない場合は Item2 はnullになる.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        (bool, HandPointerSelect) GetSelect(
            MLHandTracking.HandType type)
        {
            Vector3 start = type == MLHandTracking.HandType.Left ? leftPointerPosition.Start : rightPointerPosition.Start;
            Vector3 target = type == MLHandTracking.HandType.Left ? leftPointerPosition.Target : rightPointerPosition.Target;
            return GetRayCastHitTarget(new Ray(start, target - start), PointerRayDistance);
        }


        /// <summary>
        /// ハンドジェスチャの変更イベント取得.
        /// </summary>
        /// <param name="hand"></param>
        /// <param name="pose"></param>
        void OnHandGesturePoseChange(
            ManagedHand hand,
            MLHandTracking.HandKeyPose pose)
        {
            switch (hand.Hand.Type)
            {
                case MLHandTracking.HandType.Left:
                    LeftHandState = pose == SelectKeyPose ? HandPointerState.Selected : HandPointerState.NoSelected;
                    break;
                
                case MLHandTracking.HandType.Right:
                    RightHandState = pose == SelectKeyPose ? HandPointerState.Selected : HandPointerState.NoSelected;
                    break;
            }

            if (LeftHandState == HandPointerState.Selected)
            {
                var result = GetSelect(MLHandTracking.HandType.Left);
                if (result.Item1)
                    onSelect?.Invoke(result.Item2);
            }
            
            if (RightHandState == HandPointerState.Selected)
            {
                var result = GetSelect(MLHandTracking.HandType.Right);
                if (result.Item1)
                    onSelect?.Invoke(result.Item2);
            }
        }


        /// <summary>
        /// LineRendererを作成.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        LineRenderer CreateLineRenderer(
            string name)
        {
            var ret = GameObject.Instantiate(new GameObject(name), transform).AddComponent<LineRenderer>();
            ret.startWidth = 0.01f;
            ret.endWidth = 0.01f;
            ret.enabled = false;
            return ret;
        }
        

        /// <summary>
        /// 人差し指の根元と親指の根元の中間座標を起点として.
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        Vector3 GetRayStartPosition(ManagedHand hand)
            => Vector3.Lerp(hand.Skeleton.Thumb.Knuckle.positionFiltered, hand.Skeleton.Index.Knuckle.positionFiltered, 0.5f);


        /// <summary>
        /// Eyeトラッキングのターゲットを取得.
        /// </summary>
        /// <returns></returns>
        (bool, Vector3) GetEytTrackingTargetPos()
        {
            if (!IsEyeTrackingValid) return (false, Vector3.zero);
            
            bool isBlink = MLEyes.LeftEye.IsBlinking || MLEyes.RightEye.IsBlinking;
            if (isBlink) return (false, Vector3.zero);

            // Eyeトラッキングが有効ならEyeトラッキングの向きで補正する.
            float leftConfidence = MLEyes.LeftEye.CenterConfidence * -0.5f;
            float rightConfidence = MLEyes.RightEye.CenterConfidence * 0.5f;
            float eyeRatio = 0.5f + (leftConfidence + rightConfidence);
            Vector3 gazeRay =  Vector3.Lerp(MLEyes.LeftEye.ForwardGaze, MLEyes.RightEye.ForwardGaze, eyeRatio).normalized;
            Vector3 eyeTargetPos = mainCamera.position + (gazeRay * PointerRayDistance);

            return (true, eyeTargetPos);
        }

        
        /// <summary>
        /// Headトラッキングのターゲットを取得.
        /// </summary>
        /// <returns></returns>
        (bool, Vector3) GetHeadTrackingTargetPos()
        {
            if (mainCamera == null) return (false, Vector3.zero);
            
            Vector3 targetPos = Vector3.zero;
            targetPos = mainCamera.position + (mainCamera.forward.normalized * 2f);

            return (true, targetPos);
        }


        /// <summary>
        /// 頭の位置から推定した肩の座標を取得.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Vector3 GetShoulderPosition(
            MLHandTracking.HandType type)
        {
            Vector3 headPosition = mainCamera.position;
            Vector3 shoulderPosition = headPosition + (mainCamera.right * (type == MLHandTracking.HandType.Left ? -shoulderWidth : shoulderWidth)) + (-mainCamera.up * 0.15f);

            if (type == MLHandTracking.HandType.Left)
                hoge.position = shoulderPosition;
            else
                fuga.position = shoulderPosition;            

            return shoulderPosition;
        }


        /// <summary>
        /// 選択のイベントハンドラを登録.
        /// </summary>
        /// <param name="callback"></param>
        public void RegisterOnSelectHandler(
            UnityAction<HandPointerSelect> callback)
        {
            if (onSelect == null)
                onSelect = new OnSelectEvent();
            onSelect.AddListener(callback);
            Debug.Log($"Count : {onSelect.GetPersistentEventCount()}");
        }
        
        
        /// <summary>
        /// 長選択のイベントハンドラを登録.
        /// </summary>
        /// <param name="callback"></param>
        public void RegisterOnSelectContinueHandler(
            UnityAction<HandPointerSelect> callback)
        {
            if (onSelectContinue == null)
                onSelectContinue = new OnSelectEvent();
            onSelectContinue.AddListener(callback);
        }


        /// <summary>
        /// HandPointerを有効化.
        /// </summary>
        public void Show() => IsShow = true;


        /// <summary>
        /// HandPointerを無効化.
        /// </summary>
        public void Hide() => IsShow = false;
    }
}
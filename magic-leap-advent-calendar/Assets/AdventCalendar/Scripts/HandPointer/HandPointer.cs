using MagicLeapTools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;

namespace AdventCalendar.HandPointer
{
    /// <summary>
    /// ハンドトラッキングでのポインター.
    /// こいつだけで両手分の処理を行う.
    /// </summary>
    public class HandPointer : MonoBehaviour, IHandPointer
    {

        #region --- class SelectEvent ---
        private class SelectEvent : UnityEvent<HandPointerSelect> { }
        #endregion --- class SelectEvent ---

        
        #region --- class PointerPosition ---
        private class PointerPosition
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
                Start = Vector3.Lerp(LastStart, position, 0.5f);
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
        

        [SerializeField] private Transform mainCamera;
        [SerializeField] private float speed = 1f;
        [SerializeField] private GameObject cursorPrefab; // ポインターの先端に配置するカーソルのプレハブ,設定されていなければ利用しない.
        [SerializeField] private float eyeTrackingRatio = 0.3f;

        public float PointerRayDistance { get; set; } = 2f;
        public MLHandTracking.HandKeyPose SelectKeyPose { get; set; } = MLHandTracking.HandKeyPose.Pinch;
        public MLHandTracking.HandKeyPose RayDrawKeyPose { get; set; } = MLHandTracking.HandKeyPose.OpenHand;
        public HandPointerState LeftHandState { get; private set; } = HandPointerState.None;
        public HandPointerState RightHandState { get; private set; } = HandPointerState.None;

        private SelectEvent onSelect = new SelectEvent();
        private SelectEvent onSelectContinue = new SelectEvent();

        private PointerPosition leftPointerPosition;
        private PointerPosition rightPointerPosition;
        private IHandPointerCursor leftCursor;
        private IHandPointerCursor rightCursor;

        // TODO : デバッグ用パラメータ.
        private Vector3 debugRightShoulderPosition;
        private Vector3 debugLeftShoulderPosition;
        
        public float shoulderWidth = 0.2f;
        // =========================
        

        /// <summary>
        /// Eyeトラッキングが有効か否か.
        /// </summary>
        private bool IsEyeTrackingValid => MLEyes.IsStarted && MLEyes.CalibrationStatus == MLEyes.Calibration.Good;

        /// <summary>
        /// 描画しているか否か.
        /// </summary>
        public bool IsShow { get; private set; } = false;


        private void Start()
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

        
        private void Update()
        {
            UpdateHandRay();
            
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
        private GameObject CreateCursor(
            string name)
        {
            if (cursorPrefab == null) return null;
            GameObject cursor = Instantiate(cursorPrefab, transform);
            cursor.name = name;
            return cursor;
        }

        
        private void UpdateHandRay()
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

            // Rayのスタート位置計算.
            leftPointerPosition.SetStartPosition(GetRayStartPosition(HandInput.Left));
            rightPointerPosition.SetStartPosition(GetRayStartPosition(HandInput.Right));

            // ポインターの更新.
            leftPointerPosition.SetTarget(Vector3.Lerp(leftPointerPosition.LastTarget, GetCurrentTargetPosition(MLHandTracking.HandType.Left), Time.deltaTime * speed));
            leftCursor.Update(LeftHandState, leftPointerPosition.Start, leftPointerPosition.Target);

            rightPointerPosition.SetTarget(Vector3.Lerp(rightPointerPosition.LastTarget, GetCurrentTargetPosition(MLHandTracking.HandType.Right), Time.deltaTime * speed));
            rightCursor.Update(RightHandState, rightPointerPosition.Start, rightPointerPosition.Target);
        }


        private Vector3 GetCurrentTargetPosition(
            MLHandTracking.HandType type)
        {
            Vector3 tempTargetDir = Vector3.zero;
            (bool isValid, Vector3 dir) eyeTrackingDir = GetEyeTrackingNormalizedDir();
            if (eyeTrackingDir.isValid)
                tempTargetDir = eyeTrackingDir.dir;

            Vector3 start = type == MLHandTracking.HandType.Left ? leftPointerPosition.Start : rightPointerPosition.Start;
            Vector3 shoulderToHandDir = (start - GetShoulderPosition(type)).normalized;
            Vector3 dir = tempTargetDir == Vector3.zero ? shoulderToHandDir : Vector3.Lerp(shoulderToHandDir, tempTargetDir, eyeTrackingRatio).normalized;
            return start + dir * PointerRayDistance;
        }


        /// <summary>
        /// RaycastHitしたターゲットを返す、ヒットしない場合は Item2 はnullになる.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        private (bool, HandPointerSelect) GetRayCastHitTarget(
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
        private (bool, HandPointerSelect) GetSelect(
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
        private void OnHandGesturePoseChange(
            ManagedHand hand,
            MLHandTracking.HandKeyPose pose)
        {
            switch (hand.Hand.Type)
            {
                case MLHandTracking.HandType.Left:
                    LeftHandState = pose == SelectKeyPose ? HandPointerState.Selected : HandPointerState.NoSelected;
                    if (LeftHandState == HandPointerState.Selected)
                    {
                        (bool, HandPointerSelect) result = GetSelect(MLHandTracking.HandType.Left);
                        if (result.Item1)
                            onSelect?.Invoke(result.Item2);
                    }
                    break;
                
                case MLHandTracking.HandType.Right:
                    RightHandState = pose == SelectKeyPose ? HandPointerState.Selected : HandPointerState.NoSelected;
                    if (RightHandState == HandPointerState.Selected)
                    {
                        var result = GetSelect(MLHandTracking.HandType.Right);
                        if (result.Item1)
                            onSelect?.Invoke(result.Item2);
                    }
                    break;
            }

            /*
            if (LeftHandState == HandPointerState.Selected)
            {
                (bool, HandPointerSelect) result = GetSelect(MLHandTracking.HandType.Left);
                if (result.Item1)
                    onSelect?.Invoke(result.Item2);
            }
            
            if (RightHandState == HandPointerState.Selected)
            {
                var result = GetSelect(MLHandTracking.HandType.Right);
                if (result.Item1)
                    onSelect?.Invoke(result.Item2);
            }
        */
        }


        /// <summary>
        /// LineRendererを作成.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private LineRenderer CreateLineRenderer(
            string name)
        {
            var ret = Instantiate(new GameObject(name), transform).AddComponent<LineRenderer>();
            ret.startWidth = 0.01f;
            ret.endWidth = 0.01f;
            ret.enabled = false;
            return ret;
        }


        /// <summary>
        /// 手の中心をスタートポイントとする.
        /// </summary>
        /// <param name="hand"></param>
        /// <returns></returns>
        private Vector3 GetRayStartPosition(ManagedHand hand) 
            => Vector3.Lerp(hand.Skeleton.Thumb.Knuckle.positionFiltered, hand.Skeleton.Index.Knuckle.positionFiltered, 0.5f);

        
        /// <summary>
        /// Eyeトラッキングの方向を取得.
        /// </summary>
        /// <returns></returns>
        private (bool isValid, Vector3 normalizedDir) GetEyeTrackingNormalizedDir()
        {
            if (!IsEyeTrackingValid) return (false, Vector3.zero);
            
            bool isBlink = MLEyes.LeftEye.IsBlinking || MLEyes.RightEye.IsBlinking;
            if (isBlink) return (false, Vector3.zero);

            // Eyeトラッキングが有効ならEyeトラッキングの向きで補正する.
            float leftConfidence = MLEyes.LeftEye.CenterConfidence * -0.5f;
            float rightConfidence = MLEyes.RightEye.CenterConfidence * 0.5f;
            float eyeRatio = 0.5f + (leftConfidence + rightConfidence);
            return (true, Vector3.Lerp(MLEyes.LeftEye.ForwardGaze, MLEyes.RightEye.ForwardGaze, eyeRatio).normalized);
        }

        
        /// <summary>
        /// 頭の位置から推定した肩の座標を取得.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private Vector3 GetShoulderPosition(
            MLHandTracking.HandType type)
        {
            Vector3 headPosition = mainCamera.position;
            Vector3 shoulderPosition = headPosition + (mainCamera.right * (type == MLHandTracking.HandType.Left ? -shoulderWidth : shoulderWidth)) + (-mainCamera.up * 0.15f);

            if (type == MLHandTracking.HandType.Left)
                debugLeftShoulderPosition = shoulderPosition;
            else
                debugRightShoulderPosition = shoulderPosition;

            return shoulderPosition;
        }


        private void OnDrawGizmos()
        {
            // 推定の肩の位置を表示.
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(debugLeftShoulderPosition, 0.1f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(debugRightShoulderPosition, 0.1f);
        }


        /// <summary>
        /// 選択のイベントハンドラを登録.
        /// </summary>
        /// <param name="callback"></param>
        public void RegisterOnSelectHandler(
            UnityAction<HandPointerSelect> callback)
        {
            if (onSelect == null)
                onSelect = new SelectEvent();
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
                onSelectContinue = new SelectEvent();
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
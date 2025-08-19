using UnityEngine;

namespace LifeQuest.Core
{
    public class RigSwitcher : MonoBehaviour
    {
        public enum Mode { Auto, Desktop, VR }

        [Header("Rigs (둘 중 하나만 활성화)")]
        public GameObject desktopRig;  // 카메라 + PlayerMover(데스크탑)
        public GameObject vrRig;       // XR Origin(유신 선배님이 구성)

        [Header("Mode")]
        public Mode startMode = Mode.Auto;

        [Header("Camera Tag")]
        public bool ensureMainCameraTag = true; // 활성 카메라에 MainCamera 태그 부여

        void Awake()
        {
            var mode = ResolveMode(startMode);
            ApplyMode(mode);
        }

        Mode ResolveMode(Mode m)
        {
            if (m != Mode.Auto) return m;

            // 안전 기본값: 에디터/로컬 개발은 데스크탑
            // (XR 패키지 의존 없이 동작. 필요하면 수동 VR로 전환)
#if UNITY_EDITOR
            return Mode.Desktop;
#else
            // 런타임에서도 기본 Desktop. 코드 병합 이후 VR 테스트시 Inspector에서 VR로 강제.
            return Mode.Desktop;
#endif
        }

        public void ApplyMode(Mode mode)
        {
            if (desktopRig) desktopRig.SetActive(mode == Mode.Desktop);
            if (vrRig) vrRig.SetActive(mode == Mode.VR);

            if (ensureMainCameraTag)
            {
                // 활성 리그의 카메라에 MainCamera 태그를 보장(빌보드/레이캐스트 의존성 해결)
                var camA = desktopRig ? desktopRig.GetComponentInChildren<Camera>(true) : null;
                var camB = vrRig ? vrRig.GetComponentInChildren<Camera>(true) : null;

                if (camA) camA.tag = (mode == Mode.Desktop) ? "MainCamera" : "Untagged";
                if (camB) camB.tag = (mode == Mode.VR) ? "MainCamera" : "Untagged";
            }

            Debug.Log($"[RigSwitcher] Mode = {mode}");
        }
    }
}

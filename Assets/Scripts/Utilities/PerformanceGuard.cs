using System.Collections;
using UnityEngine;

namespace LifeQuest.Utilities
{
    /// <summary>
    /// 퍼포먼스 최적화 유틸리티.
    /// - PC/VR 환경별 기본 품질 세팅
    /// - VSync / Target FPS / 그림자 / AA / 텍스처 / GC 모드
    /// - (선택) FPS 모니터링으로 저사양 모드 강등
    /// </summary>
    public class PerformanceGuard : MonoBehaviour
    {
        public enum Profile { DesktopDev, DesktopPerf, VRPerf }

        [Header("Profile")]
        public Profile profile = Profile.DesktopDev;
        public bool applyOnAwake = true;

        [Header("Common")]
        [Tooltip("VSync 강제 OFF (기본: Off). Off일 때는 targetFrameRate 적용됨")]
        public bool forceVSyncOff = true;
        [Tooltip("데스크탑 타겟 FPS")]
        public int targetFpsDesktop = 60;
        [Tooltip("VR 타겟 FPS (일반적으로 72/80/90 중 선택)")]
        public int targetFpsVR = 72;

        [Header("Quality Overrides")]
        [Tooltip("품질 레벨 인덱스(0=Fastest ... N=Fantastic). -1이면 적용 안 함")]
        public int qualityLevel = -1;
        [Tooltip("안티앨리어싱 (0/2/4/8)")]
        public int antiAliasing = 0;
        [Tooltip("픽셀 라이트 개수")]
        public int pixelLightCount = 1;
        [Tooltip("그림자 거리")]
        public float shadowDistance = 25f;
        [Tooltip("쉐도우 캐스케이드 수 (0/2/4)")]
        public int shadowCascades = 0;
        [Tooltip("텍스처 밉맵 제한 (0=풀, 1=1/2, 2=1/4)")]
        public int masterTextureLimit = 1;
        [Tooltip("비등방성 필터링")]
        public AnisotropicFiltering anisotropic = AnisotropicFiltering.Disable;

        [Header("System")]
        [Tooltip("Incremental GC 사용 여부")]
        public bool useIncrementalGC = true;
        [Tooltip("화면 꺼지지 않도록 유지")]
        public bool neverSleep = true;

        [Header("Adaptive (optional)")]
        public bool enableAdaptive = true;
        [Tooltip("FPS 임계값 이하일 경우 단계적 품질 강등")]
        public float fpsThreshold = 55f;
        [Tooltip("FPS 평균 계산 윈도우")]
        public float fpsAvgWindow = 2.0f;
        [Tooltip("체크 주기 (초)")]
        public float checkInterval = 1.0f;
        [Tooltip("최대 강등 단계 (0=비활성, 1~3)")]
        public int maxDegradeSteps = 2;

        float _accum, _elapsed; int _frames;
        int _degradeLevel = 0;
        bool _running;

        void Awake()
        {
            // 프로파일 기본값 적용
            ApplyProfileDefaults(profile);

            if (applyOnAwake) ApplyNow();

            if (enableAdaptive && maxDegradeSteps > 0)
                StartCoroutine(AdaptiveLoop());
        }

        public void ApplyNow()
        {
            // VSync / FPS
            if (forceVSyncOff) QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = IsVRProfile(profile) ? targetFpsVR : targetFpsDesktop;

            // Quality
            if (qualityLevel >= 0 && qualityLevel < QualitySettings.names.Length)
                QualitySettings.SetQualityLevel(qualityLevel, true);

            QualitySettings.antiAliasing = Mathf.Clamp(antiAliasing, 0, 8);
            QualitySettings.pixelLightCount = Mathf.Max(0, pixelLightCount);
            QualitySettings.shadowDistance = Mathf.Max(0f, shadowDistance);
            QualitySettings.shadowCascades = Mathf.Clamp(shadowCascades, 0, 4);
            QualitySettings.globalTextureMipmapLimit = Mathf.Clamp(masterTextureLimit, 0, 2);
            QualitySettings.anisotropicFiltering = anisotropic;

#if UNITY_2019_3_OR_NEWER
            // Unity 2022+ 에서는 Incremental 모드가 기본이므로 Enabled/Disabled 만 사용 가능
            UnityEngine.Scripting.GarbageCollector.GCMode =
                useIncrementalGC ? UnityEngine.Scripting.GarbageCollector.Mode.Enabled
                                 : UnityEngine.Scripting.GarbageCollector.Mode.Disabled;
#endif
            if (neverSleep) Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Debug.Log($"[PerformanceGuard] Applied: {profile}, vsync={QualitySettings.vSyncCount}, " +
                      $"tFPS={(IsVRProfile(profile) ? targetFpsVR : targetFpsDesktop)}, QL={QualitySettings.GetQualityLevel()}, " +
                      $"AA={QualitySettings.antiAliasing}, PL={QualitySettings.pixelLightCount}, " +
                      $"SDist={QualitySettings.shadowDistance}, SCasc={QualitySettings.shadowCascades}, " +
                      $"MTex={QualitySettings.globalTextureMipmapLimit}, Aniso={QualitySettings.anisotropicFiltering}, " +
#if UNITY_2019_3_OR_NEWER
                      $"IncGC={UnityEngine.Scripting.GarbageCollector.isIncremental}"
#else
                      $"IncGC=N/A"
#endif
            );
        }

        void ApplyProfileDefaults(Profile p)
        {
            switch (p)
            {
                case Profile.DesktopDev:
                    forceVSyncOff = true;
                    targetFpsDesktop = 60;
                    qualityLevel = -1;
                    antiAliasing = 0;
                    pixelLightCount = 2;
                    shadowDistance = 35f;
                    shadowCascades = 2;
                    masterTextureLimit = 0;
                    anisotropic = AnisotropicFiltering.Enable;
                    useIncrementalGC = true;
                    neverSleep = true;
                    break;

                case Profile.DesktopPerf:
                    forceVSyncOff = true;
                    targetFpsDesktop = 60;
                    qualityLevel = 0;
                    antiAliasing = 0;
                    pixelLightCount = 1;
                    shadowDistance = 20f;
                    shadowCascades = 0;
                    masterTextureLimit = 1;
                    anisotropic = AnisotropicFiltering.Disable;
                    useIncrementalGC = true;
                    neverSleep = true;
                    break;

                case Profile.VRPerf:
                    forceVSyncOff = true;
                    targetFpsVR = 72;
                    qualityLevel = 0;
                    antiAliasing = 2;
                    pixelLightCount = 1;
                    shadowDistance = 15f;
                    shadowCascades = 0;
                    masterTextureLimit = 1;
                    anisotropic = AnisotropicFiltering.Disable;
                    useIncrementalGC = true;
                    neverSleep = true;
                    break;
            }
        }

        static bool IsVRProfile(Profile p) => p == Profile.VRPerf;

        IEnumerator AdaptiveLoop()
        {
            _running = true;
            while (_running)
            {
                _elapsed += Time.unscaledDeltaTime;
                _accum += 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                _frames++;

                if (_elapsed >= fpsAvgWindow)
                {
                    float avgFps = _accum / _frames;
                    _accum = 0f; _frames = 0; _elapsed = 0f;

                    if (avgFps < fpsThreshold && _degradeLevel < maxDegradeSteps)
                    {
                        _degradeLevel++;
                        ApplyDegradeStep(_degradeLevel);
                        Debug.Log($"[PerformanceGuard] Degrade step={_degradeLevel} (avgFPS={avgFps:0.0})");
                    }
                }

                yield return new WaitForSeconds(checkInterval);
            }
        }

        void ApplyDegradeStep(int level)
        {
            switch (level)
            {
                case 1:
                    QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 15f);
                    QualitySettings.shadowCascades = 0;
                    QualitySettings.antiAliasing = Mathf.Min(QualitySettings.antiAliasing, 2);
                    break;
                case 2:
                    QualitySettings.globalTextureMipmapLimit = Mathf.Max(QualitySettings.globalTextureMipmapLimit, 1);
                    QualitySettings.pixelLightCount = Mathf.Min(QualitySettings.pixelLightCount, 1);
                    break;
                case 3:
                    QualitySettings.globalTextureMipmapLimit = Mathf.Max(QualitySettings.globalTextureMipmapLimit, 2);
                    break;
            }
        }

        void OnDisable()
        {
            _running = false;
        }
    }
}

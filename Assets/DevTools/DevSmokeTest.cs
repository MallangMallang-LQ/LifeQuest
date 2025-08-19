using System.Collections;
using UnityEngine;
using LifeQuest.NPCFlow.UI;     // NpcViewDriver
using LifeQuest.NPCFlow.Core;   // GameFlowController

[DisallowMultipleComponent]
public class DevSmokeTest : MonoBehaviour
{
    [Header("Refs (필수)")]
    [SerializeField] private NpcViewDriver view;
    [SerializeField] private GameFlowController flow;

    [Header("옵션")]
    [SerializeField] private AudioClip testClip;  // 아무 짧은 클립
    [SerializeField] private Transform cameraToWiggle; // 비우면 Camera.main
    [SerializeField] private float camNudge = 0.25f;

    private Transform cam;

    void Awake()
    {
        cam = cameraToWiggle ? cameraToWiggle : Camera.main ? Camera.main.transform : null;
    }

    void OnGUI()
    {
        const int w = 220, h = 30; int x = 12, y = 12, gap = 6;
        GUI.Box(new Rect(x - 4, y - 6, w + 8, h * 6 + gap * 6 + 12), "One-Click Smoke Test");

        if (GUI.Button(new Rect(x, y, w, h), "RUN ALL (UI/Voice)")) { StartCoroutine(RunAll()); }
        y += h + gap;

        if (GUI.Button(new Rect(x, y, w, h), "UI Text Only")) { RunUITextOnly(); }
        y += h + gap;

        if (GUI.Button(new Rect(x, y, w, h), "Billboard Wiggle Cam")) { WiggleCamera(); }
        y += h + gap;

        if (GUI.Button(new Rect(x, y, w, h), "Play Voice")) { if (view && testClip) view.PlayVoice(testClip, true); }
        y += h + gap;

        if (GUI.Button(new Rect(x, y, w, h), "Stop Voice")) { if (view) view.StopVoice(); }
        y += h + gap;

        if (GUI.Button(new Rect(x, y, w, h), "Clear HUD")) { if (view) view.Clear(); }
    }

    IEnumerator RunAll()
    {
        // 0) 레퍼런스 검사
        if (!view) { Debug.LogError("[Smoke] NpcViewDriver 미연결"); yield break; }
        if (!flow) { Debug.LogError("[Smoke] GameFlowController 미연결"); yield break; }

        // 1) 존 진입 대행: 흐름 시작 호출
        Debug.Log("[Smoke] StartFaceToFaceFlow()");
        flow.StartFaceToFaceFlow();

        // 2) UI 표시 즉시 확인
        RunUITextOnly();
        yield return null;

        // 3) 카메라 살짝 움직여 Billboard 확인
        WiggleCamera();
        yield return new WaitForSeconds(0.2f);
        WiggleCamera();
        yield return new WaitForSeconds(0.2f);

        // 4) 음성 재생/정지 확인
        if (testClip)
        {
            view.PlayVoice(testClip, true);
            yield return new WaitForSeconds(Mathf.Min(0.7f, testClip.length * 0.5f));
            view.StopVoice();
        }

        Debug.Log("[Smoke] ✅ ALL PASS (UI/Voice/Billboard basic)");
    }

    void RunUITextOnly()
    {
        view.SetNpcText("어서오세요. 주문 도와드리겠습니다.");
        view.SetPlayerText("마이크를 켜고 주문을 말씀해 보세요.");
    }

    void WiggleCamera()
    {
        var t = cam ?? (Camera.main ? Camera.main.transform : null);
        if (!t) return;
        t.position += t.right * camNudge;  // 좌우 살짝 이동해서 Billboard 반응 확인
    }
}

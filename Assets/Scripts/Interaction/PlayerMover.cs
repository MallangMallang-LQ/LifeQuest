using UnityEngine;

namespace LifeQuest.Core
{
    public class RigSwitcher : MonoBehaviour
    {
        public enum Mode { Auto, Desktop, VR }

        [Header("Rigs (�� �� �ϳ��� Ȱ��ȭ)")]
        public GameObject desktopRig;  // ī�޶� + PlayerMover(����ũž)
        public GameObject vrRig;       // XR Origin(���� ������� ����)

        [Header("Mode")]
        public Mode startMode = Mode.Auto;

        [Header("Camera Tag")]
        public bool ensureMainCameraTag = true; // Ȱ�� ī�޶� MainCamera �±� �ο�

        void Awake()
        {
            var mode = ResolveMode(startMode);
            ApplyMode(mode);
        }

        Mode ResolveMode(Mode m)
        {
            if (m != Mode.Auto) return m;

            // ���� �⺻��: ������/���� ������ ����ũž
            // (XR ��Ű�� ���� ���� ����. �ʿ��ϸ� ���� VR�� ��ȯ)
#if UNITY_EDITOR
            return Mode.Desktop;
#else
            // ��Ÿ�ӿ����� �⺻ Desktop. �ڵ� ���� ���� VR �׽�Ʈ�� Inspector���� VR�� ����.
            return Mode.Desktop;
#endif
        }

        public void ApplyMode(Mode mode)
        {
            if (desktopRig) desktopRig.SetActive(mode == Mode.Desktop);
            if (vrRig) vrRig.SetActive(mode == Mode.VR);

            if (ensureMainCameraTag)
            {
                // Ȱ�� ������ ī�޶� MainCamera �±׸� ����(������/����ĳ��Ʈ ������ �ذ�)
                var camA = desktopRig ? desktopRig.GetComponentInChildren<Camera>(true) : null;
                var camB = vrRig ? vrRig.GetComponentInChildren<Camera>(true) : null;

                if (camA) camA.tag = (mode == Mode.Desktop) ? "MainCamera" : "Untagged";
                if (camB) camB.tag = (mode == Mode.VR) ? "MainCamera" : "Untagged";
            }

            Debug.Log($"[RigSwitcher] Mode = {mode}");
        }
    }
}

using UnityEngine;

namespace LifeQuest.NPCFlow.NPC
{
    /// <summary>
    /// NPC Animator 브릿지 + 영수증 배출 트리거 연계.
    /// - anim: NPC 본체 Animator (Idle/Talk/Greeting/Confirm/HandOff 등)
    /// - receiptAnimator: 영수증 오브젝트(ReceiptProp)의 Animator(선택)
    /// - legacyReceiptAnimation: 레거시 Animation 컴포넌트 사용하는 경우(선택)
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCAnimatorDriver : MonoBehaviour
    {
        [Header("NPC Animator")]
        public Animator anim;

        [Tooltip("말하는 중인지 여부 Bool 파라미터")]
        public string talkBool = "Talking";
        [Tooltip("인사 Trigger 파라미터")]
        public string greetTrig = "Greeting";
        [Tooltip("확인(결제확정 등) Trigger 파라미터")]
        public string confirmTrig = "Confirm";
        [Tooltip("전달(건네주기/영수증/음료 등) Trigger 파라미터")]
        public string handoffTrig = "HandOff";

        [Header("Receipt (optional)")]
        [Tooltip("영수증 오브젝트의 Animator (전용 컨트롤러 & Eject 트리거 사용)")]
        public Animator receiptAnimator;
        [Tooltip("영수증 전용 Animator의 Trigger 이름")]
        public string receiptEjectTrig = "Eject";

        [Tooltip("레거시 Animation을 쓰는 경우 지정(Receipt animation 클립 재생)")]
        public Animation legacyReceiptAnimation;
        [Tooltip("레거시 Animation 클립 이름 (예: \"Receipt animation\")")]
        public string legacyReceiptClipName = "Receipt animation";

        [Tooltip("영수증 오브젝트 자체를 켜고/끄고 싶을 때(선택)")]
        public GameObject receiptProp;

        // --- 기존 API 그대로 ---
        public void SetTalking(bool on) { if (anim) anim.SetBool(talkBool, on); }
        public void PlayGreeting() { if (anim) anim.SetTrigger(greetTrig); }
        public void PlayConfirm() { if (anim) anim.SetTrigger(confirmTrig); }
        public void PlayHandOff() { if (anim) anim.SetTrigger(handoffTrig); }

        /// <summary>
        /// 결제 완료 시 호출: NPC의 HandOff 트리거 + 영수증 배출 연계
        /// </summary>
        public void PlayPaymentComplete()
        {
            // 1) NPC 본체 애니메이션 (건네주기 포즈 등)
            PlayHandOff();

            // 2) 영수증 배출(둘 중 하나 또는 둘 다)
            if (receiptProp && !receiptProp.activeSelf) receiptProp.SetActive(true);

            if (receiptAnimator)
                receiptAnimator.SetTrigger(receiptEjectTrig);

            if (legacyReceiptAnimation)
            {
                // 자동재생/루프 방지
                legacyReceiptAnimation.wrapMode = WrapMode.Once;
                if (!string.IsNullOrEmpty(legacyReceiptClipName))
                    legacyReceiptAnimation.Play(legacyReceiptClipName);
                else
                    legacyReceiptAnimation.Play(); // 기본 클립
            }
        }

        /// <summary>
        /// (선택) 애니메이션 이벤트로 호출해 영수증 숨기기 등 마무리
        /// Receipt_Eject 마지막 프레임에 Animation Event로 이 함수 연결 가능
        /// </summary>
        public void OnReceiptEjectEnd()
        {
            if (receiptProp) receiptProp.SetActive(false);
        }
    }
}

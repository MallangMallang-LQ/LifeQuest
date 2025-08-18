using UnityEngine;

namespace LifeQuest.NPCFlow.NPC
{
    /// <summary>
    /// NPC Animator �긴�� + ������ ���� Ʈ���� ����.
    /// - anim: NPC ��ü Animator (Idle/Talk/Greeting/Confirm/HandOff ��)
    /// - receiptAnimator: ������ ������Ʈ(ReceiptProp)�� Animator(����)
    /// - legacyReceiptAnimation: ���Ž� Animation ������Ʈ ����ϴ� ���(����)
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCAnimatorDriver : MonoBehaviour
    {
        [Header("NPC Animator")]
        public Animator anim;

        [Tooltip("���ϴ� ������ ���� Bool �Ķ����")]
        public string talkBool = "Talking";
        [Tooltip("�λ� Trigger �Ķ����")]
        public string greetTrig = "Greeting";
        [Tooltip("Ȯ��(����Ȯ�� ��) Trigger �Ķ����")]
        public string confirmTrig = "Confirm";
        [Tooltip("����(�ǳ��ֱ�/������/���� ��) Trigger �Ķ����")]
        public string handoffTrig = "HandOff";

        [Header("Receipt (optional)")]
        [Tooltip("������ ������Ʈ�� Animator (���� ��Ʈ�ѷ� & Eject Ʈ���� ���)")]
        public Animator receiptAnimator;
        [Tooltip("������ ���� Animator�� Trigger �̸�")]
        public string receiptEjectTrig = "Eject";

        [Tooltip("���Ž� Animation�� ���� ��� ����(Receipt animation Ŭ�� ���)")]
        public Animation legacyReceiptAnimation;
        [Tooltip("���Ž� Animation Ŭ�� �̸� (��: \"Receipt animation\")")]
        public string legacyReceiptClipName = "Receipt animation";

        [Tooltip("������ ������Ʈ ��ü�� �Ѱ�/���� ���� ��(����)")]
        public GameObject receiptProp;

        // --- ���� API �״�� ---
        public void SetTalking(bool on) { if (anim) anim.SetBool(talkBool, on); }
        public void PlayGreeting() { if (anim) anim.SetTrigger(greetTrig); }
        public void PlayConfirm() { if (anim) anim.SetTrigger(confirmTrig); }
        public void PlayHandOff() { if (anim) anim.SetTrigger(handoffTrig); }

        /// <summary>
        /// ���� �Ϸ� �� ȣ��: NPC�� HandOff Ʈ���� + ������ ���� ����
        /// </summary>
        public void PlayPaymentComplete()
        {
            // 1) NPC ��ü �ִϸ��̼� (�ǳ��ֱ� ���� ��)
            PlayHandOff();

            // 2) ������ ����(�� �� �ϳ� �Ǵ� �� ��)
            if (receiptProp && !receiptProp.activeSelf) receiptProp.SetActive(true);

            if (receiptAnimator)
                receiptAnimator.SetTrigger(receiptEjectTrig);

            if (legacyReceiptAnimation)
            {
                // �ڵ����/���� ����
                legacyReceiptAnimation.wrapMode = WrapMode.Once;
                if (!string.IsNullOrEmpty(legacyReceiptClipName))
                    legacyReceiptAnimation.Play(legacyReceiptClipName);
                else
                    legacyReceiptAnimation.Play(); // �⺻ Ŭ��
            }
        }

        /// <summary>
        /// (����) �ִϸ��̼� �̺�Ʈ�� ȣ���� ������ ����� �� ������
        /// Receipt_Eject ������ �����ӿ� Animation Event�� �� �Լ� ���� ����
        /// </summary>
        public void OnReceiptEjectEnd()
        {
            if (receiptProp) receiptProp.SetActive(false);
        }
    }
}

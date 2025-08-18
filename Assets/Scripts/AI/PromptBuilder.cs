using System.Text;
using LifeQuest.NPCFlow.Data;

namespace LifeQuest.AI
{
    /// <summary>
    /// LLM�� �� system/user ������Ʈ ����(���Ģ �ݿ�)
    /// </summary>
    public static class PromptBuilder
    {
        public static string BuildSystemPreamble() => DialogueStepDefs.BuildSystemPreamble();

        public static string BuildUserPrompt(DialogueStepDefs.Step step, OrderState state)
        {
            // ���ø�(����) ��Ȱ��: ���� ���Ը� ª�� ������
            return DialogueStepDefs.GetUserPromptTemplate(step, state);
        }

        public static (string system, string user) Build(DialogueStepDefs.Step step, OrderState state)
        {
            return (BuildSystemPreamble(), BuildUserPrompt(step, state));
        }
    }
}

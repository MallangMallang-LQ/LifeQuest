using System.Text;
using LifeQuest.NPCFlow.Data;

namespace LifeQuest.AI
{
    /// <summary>
    /// LLM에 줄 system/user 프롬프트 구성(운영원칙 반영)
    /// </summary>
    public static class PromptBuilder
    {
        public static string BuildSystemPreamble() => DialogueStepDefs.BuildSystemPreamble();

        public static string BuildUserPrompt(DialogueStepDefs.Step step, OrderState state)
        {
            // 템플릿(폴백) 재활용: 부족 슬롯만 짧게 재질문
            return DialogueStepDefs.GetUserPromptTemplate(step, state);
        }

        public static (string system, string user) Build(DialogueStepDefs.Step step, OrderState state)
        {
            return (BuildSystemPreamble(), BuildUserPrompt(step, state));
        }
    }
}

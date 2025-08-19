using System;
using System.Collections;
using UnityEngine;

namespace LifeQuest.Utilities
{
    /// <summary>Non-MonoBehaviour에서 코루틴 실행 보조.</summary>
    public class CoroutineRunner : MonoBehaviour
    {
        static CoroutineRunner _inst;
        public static CoroutineRunner Instance
        {
            get
            {
                if (_inst) return _inst;
                var go = new GameObject("[CoroutineRunner]");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<CoroutineRunner>();
                return _inst;
            }
        }

        public static Coroutine Run(IEnumerator co) => Instance.StartCoroutine(co);
        public static Coroutine Delay(float sec, Action act) => Run(_Delay(sec, act));

        static IEnumerator _Delay(float s, Action a) { yield return new WaitForSeconds(s); a?.Invoke(); }
    }
}

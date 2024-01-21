using UnityEngine;

namespace PartOfYou.Runtime.Utils
{
    //오브젝트가 배치된 Scene에 대해서는 static하게 접근 가능한 클래스
    public class SceneAnchor<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance) return _instance;
                
                _instance = FindFirstObjectByType<T>();
                if (!_instance)
                {
                    Debug.LogError("[SceneAnchor.cs] No anchor object in the scene!");
                }

                return _instance;
            }
        }

        public virtual void Awake()
        {
            var self = GetComponent<T>();
            if (_instance == null)
            {
                _instance = self;
            }
            else if (_instance != self)
            {
                Debug.LogWarning("[SceneAnchor.cs] Multiple anchor object exists. Removing later one.");
                Destroy(gameObject);
            }
        }
    }
}
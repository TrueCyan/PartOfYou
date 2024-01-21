using UnityEngine;

namespace PartOfYou.Runtime.Logic.Actions
{
    public class ExitGame : GameAction
    {
        public override void Execute()
        {
            Application.Quit();
#if UNITY_EDITOR
            if(UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
#endif
        }
    }
}
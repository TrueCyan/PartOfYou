using PartOfYou.Runtime.Utils;

namespace PartOfYou.Runtime.Logic.System
{
    public class GameManager : SceneAnchor<GameManager>
    {
        public LevelLoadManager levelLoad;
        public TransitionManager transition;
        public SaveLoadManager SaveLoad { get; } = new();

        public override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this);
            levelLoad.Initialize();
            SaveLoad.Initialize();
            SaveLoad.LoadAllSlots();
            if (!SaveLoad.HasSave(1))
            {
                SaveLoad.CreateNewSave(1);
            }
            SaveLoad.SetSlot(1);
        }
    }
}
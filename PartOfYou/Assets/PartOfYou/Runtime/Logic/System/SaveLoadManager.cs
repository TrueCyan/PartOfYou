using System;
using System.Collections.Generic;
using System.IO;
using PartOfYou.Runtime.Logic.Level;
using UnityEngine;

namespace PartOfYou.Runtime.Logic.System
{
    public class SaveLoadManager
    {
        private readonly Dictionary<int, GameSave> _gameSaveSlots = new();
        private string _savePath;
        private readonly string _userString = "user";
        private readonly string _saveFileName = "save_{0}";
        private readonly string _backupExtension = ".bak";

        private const int SlotCount = 3;

        public int SelectedSlotId { get; private set; }

        public void Initialize()
        {
            _savePath = Application.persistentDataPath;
        }

        public void SetSlot(int slot)
        {
            SelectedSlotId = slot;
        }

        public void CreateNewSave(int slot)
        {
            _gameSaveSlots[slot] = new GameSave();
        }

        public bool HasSave(int slot)
        {
            return _gameSaveSlots.ContainsKey(slot) && _gameSaveSlots[slot] != null;
        }

        public LevelPlayInfo GetLevelPlayInfo(LevelId levelId)
        {
            return _gameSaveSlots[SelectedSlotId].GetPlayInfo(levelId);
        }

        public void ClearLevel(LevelId levelId, LevelStatistics levelStatistics, int prevClearCount = 0)
        {
            if (SelectedSlotId == 0)
            {
                Debug.LogWarning("Save slot not selected.");
                return;
            }

            var clearInfo = new LevelPlayInfo(levelId, prevClearCount + 1, levelStatistics);
            _gameSaveSlots[SelectedSlotId].AddPlayInfo(clearInfo);
            SaveData(SelectedSlotId);
        }
        
        private string GetBackupPath(string path) => path + _backupExtension;

        public void SaveData(int slot)
        {
            var json = _gameSaveSlots[slot].ToJson();
            var fileName = String.Format(_saveFileName, slot);
            var path = Path.Combine(_savePath, _userString, fileName);

            try
            {
                var directoryName = Path.GetDirectoryName(path);
                if (directoryName != null)
                {
                    Directory.CreateDirectory(directoryName);
                }
                else
                {
                    throw new Exception("Save directory creation failed.");
                }

                using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
                {
                    using (StreamWriter writer = new StreamWriter(stream)) 
                    {
                        writer.Write(json);
                    }
                }

                var verifiedGameSave = LoadData(slot);
                if (verifiedGameSave != null)
                {
                    File.Copy(path, GetBackupPath(path), true);
                }
                else
                {
                    throw new Exception("Save verification failed.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Save Failed.\n{e.Message}");
            }
        }

        public void LoadAllSlots()
        {
            for (var i = 1; i <= SlotCount; i++)
            {
                _gameSaveSlots[i] = LoadData(i);
            }
        }

        private GameSave LoadData(int slot)
        {
            var fileName = String.Format(_saveFileName, slot);
            var path = Path.Combine(_savePath, _userString, fileName);

            GameSave gameSave = null;
            try
            {
                gameSave = TryLoad(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"Load failed. Try to load from backup file...\n{e.Message}");
                
                try
                {
                    var success = AttemptRollback(path);
                    if (success)
                    {
                        gameSave = TryLoad(path);
                        Debug.Log("Load from backup success.");
                    }
                    else
                    {
                        Debug.Log("No backup file.");
                    }
                }
                catch
                {
                    Debug.LogError("Load from backup failed.");
                    var backupPath = GetBackupPath(path);
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                }
                
            }

            return gameSave;
        }

        private GameSave TryLoad(string path)
        {
            if (File.Exists(path))
            {
                string loadedString;
                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        loadedString = reader.ReadToEnd();
                    }
                }

                return GameSave.FromJson(loadedString);
            }

            return null;
        }
        
        private bool AttemptRollback(string path) 
        {
            var backupFilePath = GetBackupPath(path);
            if (!File.Exists(backupFilePath))
            {
                return false;
            }

            File.Copy(backupFilePath, path, true);
            return true;

        }
    }
}
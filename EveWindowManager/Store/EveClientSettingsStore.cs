using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace EveWindowManager.Store
{
    public class EveClientSettingsStore
    {
        private readonly string _storeFileName;
        private List<EveClientSetting> _eveClientSettings = new List<EveClientSetting>();
        private const string DefaultStoreFileName = "clientSettings.json";

        public EveClientSettingsStore(string storeFileName = DefaultStoreFileName)
        {
            _storeFileName = storeFileName;
            if(File.Exists(_storeFileName))
                LoadFromFile();
        }

        public void LoadFromFile()
        {
            var json = File.ReadAllText(_storeFileName);
            _eveClientSettings = JsonConvert.DeserializeObject<List<EveClientSetting>>(json);
        }

        public void SaveToFile()
        {
            var json = JsonConvert.SerializeObject(_eveClientSettings, Formatting.Indented);
            File.WriteAllText(_storeFileName, json);
        }

        public EveClientSetting GetSettingByWindowTitle(string windowTitle)
        {
            return _eveClientSettings.FirstOrDefault(x => x.ProcessTitle.Equals(windowTitle));
        }

        public bool IsSaved(string windowTitle)
        {
            return (GetSettingByWindowTitle(windowTitle) != null);
        }

        public void Upsert(EveClientSetting clientSetting)
        {
            //Update existing
            var existingClientSetting = _eveClientSettings.FirstOrDefault(x => x.ProcessTitle.Equals(clientSetting.ProcessTitle));
            if (existingClientSetting != null)
            {
                existingClientSetting.ProcessTitle = clientSetting.ProcessTitle;
                existingClientSetting.Height = clientSetting.Height;
                existingClientSetting.Width = clientSetting.Width;
                existingClientSetting.PositionX = clientSetting.PositionX;
                existingClientSetting.PositionY = clientSetting.PositionY;
                existingClientSetting.IsMaximized = clientSetting.IsMaximized;
            }
            //Add new
            else
            {
                _eveClientSettings.Add(clientSetting);
            }
        }

        public void DeleteByWindowTitle(string windowTitle)
        {
            _eveClientSettings.RemoveAll(x => x.ProcessTitle.Equals(windowTitle));
        }

        public void Delete(EveClientSetting clientSetting)
        {
            _eveClientSettings.Remove(clientSetting);
        }
    }
}

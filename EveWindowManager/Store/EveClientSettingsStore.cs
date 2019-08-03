using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace EveWindowManager.Store
{
    public class EveClientSettingsStore
    {
        private List<EveClientSetting> _eveClientSettings = new List<EveClientSetting>();

        public void LoadFromFile(string fileName)
        {
            if (!File.Exists(fileName)) return;
            var json = File.ReadAllText(fileName);
            _eveClientSettings = JsonConvert.DeserializeObject<List<EveClientSetting>>(json);
        }

        public void SaveToFile(string fileName)
        {
            var json = JsonConvert.SerializeObject(_eveClientSettings, Formatting.Indented);
            File.WriteAllText(fileName, json);
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

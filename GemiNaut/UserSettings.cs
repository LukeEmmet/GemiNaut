using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GemiNaut
{
    class UserSettings
    {

        //A class to manage user settings, such as bookmarks, client certificates, app settings
        //use a custom class otherwise normal .NET settings are updated for each variant build :/
        //also allows more flexibility for future upgrade behaviour etc
        //the settings are loaded on each use, so they will be effectively shared across GemiNaut instances
        //also best suited to attributes that are not chaning very frequently

        private DirectoryInfo _settingsFolder;
        private System.Xml.XmlDocument _settingsDom;
        private string _settingsPath;
        private string _bookmarksPath;


        //these are the settings that go into settings.xml of form <xml-element-name>;<default>
        private const string _homeUriAttribute = "home-uri;gemini://gemini.circumlunar.space";
        private const string _themeAttribute = "theme;Fabric (site specific themes)";
        private const string _maxDownloadSizeAttribute = "max-download-size;10";
        private const string _maxDownloadTimeAttribute = "max-download-time;10";
        private const string _httpSchemeProxyAttribute = "http-scheme-proxy;";
        private const string _webRenderModeAttribute = "web-render-mode;web-switch-simplified";
        private const string _handleWebLinks = "handle-web-links;System web browser";

        public UserSettings()
        {
            //create folder structure %APPDATA%\GemiNaut\Maj.Minor to hold the settings
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileMajorPart + "." + fvi.FileMinorPart;
            
            _settingsDom = new XmlDocument();

            //create folder if it doesnt exist
            _settingsFolder = Directory.CreateDirectory(Path.Combine(appData, fvi.ProductName + "\\" + version));

            _settingsPath = Path.Combine(_settingsFolder.FullName, "settings.xml");
            _bookmarksPath = Path.Combine(_settingsFolder.FullName, "bookmarks.gmi");



            if (!File.Exists(_settingsPath))
            {
                //**TBD could upgrade from earlier version if it exists?
                //for now just reset
                _settingsDom.LoadXml("<?xml version=\"1.0\"?>\r\n<settings/>");
                SaveSettings();
            }

            _settingsDom.Load(_settingsPath);

            if (!File.Exists(_bookmarksPath))
            {
                //**TBD could upgrade from earlier version if it exists?
                //for now just reset
                var bookmarks = "" +
                    "=> gemini://circumlunar.space Gemini Project home page\r\n" +
                    "=> gemini://gemini.marmaladefoo.com GemiNaut home page\r\n" +
                    "=> gemini://geminispace.info Gemini search engine\r\n";

                Bookmarks = bookmarks;      //this will save the content
            }



        }

        public void Save()
        {
            SaveSettings();
        }
        public void SaveSettings()
        {
            _settingsDom.Save(_settingsPath);
        }

        public void LoadSettings()
        {
            _settingsDom.Load(_settingsPath);
        }

        private string GetSetting(string settingName)
        {
            LoadSettings();
            var settingArr = settingName.Split(';');
            XmlElement settingEl = (XmlElement)_settingsDom.SelectSingleNode("//settings/" + settingArr[0]);

            return settingEl != null ? settingEl.InnerText : settingArr[1];
        }

        private void SetSetting(string settingName, string settingValue)
        {
            var settingArr = settingName.Split(';');
            XmlElement settingEl = (XmlElement)_settingsDom.SelectSingleNode("//settings/" + settingArr[0]);
            if (settingEl == null)
            {
                //create if missing
               settingEl = _settingsDom.CreateElement(settingArr[0]);
               _settingsDom.DocumentElement.AppendChild(settingEl);
            }

            settingEl.InnerText = settingValue;
            SaveSettings();
        }
        
        public string HomeUrl
        {
            get
            {
                return GetSetting(_homeUriAttribute);
            }

            set
            {
                SetSetting(_homeUriAttribute, value);
            }
        }

        public string Theme
        {
            get
            {
                return GetSetting(_themeAttribute);
            }
            set
            {
                SetSetting(_themeAttribute, value);
            }
        }

        public string WebRenderMode
        {
            get
            {
                return GetSetting(_webRenderModeAttribute);
            }
            set
            {
                SetSetting(_webRenderModeAttribute, value);
            }
        }

        public string HttpSchemeProxy
        {
            get
            {
                return GetSetting(_httpSchemeProxyAttribute);
            }
            set
            {
                SetSetting(_httpSchemeProxyAttribute, value);
            }
        }

        public string HandleWebLinks
        {
            get
            {
                return GetSetting(_handleWebLinks);
            }
            set
            {
                SetSetting(_handleWebLinks, value);
            }
        }

        //these attributes are integers
        public int MaxDownloadSizeMb
        {
            get
            {
                return int.Parse( GetSetting(_maxDownloadSizeAttribute));
            }
            set
            {
                SetSetting(_maxDownloadSizeAttribute, value.ToString());
            }
        }

        public int MaxDownloadTimeSeconds
        {
            get
            {
                return int.Parse(GetSetting(_maxDownloadTimeAttribute));
            }
            set
            {
                SetSetting(_maxDownloadTimeAttribute, value.ToString());
            }
        }

        //bookmarks live in their own GMI file
        public string Bookmarks
        {
            get
            {
                return File.ReadAllText(_bookmarksPath);
            }
            set
            {
                File.WriteAllText(_bookmarksPath, value);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Xml;
using GDax.Helpers;

namespace GDax.IoC
{
    public interface ISettingsFactory
    {
        T GetOrCreateSetting<T>(string instanceName = null);
        void SaveSettings();
    }

    public sealed class GlobalFileSettingsProvider : LocalFileSettingsProvider
    {
        private const string AppSettingsSectionGroup = "applicationSettings";
        private static readonly XmlDocument m_EscapeDoc = new XmlDocument();
        private readonly XmlElement _escapeElement = m_EscapeDoc.CreateElement("escaper");
        private string _configPath;


        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection properties)
        {
            var config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap { ExeConfigFilename = _configPath }, ConfigurationUserLevel.None);
            var sectionName = GetSectionName(context);

            var valueCollection = new SettingsPropertyValueCollection();
            var settings = GetConfigSection(config, sectionName).Settings;
            foreach (SettingsProperty property in properties)
            {
                var settingValue = new SettingsPropertyValue(property);
                var setting = settings.Get(property.Name);
                if (setting != null)
                {
                    var value = setting.Value.ValueXml.InnerXml;
                    if (setting.SerializeAs == SettingsSerializeAs.String)
                    {
                        value = UnEscapeXml(value);
                    }
                    settingValue.SerializedValue = value;
                }
                else settingValue.PropertyValue = null;
                settingValue.IsDirty = false;
                valueCollection.Add(settingValue);
            }

            return valueCollection;
        }

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection values)
        {
            var config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap { ExeConfigFilename = _configPath }, ConfigurationUserLevel.None);
            var sectionName = GetSectionName(context);

            var settings = GetConfigSection(config, sectionName).Settings;
            foreach (SettingsPropertyValue value in values)
            {
                var property = value.Property;
                var setting = settings.Get(property.Name);
                if (setting == null)
                {
                    setting = new SettingElement { Name = property.Name};
                    settings.Add(setting);
                }

                setting.SerializeAs = property.SerializeAs;
                setting.Value.ValueXml = SerializeToXmlElement(property, value);
            }

            config.Save();
        }

        public override void Initialize(string name, NameValueCollection values)
        {
            if (string.IsNullOrEmpty(name))
                name = nameof(GlobalFileSettingsProvider);
            base.Initialize(name, values);

            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppInfo.GetCompanyName(), AppInfo.GetEntryAssemblyName(), "app.config");
        }

        private string GetSectionName(SettingsContext context)
        {
            var text = (string)context["GroupName"];
            var text2 = (string)context["SettingsKey"];
            var text3 = text;
            if (!string.IsNullOrEmpty(text2))
            {
                text3 = $"{text3}.{text2}";
            }
            return XmlConvert.EncodeLocalName(text3);
        }

        private ClientSettingsSection GetConfigSection(Configuration config, string sectionName)
        {
            var sectionPath = $"{AppSettingsSectionGroup}/{sectionName}";
            if (config.GetSection(sectionPath) == null)
            {
                if (config.GetSectionGroup(AppSettingsSectionGroup) == null)
                {
                    config.SectionGroups.Add(AppSettingsSectionGroup, new ConfigurationSectionGroup());
                }

                var sectionGroup = config.GetSectionGroup(AppSettingsSectionGroup);
                if (sectionGroup != null && sectionGroup.Sections[sectionName] == null)
                {
                    var section = new ClientSettingsSection();
                    section.SectionInformation.AllowExeDefinition = ConfigurationAllowExeDefinition.MachineToApplication;
                    section.SectionInformation.RequirePermission = false;
                    sectionGroup.Sections.Add(sectionName, section);
                }
            }

            return config.GetSection(sectionPath) as ClientSettingsSection;
        }

        private XmlNode SerializeToXmlElement(SettingsProperty property, SettingsPropertyValue value)
        {
            var document = new XmlDocument();
            var element = document.CreateElement("value");
            var valueXml = value.SerializedValue as string;
            if (valueXml == null && property.SerializeAs == SettingsSerializeAs.Binary)
            {
                var array = value.SerializedValue as byte[];
                if (array != null)
                    valueXml = Convert.ToBase64String(array);
            }

            if (valueXml == null)
                valueXml = string.Empty;

            if (property.SerializeAs == SettingsSerializeAs.String)
                valueXml = EscapeXml(valueXml);

            element.InnerXml = valueXml;
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.XmlDeclaration) continue;

                element.RemoveChild(node);
                break;
            }

            return element;
        }

        private string EscapeXml(string xmlString)
        {
            if (string.IsNullOrWhiteSpace(xmlString)) return xmlString;

            _escapeElement.InnerText = xmlString;
            return _escapeElement.InnerXml;
        }

        private string UnEscapeXml(string escapedString)
        {
            if (string.IsNullOrWhiteSpace(escapedString)) return escapedString;

            _escapeElement.InnerXml = escapedString;
            return _escapeElement.InnerText;
        }
    }

    public sealed class SettingsFactory : ISettingsFactory
    {
        private readonly SettingsBase _settings;
        private readonly Dictionary<string, PropertyChangedEventHandler> _handlers = new Dictionary<string, PropertyChangedEventHandler>();

        public SettingsFactory()
        {
            _settings = SettingsBase.Synchronized((SettingsBase)Activator.CreateInstance(SettingsTypeGenerator.GetApplicationSettingsType()));
        }

        public T GetOrCreateSetting<T>(string instanceName)
        {
            var type = SettingsTypeGenerator.GetImplementationType(typeof(T));
            var key = $"{type.Name}{instanceName}";
            if (_settings.Properties[key] == null)
                CreateSettingsProperty(type, key);

            if (_settings[key] == null)
                _settings[key] = Activator.CreateInstance(type);

            var setting = (T)_settings[key];
            if (!_handlers.ContainsKey(key))
            {
                _handlers.Add(key, (sender, e) => OnSettingsChanged(sender, key));
                ((INotifyPropertyChanged)setting).PropertyChanged += _handlers[key];
            }

            return setting;
        }

        public void SaveSettings()
        {
            _settings.Save();
        }

        private void CreateSettingsProperty(Type settingType, string key)
        {
            var property = new SettingsProperty(key)
            {
                PropertyType = settingType,
                Provider = _settings.Providers[nameof(GlobalFileSettingsProvider)],
                SerializeAs = SettingsSerializeAs.Xml,
                ThrowOnErrorDeserializing = true,
                ThrowOnErrorSerializing = true
            };
            property.Attributes.Add(typeof(ApplicationScopedSettingAttribute), new ApplicationScopedSettingAttribute());
            _settings.Properties.Add(property);
        }

        private void OnSettingsChanged(object sender, string settingsKey)
        {
            _settings[settingsKey] = sender;
        }
    }
}

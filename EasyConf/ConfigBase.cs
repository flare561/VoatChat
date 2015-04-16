using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Configuration;

namespace EasyConf
{
    public abstract class ConfigBase
    {
        [AttributeUsage(AttributeTargets.Property)]
        protected class InitialProp : Attribute
        {
        }

        private List<PropertyInfo> GetInitialProperties()
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();
            var props = this.GetType().GetProperties();
            foreach (var prop in props)
            {
                if (prop.CustomAttributes.Count(x => x.AttributeType == typeof(InitialProp)) > 0)
                    properties.Add(prop);
            }
            return properties;
        }

        public void SetInitialProperties()
        {
            foreach (var prop in this.GetInitialProperties())
            {
                if (prop.PropertyType == typeof(string))
                {
                    Console.Write("Input a value for {0}: ", prop.Name);
                    prop.SetValue(this, Console.ReadLine());
                }
            }
            this.SaveChanges();
        }

        public void SaveChanges()
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            foreach (var prop in this.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(string))
                {
                    if (settings[prop.Name] == null)
                        settings.Add(prop.Name, (string)prop.GetValue(this));
                    else
                        settings[prop.Name].Value = (string)prop.GetValue(this);
                }
            }
            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
        }

        public bool LoadConfig()
        {
            bool propSet = false;
            foreach (var prop in this.GetType().GetProperties())
            {
                if (ConfigurationManager.AppSettings[prop.Name] != null && prop.PropertyType == typeof(string))
                {
                    propSet = true;
                    prop.SetValue(this, (string)ConfigurationManager.AppSettings[prop.Name]);
                }
            }
            return propSet;
        }
    }
}

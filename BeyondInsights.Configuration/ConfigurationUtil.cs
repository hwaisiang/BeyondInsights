using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BeyondInsights.Configuration
{
    public static class ConfigurationUtil
    {
        public static FundamentalRuleConfiguration FundamentalRuleConfiguration
        {
            get
            {
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();

                fileMap.ExeConfigFilename = GetPath();

                System.Configuration.Configuration config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

                return FundamentalRuleConfiguration.GetConfig(config);

            }
        }

        public static System.Configuration.Configuration CurrentConfiguration
        {
            get 
            {
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();

                fileMap.ExeConfigFilename = GetPath();

                System.Configuration.Configuration config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

                return config;
            }
        }

        public static string GetPath()
        {
            string path = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            path = Path.Combine(path, "BeyondInsights");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path = Path.Combine(path, "BeyondInsights.Configuration.dll.config");

            if (!File.Exists(path))
            {
                //create the file
                 var assembly = Assembly.GetExecutingAssembly();
                 using (var stream = assembly.GetManifestResourceStream("BeyondInsights.Configuration.BeyondInsights.Configuration.dll.config"))
                 using (var reader = new StreamReader(stream))
                 {
                     using (StreamWriter writer = new StreamWriter(path))
                     {
                         writer.Write(reader.ReadToEnd());
                         writer.Flush();
                     }
                 }
            }
            return path;
        }
    }
}

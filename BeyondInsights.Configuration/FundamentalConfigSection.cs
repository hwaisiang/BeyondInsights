using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyondInsights.Configuration
{
    public class FundamentalRuleConfiguration : System.Configuration.ConfigurationSection
    {
        private static string sConfigurationSectionConst = "fundamentalRuleConfiguration";

        /// <summary>
        /// Returns an shiConfiguration instance
        /// </summary>
        public static FundamentalRuleConfiguration GetConfig(System.Configuration.Configuration config)
        {
            return (FundamentalRuleConfiguration)config.
               GetSection(FundamentalRuleConfiguration.sConfigurationSectionConst) ??
               new FundamentalRuleConfiguration();

        }
        [System.Configuration.ConfigurationProperty("fundamentalRules")]
        public FundamentalRuleCollection FundamentalRuleSettings
        {
            get
            {
                return (FundamentalRuleCollection)this["fundamentalRules"] ??
                   new FundamentalRuleCollection();
            }
        }
    }

    public class FundamentalRuleConfig: ConfigurationElement
    {
        [ConfigurationProperty("name",IsRequired =true)]
        public string Name
        {
            get
            {
                return this["name"] as string;
            }
        }

        [System.Configuration.ConfigurationProperty("Values")]
        public FundamentalRuleValueCollection RulesValues
        {
            get
            {
                return (FundamentalRuleValueCollection)this["Values"] ??
                   new FundamentalRuleValueCollection();
            }
        }

        public FundamentalRuleValue GetSelectedRule()
        {
            foreach (FundamentalRuleValue v in RulesValues)
            {
                if (v.IsSelected)
                    return v;
            }
            return null;
        }
    }

    public class FundamentalRuleCollection : ConfigurationElementCollection
    {
        public FundamentalRuleConfig this[int index]
        {
            get
            {
                return base.BaseGet(index) as FundamentalRuleConfig;
            }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }
        protected
            override System.Configuration.ConfigurationElement CreateNewElement()
        {
            return new FundamentalRuleConfig();
        }

        protected override object GetElementKey(
            System.Configuration.ConfigurationElement element)
        {
            return ((FundamentalRuleConfig)element).Name;
        }
    }

    public class FundamentalRuleValue : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return this["name"] as string; }
        }

        [ConfigurationProperty("value", IsRequired = true)]
        public string Value
        {
            get { return this["value"] as string; }
        }

        [ConfigurationProperty("isSelected", IsRequired = true)]
        public bool IsSelected
        {
            get { return (bool)this["isSelected"]; }
        }

    }

    public class FundamentalRuleValueCollection : ConfigurationElementCollection
    {
        public FundamentalRuleValue this[int index]
        {
            get
            {
                return base.BaseGet(index) as FundamentalRuleValue;
            }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }
        protected
            override System.Configuration.ConfigurationElement CreateNewElement()
        {
            return new FundamentalRuleValue();
        }

        protected override object GetElementKey(
            System.Configuration.ConfigurationElement element)
        {
            return ((FundamentalRuleValue)element).Value;
        }
    }
}

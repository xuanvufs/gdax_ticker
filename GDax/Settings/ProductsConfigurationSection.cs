using GDax.Enums;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDax.Settings
{
    public class ProductsConfigurationSection : ConfigurationSection
    {
        // Create a "remoteOnly" attribute.
        [ConfigurationProperty("base", IsRequired = true)]
        public Currency RemoteOnly
        {
            get
            {
                return (Currency)this["base"];
            }
            set
            {
                this["base"] = value;
            }
        }

        // Create a "font" element.
        [ConfigurationProperty("target", IsRequired = true)]
        public Currency Font
        {
            get
            {
                return (Currency)this["font"];
            }
            set
            { this["font"] = value; }
        }
    }

    // Define the UrlsCollection that contains the 
    // UrlsConfigElement elements.
    // This class shows how to use the ConfigurationElementCollection.
    public class CurrencyPairsCollection : ConfigurationElementCollection
    {
        public CurrencyPairsCollection()
        {

        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new CurrencyPairElement();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((CurrencyPairElement)element).Name;
        }

        public CurrencyPairElement this[int index]
        {
            get
            {
                return (CurrencyPairElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        new public CurrencyPairElement this[string Name]
        {
            get
            {
                return (CurrencyPairElement)BaseGet(Name);
            }
        }


        public int IndexOf(CurrencyPairElement url)
        {
            return BaseIndexOf(url);
        }

        public void Add(CurrencyPairElement url)
        {
            BaseAdd(url);

            // Your custom code goes here.

        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);

            // Your custom code goes here.

        }

        public void Remove(CurrencyPairElement url)
        {
            if (BaseIndexOf(url) >= 0)
            {
                BaseRemove(url.Name);
                // Your custom code goes here.
                Console.WriteLine("UrlsCollection: {0}", "Removed collection element!");
            }
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);

            // Your custom code goes here.

        }

        public void Remove(string name)
        {
            BaseRemove(name);

            // Your custom code goes here.

        }

        public void Clear()
        {
            BaseClear();

            // Your custom code goes here.
            Console.WriteLine("UrlsCollection: {0}", "Removed entire collection!");
        }

    }

    // Define the "color" element 
    // with "background" and "foreground" attributes.
    public class CurrencyPairElement : ConfigurationElement
    {
        public CurrencyPairElement() { }

        public CurrencyPairElement(Currency baseCurrency, Currency targetCurrency)
        {
            Base = baseCurrency;
            Target = targetCurrency;
        }

        [ConfigurationProperty("base", IsRequired = true)]
        [StringValidator(MinLength = 3, MaxLength = 4)]
        public Currency Base
        {
            get
            {
                return (Currency)this["base"];
                //return (Currency)Enum.Parse(typeof(Currency), (string)this["base"]);
            }
            set
            {
                this["base"] = value;
            }
        }

        [ConfigurationProperty("target", IsRequired = true)]
        [StringValidator(MinLength = 3, MaxLength = 4)]
        public Currency Target
        {
            get
            {
                return (Currency)this["target"];
                //return (Currency)Enum.Parse(typeof(Currency), (string)this["target"]);
            }
            set
            {
                this["target"] = value;
            }
        }

    }
}

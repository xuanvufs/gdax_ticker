using GDax.Enums;
using GDax.Helpers;
using GDax.Models;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;

namespace GDax.Settings
{
    /// <summary>
    /// Defines the Products configuration in the application configuration.
    /// </summary>
    public class ProductConfigurationSection : ConfigurationSection
    {
        /// <summary>
        /// The name of this section in the app.config.
        /// </summary>
        public const string SectionName = "Products";

        private const string CurrencyPairsCollectionName = "CurrencyPairs";

        /// <summary>
        /// Maps to the CurrencyPair collections.
        /// </summary>
        [ConfigurationProperty(CurrencyPairsCollectionName)]
        [ConfigurationCollection(typeof(CurrencyPairCollection), AddItemName = "add")]
        public CurrencyPairCollection CurrencyPairCollection { get { return (CurrencyPairCollection)base[CurrencyPairsCollectionName]; } }
    }

    // Define the CurrencyPairCollection that contains the
    // currency pairs to add as widgets
    public class CurrencyPairCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new CurrencyPairElement();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((CurrencyPairElement)element).CurrencyPair.ProductId;
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            var pair = (CurrencyPairElement)element;

            if (pair.Base == pair.Target) throw new ConfigurationErrorsException($"Currency pair cannot have the same base and target currency. Base: [{pair.Base}], Target: [{pair.Target}]");

            BaseAdd(element, true);
        }
    }

    /// <summary>
    /// Defines the Currency pair element used to register a currency pair.
    /// </summary>
    public class CurrencyPairElement : ConfigurationElement
    {
        public CurrencyPairElement()
        {
        }

        [ConfigurationProperty("base", IsRequired = true)]
        [TypeConverter(typeof(CurrencyConverter))]
        [ConfigurationValidator(typeof(CurrencyValidator))]
        public Currency Base
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

        [ConfigurationProperty("target", IsRequired = true)]
        [TypeConverter(typeof(CurrencyConverter))]
        [ConfigurationValidator(typeof(CurrencyValidator))]
        public Currency Target
        {
            get
            {
                return (Currency)this["target"];
            }
            set
            {
                this["target"] = value;
            }
        }

        public CurrencyPair CurrencyPair
        {
            get
            {
                return new CurrencyPair(Base, Target);
            }
        }
    }

    /// <summary>
    /// A currency validator that ensures the configuration is valid.
    /// </summary>
    public class CurrencyValidator : ConfigurationValidatorBase
    {
        public override bool CanValidate(Type type)
        {
            return type == typeof(Currency);
        }

        public override void Validate(object value)
        {
            if (value == null) throw new ArgumentNullException("value");

            var currency = (Currency)Enum.Parse(typeof(Currency), value.ToString());

            if (!Enum.IsDefined(typeof(Currency), currency))
            {
                throw new ConfigurationErrorsException($"{value} is not a defined Currency.");
            }
        }
    }

    /// <summary>
    /// Converts the configuration currency value the currency enum.
    /// </summary>
    public class CurrencyConverter : ConfigurationConverterBase
    {
        private Type _type = typeof(Currency);

        public override bool CanConvertTo(ITypeDescriptorContext ctx, Type type)
        {
            return (type == _type);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext ctx, Type type)
        {
            return (type == _type);
        }

        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            if (Enum.IsDefined(_type, value))
            {
                return value.ToString();
            }

            return "";
        }

        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
        {
            var currencyStr = (string)data;

            return Utils.ParseCurrency(currencyStr);
        }
    }
}
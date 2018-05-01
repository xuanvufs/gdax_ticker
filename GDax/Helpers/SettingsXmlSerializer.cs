using System;
using System.Reflection;
using System.Security;
using System.Xml;
using System.Xml.Serialization;

namespace GDax.Helpers
{
    public static class SettingsXmlSerializer
    {
        public static void SerializeSettings(XmlWriter writer, IXmlSerializable settings)
        {
            var type = settings.GetType();
            var typeConvertible = typeof(IConvertible);
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;
                var value = property.GetValue(settings);
                if (value != null)
                {
                    string valueString = null;
                    if (propertyType.IsPrimitive || propertyType == typeof(string) || property.PropertyType == typeof(Guid))
                        valueString = Convert.ToString(value);
                    else if (propertyType == typeof(SecureString))
                        valueString = Convert.ToBase64String(Native.GetEncryptedData((SecureString)value));
                    else if (typeConvertible.IsAssignableFrom(propertyType))
                        valueString = (string)Convert.ChangeType(value, typeof(string));

                    if (valueString != null)
                        writer.WriteElementString(property.Name, valueString);
                }
            }
        }

        public static void DeserializeSettings(XmlReader reader, IXmlSerializable settings)
        {
            var type = settings.GetType();
            var typeConvertible = typeof(IConvertible);

            if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == type.Name)
            {
                reader.Read();
                while (reader.NodeType == XmlNodeType.Element)
                {
                    var propertyName = reader.LocalName;
                    var property = type.GetProperty(propertyName);
                    var serializedValue = reader.ReadElementContentAsString();
                    if (property != null && !string.IsNullOrWhiteSpace(serializedValue))
                    {
                        var propertyType = property.PropertyType;
                        object value = null;
                        try
                        {
                            if (propertyType.IsPrimitive || propertyType.IsValueType && typeConvertible.IsAssignableFrom(propertyType))
                                value = Convert.ChangeType(serializedValue, propertyType);
                            else if (propertyType == typeof(string))
                                value = serializedValue;
                            else if (propertyType == typeof(Guid))
                                value = Guid.Parse(serializedValue);
                            else if (propertyType == typeof(SecureString))
                                value = Native.GetSecureString(Convert.FromBase64String(serializedValue));

                            if (value != null)
                                property.SetValue(settings, value);
                        }
                        finally
                        {
                            (value as SecureString)?.Dispose();
                        }
                    }
                }
            }
        }
    }
}
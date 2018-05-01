using System;

namespace GDax.Attributes
{
    public class IconAttribute : Attribute
    {
        public IconAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; }
    }
}
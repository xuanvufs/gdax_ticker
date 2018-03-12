using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

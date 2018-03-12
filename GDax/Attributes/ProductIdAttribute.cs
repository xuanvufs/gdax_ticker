using System;

namespace GDax.Attributes
{
    public class ProductIdAttribute : Attribute
    {
        public string ProductId { get; }

        public ProductIdAttribute(string productId)
        {
            ProductId = productId;
        }
    }
}

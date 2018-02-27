using System;

namespace GDax
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

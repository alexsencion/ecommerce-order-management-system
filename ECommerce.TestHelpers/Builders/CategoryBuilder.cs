using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.TestHelpers.Builders
{
    public class CategoryBuilder
    {
        private string _name = "Electronics";
        private string _slug = "electronics";

        public CategoryBuilder WithName(string v) { _name = v; return this; }
        public CategoryBuilder WithSlug(string v) { _slug = v; return this; }

        public Category Build() => new() { Name = _name, Slug = _slug };
    }
}

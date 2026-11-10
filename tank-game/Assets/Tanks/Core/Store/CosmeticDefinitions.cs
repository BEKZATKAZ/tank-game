using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tanks.Core.Store
{
    public struct CosmeticDefinitions
    {
        public Dictionary<string, CosmeticDefinition> Items;
        public Dictionary<string, DiscountDefinition> Discounts;
    }
}

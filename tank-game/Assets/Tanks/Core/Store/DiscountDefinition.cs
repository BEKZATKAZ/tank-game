using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tanks.Core.Store
{
    public struct DiscountDefinition
    {
        public int PercentOff;
        public DateTimeOffset? EndDate;

        public bool IsActive(DateTimeOffset now)
        {
            return PercentOff is >= 0 and <= 100 && (EndDate == null || EndDate > now);
        }

        public int Apply(int price, DateTimeOffset now)
        {
            return IsActive(now) ? (int)(((long)price * (100 - PercentOff) + 99) / 100)
                : price;
        }
    }
}

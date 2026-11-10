using System;

namespace Backend.Models.Store;

public readonly record struct DiscountDefinition(int PercentOff, DateTimeOffset? EndDate);

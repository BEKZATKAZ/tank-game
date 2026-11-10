using System.Collections.Generic;

namespace Backend.Models.Store;

public readonly record struct UnlockConditions(Dictionary<string, int>? Prices);

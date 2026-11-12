using Backend.Services;
using Backend.Services.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace Backend.Modules;

public sealed class ModuleSetup : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.AddGameApiClient();

        config.Dependencies
            .AddSingleton<IDebriefingCalculationService,
                DebriefingCalculationService>();

        config.Dependencies
            .AddSingleton<IPlayerDebriefingService, PlayerDebriefingService>();

        config.Dependencies.AddSingleton<ICloudSaveService, CloudSaveService>();

        config.Dependencies.AddSingleton<IRemoteConfigService,
            RemoveConfigService>();
    }
}

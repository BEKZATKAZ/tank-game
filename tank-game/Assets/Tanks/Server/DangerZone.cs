using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server
{
    public sealed class DangerZone :
        NetworkComponent<IDangerZoneBehaviour>,
        IMessageHandler<TilemapGenerated>,
        IMessageHandler<GameStartedFromServer>
    {
        public DangerZone(
            IEntityService entityService,
            IMessageBus bus,
            ISessionSettingsService sessionSettings,
            IDamageService damageService)
        {
            _entityService = entityService;
            _bus = bus;
            _sessionSettings = sessionSettings;
            _damageService = damageService;
        }

        private readonly IEntityService _entityService;
        private readonly IMessageBus _bus;
        private readonly ISessionSettingsService _sessionSettings;
        private readonly IDamageService _damageService;

        private float2 _currentSize;
        private float2 _maxSize;
        private float _timeUntilStart;
        private bool _canSimulate;

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            _canSimulate = false;
            _maxSize = ((float3)message.Bounds.size).xy;
            _currentSize = _maxSize + 2; // Add a bit of offset
        }

        public void Handle(GameStartedFromServer message, IMessageSender sender)
        {
            DangerZoneSettings settings = _sessionSettings.GetSettings().DangerZone;
            _canSimulate = true;

            _timeUntilStart = settings.StartDelaySeconds + message.StartDelaySeconds;
            Behaviour.SetCurrentState(Rect.MinMaxRect(-10000, -10000, 10000, 10000));
        }

        protected override void OnInitialize()
        {
            _bus.RegisterHandler<TilemapGenerated>(this);
            _bus.RegisterHandler<GameStartedFromServer>(this);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<TilemapGenerated>(this);
            _bus.UnregisterHandler<GameStartedFromServer>(this);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            if (_canSimulate == false) return;

            if (_timeUntilStart > 0)
            {
                _timeUntilStart -= tickDelta;

                if (_timeUntilStart > 0) return;
                
                Behaviour.StartShrinkingClientRpc(new DangerZoneStarted
                {
                    ServerTick = tick,
                    StartSize = _currentSize
                });
            }

            DangerZoneSettings settings = _sessionSettings.GetSettings().DangerZone;

            _currentSize -= tickDelta * _maxSize / settings.DurationSeconds;
            _currentSize = math.max(_currentSize, float2.zero);

            int tickRate = (int)math.ceil(1 / tickDelta);
            if (tick % tickRate != 0) return;

            float2 difference = _maxSize - _currentSize;
            float2 min = difference * 0.5f;
            float2 max = _maxSize - difference * 0.5f;

            Rect rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            Behaviour.SetCurrentState(rect);

            foreach (IEntityBehaviour entity in _entityService.GetAllEntities())
            {
                float2 center = entity.Transform.WorldPosition;

                if (rect.Overlaps(new Rect(center.x, center.y, 0.01f, 0.01f))) continue;

                HealthEventId eventId = HealthEventId
                    .Create(typeof(IDangerZoneBehaviour), tick);

                // Don't count stats for pure damage
                _damageService.Damage(
                    settings.DamagePerSecond, entity, null, eventId, includeStats: false);
            }
        }
    }
}

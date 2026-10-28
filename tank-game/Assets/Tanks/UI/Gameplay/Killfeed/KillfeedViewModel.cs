using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Tanks.Utils;

namespace Tanks.UI.Gameplay.Killfeed
{
    public sealed class KillfeedEntry
    {
        public KillfeedEntry(string from, string target)
        {
            From = from;
            Target = target;
        }

        public string From { get; }
        public string Target { get; }
    }

    public sealed class KillfeedViewModel : IViewModel, IMessageHandler<Died>, IDisposable
    {
        public KillfeedViewModel(IMessageBus bus)
        {
            _bus = bus;
            _bus.RegisterHandler<Died>(this);
        }

        public BindableProperty<IReadOnlyList<KillfeedEntry>> Entries { get; } =
            new(Array.Empty<KillfeedEntry>());

        private readonly IMessageBus _bus;
        private readonly List<(KillfeedEntry Entry, float RemainingSeconds)> _entries = new();

        public void Dispose()
        {
            _bus.UnregisterHandler<Died>(this);
        }

        public void Handle(Died message, IMessageSender sender)
        {
            var from = message.From?.GetBehaviour<ITeamParticipantBehaviour>();
            var target = message.Target?.GetBehaviour<ITeamParticipantBehaviour>();

            if (target is null) return;

            string firstName = (from ?? target).State?.DisplayName ?? "";
            string secondName = from != null && target != null ? target.State?.DisplayName : null;

            var assistant = message.Assistant?.GetBehaviour<ITeamParticipantBehaviour>();

            if (from?.State != null && target.State != null && assistant?.State != null &&
                from.State.PlayerId != target.State.PlayerId &&
                assistant.State.PlayerId != from.State.PlayerId &&
                assistant.State.PlayerId != target.State.PlayerId)
            {
                firstName += $" + {assistant.State.DisplayName}";
            }

            _entries.Insert(0, (new KillfeedEntry(firstName, secondName), 5));
            if (_entries.Count > 5) _entries.RemoveAt(_entries.Count - 1);

            RefreshEntries();
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0) return;
            bool changed = false;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                entry.RemainingSeconds -= deltaTime;

                if (entry.RemainingSeconds > 0)
                {
                    _entries[i] = entry;
                    continue;
                }

                _entries.RemoveAt(i);
                changed = true;
            }

            if (changed) RefreshEntries();
        }

        private void RefreshEntries()
        {
            Entries.Value = _entries.Select(x => x.Entry).ToArray();
        }
    }
}

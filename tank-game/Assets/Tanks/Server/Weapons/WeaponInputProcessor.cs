using System;
using Tanks.Shared.Weapons;

namespace Tanks.Server.Weapons
{
    public sealed class WeaponInputProcessor
    {
        public void Process(
            WeaponInput input, int lastProcessedTick, Action<ClientShot> callback)
        {
            for (int i = 0; i < input.FireBuffer.Length; i++)
            {
                ClientShot shot = input.FireBuffer[i];

                if (shot.Id == default) continue;
                if (lastProcessedTick >= shot.ClientTick) continue;

                callback.Invoke(shot);
            }
        }
    }
}

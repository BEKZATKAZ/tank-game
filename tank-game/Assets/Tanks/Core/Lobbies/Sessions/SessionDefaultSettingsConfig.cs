using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Core.Lobbies.Sessions
{
    [Serializable]
    public struct PossibleIntegers
    {
        public PossibleIntegers(params int[] values)
        {
            Values = values;
            Default = Values.FirstOrDefault();
        }

        public int[] Values;
        public int Default;

        public readonly T GetDefaultAs<T>() where T : Enum
        {
            int d = Default;
            int index = Array.FindIndex(Values, x => x == d);
            return (T)Enum.ToObject(typeof(T), math.max(index, 0));
        }

        public readonly int CorrectValue(int original)
        {
            if (Values.Contains(original)) return original;
            return Default;
        }

        public readonly int GetValueFromValue<T>(T e) where T : Enum
        {
            int index = Convert.ToInt32(e);
            if (index >= 0 && index < Values.Length) return Values[index];
            return Default;
        }
    }

    [Serializable]
    public struct PossibleVectorIntegers
    {
        public PossibleVectorIntegers(params Vector2Int[] values)
        {
            Values = values;
            Default = Values.FirstOrDefault();
        }

        public Vector2Int[] Values;
        public Vector2Int Default;

        public readonly T GetDefaultAs<T>() where T : Enum
        {
            Vector2Int d = Default;
            int index = Array.FindIndex(Values, x => x == d);
            return (T)Enum.ToObject(typeof(T), math.max(index, 0));
        }

        public readonly Vector2Int CorrectValue(Vector2Int original)
        {
            if (Values.Contains(original)) return original;
            return Default;
        }

        public readonly Vector2Int GetValueFromEnum<T>(T e) where T : Enum
        {
            int index = Convert.ToInt32(e);
            if (index >= 0 && index < Values.Length) return Values[index];
            return Default;
        }
    }

    [Serializable]
    public class DefaultPlayerStats
    {
        public PossibleIntegers ForwardMovementSpeed = new(8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        public PossibleIntegers BackwardsMovementSpeed = new(8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
    }

    [Serializable]
    public class DefaultMapGeneration
    {
        public PossibleVectorIntegers Resolution = new(new(3, 2), new(6, 4), new(10, 8));
        public PossibleIntegers WallWidth = new(3, 5, 7);
    }

    [Serializable]
    public class DefaultItemSpawn
    {
        public PossibleIntegers CooldownSeconds = new(30, 12, 6, 2);
        public int MinDistanceToPlayers = 2;
    }

    [Serializable]
    public class DefaultDangerZone
    {
        public PossibleIntegers StartDelaySeconds = new(5, 15, 30, 45, 60) { Default = 15 };
        public PossibleIntegers DurationSeconds = new(10, 20, 30, 40, 50, 60) { Default = 40 };
        public PossibleIntegers DamagePerSecond = new(5, 10, 20, 50, 100) { Default = 10 };
    }

    [CreateAssetMenu(fileName = "SessionConfig", menuName = "Scriptable Objects/Session")]
    public sealed class SessionDefaultSettingsConfig :
        ScriptableObject, ISessionDefaultSettingsConfig
    {
        [SerializeField] private PossibleIntegers _desiredPlayerCount = new(1, 2, 3, 4, 5, 6, 7, 8);
        [SerializeField] private PossibleIntegers _maxScoreToReach = new(5, 15, 30, 45, 60, 75, 90);
        [SerializeField] private PossibleIntegers _selfDamagePercent = new(5, 15, 25, 50, 75, 100, 125, 150, 175, 200, 300, 400, 500) { Default = 100 };
        [SerializeField] private DefaultPlayerStats _stats;
        [SerializeField] private DefaultMapGeneration _mapGeneration;
        [SerializeField] private DefaultItemSpawn _itemSpawn;
        [SerializeField] private DefaultDangerZone _dangerZone;

        public PossibleIntegers DesiredPlayerCount => _desiredPlayerCount;
        public PossibleIntegers MaxScoreToReach => _maxScoreToReach;
        public PossibleIntegers SelfDamagePercent => _selfDamagePercent;
        public DefaultPlayerStats Stats => _stats;
        public DefaultMapGeneration MapGeneration => _mapGeneration;
        public DefaultItemSpawn ItemSpawn => _itemSpawn;
        public DefaultDangerZone DangerZone => _dangerZone;
    }
}

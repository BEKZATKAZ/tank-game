using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;

namespace Tanks.Shared.Tests.Items
{
    public abstract class ItemBehaviourTestBase<T> where T : ItemBehaviourBase
    {
        public T Behaviour { get; private set; }
        public IItemOwnerBehaviour MockOwner { get; private set; }
        public IItemConfig Config { get; private set; }

        private Dictionary<string, ItemStat> _stats;
        private Dictionary<StatType, PlayerStat> _playerStats;

        [SetUp]
        public virtual void Setup()
        {
            _stats = new Dictionary<string, ItemStat>();
            _playerStats = new Dictionary<StatType, PlayerStat>();

            MockOwner = Substitute.For<IItemOwnerBehaviour>();
            Config = Substitute.For<IItemConfig>();

            Config.ItemStats.Returns(_stats);
            Config.PlayerStats.Returns(_playerStats);
            Config.GetStat(Arg.Any<string>()).Returns(args => Config.ItemStats[(string)args[0]]);
            
            InitializeConfig();

            Behaviour = CreateBehaviour();
            Behaviour.Initialize(Config);
        }

        protected virtual T CreateBehaviour() => Activator.CreateInstance<T>();
        protected virtual void InitializeConfig() { }

        protected void AddItemStat(string name, ItemStat stat)
        {
            _stats.Add(name, stat);
        }

        protected void AddPlayerStat(StatType type, PlayerStat stat)
        {
            _playerStats.Add(type, stat);
        }
    }
}

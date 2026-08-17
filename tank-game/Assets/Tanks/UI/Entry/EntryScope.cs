using Tanks.Localization;
using Tanks.UI.Entry.AccountSetup;
using Tanks.UI.Menus.Dialogs;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.UI.Entry
{
    public sealed class EntryScope : LifetimeScope
    {
        [SerializeField] private DialogScreenService _dialogScreenService;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_dialogScreenService).As<IDialogScreenService>();
            builder.RegisterLocalization(Lifetime.Singleton);

            builder.Register<DialogScreenViewModel>(Lifetime.Transient);
            builder.Register<AccountSetupViewModel>(Lifetime.Transient);
        }
    }
}
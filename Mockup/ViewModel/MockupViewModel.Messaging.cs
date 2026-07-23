// ======================================================================================
// FILE: Mockup.ViewModel/MockupViewModel.Messaging.cs
// MO44 – Persistenz: Designer -> ViewModel (MoveBand)
// ======================================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Mockup.Messages;

namespace Mockup.ViewModel;

public partial class MockupViewModel : ObservableObject
{
    private void RegisterMessages()
    {
        WeakReferenceMessenger.Default.Unregister<MoveBandMessage>(this);

        WeakReferenceMessenger.Default.Register<MoveBandMessage>(
            this,
            (_, msg) => HandleMoveBand(msg));
    }

}

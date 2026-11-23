using Prototype_Final_Project.Views;

namespace Prototype_Final_Project;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register detail and modal navigation routes (Section 4)
        Routing.RegisterRoute(nameof(WishDetailPage), typeof(WishDetailPage));
    }
}

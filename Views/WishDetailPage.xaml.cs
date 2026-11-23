using Prototype_Final_Project.ViewModels;

namespace Prototype_Final_Project.Views;

/// <summary>
/// Code-behind for WishDetailPage.
/// Injects WishDetailViewModel and establishes the presentation BindingContext.
/// </summary>
public partial class WishDetailPage : ContentPage
{
    public WishDetailPage(WishDetailViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}

using Prototype_Final_Project.ViewModels;

namespace Prototype_Final_Project.Views;

/// <summary>
/// Code-behind for SubmitWishPage.
/// Injects SubmitWishViewModel and establishes the presentation BindingContext.
/// </summary>
public partial class SubmitWishPage : ContentPage
{
    public SubmitWishPage(SubmitWishViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}

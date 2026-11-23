using Prototype_Final_Project.ViewModels;

namespace Prototype_Final_Project.Views;

/// <summary>
/// Code-behind for AboutProjectPage.
/// Injects AboutProjectViewModel and establishes the presentation BindingContext.
/// </summary>
public partial class AboutProjectPage : ContentPage
{
    public AboutProjectPage(AboutProjectViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}

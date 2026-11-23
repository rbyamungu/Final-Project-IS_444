using Prototype_Final_Project.Models;
using Prototype_Final_Project.ViewModels;

namespace Prototype_Final_Project.Views;

/// <summary>
/// Code-behind for DeveloperDashboardPage.
/// Injects DeveloperDashboardViewModel and delegates user interactions to ViewModel commands.
/// </summary>
public partial class DeveloperDashboardPage : ContentPage
{
    private readonly DeveloperDashboardViewModel _viewModel;

    public DeveloperDashboardPage(DeveloperDashboardViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = _viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }

    /// <summary>
    /// Event handler forwarding button tap to ViewModel.ClaimIdeaCommand.
    /// Eliminates template RelativeSource resolution issues during compiled binding inflation.
    /// </summary>
    private void OnClaimIdeaClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is WishItem item)
        {
            _viewModel.ClaimIdeaCommand.Execute(item);
        }
    }

    /// <summary>
    /// Event handler forwarding button tap to ViewModel.UpdateStatusCommand.
    /// </summary>
    private void OnUpdateStatusClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is WishItem item)
        {
            _viewModel.UpdateStatusCommand.Execute(item);
        }
    }
}

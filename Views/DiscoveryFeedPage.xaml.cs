using Prototype_Final_Project.Models;
using Prototype_Final_Project.ViewModels;

namespace Prototype_Final_Project.Views;

/// <summary>
/// Code-behind for DiscoveryFeedPage.
/// Injects DiscoveryFeedViewModel and delegates user interactions to ViewModel commands.
/// </summary>
public partial class DiscoveryFeedPage : ContentPage
{
    private readonly DiscoveryFeedViewModel _viewModel;

    public DiscoveryFeedPage(DiscoveryFeedViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = _viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadFeedAsync();
    }

    /// <summary>
    /// Event handler forwarding card selection to ViewModel.WishSelectedCommand.
    /// </summary>
    private void OnWishSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is WishItem selectedWish)
        {
            if (sender is CollectionView collectionView)
            {
                collectionView.SelectedItem = null;
            }
            _viewModel.WishSelectedCommand.Execute(selectedWish);
        }
    }

    /// <summary>
    /// Event handler forwarding button tap to ViewModel.UpvoteCommand.
    /// Eliminates template RelativeSource resolution issues during compiled binding inflation.
    /// </summary>
    private void OnUpvoteClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is WishItem item)
        {
            _viewModel.UpvoteCommand.Execute(item);
        }
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Prototype_Final_Project.ViewModels;

/// <summary>
/// Abstract base class for all ViewModels implementing INotifyPropertyChanged.
/// Provides reactive two-way data-binding support and common UI state management.
/// Architectural Tier: Application State Tier (Section 7 of Architecture Specification).
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool _isBusy;
    private string _title = string.Empty;

    /// <summary>
    /// Indicates whether a long-running asynchronous operation is active (controls activity indicators).
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Page title bound to Navigation Shell or ContentPage headers.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Event raised when a property value changes to notify the XAML data-binding engine.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Helper method that updates backing field and invokes PropertyChanged if the value has changed.
    /// </summary>
    /// <typeparam name="T">Property type.</typeparam>
    /// <param name="backingStore">Reference to private backing field.</param>
    /// <param name="value">New value to assign.</param>
    /// <param name="propertyName">Automatically captured property name via [CallerMemberName].</param>
    /// <returns>True if value changed; otherwise false.</returns>
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

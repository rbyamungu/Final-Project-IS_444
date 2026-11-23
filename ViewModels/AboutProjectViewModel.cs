namespace Prototype_Final_Project.ViewModels;

/// <summary>
/// ViewModel providing architectural breakdown, academic course context,
/// and reviewer demo accounts (Section 10 and 11 of Architecture Specification).
/// </summary>
public class AboutProjectViewModel : BaseViewModel
{
    /// <summary>
    /// Course name and code for the academic project submission.
    /// </summary>
    public string CourseName => "IS 444: Wireless Application Development";

    /// <summary>
    /// Student developer and author name for course evaluation.
    /// </summary>
    public string StudentName => "Rene Ruhigita (rbyamungu)";

    /// <summary>
    /// Official title of the platform prototype.
    /// </summary>
    public string ProjectName => "Crowd-Sourced Mobile Wishlist & Demand Platform";

    /// <summary>
    /// Prototype milestone label.
    /// </summary>
    public string Semester => "Final Project Prototype";

    /// <summary>
    /// Framework version used for cross-platform binary generation.
    /// </summary>
    public string ArchitectureFramework => ".NET MAUI 10.0 (C# / XAML)";

    /// <summary>
    /// Embedded storage implementation used for offline resilience.
    /// </summary>
    public string LocalDatabase => "SQLite (sqlite-net-pcl)";

    /// <summary>
    /// Presentation and business logic architecture pattern.
    /// </summary>
    public string StatePattern => "Model-View-ViewModel (MVVM)";

    public AboutProjectViewModel()
    {
        Title = "About Prototype";
    }
}

using System.Collections.ObjectModel;
using System.Windows.Input;
using WinTime.Data;
using WinTime.Models;

namespace WinTime.ViewModels;

/// <summary>
/// ViewModel экрана «Список приложений»:
/// — отображение всех приложений с поиском
/// — редактирование DisplayName и Category прямо в таблице
/// — включение/выключение чёрного списка
/// </summary>
public sealed class ApplicationsViewModel : BaseViewModel
{
    private readonly ApplicationRepository _appRepo;
    private readonly ActivityRepository    _activityRepo;

    private List<AppModel> _allApps = [];
    private ObservableCollection<AppModel> _apps = [];
    private string _searchText = string.Empty;
    private bool   _isLoading;

    public ObservableCollection<AppModel> Apps
    {
        get => _apps;
        private set => SetProperty(ref _apps, value);
    }

    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); ApplyFilter(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    // Категории (static, доступны из XAML)

    public static string[] Categories { get; } =
    [
        "Без категории", "Работа", "Отдых", "Учёба", "Игры", "Браузеры", "Разработка", "Связь"
    ];

    // Commands

    public ICommand SaveRowCommand     { get; }
    public ICommand RefreshCommand     { get; }

    // Constructor

    public ApplicationsViewModel(ApplicationRepository appRepo, ActivityRepository activityRepo)
    {
        _appRepo      = appRepo;
        _activityRepo = activityRepo;

        SaveRowCommand  = new RelayCommand<AppModel>(async app => await SaveAppAsync(app));
        RefreshCommand  = new RelayCommand(async () => await LoadAsync());
    }

    // Load

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allApps = await _appRepo.GetAllAsync();
            ApplyFilter();
        }
        finally { IsLoading = false; }
    }

    // Save

    private async Task SaveAppAsync(AppModel app)
    {
        try
        {
            await _appRepo.UpdateDisplayNameAsync(app.Id, app.DisplayName ?? app.ProcessName);
            await _appRepo.UpdateCategoryAsync(app.Id, app.Category);
            await _appRepo.SetBlacklistAsync(app.Id, app.IsBlacklisted);
        }
        catch { /* тихо */ }
    }

    // Filter

    private void ApplyFilter()
    {
        var q = _searchText.Trim();
        var filtered = string.IsNullOrEmpty(q)
            ? _allApps
            : _allApps.Where(a =>
                a.FriendlyName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.ProcessName.Contains(q, StringComparison.OrdinalIgnoreCase));

        Apps = new ObservableCollection<AppModel>(filtered);
    }
}


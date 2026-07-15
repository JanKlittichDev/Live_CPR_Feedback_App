using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Live_CPR_Feedback_App.Models;
using Live_CPR_Feedback_App.Services;
using System.Collections.ObjectModel;

namespace Live_CPR_Feedback_App.ViewModels;

public partial class StorageViewModel : ObservableObject
{
    private readonly ICPRSessionStorageService _sessionStorage;

    public ObservableCollection<CPRSessionInfo> Sessions { get; } = new();

    public StorageViewModel(ICPRSessionStorageService sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public void LoadSessions()
    {
        Sessions.Clear();
        foreach (var s in _sessionStorage.GetAllSessions())
            Sessions.Add(s);
    }

    [RelayCommand]
    private async Task Export(CPRSessionInfo session)
    {
        await Share.Default.RequestAsync(new ShareMultipleFilesRequest
        {
            Title = "CPR Session Export",
            Files = new List<ShareFile>
            {
                new ShareFile(session.AccFilePath),
                new ShareFile(session.AnalysisResultFilePath)
            }
        });
    }

    [RelayCommand]
    private async Task Delete(CPRSessionInfo session)
    {
        await _sessionStorage.DeleteSessionAsync(session);
        Sessions.Remove(session);
    }
}
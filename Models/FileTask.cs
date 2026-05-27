using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace okoshko.Models;

public sealed class FileTask : INotifyPropertyChanged
{
    private FileTaskStatus _status;

    public required string FileName { get; init; }
    public required string FullPath { get; init; }

    public FileTaskStatus Status
    {
        get => _status;
        set
        {
            if (_status == value)
                return;

            _status = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum FileTaskStatus
{
    Waiting,
    Processing,
    Completed,
    Failed
}
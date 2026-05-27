using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using okoshko.Models;
using okoshko.Services;
using okoshko.Utils;

namespace okoshko.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IFileDialogService _fileDialogService;
    private readonly ITextRangingService _textRangingService;

    private int _blockSizePower = 11;
    private RangerAction _selectedAction = RangerAction.Encode;
    private bool _isProcessing;
    private string _statusMessage = string.Empty;

    public MainWindowViewModel(
        IFileDialogService fileDialogService,
        ITextRangingService textRangingService)
    {
        _fileDialogService = fileDialogService;
        _textRangingService = textRangingService;

        ChooseFilesCommand = new RelayCommand(ChooseFiles, CanChooseFiles);
        ProcessFilesCommand = new AsyncRelayCommand(ProcessFilesAsync, CanProcessFiles);
    }

    public ObservableCollection<FileTask> Files { get; } = new();

    public RelayCommand ChooseFilesCommand { get; }
    public AsyncRelayCommand ProcessFilesCommand { get; }

    public int BlockSizePower
    {
        get => _blockSizePower;
        set
        {
            if (_blockSizePower == value)
                return;

            _blockSizePower = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BlockSize));
            OnPropertyChanged(nameof(BlockSizeText));
        }
    }

    public int BlockSize => 1 << BlockSizePower;

    public string BlockSizeText => $"{BlockSize} символов";

    public bool IsEncodeSelected
    {
        get => _selectedAction == RangerAction.Encode;
        set
        {
            if (value)
                SelectedAction = RangerAction.Encode;
        }
    }

    public bool IsDecodeSelected
    {
        get => _selectedAction == RangerAction.Decode;
        set
        {
            if (value)
                SelectedAction = RangerAction.Decode;
        }
    }

    public RangerAction SelectedAction
    {
        get => _selectedAction;
        set
        {
            if (_selectedAction == value)
                return;

            _selectedAction = value;
            OnPropertyChanged(nameof(IsEncodeSelected));
            OnPropertyChanged(nameof(IsDecodeSelected));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
                return;

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        private set
        {
            if (_isProcessing == value)
                return;

            _isProcessing = value;
            OnPropertyChanged();

            ChooseFilesCommand.RaiseCanExecuteChanged();
            ProcessFilesCommand.RaiseCanExecuteChanged();
        }
    }

    private void ChooseFiles()
    {
        Files.Clear();

        var paths = _fileDialogService.ChooseFiles();

        foreach (var path in paths)
        {
            Files.Add(new FileTask
            {
                FileName = Path.GetFileName(path),
                FullPath = path,
                Status = FileTaskStatus.Waiting
            });
        }

        StatusMessage = $"Выбрано файлов: {Files.Count}";
        ProcessFilesCommand.RaiseCanExecuteChanged();
    }

    private async Task ProcessFilesAsync()
    {
        IsProcessing = true;
        StatusMessage = "Обработка файлов...";

        try
        {
            foreach (var file in Files)
            {
                file.Status = FileTaskStatus.Processing;

                try
                {
                    await _textRangingService.ProcessAsync(
                        file.FullPath,
                        SelectedAction,
                        BlockSize);

                    file.Status = FileTaskStatus.Completed;
                }
                catch
                {
                    file.Status = FileTaskStatus.Failed;
                }
            }

            StatusMessage = "Обработка завершена.";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private bool CanChooseFiles() => !IsProcessing;

    private bool CanProcessFiles() => !IsProcessing && Files.Count > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
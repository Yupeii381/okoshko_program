using Microsoft.Win32;

namespace okoshko.Services;

public interface IFileDialogService
{
    IReadOnlyList<string> ChooseFiles();
}

public sealed class FileDialogService : IFileDialogService
{
    public IReadOnlyList<string> ChooseFiles()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter =
                "Text Files (*.txt)|*.txt|" +
                "SSR Files (*.ssr)|*.ssr|" +
                "All Files (*.*)|*.*"
        };

        return dialog.ShowDialog() == true
            ? dialog.FileNames
            : Array.Empty<string>();
    }
}
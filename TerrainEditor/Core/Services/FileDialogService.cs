using Microsoft.Win32;
using TerrainEditor.Annotations;

namespace TerrainEditor.Core.Services
{
    public interface IFileDialogService
    {
        bool ShowOpenFileDialog(ref string filename, string filter, string initialDir = null);
        bool ShowOpenFilesDialog(out string[] filenames, string filter, string initialDir = null);
        bool ShowSaveFileDialog(ref string filename, string filter, string initialDir = null);
    }

    [IsService(typeof(IFileDialogService)),UsedImplicitly]
    public class FileDialogService : IFileDialogService
    {

        public bool ShowOpenFileDialog(ref string filename, string filter, string initialDir = null)
        {
            var dialog = new OpenFileDialog
            {
                FileName = filename,
                Filter = filter
            };

            if (initialDir != null)
                dialog.InitialDirectory = initialDir;

            if (dialog.ShowDialog() == true)
            {
                filename = dialog.FileName;
                return true;
            }
            return false;
        }
        public bool ShowOpenFilesDialog(out string[] filenames, string filter,string initialDir = null)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = filter,
            };
            if (initialDir != null)
                dialog.InitialDirectory = initialDir;

            if (dialog.ShowDialog() == true)
            {
                filenames = dialog.FileNames;
                return true;
            }
            filenames = new string[0];
            return false;
        }
        public bool ShowSaveFileDialog(ref string filename, string filter, string initialDir = null)
        {
            var dialog = new SaveFileDialog
            {
                FileName = filename,
                Filter = filter
            };
            if (initialDir != null)
                dialog.InitialDirectory = initialDir;

            if (dialog.ShowDialog() == true)
            {
                filename = dialog.FileName;
                return true;
            }
            return false;
        }
    }
}
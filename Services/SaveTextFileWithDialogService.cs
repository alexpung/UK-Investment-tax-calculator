using Microsoft.Win32;

namespace CapitalGainCalculator.Services;

public class SaveTextFileWithDialogService
{
    public void OpenFileDialogAndSaveText(string fileName, string contents)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.FileName = fileName;
        saveFileDialog.DefaultExt = ".txt";
        saveFileDialog.Filter = "Text files (*.txt)|*.txt";
        bool? result = saveFileDialog.ShowDialog();
        if (result == true)
        {
            string filename = saveFileDialog.FileName;
            System.IO.File.WriteAllText(filename, contents);
        }
    }
}

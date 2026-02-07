namespace InvestmentTaxCalculator.Services;

/// <summary>
/// Service to track the state of file import processing across components.
/// </summary>
public class FileImportStateService
{
    private bool _isProcessing;
    private int _filesProcessed;
    private int _totalFilesToProcess;

    /// <summary>
    /// Gets or sets whether files are currently being processed.
    /// </summary>
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (_isProcessing != value)
            {
                _isProcessing = value;
                NotifyStateChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the number of files that have been processed.
    /// </summary>
    public int FilesProcessed
    {
        get => _filesProcessed;
        set
        {
            if (_filesProcessed != value)
            {
                _filesProcessed = value;
                NotifyStateChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the total number of files to process.
    /// </summary>
    public int TotalFilesToProcess
    {
        get => _totalFilesToProcess;
        set
        {
            if (_totalFilesToProcess != value)
            {
                _totalFilesToProcess = value;
                NotifyStateChanged();
            }
        }
    }

    /// <summary>
    /// Event raised when the processing state changes.
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Starts a new file processing operation.
    /// </summary>
    /// <param name="totalFiles">The total number of files to process.</param>
    public void StartProcessing(int totalFiles)
    {
        TotalFilesToProcess = totalFiles;
        FilesProcessed = 0;
        IsProcessing = true;
    }

    /// <summary>
    /// Increments the count of processed files.
    /// </summary>
    public void IncrementProcessedFiles()
    {
        FilesProcessed++;
    }

    /// <summary>
    /// Completes the file processing operation.
    /// </summary>
    public void CompleteProcessing()
    {
        IsProcessing = false;
        FilesProcessed = 0;
        TotalFilesToProcess = 0;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

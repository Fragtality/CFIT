namespace CFIT.Installer.LibWorker
{
    public interface IWorkerUpdates
    {
        bool ShowUpdateInSummary { get; set; }
        bool ShowUpdateCompleted { get; set; }
        bool DisplayInSummary { get; set; }
        bool DisplayPinned { get; set; }
        bool DisplayCompleted { get; set; }
    }
}

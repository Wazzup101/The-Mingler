namespace MinglerSync.WinForms;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Local\Wazzup101.MinglerSync.SingleInstance";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: SingleInstanceMutexName,
            createdNew: out var isFirstInstance);

        if (!isFirstInstance)
        {
            MessageBox.Show(
                "Mingler Sync is already running.\n\nReturn to the open window before starting another sync.",
                "Mingler Sync is already open",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.Run(new MainForm());
    }
}

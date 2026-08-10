using MinglerSync.Core;

namespace MinglerSync.WinForms;

public sealed class MainForm : Form
{
    private readonly TextBox _command = new() { Dock = DockStyle.Top, UseSystemPasswordChar = true, PlaceholderText = "Paste the private /register link command" };
    private readonly Button _find = new() { Text = "Find my toons", AutoSize = true };
    private readonly Button _sync = new() { Text = "Sync to Discord", AutoSize = true, Enabled = false };
    private readonly ListBox _toons = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new() { AutoSize = true, Text = "Ready when you are." };
    private PairingDetails? _pairing;
    private IReadOnlyList<CompanionProfile> _profiles = Array.Empty<CompanionProfile>();

    public MainForm()
    {
        Text = "Mingler Sync";
        MinimumSize = new Size(650, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        var page = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 7 };
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.Controls.Add(new Label { Text = "Mingler Sync", AutoSize = true, Font = new Font("Segoe UI", 22, FontStyle.Bold) });
        page.Controls.Add(new Label { Text = "Open the game, enable Companion App Support, then paste the private command from /register link.", AutoSize = true, Padding = new Padding(0, 8, 0, 12) });
        page.Controls.Add(_command);
        var actions = new FlowLayoutPanel { AutoSize = true, Padding = new Padding(0, 12, 0, 8) };
        actions.Controls.AddRange([_find, _sync]);
        page.Controls.Add(actions);
        page.Controls.Add(_toons);
        page.Controls.Add(_status);
        page.Controls.Add(new Label { Text = "Free unofficial fan tool. No gameplay control, passwords, telemetry, or background syncing. Not affiliated with Toontown Rewritten, Disney, or Discord.", AutoSize = true, Padding = new Padding(0, 12, 0, 0) });
        Controls.Add(page);
        _find.Click += FindClicked;
        _sync.Click += SyncClicked;
        _command.TextChanged += (_, _) => { _pairing = null; _profiles = []; _sync.Enabled = false; _toons.Items.Clear(); };
    }

    private async void FindClicked(object? sender, EventArgs e)
    {
        if (!PairingCommandParser.TryParse(_command.Text, out _pairing, out var error)) { _status.Text = error; return; }
        SetBusy(true, "Looking for logged-in toons...");
        try
        {
            _profiles = await new CompanionClient().FindProfilesAsync();
            _toons.Items.Clear();
            foreach (var profile in _profiles) _toons.Items.Add(profile.DisplayName);
            _sync.Enabled = _profiles.Count > 0;
            _status.Text = _profiles.Count > 0 ? $"Found {_profiles.Count} toon profile(s). Review the list before syncing." : "No toons found. Check the game and Companion App Support.";
        }
        catch { _status.Text = "The local scan could not finish. No data was sent."; }
        finally { SetBusy(false); }
    }

    private async void SyncClicked(object? sender, EventArgs e)
    {
        if (_pairing is null || _profiles.Count == 0) return;
        SetBusy(true, "Sending the reviewed data...");
        try { _status.Text = (await new RegisterSyncClient().UploadAsync(_pairing, _profiles)).Message; }
        catch { _status.Text = "Registration could not finish. Run /register link again and retry."; }
        finally { _pairing = null; _profiles = []; _command.Clear(); SetBusy(false); }
    }

    private void SetBusy(bool busy, string? message = null)
    {
        _find.Enabled = !busy;
        _command.Enabled = !busy;
        _sync.Enabled = !busy && _pairing is not null && _profiles.Count > 0;
        UseWaitCursor = busy;
        if (message is not null) _status.Text = message;
    }
}

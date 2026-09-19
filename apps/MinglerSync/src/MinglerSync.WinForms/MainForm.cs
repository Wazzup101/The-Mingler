using MinglerSync.Core;

namespace MinglerSync.WinForms;

public sealed class MainForm : Form
{
    private static readonly Color Navy = Color.FromArgb(24, 30, 54);
    private static readonly Color PanelColor = Color.FromArgb(34, 42, 72);
    private static readonly Color Accent = Color.FromArgb(93, 220, 183);
    private static readonly Color Muted = Color.FromArgb(184, 194, 218);
    private static readonly Color Warning = Color.FromArgb(255, 201, 117);

    private readonly TextBox _pairingCommand = new();
    private readonly Button _findButton = new();
    private readonly Button _demoButton = new();
    private readonly Button _syncButton = new();
    private readonly ListBox _toonList = new();
    private readonly Label _status = new();
    private readonly Label _destination = new();
    private readonly ProgressBar _progress = new();

    private readonly CompanionClient _companionClient = new();
    private readonly RegisterSyncClient _syncClient = new();
    private readonly ClientDiagnosticReporter _diagnosticReporter = new();
    private PairingDetails? _pairing;
    private IReadOnlyList<CompanionProfile> _profiles = Array.Empty<CompanionProfile>();
    private bool _isDemo;

    public MainForm()
    {
        Text = "Mingler Sync";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        BackColor = Navy;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 700);
        ClientSize = new Size(780, 740);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildLayout();
    }

    private void BuildLayout()
    {
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(32, 26, 32, 24),
            ColumnCount = 1,
            RowCount = 8,
            BackColor = Navy,
            AutoScroll = true,
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        page.Controls.Add(CreateHeader(), 0, 0);
        page.Controls.Add(CreateInstruction(), 0, 1);
        page.Controls.Add(CreatePairingPanel(), 0, 2);
        page.Controls.Add(CreateActionRow(), 0, 3);
        page.Controls.Add(CreateDestinationLabel(), 0, 4);
        page.Controls.Add(CreateToonPanel(), 0, 5);
        page.Controls.Add(CreateStatusPanel(), 0, 6);
        page.Controls.Add(CreateFooter(), 0, 7);
        Controls.Add(page);
    }

    private static Control CreateHeader()
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 8),
            Margin = new Padding(0, 0, 0, 12),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var icon = new PictureBox
        {
            Image = Icon.ExtractAssociatedIcon(Application.ExecutablePath)?.ToBitmap(),
            Size = new Size(72, 72),
            Margin = new Padding(0),
            SizeMode = PictureBoxSizeMode.Zoom,
            AccessibleName = "The Mingler app icon",
        };

        var copy = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        copy.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        copy.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        copy.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        copy.Controls.Add(new Label
        {
            Text = "Mingler Sync",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0),
        }, 0, 0);
        copy.Controls.Add(new Label
        {
            Text = "Private, guided toon registration - no terminal required.",
            ForeColor = Muted,
            Font = new Font("Segoe UI", 11F),
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 6, 0, 0),
        }, 0, 1);

        panel.Controls.Add(icon, 0, 0);
        panel.Controls.Add(copy, 1, 0);
        return panel;
    }

    private static Control CreateInstruction() => new Label
    {
        Text = "Before starting: open the game, log into your toon(s), turn on Companion App Support, and accept the in-game prompt.",
        ForeColor = Warning,
        AutoSize = true,
        MaximumSize = new Size(700, 0),
        Margin = new Padding(0, 0, 0, 14),
    };

    private Control CreatePairingPanel()
    {
        var panel = new TableLayoutPanel
        {
            BackColor = PanelColor,
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0, 0, 0, 12),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = "1  Paste your private pairing command",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6),
        });
        panel.Controls.Add(new Label
        {
            Text = "In Discord, run /register link. Copy the entire command block and paste it below. Do not share it with anyone.",
            ForeColor = Muted,
            AutoSize = true,
            MaximumSize = new Size(650, 0),
            Margin = new Padding(0, 0, 0, 8),
        });

        _pairingCommand.Dock = DockStyle.Top;
        _pairingCommand.Multiline = false;
        _pairingCommand.UseSystemPasswordChar = true;
        _pairingCommand.BackColor = Color.FromArgb(18, 23, 43);
        _pairingCommand.ForeColor = Color.White;
        _pairingCommand.BorderStyle = BorderStyle.FixedSingle;
        _pairingCommand.Font = new Font("Segoe UI", 10F);
        _pairingCommand.PlaceholderText = ".\\register-sync.cmd -SyncUrl \"https://…\" -Token \"…\"";
        _pairingCommand.AccessibleName = "Private pairing command from Discord";
        _pairingCommand.TextChanged += (_, _) => ResetScan();
        panel.Controls.Add(_pairingCommand);
        return panel;
    }

    private Control CreateActionRow()
    {
        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 8),
        };

        ConfigureButton(_findButton, "Find my toons", Accent, Navy);
        _findButton.Click += FindButtonClicked;
        ConfigureButton(_demoButton, "Try safe demo", PanelColor, Color.White);
        _demoButton.FlatAppearance.BorderSize = 1;
        _demoButton.FlatAppearance.BorderColor = Muted;
        _demoButton.Click += DemoButtonClicked;
        ConfigureButton(_syncButton, "Sync to Discord", Color.FromArgb(105, 145, 255), Color.White);
        _syncButton.Enabled = false;
        _syncButton.Click += SyncButtonClicked;
        actions.Controls.Add(_findButton);
        actions.Controls.Add(_demoButton);
        actions.Controls.Add(_syncButton);
        return actions;
    }

    private Control CreateDestinationLabel()
    {
        _destination.Text = "Destination: waiting for a pairing command";
        _destination.ForeColor = Muted;
        _destination.AutoSize = true;
        _destination.Margin = new Padding(0, 0, 0, 12);
        return _destination;
    }

    private Control CreateToonPanel()
    {
        var panel = new TableLayoutPanel
        {
            BackColor = PanelColor,
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 12),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = "2  Review the toons found on this computer",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        });
        _toonList.Dock = DockStyle.Fill;
        _toonList.MinimumSize = new Size(0, 125);
        _toonList.BackColor = Color.FromArgb(18, 23, 43);
        _toonList.ForeColor = Color.White;
        _toonList.BorderStyle = BorderStyle.None;
        _toonList.Font = new Font("Segoe UI", 11F);
        _toonList.AccessibleName = "Toons found and synchronization details";
        _toonList.Items.Add("No toon data scanned yet.");
        panel.Controls.Add(_toonList);
        return panel;
    }

    private Control CreateStatusPanel()
    {
        var panel = new Panel { Height = 72, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 10) };
        _progress.Dock = DockStyle.Top;
        _progress.Height = 5;
        _progress.Style = ProgressBarStyle.Marquee;
        _progress.MarqueeAnimationSpeed = 25;
        _progress.Visible = false;
        _status.Text = "Ready when you are.";
        _status.ForeColor = Muted;
        _status.AutoSize = false;
        _status.Dock = DockStyle.Bottom;
        _status.Height = 54;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        panel.Controls.Add(_progress);
        panel.Controls.Add(_status);
        return panel;
    }

    private static Control CreateFooter() => new Label
    {
        Text = "Unofficial third-party tool — not affiliated with Toontown Rewritten or Disney. Reads your local Companion data only after you click; no gameplay control, passwords, telemetry, or background syncing.",
        ForeColor = Muted,
        AutoSize = true,
        MaximumSize = new Size(700, 0),
        Margin = new Padding(0),
    };

    private static void ConfigureButton(Button button, string text, Color backColor, Color foreColor)
    {
        button.Text = text;
        button.AutoSize = true;
        button.Padding = new Padding(18, 9, 18, 9);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = backColor;
        button.ForeColor = foreColor;
        button.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(0, 0, 10, 0);
    }

    private async void FindButtonClicked(object? sender, EventArgs e)
    {
        if (!PairingCommandParser.TryParse(_pairingCommand.Text, out var pairing, out var error))
        {
            ShowStatus(error, isError: true);
            return;
        }

        _pairing = pairing;
        _destination.Text = $"Destination: {pairing!.SyncUri.Scheme}://{pairing.SyncUri.Authority}";
        SetBusy(true, "Looking for logged-in toons… You may see a Companion consent prompt in the game.");

        try
        {
            _profiles = await _companionClient.FindProfilesAsync();
            _toonList.Items.Clear();

            if (_profiles.Count == 0)
            {
                _toonList.Items.Add("No toons found.");
                _syncButton.Enabled = false;
                ShowStatus("No toons were found. Keep the game open, log into a toon, enable Companion App Support, and accept the in-game prompt.", isError: true);
                return;
            }

            foreach (var profile in _profiles)
            {
                _toonList.Items.Add($"✓  {profile.DisplayName}");
            }

            _syncButton.Enabled = true;
            ShowStatus($"Found {_profiles.Count} toon profile(s). Review the list, then select Sync to Discord.");
        }
        catch (Exception exception)
        {
            if (_pairing is not null)
            {
                await _diagnosticReporter.TryReportUnexpectedAsync(_pairing, "companion_scan", exception);
            }
            _profiles = Array.Empty<CompanionProfile>();
            _syncButton.Enabled = false;
            ShowStatus("The local scan could not finish. No data was sent. Close and reopen Mingler Sync, then try again.", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void DemoButtonClicked(object? sender, EventArgs e)
    {
        if (_isDemo)
        {
            ResetScan();
            ShowStatus("Demo closed. Paste a private /register link command when you are ready.");
            return;
        }

        _pairingCommand.Clear();
        _isDemo = true;
        _pairing = null;
        _profiles = CertificationDemo.CreateProfiles();
        _pairingCommand.Enabled = false;
        _destination.Text = "Destination: safe certification demo — nothing will be uploaded";
        _toonList.Items.Clear();
        foreach (var profile in _profiles)
        {
            _toonList.Items.Add($"✓  {profile.DisplayName} — fictional sample data");
        }

        _syncButton.Text = "Finish demo";
        _demoButton.Text = "Exit demo";
        _syncButton.Enabled = true;
        ShowStatus("Demo data is ready. Review the fictional toons, then select Finish demo. No network request will be made.");
    }

    private async void SyncButtonClicked(object? sender, EventArgs e)
    {
        if (_isDemo)
        {
            _toonList.Items.Clear();
            foreach (var profile in _profiles)
            {
                _toonList.Items.Add($"✓  {profile.DisplayName} — demo reviewed locally");
            }
            ShowStatus("✓ Certification demo completed locally. No data was uploaded or registration changed.", isSuccess: true);
            _syncButton.Enabled = false;
            _demoButton.Enabled = true;
            return;
        }

        if (_pairing is null || _profiles.Count == 0)
        {
            ShowStatus("Find your toons before syncing.", isError: true);
            return;
        }

        SetBusy(true, "Sending the reviewed toon data securely to The Mingler…");
        var pairing = _pairing;

        try
        {
            var result = await _syncClient.UploadAsync(pairing, _profiles);

            if (!result.IsSuccess)
            {
                ShowStatus(result.Message, isError: true);
                return;
            }

            _syncButton.Enabled = false;
            ShowSyncResult(result);
        }
        catch (Exception exception)
        {
            await _diagnosticReporter.TryReportUnexpectedAsync(pairing, "sync_upload", exception);
            ShowStatus("Registration could not finish. No pairing information was saved. Run /register link again and retry.", isError: true);
        }
        finally
        {
            ClearPairingSecret();
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy, string? message = null)
    {
        _findButton.Enabled = !busy;
        _demoButton.Enabled = !busy;
        _syncButton.Enabled = !busy && _pairing is not null && _profiles.Count > 0;
        _pairingCommand.Enabled = !busy;
        _progress.Visible = busy;
        UseWaitCursor = busy;
        if (!string.IsNullOrWhiteSpace(message))
        {
            ShowStatus(message);
        }
    }

    private void ResetScan()
    {
        _isDemo = false;
        _pairing = null;
        _profiles = Array.Empty<CompanionProfile>();
        _syncButton.Enabled = false;
        _syncButton.Text = "Sync to Discord";
        _demoButton.Text = "Try safe demo";
        _pairingCommand.Enabled = true;
        _destination.Text = "Destination: waiting for a pairing command";
        _toonList.Items.Clear();
        _toonList.Items.Add("No toon data scanned yet.");
    }

    private void ClearPairingSecret()
    {
        _isDemo = false;
        _pairing = null;
        _pairingCommand.Clear();
        _profiles = Array.Empty<CompanionProfile>();
    }

    private void ShowStatus(string message, bool isError = false, bool isSuccess = false)
    {
        _status.Text = message;
        _status.ForeColor = isError ? Color.FromArgb(255, 139, 139) : isSuccess ? Accent : Muted;
    }

    private void ShowSyncResult(RegisterSyncResult result)
    {
        _toonList.Items.Clear();
        foreach (var toon in result.Toons ?? Array.Empty<ToonSyncResult>())
        {
            var change = toon.WasNew ? "new registration" : "updated";
            _toonList.Items.Add($"✓  {toon.Name} — {change}");

            if (toon.Updated.Count > 0)
            {
                _toonList.Items.Add($"     {string.Join(", ", toon.Updated.Select(FormatUpdatedField))}");
            }
        }

        if (_toonList.Items.Count == 0)
        {
            _toonList.Items.Add($"✓  {result.SyncedToons} toon(s) synchronized");
        }

        ShowStatus($"✓ {result.Message} Registration is complete. Confirm anytime with /register status in Discord.", isSuccess: true);
    }

    private static string FormatUpdatedField(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "toon details";
        }

        var spaced = System.Text.RegularExpressions.Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");
        return spaced.Replace('_', ' ').Trim().ToLowerInvariant();
    }
}

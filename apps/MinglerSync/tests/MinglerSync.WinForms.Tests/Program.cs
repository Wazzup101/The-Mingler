using MinglerSync.Core;
using MinglerSync.WinForms;

internal static class Program
{
    // Offscreen help-only check. Never constructs MainForm, reads preferences,
    // publishes activity, scans Companion, or uploads anything.
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var passed = 0;
        try
        {
            using var form = new AboutHelpForm();
            var tabs = form.Controls.OfType<TabControl>().Single();
            Require(tabs.TabPages.Count == 3, "Expected three help topics."); passed++;
            var links = Descendants(form).OfType<LinkLabel>().ToArray();
            Require(links.Length == 5 && links.All(link => link.Tag is Uri uri && uri.Scheme == "https"
                && uri.Host is "github.com" or "apps.microsoft.com"), "Help links must be fixed HTTPS destinations."); passed++;
            Require(!Descendants(form).OfType<CheckBox>().Any(), "Help must not add a second presence control."); passed++;
            Require(Descendants(form).OfType<Label>().Any(label => label.Text.Contains(AppRelease.DisplayVersion)), "Candidate version missing."); passed++;
            var appVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(AboutHelpForm).Assembly.Location);
            Require(appVersion.FileVersion == AppRelease.Version + ".0"
                && appVersion.ProductVersion?.StartsWith(AppRelease.DisplayVersion, StringComparison.Ordinal) == true,
                "Help version and executable metadata differ."); passed++;

            // Force native layout outside the desktop, without starting MainForm.
            form.CreateControl();
            foreach (var scale in new[] { 1.25F, 2F })
            {
                using var scaled = new AboutHelpForm();
                scaled.StartPosition = FormStartPosition.Manual;
                scaled.Location = new Point(-32000, -32000);
                scaled.ShowInTaskbar = false;
                scaled.Show();
                scaled.Scale(new SizeF(scale, scale));
                var scaledTabs = scaled.Controls.OfType<TabControl>().Single();
                foreach (TabPage tab in scaledTabs.TabPages)
                {
                    scaledTabs.SelectedTab = tab;
                    scaled.PerformLayout();
                    Application.DoEvents();
                    var flow = tab.Controls.OfType<FlowLayoutPanel>().Single();
                    Require(flow.IsHandleCreated && flow.Visible, "Help content did not receive native layout.");
                    Require(flow.AutoScroll && !flow.WrapContents, "Help must scroll rather than clip paragraphs.");
                    foreach (Control control in flow.Controls)
                        Require(control.Width <= Math.Max(flow.ClientSize.Width, 120), "Help text exceeds available width.");
                }
                if (args.Length == 1)
                {
                    Directory.CreateDirectory(args[0]);
                    scaledTabs.SelectedIndex = 0;
                    Application.DoEvents();
                    using var bitmap = new Bitmap(scaled.Width, scaled.Height);
                    scaled.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(Path.Combine(args[0], $"about-help-{scale * 100:0}.png"));
                }
                scaled.Hide();
                passed++;
            }
            Console.WriteLine($"PASS: {passed} help/version/link/offscreen-layout checks. No live app or Discord connection.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL: " + error.Message); return 1; }
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}

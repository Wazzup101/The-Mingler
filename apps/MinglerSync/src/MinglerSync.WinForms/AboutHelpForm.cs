using System.Diagnostics;
using MinglerSync.Core;

namespace MinglerSync.WinForms;

// Pure help UI: no preferences, game scan, upload, or Discord connection.
public sealed class AboutHelpForm : Form
{
    public AboutHelpForm()
    {
        Text = "Mingler Sync — About & Help";
        Font = new Font("Segoe UI", 10F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(650, 590);
        MinimumSize = new Size(440, 390);
        StartPosition = FormStartPosition.CenterParent;
        var tabs = new TabControl { Dock = DockStyle.Fill, AccessibleName = "Help topics" };
        Controls.Add(tabs);

        var about = AddTab(tabs, "About");
        AddText(about, $"Mingler Sync {AppRelease.DisplayVersion}\n{AppRelease.Channel}", true);
        AddText(about, "Review and sync your own Toon data with The Mingler. This candidate is not an installed Microsoft Store update.");
        AddLink(about, "Microsoft Store / Updates", AppRelease.StoreUrl);
        AddLink(about, "Public source", AppRelease.SourceUrl);
        AddLink(about, "Terms of Service", AppRelease.TermsUrl);
        AddLink(about, "Privacy Policy", AppRelease.PrivacyUrl);
        AddText(about, "Support: use /tickets in Discord. Never share your private pairing command or game password.\n\nUnofficial community tool. Not affiliated with Toontown Rewritten or Disney.");

        var start = AddTab(tabs, "Getting started");
        AddText(start, "Register your own Toons", true);
        AddText(start, "1. Open the game and log into the Toons you want to sync.\n\n2. Enable Companion App Support and accept its in-game prompt.\n\n3. Run /register link in Discord. Paste your own private pairing command into Mingler Sync.\n\n4. Select Find my toons and review the list.\n\n5. Select Sync to Discord. Use /register status to check the result if confirmation is uncertain.");
        AddText(start, "Try safe demo uses fictional data without scanning or uploading. It does not register anything. Rich Presence is a separate option.");

        var privacy = AddTab(tabs, "Privacy & troubleshooting");
        AddText(privacy, "You control scanning, uploading, and activity", true);
        AddText(privacy, "Game scanning starts only when you select Find my toons. Uploading starts only when you select Sync to Discord after reviewing the list. The hidden pairing token is held in memory and cleared after an upload attempt.\n\nDiscord activity is optional and off by default. Use the main window's single activity switch. When enabled, Discord receives only the app identity, fixed icon and session start time—not Toon names or sync details. Discord controls who can see the activity. The on/off choice is saved on this computer. Disabling or closing requests activity removal; a disconnected Discord client may take time to clear it.\n\nUnexpected faults may send a bounded authenticated diagnostic when a valid pairing token is present. See the Privacy Policy for its contents and limits.");
        AddText(privacy, "No Toons found: keep the game open, enable Companion support and accept its prompt.\n\nExpired pairing link: request a fresh /register link.\n\nCould not confirm registration: check /register status before uploading again.\n\nWaiting for Discord: open Discord desktop if you want activity sharing. Registration works without Rich Presence.");
        AddLink(privacy, "Privacy Policy", AppRelease.PrivacyUrl);
    }

    private static FlowLayoutPanel AddTab(TabControl tabs, string title)
    {
        var page = new TabPage(title);
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, Padding = new Padding(18),
        };
        flow.SizeChanged += (_, _) =>
        {
            var width = Math.Max(120, flow.ClientSize.Width - flow.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 8);
            foreach (Control child in flow.Controls) child.MaximumSize = new Size(width, 0);
        };
        page.Controls.Add(flow);
        tabs.TabPages.Add(page);
        return flow;
    }

    private static void AddText(Control parent, string text, bool heading = false) => parent.Controls.Add(new Label
    {
        Text = text, AutoSize = true, MaximumSize = new Size(560, 0),
        Margin = new Padding(0, 0, 0, 16),
        Font = heading ? new Font("Segoe UI", 12F, FontStyle.Bold) : parent.Font,
    });

    private static void AddLink(Control parent, string text, string address)
    {
        var uri = new Uri(address, UriKind.Absolute);
        if (uri.Scheme != Uri.UriSchemeHttps || uri.Host is not ("github.com" or "apps.microsoft.com"))
            throw new ArgumentException("Help links must use a fixed publisher HTTPS destination.");
        var link = new LinkLabel { Text = text, AutoSize = true, Tag = uri, Margin = new Padding(0, 0, 0, 12) };
        link.LinkClicked += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
            catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
            { MessageBox.Show("The link could not be opened. Please visit the published Mingler project page in your browser.", "Mingler Sync"); }
        };
        parent.Controls.Add(link);
    }
}

using System.Diagnostics;
using System.IO.Compression;
using System.Net.Security;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using Npgsql;
using Microsoft.Web.Administration;

namespace BAUSolutions.eMilimo.Setup;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try
        {
            var captureArgument = args.FirstOrDefault(arg => arg.StartsWith("/capture-ui=", StringComparison.OrdinalIgnoreCase));
            if (captureArgument != null)
            {
                var path = captureArgument[(captureArgument.IndexOf('=') + 1)..].Trim('"');
                var requestedPage = args.FirstOrDefault(arg => arg.StartsWith("/preview-page=", StringComparison.OrdinalIgnoreCase));
                var previewPage = requestedPage != null && int.TryParse(requestedPage[(requestedPage.IndexOf('=') + 1)..], out var value) ? value : 0;
                using var preview = new SetupForm(args, SetupForm.FindPackageRoot());
                preview.CapturePreview(previewPage, path);
                return;
            }
            var packageRoot = PayloadPreparationDialog.Prepare();
            System.Windows.Forms.Application.Run(new SetupForm(args, packageRoot));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"eMilimo Setup could not prepare the installation package.\n\n{ex.Message}",
                "eMilimo Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

internal sealed class SetupForm : Form
{
    private static readonly Color Ink = Color.FromArgb(24, 29, 27);
    private static readonly Color Muted = Color.FromArgb(102, 112, 107);
    private static readonly Color Forest = Color.FromArgb(20, 75, 56);
    private static readonly Color Gold = Color.FromArgb(184, 134, 46);
    private static readonly Color Success = Color.FromArgb(22, 125, 78);
    private static readonly Color Danger = Color.FromArgb(185, 45, 45);
    private static readonly Color Line = Color.FromArgb(222, 226, 223);
    private static readonly Color Surface = Color.FromArgb(249, 250, 248);
    private const string VersionText = "v1.0.1";
    private const string PayloadMarker = "EMILIMO_PAYLOAD_V1";
    private static readonly Version CurrentVersion = new(1, 0, 1);
    private const string UninstallRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\eMilimo";
    private Dictionary<string, OnlinePrerequisite>? onlinePrerequisites;

    private readonly Panel content = new() { Dock = DockStyle.Fill, BackColor = Color.White };
    private readonly Label stepLabel = new() { AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI", 9) };
    private readonly TableLayoutPanel stepStrip = new() { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1 };
    private readonly Label inlineMessage = new() { AutoSize = true, ForeColor = Danger, Font = new Font("Segoe UI", 8.5f) };
    private readonly Button back = SecondaryButton("Back");
    private readonly Button next = PrimaryButton("Continue");
    private readonly Button cancel = SecondaryButton("Cancel");
    private readonly Label status = new() { AutoSize = true, ForeColor = Muted };

    private readonly RadioButton fresh = new() { Text = "Create a new eMilimo environment", Checked = true, AutoSize = true };
    private readonly RadioButton existing = new() { Text = "Connect to an existing PostgreSQL database", AutoSize = true };
    
    // Connection string fields
    private readonly TextBox connection = Input();
    private readonly Label connectionLabel = new() { Text = "Existing PostgreSQL connection string", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
    private readonly Label connectionHint = new() { Text = "Example: Host=localhost;Port=5432;Database=emilimo_db;Username=postgres;Password=yourpassword", AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI", 8) };
    
    // New environment config fields
    private readonly Panel newEnvPanel = new() { AutoSize = true, Dock = DockStyle.Top };
    private readonly TextBox dbHost = Input("localhost");
    private readonly NumericUpDown dbPort = PortInput(5432);
    private readonly TextBox dbUsername = Input("postgres");
    private readonly TextBox dbPassword = Input();
    private readonly TextBox dbName = Input("emilimo_db");
    
    private readonly TextBox destination = Input();
    private readonly TextBox publicHost = Input("localhost");
    private readonly TextBox publicAdminUrl = Input();
    private readonly TextBox publicApiUrl = Input();
    private readonly TextBox publicGivingUrl = Input();
    private readonly TextBox instance = Input("main");
    private readonly TextBox congregationName = Input();
    private readonly ComboBox adminTitle = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
    private readonly TextBox adminFirstName = Input();
    private readonly TextBox adminLastName = Input();
    private readonly TextBox adminEmail = Input();
    private readonly TextBox adminPhone = Input();
    private readonly TextBox adminPassword = Input();
    private readonly TextBox adminPasswordConfirmation = Input();
    private readonly Panel adminPanel = new() { AutoSize = true, Dock = DockStyle.Top };
    private readonly RadioButton trialLicense = new() { Text = "Start with a 30-day trial", Checked = true, AutoSize = true };
    private readonly RadioButton signedLicense = new() { Text = "Install a signed BAU licence", AutoSize = true };
    private readonly TextBox licenseFilePath = new() { ReadOnly = true, Width = 500, PlaceholderText = "Select a BAU-issued .xml licence file" };
    private readonly Label licenseSelectionStatus = new() { AutoSize = true, ForeColor = Muted, Text = "A trial licence will be created during first-run setup." };
    private readonly NumericUpDown webPort = PortInput(4200);
    private readonly NumericUpDown apiPort = PortInput(5290);
    private readonly NumericUpDown givingPort = PortInput(9200);
    private readonly Label webPortStatus = PortStatusLabel();
    private readonly Label apiPortStatus = PortStatusLabel();
    private readonly Label givingPortStatus = PortStatusLabel();
    private readonly CheckBox acceptTerms = new() { Text = "I have read and accept the Terms and Conditions", AutoSize = true };
    private readonly CheckBox acceptPrivacy = new() { Text = "I have read and accept the Privacy Policy", AutoSize = true };
    
    // Components with deployment options
    private readonly CheckBox core = Component("eMilimo web portal and API", "Required", true, false);
    private readonly CheckBox database = Component("PostgreSQL database", "Required for a new environment", true, false);
    private readonly CheckBox pgAdmin = Component("pgAdmin database administration tool", "Optional", false, true);
    private readonly CheckBox cache = Component("Redis-compatible distributed cache", "Optional", false, false);
    private readonly CheckBox docker = Component("Docker container runtime", "Required for containerized deployment", true, true);
    private readonly CheckBox hostingBundle = Component("ASP.NET Core 8 Hosting Bundle", "Required for IIS", true, true);
    private readonly CheckBox trial = Component("30-day machine-bound trial licence", "Included", true, false);
    private readonly CheckBox shortcut = Component("Desktop shortcut", "Optional", true, true);
    private readonly CheckBox launch = Component("Open eMilimo when setup completes", "Optional", true, true);

    // Deployment options
    private readonly ComboBox deploymentMode = new() 
    { 
        DropDownStyle = ComboBoxStyle.DropDownList, 
        Width = 250, 
        Height = 36,
        Font = new Font("Segoe UI", 10)
    };
    private readonly ComboBox databaseType = new() 
    { 
        DropDownStyle = ComboBoxStyle.DropDownList, 
        Width = 250, 
        Height = 36,
        Font = new Font("Segoe UI", 10)
    };
    private readonly ComboBox cacheMode = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 360,
        Height = 36,
        Font = new Font("Segoe UI", 10)
    };
    private readonly TextBox cacheHost = Input("localhost");
    private readonly NumericUpDown cachePort = PortInput(6379);
    private readonly TextBox cacheUsername = Input();
    private readonly TextBox cachePassword = Input();
    private readonly CheckBox cacheTls = new() { Text = "Use TLS", AutoSize = true };
    private readonly NumericUpDown cacheDatabase = new() { Minimum = 0, Maximum = 15, Value = 0, Width = 110 };
    private readonly Label cacheTestStatus = new() { AutoSize = true, ForeColor = Muted, Text = "Connection not tested" };

    // Installation progress controls
    private ProgressBar? progressBar;
    private Label? progressLabel;
    private Label? elapsedLabel;
    private RichTextBox? progressLog;
    private TableLayoutPanel? milestonePanel;
    private readonly List<Milestone> milestones = new();
    private Button? installCompleteButton;
    private bool installationSucceeded;
    private CancellationTokenSource? installationCancellation;
    private readonly Stopwatch installationTimer = new();
    private readonly object logSync = new();
    private readonly System.Windows.Forms.Timer elapsedTimer = new() { Interval = 1000 };
    private readonly string installerLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BAU Solutions", "eMilimo", "Installer", $"install-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    private int page;
    private bool installing;
    private readonly string? packageRoot;
    private readonly string[] startupArgs;
    private InstalledProduct? installedProduct;
    private bool maintenanceSession;
    private bool restoreFromRepairCache;
    private bool termsReviewed;
    private bool privacyReviewed;

    public SetupForm(string[] args, string? preparedPackageRoot)
    {
        startupArgs = args;
        packageRoot = preparedPackageRoot;
        installedProduct = InstalledProduct.Read(UninstallRegistryPath);
        
        // Default to inetpub\wwwroot\BAUSolutions.eMilimo
        var defaultPath = Path.Combine(
            Environment.GetEnvironmentVariable("SystemDrive") ?? "C:", 
            "inetpub", 
            "wwwroot", 
            "BAUSolutions.eMilimo"
        );
        destination.Text = defaultPath;
        
        // Populate deployment options
        deploymentMode.Items.AddRange(new object[] 
        { 
            "Docker Container (Recommended)",
            "IIS Windows Service",
            "Standalone Console"
        });
        deploymentMode.SelectedIndex = 0;
        deploymentMode.SelectedIndexChanged += (_, _) =>
        {
            if (page is 1 or 4) RenderPage();
        };
        webPort.ValueChanged += (_, _) => UpdatePortAvailability();
        apiPort.ValueChanged += (_, _) => UpdatePortAvailability();
        givingPort.ValueChanged += (_, _) => UpdatePortAvailability();

        databaseType.Items.Add("PostgreSQL");
        databaseType.SelectedIndex = 0;

        cacheMode.Items.AddRange(new object[]
        {
            "Managed Valkey container (open source)",
            "Use existing Redis-compatible server",
            "No distributed cache"
        });
        cacheMode.SelectedIndex = 0;
        cacheMode.SelectedIndexChanged += (_, _) =>
        {
            cacheTestStatus.Text = "Connection not tested";
            if (page == 4) RenderPage();
        };
        cachePassword.PasswordChar = '•';
        adminTitle.Items.AddRange(["Mr.", "Mrs.", "Ms.", "Dr.", "Rev."]);
        adminTitle.SelectedIndex = 0;
        adminPassword.PasswordChar = '•';
        adminPasswordConfirmation.PasswordChar = '•';
        acceptTerms.Enabled = false;
        acceptPrivacy.Enabled = false;
        acceptTerms.CheckedChanged += (_, _) => UpdateWelcomeReadiness();
        acceptPrivacy.CheckedChanged += (_, _) => UpdateWelcomeReadiness();
        trialLicense.CheckedChanged += (_, _) =>
        {
            trial.Checked = trialLicense.Checked;
            licenseFilePath.Enabled = signedLicense.Checked;
            licenseSelectionStatus.Text = trialLicense.Checked
                ? "A trial licence will be created during first-run setup."
                : string.IsNullOrWhiteSpace(licenseFilePath.Text) ? "Select a signed XML licence file." : "The signed licence will be verified against this installation.";
        };
        signedLicense.CheckedChanged += (_, _) => licenseFilePath.Enabled = signedLicense.Checked;

        // Password field
        dbPassword.PasswordChar = '•';
        dbPassword.PlaceholderText = "Enter a strong database password";
        
        Text = "eMilimo Setup";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 540);
        Size = new Size(800, 620);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10);
        Icon = Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);

        // Handle environment selection
        fresh.CheckedChanged += (_, _) => ToggleEnvironmentFields();
        existing.CheckedChanged += (_, _) => ToggleEnvironmentFields();
        
        back.Click += (_, _) => { if (page > 0 && !installing) { page--; RenderPage(); } };
        next.Click += NextClicked;
        cancel.Click += (_, _) =>
        {
            if (!installing) { Close(); return; }
            if (MessageBox.Show("Stop the current installation? Completed steps will be left in place so setup can be retried.",
                "Cancel installation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                installationCancellation?.Cancel();
        };
        FormClosing += (_, e) =>
        {
            if (!installing) return;
            e.Cancel = true;
            installationCancellation?.Cancel();
        };
        elapsedTimer.Tick += (_, _) =>
        {
            if (elapsedLabel != null) elapsedLabel.Text = $"Elapsed {installationTimer.Elapsed:mm\\:ss}";
        };

        Controls.Add(BuildMain());
        installedProduct ??= DetectLegacyInstallation(defaultPath);
        if (installedProduct != null)
        {
            LoadInstalledConfiguration(installedProduct);
            page = 8;
        }
        RenderPage();
    }

    internal void CapturePreview(int requestedPage, string path)
    {
        page = Math.Clamp(requestedPage, 0, 7);
        RenderPage();
        Show();
        System.Windows.Forms.Application.DoEvents();
        using var bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
        DrawToBitmap(bitmap, new Rectangle(Point.Empty, ClientSize));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        Hide();
    }

    private void ToggleEnvironmentFields()
    {
        connectionLabel.Visible = existing.Checked;
        connection.Visible = existing.Checked;
        connectionHint.Visible = existing.Checked;
        newEnvPanel.Visible = fresh.Checked;
        adminPanel.Visible = fresh.Checked;
        databaseType.Enabled = fresh.Checked;
        database.Text = existing.Checked ? "PostgreSQL database (existing)" : "PostgreSQL database";
        
        if (existing.Checked && string.IsNullOrWhiteSpace(connection.Text))
        {
            connection.Text = "Host=localhost;Port=5432;Database=emilimo_db;Username=postgres;Password=yourpassword";
        }
    }

    private Control BuildVisual()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Ink };
        panel.Paint += (_, e) =>
        {
            var image = LoadImage("BAUSolutions.eMilimo.Setup.setup-background.png");
            if (image != null)
            {
                using (image)
                {
                    var scale = Math.Max(panel.ClientSize.Width / (float)image.Width, panel.ClientSize.Height / (float)image.Height);
                    var size = new Size((int)(image.Width * scale), (int)(image.Height * scale));
                    var target = new Rectangle((panel.Width - size.Width) / 2, (panel.Height - size.Height) / 2, size.Width, size.Height);
                    e.Graphics.DrawImage(image, target);
                }
            }
            using var veil = new SolidBrush(Color.FromArgb(155, 10, 31, 24));
            e.Graphics.FillRectangle(veil, panel.ClientRectangle);
        };

        var brand = new Label { Text = "eMilimo", AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Segoe UI", 30, FontStyle.Bold), Left = 38, Top = 54 };
        var tagline = new Label { Text = "Church work, organised.", AutoSize = true, ForeColor = Color.FromArgb(225, 235, 230), BackColor = Color.Transparent, Font = new Font("Segoe UI", 13), Left = 41, Top = 112 };
        var productCopy = new Label { Text = "Secure church operations, finance and ministry administration.", AutoSize = true, MaximumSize = new Size(225, 0), ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Segoe UI", 11), Left = 36, Top = 455 };
        var version = new Label { Text = VersionText, AutoSize = true, ForeColor = Color.FromArgb(205, 218, 211), BackColor = Color.Transparent, Font = new Font("Segoe UI", 8), Left = 42, Top = 665 };
        panel.Resize += (_, _) => version.Top = panel.ClientSize.Height - 45;
        panel.Controls.AddRange([brand, tagline, productCopy, version]);
        return panel;
    }

    private Control BuildMain()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(28, 18, 28, 16), BackColor = Color.White };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(new Label { Text = "eMilimo Setup", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 14, FontStyle.Bold), Margin = new Padding(0, 2, 12, 0) }, 0, 0);
        header.Controls.Add(stepLabel, 1, 0);
        var bau = new PictureBox { Image = LoadImage("BAUSolutions.eMilimo.Setup.bau-logo.png"), SizeMode = PictureBoxSizeMode.Zoom, Width = 82, Height = 24, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        header.Controls.Add(bau, 2, 0);

        string[] steps = ["Welcome", "Deployment", "Admin", "Database", "Cache", "Review"];
        for (var i = 0; i < steps.Length; i++)
        {
            stepStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / steps.Length));
            stepStrip.Controls.Add(new Label { Text = steps[i], Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5f), Margin = new Padding(2) }, i, 0);
        }

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, Padding = new Padding(0, 6, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var feedback = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0) };
        feedback.Controls.Add(inlineMessage);
        feedback.Controls.Add(status);
        footer.Controls.Add(feedback, 0, 0);
        footer.Controls.Add(cancel, 2, 0);
        footer.Controls.Add(back, 3, 0);
        footer.Controls.Add(next, 4, 0);

        panel.Controls.Add(header, 0, 0);
        panel.Controls.Add(stepStrip, 0, 1);
        panel.Controls.Add(content, 0, 2);
        panel.Controls.Add(footer, 0, 3);
        return panel;
    }

    private void RenderPage()
    {
        content.Controls.Clear();
        status.Text = string.Empty;
        inlineMessage.Text = string.Empty;
        back.Enabled = page > 0 && !installing && page < 6;
        cancel.Enabled = page != 6 || installing;
        cancel.Text = page is 7 or 8 ? "Close" : installing ? "Cancel installation" : "Cancel";
        next.Visible = page < 6;
        next.Enabled = !installing;
        next.Text = page == 5 ? "Install eMilimo" : "Continue";
        stepLabel.Text = page == 6 ? "Installing" : page == 7 ? "Complete" : page == 8 ? "Maintenance" : $"Step {page + 1} of 6";
        stepLabel.Margin = new Padding(0, 6, 0, 0);
        stepStrip.Visible = page < 6;
        for (var i = 0; i < stepStrip.Controls.Count; i++)
        {
            if (stepStrip.Controls[i] is not Label label) continue;
            label.ForeColor = i == page ? Forest : Muted;
            label.BackColor = i == page ? Color.FromArgb(232, 244, 238) : Color.White;
            label.Font = new Font("Segoe UI", 8.5f, i == page ? FontStyle.Bold : FontStyle.Regular);
        }

        Control view = page switch
        {
            0 => WelcomePage(),
            1 => DeploymentPage(),
            2 => AdministratorPage(),
            3 => DatabasePage(),
            4 => ComponentsPage(),
            5 => ReviewPage(),
            6 => InstallProgressPage(),
            7 => CompletionPage(),
            _ => MaintenancePage()
        };
        content.Controls.Add(view);
        UpdateWelcomeReadiness();
    }

    private Control WelcomePage()
    {
        var layout = Page("Welcome to eMilimo", "We’ll configure a secure, ready-to-use church administration environment in a few short steps.");
        layout.Controls.Add(InfoCard("Before you begin", "Administrator access is required. Keep your congregation details and default administrator information ready; setup checks prerequisites and validates each choice as you go."));
        var legal = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 10, 0, 0) };
        legal.Controls.Add(acceptTerms);
        legal.Controls.Add(acceptPrivacy);
        var links = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 7, 0, 0) };
        var terms = LinkButton("View terms");
        terms.Click += (_, _) =>
        {
            if (!ShowReviewDocument("Terms and Conditions", ReadResource("BAUSolutions.eMilimo.Setup.TermsAndConditions.txt"))) return;
            termsReviewed = true; acceptTerms.Enabled = true; terms.Text = "Terms reviewed ✓"; UpdateWelcomeReadiness();
        };
        var privacy = LinkButton("View privacy policy");
        privacy.Click += (_, _) =>
        {
            if (!ShowReviewDocument("Privacy Policy", ReadResource("BAUSolutions.eMilimo.Setup.PrivacyPolicy.txt"))) return;
            privacyReviewed = true; acceptPrivacy.Enabled = true; privacy.Text = "Privacy reviewed ✓"; UpdateWelcomeReadiness();
        };
        links.Controls.AddRange([terms, privacy]);
        legal.Controls.Add(links);
        layout.Controls.Add(legal);
        return ScrollPage(layout);
    }

    private Control DeploymentPage()
    {
        var layout = Page("Choose deployment", "Select how eMilimo will run. Recommended choices are already selected for a typical installation.");
        layout.Controls.Add(Field("Deployment mode", deploymentMode, "Docker is recommended for predictable upgrades and isolated services."));
        layout.Controls.Add(Field("Congregation name", congregationName, "Shown throughout eMilimo and included in first-run configuration."));

        var licence = SectionCard("Licence", "Start with a trial or provide a BAU-issued signed licence.");
        licence.Controls.Add(trialLicense);
        licence.Controls.Add(signedLicense);
        var licenceRow = ResponsiveRow(78);
        licenseFilePath.Dock = DockStyle.Fill;
        licenceRow.Controls.Add(licenseFilePath, 0, 0);
        var browseLicence = SecondaryButton("Browse…");
        browseLicence.Click += (_, _) =>
        {
            if (!signedLicense.Checked) { signedLicense.Checked = true; }
            using var dialog = new OpenFileDialog { Filter = "eMilimo licence (*.xml)|*.xml", CheckFileExists = true, Title = "Select signed eMilimo licence" };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            licenseFilePath.Text = dialog.FileName;
            licenseSelectionStatus.Text = "Selected. The licence will be verified before setup completes.";
        };
        licenceRow.Controls.Add(browseLicence, 1, 0);
        licence.Controls.Add(licenceRow);
        licence.Controls.Add(licenseSelectionStatus);
        var identifier = new TextBox { Text = licenseUserIdentifier, ReadOnly = true, Dock = DockStyle.Top, BackColor = Color.White, Font = new Font("Cascadia Mono", 9) };
        var identifierRow = ResponsiveRow(78);
        identifierRow.Controls.Add(identifier, 0, 0);
        var copyIdentifier = SecondaryButton("Copy");
        copyIdentifier.Click += (_, _) => Clipboard.SetText(licenseUserIdentifier);
        identifierRow.Controls.Add(copyIdentifier, 1, 0);
        licence.Controls.Add(Field("Installation licence identifier", identifierRow, "Generated uniquely for this installation. A signed licence must contain this exact identifier."));
        var systemIdentifier = new TextBox { Text = systemId, ReadOnly = true, Dock = DockStyle.Top, BackColor = Color.White, Font = new Font("Cascadia Mono", 9) };
        var systemIdentifierRow = ResponsiveRow(78);
        systemIdentifierRow.Controls.Add(systemIdentifier, 0, 0);
        var copySystemIdentifier = SecondaryButton("Copy");
        copySystemIdentifier.Click += (_, _) => Clipboard.SetText(systemId);
        systemIdentifierRow.Controls.Add(copySystemIdentifier, 1, 0);
        licence.Controls.Add(Field("Installation system identifier", systemIdentifierRow, "Use this exact value as userSystem when BAU Solutions issues the signed licence."));
        layout.Controls.Add(licence);

        var advanced = SectionCard("Advanced options", "Change network identity or installation location only when required.");
        advanced.Visible = false;
        var advancedToggle = LinkButton("Show advanced options");
        advancedToggle.Click += (_, _) => { advanced.Visible = !advanced.Visible; advancedToggle.Text = advanced.Visible ? "Hide advanced options" : "Show advanced options"; };
        layout.Controls.Add(advancedToggle);
        advanced.Controls.Add(Field("Server host name or IP address", publicHost, "Used for local and LAN access when public URLs are not supplied."));
        advanced.Controls.Add(Field("Public administration URL", publicAdminUrl, "Optional production URL, for example https://admin.church.org"));
        advanced.Controls.Add(Field("Public API URL", publicApiUrl, "Optional production URL, for example https://api.church.org"));
        advanced.Controls.Add(Field("Public Giving URL", publicGivingUrl, "Optional production URL, for example https://give.church.org"));
        advanced.Controls.Add(Field("eMilimo instance", instance));
        var destinationRow = ResponsiveRow(70);
        destination.Dock = DockStyle.Fill;
        destinationRow.Controls.Add(destination, 0, 0);
        var browse = SecondaryButton("Browse");
        browse.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = destination.Text, Description = "Choose the eMilimo installation folder" };
            if (dialog.ShowDialog() == DialogResult.OK) destination.Text = dialog.SelectedPath;
        };
        destinationRow.Controls.Add(browse, 1, 0);
        advanced.Controls.Add(Field("Installation folder", destinationRow));
        advanced.Controls.Add(Field("API port", apiPort, "Used separately by Docker deployments."));
        advanced.Controls.Add(Field("Public Giving port", givingPort, "The independent Giving application listens here; the default is 9200."));
        advanced.Controls.Add(givingPortStatus);
        layout.Controls.Add(advanced);
        return ScrollPage(layout);
    }

    private Control AdministratorPage()
    {
        var layout = Page("Default administrator", "Create the first SuperAdmin account. This account is ready before installation finishes.");
        var names = ResponsiveGrid(3, 18, 41, 41);
        names.Controls.Add(Field("Title", adminTitle), 0, 0);
        names.Controls.Add(Field("First name", adminFirstName), 1, 0);
        names.Controls.Add(Field("Surname", adminLastName), 2, 0);
        layout.Controls.Add(names);
        var contact = ResponsiveGrid(2, 58, 42);
        contact.Controls.Add(Field("Email address", adminEmail), 0, 0);
        contact.Controls.Add(Field("Phone number", adminPhone, "Include country code where possible"), 1, 0);
        layout.Controls.Add(contact);
        var passwords = ResponsiveGrid(2, 50, 50);
        passwords.Controls.Add(Field("Password", adminPassword, "8+ characters with uppercase, lowercase, number and symbol"), 0, 0);
        passwords.Controls.Add(Field("Confirm password", adminPasswordConfirmation), 1, 0);
        layout.Controls.Add(passwords);
        layout.Controls.Add(Field("Web portal port", webPort));
        layout.Controls.Add(webPortStatus);
        var check = SecondaryButton("Check availability");
        check.Click += (_, _) => UpdatePortAvailability();
        layout.Controls.Add(check);
        UpdatePortAvailability();
        return ScrollPage(layout);
    }

    private Control DatabasePage()
    {
        var layout = Page("Database", "Create a managed PostgreSQL database or connect eMilimo to an existing one.");
        layout.Controls.Add(fresh);
        layout.Controls.Add(existing);
        if (existing.Checked)
        {
            layout.Controls.Add(Field("PostgreSQL connection string", connection, "Credentials are validated before installation."));
        }
        else
        {
            var deployMode = deploymentMode.SelectedItem?.ToString() ?? "Docker Container (Recommended)";
            database.Enabled = deployMode != "Docker Container (Recommended)" && HasAvailablePrerequisite("postgresql", "postgresql*.exe");
            database.Checked = deployMode == "Docker Container (Recommended)" || database.Checked;
            database.Text = deployMode == "Docker Container (Recommended)"
                ? "PostgreSQL managed container (included with Docker deployment)"
                : HasBundledPrerequisite("postgresql*.exe") ? "Install bundled PostgreSQL server" : "Download and install PostgreSQL when setup begins";
            layout.Controls.Add(database);
            var row1 = ResponsiveGrid(2, 72, 28);
            row1.Controls.Add(Field("Host", dbHost), 0, 0);
            row1.Controls.Add(Field("Port", dbPort), 1, 0);
            var row2 = ResponsiveGrid(2, 50, 50);
            row2.Controls.Add(Field("Database name", dbName), 0, 0);
            row2.Controls.Add(Field("Username", dbUsername), 1, 0);
            layout.Controls.Add(row1);
            layout.Controls.Add(row2);
            layout.Controls.Add(Field("Database password", dbPassword, "Used to provision and secure the eMilimo database."));
            pgAdmin.Enabled = HasAvailablePrerequisite("pgadmin", "pgadmin*.exe");
            if (!pgAdmin.Enabled) pgAdmin.Checked = false;
            pgAdmin.Text = pgAdmin.Enabled ? "Install pgAdmin (optional)" : "pgAdmin (not available in this package)";
            layout.Controls.Add(pgAdmin);
        }
        layout.Controls.Add(InfoCard("What setup will do", fresh.Checked
            ? "Setup will install or provision PostgreSQL, create the database, apply migrations, and verify readiness."
            : "Setup will test the supplied connection, apply required migrations, and leave the database service under your control."));
        return ScrollPage(layout);
    }

    private Control LegalPage()
    {
        var layout = Page("Legal agreements", "Review both documents. You must accept each agreement before setup can continue.");
        layout.Controls.Add(DocumentBox("Terms and Conditions", ReadResource("BAUSolutions.eMilimo.Setup.TermsAndConditions.txt")));
        layout.Controls.Add(acceptTerms);
        layout.Controls.Add(DocumentBox("Privacy Policy", ReadResource("BAUSolutions.eMilimo.Setup.PrivacyPolicy.txt")));
        layout.Controls.Add(acceptPrivacy);
        return ScrollPage(layout);
    }

    private Control EnvironmentPage()
    {
        var layout = Page("Deployment settings", "Choose the environment and network address used to access eMilimo.");
        
        var deploymentRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, Width = 625, Padding = new Padding(0, 0, 0, 10) };
        deploymentRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        deploymentRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        deploymentRow.Controls.Add(new Label { Text = "Deployment mode:", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 8, 15, 0) }, 0, 0);
        deploymentRow.Controls.Add(deploymentMode, 1, 0);
        layout.Controls.Add(deploymentRow);

        var dbRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, Width = 625, Padding = new Padding(0, 0, 0, 10) };
        dbRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        dbRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        dbRow.Controls.Add(new Label { Text = "Database type:", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 8, 15, 0) }, 0, 0);
        dbRow.Controls.Add(databaseType, 1, 0);
        layout.Controls.Add(dbRow);
        
        layout.Controls.Add(fresh);
        layout.Controls.Add(existing);
        
        var connPanel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Width = 625, ColumnCount = 1 };
        connPanel.Controls.Add(connectionLabel, 0, 0);
        connection.Width = 610;
        connection.Height = 36;
        connection.Font = new Font("Segoe UI", 10);
        connPanel.Controls.Add(connection, 0, 1);
        connPanel.Controls.Add(connectionHint, 0, 2);
        layout.Controls.Add(connPanel);
        
        newEnvPanel.Controls.Clear();
        newEnvPanel.Padding = new Padding(0, 10, 0, 0);
        
        var hostRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Width = 625, ColumnCount = 2, Padding = new Padding(0, 5, 0, 5) };
        hostRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        hostRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hostRow.Controls.Add(new Label { Text = "Database host:", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 9), Margin = new Padding(0, 8, 10, 0) }, 0, 0);
        dbHost.Width = 480;
        dbHost.Height = 36;
        dbHost.Font = new Font("Segoe UI", 10);
        hostRow.Controls.Add(dbHost, 1, 0);
        newEnvPanel.Controls.Add(hostRow);
        
        var portRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Width = 625, ColumnCount = 2, Padding = new Padding(0, 5, 0, 5) };
        portRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        portRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        portRow.Controls.Add(new Label { Text = "Database port:", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 9), Margin = new Padding(0, 8, 10, 0) }, 0, 0);
        dbPort.Width = 480;
        dbPort.Height = 36;
        dbPort.Font = new Font("Segoe UI", 10);
        portRow.Controls.Add(dbPort, 1, 0);
        newEnvPanel.Controls.Add(portRow);
        
        var nameRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Width = 625, ColumnCount = 2, Padding = new Padding(0, 5, 0, 5) };
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        nameRow.Controls.Add(new Label { Text = "Database name:", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 9), Margin = new Padding(0, 8, 10, 0) }, 0, 0);
        dbName.Width = 480;
        dbName.Height = 36;
        dbName.Font = new Font("Segoe UI", 10);
        nameRow.Controls.Add(dbName, 1, 0);
        newEnvPanel.Controls.Add(nameRow);
        
        var userRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Width = 625, ColumnCount = 2, Padding = new Padding(0, 5, 0, 5) };
        userRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        userRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        userRow.Controls.Add(new Label { Text = "Database username:", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 9), Margin = new Padding(0, 8, 10, 0) }, 0, 0);
        dbUsername.Width = 480;
        dbUsername.Height = 36;
        dbUsername.Font = new Font("Segoe UI", 10);
        userRow.Controls.Add(dbUsername, 1, 0);
        newEnvPanel.Controls.Add(userRow);
        
        var passRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Width = 625, ColumnCount = 2, Padding = new Padding(0, 5, 0, 5) };
        passRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        passRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        passRow.Controls.Add(new Label { Text = "Database password:", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 9), Margin = new Padding(0, 8, 10, 0) }, 0, 0);
        dbPassword.Width = 480;
        dbPassword.Height = 36;
        dbPassword.Font = new Font("Segoe UI", 10);
        passRow.Controls.Add(dbPassword, 1, 0);
        newEnvPanel.Controls.Add(passRow);
        
        layout.Controls.Add(newEnvPanel);
        ToggleEnvironmentFields();

        var destinationRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, Width = 625, Padding = new Padding(0, 10, 0, 0) };
        destinationRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        destinationRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        
        destination.Width = 550;
        destination.Height = 36;
        destination.Font = new Font("Segoe UI", 10);
        destinationRow.Controls.Add(destination, 0, 0);
        
        var browse = new Button 
        { 
            Text = "...", 
            Dock = DockStyle.Fill, 
            MinimumSize = new Size(40, 36),
            BackColor = Color.White,
            ForeColor = Ink,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Margin = new Padding(4, 0, 0, 0)
        };
        browse.FlatAppearance.BorderColor = Line;
        browse.Click += (_, _) => 
        { 
            using var dialog = new FolderBrowserDialog 
            { 
                SelectedPath = destination.Text, 
                Description = "Choose the eMilimo installation folder" 
            }; 
            if (dialog.ShowDialog() == DialogResult.OK) 
            {
                destination.Text = dialog.SelectedPath;
                EnsureInstallationFolder();
            }
        };
        destinationRow.Controls.Add(browse, 1, 0);
        
        layout.Controls.Add(Field("Installation folder", destinationRow, "Recommended: C:\\inetpub\\wwwroot\\BAU.eMilimo. The folder will be created automatically if it does not exist."));
        layout.Controls.Add(Field("Public host name or IP address", publicHost, "Use the server name or LAN IP that other devices will use."));
        layout.Controls.Add(Field("eMilimo instance", instance, "A stable name for this installation, for example main or production."));
        layout.Controls.Add(Field("Congregation name", congregationName, "Required. This name is shown throughout eMilimo and is carried into first-run setup."));

        var licensePanel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Width = 625, ColumnCount = 1, Padding = new Padding(0, 8, 0, 8) };
        licensePanel.Controls.Add(new Label { Text = "Licence", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 0, 0, 6) });
        licensePanel.Controls.Add(trialLicense);
        licensePanel.Controls.Add(signedLicense);
        var licenseRow = new TableLayoutPanel { AutoSize = true, Width = 610, ColumnCount = 2, Margin = new Padding(22, 7, 0, 0) };
        licenseRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        licenseRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        licenseFilePath.Enabled = signedLicense.Checked;
        licenseRow.Controls.Add(licenseFilePath, 0, 0);
        var browseLicense = SecondaryButton("Browse…");
        browseLicense.Enabled = signedLicense.Checked;
        signedLicense.CheckedChanged += (_, _) => browseLicense.Enabled = signedLicense.Checked;
        browseLicense.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = "eMilimo licence (*.xml)|*.xml", CheckFileExists = true, Multiselect = false, Title = "Select signed eMilimo licence" };
            if (dialog.ShowDialog() != DialogResult.OK) return;
            licenseFilePath.Text = dialog.FileName;
            licenseSelectionStatus.Text = "Selected. The signature, system binding, and expiry will be validated before installation completes.";
        };
        licenseRow.Controls.Add(browseLicense, 1, 0);
        licensePanel.Controls.Add(licenseRow);
        licensePanel.Controls.Add(licenseSelectionStatus);
        layout.Controls.Add(licensePanel);

        adminPanel.Controls.Clear();
        adminPanel.Padding = new Padding(0, 8, 0, 8);
        var adminFields = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Width = 625, ColumnCount = 2 };
        adminFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        adminFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        adminFields.Controls.Add(Field("Title", adminTitle), 0, 0);
        adminFields.Controls.Add(Field("Administrator email", adminEmail), 1, 0);
        adminFields.Controls.Add(Field("First name", adminFirstName), 0, 1);
        adminFields.Controls.Add(Field("Surname", adminLastName), 1, 1);
        adminFields.Controls.Add(Field("Phone number", adminPhone, "Include country code where possible"), 0, 2);
        adminFields.Controls.Add(new Panel { Height = 1 }, 1, 2);
        adminFields.Controls.Add(Field("Administrator password", adminPassword, "Uppercase, lowercase, number and special character"), 0, 3);
        adminFields.Controls.Add(Field("Confirm password", adminPasswordConfirmation), 1, 3);
        var adminStack = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 1 };
        adminStack.Controls.Add(new Label { Text = "Default administrator", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 0, 0, 6) });
        adminStack.Controls.Add(new Label { Text = "Required for a new installation. This SuperAdmin account is created before setup finishes.", AutoSize = true, MaximumSize = new Size(610, 0), ForeColor = Muted, Margin = new Padding(0, 0, 0, 8) });
        adminStack.Controls.Add(adminFields);
        adminPanel.Controls.Add(adminStack);
        adminPanel.Visible = fresh.Checked;
        layout.Controls.Add(adminPanel);

        var ports = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, Width = 625, Padding = new Padding(0, 5, 0, 0) };
        ports.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        ports.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        
        var webPortField = new TableLayoutPanel { AutoSize = true, Width = 300, ColumnCount = 1 };
        webPortField.Controls.Add(new Label { Text = "Web portal port", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 0, 0, 6) });
        webPort.Width = 280;
        webPort.Height = 36;
        webPort.Font = new Font("Segoe UI", 10);
        webPortField.Controls.Add(webPort);
        webPortField.Controls.Add(webPortStatus);
        ports.Controls.Add(webPortField, 0, 0);
        
        var apiPortField = new TableLayoutPanel { AutoSize = true, Width = 300, ColumnCount = 1 };
        apiPortField.Controls.Add(new Label { Text = "API port", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 0, 0, 6) });
        apiPort.Width = 280;
        apiPort.Height = 36;
        apiPort.Font = new Font("Segoe UI", 10);
        apiPortField.Controls.Add(apiPort);
        apiPortField.Controls.Add(apiPortStatus);
        apiPortField.Visible = deploymentMode.SelectedItem?.ToString() == "Docker Container (Recommended)";
        ports.Controls.Add(apiPortField, 1, 0);
        
        layout.Controls.Add(ports);
        var checkPorts = SecondaryButton("Check port availability");
        checkPorts.MinimumSize = new Size(165, 34);
        checkPorts.Margin = new Padding(0, 8, 0, 0);
        checkPorts.Click += (_, _) => UpdatePortAvailability();
        layout.Controls.Add(checkPorts);
        UpdatePortAvailability();
        return ScrollPage(layout);
    }

    private Control ComponentsPage()
    {
        var layout = Page("Configure cache", "Choose the cache arrangement for this installation. The managed open-source option is recommended with Docker.");
        var deployMode = deploymentMode.SelectedItem?.ToString() ?? "Docker Container (Recommended)";
        if (deployMode != "Docker Container (Recommended)" && CacheModeIsBundled()) cacheMode.SelectedIndex = 2;
        cache.Checked = CacheModeEnabled();
        layout.Controls.Add(Field("Cache mode", cacheMode, deployMode == "Docker Container (Recommended)"
            ? "Managed Valkey runs inside Docker. You may also use an existing Redis-compatible endpoint."
            : "Use an existing Redis-compatible endpoint or the safe single-node in-memory fallback."));

        if (CacheModeIsExisting())
        {
            var endpoint = ResponsiveGrid(2, 72, 28);
            endpoint.Controls.Add(Field("Host", cacheHost), 0, 0);
            endpoint.Controls.Add(Field("Port", cachePort), 1, 0);
            var credentials = ResponsiveGrid(2, 58, 42);
            credentials.Controls.Add(Field("Username (optional)", cacheUsername), 0, 0);
            credentials.Controls.Add(Field("Database", cacheDatabase), 1, 0);
            layout.Controls.Add(endpoint);
            layout.Controls.Add(credentials);
            layout.Controls.Add(Field("Password (optional)", cachePassword));
            layout.Controls.Add(cacheTls);
            var testRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 7, 0, 8) };
            var test = SecondaryButton("Test connection");
            test.Click += async (_, _) => await TestCacheConnectionFromUiAsync();
            testRow.Controls.Add(test);
            testRow.Controls.Add(cacheTestStatus);
            layout.Controls.Add(testRow);
        }

        var options = SectionCard("Installation options", "Optional shortcuts and detected prerequisites.");
        options.Visible = false;
        options.Controls.Add(shortcut);
        options.Controls.Add(launch);
        options.Controls.Add(new Label { Text = BundledPrerequisiteSummary(), AutoSize = true, ForeColor = Muted, Margin = new Padding(0, 6, 0, 0) });
        var toggle = LinkButton("Show installation options");
        toggle.Click += (_, _) => { options.Visible = !options.Visible; toggle.Text = options.Visible ? "Hide installation options" : "Show installation options"; };
        layout.Controls.Add(toggle);
        layout.Controls.Add(options);

        database.Checked = fresh.Checked;
        docker.Checked = deployMode == "Docker Container (Recommended)";
        hostingBundle.Checked = deployMode == "IIS Windows Service";
        return ScrollPage(layout);
    }

    private Control LegacyComponentsPage()
    {
        var layout = Page("Choose components", "Choose application prerequisites and the supported open-source cache deployment mode.");
        var deployMode = deploymentMode.SelectedItem?.ToString() ?? "Docker Container (Recommended)";
        database.Enabled = fresh.Checked && deployMode != "Docker Container (Recommended)";
        database.Checked = fresh.Checked;
        database.Text = fresh.Checked
            ? "PostgreSQL database   ·   Install a new local database"
            : "PostgreSQL database   ·   Use the existing connection";
        if (deployMode != "Docker Container (Recommended)" && CacheModeIsBundled())
            cacheMode.SelectedIndex = 2;
        cache.Checked = CacheModeEnabled();
        hostingBundle.Visible = deployMode == "IIS Windows Service";
        hostingBundle.Checked = hostingBundle.Visible;
        hostingBundle.Enabled = hostingBundle.Visible;
        
        foreach (var item in new[] { core, database, docker, hostingBundle, trial, shortcut, launch })
        {
            if (item == docker)
            {
                docker.Text = deploymentMode.SelectedItem?.ToString() == "Docker Container (Recommended)" 
                    ? "Docker container runtime (Required for Docker deployment)" 
                    : "Docker container runtime (Not required for non-container deployment)";
                docker.Enabled = deploymentMode.SelectedItem?.ToString() == "Docker Container (Recommended)";
                docker.Checked = deploymentMode.SelectedItem?.ToString() == "Docker Container (Recommended)";
            }
            if (!item.Visible) continue;
            layout.Controls.Add(item);
        }

        var cachePanel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Width = 625, ColumnCount = 1, Padding = new Padding(0, 12, 0, 8) };
        cachePanel.Controls.Add(new Label { Text = "Distributed cache", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 0, 0, 6) });
        cachePanel.Controls.Add(cacheMode);
        cachePanel.Controls.Add(new Label
        {
            Text = deployMode == "Docker Container (Recommended)"
                ? "Valkey 8.1.10 runs inside Docker, or connect to an existing Redis-compatible endpoint. The cache is optional for single-node eMilimo."
                : "Native Windows cache packages are not bundled. Connect to a supported Redis/Valkey endpoint, or use the safe in-memory fallback.",
            AutoSize = true, MaximumSize = new Size(610, 0), ForeColor = Muted, Margin = new Padding(0, 5, 0, 8)
        });

        if (CacheModeIsExisting())
        {
            var endpoint = new TableLayoutPanel { AutoSize = true, Width = 610, ColumnCount = 2 };
            endpoint.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            endpoint.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            endpoint.Controls.Add(Field("Host", cacheHost, "Server name or IP address"), 0, 0);
            endpoint.Controls.Add(Field("Port", cachePort, "Usually 6379 or 6380 for TLS"), 1, 0);
            endpoint.Controls.Add(Field("Username (optional)", cacheUsername, "ACL username"), 0, 1);
            endpoint.Controls.Add(Field("Database", cacheDatabase, "Logical database number"), 1, 1);
            endpoint.Controls.Add(Field("Password (optional)", cachePassword, "Stored only in the protected application configuration"), 0, 2);
            endpoint.SetColumnSpan(endpoint.GetControlFromPosition(0, 2)!, 2);
            cachePanel.Controls.Add(endpoint);
            cachePanel.Controls.Add(cacheTls);
            var testRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 8, 0, 0) };
            var test = SecondaryButton("Test connection");
            test.Click += async (_, _) => await TestCacheConnectionFromUiAsync();
            testRow.Controls.Add(test);
            testRow.Controls.Add(cacheTestStatus);
            cachePanel.Controls.Add(testRow);
        }
        layout.Controls.Add(cachePanel);

        layout.Controls.Add(InfoCard("Prerequisite sources", BundledPrerequisiteSummary()));
        
        return ScrollPage(layout);
    }

    private Control ReviewPage()
    {
        var layout = Page("Ready to install", "Review the deployment details, then install eMilimo.");
        var mode = fresh.Checked ? "New PostgreSQL environment" : "Existing PostgreSQL environment";
        var deployMode = deploymentMode.SelectedItem?.ToString() ?? "Docker Container";
        var dbType = databaseType.SelectedItem?.ToString() ?? "PostgreSQL";
        
        var separateApi = deployMode == "Docker Container (Recommended)";
        var details = $"Folder: {destination.Text.Trim()}\n" +
                      $"Deployment: {deployMode}\n" +
                      $"Database: {dbType}\n" +
                      $"Congregation: {congregationName.Text.Trim()}\n" +
                      $"Environment: {mode}\n" +
                      $"Administration: {AdminBaseUrl()}\n" +
                      $"Public Giving: {GivingBaseUrl()}\n" +
                      $"API: {ApiBaseUrl(deployMode)}" +
                      (separateApi ? $" (local port {apiPort.Value})" : " (shared administration listener)");
        
        if (fresh.Checked)
        {
            details += $"\nDB Host: {dbHost.Text}\nDB Name: {dbName.Text}\nDefault administrator: {adminEmail.Text.Trim()}";
        }
        
        layout.Controls.Add(InfoCard("Installation", details));
        
        var components = "eMilimo web portal and API";
        components += "\n" + (database.Checked ? "PostgreSQL" : "External database");
        components += "\n" + (cache.Checked ? "Redis distributed cache" : "No cache");
        components += "\n" + (docker.Checked && deployMode == "Docker Container (Recommended)" ? "Docker containers" : "Native Windows service");
        if (deployMode == "IIS Windows Service") components += "\nASP.NET Core IIS Hosting Bundle";
        components += "\n" + (trialLicense.Checked ? "30-day trial licence" : $"Signed licence: {Path.GetFileName(licenseFilePath.Text)}");
        components += shortcut.Checked ? "\nDesktop shortcut" : "";
        
        layout.Controls.Add(InfoCard("Components", components));
        return ScrollPage(layout);
    }

    private Control InstallProgressPage()
    {
        var layout = Page("Installing eMilimo", "Setup is working through each step. Live activity and recovery controls remain available below.");

        var stateRow = new TableLayoutPanel { Width = 620, Height = 32, ColumnCount = 2, Margin = new Padding(0, 0, 0, 6) };
        stateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stateRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var state = new Label { Text = "INSTALLATION IN PROGRESS", AutoSize = true, ForeColor = Forest,
            Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.FromArgb(232, 244, 238), Padding = new Padding(10, 6, 10, 6) };
        elapsedLabel = new Label { Text = "Elapsed 00:00", AutoSize = true, ForeColor = Muted, Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 7, 0, 0) };
        stateRow.Controls.Add(state, 0, 0);
        stateRow.Controls.Add(elapsedLabel, 1, 0);
        layout.Controls.Add(stateRow);
        
        progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 22,
            Width = 620,
            Style = ProgressBarStyle.Continuous,
            Margin = new Padding(0, 10, 0, 15)
        };
        layout.Controls.Add(progressBar);
        
        progressLabel = new Label
        {
            Text = "Preparing installation...",
            AutoSize = true,
            ForeColor = Ink,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 20)
        };
        layout.Controls.Add(progressLabel);
        
        milestonePanel = new TableLayoutPanel
        {
            AutoSize = true,
            Width = 620,
            ColumnCount = 1,
            Padding = new Padding(0)
        };
        
        milestones.Clear();
        milestones.Add(new Milestone { Text = "Validating system requirements (10%)", Status = MilestoneStatus.Pending });
        milestones.Add(new Milestone { Text = "Copying prepared application files (40%)", Status = MilestoneStatus.Pending });
        milestones.Add(new Milestone { Text = "Configuring database (60%)", Status = MilestoneStatus.Pending });
        milestones.Add(new Milestone { Text = "Setting up application services (80%)", Status = MilestoneStatus.Pending });
        milestones.Add(new Milestone { Text = "Creating desktop shortcuts (90%)", Status = MilestoneStatus.Pending });
        milestones.Add(new Milestone { Text = "Finalizing installation (100%)", Status = MilestoneStatus.Pending });
        
        foreach (var milestone in milestones)
        {
            var row = CreateMilestoneRow(milestone);
            milestonePanel.Controls.Add(row);
        }
        
        layout.Controls.Add(milestonePanel);

        var logHeader = new TableLayoutPanel { Width = 620, Height = 30, ColumnCount = 2, Margin = new Padding(0, 16, 0, 4) };
        logHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        logHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        logHeader.Controls.Add(new Label { Text = "Installation activity", AutoSize = true, ForeColor = Ink,
            Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 0);
        var openLog = SecondaryButton("Open log folder");
        openLog.MinimumSize = new Size(115, 30);
        openLog.Click += (_, _) =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(installerLogPath)!);
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{installerLogPath}\"") { UseShellExecute = true });
        };
        logHeader.Controls.Add(openLog, 1, 0);
        layout.Controls.Add(logHeader);

        progressLog = new RichTextBox
        {
            Width = 620, Height = 145, ReadOnly = true, DetectUrls = false, BackColor = Color.FromArgb(247, 249, 248),
            ForeColor = Color.FromArgb(48, 58, 53), BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Cascadia Mono", 8.5f), ScrollBars = RichTextBoxScrollBars.Vertical
        };
        layout.Controls.Add(progressLog);
        
        installCompleteButton = new Button
        {
            Text = "Launch eMilimo",
            AutoSize = true,
            MinimumSize = new Size(150, 40),
            BackColor = Forest,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 20, 0, 0),
            Visible = false
        };
        installCompleteButton.FlatAppearance.BorderSize = 0;
        installCompleteButton.Click += async (_, _) =>
        {
            if (!installationSucceeded)
            {
                RenderPage();
                await InstallWithProgressAsync();
                return;
            }
            if (launch.Checked) Process.Start(new ProcessStartInfo(AdminBaseUrl()) { UseShellExecute = true });
            Close();
        };
        layout.Controls.Add(installCompleteButton);
        
        return ScrollPage(layout);
    }

    private Control CompletionPage()
    {
        var layout = Page("eMilimo is ready", "Installation completed successfully and the application passed its readiness check.");
        var check = new Label
        {
            Text = "✓",
            AutoSize = false,
            Width = 72,
            Height = 72,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(226, 244, 235),
            ForeColor = Success,
            Font = new Font("Segoe UI", 32, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 18)
        };
        layout.Controls.Add(check);
        layout.Controls.Add(InfoCard("Installation complete",
            $"Administration: {AdminBaseUrl()}\n" +
            $"Public Giving: {GivingBaseUrl()}\n" +
            $"Deployment: {deploymentMode.SelectedItem}\n" +
            $"Installed to: {destination.Text.Trim()}\n" +
            (fresh.Checked
                ? $"Administrator: {adminEmail.Text.Trim()}\n\nThe installation is configured and ready for sign-in."
                : "\nThe setup authorization code is on your clipboard for database adoption.")));

        var actions = new FlowLayoutPanel { AutoSize = true, Width = 620, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 8, 0, 12) };
        var launchButton = PrimaryButton("Open eMilimo");
        launchButton.Click += (_, _) => Process.Start(new ProcessStartInfo(AdminBaseUrl()) { UseShellExecute = true });
        var copyButton = SecondaryButton("Copy setup code");
        copyButton.Click += (_, _) => Clipboard.SetText(installationSetupToken);
        copyButton.Visible = !fresh.Checked;
        var logButton = SecondaryButton("Open diagnostic log");
        logButton.Click += (_, _) => Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{installerLogPath}\"") { UseShellExecute = true });
        actions.Controls.AddRange([launchButton, copyButton, logButton]);
        layout.Controls.Add(actions);
        layout.Controls.Add(new Label { Text = "Select Close to exit setup.", AutoSize = true, ForeColor = Muted, Margin = new Padding(0, 8, 0, 0) });
        return ScrollPage(layout);
    }

    private static Version NormalizeVersion(Version version) => new(
        Math.Max(version.Major, 0),
        Math.Max(version.Minor, 0),
        Math.Max(version.Build, 0));

    private Control MaintenancePage()
    {
        var product = installedProduct!;
        // File versions commonly include a fourth revision component (1.0.0.0),
        // while the product version is intentionally three-part (1.0.0).
        var comparison = NormalizeVersion(CurrentVersion).CompareTo(NormalizeVersion(product.Version));
        var title = comparison switch
        {
            > 0 => "A newer eMilimo version is ready",
            < 0 => "A newer version is already installed",
            _ => "eMilimo is already installed"
        };
        var explanation = comparison switch
        {
            > 0 => $"Installed: {product.Version}   •   Setup package: {CurrentVersion}. Upgrade preserves the database and application data.",
            < 0 => $"Installed: {product.Version}   •   Setup package: {CurrentVersion}. Downgrading is blocked to protect the database and configuration.",
            _ => $"Version {product.Version} is installed. Choose a maintenance action below."
        };
        var layout = Page(title, explanation);
        layout.Controls.Add(InfoCard("Installed product",
            $"Location: {product.InstallLocation}\nDeployment: {product.DeploymentMode}\nAddress: http://{product.Host}:{product.WebPort}"));

        var actions = new FlowLayoutPanel { AutoSize = true, Width = 620, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        if (comparison > 0)
        {
            var upgrade = MaintenanceButton("Upgrade eMilimo", "Install the newer version while preserving configuration and data.", true);
            upgrade.Click += async (_, _) => await BeginMaintenanceInstallAsync(restoreFiles: false);
            actions.Controls.Add(upgrade);
        }
        if (comparison >= 0)
        {
            var repair = MaintenanceButton("Repair", "Restore application files, IIS configuration, permissions, and services without deleting data.");
            repair.Click += async (_, _) => await BeginMaintenanceInstallAsync(restoreFiles: true);
            var modify = MaintenanceButton("Modify", "Change ports, deployment components, shortcuts, and optional services.");
            modify.Click += (_, _) => { maintenanceSession = true; page = 1; RenderPage(); };
            actions.Controls.Add(repair);
            actions.Controls.Add(modify);
        }
        else
        {
            actions.Controls.Add(InfoCard("Downgrade blocked", "Use a setup package matching or newer than the installed version. You can still launch or uninstall the current version."));
        }

        var launchButton = MaintenanceButton("Open eMilimo", "Launch the currently installed application.");
        launchButton.Click += (_, _) => Process.Start(new ProcessStartInfo($"http://{product.Host}:{product.WebPort}") { UseShellExecute = true });
        var uninstallButton = MaintenanceButton("Uninstall", "Remove eMilimo application files and Windows registration. Data is preserved unless explicitly removed.");
        uninstallButton.ForeColor = Danger;
        uninstallButton.Click += async (_, _) => await UninstallAsync(product);
        actions.Controls.Add(launchButton);
        actions.Controls.Add(uninstallButton);
        layout.Controls.Add(actions);

        if (startupArgs.Any(arg => arg.Equals("/uninstall", StringComparison.OrdinalIgnoreCase)))
            layout.Controls.Add(new Label { Text = "Uninstall was requested from Windows Settings. Select Uninstall above to confirm.", AutoSize = true, ForeColor = Gold, Margin = new Padding(0, 12, 0, 0) });
        return ScrollPage(layout);
    }

    private static Button MaintenanceButton(string title, string description, bool primary = false)
    {
        var button = primary ? PrimaryButton($"{title}    {description}") : SecondaryButton($"{title}    {description}");
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.AutoSize = false;
        button.Width = 610;
        button.Height = 48;
        button.Margin = new Padding(0, 0, 0, 10);
        return button;
    }

    private async Task BeginMaintenanceInstallAsync(bool restoreFiles)
    {
        maintenanceSession = true;
        restoreFromRepairCache = restoreFiles;
        page = 6;
        RenderPage();
        await InstallWithProgressAsync();
    }

    private Control CreateMilestoneRow(Milestone milestone)
    {
        var row = new TableLayoutPanel
        {
            AutoSize = true,
            Width = 620,
            ColumnCount = 2,
            Padding = new Padding(0, 5, 0, 5),
            Margin = new Padding(0, 2, 0, 2),
            BackColor = Color.Transparent
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        
        var icon = new Label
        {
            Text = "○",
            AutoSize = true,
            ForeColor = Muted,
            Font = new Font("Segoe UI", 12),
            TextAlign = ContentAlignment.MiddleCenter
        };
        row.Controls.Add(icon, 0, 0);
        
        var text = new Label
        {
            Text = milestone.Text,
            AutoSize = true,
            ForeColor = Ink,
            Font = new Font("Segoe UI", 10),
            Padding = new Padding(5, 0, 0, 0)
        };
        row.Controls.Add(text, 1, 0);
        
        milestone.Control = row;
        milestone.Icon = icon;
        milestone.TextControl = text;
        
        return row;
    }

    private void UpdateMilestone(int index, MilestoneStatus status)
    {
        if (milestonePanel == null || index >= milestones.Count) return;
        
        var milestone = milestones[index];
        milestone.Status = status;
        
        if (milestone.Icon != null)
        {
            switch (status)
            {
                case MilestoneStatus.Pending:
                    milestone.Icon.Text = "○";
                    milestone.Icon.ForeColor = Muted;
                    break;
                case MilestoneStatus.InProgress:
                    milestone.Icon.Text = "●";
                    milestone.Icon.ForeColor = Forest;
                    break;
                case MilestoneStatus.Completed:
                    milestone.Icon.Text = "✓";
                    milestone.Icon.ForeColor = Success;
                    break;
                case MilestoneStatus.Failed:
                    milestone.Icon.Text = "✗";
                    milestone.Icon.ForeColor = Danger;
                    break;
            }
        }
        
        if (milestone.TextControl != null)
        {
            milestone.TextControl.ForeColor = status == MilestoneStatus.Completed ? Success : status == MilestoneStatus.Failed ? Danger : Ink;
            if (status == MilestoneStatus.Completed)
                milestone.TextControl.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            else
                milestone.TextControl.Font = new Font("Segoe UI", 10);
        }
        
        milestonePanel?.Refresh();
    }

    private async void NextClicked(object? sender, EventArgs e)
    {
        inlineMessage.Text = string.Empty;
        if (page == 0 && (!acceptTerms.Checked || !acceptPrivacy.Checked))
        {
            Warn("Accept the Terms and Conditions and Privacy Policy to continue.");
            return;
        }
        if (page == 1 && string.IsNullOrWhiteSpace(congregationName.Text))
        {
            Warn("Enter the congregation name.");
            congregationName.Focus();
            return;
        }
        if (page == 2)
        {
            if (string.IsNullOrWhiteSpace(adminFirstName.Text) || string.IsNullOrWhiteSpace(adminLastName.Text) ||
                !System.Net.Mail.MailAddress.TryCreate(adminEmail.Text.Trim(), out _))
            {
                Warn("Enter the administrator’s first name, surname, and a valid email address.");
                return;
            }
            if (!StrongPassword(adminPassword.Text))
            {
                Warn("Use at least 8 characters with uppercase, lowercase, a number, and a special character.");
                adminPassword.Focus();
                return;
            }
            if (adminPassword.Text != adminPasswordConfirmation.Text)
            {
                Warn("The administrator passwords do not match.");
                adminPasswordConfirmation.Focus();
                return;
            }
            UpdatePortAvailability();
            if (!PortIsAvailableForInstall((int)webPort.Value, deploymentMode.SelectedItem?.ToString() ?? string.Empty))
            {
                Warn($"Port {webPort.Value} is already in use. Choose another web portal port.");
                return;
            }
        }
        if (page == 4 && !await ValidateCacheSettingsAsync()) return;
        if (page == 5)
        {
            if (!await ValidateSettingsAsync() || !await ValidateCacheSettingsAsync()) return;
            page = 6;
            RenderPage();
            await InstallWithProgressAsync();
            return;
        }
        if (page < 5) { page++; RenderPage(); }
    }

    private async Task<bool> ValidateSettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(destination.Text)) 
        { 
            Warn("Choose an installation folder."); 
            return false; 
        }
        
        try
        {
            var path = destination.Text.Trim();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception ex)
        {
            Warn($"Could not create installation folder: {ex.Message}");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(publicHost.Text)) 
        { 
            Warn("Enter the public host name or IP address."); 
            return false; 
        }
        foreach (var candidate in new[]
                 {
                     (Name: "administration", Value: publicAdminUrl.Text),
                     (Name: "API", Value: publicApiUrl.Text),
                     (Name: "Giving", Value: publicGivingUrl.Text)
                 })
        {
            if (string.IsNullOrWhiteSpace(candidate.Value)) continue;
            if (!Uri.TryCreate(candidate.Value.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            {
                Warn($"Enter a valid absolute HTTP or HTTPS public {candidate.Name} URL without a query or fragment.");
                return false;
            }
        }
        if (string.IsNullOrWhiteSpace(congregationName.Text))
        {
            Warn("Enter the congregation name.");
            return false;
        }
        if (signedLicense.Checked)
        {
            if (string.IsNullOrWhiteSpace(licenseFilePath.Text) || !File.Exists(licenseFilePath.Text))
            {
                Warn("Select the signed BAU licence XML file.");
                return false;
            }
            try
            {
                var info = new FileInfo(licenseFilePath.Text);
                if (info.Length is 0 or > 1048576 || !info.Extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The licence must be a non-empty XML file no larger than 1 MB.");
                _ = System.Xml.Linq.XDocument.Load(info.FullName, System.Xml.Linq.LoadOptions.None);
            }
            catch (Exception ex)
            {
                Warn($"The selected licence file could not be read: {ex.Message}");
                return false;
            }
        }
        if (fresh.Checked)
        {
            if (string.IsNullOrWhiteSpace(adminFirstName.Text) || string.IsNullOrWhiteSpace(adminLastName.Text) ||
                !System.Net.Mail.MailAddress.TryCreate(adminEmail.Text.Trim(), out _))
            {
                Warn("Enter the default administrator's first name, surname, and a valid email address.");
                return false;
            }
            var password = adminPassword.Text;
            if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
                !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                Warn("The administrator password must contain at least 8 characters, including uppercase, lowercase, a number, and a special character.");
                return false;
            }
            if (!string.Equals(password, adminPasswordConfirmation.Text, StringComparison.Ordinal))
            {
                Warn("The administrator passwords do not match.");
                return false;
            }
        }
        
        var deployMode = deploymentMode.SelectedItem?.ToString() ?? "Docker Container (Recommended)";
        if (webPort.Value == givingPort.Value ||
            (deployMode == "Docker Container (Recommended)" && (webPort.Value == apiPort.Value || apiPort.Value == givingPort.Value)))
        { 
            Warn("The administration portal, API, and public Giving application must use different ports."); 
            return false; 
        }
        if (!PortIsAvailableForInstall((int)webPort.Value, deployMode))
        {
            Warn($"Port {webPort.Value} is already being used by another application. Choose an available web port and try again.");
            UpdatePortAvailability();
            return false;
        }
        if (deployMode == "Docker Container (Recommended)" && !PortIsAvailableForInstall((int)apiPort.Value, deployMode))
        {
            Warn($"Port {apiPort.Value} is already being used by another application. Choose an available API port and try again.");
            UpdatePortAvailability();
            return false;
        }
        if (!PortIsAvailableForInstall((int)givingPort.Value, deployMode))
        {
            Warn($"Port {givingPort.Value} is already being used by another application. Choose an available public Giving port and try again.");
            UpdatePortAvailability();
            return false;
        }
        
        if (existing.Checked)
        {
            if (string.IsNullOrWhiteSpace(connection.Text))
            {
                Warn("Enter the existing PostgreSQL connection string.");
                return false;
            }
            
            try
            {
                status.Text = "Validating database connection...";
                await Task.Delay(100);
                
                using var conn = new NpgsqlConnection(BoundedConnectionString(connection.Text.Trim()));
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                await conn.OpenAsync(timeout.Token);
                
                var dbName = new NpgsqlConnectionStringBuilder(connection.Text.Trim()).Database;
                if (string.IsNullOrEmpty(dbName))
                {
                    Warn("Database name not specified in connection string.");
                    return false;
                }
                
                status.Text = "Database connection successful!";
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Warn($"Cannot connect to database: {ex.Message}\n\nPlease verify the connection string and ensure the database server is running.");
                return false;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dbHost.Text))
            {
                Warn("Enter the database host.");
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(dbName.Text))
            {
                Warn("Enter the database name.");
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(dbUsername.Text))
            {
                Warn("Enter the database username.");
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(dbPassword.Text))
            {
                Warn("Enter the database password.");
                return false;
            }
            
            if (deployMode == "Docker Container (Recommended)")
            {
                status.Text = "The managed PostgreSQL container will be created during installation.";
            }
            else if (IsTcpPortAvailable((int)dbPort.Value) && HasAvailablePrerequisite("postgresql", "postgresql*.exe"))
            {
                status.Text = HasBundledPrerequisite("postgresql*.exe")
                    ? "The bundled PostgreSQL service will be installed during setup."
                    : "PostgreSQL will be downloaded, integrity-checked, and installed during setup.";
            }
            else try
            {
                status.Text = "Validating database credentials...";
                await Task.Delay(100);
                
                var adminConnection = new NpgsqlConnectionStringBuilder
                {
                    Host = dbHost.Text.Trim(),
                    Port = (int)dbPort.Value,
                    Database = "postgres",
                    Username = dbUsername.Text.Trim(),
                    Password = dbPassword.Text
                }.ConnectionString;
                var connString = adminConnection;
                using var conn = new NpgsqlConnection(BoundedConnectionString(connString));
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                await conn.OpenAsync(timeout.Token);
                
                var checkDbConnString = new NpgsqlConnectionStringBuilder(adminConnection)
                {
                    Database = dbName.Text.Trim()
                }.ConnectionString;
                try
                {
                    using var checkConn = new NpgsqlConnection(BoundedConnectionString(checkDbConnString));
                    await checkConn.OpenAsync(timeout.Token);
                    status.Text = "Database exists and credentials are valid!";
                }
                catch (Npgsql.PostgresException ex) when (ex.SqlState == "3D000")
                {
                    using var createConn = new NpgsqlConnection(BoundedConnectionString(adminConnection));
                    await createConn.OpenAsync(timeout.Token);
                    var quotedDatabaseName = dbName.Text.Trim().Replace("\"", "\"\"");
                    using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{quotedDatabaseName}\"", createConn);
                    await cmd.ExecuteNonQueryAsync(timeout.Token);
                    status.Text = "Database created successfully!";
                }
                
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Warn($"Cannot connect to database server: {ex.Message}\n\nPlease verify the host, port, username, and password.");
                return false;
            }
        }
        
        if (!maintenanceSession && !HasPreparedPayload())
        {
            Warn("The prepared eMilimo payload is missing. Recreate the installer package with the published backend and frontend files.");
            return false;
        }

        if (deployMode == "Standalone Console" && !await HasDotnet8RuntimeAsync() &&
            !HasAvailablePrerequisite("dotnet-hosting", "dotnet-hosting*.exe"))
        {
            Warn("The .NET 8 ASP.NET Core Runtime is required for standalone deployment and is not available to this installer.");
            return false;
        }

        if (deployMode == "Docker Container (Recommended)" && docker.Checked &&
            !await CommandWorks("docker", "version --format {{.Server.Version}}", packageRoot!, TimeSpan.FromSeconds(20)) &&
            !HasBundledPrerequisite("Docker Desktop Installer.exe"))
        {
            Warn("Docker is not running and its offline installer is not present in this package. Start Docker or use a fully bundled installer package.");
            return false;
        }

        if (deployMode == "Docker Container (Recommended)" && !docker.Checked)
        {
            Warn("Docker is required for containerized deployment. Please select Docker in components or choose a different deployment mode.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(instance.Text))
        {
            Warn("Enter the eMilimo instance name.");
            return false;
        }
        
        // Check IIS if deploying to IIS
        if (deployMode == "IIS Windows Service")
        {
            try
            {
                using var iisManager = new ServerManager();
                _ = iisManager.Sites;
            }
            catch (Exception ex)
            {
                status.Text = $"IIS components will be enabled during installation ({ex.Message}).";
            }
        }
        
        status.Text = "All settings validated!";
        return true;
    }

    private bool CacheModeIsBundled() => cacheMode.SelectedItem?.ToString()?.StartsWith("Managed", StringComparison.Ordinal) == true;
    private bool CacheModeIsExisting() => cacheMode.SelectedItem?.ToString()?.StartsWith("Use existing", StringComparison.Ordinal) == true;
    private bool CacheModeEnabled() => CacheModeIsBundled() || CacheModeIsExisting();

    private async Task<bool> ValidateCacheSettingsAsync()
    {
        var deployMode = deploymentMode.SelectedItem?.ToString() ?? string.Empty;
        if (CacheModeIsBundled() && deployMode != "Docker Container (Recommended)")
        {
            Warn("The bundled open-source Valkey runtime requires Docker. Choose Docker deployment, an existing Redis-compatible server, or no distributed cache.");
            return false;
        }
        if (!CacheModeIsExisting()) return true;
        if (string.IsNullOrWhiteSpace(cacheHost.Text))
        {
            Warn("Enter the Redis-compatible server host name or IP address.");
            return false;
        }
        if (cacheUsername.Text.Contains(',') || cachePassword.Text.Contains(','))
        {
            Warn("The cache username and password cannot contain commas because commas delimit Redis connection-string options.");
            return false;
        }
        return await TestCacheConnectionFromUiAsync(showSuccessDialog: false);
    }

    private async Task<bool> TestCacheConnectionFromUiAsync(bool showSuccessDialog = false)
    {
        cacheTestStatus.Text = "Testing...";
        cacheTestStatus.ForeColor = Muted;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await TestRedisProtocolAsync(cacheHost.Text.Trim(), (int)cachePort.Value,
                cacheUsername.Text.Trim(), cachePassword.Text, cacheTls.Checked,
                (int)cacheDatabase.Value, timeout.Token);
            cacheTestStatus.Text = "Connection successful";
            cacheTestStatus.ForeColor = Success;
            if (showSuccessDialog)
                MessageBox.Show("The Redis-compatible server responded successfully.", "Connection successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }
        catch (Exception ex)
        {
            cacheTestStatus.Text = "Connection failed";
            cacheTestStatus.ForeColor = Danger;
            Warn($"Cannot connect to the Redis-compatible server: {ex.Message}");
            return false;
        }
    }

    private static async Task TestRedisProtocolAsync(string host, int port, string username, string password, bool tls, int database, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken);
        Stream stream = client.GetStream();
        if (tls)
        {
            var ssl = new SslStream(stream, leaveInnerStreamOpen: false);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = host }, cancellationToken);
            stream = ssl;
        }
        await using (stream)
        {
            if (!string.IsNullOrEmpty(password))
            {
                var auth = string.IsNullOrEmpty(username) ? new[] { "AUTH", password } : new[] { "AUTH", username, password };
                await SendRedisCommandAsync(stream, auth, cancellationToken);
            }
            if (database > 0)
                await SendRedisCommandAsync(stream, new[] { "SELECT", database.ToString() }, cancellationToken);
            var pong = await SendRedisCommandAsync(stream, new[] { "PING" }, cancellationToken);
            if (!pong.Equals("PONG", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unexpected PING response: {pong}");
        }
    }

    private static async Task<string> SendRedisCommandAsync(Stream stream, IReadOnlyList<string> values, CancellationToken cancellationToken)
    {
        var payload = new StringBuilder($"*{values.Count}\r\n");
        foreach (var value in values)
        {
            var byteCount = Encoding.UTF8.GetByteCount(value);
            payload.Append('$').Append(byteCount).Append("\r\n").Append(value).Append("\r\n");
        }
        var bytes = Encoding.UTF8.GetBytes(payload.ToString());
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        var prefix = new byte[1];
        if (await stream.ReadAsync(prefix, cancellationToken) != 1)
            throw new IOException("The server closed the connection without a response.");
        var line = new List<byte>();
        var current = new byte[1];
        while (true)
        {
            if (await stream.ReadAsync(current, cancellationToken) != 1)
                throw new IOException("The server returned an incomplete response.");
            if (current[0] == '\r')
            {
                if (await stream.ReadAsync(current, cancellationToken) != 1 || current[0] != '\n')
                    throw new IOException("The server returned an invalid response.");
                break;
            }
            line.Add(current[0]);
        }
        var response = Encoding.UTF8.GetString(line.ToArray());
        if (prefix[0] == '-') throw new InvalidOperationException(response);
        if (prefix[0] != (byte)'+' && prefix[0] != (byte)':') throw new InvalidOperationException("The endpoint did not return a Redis protocol response.");
        return response;
    }

    private void EnsureInstallationFolder()
    {
        try
        {
            var path = destination.Text.Trim();
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create directory: {ex.Message}");
        }
    }

    private async Task InstallWithProgressAsync()
    {
        if (packageRoot == null)
        {
            MessageBox.Show("The installer payload is unavailable. Place the setup executable beside the eMilimo deployment package and run it again.", "Installation package", MessageBoxButtons.OK, MessageBoxIcon.Error);
            page = 5;
            RenderPage();
            return;
        }

        installationCancellation?.Dispose();
        installationCancellation = new CancellationTokenSource();
        var cancellationToken = installationCancellation.Token;
        installationSucceeded = false;
        installing = true;
        installationTimer.Restart();
        elapsedTimer.Start();
        back.Enabled = false;
        cancel.Enabled = true;
        cancel.Text = "Cancel installation";
        next.Enabled = false;
        Directory.CreateDirectory(Path.GetDirectoryName(installerLogPath)!);
        await File.AppendAllTextAsync(installerLogPath, $"{Environment.NewLine}=== eMilimo setup {DateTimeOffset.Now:O} ==={Environment.NewLine}", cancellationToken);
        AppendLog("Starting installation and pre-flight checks.");
        
        try
        {
            var deployMode = deploymentMode.SelectedItem?.ToString() ?? "Docker Container (Recommended)";
            
            // Milestone 1: Validating system requirements
            UpdateMilestone(0, MilestoneStatus.InProgress);
            progressLabel!.Text = "Validating system requirements...";
            progressBar!.Value = 10;
            AppendLog($"Deployment mode: {deployMode}");

            await InstallSelectedPrerequisitesAsync(deployMode, cancellationToken);
            
            if (deployMode == "Docker Container (Recommended)" && docker.Checked)
            {
                if (!await CommandWorks("docker", "version --format {{.Server.Version}}", packageRoot, TimeSpan.FromSeconds(20), cancellationToken))
                {
                    throw new InvalidOperationException("Docker is not available. Install or start Docker Desktop, then retry, or select a different deployment mode.");
                }
            }
            UpdateMilestone(0, MilestoneStatus.Completed);
            progressBar.Value = 20;
            
            // Milestone 2: Copying application files
            UpdateMilestone(1, MilestoneStatus.InProgress);
            progressLabel.Text = "Copying application files...";
            
            var target = Path.GetFullPath(destination.Text.Trim());
            Directory.CreateDirectory(target);
            if (maintenanceSession && PathsEqual(packageRoot, target))
            {
                if (restoreFromRepairCache) await RestoreRepairCacheAsync(target, cancellationToken);
                else AppendLog("Keeping installed application files while applying modified configuration.");
            }
            else
            {
                await CopyPackageAsync(packageRoot, target, cancellationToken, AppendLog);
            }
            
            UpdateMilestone(1, MilestoneStatus.Completed);
            progressBar.Value = 40;
            
            // Milestone 3: Configuring database
            UpdateMilestone(2, MilestoneStatus.InProgress);
            progressLabel.Text = "Configuring database...";
            
            var jwt = string.IsNullOrWhiteSpace(installationJwt) ? Secret(64) : installationJwt;
            var setupToken = string.IsNullOrWhiteSpace(installationSetupToken) ? Secret(28) : installationSetupToken;
            var bundledCachePassword = string.IsNullOrWhiteSpace(installationCachePassword) ? Secret(32) : installationCachePassword;
            installationJwt = jwt;
            installationSetupToken = setupToken;
            installationCachePassword = bundledCachePassword;
            instanceName = instance.Text.Trim();
            if (string.IsNullOrWhiteSpace(systemId)) systemId = Guid.NewGuid().ToString("N");
            
            string db;
            if (fresh.Checked)
            {
                var databaseHost = deployMode == "Docker Container (Recommended)" ? "postgres" : dbHost.Text.Trim();
                var databasePort = deployMode == "Docker Container (Recommended)" ? 5432 : (int)dbPort.Value;
                db = new NpgsqlConnectionStringBuilder
                {
                    Host = databaseHost,
                    Port = databasePort,
                    Database = dbName.Text.Trim(),
                    Username = dbUsername.Text.Trim(),
                    Password = dbPassword.Text
                }.ConnectionString;
            }
            else
            {
                db = connection.Text.Trim();
            }
            
            var dbType = databaseType.SelectedItem?.ToString() ?? "PostgreSQL";
            
            var env = string.Join(Environment.NewLine,
                $"DATABASE_CONNECTION={ComposeEnvironmentValue(db)}",
                $"POSTGRES_USER={ComposeEnvironmentValue(dbUsername.Text.Trim())}",
                $"POSTGRES_PASSWORD={ComposeEnvironmentValue(dbPassword.Text)}",
                $"POSTGRES_DB={ComposeEnvironmentValue(dbName.Text.Trim())}",
                $"JWT_SECRET={ComposeEnvironmentValue(jwt)}",
                $"SETUP_TOKEN={ComposeEnvironmentValue(setupToken)}",
                $"CONGREGATION_NAME={ComposeEnvironmentValue(congregationName.Text.Trim())}",
                $"INSTANCE={ComposeEnvironmentValue(instanceName)}",
                $"SYSTEM_ID={ComposeEnvironmentValue(systemId)}",
                $"FRONTEND_URL={ComposeEnvironmentValue(AdminBaseUrl())}",
                $"PUBLIC_API_URL={ComposeEnvironmentValue(ApiBaseUrl(deployMode))}",
                $"ALLOW_LOOPBACK_PUBLIC_URLS={string.IsNullOrWhiteSpace(publicApiUrl.Text).ToString().ToLowerInvariant()}",
                $"GIVING_URL={ComposeEnvironmentValue(GivingBaseUrl())}",
                $"WEB_PORT={webPort.Value}",
                $"API_PORT={apiPort.Value}",
                $"GIVING_PORT={givingPort.Value}",
                $"CACHE_ENABLED={CacheModeEnabled().ToString().ToLowerInvariant()}",
                $"CACHE_CONNECTION={ComposeEnvironmentValue(BuildCacheConnectionString(forContainer: true))}",
                $"CACHE_PASSWORD={ComposeEnvironmentValue(bundledCachePassword)}",
                $"DEPLOYMENT_MODE={ComposeEnvironmentValue(deployMode)}",
                $"DATABASE_TYPE={ComposeEnvironmentValue(dbType)}");
            
            await File.WriteAllTextAsync(Path.Combine(target, ".env.install"), env + Environment.NewLine, cancellationToken);
            var separateApi = deployMode == "Docker Container (Recommended)";
            await WriteRuntimeConfigAsync(target,
                !string.IsNullOrWhiteSpace(publicApiUrl.Text) || separateApi ? $"{ApiBaseUrl(deployMode)}/api" : "/api",
                !string.IsNullOrWhiteSpace(publicApiUrl.Text) || separateApi ? ApiBaseUrl(deployMode) : string.Empty,
                GivingBaseUrl(),
                ApiBaseUrl(deployMode),
                congregationName.Text.Trim(),
                signedLicense.Checked ? await File.ReadAllTextAsync(licenseFilePath.Text, cancellationToken) : string.Empty,
                licenseUserIdentifier,
                cancellationToken);
            AppendLog("Runtime configuration created.");
            
            UpdateMilestone(2, MilestoneStatus.Completed);
            progressBar.Value = 60;
            
            // Milestone 4: Setting up application services
            UpdateMilestone(3, MilestoneStatus.InProgress);
            progressLabel.Text = "Setting up application services...";
            
            if (deployMode == "Docker Container (Recommended)" && docker.Checked)
            {
                // Docker deployment
                var composeFile = "docker-compose.installer.yml";
                var compose = Path.Combine(target, "deployment", composeFile);
                
                if (!File.Exists(compose))
                {
                    throw new InvalidOperationException($"Docker compose file not found: {compose}");
                }
                
                var projectName = DockerProjectName(instanceName);
                var profiles = new List<string>();
                if (fresh.Checked) profiles.Add("--profile managed-db");
                if (CacheModeIsBundled()) profiles.Add("--profile managed-cache");
                var profile = profiles.Count == 0 ? string.Empty : string.Join(' ', profiles) + " ";
                AppendLog($"Starting Docker project {projectName}. First-time image preparation can take several minutes.");
                var result = await Run("docker",
                    $"compose -p {projectName} --env-file .env.install -f \"{compose}\" {profile}up -d --build --remove-orphans",
                    target, TimeSpan.FromMinutes(20), AppendLog, cancellationToken);
                if (result.ExitCode != 0) throw new InvalidOperationException(Summarize(result.Output));
                progressLabel.Text = "Waiting for eMilimo services to become ready...";
                await WaitForApplicationAsync(projectName, compose, target, cancellationToken);
            }
            else if (deployMode == "IIS Windows Service")
            {
                // IIS Deployment
                await InstallIISAsync(target);
            }
            else
            {
                // Standalone Console
                await InstallStandaloneAsync(target);
            }

            if (fresh.Checked)
            {
                progressLabel.Text = "Creating the default administrator and completing setup...";
                await CompleteFreshSetupAsync(deployMode, cancellationToken);
                AppendLog($"Initial setup completed for administrator {adminEmail.Text.Trim()}.");
            }
            else if (signedLicense.Checked)
            {
                progressLabel.Text = "Validating the signed licence...";
                await ValidateSelectedLicenseAsync(deployMode, cancellationToken);
                AppendLog("Signed licence passed signature, system binding, and expiry validation.");
            }
            
            UpdateMilestone(3, MilestoneStatus.Completed);
            progressBar.Value = 80;
            
            // Milestone 5: Creating desktop shortcuts
            UpdateMilestone(4, MilestoneStatus.InProgress);
            progressLabel.Text = "Creating desktop shortcuts...";
            
            if (shortcut.Checked)
                CreateDesktopShortcut(target, AdminBaseUrl());
            
            if (!fresh.Checked) Clipboard.SetText(setupToken);
            
            UpdateMilestone(4, MilestoneStatus.Completed);
            progressBar.Value = 90;
            
            // Milestone 6: Finalizing installation
            UpdateMilestone(5, MilestoneStatus.InProgress);
            progressLabel.Text = "Finalizing installation...";
            
            await Task.Delay(500);

            await CreateRepairCacheAsync(target, cancellationToken);
            RegisterInstalledProduct(target, deployMode);
            
            UpdateMilestone(5, MilestoneStatus.Completed);
            progressBar.Value = 100;
            
            progressLabel.Text = "Installation complete!";
            installationSucceeded = true;
            AppendLog("Installation completed successfully.");
            page = 7;
            RenderPage();
            if (launch.Checked)
                Process.Start(new ProcessStartInfo(AdminBaseUrl()) { UseShellExecute = true });
            
        }
        catch (OperationCanceledException) when (installationCancellation?.IsCancellationRequested == true)
        {
            status.Text = "Installation cancelled";
            progressLabel!.Text = "Installation was cancelled. You can safely retry.";
            AppendLog("Installation cancelled by the user.");
            UpdateMilestone(GetActiveMilestone(), MilestoneStatus.Failed);
            installCompleteButton!.Visible = true;
            installCompleteButton.Text = "Retry installation";
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("Setup could not write to the installation folder. Run eMilimo Setup as administrator and try again.", "Administrator access required", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateMilestone(GetActiveMilestone(), MilestoneStatus.Failed);
            installCompleteButton!.Visible = true;
            AppendLog("Administrator access is required for the selected installation folder.");
            installCompleteButton.Text = "Retry installation";
        }
        catch (Exception ex)
        {
            status.Text = "Installation did not complete";
            AppendLog($"ERROR: {ex}");
            MessageBox.Show($"{ex.Message}\n\nDiagnostic log:\n{installerLogPath}", "Installation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateMilestone(GetActiveMilestone(), MilestoneStatus.Failed);
            installCompleteButton!.Visible = true;
            installCompleteButton.Text = "Retry installation";
        }
        finally
        {
            installing = false;
            installationTimer.Stop();
            elapsedTimer.Stop();
            cancel.Text = "Close";
            cancel.Enabled = true;
            if (!IsDisposed && page == 6)
            {
                // Keep showing progress page
            }
        }
    }

    private async Task InstallIISAsync(string target)
    {
        progressLabel!.Text = "Configuring the eMilimo IIS site...";
        
        try
        {
            var frontendPath = Path.Combine(target, "frontend", "web", "dist", "web", "browser");
            var apiPath = Path.Combine(target, "standalone", "api");
            if (!Directory.Exists(frontendPath) || !File.Exists(Path.Combine(apiPath, "eMilimo.API.dll")))
                throw new InvalidOperationException("The prepared web or API payload is missing from the installation package.");
            await WriteApiSettingsAsync(apiPath, (int)webPort.Value, (int)givingPort.Value, installationCancellation?.Token ?? CancellationToken.None);
            await CopyPayloadDirectoryAsync(frontendPath, Path.Combine(apiPath, "wwwroot"),
                installationCancellation?.Token ?? CancellationToken.None, AppendLog);
            var givingPath = Path.Combine(target, "frontend", "giving", "dist", "giving", "browser");
            await CopyPayloadDirectoryAsync(givingPath, Path.Combine(apiPath, "wwwroot-giving"),
                installationCancellation?.Token ?? CancellationToken.None, AppendLog);
            if (!HasAspNetCoreModule())
                throw new InvalidOperationException("The ASP.NET Core IIS Module is not installed. Run the fully bundled setup package or install the .NET 8 Hosting Bundle, then retry.");
            await WriteIisWebConfigAsync(apiPath, installationCancellation?.Token ?? CancellationToken.None);

            using var iisManager = new ServerManager();
            var siteName = "eMilimo";
            
            // Remove existing site if it exists
            var existingSite = iisManager.Sites[siteName];
            if (existingSite != null)
            {
                iisManager.Sites.Remove(existingSite);
            }
            
            // Create application pool
            var appPool = iisManager.ApplicationPools[siteName];
            if (appPool == null)
            {
                appPool = iisManager.ApplicationPools.Add(siteName);
            }
            appPool.ManagedRuntimeVersion = string.Empty;
            appPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
            appPool.ProcessModel.IdentityType = ProcessModelIdentityType.ApplicationPoolIdentity;
            appPool.AutoStart = true;
            
            // The API serves the Angular application from wwwroot, keeping API and files on one origin.
            var site = iisManager.Sites.Add(siteName, "http", $"*:{(int)webPort.Value}:", apiPath);
            site.Bindings.Add($"*:{(int)givingPort.Value}:", "http");
            site.ApplicationDefaults.ApplicationPoolName = siteName;
            
            // Commit changes
            iisManager.CommitChanges();

            var acl = await Run("icacls.exe", $"\"{apiPath}\" /grant \"IIS AppPool\\{siteName}:(OI)(CI)M\" /T /C",
                apiPath, TimeSpan.FromMinutes(2), AppendLog, installationCancellation?.Token ?? CancellationToken.None);
            if (acl.ExitCode != 0) throw new InvalidOperationException($"Could not grant the IIS application pool access to the installation folder. {Summarize(acl.Output)}");
            
            // Start the site
            site.Start();
            
            progressLabel.Text = "Waiting for the IIS application to become ready...";
            await WaitForHttpReadyAsync((int)webPort.Value, TimeSpan.FromMinutes(3), installationCancellation?.Token ?? CancellationToken.None, apiPath);
            await WaitForHttpReadyAsync((int)givingPort.Value, TimeSpan.FromMinutes(3), installationCancellation?.Token ?? CancellationToken.None, apiPath);
            AppendLog($"IIS site '{siteName}' is ready on administration port {webPort.Value} and Giving port {givingPort.Value}.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set up IIS: {ex.Message}");
        }
    }

    private async Task InstallStandaloneAsync(string target)
    {
        progressLabel!.Text = "Setting up standalone application...";
        await Task.Delay(100);
        
        try
        {
            var publishPath = Path.Combine(target, "standalone", "api");
            if (!File.Exists(Path.Combine(publishPath, "eMilimo.API.dll")))
                throw new InvalidOperationException($"The prepared backend payload was not found: {publishPath}");
            await WriteApiSettingsAsync(publishPath, (int)webPort.Value, (int)givingPort.Value, installationCancellation?.Token ?? CancellationToken.None);
            var frontendPath = Path.Combine(target, "frontend", "web", "dist", "web", "browser");
            await CopyPayloadDirectoryAsync(frontendPath, Path.Combine(publishPath, "wwwroot"),
                installationCancellation?.Token ?? CancellationToken.None, AppendLog);
            var givingPath = Path.Combine(target, "frontend", "giving", "dist", "giving", "browser");
            await CopyPayloadDirectoryAsync(givingPath, Path.Combine(publishPath, "wwwroot-giving"),
                installationCancellation?.Token ?? CancellationToken.None, AppendLog);
            
            // Create a batch file to run the application
            var batchContent = $@"
@echo off
echo Starting eMilimo API Server...
cd ""{publishPath}""
start ""eMilimo"" dotnet eMilimo.API.dll --environment Production
echo eMilimo is starting...
";
            await File.WriteAllTextAsync(Path.Combine(target, "start-emilimo.bat"), batchContent);

            var standaloneProcess = Process.Start(new ProcessStartInfo("dotnet", "eMilimo.API.dll --environment Production")
                { WorkingDirectory = publishPath, UseShellExecute = false, CreateNoWindow = true })
                ?? throw new InvalidOperationException("The standalone eMilimo process could not be started.");
            await File.WriteAllTextAsync(Path.Combine(target, "standalone.pid"), standaloneProcess.Id.ToString(), installationCancellation?.Token ?? CancellationToken.None);
            progressLabel.Text = "Waiting for the standalone application to become ready...";
            await WaitForHttpReadyAsync((int)webPort.Value, TimeSpan.FromMinutes(3), installationCancellation?.Token ?? CancellationToken.None, publishPath);
            await WaitForHttpReadyAsync((int)givingPort.Value, TimeSpan.FromMinutes(3), installationCancellation?.Token ?? CancellationToken.None, publishPath);
            AppendLog($"Standalone application is ready on administration port {webPort.Value} and Giving port {givingPort.Value}.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set up standalone application: {ex.Message}");
        }
    }

    private string installationJwt = string.Empty;
    private string installationSetupToken = string.Empty;
    private string installationCachePassword = string.Empty;
    private string instanceName = "emilimo";
    private string systemId = Guid.NewGuid().ToString("N");
    private readonly string licenseUserIdentifier = Guid.NewGuid().ToString("N");

    private static InstalledProduct? DetectLegacyInstallation(string expectedPath)
    {
        var api = Path.Combine(expectedPath, "standalone", "api", "eMilimo.API.dll");
        if (!File.Exists(api)) return null;
        var versionText = FileVersionInfo.GetVersionInfo(api).FileVersion;
        if (!Version.TryParse(versionText, out var version)) version = new Version(1, 0, 0);
        var mode = "Standalone Console";
        var webPort = 4200;
        try
        {
            using var manager = new ServerManager();
            var site = manager.Sites["eMilimo"];
            if (site != null)
            {
                mode = "IIS Windows Service";
                webPort = site.Bindings.Select(binding => binding.EndPoint?.Port ?? 0).FirstOrDefault(port => port > 0);
                if (webPort == 0) webPort = 4200;
            }
        }
        catch { }
        if (File.Exists(Path.Combine(expectedPath, ".env.install")) && File.Exists(Path.Combine(expectedPath, "deployment", "docker-compose.installer.yml")))
        {
            var env = File.ReadLines(Path.Combine(expectedPath, ".env.install"));
            if (env.Any(line => line.StartsWith("DEPLOYMENT_MODE=", StringComparison.OrdinalIgnoreCase) && line.Contains("Docker", StringComparison.OrdinalIgnoreCase)))
                mode = "Docker Container (Recommended)";
        }
        return new InstalledProduct(version, expectedPath, mode, "localhost", webPort, 5290, 9200, "main", string.Empty, string.Empty, string.Empty);
    }

    private void LoadInstalledConfiguration(InstalledProduct product)
    {
        destination.Text = product.InstallLocation;
        publicHost.Text = product.Host;
        webPort.Value = Math.Clamp(product.WebPort, (int)webPort.Minimum, (int)webPort.Maximum);
        apiPort.Value = Math.Clamp(product.ApiPort, (int)apiPort.Minimum, (int)apiPort.Maximum);
        givingPort.Value = Math.Clamp(product.GivingPort, (int)givingPort.Minimum, (int)givingPort.Maximum);
        publicAdminUrl.Text = product.PublicAdminUrl;
        publicApiUrl.Text = product.PublicApiUrl;
        publicGivingUrl.Text = product.PublicGivingUrl;
        instance.Text = product.Instance;
        instanceName = product.Instance;
        var modeIndex = deploymentMode.Items.IndexOf(product.DeploymentMode);
        if (modeIndex >= 0) deploymentMode.SelectedIndex = modeIndex;
        existing.Checked = true;
        fresh.Checked = false;
        database.Checked = false;

        var apiSettings = Path.Combine(product.InstallLocation, "standalone", "api", "appsettings.Production.json");
        if (File.Exists(apiSettings))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(apiSettings));
                var root = document.RootElement;
                connection.Text = ReadJsonString(root, "ConnectionStrings", "DefaultConnection");
                installationJwt = ReadJsonString(root, "Jwt", "Secret");
                installationSetupToken = ReadJsonString(root, "Setup", "BootstrapToken");
                systemId = ReadJsonString(root, "Platform", "DeploymentId");
                var redisEnabled = ReadJsonBoolean(root, "Redis", "Enabled");
                cache.Checked = redisEnabled;
                cacheMode.SelectedIndex = redisEnabled ? 1 : 2;
                if (redisEnabled) LoadCacheConnectionString(ReadJsonString(root, "Redis", "ConnectionString"));
            }
            catch { }
        }

        var environmentFile = Path.Combine(product.InstallLocation, ".env.install");
        if (File.Exists(environmentFile))
        {
            var values = File.ReadLines(environmentFile)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith('#') && line.Contains('='))
                .Select(line => line.Split('=', 2))
                .ToDictionary(parts => parts[0], parts => DecodeEnvironmentValue(parts[1]), StringComparer.OrdinalIgnoreCase);
            if (values.TryGetValue("DATABASE_CONNECTION", out var databaseConnection)) connection.Text = databaseConnection;
            if (values.TryGetValue("JWT_SECRET", out var jwt)) installationJwt = jwt;
            if (values.TryGetValue("SETUP_TOKEN", out var setupToken)) installationSetupToken = setupToken;
            if (values.TryGetValue("SYSTEM_ID", out var deploymentId)) systemId = deploymentId;
            if (values.TryGetValue("CACHE_PASSWORD", out var cacheSecret)) installationCachePassword = cacheSecret;
            if (values.TryGetValue("CACHE_ENABLED", out var cacheEnabled) && bool.TryParse(cacheEnabled, out var enabled))
                cacheMode.SelectedIndex = enabled ? 1 : 2;
            if (values.TryGetValue("CACHE_CONNECTION", out var cacheConnection) && cacheMode.SelectedIndex == 1)
                LoadCacheConnectionString(cacheConnection);
        }
        ToggleEnvironmentFields();
    }

    private static string ReadJsonString(JsonElement root, string section, string property) =>
        root.TryGetProperty(section, out var sectionElement) && sectionElement.TryGetProperty(property, out var propertyElement)
            ? propertyElement.GetString() ?? string.Empty : string.Empty;

    private static bool ReadJsonBoolean(JsonElement root, string section, string property) =>
        root.TryGetProperty(section, out var sectionElement) && sectionElement.TryGetProperty(property, out var propertyElement) &&
        propertyElement.ValueKind is JsonValueKind.True;

    private static string DecodeEnvironmentValue(string value)
    {
        var trimmed = value.Trim();
        try
        {
            if (trimmed.StartsWith('"') && trimmed.EndsWith('"'))
                return (JsonSerializer.Deserialize<string>(trimmed) ?? string.Empty).Replace("$$", "$", StringComparison.Ordinal);
        }
        catch { }
        return trimmed.Replace("$$", "$", StringComparison.Ordinal);
    }

    private async Task UninstallAsync(InstalledProduct product)
    {
        var choice = MessageBox.Show(
            "Remove eMilimo from this computer?\n\nYes: remove application files and Docker volumes.\nNo: remove the application but preserve uploaded files, configuration, and Docker volumes.\nCancel: keep eMilimo installed.\n\nPostgreSQL and other shared prerequisite services are never removed automatically.",
            "Uninstall eMilimo", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
        if (choice == DialogResult.Cancel) return;
        var removeData = choice == DialogResult.Yes;
        installing = true;
        status.Text = "Uninstalling eMilimo...";
        using var progress = new UninstallProgressDialog(removeData);
        progress.Show(this);
        progress.SetProgress(5, "Preparing to remove eMilimo…");
        await Task.Yield();
        try
        {
            progress.SetProgress(15, "Stopping application services…");
            if (product.DeploymentMode == "IIS Windows Service")
            {
                using var manager = new ServerManager();
                var site = manager.Sites["eMilimo"];
                if (site != null) manager.Sites.Remove(site);
                var pool = manager.ApplicationPools["eMilimo"];
                if (pool != null) manager.ApplicationPools.Remove(pool);
                manager.CommitChanges();
            }
            else if (product.DeploymentMode == "Docker Container (Recommended)")
            {
                var compose = Path.Combine(product.InstallLocation, "deployment", "docker-compose.installer.yml");
                if (File.Exists(compose))
                {
                    var volumeFlag = removeData ? " --volumes" : string.Empty;
                    await Run("docker", $"compose -p {DockerProjectName(product.Instance)} --env-file .env.install -f {QuoteArgument(compose)} down --remove-orphans{volumeFlag}",
                        product.InstallLocation, TimeSpan.FromMinutes(5));
                }
            }
            else
            {
                var pidFile = Path.Combine(product.InstallLocation, "standalone.pid");
                if (File.Exists(pidFile) && int.TryParse(File.ReadAllText(pidFile), out var pid))
                {
                    try { Process.GetProcessById(pid).Kill(true); } catch { }
                }
            }

            progress.SetProgress(50, removeData ? "Removing application data…" : "Preserving configuration and uploaded data…");
            if (!removeData) PreserveInstalledData(product);
            progress.SetProgress(70, "Removing shortcuts and installation registration…");
            Registry.LocalMachine.DeleteSubKeyTree(UninstallRegistryPath, throwOnMissingSubKey: false);
            var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "eMilimo.url");
            if (File.Exists(shortcutPath)) File.Delete(shortcutPath);
            progress.SetProgress(88, "Scheduling final application file cleanup…");
            ScheduleInstallationFolderRemoval(product.InstallLocation);
            progress.SetProgress(100, "Uninstallation complete.");
            await Task.Delay(500);
            progress.Close();
            MessageBox.Show(removeData
                    ? "eMilimo has been unregistered. Application files will be removed when this window closes. PostgreSQL remains installed."
                    : "eMilimo has been unregistered. Preserved data was copied under ProgramData\\BAU Solutions\\eMilimo\\PreservedData.",
                "eMilimo uninstalled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            installing = false;
            Close();
        }
        catch (Exception ex)
        {
            installing = false;
            progress.MarkFailed("Uninstallation stopped before completion.");
            MessageBox.Show($"Uninstall could not complete: {ex.Message}", "Uninstall eMilimo", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void PreserveInstalledData(InstalledProduct product)
    {
        var destinationRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BAU Solutions", "eMilimo", "PreservedData", $"{product.Instance}-{DateTime.Now:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(destinationRoot);
        var sharedData = Path.Combine(product.InstallLocation, "standalone", "api", "shared-data");
        if (Directory.Exists(sharedData)) Directory.Move(sharedData, Path.Combine(destinationRoot, "shared-data"));
        foreach (var file in new[] { ".env.install", Path.Combine("standalone", "api", "appsettings.Production.json") })
        {
            var source = Path.Combine(product.InstallLocation, file);
            if (!File.Exists(source)) continue;
            var output = Path.Combine(destinationRoot, Path.GetFileName(file));
            File.Copy(source, output, true);
        }
    }

    private static void ScheduleInstallationFolderRemoval(string target)
    {
        var fullTarget = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar);
        var root = Path.GetPathRoot(fullTarget)?.TrimEnd(Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(root) || fullTarget.Equals(root, StringComparison.OrdinalIgnoreCase) || fullTarget.Length < 12)
            throw new InvalidOperationException("The installation path is not safe to remove automatically.");
        var scriptPath = Path.Combine(Path.GetTempPath(), $"emilimo-uninstall-{Guid.NewGuid():N}.ps1");
        const string script = "param([int]$ProcessId,[string]$Target,[string]$ScriptPath)\nWait-Process -Id $ProcessId -ErrorAction SilentlyContinue\nStart-Sleep -Milliseconds 500\nRemove-Item -LiteralPath $Target -Recurse -Force -ErrorAction SilentlyContinue\nRemove-Item -LiteralPath $ScriptPath -Force -ErrorAction SilentlyContinue\n";
        File.WriteAllText(scriptPath, script, new UTF8Encoding(false));
        Process.Start(new ProcessStartInfo("powershell.exe",
            $"-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File {QuoteArgument(scriptPath)} -ProcessId {Environment.ProcessId} -Target {QuoteArgument(fullTarget)} -ScriptPath {QuoteArgument(scriptPath)}")
        { UseShellExecute = false, CreateNoWindow = true });
    }

    private string GetDatabaseConnectionString() => fresh.Checked
        ? $"Host={dbHost.Text};Port={dbPort.Value};Database={dbName.Text};Username={dbUsername.Text};Password={dbPassword.Text}"
        : connection.Text.Trim();

    private static string ComposeEnvironmentValue(string value) =>
        $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("$", "$$").Replace("\r", "").Replace("\n", "\\n")}\"";
    private static string JsonEscape(string value) => System.Text.Json.JsonSerializer.Serialize(value).Trim('"');
    private bool HasPreparedPayload()
    {
        if (packageRoot == null) return false;
        return File.Exists(Path.Combine(packageRoot, "backend", "eMilimo.API", "bin", "Release", "net8.0", "eMilimo.API.dll")) &&
               File.Exists(Path.Combine(packageRoot, "frontend", "web", "dist", "web", "browser", "index.html"));
    }

    private string? FindBundledPrerequisite(string pattern)
    {
        if (packageRoot == null) return null;
        var folder = Path.Combine(packageRoot, "prerequisites");
        return Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, pattern, SearchOption.TopDirectoryOnly).FirstOrDefault()
            : null;
    }

    private bool HasBundledPrerequisite(string pattern) => FindBundledPrerequisite(pattern) != null;

    private bool HasAvailablePrerequisite(string key, string bundledPattern) =>
        HasBundledPrerequisite(bundledPattern) || OnlinePrerequisites().ContainsKey(key);

    private IReadOnlyDictionary<string, OnlinePrerequisite> OnlinePrerequisites()
    {
        if (onlinePrerequisites != null) return onlinePrerequisites;
        onlinePrerequisites = new Dictionary<string, OnlinePrerequisite>(StringComparer.OrdinalIgnoreCase);
        if (packageRoot == null) return onlinePrerequisites;
        var manifestPath = Path.Combine(packageRoot, "prerequisites", "online-prerequisites.json");
        if (!File.Exists(manifestPath)) return onlinePrerequisites;
        try
        {
            var manifest = JsonSerializer.Deserialize<OnlinePrerequisiteManifest>(File.ReadAllText(manifestPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            foreach (var item in manifest?.Prerequisites ?? [])
            {
                if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.DisplayName) ||
                    string.IsNullOrWhiteSpace(item.FileName) || Path.GetFileName(item.FileName) != item.FileName ||
                    !Uri.TryCreate(item.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
                    string.IsNullOrWhiteSpace(item.Sha256) || item.Sha256.Length != 64 || !item.Sha256.All(Uri.IsHexDigit))
                    continue;
                onlinePrerequisites[item.Key] = item;
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Online prerequisite manifest was ignored: {ex.Message}");
        }
        return onlinePrerequisites;
    }

    private async Task<string> FindOrDownloadPrerequisiteAsync(
        string key, string bundledPattern, CancellationToken cancellationToken)
    {
        var bundled = FindBundledPrerequisite(bundledPattern);
        if (bundled != null) return bundled;
        if (!OnlinePrerequisites().TryGetValue(key, out var item))
            throw new InvalidOperationException($"{key} is required but is neither bundled nor available from the online prerequisite manifest.");

        var downloadRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BAU Solutions", "eMilimo", "Installer", "Downloads", "v1.0.1");
        Directory.CreateDirectory(downloadRoot);
        var destination = Path.Combine(downloadRoot, item.FileName);
        if (File.Exists(destination) && await FileMatchesSha256Async(destination, item.Sha256, cancellationToken))
        {
            AppendLog($"Using verified cached prerequisite: {item.DisplayName}.");
            return destination;
        }

        var partial = destination + $".{Guid.NewGuid():N}.partial";
        try
        {
            progressLabel!.Text = $"Downloading {item.DisplayName}...";
            AppendLog($"Downloading {item.DisplayName} from its pinned HTTPS source.");
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            using var response = await client.GetAsync(item.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, true))
                await input.CopyToAsync(output, 1024 * 1024, cancellationToken);

            if (!await FileMatchesSha256Async(partial, item.Sha256, cancellationToken))
                throw new CryptographicException($"The downloaded {item.DisplayName} file did not match its pinned SHA-256 digest.");
            File.Move(partial, destination, overwrite: true);
            AppendLog($"Verified {item.DisplayName} (SHA-256 {item.Sha256}).");
            return destination;
        }
        catch
        {
            try { if (File.Exists(partial)) File.Delete(partial); } catch { }
            throw;
        }
    }

    private static async Task<bool> FileMatchesSha256Async(string path, string expected, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, true);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[1024 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            hash.AppendData(buffer, 0, read);
        }
        return CryptographicOperations.FixedTimeEquals(hash.GetHashAndReset(), Convert.FromHexString(expected));
    }

    private string BundledPrerequisiteSummary()
    {
        var items = new[]
        {
            ("ASP.NET Core Hosting Bundle", "dotnet-hosting", "dotnet-hosting*.exe"),
            ("PostgreSQL", "postgresql", "postgresql*.exe"),
            ("pgAdmin", "pgadmin", "pgadmin*.exe"),
            ("Docker Desktop", "", "Docker Desktop Installer.exe"),
            ("Offline Docker images", "", "emilimo-images*.tar")
        };
        return string.Join("\n", items.Select(item =>
        {
            if (HasBundledPrerequisite(item.Item3)) return $"✓ {item.Item1} — bundled";
            if (!string.IsNullOrEmpty(item.Item2) && OnlinePrerequisites().ContainsKey(item.Item2))
                return $"↓ {item.Item1} — downloaded only if selected";
            return $"○ {item.Item1} — existing installation required";
        }));
    }

    private static bool HasAspNetCoreModule()
    {
        var moduleExists = File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "IIS", "Asp.Net Core Module", "V2", "aspnetcorev2.dll")) ||
                           File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "IIS", "Asp.Net Core Module", "V2", "aspnetcorev2.dll"));
        if (!moduleExists) return false;
        try
        {
            var applicationHostConfig = Path.Combine(Environment.SystemDirectory, "inetsrv", "config", "applicationHost.config");
            return File.ReadAllText(applicationHostConfig).Contains("AspNetCoreModuleV2", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private async Task InstallSelectedPrerequisitesAsync(string deployMode, CancellationToken cancellationToken)
    {
        if (deployMode == "Docker Container (Recommended)")
        {
            if (!await CommandWorks("docker", "version --format {{.Server.Version}}", packageRoot!, TimeSpan.FromSeconds(20), cancellationToken))
            {
                var dockerInstaller = FindBundledPrerequisite("Docker Desktop Installer.exe")
                    ?? throw new InvalidOperationException("Docker is not running and its offline installer is missing from this package.");
                progressLabel!.Text = "Installing Docker Desktop...";
                await RunInstallerAsync(dockerInstaller, "install --quiet --accept-license", TimeSpan.FromMinutes(20), cancellationToken);
                throw new InvalidOperationException("Docker Desktop was installed. Start Docker Desktop and, if Windows requests it, restart the computer before retrying eMilimo setup.");
            }

            var imageArchive = FindBundledPrerequisite("emilimo-images*.tar");
            if (imageArchive != null)
            {
                progressLabel!.Text = "Loading bundled Docker images...";
                var load = await Run("docker", $"load --input {QuoteArgument(imageArchive)}", packageRoot!, TimeSpan.FromMinutes(20), AppendLog, cancellationToken);
                if (load.ExitCode != 0) throw new InvalidOperationException($"Could not load bundled Docker images. {Summarize(load.Output)}");
            }
            return;
        }

        if (deployMode == "IIS Windows Service")
        {
            progressLabel!.Text = "Enabling IIS web server components...";
            var iis = await Run("dism.exe", "/Online /Enable-Feature /FeatureName:IIS-WebServerRole /FeatureName:IIS-WebServer /FeatureName:IIS-StaticContent /FeatureName:IIS-DefaultDocument /FeatureName:IIS-HttpErrors /FeatureName:IIS-ManagementConsole /All /NoRestart",
                packageRoot!, TimeSpan.FromMinutes(10), AppendLog, cancellationToken);
            if (iis.ExitCode is not (0 or 3010)) throw new InvalidOperationException($"IIS could not be enabled. {Summarize(iis.Output)}");

            if (!HasAspNetCoreModule())
            {
                var hostingInstaller = await FindOrDownloadPrerequisiteAsync("dotnet-hosting", "dotnet-hosting*.exe", cancellationToken);
                progressLabel.Text = "Installing the ASP.NET Core Hosting Bundle...";
                await RunInstallerAsync(hostingInstaller, "/install /quiet /norestart", TimeSpan.FromMinutes(10), cancellationToken);
                var reset = await Run("iisreset.exe", "/restart", packageRoot!, TimeSpan.FromMinutes(2), AppendLog, cancellationToken);
                if (reset.ExitCode != 0) AppendLog("IIS restart will be completed when the site starts.");
                if (!HasAspNetCoreModule())
                    throw new InvalidOperationException("The Hosting Bundle was installed but IIS has not loaded the ASP.NET Core module. Restart Windows, then retry setup.");
            }
        }
        else if (!await HasDotnet8RuntimeAsync())
        {
            var runtimeInstaller = await FindOrDownloadPrerequisiteAsync("dotnet-hosting", "dotnet-hosting*.exe", cancellationToken);
            progressLabel!.Text = "Installing the ASP.NET Core runtime...";
            await RunInstallerAsync(runtimeInstaller, "/install /quiet /norestart", TimeSpan.FromMinutes(10), cancellationToken);
        }

        if (fresh.Checked && database.Checked && IsTcpPortAvailable((int)dbPort.Value))
        {
            var postgresInstaller = await FindOrDownloadPrerequisiteAsync("postgresql", "postgresql*.exe", cancellationToken);
            progressLabel!.Text = "Installing PostgreSQL...";
            var arguments = $"--mode unattended --unattendedmodeui none --superaccount {QuoteArgument(dbUsername.Text.Trim())} --superpassword {QuoteArgument(dbPassword.Text)} --servicepassword {QuoteArgument(dbPassword.Text)} --serverport {dbPort.Value} --disable-components stackbuilder";
            await RunInstallerAsync(postgresInstaller, arguments, TimeSpan.FromMinutes(20), cancellationToken);
            await EnsurePostgresDatabaseAsync(cancellationToken);
        }

        if (pgAdmin.Checked)
        {
            var pgAdminInstaller = await FindOrDownloadPrerequisiteAsync("pgadmin", "pgadmin*.exe", cancellationToken);
            progressLabel!.Text = "Installing pgAdmin...";
            await RunInstallerAsync(pgAdminInstaller, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART", TimeSpan.FromMinutes(15), cancellationToken);
        }

    }

    private async Task RunInstallerAsync(string file, string arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        AppendLog($"Running verified prerequisite: {Path.GetFileName(file)}");
        var result = await Run(file, arguments, packageRoot!, timeout, AppendLog, cancellationToken);
        if (result.ExitCode is not (0 or 1641 or 3010))
            throw new InvalidOperationException($"{Path.GetFileName(file)} failed with exit code {result.ExitCode}. {Summarize(result.Output)}");
    }

    private async Task EnsurePostgresDatabaseAsync(CancellationToken cancellationToken)
    {
        var admin = new NpgsqlConnectionStringBuilder
        {
            Host = dbHost.Text.Trim(), Port = (int)dbPort.Value, Database = "postgres",
            Username = dbUsername.Text.Trim(), Password = dbPassword.Text, Timeout = 10, CommandTimeout = 30
        };
        Exception? lastError = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                await using var connection = new NpgsqlConnection(admin.ConnectionString);
                await connection.OpenAsync(cancellationToken);
                await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
                exists.Parameters.AddWithValue("name", dbName.Text.Trim());
                if (await exists.ExecuteScalarAsync(cancellationToken) == null)
                {
                    var quotedName = dbName.Text.Trim().Replace("\"", "\"\"");
                    await using var create = new NpgsqlCommand($"CREATE DATABASE \"{quotedName}\"", connection);
                    await create.ExecuteNonQueryAsync(cancellationToken);
                }
                return;
            }
            catch (Exception ex) when (attempt < 19)
            {
                lastError = ex;
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
        throw new InvalidOperationException($"PostgreSQL was installed but did not become ready. {lastError?.Message}");
    }

    private static string QuoteArgument(string value) => $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

    private async Task<bool> HasDotnet8RuntimeAsync()
    {
        try
        {
            var result = await Run("dotnet", "--list-runtimes", packageRoot ?? AppContext.BaseDirectory, TimeSpan.FromSeconds(20));
            return result.ExitCode == 0 && result.Output.Contains("Microsoft.AspNetCore.App 8.", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private int GetActiveMilestone()
    {
        for (int i = 0; i < milestones.Count; i++)
        {
            if (milestones[i].Status == MilestoneStatus.InProgress)
                return i;
        }
        return milestones.Count - 1;
    }

    private async Task CopyPackageAsync(string source, string target, CancellationToken cancellationToken, Action<string> report)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".vs", "node_modules", "bin", "obj", "dist", "logs", "eMilimo.SetupWizard" };
        foreach (var directory in new[] { "deployment", "prerequisites" })
        {
            var from = Path.Combine(source, directory);
            var to = Path.Combine(target, directory);
            if (!Directory.Exists(from)) continue;
            
            foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(from, file);
                if (relative.Split(Path.DirectorySeparatorChar).Any(excluded.Contains)) continue;
                var output = Path.Combine(to, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                await using var input = File.OpenRead(file);
                await using var destination = File.Create(output);
                await input.CopyToAsync(destination, cancellationToken);
            }
        }

        var preparedBackend = Path.Combine(source, "backend", "eMilimo.API", "bin", "Release", "net8.0");
        if (!File.Exists(Path.Combine(preparedBackend, "eMilimo.API.dll")))
            throw new InvalidOperationException($"Prepared backend output not found: {preparedBackend}");
        await CopyPayloadDirectoryAsync(preparedBackend, Path.Combine(target, "standalone", "api"), cancellationToken, report);

        var preparedFrontend = Path.Combine(source, "frontend", "web", "dist", "web", "browser");
        if (!Directory.Exists(preparedFrontend))
            throw new InvalidOperationException($"Prepared frontend output not found: {preparedFrontend}");
        await CopyPayloadDirectoryAsync(preparedFrontend, Path.Combine(target, "frontend", "web", "dist", "web", "browser"), cancellationToken, report);
        var nginxConfig = Path.Combine(source, "frontend", "web", "nginx.production.conf");
        if (!File.Exists(nginxConfig)) throw new InvalidOperationException($"Prepared Nginx configuration not found: {nginxConfig}");
        var nginxTarget = Path.Combine(target, "frontend", "web", "nginx.production.conf");
        Directory.CreateDirectory(Path.GetDirectoryName(nginxTarget)!);
        File.Copy(nginxConfig, nginxTarget, true);
    }

    private static async Task CopyPayloadDirectoryAsync(string source, string target, CancellationToken cancellationToken, Action<string> report)
    {
        var excludedSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "logs", "backups", "uploads", "shared-data", "artifacts", "obj", "bin" };
        var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Where(file => !Path.GetRelativePath(source, file).Split(Path.DirectorySeparatorChar).Any(excludedSegments.Contains))
            .ToArray();
        Directory.CreateDirectory(target);
        for (var index = 0; index < files.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var file = files[index];
            var output = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            await using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 128, true);
            await using var destination = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 128, true);
            await input.CopyToAsync(destination, cancellationToken);
            if (index == 0 || (index + 1) % 50 == 0 || index == files.Length - 1)
                report($"Copied {index + 1:N0} of {files.Length:N0} payload files.");
        }
    }

    private async Task CreateRepairCacheAsync(string target, CancellationToken cancellationToken)
    {
        var installerFolder = Path.Combine(target, "installer");
        var cache = Path.Combine(installerFolder, "repair-cache");
        Directory.CreateDirectory(installerFolder);
        await CopyPayloadDirectoryAsync(Path.Combine(target, "standalone", "api"), Path.Combine(cache, "api"), cancellationToken, AppendLog);
        await CopyPayloadDirectoryAsync(Path.Combine(target, "frontend", "web", "dist", "web", "browser"), Path.Combine(cache, "web"), cancellationToken, AppendLog);
        await CopyPayloadDirectoryAsync(Path.Combine(target, "frontend", "giving", "dist", "giving", "browser"), Path.Combine(cache, "giving"), cancellationToken, AppendLog);
        foreach (var configurationFile in new[] { "appsettings.Production.json", "web.config" })
        {
            var cachedConfiguration = Path.Combine(cache, "api", configurationFile);
            if (File.Exists(cachedConfiguration)) File.Delete(cachedConfiguration);
        }

        var installedSetup = Path.Combine(installerFolder, "eMilimo-Setup.exe");
        if (!PathsEqual(System.Windows.Forms.Application.ExecutablePath, installedSetup))
            File.Copy(System.Windows.Forms.Application.ExecutablePath, installedSetup, true);
        AppendLog("Windows maintenance and repair cache created.");
    }

    private async Task RestoreRepairCacheAsync(string target, CancellationToken cancellationToken)
    {
        var cache = Path.Combine(target, "installer", "repair-cache");
        var apiCache = Path.Combine(cache, "api");
        var webCache = Path.Combine(cache, "web");
        var givingCache = Path.Combine(cache, "giving");
        if (!File.Exists(Path.Combine(apiCache, "eMilimo.API.dll")) || !File.Exists(Path.Combine(webCache, "index.html")) || !File.Exists(Path.Combine(givingCache, "index.html")))
            throw new InvalidOperationException("The local repair cache is incomplete. Run a setup package matching or newer than the installed version and choose Repair.");
        progressLabel!.Text = "Restoring application files from the repair cache...";
        await CopyPayloadDirectoryAsync(apiCache, Path.Combine(target, "standalone", "api"), cancellationToken, AppendLog);
        await CopyPayloadDirectoryAsync(webCache, Path.Combine(target, "frontend", "web", "dist", "web", "browser"), cancellationToken, AppendLog);
        await CopyPayloadDirectoryAsync(givingCache, Path.Combine(target, "frontend", "giving", "dist", "giving", "browser"), cancellationToken, AppendLog);
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        return string.Equals(Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }

    private void RegisterInstalledProduct(string target, string deployMode)
    {
        var maintenanceExe = Path.Combine(target, "installer", "eMilimo-Setup.exe");
        using var key = Registry.LocalMachine.CreateSubKey(UninstallRegistryPath, writable: true)
            ?? throw new InvalidOperationException("Windows could not create the eMilimo Add/Remove Programs registration.");
        key.SetValue("DisplayName", "eMilimo");
        key.SetValue("DisplayVersion", CurrentVersion.ToString());
        key.SetValue("Publisher", "BAU Solutions Limited");
        key.SetValue("InstallLocation", target);
        key.SetValue("DisplayIcon", $"{maintenanceExe},0");
        key.SetValue("UninstallString", $"\"{maintenanceExe}\" /uninstall");
        key.SetValue("ModifyPath", $"\"{maintenanceExe}\" /modify");
        key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
        key.SetValue("EstimatedSize", (int)Math.Min(int.MaxValue, DirectorySize(target) / 1024), RegistryValueKind.DWord);
        key.SetValue("NoModify", 0, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 0, RegistryValueKind.DWord);
        key.SetValue("DeploymentMode", deployMode);
        key.SetValue("Host", NormalHost());
        key.SetValue("WebPort", (int)webPort.Value, RegistryValueKind.DWord);
        key.SetValue("ApiPort", (int)apiPort.Value, RegistryValueKind.DWord);
        key.SetValue("GivingPort", (int)givingPort.Value, RegistryValueKind.DWord);
        key.SetValue("PublicAdminUrl", publicAdminUrl.Text.Trim());
        key.SetValue("PublicApiUrl", publicApiUrl.Text.Trim());
        key.SetValue("PublicGivingUrl", publicGivingUrl.Text.Trim());
        key.SetValue("Instance", instanceName);
        installedProduct = InstalledProduct.Read(UninstallRegistryPath);
        AppendLog("Registered eMilimo in Windows Apps & features.");
    }

    private static long DirectorySize(string path)
    {
        try { return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length); }
        catch { return 0; }
    }

    private static void CreateDesktopShortcut(string target, string url)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "eMilimo.url");
        var icon = Path.Combine(target, "frontend", "web", "dist", "web", "browser", "images", "bau-logo.ico");
        var iconLine = File.Exists(icon) ? $"IconFile={icon}{Environment.NewLine}" : string.Empty;
        File.WriteAllText(path, $"[InternetShortcut]{Environment.NewLine}URL={url.TrimEnd('/')}/{Environment.NewLine}{iconLine}");
    }

    private string NormalHost() => publicHost.Text.Trim().TrimEnd('/').Replace("http://", "", StringComparison.OrdinalIgnoreCase).Replace("https://", "", StringComparison.OrdinalIgnoreCase).Split(':')[0];
    private static string NormalizePublicUrl(string configured, string fallback) =>
        (string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim()).TrimEnd('/');
    private string AdminBaseUrl() => NormalizePublicUrl(publicAdminUrl.Text, $"http://{NormalHost()}:{webPort.Value}");
    private string ApiBaseUrl(string deployMode) => NormalizePublicUrl(publicApiUrl.Text,
        deployMode == "Docker Container (Recommended)" ? $"http://{NormalHost()}:{apiPort.Value}" : AdminBaseUrl());
    private string GivingBaseUrl() => NormalizePublicUrl(publicGivingUrl.Text, $"http://{NormalHost()}:{givingPort.Value}");
    private static string BoundedConnectionString(string value)
    {
        var builder = new NpgsqlConnectionStringBuilder(value) { Timeout = 10, CommandTimeout = 30 };
        return builder.ConnectionString;
    }
    private static string DockerProjectName(string value)
    {
        var cleaned = new string(value.ToLowerInvariant().Where(x => char.IsAsciiLetterOrDigit(x) || x is '-' or '_').ToArray()).Trim('-', '_');
        return $"emilimo-{(string.IsNullOrWhiteSpace(cleaned) ? "main" : cleaned)}";
    }
    private void AppendLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        var line = $"[{DateTime.Now:HH:mm:ss}] {message.Trim()}{Environment.NewLine}";
        try { lock (logSync) File.AppendAllText(installerLogPath, line); } catch { }
        if (IsDisposed || progressLog == null) return;
        void Append()
        {
            if (progressLog == null || progressLog.IsDisposed) return;
            progressLog.AppendText(line);
            progressLog.SelectionStart = progressLog.TextLength;
            progressLog.ScrollToCaret();
        }
        if (InvokeRequired) BeginInvoke((Action)Append); else Append();
    }

    private static async Task WriteRuntimeConfigAsync(string target, string adminApiUrl, string fileBaseUrl, string givingBaseUrl, string givingApiBaseUrl, string congregation, string bootstrapLicenseXml, string licenseUserIdentifier, CancellationToken cancellationToken)
    {
        var path = Path.Combine(target, "frontend", "web", "dist", "web", "browser", "runtime-config.js");
        var content = $"window.__EMILIMO_CONFIG__ = {{ apiUrl: {System.Text.Json.JsonSerializer.Serialize(adminApiUrl)}, fileBaseUrl: {System.Text.Json.JsonSerializer.Serialize(fileBaseUrl)}, givingBaseUrl: {System.Text.Json.JsonSerializer.Serialize(givingBaseUrl)}, congregationName: {System.Text.Json.JsonSerializer.Serialize(congregation)}, bootstrapLicenseXml: {System.Text.Json.JsonSerializer.Serialize(bootstrapLicenseXml)}, licenseUserIdentifier: {System.Text.Json.JsonSerializer.Serialize(licenseUserIdentifier)} }};{Environment.NewLine}";
        await File.WriteAllTextAsync(path, content, cancellationToken);
        var givingPath = Path.Combine(target, "frontend", "giving", "dist", "giving", "browser", "runtime-config.js");
        var givingContent = $"window.EMILIMO_CONFIG = {{ API_URL: {System.Text.Json.JsonSerializer.Serialize(givingApiBaseUrl)}, CHURCH_NAME: {System.Text.Json.JsonSerializer.Serialize(congregation)}, ENVIRONMENT: 'production' }};{Environment.NewLine}";
        await File.WriteAllTextAsync(givingPath, givingContent, cancellationToken);
    }

    private string BuildCacheConnectionString(bool forContainer)
    {
        if (!CacheModeEnabled()) return "localhost:6379,abortConnect=false,connectTimeout=5000,syncTimeout=5000";
        if (CacheModeIsBundled())
            return $"redis:6379,password={installationCachePassword},abortConnect=false,connectTimeout=5000,syncTimeout=5000";

        var host = cacheHost.Text.Trim();
        if (forContainer && host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) host = "host.docker.internal";
        var parts = new List<string>
        {
            $"{host}:{cachePort.Value}",
            "abortConnect=false",
            "connectTimeout=5000",
            "syncTimeout=5000",
            $"defaultDatabase={cacheDatabase.Value}"
        };
        if (!string.IsNullOrWhiteSpace(cacheUsername.Text)) parts.Add($"user={cacheUsername.Text.Trim()}");
        if (!string.IsNullOrEmpty(cachePassword.Text)) parts.Add($"password={cachePassword.Text}");
        if (cacheTls.Checked)
        {
            parts.Add("ssl=true");
            parts.Add($"sslHost={cacheHost.Text.Trim()}");
        }
        return string.Join(',', parts);
    }

    private void LoadCacheConnectionString(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var endpoint = parts[0].Split(':', 2);
        cacheHost.Text = endpoint[0];
        if (endpoint.Length == 2 && decimal.TryParse(endpoint[1], out var port) && port is >= 1 and <= 65535) cachePort.Value = port;
        foreach (var part in parts.Skip(1))
        {
            var option = part.Split('=', 2);
            if (option.Length != 2) continue;
            switch (option[0].ToLowerInvariant())
            {
                case "user": cacheUsername.Text = option[1]; break;
                case "password": cachePassword.Text = option[1]; break;
                case "ssl": cacheTls.Checked = bool.TryParse(option[1], out var tls) && tls; break;
                case "defaultdatabase" when decimal.TryParse(option[1], out var db) && db is >= 0 and <= 15: cacheDatabase.Value = db; break;
            }
        }
    }

    private async Task WriteApiSettingsAsync(string apiPath, int listenPort, int publicGivingPort, CancellationToken cancellationToken)
    {
        var redisEnabled = CacheModeIsExisting();
        var deployMode = deploymentMode.SelectedItem?.ToString() ?? "Standalone Console";
        var allowLoopbackPublicUrls = string.IsNullOrWhiteSpace(publicApiUrl.Text).ToString().ToLowerInvariant();
        var productionSettings = $"{{\n  \"ConnectionStrings\": {{ \"DefaultConnection\": \"{JsonEscape(GetDatabaseConnectionString())}\" }},\n  \"Jwt\": {{ \"Secret\": \"{JsonEscape(installationJwt)}\" }},\n  \"Setup\": {{ \"BootstrapToken\": \"{JsonEscape(installationSetupToken)}\", \"CongregationName\": \"{JsonEscape(congregationName.Text.Trim())}\" }},\n  \"Platform\": {{ \"Instance\": \"{JsonEscape(instanceName)}\", \"DeploymentId\": \"{JsonEscape(systemId)}\" }},\n  \"Frontend\": {{ \"Url\": \"{JsonEscape(AdminBaseUrl())}\" }},\n  \"Giving\": {{ \"Port\": {publicGivingPort}, \"PublicUrl\": \"{JsonEscape(GivingBaseUrl())}\" }},\n  \"DigitalGiving\": {{ \"PublicApiBaseUrl\": \"{JsonEscape(ApiBaseUrl(deployMode))}\", \"AllowLoopbackPublicUrls\": {allowLoopbackPublicUrls} }},\n  \"Cors\": {{ \"AllowedOrigins\": [\"{JsonEscape(AdminBaseUrl())}\", \"{JsonEscape(GivingBaseUrl())}\"] }},\n  \"Redis\": {{ \"Enabled\": {redisEnabled.ToString().ToLowerInvariant()}, \"ConnectionString\": \"{JsonEscape(BuildCacheConnectionString(forContainer: false))}\", \"InstanceName\": \"emilimo:v1:\" }},\n  \"Urls\": \"http://0.0.0.0:{listenPort};http://0.0.0.0:{publicGivingPort}\"\n}}";
        await File.WriteAllTextAsync(Path.Combine(apiPath, "appsettings.Production.json"), productionSettings, cancellationToken);
    }

    private static async Task WriteIisWebConfigAsync(string apiPath, CancellationToken cancellationToken)
    {
        const string webConfig = """
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <remove name="WebDAV" />
        <remove name="aspNetCore" />
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\eMilimo.API.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
""";
        Directory.CreateDirectory(Path.Combine(apiPath, "logs"));
        await File.WriteAllTextAsync(Path.Combine(apiPath, "web.config"), webConfig,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
    }

    private async Task WaitForHttpReadyAsync(int port, TimeSpan timeout, CancellationToken cancellationToken, string? apiPath = null)
    {
        var probe = await ProbeHttpReadyAsync(port, timeout, cancellationToken);
        if (probe.IsReady) return;

        var details = $"Last readiness probe: {probe.LastFailure}";
        if (!string.IsNullOrWhiteSpace(apiPath))
        {
            var stdout = await ReadLatestIisStdoutAsync(apiPath, cancellationToken);
            if (!string.IsNullOrWhiteSpace(stdout)) details += $"{Environment.NewLine}Latest IIS stdout log:{Environment.NewLine}{stdout}";
        }
        AppendLog(details);
        throw new TimeoutException($"eMilimo did not become ready on port {port} within {timeout.TotalMinutes:0.#} minutes. {details}");
    }

    private async Task CompleteFreshSetupAsync(string deployMode, CancellationToken cancellationToken)
    {
        var port = deployMode == "Docker Container (Recommended)" ? (int)apiPort.Value : (int)webPort.Value;
        var licenseXml = signedLicense.Checked
            ? await File.ReadAllTextAsync(licenseFilePath.Text, cancellationToken)
            : null;
        var request = new
        {
            mode = "new",
            churchName = congregationName.Text.Trim(),
            instance = instanceName,
            systemId,
            licenseUserIdentifier,
            frontendBaseUrl = AdminBaseUrl(),
            givingBaseUrl = GivingBaseUrl(),
            title = adminTitle.SelectedItem?.ToString() ?? "",
            firstName = adminFirstName.Text.Trim(),
            lastName = adminLastName.Text.Trim(),
            gender = "",
            email = adminEmail.Text.Trim(),
            phoneNumber = adminPhone.Text.Trim(),
            password = adminPassword.Text,
            setupToken = installationSetupToken,
            licenseXml,
            useTrial = trialLicense.Checked
        };
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var response = await client.PostAsync($"http://127.0.0.1:{port}/api/setup/complete",
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"), cancellationToken);
        if (response.IsSuccessStatusCode) return;
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(detail);
            detail = document.RootElement.TryGetProperty("message", out var message) ? message.GetString() ?? detail : detail;
        }
        catch (JsonException) { }
        throw new InvalidOperationException($"Initial administrator setup failed: {detail}");
    }

    private async Task ValidateSelectedLicenseAsync(string deployMode, CancellationToken cancellationToken)
    {
        var xml = await File.ReadAllTextAsync(licenseFilePath.Text, cancellationToken);
        var port = deployMode == "Docker Container (Recommended)" ? (int)apiPort.Value : (int)webPort.Value;
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var body = JsonSerializer.Serialize(new { licenseXml = xml, setupToken = installationSetupToken, licenseUserIdentifier });
        using var response = await client.PostAsync($"http://127.0.0.1:{port}/api/setup/validate-license",
            new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
        if (response.IsSuccessStatusCode) return;
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(detail);
            detail = document.RootElement.TryGetProperty("message", out var message) ? message.GetString() ?? detail : detail;
        }
        catch (JsonException) { }
        throw new InvalidOperationException($"The selected licence did not validate: {detail}");
    }

    private sealed record ReadinessProbeResult(bool IsReady, string LastFailure);

    private async Task<ReadinessProbeResult> ProbeHttpReadyAsync(int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow.Add(timeout);
        var attempt = 0;
        var lastFailure = "No response was received from the readiness endpoint.";
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;
            try
            {
                using var response = await client.GetAsync($"http://127.0.0.1:{port}/health/ready", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    AppendLog("API readiness check passed.");
                    return new ReadinessProbeResult(true, string.Empty);
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                lastFailure = $"HTTP {(int)response.StatusCode} ({response.StatusCode}) from /health/ready" +
                    (string.IsNullOrWhiteSpace(body) ? "." : $": {Summarize(body)}");
            }
            catch (HttpRequestException ex) { lastFailure = $"Connection to /health/ready failed: {ex.Message}"; }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested) { lastFailure = $"Readiness request timed out: {ex.Message}"; }
            if (attempt == 1 || attempt % 5 == 0)
                AppendLog($"Waiting for database migrations and API readiness... {lastFailure}");
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
        return new ReadinessProbeResult(false, lastFailure);
    }

    private static async Task<string?> ReadLatestIisStdoutAsync(string apiPath, CancellationToken cancellationToken)
    {
        var logDirectory = Path.Combine(apiPath, "logs");
        if (!Directory.Exists(logDirectory)) return null;

        try
        {
            var latestLog = Directory.GetFiles(logDirectory, "stdout*.log")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (latestLog == null) return null;

            const int maxLogBytes = 8192;
            await using var stream = new FileStream(latestLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length > maxLogBytes) stream.Seek(-maxLogBytes, SeekOrigin.End);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            var contents = await reader.ReadToEndAsync(cancellationToken);
            return $"{Path.GetFileName(latestLog)}: {Summarize(contents)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"Could not read IIS stdout log: {ex.Message}";
        }
    }

    private async Task WaitForApplicationAsync(string projectName, string composePath, string workingDirectory, CancellationToken cancellationToken)
    {
        var probe = await ProbeHttpReadyAsync((int)apiPort.Value, TimeSpan.FromMinutes(4), cancellationToken);
        if (probe.IsReady) return;

        var diagnostics = await Run("docker", $"compose -p {projectName} -f \"{composePath}\" ps --all",
            workingDirectory, TimeSpan.FromSeconds(30), cancellationToken: cancellationToken);
        var logs = await Run("docker", $"compose -p {projectName} -f \"{composePath}\" logs --no-color --tail 120 api redis",
            workingDirectory, TimeSpan.FromSeconds(30), cancellationToken: cancellationToken);
        throw new TimeoutException($"eMilimo services did not become ready within four minutes.\nLast readiness probe: {probe.LastFailure}\n\n{Summarize(diagnostics.Output + Environment.NewLine + logs.Output)}");
    }
    private void Warn(string message)
    {
        inlineMessage.Text = message.Replace("\n\n", " ").Replace('\n', ' ');
        status.Text = string.Empty;
    }
    private static bool StrongPassword(string value) => value.Length >= 8 && value.Any(char.IsUpper) &&
        value.Any(char.IsLower) && value.Any(char.IsDigit) && value.Any(ch => !char.IsLetterOrDigit(ch));
    private static string Summarize(string value) => string.IsNullOrWhiteSpace(value) ? "The deployment command failed." : value.Length <= 1800 ? value.Trim() : value[^1800..].Trim();

    private void UpdatePortAvailability()
    {
        var deployMode = deploymentMode.SelectedItem?.ToString() ?? "Docker Container (Recommended)";
        UpdatePortStatus(webPortStatus, (int)webPort.Value, PortIsAvailableForInstall((int)webPort.Value, deployMode), "web");
        if (deployMode == "Docker Container (Recommended)")
            UpdatePortStatus(apiPortStatus, (int)apiPort.Value, PortIsAvailableForInstall((int)apiPort.Value, deployMode), "API");
        UpdatePortStatus(givingPortStatus, (int)givingPort.Value, PortIsAvailableForInstall((int)givingPort.Value, deployMode), "public Giving app");
    }

    private bool PortIsAvailableForInstall(int port, string deployMode)
    {
        if (IsTcpPortAvailable(port)) return true;
        if (maintenanceSession && installedProduct != null && (installedProduct.WebPort == port || installedProduct.ApiPort == port || installedProduct.GivingPort == port)) return true;
        if (deployMode != "IIS Windows Service") return false;
        try
        {
            using var manager = new ServerManager();
            var site = manager.Sites["eMilimo"];
            return site?.Bindings.Any(binding => binding.EndPoint?.Port == port) == true;
        }
        catch { return false; }
    }

    private static bool IsTcpPortAvailable(int port) =>
        !IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(endpoint => endpoint.Port == port);

    private static void UpdatePortStatus(Label label, int port, bool available, string purpose)
    {
        label.Text = available ? $"✓ Port {port} is available for the {purpose}." : $"✗ Port {port} is already in use.";
        label.ForeColor = available ? Success : Danger;
    }

    private static TableLayoutPanel Page(string title, string subtitle)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(2, 14, 12, 18) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 22, FontStyle.Bold), Margin = new Padding(0) });
        layout.Controls.Add(new Label { Text = subtitle, AutoSize = true, Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 6, 0, 18) });
        return layout;
    }

    private static Panel ScrollPage(Control child) { var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true }; panel.Controls.Add(child); return panel; }
    private static Control Field(string label, Control control, string? hint = null)
    {
        var box = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 1, Margin = new Padding(0, 7, 8, 4) };
        box.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        box.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 0, 0, 6) });
        control.Margin = new Padding(0);
        control.Dock = DockStyle.Top;
        box.Controls.Add(control);
        if (!string.IsNullOrWhiteSpace(hint)) box.Controls.Add(new Label { Text = hint, AutoSize = true, Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font("Segoe UI", 8), Margin = new Padding(0, 5, 0, 0) });
        return box;
    }
    
    private static Control InfoCard(string title, string copy)
    {
        var box = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(16), Margin = new Padding(0, 0, 8, 12) };
        box.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        box.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 11, FontStyle.Bold) });
        box.Controls.Add(new Label { Text = copy, AutoSize = true, Dock = DockStyle.Fill, ForeColor = Muted, Margin = new Padding(0, 7, 0, 0) });
        return box;
    }

    private static TableLayoutPanel ResponsiveGrid(int columns, params float[] widths)
    {
        var grid = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = columns, Margin = new Padding(0) };
        for (var i = 0; i < columns; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, widths[i]));
        return grid;
    }

    private static TableLayoutPanel ResponsiveRow(int trailingWidth)
    {
        var row = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, Margin = new Padding(0, 4, 0, 4) };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, trailingWidth));
        return row;
    }

    private static FlowLayoutPanel SectionCard(string title, string copy)
    {
        var card = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Surface, Padding = new Padding(14), Margin = new Padding(0, 5, 8, 10) };
        card.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 10, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = copy, AutoSize = true, ForeColor = Muted, Margin = new Padding(0, 3, 0, 8) });
        return card;
    }

    private static Button LinkButton(string text)
    {
        var button = new Button { Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, ForeColor = Forest, BackColor = Color.White, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 12, 0) };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private void UpdateWelcomeReadiness()
    {
        if (page != 0 || installing) return;
        next.Enabled = termsReviewed && privacyReviewed && acceptTerms.Checked && acceptPrivacy.Checked;
        if (!termsReviewed || !privacyReviewed)
            status.Text = "Review both documents to continue.";
        else if (!acceptTerms.Checked || !acceptPrivacy.Checked)
            status.Text = "Accept both reviewed documents to continue.";
        else
            status.Text = string.Empty;
    }

    private bool ShowReviewDocument(string title, string copy)
    {
        using var dialog = new Form { Text = title, StartPosition = FormStartPosition.CenterParent, Size = new Size(620, 480), MinimumSize = new Size(480, 360), Font = Font, BackColor = Color.White, ShowInTaskbar = false };
        var text = new RichTextBox { Text = copy, ReadOnly = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Color.White, ForeColor = Ink, DetectUrls = true, Padding = new Padding(16) };
        var instruction = new Label { Text = "Scroll to the end to confirm you have reviewed this document.", AutoSize = true, ForeColor = Muted, Margin = new Padding(0, 8, 12, 0) };
        var close = PrimaryButton("Reviewed — close");
        close.Enabled = false;
        close.Click += (_, _) => { dialog.DialogResult = DialogResult.OK; dialog.Close(); };
        void CheckEnd()
        {
            if (text.TextLength == 0) return;
            var lastVisible = text.GetCharIndexFromPosition(new Point(Math.Max(1, text.ClientSize.Width - 8), Math.Max(1, text.ClientSize.Height - 8)));
            if (lastVisible < text.TextLength - 4) return;
            close.Enabled = true;
            instruction.Text = "Document reviewed.";
            instruction.ForeColor = Success;
        }
        text.VScroll += (_, _) => CheckEnd();
        text.KeyUp += (_, _) => CheckEnd();
        text.MouseWheel += (_, _) => BeginInvoke((Action)CheckEnd);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12, 8, 12, 8) };
        footer.Controls.Add(close);
        footer.Controls.Add(instruction);
        dialog.Controls.Add(text);
        dialog.Controls.Add(footer);
        dialog.Shown += (_, _) => CheckEnd();
        return dialog.ShowDialog(this) == DialogResult.OK;
    }

    private static Control DocumentBox(string title, string copy)
    {
        var box = new TableLayoutPanel { AutoSize = true, Width = 625, Margin = new Padding(0, 0, 0, 7) };
        box.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 11, FontStyle.Bold), Margin = new Padding(0, 0, 0, 7) });
        
        var textBox = new RichTextBox
        {
            Text = copy,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Width = 610,
            Height = 142,
            BackColor = Surface,
            ForeColor = Ink,
            BorderStyle = BorderStyle.FixedSingle,
            WordWrap = true,
            DetectUrls = true,
            ZoomFactor = 1.0f
        };
        
        textBox.LinkClicked += (sender, e) =>
        {
            try
            {
                if (e.LinkText != null)
                {
                    var linkText = e.LinkText.Trim();
                    
                    if (linkText.Contains('@') && !linkText.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                    {
                        linkText = "mailto:" + linkText;
                    }
                    else if (!linkText.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !linkText.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        linkText = "https://" + linkText;
                    }
                    
                    Process.Start(new ProcessStartInfo(linkText) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        
        box.Controls.Add(textBox);
        return box;
    }

    private static CheckBox Component(string title, string note, bool selected, bool enabled) => 
        new() { Text = $"{title}   ·   {note}", Checked = selected, Enabled = enabled, AutoSize = true, ForeColor = enabled ? Ink : Muted, Margin = new Padding(0, 0, 0, 14) };
    private static Label PortStatusLabel() => new() { Text = "Choose a port, then check availability.", AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI", 8), Margin = new Padding(0, 5, 0, 0) };
    private static TextBox Input(string value = "") => new() { Text = value, Height = 36, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
    private static NumericUpDown PortInput(int value) => new() { Minimum = 1, Maximum = 65535, Value = value, Height = 36, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
    private static Button PrimaryButton(string text) { var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(125, 38), BackColor = Forest, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 0, 0, 0) }; button.FlatAppearance.BorderSize = 0; return button; }
    private static Button SecondaryButton(string text) { var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(90, 38), BackColor = Color.White, ForeColor = Ink, FlatStyle = FlatStyle.Flat, Margin = new Padding(8, 0, 0, 0) }; button.FlatAppearance.BorderColor = Line; return button; }

    private static string Secret(int bytes) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).Replace("+", "A").Replace("/", "B").TrimEnd('=');
    private static Image? LoadImage(string name) { using var stream = typeof(SetupForm).Assembly.GetManifestResourceStream(name); if (stream == null) return null; using var source = new Bitmap(stream); return new Bitmap(source); }
    
    private static string ReadResource(string name) 
    { 
        using var stream = typeof(SetupForm).Assembly.GetManifestResourceStream(name); 
        if (stream == null) return "Document unavailable."; 
        using var reader = new StreamReader(stream); 
        var text = reader.ReadToEnd();
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        return text; 
    }
    
    private static async Task<bool> CommandWorks(string file, string args, string cwd, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        try { return (await Run(file, args, cwd, timeout, cancellationToken: cancellationToken)).ExitCode == 0; }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return false; }
    }
    private static async Task<(int ExitCode, string Output)> Run(string file, string args, string cwd,
        TimeSpan? timeout = null, Action<string>? onOutput = null, CancellationToken cancellationToken = default)
    {
        using var process = new Process { StartInfo = new ProcessStartInfo(file, args)
            { WorkingDirectory = cwd, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } };
        var output = new StringBuilder();
        var outputSync = new object();
        void Capture(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            lock (outputSync) output.AppendLine(value);
            onOutput?.Invoke(value);
        }
        process.OutputDataReceived += (_, e) => Capture(e.Data);
        process.ErrorDataReceived += (_, e) => Capture(e.Data);
        if (!process.Start()) throw new InvalidOperationException($"Could not start {file}.");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using var timeoutSource = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
            process.WaitForExit();
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(true); } catch { }
            if (cancellationToken.IsCancellationRequested) throw;
            throw new TimeoutException($"{file} did not finish within {timeout?.TotalMinutes:0.#} minutes and was stopped.");
        }
        lock (outputSync) return (process.ExitCode, output.ToString());
    }
    
    internal static string? FindPackageRoot()
    {
        var embedded = ExtractEmbeddedPackage();
        if (embedded != null) return embedded;
        foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            var current = new DirectoryInfo(start);
            while (current != null) 
            { 
                if (File.Exists(Path.Combine(current.FullName, "deployment", "docker-compose.installer.yml"))) 
                    return current.FullName; 
                current = current.Parent; 
            }
        }
        return null;
    }

    private static string? ExtractEmbeddedPackage()
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable)) return null;
        var marker = Encoding.ASCII.GetBytes(PayloadMarker);
        using var source = new FileStream(executable, FileMode.Open, FileAccess.Read, FileShare.Read);
        var tailLength = (int)Math.Min(source.Length, 256 * 1024);
        var tail = new byte[tailLength];
        source.Position = source.Length - tailLength;
        source.ReadExactly(tail);
        var markerIndex = LastIndexOf(tail, marker);
        if (markerIndex < 40) return null;

        var markerOffset = source.Length - tailLength + markerIndex;
        source.Position = markerOffset - 40;
        using var metadata = new BinaryReader(source, Encoding.UTF8, leaveOpen: true);
        var payloadLength = metadata.ReadInt64();
        var expectedHash = metadata.ReadBytes(32);
        var payloadOffset = markerOffset - 40 - payloadLength;
        if (payloadLength <= 0 || payloadOffset < 0 || expectedHash.Length != 32)
            throw new InvalidDataException("The embedded installation payload metadata is invalid.");

        source.Position = payloadOffset;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var remaining = payloadLength;
        var buffer = new byte[1024 * 1024];
        while (remaining > 0)
        {
            var read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read <= 0) throw new EndOfStreamException("The embedded installation payload is incomplete.");
            hash.AppendData(buffer, 0, read);
            remaining -= read;
        }
        if (!CryptographicOperations.FixedTimeEquals(hash.GetHashAndReset(), expectedHash))
            throw new CryptographicException("The embedded installation payload failed its integrity check.");

        var hashText = Convert.ToHexString(expectedHash).ToLowerInvariant();
        var cacheBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BAU Solutions", "eMilimo", "InstallerCache");
        var cacheRoot = Path.Combine(cacheBase, $"1.0.1-{hashText[..16]}");
        var ready = Path.Combine(cacheRoot, ".payload-ready");
        if (File.Exists(ready) && File.Exists(Path.Combine(cacheRoot, "deployment", "docker-compose.installer.yml")))
            return cacheRoot;

        Directory.CreateDirectory(cacheBase);
        var extracting = Path.Combine(cacheBase, $"extracting-{Guid.NewGuid():N}");
        Directory.CreateDirectory(extracting);
        try
        {
            source.Position = payloadOffset;
            using (var bounded = new BoundedReadStream(source, payloadOffset, payloadLength))
            using (var archive = new ZipArchive(bounded, ZipArchiveMode.Read, leaveOpen: false))
            {
                var extractionRoot = Path.GetFullPath(extracting) + Path.DirectorySeparatorChar;
                foreach (var entry in archive.Entries)
                {
                    var destinationPath = Path.GetFullPath(Path.Combine(extracting, entry.FullName));
                    if (!destinationPath.StartsWith(extractionRoot, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The embedded payload contains an unsafe path.");
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
            File.WriteAllText(Path.Combine(extracting, ".payload-ready"), hashText, new UTF8Encoding(false));
            if (Directory.Exists(cacheRoot)) Directory.Delete(cacheRoot, recursive: true);
            Directory.Move(extracting, cacheRoot);
            return cacheRoot;
        }
        catch
        {
            try { if (Directory.Exists(extracting)) Directory.Delete(extracting, recursive: true); } catch { }
            throw;
        }
    }

    private static int LastIndexOf(byte[] source, byte[] value)
    {
        for (var i = source.Length - value.Length; i >= 0; i--)
        {
            var match = true;
            for (var j = 0; j < value.Length; j++)
                if (source[i + j] != value[j]) { match = false; break; }
            if (match) return i;
        }
        return -1;
    }
}

internal sealed class OnlinePrerequisiteManifest
{
    public int SchemaVersion { get; init; }
    public List<OnlinePrerequisite> Prerequisites { get; init; } = [];
}

internal sealed class OnlinePrerequisite
{
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
}

internal sealed class PayloadPreparationDialog : Form
{
    private readonly ProgressBar progress = new() { Dock = DockStyle.Top, Height = 8, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 24 };

    private PayloadPreparationDialog()
    {
        Text = "eMilimo Setup";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ControlBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(470, 150);
        BackColor = Color.White;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(28, 24, 28, 22) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        layout.Controls.Add(new Label { Text = "Preparing eMilimo", AutoSize = true, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(24, 29, 27) }, 0, 0);
        layout.Controls.Add(new Label { Text = "Verifying and unpacking the secure installation payload…", AutoSize = true, ForeColor = Color.FromArgb(102, 112, 107), Margin = new Padding(0, 8, 0, 14) }, 0, 1);
        layout.Controls.Add(progress, 0, 2);
        Controls.Add(layout);
    }

    public static string? Prepare()
    {
        string? result = null;
        Exception? failure = null;
        using var dialog = new PayloadPreparationDialog();
        dialog.Shown += async (_, _) =>
        {
            try { result = await Task.Run(SetupForm.FindPackageRoot); }
            catch (Exception ex) { failure = ex; }
            finally { dialog.Close(); }
        };
        System.Windows.Forms.Application.Run(dialog);
        if (failure != null) throw new InvalidOperationException(failure.Message, failure);
        return result;
    }
}

internal sealed class BoundedReadStream(Stream inner, long start, long length) : Stream
{
    private long position;
    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => length;
    public override long Position { get => position; set => Seek(value, SeekOrigin.Begin); }
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (position >= length) return 0;
        inner.Position = start + position;
        var read = inner.Read(buffer, offset, (int)Math.Min(count, length - position));
        position += read;
        return read;
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        var target = origin switch { SeekOrigin.Begin => offset, SeekOrigin.Current => position + offset, SeekOrigin.End => length + offset, _ => throw new ArgumentOutOfRangeException(nameof(origin)) };
        if (target < 0 || target > length) throw new IOException("Attempted to seek outside the embedded payload.");
        position = target;
        return position;
    }
    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

internal sealed class UninstallProgressDialog : Form
{
    private readonly ProgressBar progress = new() { Minimum = 0, Maximum = 100, Value = 0, Dock = DockStyle.Top, Height = 24, Style = ProgressBarStyle.Continuous };
    private readonly Label message = new() { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(24, 29, 27) };
    private readonly Label percentage = new() { AutoSize = true, Dock = DockStyle.Right, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(20, 75, 56) };

    public UninstallProgressDialog(bool removingData)
    {
        Text = "Uninstalling eMilimo";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ControlBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 180);
        BackColor = Color.White;
        var title = new Label { Text = "Removing eMilimo", AutoSize = true, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(24, 29, 27), Margin = new Padding(0, 0, 0, 5) };
        var description = new Label
        {
            Text = removingData ? "Application files and eMilimo-managed data are being removed." : "Application files are being removed and your data is being preserved.",
            AutoSize = true, ForeColor = Color.FromArgb(102, 112, 107), MaximumSize = new Size(510, 0), Margin = new Padding(0, 0, 0, 18)
        };
        var statusRow = new TableLayoutPanel { Dock = DockStyle.Top, Height = 36, ColumnCount = 2 };
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        statusRow.Controls.Add(message, 0, 0);
        statusRow.Controls.Add(percentage, 1, 0);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(25, 22, 25, 20) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(description, 0, 1);
        layout.Controls.Add(progress, 0, 2);
        layout.Controls.Add(statusRow, 0, 3);
        Controls.Add(layout);
    }

    public void SetProgress(int value, string text)
    {
        progress.Value = Math.Clamp(value, progress.Minimum, progress.Maximum);
        message.Text = text;
        percentage.Text = $"{progress.Value}%";
        Refresh();
    }

    public void MarkFailed(string text)
    {
        message.Text = text;
        message.ForeColor = Color.FromArgb(185, 45, 45);
        percentage.Text = "Stopped";
        Refresh();
    }
}

public class Milestone
{
    public string Text { get; set; } = string.Empty;
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
    public Control? Control { get; set; }
    public Label? Icon { get; set; }
    public Label? TextControl { get; set; }
}

public enum MilestoneStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

public sealed record InstalledProduct(
    Version Version,
    string InstallLocation,
    string DeploymentMode,
    string Host,
    int WebPort,
    int ApiPort,
    int GivingPort,
    string Instance,
    string PublicAdminUrl,
    string PublicApiUrl,
    string PublicGivingUrl)
{
    public static InstalledProduct? Read(string registryPath)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(registryPath, writable: false);
            if (key == null) return null;
            var location = key.GetValue("InstallLocation")?.ToString();
            var versionText = key.GetValue("DisplayVersion")?.ToString();
            if (string.IsNullOrWhiteSpace(location) || !Version.TryParse(versionText, out var version)) return null;
            return new InstalledProduct(
                version,
                location,
                key.GetValue("DeploymentMode")?.ToString() ?? "IIS Windows Service",
                key.GetValue("Host")?.ToString() ?? "localhost",
                Convert.ToInt32(key.GetValue("WebPort") ?? 4200),
                Convert.ToInt32(key.GetValue("ApiPort") ?? 5290),
                Convert.ToInt32(key.GetValue("GivingPort") ?? 9200),
                key.GetValue("Instance")?.ToString() ?? "main",
                key.GetValue("PublicAdminUrl")?.ToString() ?? string.Empty,
                key.GetValue("PublicApiUrl")?.ToString() ?? string.Empty,
                key.GetValue("PublicGivingUrl")?.ToString() ?? string.Empty);
        }
        catch { return null; }
    }
}

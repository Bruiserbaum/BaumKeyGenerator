using System.Drawing;
using System.Windows.Forms;

namespace BaumKeyGenerator;

public class MainForm : Form
{
    // ── Controls ────────────────────────────────────────────────────────────
    private readonly ComboBox    _typeCombo       = new();
    private readonly TextBox     _purposeBox      = new();
    private readonly Panel       _vaultPanel      = new();
    private readonly TextBox     _vaultPassBox    = new();
    private readonly CheckBox    _vaultAutoCheck  = new();
    private readonly Button      _generateBtn     = new() { Text = "Generate" };
    private readonly TextBox     _outputBox       = new();
    private readonly TextBox     _vaultPassOut    = new();   // shows plain password for vaultwarden
    private readonly Button      _copyOutputBtn   = new() { Text = "Copy" };
    private readonly Button      _copyPassBtn     = new() { Text = "Copy Password" };
    private readonly Label       _copiedLabel     = new() { Text = "Copied!", Visible = false };
    private readonly ListView    _historyView     = new();
    private readonly Button      _clearHistBtn    = new() { Text = "Clear History" };
    private readonly Label       _statusLabel     = new();

    private List<HistoryEntry>   _history         = [];
    private string?              _lastVaultPass;

    public MainForm()
    {
        BuildUI();
        LoadHistory();
    }

    // ── UI Construction ──────────────────────────────────────────────────────

    private void BuildUI()
    {
        Text            = "BaumKeyGenerator";
        Size            = new Size(860, 720);
        MinimumSize     = new Size(760, 620);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        AppTheme.ApplyToForm(this);

        // ── Outer layout: top panel + history panel ──
        var outer = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 2,
            ColumnCount = 1,
            BackColor   = AppTheme.Background,
            Padding     = new Padding(0),
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 340));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(outer);

        // ── Top card ──
        var topCard = MakeCard();
        outer.Controls.Add(topCard, 0, 0);

        // ── History card ──
        var histCard = MakeCard();
        histCard.Padding = new Padding(16, 12, 16, 12);
        outer.Controls.Add(histCard, 0, 1);

        BuildTopCard(topCard);
        BuildHistoryCard(histCard);
    }

    private static Panel MakeCard()
    {
        return new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding   = new Padding(20, 16, 20, 16),
            Margin    = new Padding(10),
        };
    }

    private void BuildTopCard(Panel card)
    {
        // Title
        var title = new Label
        {
            Text      = "BaumKeyGenerator",
            Dock      = DockStyle.Top,
            Height    = 36,
            Font      = AppTheme.FontTitle,
            ForeColor = AppTheme.TextPrimary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        card.Controls.Add(title);

        // Two-column inner layout: left = controls, right = output
        var inner = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 1,
            ColumnCount = 2,
            BackColor   = Color.Transparent,
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.Controls.Add(inner);

        BuildLeftControls(inner);
        BuildRightOutput(inner);
    }

    private void BuildLeftControls(TableLayoutPanel parent)
    {
        var left = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 4, 16, 0),
        };
        parent.Controls.Add(left, 0, 0);

        // Key Type
        var typeLabel = new Label { Text = "Key Type", Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent };
        AppTheme.Style(typeLabel);

        foreach (KeyType kt in Enum.GetValues<KeyType>())
            _typeCombo.Items.Add(KeyGenerator.DisplayName(kt));
        _typeCombo.SelectedIndex = 0;
        _typeCombo.DropDownStyle  = ComboBoxStyle.DropDownList;
        _typeCombo.Dock           = DockStyle.Top;
        _typeCombo.Height         = 30;
        AppTheme.Style(_typeCombo);
        _typeCombo.SelectedIndexChanged += OnTypeChanged;

        // Purpose
        var purposeLabel = new Label { Text = "Purpose / Label", Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent };
        AppTheme.Style(purposeLabel);
        _purposeBox.Dock        = DockStyle.Top;
        _purposeBox.Height      = 30;
        _purposeBox.PlaceholderText = "e.g. immich SECRET_KEY, vaultwarden, mailcow…";
        AppTheme.Style(_purposeBox);
        _purposeBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; GenerateKey(); } };

        // Vaultwarden-specific panel
        BuildVaultPanel();

        // Generate button
        _generateBtn.Dock   = DockStyle.Top;
        _generateBtn.Height = 38;
        AppTheme.StylePrimary(_generateBtn);
        _generateBtn.Click += (_, _) => GenerateKey();

        // Status label
        _statusLabel.Dock      = DockStyle.Top;
        _statusLabel.Height    = 22;
        _statusLabel.Font      = AppTheme.FontSmall;
        _statusLabel.ForeColor = AppTheme.TextSecondary;
        _statusLabel.BackColor = Color.Transparent;
        _statusLabel.Text      = "Ready.";

        // Add in reverse order (DockStyle.Top stacks from bottom)
        left.Controls.Add(_statusLabel);
        left.Controls.Add(Spacer(6));
        left.Controls.Add(_generateBtn);
        left.Controls.Add(Spacer(10));
        left.Controls.Add(_vaultPanel);
        left.Controls.Add(Spacer(4));
        left.Controls.Add(_purposeBox);
        left.Controls.Add(purposeLabel);
        left.Controls.Add(Spacer(6));
        left.Controls.Add(_typeCombo);
        left.Controls.Add(typeLabel);
    }

    private void BuildVaultPanel()
    {
        _vaultPanel.Dock      = DockStyle.Top;
        _vaultPanel.Height    = 80;
        _vaultPanel.BackColor = Color.Transparent;
        _vaultPanel.Visible   = false;

        _vaultAutoCheck.Text      = "Auto-generate password";
        _vaultAutoCheck.Checked   = true;
        _vaultAutoCheck.Dock      = DockStyle.Top;
        _vaultAutoCheck.Height    = 22;
        _vaultAutoCheck.ForeColor = AppTheme.TextSecondary;
        _vaultAutoCheck.BackColor = Color.Transparent;
        _vaultAutoCheck.Font      = AppTheme.FontSmall;
        _vaultAutoCheck.CheckedChanged += (_, _) => _vaultPassBox.Enabled = !_vaultAutoCheck.Checked;

        var passLabel = new Label { Text = "Password (for Argon2id hash)", Dock = DockStyle.Top, Height = 18, BackColor = Color.Transparent };
        passLabel.Font      = AppTheme.FontSmall;
        passLabel.ForeColor = AppTheme.TextSecondary;

        _vaultPassBox.Dock          = DockStyle.Top;
        _vaultPassBox.Height        = 28;
        _vaultPassBox.PasswordChar  = '●';
        _vaultPassBox.Enabled       = false;
        _vaultPassBox.PlaceholderText = "Leave blank to auto-generate";
        AppTheme.Style(_vaultPassBox);

        _vaultPanel.Controls.Add(_vaultAutoCheck);
        _vaultPanel.Controls.Add(Spacer(3));
        _vaultPanel.Controls.Add(_vaultPassBox);
        _vaultPanel.Controls.Add(passLabel);
    }

    private void BuildRightOutput(TableLayoutPanel parent)
    {
        var right = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding   = new Padding(0, 4, 0, 0),
        };
        parent.Controls.Add(right, 1, 0);

        // Output header row
        var outHeaderPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 24,
            BackColor = Color.Transparent,
        };
        var outLabel = new Label
        {
            Text      = "Generated Key",
            Dock      = DockStyle.Left,
            Width     = 200,
            BackColor = Color.Transparent,
        };
        AppTheme.Style(outLabel);
        _copiedLabel.Dock      = DockStyle.Right;
        _copiedLabel.Width     = 60;
        _copiedLabel.ForeColor = AppTheme.Success;
        _copiedLabel.Font      = AppTheme.FontSmall;
        _copiedLabel.TextAlign = ContentAlignment.MiddleRight;
        _copiedLabel.BackColor = Color.Transparent;
        outHeaderPanel.Controls.Add(_copiedLabel);
        outHeaderPanel.Controls.Add(outLabel);

        // Main output textbox
        _outputBox.Multiline  = true;
        _outputBox.ReadOnly   = true;
        _outputBox.ScrollBars = ScrollBars.Vertical;
        _outputBox.Dock       = DockStyle.Top;
        _outputBox.Height     = 100;
        _outputBox.PlaceholderText = "Key will appear here…";
        AppTheme.Style(_outputBox, mono: true);

        // Copy main output
        _copyOutputBtn.Dock   = DockStyle.Top;
        _copyOutputBtn.Height = 30;
        AppTheme.StyleSecondary(_copyOutputBtn);
        _copyOutputBtn.Click += (_, _) => CopyToClipboard(_outputBox.Text, _copiedLabel);

        // Vaultwarden plain password row (hidden unless vaultwarden type)
        var passHeaderLabel = new Label
        {
            Text      = "Admin Password (enter this in Vaultwarden login)",
            Dock      = DockStyle.Top,
            Height    = 20,
            BackColor = Color.Transparent,
        };
        AppTheme.Style(passHeaderLabel, secondary: true);
        passHeaderLabel.Font = AppTheme.FontSmall;

        _vaultPassOut.ReadOnly = true;
        _vaultPassOut.Dock     = DockStyle.Top;
        _vaultPassOut.Height   = 30;
        AppTheme.Style(_vaultPassOut, mono: true);

        _copyPassBtn.Dock    = DockStyle.Top;
        _copyPassBtn.Height  = 28;
        AppTheme.StyleSecondary(_copyPassBtn);
        _copyPassBtn.Click  += (_, _) => CopyToClipboard(_vaultPassOut.Text, _copiedLabel);

        // Group the vault-password output elements so we can hide/show together
        var vaultOutPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 84,
            BackColor = Color.Transparent,
            Tag       = "vaultout",
            Visible   = false,
        };
        vaultOutPanel.Controls.Add(_copyPassBtn);
        vaultOutPanel.Controls.Add(_vaultPassOut);
        vaultOutPanel.Controls.Add(passHeaderLabel);

        // Stack (reverse order for DockStyle.Top)
        right.Controls.Add(vaultOutPanel);
        right.Controls.Add(Spacer(6));
        right.Controls.Add(_copyOutputBtn);
        right.Controls.Add(_outputBox);
        right.Controls.Add(Spacer(4));
        right.Controls.Add(outHeaderPanel);
    }

    private void BuildHistoryCard(Panel card)
    {
        // Header row
        var hdrPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 30,
            BackColor = Color.Transparent,
        };
        var hdrLabel = new Label
        {
            Text      = "Generation History  (stored encrypted, current user only)",
            Dock      = DockStyle.Left,
            Width     = 500,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        AppTheme.Style(hdrLabel, secondary: true);
        hdrLabel.Font = AppTheme.FontSmall;

        _clearHistBtn.Dock   = DockStyle.Right;
        _clearHistBtn.Width  = 110;
        AppTheme.StyleDanger(_clearHistBtn);
        _clearHistBtn.Click += OnClearHistory;

        hdrPanel.Controls.Add(_clearHistBtn);
        hdrPanel.Controls.Add(hdrLabel);

        // ListView
        _historyView.Dock          = DockStyle.Fill;
        _historyView.View          = View.Details;
        _historyView.FullRowSelect  = true;
        _historyView.GridLines      = false;
        _historyView.BackColor      = AppTheme.SurfaceAlt;
        _historyView.ForeColor      = AppTheme.TextPrimary;
        _historyView.BorderStyle    = BorderStyle.None;
        _historyView.Font           = AppTheme.FontSmall;
        _historyView.HeaderStyle    = ColumnHeaderStyle.Nonclickable;
        _historyView.MultiSelect    = false;

        _historyView.Columns.Add("Date / Time",  138);
        _historyView.Columns.Add("Type",         160);
        _historyView.Columns.Add("Purpose",      200);
        _historyView.Columns.Add("Value",        -2);    // fill remaining

        _historyView.DoubleClick += OnHistoryDoubleClick;

        card.Controls.Add(_historyView);
        card.Controls.Add(Spacer(6));
        card.Controls.Add(hdrPanel);
    }

    // ── Logic ────────────────────────────────────────────────────────────────

    private void GenerateKey()
    {
        var type    = (KeyType)_typeCombo.SelectedIndex;
        var purpose = _purposeBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(purpose))
        {
            _purposeBox.Focus();
            FlashStatus("Please enter a purpose/label first.", error: true);
            return;
        }

        _statusLabel.Text      = "Generating…";
        _statusLabel.ForeColor = AppTheme.TextSecondary;
        Application.DoEvents();

        string value;
        _lastVaultPass = null;
        HideVaultOut();

        try
        {
            switch (type)
            {
                case KeyType.GeneralHex32:
                    value = KeyGenerator.GeneralHex32();
                    break;

                case KeyType.GeneralBase64:
                    value = KeyGenerator.GeneralBase64();
                    break;

                case KeyType.JwtSecret:
                    value = KeyGenerator.JwtSecret();
                    break;

                case KeyType.DatabasePassword:
                    value = KeyGenerator.DatabasePassword();
                    break;

                case KeyType.AlphanumericKey:
                    value = KeyGenerator.Alphanumeric();
                    break;

                case KeyType.VaultwardenToken:
                    string? providedPass = _vaultAutoCheck.Checked ? null : _vaultPassBox.Text.Trim();
                    if (!_vaultAutoCheck.Checked && string.IsNullOrEmpty(providedPass))
                    {
                        FlashStatus("Enter a password or check auto-generate.", error: true);
                        return;
                    }
                    var (pass, phc) = KeyGenerator.VaultwardenToken(providedPass);
                    _lastVaultPass  = pass;
                    value           = phc;
                    ShowVaultOut(pass);
                    break;

                default:
                    value = KeyGenerator.GeneralHex32();
                    break;
            }
        }
        catch (Exception ex)
        {
            FlashStatus($"Error: {ex.Message}", error: true);
            return;
        }

        _outputBox.Text = value;

        var entry = new HistoryEntry(
            GeneratedAt: DateTime.Now,
            KeyType:     KeyGenerator.DisplayName(type),
            Purpose:     purpose,
            Value:       value
        );

        _history.Insert(0, entry);
        HistoryStore.Append(entry);
        RefreshHistoryView();
        FlashStatus($"Generated {KeyGenerator.DisplayName(type)} for '{purpose}'.");
    }

    private void OnTypeChanged(object? sender, EventArgs e)
    {
        var type = (KeyType)_typeCombo.SelectedIndex;
        _vaultPanel.Visible = type == KeyType.VaultwardenToken;
        HideVaultOut();
        _outputBox.Clear();
    }

    private void ShowVaultOut(string password)
    {
        _vaultPassOut.Text = password;
        var panel = FindVaultOutPanel();
        if (panel != null) panel.Visible = true;
    }

    private void HideVaultOut()
    {
        _vaultPassOut.Clear();
        var panel = FindVaultOutPanel();
        if (panel != null) panel.Visible = false;
    }

    private Panel? FindVaultOutPanel()
    {
        // Recursively find the panel tagged "vaultout"
        return FindTaggedPanel(this, "vaultout");
    }

    private static Panel? FindTaggedPanel(Control parent, string tag)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is Panel p && p.Tag?.ToString() == tag) return p;
            var found = FindTaggedPanel(c, tag);
            if (found != null) return found;
        }
        return null;
    }

    private void OnHistoryDoubleClick(object? sender, EventArgs e)
    {
        if (_historyView.SelectedItems.Count == 0) return;
        int idx = _historyView.SelectedItems[0].Index;
        if (idx < 0 || idx >= _history.Count) return;
        var entry = _history[idx];
        CopyToClipboard(entry.Value, _copiedLabel);
        FlashStatus($"Copied value for '{entry.Purpose}' to clipboard.");
    }

    private void OnClearHistory(object? sender, EventArgs e)
    {
        if (MessageBox.Show(
                "Clear all history? This cannot be undone.",
                "Clear History",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _history.Clear();
        HistoryStore.Clear();
        _historyView.Items.Clear();
        FlashStatus("History cleared.");
    }

    private void LoadHistory()
    {
        _history = HistoryStore.Load();
        RefreshHistoryView();
    }

    private void RefreshHistoryView()
    {
        _historyView.Items.Clear();
        foreach (var e in _history)
        {
            string displayValue = e.KeyType.Contains("Vaultwarden")
                ? e.Value[..Math.Min(40, e.Value.Length)] + "…"
                : e.Value[..Math.Min(32, e.Value.Length)] + (e.Value.Length > 32 ? "…" : "");

            var item = new ListViewItem(e.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            item.SubItems.Add(e.KeyType);
            item.SubItems.Add(e.Purpose);
            item.SubItems.Add(displayValue);
            _historyView.Items.Add(item);
        }
    }

    private static void CopyToClipboard(string text, Label? indicator = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        Clipboard.SetText(text);
        if (indicator == null) return;
        indicator.Visible = true;
        var t = new System.Windows.Forms.Timer { Interval = 1800 };
        t.Tick += (_, _) => { indicator.Visible = false; t.Stop(); t.Dispose(); };
        t.Start();
    }

    private void FlashStatus(string msg, bool error = false)
    {
        _statusLabel.Text      = msg;
        _statusLabel.ForeColor = error ? AppTheme.Danger : AppTheme.Success;
        if (error) return;
        var t = new System.Windows.Forms.Timer { Interval = 4000 };
        t.Tick += (_, _) =>
        {
            _statusLabel.Text      = "Ready.";
            _statusLabel.ForeColor = AppTheme.TextSecondary;
            t.Stop(); t.Dispose();
        };
        t.Start();
    }

    private static Panel Spacer(int h) =>
        new() { Dock = DockStyle.Top, Height = h, BackColor = Color.Transparent };
}

using System.Drawing;
using System.Windows.Forms;

namespace BaumKeyGenerator;

public class MainForm : Form
{
    // ── Controls ────────────────────────────────────────────────────────────
    private readonly ComboBox _typeCombo      = new();
    private readonly TextBox  _purposeBox     = new();
    private readonly Panel    _vaultPanel     = new();
    private readonly TextBox  _vaultPassBox   = new();
    private readonly CheckBox _vaultAutoCheck = new();
    private readonly Button   _generateBtn    = new() { Text = "Generate" };
    private readonly TextBox  _outputBox      = new();
    private readonly TextBox  _vaultPassOut   = new();
    private readonly Button   _copyOutputBtn  = new() { Text = "Copy" };
    private readonly Button   _copyPassBtn    = new() { Text = "Copy Password" };
    private readonly Label    _copiedLabel    = new() { Text = "Copied!", Visible = false };
    private readonly Panel    _vaultOutPanel  = new() { Visible = false };
    private readonly ListView _historyView    = new();
    private readonly Button   _clearHistBtn   = new() { Text = "Clear History" };
    private readonly Label    _statusLabel    = new();

    private List<HistoryEntry> _history = [];

    public MainForm()
    {
        BuildUI();
        LoadHistory();
    }

    // ── UI Construction ──────────────────────────────────────────────────────

    private void BuildUI()
    {
        Text            = "BaumKeyGenerator";
        Size            = new Size(900, 720);
        MinimumSize     = new Size(760, 620);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        AppTheme.ApplyToForm(this);

        // Root: 3 rows — title bar, top card (controls + output), history
        var root = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 3,
            ColumnCount = 1,
            BackColor   = AppTheme.Background,
            Padding     = new Padding(10),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // title
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 300));  // controls + output
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // history
        Controls.Add(root);

        root.Controls.Add(BuildTitleRow(),    0, 0);
        root.Controls.Add(BuildMiddleRow(),   0, 1);
        root.Controls.Add(BuildHistoryCard(), 0, 2);
    }

    // ── Title row ─────────────────────────────────────────────────────────────

    private static Label BuildTitleRow()
    {
        return new Label
        {
            Text      = "BaumKeyGenerator",
            Dock      = DockStyle.Fill,
            Font      = AppTheme.FontTitle,
            ForeColor = AppTheme.TextPrimary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(4, 0, 0, 0),
        };
    }

    // ── Middle row: left controls + right output side by side ─────────────────

    private TableLayoutPanel BuildMiddleRow()
    {
        var mid = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 1,
            ColumnCount = 2,
            BackColor   = AppTheme.Surface,
            Padding     = new Padding(16, 12, 16, 12),
            Margin      = new Padding(0),
        };
        mid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290));
        mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        mid.Controls.Add(BuildLeftGrid(),  0, 0);
        mid.Controls.Add(BuildRightGrid(), 1, 0);
        return mid;
    }

    // ── Left grid: key type, purpose, vault panel, generate, status ───────────

    private TableLayoutPanel BuildLeftGrid()
    {
        // Row heights (px): label, combo, gap, label, textbox, gap, vault, gap, btn, status, fill
        var grid = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 11,
            BackColor   = Color.Transparent,
            Padding     = new Padding(0, 0, 14, 0),
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));  // 0 key type label
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));  // 1 key type combo
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));   // 2 gap
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));  // 3 purpose label
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));  // 4 purpose textbox
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));   // 5 gap
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));  // 6 vault panel (hidden)
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));   // 7 gap
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));  // 8 generate button
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));  // 9 status label
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // 10 fill

        // Key Type label
        var typeLabel = MakeLabel("Key Type");
        grid.Controls.Add(typeLabel, 0, 0);

        // Key Type combo
        foreach (KeyType kt in Enum.GetValues<KeyType>())
            _typeCombo.Items.Add(KeyGenerator.DisplayName(kt));
        _typeCombo.SelectedIndex            = 0;
        _typeCombo.DropDownStyle            = ComboBoxStyle.DropDownList;
        _typeCombo.Dock                     = DockStyle.Fill;
        _typeCombo.Margin                   = new Padding(0);
        AppTheme.Style(_typeCombo);
        _typeCombo.SelectedIndexChanged    += OnTypeChanged;
        grid.Controls.Add(_typeCombo, 0, 1);

        // Purpose label
        grid.Controls.Add(MakeLabel("Purpose / Label"), 0, 3);

        // Purpose textbox
        _purposeBox.Dock            = DockStyle.Fill;
        _purposeBox.Margin          = new Padding(0);
        _purposeBox.PlaceholderText = "e.g. immich SECRET_KEY, vaultwarden…";
        AppTheme.Style(_purposeBox);
        _purposeBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; GenerateKey(); }
        };
        grid.Controls.Add(_purposeBox, 0, 4);

        // Vaultwarden panel
        BuildVaultInputPanel();
        _vaultPanel.Dock   = DockStyle.Fill;
        _vaultPanel.Margin = new Padding(0);
        grid.Controls.Add(_vaultPanel, 0, 6);

        // Generate button
        _generateBtn.Dock   = DockStyle.Fill;
        _generateBtn.Margin = new Padding(0);
        AppTheme.StylePrimary(_generateBtn);
        _generateBtn.Click += (_, _) => GenerateKey();
        grid.Controls.Add(_generateBtn, 0, 8);

        // Status label
        _statusLabel.Dock      = DockStyle.Fill;
        _statusLabel.Margin    = new Padding(0, 4, 0, 0);
        _statusLabel.Font      = AppTheme.FontSmall;
        _statusLabel.ForeColor = AppTheme.TextSecondary;
        _statusLabel.BackColor = Color.Transparent;
        _statusLabel.Text      = "Ready.";
        grid.Controls.Add(_statusLabel, 0, 9);

        return grid;
    }

    private void BuildVaultInputPanel()
    {
        _vaultPanel.BackColor = Color.Transparent;
        _vaultPanel.Visible   = false;

        // Inner grid: label, textbox, gap, checkbox
        var inner = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 4,
            BackColor   = Color.Transparent,
            Padding     = new Padding(0),
            Margin      = new Padding(0),
        };
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));  // label
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // textbox
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));   // gap
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));  // checkbox

        var passLabel = new Label
        {
            Text      = "Password (leave blank to auto-generate)",
            Dock      = DockStyle.Fill,
            Margin    = new Padding(0),
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.BottomLeft,
        };
        inner.Controls.Add(passLabel, 0, 0);

        _vaultPassBox.Dock          = DockStyle.Fill;
        _vaultPassBox.Margin        = new Padding(0);
        _vaultPassBox.PasswordChar  = '●';
        _vaultPassBox.PlaceholderText = "Auto-generated if blank";
        AppTheme.Style(_vaultPassBox);
        inner.Controls.Add(_vaultPassBox, 0, 1);

        _vaultAutoCheck.Text      = "Auto-generate password";
        _vaultAutoCheck.Checked   = true;
        _vaultAutoCheck.Dock      = DockStyle.Fill;
        _vaultAutoCheck.Margin    = new Padding(0);
        _vaultAutoCheck.Font      = AppTheme.FontSmall;
        _vaultAutoCheck.ForeColor = AppTheme.TextSecondary;
        _vaultAutoCheck.BackColor = Color.Transparent;
        _vaultAutoCheck.CheckedChanged += (_, _) =>
        {
            _vaultPassBox.Enabled = !_vaultAutoCheck.Checked;
            _vaultPassBox.Clear();
        };
        inner.Controls.Add(_vaultAutoCheck, 0, 3);

        _vaultPanel.Controls.Add(inner);
    }

    // ── Right grid: output label, output box, copy, vault password section ────

    private TableLayoutPanel BuildRightGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 5,
            BackColor   = Color.Transparent,
            Padding     = new Padding(0),
            Margin      = new Padding(0),
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));   // 0 header row (label + Copied!)
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // 1 output textbox (fills)
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // 2 copy button
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));    // 3 gap
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));   // 4 vault password out

        // Header: label + "Copied!" badge side by side
        var hdr = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            Margin      = new Padding(0),
            ColumnCount = 2,
            RowCount    = 1,
            BackColor   = Color.Transparent,
        };
        hdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hdr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

        hdr.Controls.Add(MakeLabel("Generated Key"), 0, 0);

        _copiedLabel.Dock      = DockStyle.Fill;
        _copiedLabel.Margin    = new Padding(0);
        _copiedLabel.ForeColor = AppTheme.Success;
        _copiedLabel.Font      = AppTheme.FontSmall;
        _copiedLabel.TextAlign = ContentAlignment.MiddleRight;
        _copiedLabel.BackColor = Color.Transparent;
        hdr.Controls.Add(_copiedLabel, 1, 0);

        grid.Controls.Add(hdr, 0, 0);

        // Output textbox
        _outputBox.Multiline    = true;
        _outputBox.ReadOnly     = true;
        _outputBox.ScrollBars   = ScrollBars.Vertical;
        _outputBox.Dock         = DockStyle.Fill;
        _outputBox.Margin       = new Padding(0);
        _outputBox.PlaceholderText = "Key will appear here…";
        AppTheme.Style(_outputBox, mono: true);
        grid.Controls.Add(_outputBox, 0, 1);

        // Copy button
        _copyOutputBtn.Dock   = DockStyle.Fill;
        _copyOutputBtn.Margin = new Padding(0, 4, 0, 0);
        AppTheme.StyleSecondary(_copyOutputBtn);
        _copyOutputBtn.Click += (_, _) => CopyToClipboard(_outputBox.Text, _copiedLabel);
        grid.Controls.Add(_copyOutputBtn, 0, 2);

        // Vault password output panel
        _vaultOutPanel.Dock      = DockStyle.Fill;
        _vaultOutPanel.Margin    = new Padding(0);
        _vaultOutPanel.BackColor = Color.Transparent;
        BuildVaultOutputPanel(_vaultOutPanel);
        grid.Controls.Add(_vaultOutPanel, 0, 4);

        return grid;
    }

    private void BuildVaultOutputPanel(Panel container)
    {
        var inner = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 3,
            BackColor   = Color.Transparent,
            Padding     = new Padding(0),
            Margin      = new Padding(0),
        };
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));  // label
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));  // textbox
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));  // copy button

        var lbl = new Label
        {
            Text      = "Admin Password (use this to log into Vaultwarden)",
            Dock      = DockStyle.Fill,
            Margin    = new Padding(0),
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.BottomLeft,
        };
        inner.Controls.Add(lbl, 0, 0);

        _vaultPassOut.ReadOnly = true;
        _vaultPassOut.Dock     = DockStyle.Fill;
        _vaultPassOut.Margin   = new Padding(0);
        AppTheme.Style(_vaultPassOut, mono: true);
        inner.Controls.Add(_vaultPassOut, 0, 1);

        _copyPassBtn.Dock   = DockStyle.Fill;
        _copyPassBtn.Margin = new Padding(0, 4, 0, 0);
        AppTheme.StyleSecondary(_copyPassBtn);
        _copyPassBtn.Click += (_, _) => CopyToClipboard(_vaultPassOut.Text, _copiedLabel);
        inner.Controls.Add(_copyPassBtn, 0, 2);

        container.Controls.Add(inner);
    }

    // ── History card ─────────────────────────────────────────────────────────

    private Panel BuildHistoryCard()
    {
        var card = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding   = new Padding(16, 10, 16, 10),
            Margin    = new Padding(0, 8, 0, 0),
        };

        // Header
        var hdr = new TableLayoutPanel
        {
            Dock        = DockStyle.Top,
            Height      = 30,
            ColumnCount = 2,
            RowCount    = 1,
            BackColor   = Color.Transparent,
            Margin      = new Padding(0),
            Padding     = new Padding(0),
        };
        hdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hdr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));

        var hdrLabel = new Label
        {
            Text      = "Generation History  (encrypted, current user only)",
            Dock      = DockStyle.Fill,
            Margin    = new Padding(0),
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        hdr.Controls.Add(hdrLabel, 0, 0);

        _clearHistBtn.Dock   = DockStyle.Fill;
        _clearHistBtn.Margin = new Padding(0);
        AppTheme.StyleDanger(_clearHistBtn);
        _clearHistBtn.Click += OnClearHistory;
        hdr.Controls.Add(_clearHistBtn, 1, 0);

        var spacer = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Color.Transparent };

        card.Controls.Add(hdr);
        card.Controls.Add(spacer);

        // ListView
        _historyView.Dock         = DockStyle.Fill;
        _historyView.View         = View.Details;
        _historyView.FullRowSelect = true;
        _historyView.GridLines    = false;
        _historyView.BackColor    = AppTheme.SurfaceAlt;
        _historyView.ForeColor    = AppTheme.TextPrimary;
        _historyView.BorderStyle  = BorderStyle.None;
        _historyView.Font         = AppTheme.FontSmall;
        _historyView.HeaderStyle  = ColumnHeaderStyle.Nonclickable;
        _historyView.MultiSelect  = false;
        _historyView.Columns.Add("Date / Time", 138);
        _historyView.Columns.Add("Type",        160);
        _historyView.Columns.Add("Purpose",     200);
        _historyView.Columns.Add("Value",        -2);
        _historyView.DoubleClick += OnHistoryDoubleClick;

        card.Controls.Add(_historyView);
        return card;
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

        FlashStatus("Generating…", color: AppTheme.TextSecondary);
        Application.DoEvents();

        string value;
        _vaultOutPanel.Visible = false;
        _vaultPassOut.Clear();

        try
        {
            switch (type)
            {
                case KeyType.GeneralHex32:
                    value = KeyGenerator.GeneralHex32(); break;
                case KeyType.GeneralBase64:
                    value = KeyGenerator.GeneralBase64(); break;
                case KeyType.JwtSecret:
                    value = KeyGenerator.JwtSecret(); break;
                case KeyType.DatabasePassword:
                    value = KeyGenerator.DatabasePassword(); break;
                case KeyType.AlphanumericKey:
                    value = KeyGenerator.Alphanumeric(); break;
                case KeyType.VaultwardenToken:
                    string? provided = _vaultAutoCheck.Checked ? null : _vaultPassBox.Text.Trim();
                    if (!_vaultAutoCheck.Checked && string.IsNullOrEmpty(provided))
                    {
                        FlashStatus("Enter a password or check auto-generate.", error: true);
                        return;
                    }
                    var (pass, phc) = KeyGenerator.VaultwardenToken(provided);
                    _vaultPassOut.Text     = pass;
                    _vaultOutPanel.Visible = true;
                    value = phc;
                    break;
                default:
                    value = KeyGenerator.GeneralHex32(); break;
            }
        }
        catch (Exception ex)
        {
            FlashStatus($"Error: {ex.Message}", error: true);
            return;
        }

        _outputBox.Text = value;

        var entry = new HistoryEntry(DateTime.Now, KeyGenerator.DisplayName(type), purpose, value);
        _history.Insert(0, entry);
        HistoryStore.Append(entry);
        RefreshHistoryView();
        FlashStatus($"Generated {KeyGenerator.DisplayName(type)} for '{purpose}'.");
    }

    private void OnTypeChanged(object? sender, EventArgs e)
    {
        var type = (KeyType)_typeCombo.SelectedIndex;
        _vaultPanel.Visible    = type == KeyType.VaultwardenToken;
        _vaultOutPanel.Visible = false;
        _outputBox.Clear();
        _vaultPassOut.Clear();
    }

    private void OnHistoryDoubleClick(object? sender, EventArgs e)
    {
        if (_historyView.SelectedItems.Count == 0) return;
        int idx = _historyView.SelectedItems[0].Index;
        if (idx < 0 || idx >= _history.Count) return;
        CopyToClipboard(_history[idx].Value, _copiedLabel);
        FlashStatus($"Copied value for '{_history[idx].Purpose}' to clipboard.");
    }

    private void OnClearHistory(object? sender, EventArgs e)
    {
        if (MessageBox.Show("Clear all history? This cannot be undone.", "Clear History",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
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
            bool isVault     = e.KeyType.Contains("Vaultwarden");
            int  truncLen    = isVault ? 40 : 32;
            string display   = e.Value.Length > truncLen
                ? e.Value[..truncLen] + "…"
                : e.Value;

            var item = new ListViewItem(e.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            item.SubItems.Add(e.KeyType);
            item.SubItems.Add(e.Purpose);
            item.SubItems.Add(display);
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

    private void FlashStatus(string msg, bool error = false, Color? color = null)
    {
        _statusLabel.Text      = msg;
        _statusLabel.ForeColor = error ? AppTheme.Danger : (color ?? AppTheme.Success);
        if (error || color.HasValue) return;
        var t = new System.Windows.Forms.Timer { Interval = 4000 };
        t.Tick += (_, _) =>
        {
            _statusLabel.Text      = "Ready.";
            _statusLabel.ForeColor = AppTheme.TextSecondary;
            t.Stop(); t.Dispose();
        };
        t.Start();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Label MakeLabel(string text) => new()
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        Margin    = new Padding(0),
        Font      = AppTheme.FontLabel,
        ForeColor = AppTheme.TextPrimary,
        BackColor = Color.Transparent,
        TextAlign = ContentAlignment.BottomLeft,
    };
}

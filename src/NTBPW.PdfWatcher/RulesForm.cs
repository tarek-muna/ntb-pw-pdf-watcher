namespace NTBPW.PdfWatcher;

// Bearbeitung der Ablageregeln. Die Regeln werden von oben nach unten geprueft,
// die erste passende Regel verschiebt bzw. kopiert die PDF.
internal sealed class RulesForm : Form
{
    private readonly List<FilingRule> _rules;
    private readonly Icon _appIcon;
    private readonly ListBox _ruleList = new();
    private readonly TextBox _name = new();
    private readonly CheckBox _enabled = new() { Text = "Regel aktiv", AutoSize = true };
    private readonly TextBox _contains = new() { PlaceholderText = "z. B. Rechnung;Invoice – leer = alle PDFs" };
    private readonly TextBox _destination = new();
    private readonly CheckBox _copy = new() { Text = "Kopieren statt verschieben (Original bleibt im Scanordner)", AutoSize = true };
    private readonly Panel _editor = new();
    private bool _loading;
    private int _currentIndex = -1;

    public RulesForm(List<FilingRule> rules, Icon appIcon)
    {
        _rules = rules;
        _appIcon = (Icon)appIcon.Clone();

        Text = "Ablageregeln";
        Icon = _appIcon;
        Width = 820;
        Height = 520;
        MinimumSize = new Size(760, 470);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 9F);

        BuildUi();
        RefreshRuleList();
        FormClosed += (_, _) => _appIcon.Dispose();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 2,
            BackColor = BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        Controls.Add(root);

        root.Controls.Add(BuildListPanel(), 0, 0);
        root.Controls.Add(BuildEditorPanel(), 1, 0);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0)
        };
        var save = CreateButton("Übernehmen", true, 112);
        var cancel = CreateButton("Abbrechen", false, 100);
        save.Click += (_, _) => SaveAndClose();
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        footer.Controls.Add(save);
        footer.Controls.Add(cancel);
        root.SetColumnSpan(footer, 2);
        root.Controls.Add(footer, 0, 1);
    }

    private Control BuildListPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0, 0, 12, 0), Padding = new Padding(12) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        panel.Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            Text = "Regeln (Reihenfolge)",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 45, 61),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        _ruleList.Dock = DockStyle.Fill;
        _ruleList.BorderStyle = BorderStyle.None;
        _ruleList.Font = new Font("Segoe UI", 9.5F);
        _ruleList.SelectedIndexChanged += (_, _) => LoadSelectedRule();
        layout.Controls.Add(_ruleList, 0, 1);

        layout.Controls.Add(ButtonRow(("+ Hinzufügen", AddRule), ("– Entfernen", RemoveRule)), 0, 2);
        layout.Controls.Add(ButtonRow(("▲ Nach oben", () => MoveRule(-1)), ("▼ Nach unten", () => MoveRule(1))), 0, 3);
        return panel;
    }

    private static TableLayoutPanel ButtonRow(params (string Text, Action Action)[] buttons)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = buttons.Length };
        for (var i = 0; i < buttons.Length; i++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / buttons.Length));
            var button = CreateButton(buttons[i].Text, false, 0);
            button.Dock = DockStyle.Fill;
            var action = buttons[i].Action;
            button.Click += (_, _) => action();
            row.Controls.Add(button, i, 0);
        }
        return row;
    }

    private Control BuildEditorPanel()
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(12, 0, 0, 0), Padding = new Padding(18) };

        _editor.Dock = DockStyle.Fill;
        var form = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        _editor.Controls.Add(form);
        outer.Controls.Add(_editor);

        var title = new Label
        {
            Text = "Regel bearbeiten",
            AutoSize = true,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 45, 61),
            Margin = new Padding(0, 0, 0, 16)
        };
        form.Controls.Add(title, 0, 0);
        form.SetColumnSpan(title, 3);

        AddField(form, "Name", _name, 1);
        AddField(form, "Dateiname enthält", _contains, 2);
        AddField(form, "Zielordner", _destination, 3);
        var browse = CreateButton("…", false, 34);
        browse.Click += (_, _) => BrowseFolder();
        form.Controls.Add(browse, 2, 3);

        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
        options.Controls.Add(_enabled);
        options.Controls.Add(_copy);
        form.Controls.Add(new Label { Text = "Optionen", AutoSize = true, ForeColor = Color.FromArgb(75, 88, 104), Margin = new Padding(0, 10, 8, 0) }, 0, 4);
        form.Controls.Add(options, 1, 4);
        form.SetColumnSpan(options, 2);

        var hint = new Label
        {
            Text = "Die Regeln werden von oben nach unten geprüft; die erste passende Regel wird angewendet. " +
                   "Mehrere Suchbegriffe mit Semikolon trennen. Groß-/Kleinschreibung spielt keine Rolle. " +
                   "Existiert die Datei im Zielordner schon, wird eine Nummer angehängt.",
            AutoSize = true,
            MaximumSize = new Size(470, 0),
            ForeColor = Color.FromArgb(112, 126, 144),
            Padding = new Padding(0, 14, 0, 0)
        };
        form.Controls.Add(hint, 0, 5);
        form.SetColumnSpan(hint, 3);

        return outer;
    }

    private static void AddField(TableLayoutPanel form, string labelText, Control control, int row)
    {
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        form.Controls.Add(new Label { Text = labelText, AutoSize = true, ForeColor = Color.FromArgb(75, 88, 104), Margin = new Padding(0, 9, 8, 0) }, 0, row);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 5, 6, 5);
        form.Controls.Add(control, 1, row);
    }

    private static Button CreateButton(string text, bool primary, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(36, 111, 187) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(45, 58, 74),
            Margin = new Padding(6, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = primary ? button.BackColor : Color.FromArgb(200, 208, 218);
        return button;
    }

    private void RefreshRuleList(int select = -1)
    {
        _loading = true;
        _ruleList.Items.Clear();
        foreach (var rule in _rules)
            _ruleList.Items.Add($"{(rule.Enabled ? "●" : "○")}  {rule.Name}");
        _loading = false;

        _currentIndex = -1;
        _editor.Enabled = _rules.Count > 0;
        if (_rules.Count == 0)
        {
            ClearEditor();
            return;
        }
        _ruleList.SelectedIndex = Math.Clamp(select < 0 ? 0 : select, 0, _rules.Count - 1);
    }

    private void ClearEditor()
    {
        _loading = true;
        _name.Text = _contains.Text = _destination.Text = "";
        _enabled.Checked = _copy.Checked = false;
        _loading = false;
    }

    private void LoadSelectedRule()
    {
        if (_loading) return;
        SaveCurrentRule();
        _currentIndex = _ruleList.SelectedIndex;
        if (_currentIndex < 0 || _currentIndex >= _rules.Count) return;

        _loading = true;
        var rule = _rules[_currentIndex];
        _name.Text = rule.Name;
        _enabled.Checked = rule.Enabled;
        _contains.Text = rule.FileNameContains;
        _destination.Text = rule.DestinationFolder;
        _copy.Checked = rule.CopyInsteadOfMove;
        _loading = false;
    }

    private void SaveCurrentRule()
    {
        if (_loading || _currentIndex < 0 || _currentIndex >= _rules.Count) return;
        var rule = _rules[_currentIndex];
        rule.Name = string.IsNullOrWhiteSpace(_name.Text) ? $"Regel {_currentIndex + 1}" : _name.Text.Trim();
        rule.Enabled = _enabled.Checked;
        rule.FileNameContains = _contains.Text.Trim();
        rule.DestinationFolder = _destination.Text.Trim();
        rule.CopyInsteadOfMove = _copy.Checked;
    }

    private void AddRule()
    {
        SaveCurrentRule();
        _rules.Add(new FilingRule { Name = $"Regel {_rules.Count + 1}" });
        RefreshRuleList(_rules.Count - 1);
        _name.Focus();
    }

    private void RemoveRule()
    {
        var index = _ruleList.SelectedIndex;
        if (index < 0 || index >= _rules.Count) return;
        if (MessageBox.Show(this, $"Regel '{_rules[index].Name}' wirklich entfernen?", "Ablageregeln", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        _currentIndex = -1;
        _rules.RemoveAt(index);
        RefreshRuleList(index);
    }

    private void MoveRule(int offset)
    {
        var index = _ruleList.SelectedIndex;
        var target = index + offset;
        if (index < 0 || target < 0 || target >= _rules.Count) return;
        SaveCurrentRule();
        (_rules[index], _rules[target]) = (_rules[target], _rules[index]);
        RefreshRuleList(target);
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Zielordner auswählen",
            ShowNewFolderButton = true,
            SelectedPath = Directory.Exists(_destination.Text) ? _destination.Text : string.Empty
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _destination.Text = dialog.SelectedPath;
    }

    private void SaveAndClose()
    {
        SaveCurrentRule();
        var invalid = _rules.FindIndex(r => r.Enabled && string.IsNullOrWhiteSpace(r.DestinationFolder));
        if (invalid >= 0)
        {
            MessageBox.Show(this, $"Die aktive Regel '{_rules[invalid].Name}' hat keinen Zielordner.", "Ablageregeln", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshRuleList(invalid);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}

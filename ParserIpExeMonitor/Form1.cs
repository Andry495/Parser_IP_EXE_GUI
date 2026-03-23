using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace ParserIpExeMonitor;

public partial class Form1 : Form
{
    private readonly TextBox _processSearchText = new();
    private readonly ComboBox _processCombo = new();
    private readonly Button _refreshProcessesButton = new();
    private readonly Button _startDumpButton = new();
    private readonly Button _stopDumpButton = new();
    private readonly Button _browseExeButton = new();
    private readonly Button _runProcessButton = new();
    private readonly TextBox _exePathText = new();
    private readonly TextBox _exeArgsText = new();
    private readonly DataGridView _connectionsGrid = new();
    private readonly Label _connectionCountLabel = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly SaveFileDialog _saveFileDialog = new();
    private readonly HashSet<string> _dumpedIps = new(StringComparer.OrdinalIgnoreCase);
    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _statusMain = new();
    private readonly ToolStripStatusLabel _statusDump = new();

    private List<ProcessItem> _processCache = [];
    private StreamWriter? _dumpWriter;
    private Process? _launchedProcess;
    private int? _selectedPid;
    private string _selectedProcessName = string.Empty;

    public Form1()
    {
        InitializeComponent();
        BuildUi();
        WireEvents();
        RefreshProcessList();
        StartRefreshLoop();
    }

    private void BuildUi()
    {
        Text = "Parser IP — монитор соединений";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1024, 680);
        ClientSize = new Size(1180, 720);
        BackColor = AppTheme.BgDeep;
        Font = AppTheme.UiFont();
        ForeColor = AppTheme.TextPrimary;
        DoubleBuffered = true;

        var header = BuildHeader();
        header.Dock = DockStyle.Top;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16, 8, 16, 8),
            BackColor = AppTheme.BgDeep
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 210f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var topRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
        topRow.Padding = new Padding(0, 0, 0, 12);

        var cardProcess = BuildProcessCard();
        cardProcess.Dock = DockStyle.Fill;
        var cardRun = BuildRunProcessCard();
        cardRun.Dock = DockStyle.Fill;
        topRow.Controls.Add(cardProcess, 0, 0);
        topRow.Controls.Add(cardRun, 1, 0);

        var connectionsCard = BuildConnectionsCard();
        connectionsCard.Dock = DockStyle.Fill;

        mainLayout.Controls.Add(topRow, 0, 0);
        mainLayout.Controls.Add(connectionsCard, 0, 1);

        _statusStrip.Dock = DockStyle.Bottom;
        _statusStrip.SizingGrip = false;
        _statusStrip.BackColor = AppTheme.BgSurface;
        _statusStrip.ForeColor = AppTheme.TextMuted;
        _statusStrip.Padding = new Padding(8, 4, 8, 4);
        _statusStrip.RenderMode = ToolStripRenderMode.System;
        _statusMain.Spring = true;
        _statusMain.TextAlign = ContentAlignment.MiddleLeft;
        _statusMain.Text = "Выберите процесс или запустите .exe";
        _statusDump.Text = "";
        _statusDump.AutoSize = true;
        _statusStrip.Items.Add(_statusMain);
        _statusStrip.Items.Add(new ToolStripSeparator());
        _statusStrip.Items.Add(_statusDump);

        Controls.Add(mainLayout);
        Controls.Add(_statusStrip);
        Controls.Add(header);

        StyleDataGridView(_connectionsGrid);
        ApplyGridDoubleBuffer();

        _saveFileDialog.Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*";
        _saveFileDialog.FileName = $"dump_{DateTime.Now:yyyyMMdd_HHmmss}.log";
    }

    private Panel BuildHeader()
    {
        var p = new Panel
        {
            Height = 64,
            BackColor = AppTheme.BgSurface,
            Padding = new Padding(20, 14, 20, 12)
        };
        var title = new Label
        {
            Text = "Parser IP / GUI",
            Font = AppTheme.TitleFont(),
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(20, 12)
        };
        var subtitle = new Label
        {
            Text = "Сокеты выбранного процесса (TCP и UDP, IPv4/IPv6) · дамп · запуск .exe с автологом",
            Font = AppTheme.CaptionFont(),
            ForeColor = AppTheme.TextMuted,
            AutoSize = true,
            Location = new Point(20, 36)
        };
        p.Controls.Add(title);
        p.Controls.Add(subtitle);
        p.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderSubtle, 1f);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        };
        return p;
    }

    private CardPanel BuildProcessCard()
    {
        var card = new CardPanel();
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));

        var title = SectionTitle("Процесс");
        layout.Controls.Add(title, 0, 0);

        _processSearchText.PlaceholderText = "Поиск по имени…";
        StyleTextBox(_processSearchText);
        layout.Controls.Add(_processSearchText, 0, 1);

        StyleCombo(_processCombo);
        _processCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        layout.Controls.Add(_processCombo, 0, 2);

        var btnRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        StyleButtonSecondary(_refreshProcessesButton, "Обновить список", 132);
        StyleButtonPrimary(_startDumpButton, "Начать дамп", 120);
        StyleButtonDanger(_stopDumpButton, "Стоп", 88);
        _stopDumpButton.Enabled = false;
        btnRow.Controls.Add(_refreshProcessesButton);
        btnRow.Controls.Add(_startDumpButton);
        btnRow.Controls.Add(_stopDumpButton);
        layout.Controls.Add(btnRow, 0, 3);

        card.Controls.Add(layout);
        return card;
    }

    private CardPanel BuildRunProcessCard()
    {
        var card = new CardPanel();
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));

        var title = SectionTitle("Запуск .exe");
        layout.SetColumnSpan(title, 2);
        layout.Controls.Add(title, 0, 0);

        layout.Controls.Add(MutedLabel("Файл:"), 0, 1);

        StyleTextBox(_exePathText);
        StyleButtonSecondary(_browseExeButton, "…", 40);
        var pathRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48f));
        pathRow.Controls.Add(_exePathText, 0, 0);
        pathRow.Controls.Add(_browseExeButton, 1, 0);
        layout.Controls.Add(pathRow, 1, 1);

        layout.Controls.Add(MutedLabel("Аргументы:"), 0, 2);
        StyleTextBox(_exeArgsText);
        layout.Controls.Add(_exeArgsText, 1, 2);

        var runWrap = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 0)
        };
        StyleButtonPrimary(_runProcessButton, "Запустить и дампить", 200);
        runWrap.Controls.Add(_runProcessButton);
        layout.SetColumnSpan(runWrap, 2);
        layout.Controls.Add(runWrap, 0, 3);

        card.Controls.Add(layout);
        return card;
    }

    private CardPanel BuildConnectionsCard()
    {
        var card = new CardPanel();
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var head = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
        var connTitle = SectionTitle("Сокеты процесса (TCP + UDP, IPv4/IPv6)");
        head.Controls.Add(connTitle, 0, 0);
        _connectionCountLabel.Text = "0 соединений";
        _connectionCountLabel.Font = AppTheme.CaptionFont();
        _connectionCountLabel.ForeColor = AppTheme.TextMuted;
        _connectionCountLabel.Anchor = AnchorStyles.Right;
        _connectionCountLabel.TextAlign = ContentAlignment.MiddleRight;
        _connectionCountLabel.Dock = DockStyle.Fill;
        head.Controls.Add(_connectionCountLabel, 1, 0);

        outer.Controls.Add(head, 0, 0);
        _connectionsGrid.Dock = DockStyle.Fill;
        _connectionsGrid.Margin = new Padding(0, 8, 0, 0);
        outer.Controls.Add(_connectionsGrid, 0, 1);

        card.Controls.Add(outer);

        _connectionsGrid.ReadOnly = true;
        _connectionsGrid.AllowUserToAddRows = false;
        _connectionsGrid.AllowUserToDeleteRows = false;
        _connectionsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _connectionsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _connectionsGrid.RowHeadersVisible = false;
        _connectionsGrid.BorderStyle = BorderStyle.None;
        _connectionsGrid.Columns.Add("Proto", "Протокол");
        _connectionsGrid.Columns.Add("Local", "Локальный адрес");
        _connectionsGrid.Columns.Add("Remote", "Удалённый адрес");
        _connectionsGrid.Columns.Add("State", "Состояние");

        return card;
    }

    private static Label SectionTitle(string text) =>
        new()
        {
            Text = text,
            Font = AppTheme.UiFont(10, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Margin = new Padding(0, 0, 0, 4)
        };

    private static Label MutedLabel(string text) =>
        new()
        {
            Text = text,
            ForeColor = AppTheme.TextMuted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

    private static void StyleButtonPrimary(Button b, string text, int width)
    {
        b.Text = text;
        b.Width = width;
        b.Height = 34;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = AppTheme.Accent;
        b.ForeColor = Color.White;
        b.Cursor = Cursors.Hand;
        b.Font = AppTheme.UiFont(9.5f);
        b.Margin = new Padding(0, 0, 8, 0);
        b.MouseEnter += (_, _) => b.BackColor = AppTheme.AccentHover;
        b.MouseLeave += (_, _) => b.BackColor = AppTheme.Accent;
    }

    private static void StyleButtonSecondary(Button b, string text, int width)
    {
        b.Text = text;
        b.Width = width;
        b.Height = 34;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = AppTheme.BorderSubtle;
        b.BackColor = AppTheme.BgSurface;
        b.ForeColor = AppTheme.TextPrimary;
        b.Cursor = Cursors.Hand;
        b.Font = AppTheme.UiFont(9.5f);
        b.Margin = new Padding(0, 0, 8, 0);
        b.MouseEnter += (_, _) => b.BackColor = AppTheme.BgElevated;
        b.MouseLeave += (_, _) => b.BackColor = AppTheme.BgSurface;
    }

    private static void StyleButtonDanger(Button b, string text, int width)
    {
        b.Text = text;
        b.Width = width;
        b.Height = 34;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = AppTheme.AccentDanger;
        b.ForeColor = Color.White;
        b.Cursor = Cursors.Hand;
        b.Font = AppTheme.UiFont(9.5f);
        b.Margin = new Padding(0, 0, 8, 0);
        b.MouseEnter += (_, _) => b.BackColor = AppTheme.AccentDangerHover;
        b.MouseLeave += (_, _) => b.BackColor = AppTheme.AccentDanger;
    }

    private static void StyleTextBox(TextBox t)
    {
        t.BorderStyle = BorderStyle.FixedSingle;
        t.BackColor = AppTheme.BgSurface;
        t.ForeColor = AppTheme.TextPrimary;
        t.Dock = DockStyle.Fill;
        t.Margin = new Padding(0, 2, 0, 4);
    }

    private static void StyleCombo(ComboBox c)
    {
        c.FlatStyle = FlatStyle.Flat;
        c.BackColor = AppTheme.BgSurface;
        c.ForeColor = AppTheme.TextPrimary;
        c.Margin = new Padding(0, 2, 0, 4);
        c.Dock = DockStyle.Fill;
    }

    private static void StyleDataGridView(DataGridView g)
    {
        g.BackgroundColor = AppTheme.BgSurface;
        g.GridColor = AppTheme.BorderSubtle;
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        g.ColumnHeadersHeight = 36;
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.BgSurface,
            ForeColor = AppTheme.TextPrimary,
            SelectionBackColor = AppTheme.Accent,
            SelectionForeColor = Color.White,
            Font = AppTheme.UiFont(9.5f),
            Padding = new Padding(8, 4, 8, 4)
        };
        g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.BgElevated,
            ForeColor = AppTheme.TextPrimary,
            SelectionBackColor = AppTheme.Accent,
            SelectionForeColor = Color.White
        };
        g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.BgDeep,
            ForeColor = AppTheme.TextMuted,
            Font = AppTheme.UiFont(9f, FontStyle.Bold),
            Padding = new Padding(10, 8, 10, 8)
        };
        g.RowTemplate.Height = 30;
    }

    private void ApplyGridDoubleBuffer()
    {
        var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
        prop?.SetValue(_connectionsGrid, true, null);
    }

    private void WireEvents()
    {
        _refreshProcessesButton.Click += (_, _) => RefreshProcessList();
        _processSearchText.TextChanged += (_, _) => ApplyProcessFilter();
        _processCombo.SelectedIndexChanged += (_, _) => SelectProcessFromList();
        _startDumpButton.Click += (_, _) => StartDumpInteractive();
        _stopDumpButton.Click += (_, _) => StopDump();
        _browseExeButton.Click += (_, _) => BrowseExe();
        _runProcessButton.Click += (_, _) => RunProcessAndStartDump();
        FormClosing += (_, _) => StopDump();
    }

    private void StartRefreshLoop()
    {
        _refreshTimer.Interval = 2000;
        _refreshTimer.Tick += (_, _) => RefreshConnectionsView();
        _refreshTimer.Start();
    }

    private void RefreshProcessList()
    {
        var current = _processCombo.SelectedItem as ProcessItem;
        _processCache = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .Select(p => new ProcessItem(p.Id, p.ProcessName))
            .ToList();

        ApplyProcessFilter(current);
    }

    private void ApplyProcessFilter(ProcessItem? preferredSelection = null)
    {
        var current = preferredSelection ?? (_processCombo.SelectedItem as ProcessItem);
        var q = _processSearchText.Text.Trim();
        var filtered = string.IsNullOrEmpty(q)
            ? _processCache
            : _processCache.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

        _processCombo.BeginUpdate();
        _processCombo.Items.Clear();
        foreach (var item in filtered)
        {
            _processCombo.Items.Add(item);
        }

        if (current is not null)
        {
            var index = filtered.FindIndex(p => p.Pid == current.Pid);
            _processCombo.SelectedIndex = index >= 0 ? index : (filtered.Count > 0 ? 0 : -1);
        }
        else if (_processCombo.Items.Count > 0)
        {
            _processCombo.SelectedIndex = 0;
        }

        _processCombo.EndUpdate();
    }

    private void SelectProcessFromList()
    {
        if (_processCombo.SelectedItem is not ProcessItem item)
        {
            _selectedPid = null;
            _selectedProcessName = string.Empty;
            _statusMain.Text = "Процесс не выбран";
            _connectionsGrid.Rows.Clear();
            _connectionCountLabel.Text = "0 записей";
            return;
        }

        _selectedPid = item.Pid;
        _selectedProcessName = item.Name;
        _statusMain.Text = $"Выбрано: {_selectedProcessName} (PID {_selectedPid})";
        RefreshConnectionsView();
    }

    private void RefreshConnectionsView()
    {
        if (_selectedPid is null)
        {
            return;
        }

        var list = NetTableReader.GetAll()
            .Where(c => c.ProcessId == _selectedPid)
            .OrderBy(c => c.Protocol)
            .ThenBy(c => c.RemoteDisplay)
            .ThenBy(c => c.LocalEndpoint)
            .ToList();

        _connectionsGrid.Rows.Clear();
        foreach (var connection in list)
        {
            _connectionsGrid.Rows.Add(
                connection.Protocol,
                connection.LocalEndpoint,
                connection.RemoteDisplay,
                connection.State);
        }

        var n = list.Count;
        _connectionCountLabel.Text = n == 0 ? "Нет сокетов" : $"Записей: {n}";

        if (_dumpWriter is not null)
        {
            WriteDumpSnapshot(list);
        }
    }

    private void StartDumpInteractive()
    {
        if (_selectedPid is null)
        {
            MessageBox.Show("Сначала выберите процесс.", "Parser IP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_saveFileDialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        StartDump(_saveFileDialog.FileName);
    }

    private void StartDump(string filePath)
    {
        StopDump();

        _dumpWriter = new StreamWriter(filePath, append: true, Encoding.UTF8);
        _dumpedIps.Clear();
        _statusDump.Text = $"Файл: {Path.GetFileName(filePath)}";
        _statusMain.Text = $"Дамп: {_selectedProcessName} (PID {_selectedPid})";
        _startDumpButton.Enabled = false;
        _stopDumpButton.Enabled = true;

        _dumpWriter.WriteLine($"=== Dump started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        _dumpWriter.WriteLine($"Process: {_selectedProcessName} (PID {_selectedPid})");
        _dumpWriter.Flush();
    }

    private void StopDump()
    {
        if (_dumpWriter is null)
        {
            return;
        }

        _dumpWriter.WriteLine($"=== Dump stopped {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        _dumpWriter.Flush();
        _dumpWriter.Dispose();
        _dumpWriter = null;
        _startDumpButton.Enabled = true;
        _stopDumpButton.Enabled = false;
        _statusDump.Text = "";
        _statusMain.Text = _selectedPid is null
            ? "Выберите процесс или запустите .exe"
            : $"Выбрано: {_selectedProcessName} (PID {_selectedPid})";
    }

    private void WriteDumpSnapshot(List<NetConnectionInfo> list)
    {
        if (_dumpWriter is null || _selectedPid is null)
        {
            return;
        }

        _dumpWriter.WriteLine($"--- Snapshot {DateTime.Now:yyyy-MM-dd HH:mm:ss} | rows: {list.Count} ---");
        foreach (var connection in list)
        {
            var key = connection.DumpRowKey();
            var isNew = _dumpedIps.Add(key);
            _dumpWriter.WriteLine(
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\tPID={_selectedPid}\tProto={connection.Protocol}\tLocal={connection.LocalEndpoint}\tRemote={connection.RemoteDisplay}\tState={connection.State}\tNew={(isNew ? 1 : 0)}");
            if (isNew)
            {
                var extra = connection.UniqueExtraLine();
                if (extra is not null)
                {
                    _dumpWriter.WriteLine(extra);
                }
            }
        }

        _dumpWriter.Flush();
    }

    private void BrowseExe()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _exePathText.Text = dialog.FileName;
        }
    }

    private void RunProcessAndStartDump()
    {
        var exePath = _exePathText.Text.Trim();
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            MessageBox.Show("Укажите существующий .exe файл.", "Parser IP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = _exeArgsText.Text.Trim(),
            UseShellExecute = true
        };

        try
        {
            _launchedProcess = Process.Start(startInfo);
            if (_launchedProcess is null)
            {
                MessageBox.Show("Не удалось запустить процесс.", "Parser IP", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка запуска: {ex.Message}", "Parser IP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _selectedPid = _launchedProcess.Id;
        _selectedProcessName = Path.GetFileName(exePath);
        _statusMain.Text = $"Запущено: {_selectedProcessName} (PID {_selectedPid})";

        RefreshProcessList();
        SelectProcessInComboByPid(_launchedProcess.Id);

        if (_saveFileDialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        StartDump(_saveFileDialog.FileName);
    }

    private void SelectProcessInComboByPid(int pid)
    {
        for (var i = 0; i < _processCombo.Items.Count; i++)
        {
            if (_processCombo.Items[i] is ProcessItem item && item.Pid == pid)
            {
                _processCombo.SelectedIndex = i;
                return;
            }
        }
    }

    private sealed record ProcessItem(int Pid, string Name)
    {
        public override string ToString() => $"{Name} ({Pid})";
    }
}

using SMM.Core;
using SMM.Data.Repositories;
using SMM.Desktop.Workspace;

namespace SMM.Desktop;

/// <summary>
/// Shell form: layout, data binding, and user prompts only. Business rules live in <see cref="IStudentWorkspace"/>,
/// <see cref="ISocietyHeadWorkspace"/>, and <see cref="IAdminWorkspace"/>.
/// </summary>
public class MainForm : Form
{
    private readonly AuthUser _user;
    private readonly IStudentWorkspace _student;
    private readonly ISocietyHeadWorkspace _head;
    private readonly IAdminWorkspace? _admin;

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly TabPage _tabStudent = new("Student");
    private readonly TabPage _tabHead = new("Society leader");
    private readonly TabPage _tabAdmin = new("Administration");

    private DataGridView _dgvBrowseSocieties = null!;
    private DataGridView _dgvMembership = null!;
    private DataGridView _dgvEvents = null!;
    private DataGridView _dgvTickets = null!;

    private ComboBox _cmbHeadSociety = null!;
    private TextBox _txtSocName = null!;
    private TextBox _txtSocDesc = null!;
    private DataGridView _dgvRequests = null!;
    private DataGridView _dgvMembers = null!;
    private DataGridView _dgvSocEvents = null!;
    private DataGridView _dgvSocTasks = null!;
    private TextBox _txtNewSocName = null!;
    private TextBox _txtEvTitle = null!;
    private TextBox _txtEvVenue = null!;
    private DateTimePicker _dtEvStart = null!;
    private NumericUpDown _numEvCap = null!;
    private TextBox _txtTaskTitle = null!;
    private ComboBox _cmbTaskAssignee = null!;

    private DataGridView _dgvAdminUsers = null!;
    private DataGridView _dgvAdminSocieties = null!;
    private DataGridView _dgvPendingEvents = null!;
    private DataGridView _dgvActivity = null!;

    public MainForm(AuthUser user)
    {
        _user = user;

        var societyRepo = new SocietyRepository();
        var eventRepo = new EventRepository();
        var taskRepo = new TaskRepository();
        var activityRepo = new ActivityRepository();
        var userRepo = new UserRepository();

        _student = new StudentWorkspace(user.UserId, societyRepo, eventRepo, activityRepo);
        _head = new SocietyHeadWorkspace(user.UserId, societyRepo, eventRepo, taskRepo, activityRepo);
        _admin = user.Role == UserRole.Admin
            ? new AdminWorkspace(user.UserId, userRepo, societyRepo, eventRepo, activityRepo)
            : null;

        Text = $"Societies — {_user.FullName}";
        Width = 1100;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        var bar = new ToolStrip();
        var logout = new ToolStripButton("Sign out");
        logout.Click += (_, _) => Close();
        bar.Items.Add(logout);
        bar.Dock = DockStyle.Top;

        BuildStudentTab();
        BuildHeadTab();
        BuildAdminTab();

        _tabs.TabPages.Add(_tabStudent);
        if (_tabHead.Controls.Count > 0)
            _tabs.TabPages.Add(_tabHead);
        if (_admin is not null)
            _tabs.TabPages.Add(_tabAdmin);

        Controls.Add(_tabs);
        Controls.Add(bar);

        Load += (_, _) => RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshStudent();
        RefreshHead();
        if (_admin is not null)
            RefreshAdmin();
    }

    private void BuildStudentTab()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        for (var i = 0; i < 4; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        _dgvBrowseSocieties = MkGrid();
        var p1 = PanelWithLabel("Browse approved societies", _dgvBrowseSocieties);
        var btnApply = new Button { Text = "Apply to selected society", AutoSize = true };
        btnApply.Click += (_, _) => RunSafe(() =>
        {
            var id = GridSelectionHelper.GetSelectedId(_dgvBrowseSocieties, "SocietyId");
            if (id is null) return;
            _student.ApplyToSociety(id.Value);
            MessageBox.Show("Application submitted.");
            RefreshStudent();
        });

        var row1 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
        row1.Controls.Add(btnApply);
        row1.Controls.Add(p1);

        _dgvMembership = MkGrid();
        var p2 = PanelWithLabel("My membership status", _dgvMembership);

        _dgvEvents = MkGrid();
        var btnReg = new Button { Text = "Register for selected event", AutoSize = true };
        btnReg.Click += (_, _) => RunSafe(() =>
        {
            var id = GridSelectionHelper.GetSelectedId(_dgvEvents, "EventId");
            if (id is null) return;
            var code = _student.RegisterForEvent(id.Value);
            MessageBox.Show($"Registered. Ticket code: {code}");
            RefreshStudent();
        });
        var row3 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
        row3.Controls.Add(btnReg);
        row3.Controls.Add(PanelWithLabel("Upcoming published events", _dgvEvents));

        _dgvTickets = MkGrid();
        var p4 = PanelWithLabel("My tickets / passes", _dgvTickets);

        layout.Controls.Add(row1, 0, 0);
        layout.Controls.Add(p2, 0, 1);
        layout.Controls.Add(row3, 0, 2);
        layout.Controls.Add(p4, 0, 3);
        _tabStudent.Controls.Add(layout);
    }

    private void RefreshStudent()
    {
        var d = _student.LoadDashboard();
        _dgvBrowseSocieties.DataSource = d.ApprovedSocieties;
        _dgvMembership.DataSource = d.MembershipOverview;
        _dgvEvents.DataSource = d.PublishedEvents;
        _dgvTickets.DataSource = d.Tickets;
    }

    private void BuildHeadTab()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5 };
        for (var i = 0; i < 5; i++)
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

        _cmbHeadSociety = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbHeadSociety.SelectedIndexChanged += (_, _) => RefreshHeadDetail();

        _txtSocName = new TextBox { Dock = DockStyle.Top };
        _txtSocDesc = new TextBox { Dock = DockStyle.Top, Height = 60, Multiline = true };
        var btnSave = new Button { Text = "Save society profile", Dock = DockStyle.Top };
        btnSave.Click += (_, _) => RunSafe(() =>
        {
            var sid = CurrentSocietyId();
            if (sid is null) return;
            _head.SaveProfile(sid.Value, _txtSocName.Text, _txtSocDesc.Text);
            MessageBox.Show("Saved.");
            RefreshHead();
        });

        _txtNewSocName = new TextBox { Dock = DockStyle.Top, PlaceholderText = "New society name" };
        var btnCreateSoc = new Button { Text = "Request new society", Dock = DockStyle.Top };
        btnCreateSoc.Click += (_, _) => RunSafe(() =>
        {
            if (string.IsNullOrWhiteSpace(_txtNewSocName.Text))
            {
                MessageBox.Show("Enter a society name.");
                return;
            }

            _head.RequestNewSociety(_txtNewSocName.Text);
            MessageBox.Show("Society submitted. Awaiting admin approval.");
            _txtNewSocName.Clear();
            RefreshHead();
            RefreshStudent();
        });

        var grpProfile = new GroupBox { Text = "My society (as head)", Dock = DockStyle.Fill };
        var fp = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        fp.Controls.Add(new Label { Text = "Select society", AutoSize = true });
        fp.Controls.Add(_cmbHeadSociety);
        fp.Controls.Add(new Label { Text = "Name", AutoSize = true });
        fp.Controls.Add(_txtSocName);
        fp.Controls.Add(new Label { Text = "Description", AutoSize = true });
        fp.Controls.Add(_txtSocDesc);
        fp.Controls.Add(btnSave);
        fp.Controls.Add(new Label { Text = "Start another society", AutoSize = true });
        fp.Controls.Add(_txtNewSocName);
        fp.Controls.Add(btnCreateSoc);
        grpProfile.Controls.Add(fp);
        left.Controls.Add(grpProfile, 0, 0);
        left.SetRowSpan(grpProfile, 2);

        _dgvRequests = MkGrid();
        var btnAp = new Button { Text = "Approve request", AutoSize = true };
        btnAp.Click += (_, _) => RunHeadRequest(true);
        var btnRej = new Button { Text = "Reject request", AutoSize = true };
        btnRej.Click += (_, _) => RunHeadRequest(false);
        var reqBar = new FlowLayoutPanel { AutoSize = true };
        reqBar.Controls.AddRange(new Control[] { btnAp, btnRej });
        var reqPanel = new Panel { Dock = DockStyle.Fill };
        reqBar.Dock = DockStyle.Top;
        _dgvRequests.Dock = DockStyle.Fill;
        reqPanel.Controls.Add(reqBar);
        reqPanel.Controls.Add(_dgvRequests);
        left.Controls.Add(PanelWithLabel("Pending membership requests", reqPanel), 0, 2);

        _dgvMembers = MkGrid();
        var btnRemove = new Button { Text = "Deactivate selected member", AutoSize = true };
        btnRemove.Click += (_, _) => RunSafe(() =>
        {
            var sid = CurrentSocietyId();
            var uid = GridSelectionHelper.GetSelectedId(_dgvMembers, "UserId");
            if (sid is null || uid is null) return;
            _head.DeactivateMember(sid.Value, uid.Value);
            RefreshHeadDetail();
        });
        var mb = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        mb.Controls.Add(btnRemove);
        var mp = new Panel { Dock = DockStyle.Fill };
        mb.Dock = DockStyle.Top;
        _dgvMembers.Dock = DockStyle.Fill;
        mp.Controls.Add(mb);
        mp.Controls.Add(_dgvMembers);
        left.Controls.Add(PanelWithLabel("Members", mp), 0, 3);
        left.SetRowSpan(left.Controls[^1], 2);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 35));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        _dgvSocEvents = MkGrid();
        _txtEvTitle = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Title" };
        _txtEvVenue = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Venue" };
        _dtEvStart = new DateTimePicker { Dock = DockStyle.Top, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm", ShowUpDown = true };
        _numEvCap = new NumericUpDown { Dock = DockStyle.Top, Minimum = 0, Maximum = 100000, Value = 0 };
        var lblCap = new Label { Text = "Capacity (0 = unlimited)", AutoSize = true };
        var btnAddEv = new Button { Text = "Create event (draft, awaits admin)", Dock = DockStyle.Top };
        btnAddEv.Click += (_, _) => RunSafe(() =>
        {
            var sid = CurrentSocietyId();
            if (sid is null) return;
            int? cap = _numEvCap.Value <= 0 ? null : (int)_numEvCap.Value;
            _head.CreateDraftEvent(sid.Value, _txtEvTitle.Text, _txtEvVenue.Text, _dtEvStart.Value, cap);
            MessageBox.Show("Event created (pending admin approval).");
            RefreshHeadDetail();
        });
        var btnPub = new Button { Text = "Publish selected (must be admin-approved)", Dock = DockStyle.Top };
        btnPub.Click += (_, _) => RunSafe(() =>
        {
            var sid = CurrentSocietyId();
            var eid = GridSelectionHelper.GetSelectedId(_dgvSocEvents, "EventId");
            if (sid is null || eid is null) return;
            _head.PublishEvent(sid.Value, eid.Value);
            RefreshHeadDetail();
            RefreshStudent();
        });
        var btnCancelEv = new Button { Text = "Cancel selected event", Dock = DockStyle.Top };
        btnCancelEv.Click += (_, _) => RunSafe(() =>
        {
            var sid = CurrentSocietyId();
            var eid = GridSelectionHelper.GetSelectedId(_dgvSocEvents, "EventId");
            if (sid is null || eid is null) return;
            _head.CancelEvent(sid.Value, eid.Value);
            RefreshHeadDetail();
            RefreshStudent();
        });
        var evForm = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, AutoSize = true };
        evForm.Controls.AddRange(new Control[] { _txtEvTitle, _txtEvVenue, _dtEvStart, lblCap, _numEvCap, btnAddEv, btnPub, btnCancelEv });
        var evPanel = new Panel { Dock = DockStyle.Fill };
        var evTop = new Panel { Dock = DockStyle.Top, Height = 220 };
        evTop.Controls.Add(evForm);
        _dgvSocEvents.Dock = DockStyle.Fill;
        evPanel.Controls.Add(evTop);
        evPanel.Controls.Add(_dgvSocEvents);
        right.Controls.Add(PanelWithLabel("Events", evPanel), 0, 0);

        _dgvSocTasks = MkGrid();
        _txtTaskTitle = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Task title" };
        _cmbTaskAssignee = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
        var btnAddTask = new Button { Text = "Assign task", Dock = DockStyle.Top };
        btnAddTask.Click += (_, _) => RunSafe(() =>
        {
            var sid = CurrentSocietyId();
            if (sid is null || _cmbTaskAssignee.SelectedValue is not int assignee) return;
            _head.AssignTask(sid.Value, assignee, _txtTaskTitle.Text);
            _txtTaskTitle.Clear();
            RefreshHeadDetail();
        });
        var tf = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, AutoSize = true };
        tf.Controls.AddRange(new Control[] { _txtTaskTitle, new Label { Text = "Assign to", AutoSize = true }, _cmbTaskAssignee, btnAddTask });
        var tp = new Panel { Dock = DockStyle.Fill };
        var tt = new Panel { Dock = DockStyle.Top, Height = 120 };
        tt.Controls.Add(tf);
        _dgvSocTasks.Dock = DockStyle.Fill;
        tp.Controls.Add(tt);
        tp.Controls.Add(_dgvSocTasks);
        right.Controls.Add(PanelWithLabel("Tasks", tp), 0, 1);

        var btnRep = new Button { Text = "Society report (counts)", Dock = DockStyle.Top, Height = 40 };
        btnRep.Click += (_, _) =>
        {
            var sid = CurrentSocietyId();
            if (sid is null) return;
            var r = _head.SocietyReport(sid.Value);
            MessageBox.Show($"Members (active): {r.Members}\nEvents (total): {r.Events}\nPending requests: {r.PendingRequests}");
        };
        right.Controls.Add(btnRep, 0, 2);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);
        _tabHead.Controls.Add(root);
    }

    private void RunHeadRequest(bool approve)
    {
        RunSafe(() =>
        {
            var id = GridSelectionHelper.GetSelectedId(_dgvRequests, "RequestId");
            if (id is null) return;
            _head.ApproveOrRejectRequest(id.Value, approve);
            RefreshHeadDetail();
            RefreshStudent();
        });
    }

    private void RefreshHead()
    {
        var list = _head.ListMySocieties().ToList();
        _cmbHeadSociety.DataSource = null;
        _cmbHeadSociety.DisplayMember = "Name";
        _cmbHeadSociety.ValueMember = "SocietyId";
        _cmbHeadSociety.DataSource = list;
        if (list.Count > 0 && _cmbHeadSociety.SelectedIndex < 0)
            _cmbHeadSociety.SelectedIndex = 0;
        RefreshHeadDetail();
    }

    private int? CurrentSocietyId() =>
        _cmbHeadSociety.SelectedValue is int id ? id : null;

    private void RefreshHeadDetail()
    {
        var sid = CurrentSocietyId();
        if (sid is null)
        {
            _dgvRequests.DataSource = null;
            _dgvMembers.DataSource = null;
            _dgvSocEvents.DataSource = null;
            _dgvSocTasks.DataSource = null;
            _cmbTaskAssignee.DataSource = null;
            return;
        }

        var d = _head.LoadDetail(sid.Value);
        _txtSocName.Text = d.Summary?.Name ?? "";
        _txtSocDesc.Text = d.Summary?.Description ?? "";
        _dgvRequests.DataSource = d.PendingRequests;
        _dgvMembers.DataSource = d.Members;
        _dgvSocEvents.DataSource = d.Events;
        _dgvSocTasks.DataSource = d.Tasks;
        _cmbTaskAssignee.DataSource = d.TaskAssigneeOptions;
        _cmbTaskAssignee.DisplayMember = "FullName";
        _cmbTaskAssignee.ValueMember = "UserId";
    }

    private void BuildAdminTab()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        _dgvAdminUsers = MkGrid();
        var bu1 = new Button { Text = "Suspend user", AutoSize = true };
        bu1.Click += (_, _) => RunAdminUser(false);
        var bu2 = new Button { Text = "Activate user", AutoSize = true };
        bu2.Click += (_, _) => RunAdminUser(true);
        var uflow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        uflow.Controls.AddRange(new Control[] { bu1, bu2 });
        var up = new Panel { Dock = DockStyle.Fill };
        _dgvAdminUsers.Dock = DockStyle.Fill;
        up.Controls.Add(_dgvAdminUsers);
        up.Controls.Add(uflow);

        _dgvAdminSocieties = MkGrid();
        var bs1 = new Button { Text = "Approve society", AutoSize = true };
        bs1.Click += (_, _) => RunAdminSociety(SocietyStatus.Approved);
        var bs2 = new Button { Text = "Suspend society", AutoSize = true };
        bs2.Click += (_, _) => RunAdminSociety(SocietyStatus.Suspended);
        var sflow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        sflow.Controls.AddRange(new Control[] { bs1, bs2 });
        var sp = new Panel { Dock = DockStyle.Fill };
        _dgvAdminSocieties.Dock = DockStyle.Fill;
        sp.Controls.Add(_dgvAdminSocieties);
        sp.Controls.Add(sflow);

        _dgvPendingEvents = MkGrid();
        var be1 = new Button { Text = "Approve event", AutoSize = true };
        be1.Click += (_, _) => RunAdminEvent(EventAdminStatus.Approved);
        var be2 = new Button { Text = "Reject event", AutoSize = true };
        be2.Click += (_, _) => RunAdminEvent(EventAdminStatus.Rejected);
        var eflow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        eflow.Controls.AddRange(new Control[] { be1, be2 });
        var ep = new Panel { Dock = DockStyle.Fill };
        _dgvPendingEvents.Dock = DockStyle.Fill;
        ep.Controls.Add(_dgvPendingEvents);
        ep.Controls.Add(eflow);

        _dgvActivity = MkGrid();
        var btnUni = new Button { Text = "University-wide report", Dock = DockStyle.Bottom, Height = 36 };
        btnUni.Click += (_, _) =>
        {
            if (_admin is null) return;
            var u = _admin.UniversityReport();
            MessageBox.Show($"Active students: {u.Students}\nApproved societies: {u.Societies}\nLive events: {u.Events}\nOpen tasks: {u.OpenTasks}");
        };
        var ap = new Panel { Dock = DockStyle.Fill };
        _dgvActivity.Dock = DockStyle.Fill;
        ap.Controls.Add(_dgvActivity);
        ap.Controls.Add(btnUni);

        layout.Controls.Add(PanelWithLabel("Users", up), 0, 0);
        layout.Controls.Add(PanelWithLabel("Societies", sp), 1, 0);
        layout.Controls.Add(PanelWithLabel("Events pending approval", ep), 0, 1);
        layout.Controls.Add(PanelWithLabel("Activity log", ap), 1, 1);
        _tabAdmin.Controls.Add(layout);
    }

    private void RunAdminUser(bool active)
    {
        if (_admin is null) return;
        RunSafe(() =>
        {
            var id = GridSelectionHelper.GetSelectedId(_dgvAdminUsers, "UserId");
            if (id is null || id == _user.UserId) return;
            _admin.SetUserActive(id.Value, active);
            RefreshAdmin();
        });
    }

    private void RunAdminSociety(SocietyStatus st)
    {
        if (_admin is null) return;
        RunSafe(() =>
        {
            var id = GridSelectionHelper.GetSelectedId(_dgvAdminSocieties, "SocietyId");
            if (id is null) return;
            _admin.SetSocietyStatus(id.Value, st);
            RefreshAdmin();
            RefreshStudent();
            RefreshHead();
        });
    }

    private void RunAdminEvent(EventAdminStatus st)
    {
        if (_admin is null) return;
        RunSafe(() =>
        {
            var id = GridSelectionHelper.GetSelectedId(_dgvPendingEvents, "EventId");
            if (id is null) return;
            _admin.SetEventAdminStatus(id.Value, st);
            RefreshAdmin();
            RefreshStudent();
            RefreshHead();
        });
    }

    private void RefreshAdmin()
    {
        if (_admin is null) return;
        var d = _admin.LoadDashboard();
        _dgvAdminUsers.DataSource = d.Users;
        _dgvAdminSocieties.DataSource = d.Societies;
        _dgvPendingEvents.DataSource = d.PendingEvents;
        _dgvActivity.DataSource = d.Activity;
    }

    private static void RunSafe(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private static DataGridView MkGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        AllowUserToAddRows = false
    };

    private static Panel PanelWithLabel(string title, Control content)
    {
        var p = new Panel { Dock = DockStyle.Fill };
        var l = new Label { Text = title, Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 4, 0, 4) };
        content.Dock = DockStyle.Fill;
        p.Controls.Add(l);
        p.Controls.Add(content);
        return p;
    }
}

using SMM.Core;
using SMM.Data.Repositories;

namespace SMM.Desktop;

public class MainForm : Form
{
    private readonly AuthUser _user;
    private readonly UserRepository _users = new();
    private readonly SocietyRepository _society = new();
    private readonly EventRepository _events = new();
    private readonly TaskRepository _tasks = new();
    private readonly ActivityRepository _activity = new();

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
        if (_user.Role == UserRole.Admin)
            _tabs.TabPages.Add(_tabAdmin);

        Controls.Add(_tabs);
        Controls.Add(bar);

        Load += (_, _) => RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshStudent();
        RefreshHead();
        if (_user.Role == UserRole.Admin)
            RefreshAdmin();
    }

    #region Student

    private void BuildStudentTab()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        _dgvBrowseSocieties = MkGrid();
        var p1 = PanelWithLabel("Browse approved societies", _dgvBrowseSocieties);
        var btnApply = new Button { Text = "Apply to selected society", AutoSize = true };
        btnApply.Click += (_, _) => StudentApply();
        var row1 = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
        row1.Controls.Add(btnApply);
        row1.Controls.Add(p1);

        _dgvMembership = MkGrid();
        var p2 = PanelWithLabel("My membership status", _dgvMembership);

        _dgvEvents = MkGrid();
        var btnReg = new Button { Text = "Register for selected event", AutoSize = true };
        btnReg.Click += (_, _) => StudentRegisterEvent();
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
        _dgvBrowseSocieties.DataSource = _society.ListApprovedSocieties().ToList();
        _dgvMembership.DataSource = _society.ListMembershipOverviewForStudent(_user.UserId).ToList();
        _dgvEvents.DataSource = _events.ListPublishedForStudents().ToList();
        _dgvTickets.DataSource = _events.ListTicketsForStudent(_user.UserId).ToList();
    }

    private void StudentApply()
    {
        var id = GetSelectedId(_dgvBrowseSocieties, "SocietyId");
        if (id is null) return;
        try
        {
            _society.CreateMembershipRequest(_user.UserId, id.Value);
            _activity.Log(_user.UserId, "MembershipApply", "Society", id, null);
            MessageBox.Show("Application submitted.");
            RefreshStudent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void StudentRegisterEvent()
    {
        var id = GetSelectedId(_dgvEvents, "EventId");
        if (id is null) return;
        try
        {
            var code = _events.RegisterStudent(id.Value, _user.UserId);
            _activity.Log(_user.UserId, "EventRegister", "Event", id, code);
            MessageBox.Show($"Registered. Ticket code: {code}");
            RefreshStudent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    #endregion

    #region Society head

    private void BuildHeadTab()
    {
        var headSocieties = _society.ListSocietiesForHead(_user.UserId);
        if (headSocieties.Count == 0 && _user.Role != UserRole.Admin)
        {
            // Still show create society for any student
        }

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5 };
        for (int i = 0; i < 5; i++)
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

        _cmbHeadSociety = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbHeadSociety.SelectedIndexChanged += (_, _) => RefreshHeadDetail();

        _txtSocName = new TextBox { Dock = DockStyle.Top };
        _txtSocDesc = new TextBox { Dock = DockStyle.Top, Height = 60, Multiline = true };
        var btnSave = new Button { Text = "Save society profile", Dock = DockStyle.Top };
        btnSave.Click += (_, _) => HeadSaveProfile();

        _txtNewSocName = new TextBox { Dock = DockStyle.Top, PlaceholderText = "New society name" };
        var btnCreateSoc = new Button { Text = "Request new society", Dock = DockStyle.Top };
        btnCreateSoc.Click += (_, _) => HeadCreateSociety();

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
        btnAp.Click += (_, _) => HeadApproveRequest(true);
        var btnRej = new Button { Text = "Reject request", AutoSize = true };
        btnRej.Click += (_, _) => HeadApproveRequest(false);
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
        btnRemove.Click += (_, _) => HeadDeactivateMember();
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
        btnAddEv.Click += (_, _) => HeadCreateEvent();
        var btnPub = new Button { Text = "Publish selected (must be admin-approved)", Dock = DockStyle.Top };
        btnPub.Click += (_, _) => HeadPublishEvent();
        var btnCancelEv = new Button { Text = "Cancel selected event", Dock = DockStyle.Top };
        btnCancelEv.Click += (_, _) => HeadCancelEvent();
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
        btnAddTask.Click += (_, _) => HeadAddTask();
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
        btnRep.Click += (_, _) => HeadReport();
        right.Controls.Add(btnRep, 0, 2);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);
        _tabHead.Controls.Add(root);
    }

    private void RefreshHead()
    {
        var list = _society.ListSocietiesForHead(_user.UserId).ToList();
        _cmbHeadSociety.DataSource = null;
        _cmbHeadSociety.DisplayMember = "Name";
        _cmbHeadSociety.ValueMember = "SocietyId";
        _cmbHeadSociety.DataSource = list;
        if (list.Count > 0 && _cmbHeadSociety.SelectedIndex < 0)
            _cmbHeadSociety.SelectedIndex = 0;
        RefreshHeadDetail();
    }

    private int? CurrentSocietyId()
    {
        if (_cmbHeadSociety.SelectedValue is int id)
            return id;
        return null;
    }

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

        var soc = _society.ListSocietiesForHead(_user.UserId).FirstOrDefault(s => s.SocietyId == sid);
        if (soc is not null)
        {
            _txtSocName.Text = soc.Name;
            _txtSocDesc.Text = soc.Description ?? "";
        }

        _dgvRequests.DataSource = _society.ListPendingRequestsForSociety(sid.Value).ToList();
        _dgvMembers.DataSource = _society.ListMembers(sid.Value).ToList();
        _dgvSocEvents.DataSource = _events.ListForSociety(sid.Value).ToList();
        _dgvSocTasks.DataSource = _tasks.ListForSociety(sid.Value).ToList();

        var mems = _society.ListMembers(sid.Value).Where(m => m.Active && !m.IsHead).ToList();
        _cmbTaskAssignee.DataSource = mems;
        _cmbTaskAssignee.DisplayMember = "FullName";
        _cmbTaskAssignee.ValueMember = "UserId";
    }

    private void HeadSaveProfile()
    {
        var sid = CurrentSocietyId();
        if (sid is null) return;
        try
        {
            _society.UpdateSocietyProfile(sid.Value, _user.UserId, _txtSocName.Text, _txtSocDesc.Text);
            _activity.Log(_user.UserId, "SocietyUpdate", "Society", sid, null);
            MessageBox.Show("Saved.");
            RefreshHead();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void HeadCreateSociety()
    {
        if (string.IsNullOrWhiteSpace(_txtNewSocName.Text))
        {
            MessageBox.Show("Enter a society name.");
            return;
        }

        try
        {
            var id = _society.CreateSociety(_user.UserId, _txtNewSocName.Text, null);
            _activity.Log(_user.UserId, "SocietyCreate", "Society", id, _txtNewSocName.Text);
            MessageBox.Show("Society submitted. Awaiting admin approval.");
            _txtNewSocName.Clear();
            RefreshHead();
            RefreshStudent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void HeadApproveRequest(bool approve)
    {
        var id = GetSelectedId(_dgvRequests, "RequestId");
        if (id is null) return;
        try
        {
            if (approve)
                _society.ApproveMembershipRequest(id.Value, _user.UserId);
            else
                _society.RejectMembershipRequest(id.Value, _user.UserId);
            _activity.Log(_user.UserId, approve ? "MembershipApprove" : "MembershipReject", "MembershipRequest", id, null);
            RefreshHeadDetail();
            RefreshStudent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void HeadDeactivateMember()
    {
        var sid = CurrentSocietyId();
        var uid = GetSelectedId(_dgvMembers, "UserId");
        if (sid is null || uid is null) return;
        try
        {
            _society.SetMemberActive(sid.Value, _user.UserId, uid.Value, false);
            _activity.Log(_user.UserId, "MemberDeactivate", "Society", sid, uid.ToString());
            RefreshHeadDetail();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void HeadCreateEvent()
    {
        var sid = CurrentSocietyId();
        if (sid is null) return;
        try
        {
            int? cap = _numEvCap.Value <= 0 ? null : (int)_numEvCap.Value;
            var id = _events.CreateEvent(sid.Value, _user.UserId, _txtEvTitle.Text, null, _txtEvVenue.Text, _dtEvStart.Value, null, cap);
            _activity.Log(_user.UserId, "EventCreate", "Event", id, _txtEvTitle.Text);
            MessageBox.Show("Event created (pending admin approval).");
            RefreshHeadDetail();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void HeadPublishEvent()
    {
        var sid = CurrentSocietyId();
        var eid = GetSelectedId(_dgvSocEvents, "EventId");
        if (sid is null || eid is null) return;
        try
        {
            var row = _events.ListForSociety(sid.Value).First(ev => ev.EventId == eid);
            if (row.AdminStatus != EventAdminStatus.Approved)
            {
                MessageBox.Show("Admin must approve the event first.");
                return;
            }

            _events.UpdateEvent(eid.Value, sid.Value, _user.UserId, row.Title, row.Description, row.Venue, row.StartsAt, row.EndsAt, row.Capacity, EventLifecycleStatus.Published);
            _activity.Log(_user.UserId, "EventPublish", "Event", eid, null);
            RefreshHeadDetail();
            RefreshStudent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void HeadCancelEvent()
    {
        var sid = CurrentSocietyId();
        var eid = GetSelectedId(_dgvSocEvents, "EventId");
        if (sid is null || eid is null) return;
        try
        {
            var row = _events.ListForSociety(sid.Value).First(ev => ev.EventId == eid);
            _events.UpdateEvent(eid.Value, sid.Value, _user.UserId, row.Title, row.Description, row.Venue, row.StartsAt, row.EndsAt, row.Capacity, EventLifecycleStatus.Cancelled);
            _activity.Log(_user.UserId, "EventCancel", "Event", eid, null);
            RefreshHeadDetail();
            RefreshStudent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void HeadAddTask()
    {
        var sid = CurrentSocietyId();
        if (sid is null || _cmbTaskAssignee.SelectedValue is not int assignee) return;
        try
        {
            var id = _tasks.CreateTask(sid.Value, _user.UserId, assignee, _txtTaskTitle.Text, null, null);
            _activity.Log(_user.UserId, "TaskCreate", "SocietyTask", id, null);
            _txtTaskTitle.Clear();
            RefreshHeadDetail();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void HeadReport()
    {
        var sid = CurrentSocietyId();
        if (sid is null) return;
        var r = _society.GetSocietyReport(sid.Value);
        MessageBox.Show($"Members (active): {r.Members}\nEvents (total): {r.Events}\nPending requests: {r.PendingRequests}");
    }

    #endregion

    #region Admin

    private void BuildAdminTab()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        _dgvAdminUsers = MkGrid();
        var bu1 = new Button { Text = "Suspend user", AutoSize = true };
        bu1.Click += (_, _) => AdminSetUserActive(false);
        var bu2 = new Button { Text = "Activate user", AutoSize = true };
        bu2.Click += (_, _) => AdminSetUserActive(true);
        var uflow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        uflow.Controls.AddRange(new Control[] { bu1, bu2 });
        var up = new Panel { Dock = DockStyle.Fill };
        _dgvAdminUsers.Dock = DockStyle.Fill;
        up.Controls.Add(_dgvAdminUsers);
        up.Controls.Add(uflow);

        _dgvAdminSocieties = MkGrid();
        var bs1 = new Button { Text = "Approve society", AutoSize = true };
        bs1.Click += (_, _) => AdminSocietyStatus(SocietyStatus.Approved);
        var bs2 = new Button { Text = "Suspend society", AutoSize = true };
        bs2.Click += (_, _) => AdminSocietyStatus(SocietyStatus.Suspended);
        var sflow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        sflow.Controls.AddRange(new Control[] { bs1, bs2 });
        var sp = new Panel { Dock = DockStyle.Fill };
        _dgvAdminSocieties.Dock = DockStyle.Fill;
        sp.Controls.Add(_dgvAdminSocieties);
        sp.Controls.Add(sflow);

        _dgvPendingEvents = MkGrid();
        var be1 = new Button { Text = "Approve event", AutoSize = true };
        be1.Click += (_, _) => AdminEvent(EventAdminStatus.Approved);
        var be2 = new Button { Text = "Reject event", AutoSize = true };
        be2.Click += (_, _) => AdminEvent(EventAdminStatus.Rejected);
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
            var u = _society.GetUniversityReport();
            MessageBox.Show($"Active students: {u.Students}\nApproved societies: {u.Societies}\nLive events: {u.Events}\nOpen tasks: {u.OpenTasks}");
        };
        var ap = new Panel { Dock = DockStyle.Fill };
        _dgvActivity.Dock = DockStyle.Fill;
        ap.Controls.Add(_dgvActivity);
        ap.Controls.Add(btnUni);

        var usersPanel = PanelWithLabel("Users", up);
        var socPanel = PanelWithLabel("Societies", sp);
        var evAdPanel = PanelWithLabel("Events pending approval", ep);
        var actPanel = PanelWithLabel("Activity log", ap);
        layout.Controls.Add(usersPanel, 0, 0);
        layout.Controls.Add(socPanel, 1, 0);
        layout.Controls.Add(evAdPanel, 0, 1);
        layout.Controls.Add(actPanel, 1, 1);
        _tabAdmin.Controls.Add(layout);
    }

    private void RefreshAdmin()
    {
        _dgvAdminUsers.DataSource = _users.ListUsers().ToList();
        _dgvAdminSocieties.DataSource = _society.ListAllSocietiesForAdmin().ToList();
        _dgvPendingEvents.DataSource = _events.ListPendingAdminApproval().ToList();
        _dgvActivity.DataSource = _activity.ListRecent(300).ToList();
    }

    private void AdminSetUserActive(bool active)
    {
        var id = GetSelectedId(_dgvAdminUsers, "UserId");
        if (id is null || id == _user.UserId)
            return;
        try
        {
            _users.SetUserActive(id.Value, active);
            _activity.Log(_user.UserId, active ? "UserActivate" : "UserSuspend", "User", id, null);
            RefreshAdmin();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void AdminSocietyStatus(SocietyStatus st)
    {
        var id = GetSelectedId(_dgvAdminSocieties, "SocietyId");
        if (id is null) return;
        try
        {
            _society.SetSocietyStatus(id.Value, st, _user.UserId);
            _activity.Log(_user.UserId, "SocietyStatus", "Society", id, st.ToString());
            RefreshAdmin();
            RefreshStudent();
            RefreshHead();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void AdminEvent(EventAdminStatus st)
    {
        var id = GetSelectedId(_dgvPendingEvents, "EventId");
        if (id is null) return;
        try
        {
            _events.SetAdminStatus(id.Value, st, _user.UserId);
            _activity.Log(_user.UserId, "EventAdmin", "Event", id, st.ToString());
            RefreshAdmin();
            RefreshStudent();
            RefreshHead();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    #endregion

    #region Helpers

    private static DataGridView MkGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false
        };
    }

    private static Panel PanelWithLabel(string title, Control content)
    {
        var p = new Panel { Dock = DockStyle.Fill };
        var l = new Label { Text = title, Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 4, 0, 4) };
        content.Dock = DockStyle.Fill;
        p.Controls.Add(l);
        p.Controls.Add(content);
        return p;
    }

    private static int? GetSelectedId(DataGridView dgv, string propertyName)
    {
        if (dgv.CurrentRow?.DataBoundItem is null)
        {
            MessageBox.Show("Select a row.");
            return null;
        }

        var item = dgv.CurrentRow.DataBoundItem;
        var prop = item.GetType().GetProperty(propertyName);
        if (prop?.GetValue(item) is int v)
            return v;
        MessageBox.Show("Could not read id.");
        return null;
    }

    #endregion
}

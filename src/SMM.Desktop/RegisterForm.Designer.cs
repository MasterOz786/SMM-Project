namespace SMM.Desktop;

partial class RegisterForm
{
    private TextBox _txtName = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtPassword = null!;
    private TextBox _txtStudentNo = null!;
    private TextBox _txtProgram = null!;
    private NumericUpDown _numYear = null!;
    private Label _lblMsg = null!;
    private Button _btnCreate = null!;

    private void InitializeComponent()
    {
        Text = "Student registration";
        ClientSize = new Size(440, 380);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        int y = 20;
        int dy = 56;

        Controls.Add(MkLabel("Full name", 24, y));
        _txtName = MkBox(24, y + 20, 390);
        y += dy;

        Controls.Add(MkLabel("Email", 24, y));
        _txtEmail = MkBox(24, y + 20, 390);
        y += dy;

        Controls.Add(MkLabel("Password (min 6 chars)", 24, y));
        _txtPassword = MkBox(24, y + 20, 390);
        _txtPassword.UseSystemPasswordChar = true;
        y += dy;

        Controls.Add(MkLabel("Student number (optional)", 24, y));
        _txtStudentNo = MkBox(24, y + 20, 390);
        y += dy;

        Controls.Add(MkLabel("Program (optional)", 24, y));
        _txtProgram = MkBox(24, y + 20, 390);
        y += dy;

        Controls.Add(MkLabel("Year of study (optional, 0 = blank)", 24, y));
        _numYear = new NumericUpDown { Location = new Point(24, y + 20), Width = 80, Maximum = 8, Minimum = 0 };
        Controls.Add(_numYear);
        y += dy;

        _lblMsg = new Label { Location = new Point(24, y), Size = new Size(390, 36), ForeColor = Color.Firebrick };
        Controls.Add(_lblMsg);

        _btnCreate = new Button { Text = "Create account", Location = new Point(24, y + 40), Width = 120 };
        _btnCreate.Click += OnCreate;
        Controls.Add(_btnCreate);

        var btnCancel = new Button { Text = "Cancel", Location = new Point(160, y + 40), Width = 80 };
        btnCancel.Click += OnCancel;
        Controls.Add(btnCancel);
    }

    private static Label MkLabel(string text, int x, int y) => new() { Text = text, Location = new Point(x, y), AutoSize = true };

    private static TextBox MkBox(int x, int y, int w) => new() { Location = new Point(x, y), Width = w };
}

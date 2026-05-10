namespace SMM.Desktop;

partial class LoginForm
{
    private TextBox _txtEmail = null!;
    private TextBox _txtPassword = null!;
    private Button _btnLogin = null!;
    private Button _btnRegister = null!;
    private Button _btnExit = null!;
    private Label _lblError = null!;

    private void InitializeComponent()
    {
        Text = "Societies Management — Sign in";
        ClientSize = new Size(420, 260);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var lblEmail = new Label { Text = "Email", Location = new Point(24, 28), AutoSize = true };
        _txtEmail = new TextBox { Location = new Point(24, 48), Width = 360 };

        var lblPassword = new Label { Text = "Password", Location = new Point(24, 84), AutoSize = true };
        _txtPassword = new TextBox { Location = new Point(24, 104), Width = 360, UseSystemPasswordChar = true };

        _lblError = new Label
        {
            Location = new Point(24, 136),
            Size = new Size(360, 40),
            ForeColor = Color.Firebrick,
            AutoEllipsis = true
        };

        _btnLogin = new Button { Text = "Sign in", Location = new Point(24, 180), Width = 100 };
        _btnLogin.Click += OnLoginClick;

        _btnRegister = new Button { Text = "Register (student)", Location = new Point(140, 180), Width = 140 };
        _btnRegister.Click += OnRegisterClick;

        _btnExit = new Button { Text = "Exit", Location = new Point(300, 180), Width = 84 };
        _btnExit.Click += OnExitClick;

        Controls.AddRange(new Control[] { lblEmail, _txtEmail, lblPassword, _txtPassword, _lblError, _btnLogin, _btnRegister, _btnExit });
    }
}

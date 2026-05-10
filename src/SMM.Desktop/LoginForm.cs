using SMM.Core;
using SMM.Data.Repositories;

namespace SMM.Desktop;

public partial class LoginForm : Form
{
    private readonly UserRepository _users = new();

    public LoginForm()
    {
        InitializeComponent();
        AcceptButton = _btnLogin;
        CancelButton = _btnExit;
    }

    private void OnLoginClick(object? sender, EventArgs e)
    {
        _lblError.Text = "";
        try
        {
            var user = _users.TryLogin(_txtEmail.Text, _txtPassword.Text);
            if (user is null)
            {
                _lblError.Text = "Invalid email or password.";
                return;
            }

            if (!user.IsActive)
            {
                _lblError.Text = "Account is suspended.";
                return;
            }

            var main = new MainForm(user);
            main.FormClosed += (_, _) => Close();
            Hide();
            main.Show();
        }
        catch (Exception ex)
        {
            _lblError.Text = ex.Message;
        }
    }

    private void OnRegisterClick(object? sender, EventArgs e)
    {
        using var reg = new RegisterForm();
        reg.ShowDialog(this);
    }

    private void OnExitClick(object? sender, EventArgs e) => Close();
}

using SMM.Data.Repositories;

namespace SMM.Desktop;

public partial class RegisterForm : Form
{
    private readonly UserRepository _users = new();

    public RegisterForm()
    {
        InitializeComponent();
        AcceptButton = _btnCreate;
    }

    private void OnCreate(object? sender, EventArgs e)
    {
        _lblMsg.Text = "";
        try
        {
            if (string.IsNullOrWhiteSpace(_txtPassword.Text) || _txtPassword.Text.Length < 6)
            {
                _lblMsg.Text = "Password must be at least 6 characters.";
                return;
            }

            byte? year = _numYear.Value is >= 1 and <= 8 ? (byte)_numYear.Value : null;
            _users.RegisterStudent(
                _txtName.Text,
                _txtEmail.Text,
                _txtPassword.Text,
                string.IsNullOrWhiteSpace(_txtStudentNo.Text) ? null : _txtStudentNo.Text.Trim(),
                string.IsNullOrWhiteSpace(_txtProgram.Text) ? null : _txtProgram.Text.Trim(),
                year);

            MessageBox.Show(this, "Account created. You can sign in now.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblMsg.Text = ex.Message;
        }
    }

    private void OnCancel(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}

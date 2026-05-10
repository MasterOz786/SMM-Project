using SMM.Data;

namespace SMM.Desktop;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var cs = Environment.GetEnvironmentVariable("SMM_SQL");
        if (!string.IsNullOrWhiteSpace(cs))
            Database.ConnectionString = cs;

        Application.Run(new LoginForm());
    }
}

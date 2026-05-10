namespace SMM.Desktop.Workspace;

public static class GridSelectionHelper
{
    public static int? GetSelectedId(DataGridView dgv, string propertyName)
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
}

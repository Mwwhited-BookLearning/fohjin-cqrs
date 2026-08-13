using Fohjin.DDD.ApiClient;
using System.ComponentModel;

namespace Fohjin.DDD.BankApplication.Views;

public partial class ClientSearchForm : ViewFormBase, IClientSearchFormView
{
    public ClientSearchForm()
    {
        InitializeComponent();
        tabControl1.Appearance = TabAppearance.FlatButtons;
        tabControl1.ItemSize = new Size(0, 1);
        tabControl1.SizeMode = TabSizeMode.Fixed;
        RegisterCLientEvents();

        // docs/09-client-uis.md: Program.cs now needs a real Application.Run()
        // message loop (real HTTP calls never complete synchronously the way the old in-process
        // calls sometimes did, so ClientSearchFormPresenter.Display()'s `await LoadDataAsync()`
        // always genuinely suspends - without a message loop pumping, Main() would return and
        // the whole process would exit before that continuation ever runs). This is this
        // window's half of ending that loop once the user closes it, matching the app's
        // original exit-on-close-of-the-main-window behavior from before Application.Run() existed.
        FormClosed += (_, _) => Application.Exit();
    }

    public event Action? OnCreateNewClient;
    public event Action? OnOpenSelectedClient;

    private void RegisterCLientEvents()
    {
        addANewClientToolStripMenuItem.Click += (s, e) => OnCreateNewClient?.Invoke();
        _clients.Click += (s, e) => OnOpenSelectedClient?.Invoke();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IEnumerable<ClientReport>? Clients
    {
        get { return _clients.DataSource as IEnumerable<ClientReport>;  }
        set { _clients.DataSource = value; }
    }

    public ClientReport? GetSelectedClient()
    {
        return _clients.SelectedItem as ClientReport;
    }
}
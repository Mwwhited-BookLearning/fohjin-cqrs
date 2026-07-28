using Fohjin.DDD.Reporting.Dtos;
using System.ComponentModel;

namespace Fohjin.DDD.BankApplication.Views
{
    public partial class ClientSearchForm : ViewFormBase, IClientSearchFormView
    {
        public ClientSearchForm()
        {
            InitializeComponent();
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;
            RegisterCLientEvents();
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
}
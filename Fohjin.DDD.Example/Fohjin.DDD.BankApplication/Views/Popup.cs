using System.ComponentModel;

namespace Fohjin.DDD.BankApplication.Views
{
    public partial class Popup : ViewFormBase, IPopupView
    {
        public Popup()
        {
            InitializeComponent();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Exception
        {
            set { _exception.Text = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Message
        {
            set { _message.Text = value; }
        }
    }
}

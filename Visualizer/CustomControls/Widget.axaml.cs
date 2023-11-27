using Avalonia.Controls;

namespace WpfPanAndZoom.CustomControls
{
    public partial class Widget : UserControl
    {
        public string ID { set; get; }

        public bool DisableMove { set; get; }

        public Widget()
        {
            InitializeComponent();
        }
    }
}

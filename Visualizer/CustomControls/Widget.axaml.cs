using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace WpfPanAndZoom.CustomControls
{
    public partial class Widget : UserControl
    {
        public int ID { set; get; }

        public bool DisableMove { set; get; }

        public Widget()
        {
            InitializeComponent();
        }

        private void list_PointerLeave(object sender, PointerEventArgs e)
        {
            grid.ClipToBounds = true;
            listBorder.BorderBrush = new SolidColorBrush(Color.Parse("#00000000"));
        }

        private void list_PointerEnter(object sender, PointerEventArgs e)
        {
            grid.ClipToBounds = false;
            listBorder.BorderBrush = new SolidColorBrush(Color.Parse("#ff1e90ff"));
        }
    }
}

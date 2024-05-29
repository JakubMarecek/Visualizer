using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;
using Visualizer;

namespace WpfPanAndZoom.CustomControls
{
    /// <summary>
    /// Interaktionslogik für PanAndZoomCanvas.xaml
    /// https://stackoverflow.com/questions/35165349/how-to-drag-rendertransform-with-mouse-in-wpf
    /// </summary>
    public partial class PanAndZoomCanvas : Canvas
    {
        private MatrixTransform _transform = new MatrixTransform();
        private Point _initialMousePosition;

        public Point CurrentMousePos { set; get; } = new(0, 0);

        private Color _backgroundColor = Color.Parse("#1e1e1e"); // Color.FromArgb(0xFF, 0x33, 0x33, 0x33);

        int zoom = 0;

        public bool SnapToGrid { set; get; } = false;

        public PanAndZoomCanvas()
        {
            InitializeComponent();

            PointerPressed += PanAndZoomCanvas_MouseDown;
            PointerReleased += PanAndZoomCanvas_MouseUp;
            PointerMoved += PanAndZoomCanvas_MouseMove;
            PointerWheelChanged += PanAndZoomCanvas_MouseWheel;

            Clean();
        }

        public void Clean()
        {
            _transform = new MatrixTransform();
            Children.Clear();
            zoom = 0;

            Background = new SolidColorBrush(_backgroundColor);

            ResetGridArea();
        }

        public int MinX { set; get; }
        public int MinY { set; get; }
        public int MaxX { set; get; }
        public int MaxY { set; get; }

        public void ResetGridArea()
        {
            MinX = 0;
            MinY = 0;
            MaxX = 4000; //(int)window.Screens.Primary.WorkingArea.Width;
            MaxY = 2000; //(int)System.Windows.SystemParameters.PrimaryScreenHeight;
        }

        public void RefreshChilds()
        {
            foreach (Control child in this.Children)
            {
                child.RenderTransformOrigin = new(new(0, 0), RelativeUnit.Absolute);
                child.RenderTransform = _transform;
            }
        }

        public void RefreshChild(Control child)
        {
            child.RenderTransformOrigin = new(new(0, 0), RelativeUnit.Absolute);
            child.RenderTransform = _transform;
        }

        public Point Transform(Point source)
        {
            return _transform.Matrix.Invert().Transform(source);
        }

        public Point Transform2(Point source)
        {
            var bb = Vector.Subtract(_transform.Matrix.Invert().Transform(source), _transform.Matrix.Invert().Transform(new Point(0, 0)));
            return new(bb.X, bb.Y);
        }

        public Point Transform3(Point source)
        {
            var aa = _transform.Matrix.Invert().Transform(new Point(0, 0));
            var bb = _transform.Matrix.Invert().Transform(source);

            var cc = Vector.Add(aa, new(bb.X, bb.Y));

            var dd = _transform.Matrix.Transform(new(cc.X, cc.Y));

            return new(dd.X, dd.Y);
        }

        public Point Transform4(Point source)
        {
            var bb = Vector.Subtract(_transform.Matrix.Transform(source), _transform.Matrix.Transform(new Point(0, 0)));
            return new(bb.X, bb.Y);
        }

        public Vector Transform5(IInputElement source, Point mousePos, Point mousePosSource)
        {
            var a = _transform.Matrix.Invert().Transform(mousePos);
            return Vector.Subtract(a, mousePosSource);
        }

        private float Zoomfactor = 1.1f;

        private void PanAndZoomCanvas_MouseDown(object sender, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(this).Properties;

            if (props.IsRightButtonPressed)
            {
                _initialMousePosition = _transform.Matrix.Invert().Transform(e.GetPosition(this));
                Cursor = new(StandardCursorType.SizeAll);
            }
        }

        private void PanAndZoomCanvas_MouseUp(object sender, PointerReleasedEventArgs e)
        {
            Cursor = new(StandardCursorType.Arrow);
        }

        private void PanAndZoomCanvas_MouseMove(object sender, PointerEventArgs e)
        {
            var props = e.GetCurrentPoint(this).Properties;

            CurrentMousePos = e.GetPosition(this);

            if (props.IsRightButtonPressed)
            {
                Point mousePosition = _transform.Matrix.Invert().Transform(e.GetPosition(this));
                Vector delta = Vector.Subtract(mousePosition, _initialMousePosition);
                var translate = new TranslateTransform(delta.X, delta.Y);
                _transform.Matrix = translate.Value * _transform.Matrix;

                HideOutside();
            }
        }

        private void ShowOutside()
        {
            foreach (Control child in this.Children)
            {
                child.IsVisible = true;
                child.RenderTransform = _transform;
            }
        }

        private void HideOutside()
        {
            Point wndStart = Transform3(new(0, 0));
            Point wndEnd = Transform3(new(MainWindow.MainWnd.Bounds.Width - 0, MainWindow.MainWnd.Bounds.Height - 0));

            foreach (Control child in this.Children)
            {
                if (child is ArrowLineNew @new)
                {
                    if (zoom < -60)
                        child.IsVisible = false;
                    else
                    {
                        var arrowB = @new.GetStartEnd();
                        Point ps = Transform4(arrowB.Item1);
                        Point pe = Transform4(arrowB.Item2);

                        if (
                            (ps.X > wndEnd.X) ||
                            (pe.X < wndStart.X) ||
                            (ps.Y > wndEnd.Y) ||
                            (pe.Y < wndStart.Y)
                            )
                            child.IsVisible = false;
                        else
                            child.IsVisible = true;
                    }
                }
                else
                {
                    Point childBounds = Transform4(new(child.Bounds.Width, child.Bounds.Height));

                    if (
                        (Canvas.GetLeft(child) > wndEnd.X) ||
                        (Canvas.GetLeft(child) + childBounds.X < wndStart.X) ||
                        (Canvas.GetTop(child) > wndEnd.Y) ||
                        (Canvas.GetTop(child) + childBounds.Y < wndStart.Y)
                        )
                        child.IsVisible = false;
                    else
                        child.IsVisible = true;
                }

                if (child.IsVisible)
                    child.RenderTransform = _transform;
            }
        }

        private void PanAndZoomCanvas_MouseWheel(object sender, PointerWheelEventArgs e)
        {
            if (e.Delta.Y > 0)
                zoom++;
            else
                zoom--;

            float scaleFactor = Zoomfactor;
            if (e.Delta.Y < 0)
            {
                scaleFactor = 1f / scaleFactor;
            }

            //Point mousePostion = e.GetPosition(this);
            Point mousePostion = _transform.Matrix.Invert().Transform(e.GetPosition(this));

            Matrix scaleMatrix = _transform.Matrix;
            //scaleMatrix.ScaleAt(scaleFactor, scaleFactor, mousePostion.X, mousePostion.Y);
            _transform.Matrix = MatrixHelper.ScaleAtPrepend(scaleMatrix, scaleFactor, scaleFactor, mousePostion.X, mousePostion.Y);

            foreach (Control child in this.Children)
            {
                double x = Canvas.GetLeft(child);
                double y = Canvas.GetTop(child);

                double sx = x * scaleFactor;
                double sy = y * scaleFactor;

                Canvas.SetLeft(child, sx);
                Canvas.SetTop(child, sy);

                //child.RenderTransform = _transform;
            }

            HideOutside();
        }
    }
}

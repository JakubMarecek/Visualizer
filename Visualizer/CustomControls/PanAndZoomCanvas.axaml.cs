using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Visualizer;
using System.Collections.Generic;
using System.Linq;
using System;

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

        private bool _dragging;
        private Control _selectedElement;
        private Vector _draggingDelta;
        private Rectangle _selectRect;

        private Color _lineColor = Color.FromArgb(0xFF, 0x66, 0x66, 0x66);
        private Color _backgroundColor = Color.Parse("#121212"); // Color.FromArgb(0xFF, 0x33, 0x33, 0x33);
        private List<Line> _gridLines = new List<Line>();
        private List<Line> _gridLinesSmall = new List<Line>();

        private bool _selecting;
        private List<Control> _selectionItems = new();
        private List<Vector> _selectionItemsDeltas = new();

        public List<Control> SelectedItems { get { return _selectionItems; } }

        public delegate void MovingEventHandler(int foo, double x, double y);

        public event MovingEventHandler Moving;

        public delegate void ZoomEventHandler(int zoomFactor);

        public event ZoomEventHandler Zoomed;

        public delegate void MovedEventHandler();

        public event MovedEventHandler Moved;

        public delegate void DebugEventHandler(string txt);

        public event DebugEventHandler Debug;

        private List<Control> _borderChilds = new();
        private List<Vector> _borderChildsDeltas = new();

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
            _gridLines = new();
            Children.Clear();
            _borderChilds = new();
            _borderChildsDeltas = new();
            zoom = 0;

            _selectRect = new();
            _selectRect.Fill = new SolidColorBrush(Color.Parse("#552979ff"));
            _selectRect.Stroke = new SolidColorBrush(Color.Parse("#2979ff"));
            _selectRect.StrokeThickness = 2;
            _selectRect.RadiusX = 5;
            _selectRect.RadiusY = 5;
            _selectRect.Width = 1;
            _selectRect.Height = 1;
            _selectRect.IsVisible = false;
            Children.Add(_selectRect);
            _selectRect.ZIndex = 100;
            _selectRect.RenderTransformOrigin = new(0, 0, RelativeUnit.Relative);
            _selectRect.Name = "selectionRect";
            //Canvas.SetZIndex(_selectRect, 100);
            Canvas.SetLeft(_selectRect, -10);
            Canvas.SetTop(_selectRect, -10);
            _selectionItems = new();
            _selectionItemsDeltas = new();

            Background = new SolidColorBrush(_backgroundColor);

            ResetGridArea();
            MakeGrid();
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

        public void MakeGrid()
        {
            return;
            
            foreach (var l in _gridLines)
                Children.Remove(l);

            _gridLines.Clear();
            _gridLinesSmall.Clear();

            for (int x = MinX - 100; x <= MaxX + 1000; x += 100)
            {
                Line verticalLine = new Line
                {
                    ZIndex = 1,
                    Stroke = new SolidColorBrush(_lineColor),
                    StartPoint = new(x, MinY - 100),
                    EndPoint = new(x, MaxY + 1000),
                    RenderTransformOrigin = new(new(0, 0), RelativeUnit.Absolute)
                };

                if (x % 1000 == 0)
                {
                    verticalLine.StrokeThickness = 6;
                }
                else
                {
                    verticalLine.StrokeThickness = 2;
                    _gridLinesSmall.Add(verticalLine);
                }

                Children.Add(verticalLine);
                _gridLines.Add(verticalLine);
            }

            for (int y = MinY - 100; y <= MaxY + 1000; y += 100)
            {
                Line horizontalLine = new Line
                {
                    ZIndex = 1,
                    Stroke = new SolidColorBrush(_lineColor),
                    StartPoint = new(MinX - 100, y),
                    EndPoint = new(MaxX + 1000, y),
                    RenderTransformOrigin = new(new(0, 0), RelativeUnit.Absolute)
                };

                if (y % 1000 == 0)
                {
                    horizontalLine.StrokeThickness = 6;
                }
                else
                {
                    horizontalLine.StrokeThickness = 2;
                    _gridLinesSmall.Add(horizontalLine);
                }

                Children.Add(horizontalLine);
                _gridLines.Add(horizontalLine);
            }
        }

        public void RedrawGrid()
        {
            void setGrid(int x, int y)
            {
                if (x < MinX)
                    MinX = x;

                if (x > MaxX)
                    MaxX = x;

                if (y < MinY)
                    MinY = y;

                if (y > MaxY)
                    MaxY = y;

                MinX = (int)Math.Round(MinX / 100d, 0) * 100;
                MaxX = (int)Math.Round(MaxX / 100d, 0) * 100;
                MinY = (int)Math.Round(MinY / 100d, 0) * 100;
                MaxY = (int)Math.Round(MaxY / 100d, 0) * 100;
            }

            ResetGridArea();

            foreach (var child in Children)
            {
                if (child is not ArrowLineNew && child is not Line)
                {
                    var a = Transform2(new(Canvas.GetLeft(child), Canvas.GetTop(child)));
                    setGrid((int)a.X, (int)a.Y);
                }
            }

            MakeGrid();
            RefreshChilds();
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

        /*public void ResetZoom()
        {
            for (int i = 0; i < Math.Abs(zoom); i++)
            {
                double xx = Mouse.GetPosition(this).X;
                double yy = Mouse.GetPosition(this).Y;

                aaa(zoom * -1, new(xx, yy));
            }
            zoom = 0;
        }*/

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

            Control ct = ((Control)e.Source).FindAncestorOfType<Widget>();

            if (props.IsLeftButtonPressed && this.Children.Contains(ct) /*&& (e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Shift)*/)
            {
                Cursor = new(StandardCursorType.SizeAll);

                if (this.Children.Contains(ct)) //(Control)e.Source
                {
                    if (ct is Widget widget)
                        if (widget.DisableMove)
                            return;

                    //ResetZoom();

                    _selectedElement = ct; // (Control)e.Source;
                    Point mousePosition = e.GetPosition(this);
                    double x = Canvas.GetLeft(_selectedElement);
                    double y = Canvas.GetTop(_selectedElement);
                    Point elementPosition = new Point(x, y);
                    _draggingDelta = elementPosition - mousePosition;

                    _selectionItemsDeltas = new();
                    if (_selectionItems.Any())
                    {
                        foreach (Control child in _selectionItems)
                        {
                            double xc = Canvas.GetLeft(child);
                            double yc = Canvas.GetTop(child);
                            _selectionItemsDeltas.Add(new Point(xc, yc) - elementPosition);
                        }
                    }
                }

                _dragging = true;
            }
            else if (props.IsLeftButtonPressed)
            {
                var a = Transform3(e.GetPosition(this));
                Canvas.SetLeft(_selectRect, a.X);
                Canvas.SetTop(_selectRect, a.Y);
                _selectRect.Width = 1;
                _selectRect.Height = 1;
                _draggingDelta = new Vector(a.X, a.Y);

                _selecting = true;
            }
        }

        private void PanAndZoomCanvas_MouseUp(object sender, PointerReleasedEventArgs e)
        {
            if (_dragging)
            {
                Moved();
                RedrawGrid();
            }

            _dragging = false;
            _selectedElement = null;
            _borderChilds.Clear();
            _borderChildsDeltas.Clear();
            Cursor = new(StandardCursorType.Arrow);

            if (_selecting)
                ResetSelection();
        }

        public void ResetSelection()
        {
            _selectionItems = new();
            _selectionItemsDeltas = new();

            double selX = Canvas.GetLeft(_selectRect);
            double selY = Canvas.GetTop(_selectRect);

            foreach (Control c in Children)
            {
                Widget w = null;

                if (c is Widget)
                {
                    w = c as Widget;
                    if (w.DisableMove)
                        continue;
                }
                else
                    continue;

                double widX = Canvas.GetLeft(c);
                double widY = Canvas.GetTop(c);
                var selSize = Transform4(new(_selectRect.Width, _selectRect.Height));

                if (w != null)
                {
                    w.Border.Stroke = MainWindow.brushBlue;
                    w.Border.StrokeDashArray = new() { 1, 0 };
                    w.Border.StrokeThickness = 2;
                    w.selected.IsVisible = false;
                }

                if (_selectRect.Width > 10 && _selectRect.Height > 10)
                {
                    var widSize = Transform4(new((c as Control).Bounds.Width, (c as Control).Bounds.Height));

                    if (
                        (
                            (widX > selX && widY > selY) ||
                            (widX + widSize.X > selX && widY + widSize.Y > selY)
                        ) &&
                        widX < selX + selSize.X &&
                        widY < selY + selSize.Y &&
                        !(
                            widX < selX && widX + widSize.X > selX + selSize.X &&
                            widY < selY && widY + widSize.Y > selY + selSize.Y
                        )
                    )
                    {
                        if (w != null)
                        {
                            w.Border.Stroke = new SolidColorBrush(Color.Parse("#ffa500"));
                            //w.Border.StrokeDashArray = new() { 4, 2 };
                            w.Border.StrokeThickness = 4;
                            w.selected.IsVisible = true;
                        }

                        _selectionItems.Add(c);
                    }
                }
            }

            _selectRect.Width = 1;
            _selectRect.Height = 1;
            Canvas.SetLeft(_selectRect, -10);
            Canvas.SetTop(_selectRect, -10);
            _selectRect.IsVisible = false;

            _selecting = false;
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

            if (_dragging && props.IsLeftButtonPressed /*&& (e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Shift)*/ && !_selectionItems.Any())
            {
                if (_selectedElement != null)
                {
                    double x = e.GetPosition(this).X;
                    double y = e.GetPosition(this).Y;

                    if (SnapToGrid || e.KeyModifiers == KeyModifiers.Shift)
                    {
                        var a = Transform4(new(100, 100));

                        double sx = x + _draggingDelta.X;
                        double sy = y + _draggingDelta.Y;

                        sx -= sx % a.X;
                        sy -= sy % a.Y;

                        Canvas.SetLeft(_selectedElement, sx);
                        Canvas.SetTop(_selectedElement, sy);
                    }
                    else
                    {
                        Canvas.SetLeft(_selectedElement, x + _draggingDelta.X);
                        Canvas.SetTop(_selectedElement, y + _draggingDelta.Y);
                    }

                    if (_selectedElement is Widget widget)
                    {
                        var b = Transform5(_selectedElement, e.GetPosition(this), e.GetPosition(_selectedElement));
                        Moving(widget.ID, b.X, b.Y);
                    }

                    if (_borderChilds.Any())
                    {
                        for (int i = 0; i < _borderChilds.Count; i++)
                        {
                            Point parentPosition = new Point(Canvas.GetLeft(_selectedElement), Canvas.GetTop(_selectedElement));

                            Canvas.SetLeft(_borderChilds[i], parentPosition.X + _borderChildsDeltas[i].X);
                            Canvas.SetTop(_borderChilds[i], parentPosition.Y + _borderChildsDeltas[i].Y);

                            if (_borderChilds[i] is Widget widgetC)
                            {
                                /*var a = _transform.Inverse.Transform(Mouse.GetPosition(this));

                                var aa = new Point(_borderChildsDeltas[i].X, _borderChildsDeltas[i].Y);

                                var bb = _transform.Transform(aa);

                                var c = Point.Add(a, new(bb.X, bb.Y));*/

                                var c = Transform5(_borderChilds[i], e.GetPosition(this), e.GetPosition(_borderChilds[i]));
                                Moving(widgetC.ID, c.X, c.Y);
                            }
                        }
                    }
                }
            }
            else if (_dragging && props.IsLeftButtonPressed /*&& (e.KeyModifiers == KeyModifiers.Control || e.KeyModifiers == KeyModifiers.Shift)*/ && _selectionItems.Any())
            {
                double x = e.GetPosition(this).X;
                double y = e.GetPosition(this).Y;

                ShowOutside();

                if (_selectedElement == null)
                    return;

                if (SnapToGrid || e.KeyModifiers == KeyModifiers.Shift)
                {
                    var a = Transform4(new(100, 100));

                    double sx = x + _draggingDelta.X;
                    double sy = y + _draggingDelta.Y;

                    sx -= sx % a.X;
                    sy -= sy % a.Y;

                    Canvas.SetLeft(_selectedElement, sx);
                    Canvas.SetTop(_selectedElement, sy);
                }
                else
                {
                    Canvas.SetLeft(_selectedElement, x + _draggingDelta.X);
                    Canvas.SetTop(_selectedElement, y + _draggingDelta.Y);
                }

                for (int i = 0; i < _selectionItems.Count; i++)
                {
                    if (_selectionItems[i] != _selectedElement)
                    {
                        Point parentPosition = new Point(Canvas.GetLeft(_selectedElement), Canvas.GetTop(_selectedElement));

                        Canvas.SetLeft(_selectionItems[i], parentPosition.X + _selectionItemsDeltas[i].X);
                        Canvas.SetTop(_selectionItems[i], parentPosition.Y + _selectionItemsDeltas[i].Y);
                    }

                    var b = Transform5(_selectionItems[i], e.GetPosition(this), e.GetPosition(_selectionItems[i]));

                    if (_selectionItems[i] is Widget w)
                        Moving(w.ID, b.X, b.Y);
                }
            }
            else if (!_dragging && props.IsLeftButtonPressed && _selecting)
            {
                double x = e.GetPosition(this).X;
                double y = e.GetPosition(this).Y;

                x -= Canvas.GetLeft(_selectRect);
                y -= Canvas.GetTop(_selectRect);

                var a = _transform.Matrix.Invert().Transform(new(x, y));

                var aa = Transform3(e.GetPosition(this));
                var bb = Transform2(new(_draggingDelta.X - aa.X, _draggingDelta.Y - aa.Y));

                //Debug("Left: " + _draggingDelta.X.ToString() + Environment.NewLine + "Width: " + a.X.ToString());

                if (a.X > 0 && bb.X < 0)
                {
                    _selectRect.Width = a.X;
                    Canvas.SetLeft(_selectRect, _draggingDelta.X);
                }
                else
                {
                    Canvas.SetLeft(_selectRect, aa.X);
                    _selectRect.Width = bb.X;
                }

                if (a.Y > 0 && bb.Y < 0)
                {
                    Canvas.SetTop(_selectRect, _draggingDelta.Y);
                    _selectRect.Height = a.Y;
                }
                else
                {
                    Canvas.SetTop(_selectRect, aa.Y);
                    _selectRect.Height = bb.Y;
                }

                _selectRect.IsVisible = true;
            }
        }

        private void ShowOutside()
        {
            foreach (Control child in this.Children)
            {
                if (child.Name == "selectionRect")
                    continue;

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
                    /*if (zoom < -30)
                        child.IsVisible = false;
                    else*/
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
                    if (child.Name == "selectionRect")
                        continue;

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

        /*
        int zoom = 0;

        void aaa(int delta, Point mousePostion)
        {
            float scaleFactor = Zoomfactor;
            if (delta < 0)
            {
                scaleFactor = 1f / scaleFactor;
            }

            Matrix scaleMatrix = _transform.Matrix;
            scaleMatrix.ScaleAt(scaleFactor, scaleFactor, mousePostion.X, mousePostion.Y);
            _transform.Matrix = scaleMatrix;

            foreach (UIElement child in this.Children)
            {
                double x = Canvas.GetLeft(child);
                double y = Canvas.GetTop(child);

                double sx = x * scaleFactor;
                double sy = y * scaleFactor;

                Canvas.SetLeft(child, sx);
                Canvas.SetTop(child, sy);

                child.RenderTransform = _transform;
            }
        }*/

        private void PanAndZoomCanvas_MouseWheel(object sender, PointerWheelEventArgs e)
        {
            if (e.Delta.Y > 0)
                zoom++;
            else
                zoom--;

            Zoomed(zoom);

            foreach (var b in _gridLinesSmall)
                b.IsVisible = zoom < -20 ? false : true;

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
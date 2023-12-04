using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using WpfPanAndZoom.CustomControls;

namespace Visualizer
{
    public partial class MainWindow : Window
	{
        public static MainWindow MainWnd;

        public MainWindow()
		{
			InitializeComponent();
            MainWnd = this;
            AddColors();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FilePickerOpenOptions opts = new();
            opts.AllowMultiple = false;
            opts.FileTypeFilter = new FilePickerFileType[] { new("Json") { Patterns = new[] { "*.scene.json" } } };
            opts.Title = "Select scene json";

            var d = await StorageProvider.OpenFilePickerAsync(opts);
            if (d != null && d.Count > 0)
            {
                Title = Path.GetFileName(d[0].Path.LocalPath) + " - " + d[0].Path.LocalPath;

                var text = File.ReadAllText(d[0].Path.LocalPath);
                var jsonData = (JObject)JsonConvert.DeserializeObject(text);

                Dictionary<string, Item> Items = new();

                var graph = jsonData.SelectToken("Data").SelectToken("RootChunk").SelectToken("sceneGraph").SelectToken("Data").SelectToken("graph");
                foreach (var g in graph)
                {
                    var it = g.SelectToken("Data");

                    var id = it?.SelectToken("nodeId")?.SelectToken("id");
                    Console.WriteLine("" + id);

                    if (id == null) continue;

                    List<string> a = new();

                    var outScks = it.SelectToken("outputSockets");
                    if (outScks != null)
                        foreach (var sck in outScks)
                        {
                            var dsts = sck.SelectToken("destinations");
                            foreach (var dst in dsts)
                            {
                                var destId = dst.SelectToken("nodeId").SelectToken("id");
                                Console.WriteLine("-" + destId);

                                a.Add(destId.ToString());
                            }
                        }

                    List<string> b = new();

                    var questNode = it.SelectToken("questNode");
                    if (questNode != null)
                        b.Add(questNode.SelectToken("Data").SelectToken("$type").ToString());

                    var events = it.SelectToken("events");
                    if (events != null)
                        foreach (var ev in events)
                        {
                            b.Add(ev.SelectToken("Data").SelectToken("$type").ToString());
                        }

                    Items.Add(id.ToString(), new() { Dests = a, Draw = false, Name = it.SelectToken("$type").ToString(), Params = b });
                }

                int x = 0;
                int y = 0;

                List<string> ItemsDraw = new();

                void dr(string id, Item item, int xs = 0)
                {
                    if (!item.Draw)
                    {
                        var w = new Widget();
                        w.ID = w.Header.Text = id + " - " + item.Name;
                        w.Header.Foreground = Brushes.White;
                        w.Width = 300;
                        w.HeaderRectangle.Fill = item.Name == "scnEndNode" ? Brushes.Red : Brushes.Green;
                        canvas.Children.Add(w);
                        Canvas.SetLeft(w, xs);
                        Canvas.SetTop(w, y);

                        foreach (var p in item.Params)
                            w.list.Children.Add(new TextBlock() { Text = p });

                        item.X = xs;
                        item.Y = y;
                        item.Draw = true;

                        ItemsDraw.Add(id);

                        y += 100;

                        foreach (var sub in item.Dests)
                        {
                            var p = Items[sub];

                            dr(sub, p, xs + 400);
                        }
                    }
                }

                var entryPoints = jsonData.SelectToken("Data").SelectToken("RootChunk").SelectToken("entryPoints");
                foreach (var ep in entryPoints)
                {
                    string start = ep.SelectToken("nodeId").SelectToken("id").ToString();
                    var startItem = Items[start];
                    dr(start, startItem, x);
                }

                /*foreach (var item in Items)
                {
                    dr(item.Key, item.Value);
                }*/

                int selClr = 0;
                foreach (var item in Items)
                {
                    if (item.Value.Draw)
                        foreach (var sub in item.Value.Dests)
                        {
                            var p = Items[sub];
                            if (p.Draw)
                            {
                                ArrowLineNew l = new()
                                {
                                    StrokeThickness = 2,
                                    Stroke = new SolidColorBrush(linesColors[selClr]),
                                    X1 = item.Value.X + 300,
                                    Y1 = item.Value.Y,
                                    X2 = p.X,
                                    Y2 = p.Y
                                };
                                l.MakeBezierAlt = true;
                                l.MakePoly = false;
                                canvas.Children.Add(l);

                                selClr++;
                                if (selClr >= linesColors.Count)
                                    selClr = 0;
                            }
                        }
                }

                var w = new Widget();
                w.ID = w.Header.Text = "Not included";
                w.Header.Foreground = Brushes.White;
                w.Width = 300;
                w.HeaderRectangle.Fill = Brushes.Orange;
                canvas.Children.Add(w);
                Canvas.SetLeft(w, -500);
                Canvas.SetTop(w, 0);

                foreach (var p in Items)
                    if (!ItemsDraw.Contains(p.Key))
                        w.list.Children.Add(new TextBlock() { Text = p.Key });
            }
            else
                Environment.Exit(0);
        }

        List<Color> linesColors = new();

        private void AddColors()
        {
            linesColors.Add(Color.Parse("#ff1744")); // red
            linesColors.Add(Color.Parse("#2979ff")); // blue
            linesColors.Add(Color.Parse("#00e676")); // green
            linesColors.Add(Color.Parse("#ffea00")); // yellow
            linesColors.Add(Color.Parse("#f50057")); // pink
            linesColors.Add(Color.Parse("#d500f9")); // purple
            linesColors.Add(Color.Parse("#651fff")); // deep purple
            linesColors.Add(Color.Parse("#3d5afe")); // indigo
            linesColors.Add(Color.Parse("#00b0ff")); // light blue
            linesColors.Add(Color.Parse("#00e5ff")); // cyan
            linesColors.Add(Color.Parse("#1de9b6")); // teal
            linesColors.Add(Color.Parse("#76ff03")); // light green
            linesColors.Add(Color.Parse("#c6ff00")); // lime
            linesColors.Add(Color.Parse("#ffc400")); // amber
            linesColors.Add(Color.Parse("#ff9100")); // orange
            linesColors.Add(Color.Parse("#ff3d00")); // deep orange

            linesColors.Add(Color.Parse("#f44336")); // red
            linesColors.Add(Color.Parse("#2196f3")); // blue
            linesColors.Add(Color.Parse("#4caf50")); // green
            linesColors.Add(Color.Parse("#ffeb3b")); // yellow
            linesColors.Add(Color.Parse("#e91e63")); // pink
            linesColors.Add(Color.Parse("#9c27b0")); // purple
            linesColors.Add(Color.Parse("#673ab7")); // deep purple
            linesColors.Add(Color.Parse("#3f51b5")); // indigo
            linesColors.Add(Color.Parse("#03a9f4")); // light blue
            linesColors.Add(Color.Parse("#00bcd4")); // cyan
            linesColors.Add(Color.Parse("#009688")); // teal
            linesColors.Add(Color.Parse("#8bc34a")); // light green
            linesColors.Add(Color.Parse("#cddc39")); // lime
            linesColors.Add(Color.Parse("#ffc107")); // amber
            linesColors.Add(Color.Parse("#ff9800")); // orange
            linesColors.Add(Color.Parse("#ff5722")); // deep orange
            linesColors.Add(Color.Parse("#795548")); // brown
            linesColors.Add(Color.Parse("#9e9e9e")); // grey
            linesColors.Add(Color.Parse("#607d8b")); // blue grey

            linesColors.Add(Color.Parse("#ef9a9a")); // red
            linesColors.Add(Color.Parse("#90caf9")); // blue
            linesColors.Add(Color.Parse("#a5d6a7")); // green
            linesColors.Add(Color.Parse("#fff59d")); // yellow
            linesColors.Add(Color.Parse("#f48fb1")); // pink
            linesColors.Add(Color.Parse("#ce93d8")); // purple
            linesColors.Add(Color.Parse("#b39ddb")); // deep purple
            linesColors.Add(Color.Parse("#9fa8da")); // indigo
            linesColors.Add(Color.Parse("#81d4fa")); // light blue
            linesColors.Add(Color.Parse("#80deea")); // cyan
            linesColors.Add(Color.Parse("#80cbc4")); // teal
            linesColors.Add(Color.Parse("#c5e1a5")); // light green
            linesColors.Add(Color.Parse("#e6ee9c")); // lime
            linesColors.Add(Color.Parse("#ffe082")); // amber
            linesColors.Add(Color.Parse("#ffcc80")); // orange
            linesColors.Add(Color.Parse("#ffab91")); // deep orange
            linesColors.Add(Color.Parse("#bcaaa4")); // brown
            linesColors.Add(Color.Parse("#eeeeee")); // grey
            linesColors.Add(Color.Parse("#b0bec5")); // blue grey

            linesColors.Add(Color.Parse("#b71c1c")); // red
            linesColors.Add(Color.Parse("#0d47a1")); // blue
            linesColors.Add(Color.Parse("#1b5e20")); // green
            linesColors.Add(Color.Parse("#f57f17")); // yellow
            linesColors.Add(Color.Parse("#880e4f")); // pink
            linesColors.Add(Color.Parse("#4a148c")); // purple
            linesColors.Add(Color.Parse("#311b92")); // deep purple
            linesColors.Add(Color.Parse("#1a237e")); // indigo
            linesColors.Add(Color.Parse("#01579b")); // light blue
            linesColors.Add(Color.Parse("#006064")); // cyan
            linesColors.Add(Color.Parse("#004d40")); // teal
            linesColors.Add(Color.Parse("#33691e")); // light green
            linesColors.Add(Color.Parse("#827717")); // lime
            linesColors.Add(Color.Parse("#ff6f00")); // amber
            linesColors.Add(Color.Parse("#e65100")); // orange
            linesColors.Add(Color.Parse("#bf360c")); // deep orange
            linesColors.Add(Color.Parse("#3e2723")); // brown
            linesColors.Add(Color.Parse("#212121")); // grey
            linesColors.Add(Color.Parse("#263238")); // blue grey

            var a = typeof(Colors).GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public)
                                .Select(c => (Color)c.GetValue(null, null))
                                .ToList();

            foreach (var c in a)
                if (!linesColors.Contains(c))
                    linesColors.Add(c);
        }
    }

    class Item
    {
        public int X {  get; set; }

        public int Y { get; set; }

        public bool Draw { get; set; }

        public string Name { get; set; }

        public List<string> Dests = new();

        public List<string> Params = new();
    }
}

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
			opts.FileTypeFilter = new FilePickerFileType[] { new("Json") { Patterns = new[] { "*.scene.json", "*.questphase.json" } } };
			opts.Title = "Select json";

			int boxWidth = 500;
			int space = 200;

			var d = await StorageProvider.OpenFilePickerAsync(opts);
			if (d != null && d.Count > 0)
			{
				var fileName = d[0].Path.LocalPath;
				Title = Path.GetFileName(fileName) + " - " + fileName;

				var text = File.ReadAllText(fileName);
				var jsonData = (JObject)JsonConvert.DeserializeObject(text);

				Dictionary<string, Item> Items = new();

				List<string> getNodeParams(JToken questNode)
				{
					List<string> b = new();

					if (questNode != null)
					{
						var type = questNode.SelectToken("Data.$type").ToString();
						b.Add(type);

						var ts = questNode.SelectToken("Data.type.Data.$type");
						if (ts != null)
						{
							b.Add("    Type: " + ts.ToString());
						}

						if (type == "questConditionNodeDefinition" || type == "questPauseConditionNodeDefinition")
						{
							var condChild = questNode.SelectToken("Data.condition.Data.type.Data");
							b.Add("    CondType: " + questNode.SelectToken("Data.condition.Data.$type"));

							if (condChild != null && condChild?.SelectToken("$type")?.ToString() == "questRealtimeDelay_ConditionType")
								b.Add(
									"    Hours: " + condChild.SelectToken("hours").ToString() + "\n    Minutes: " + condChild.SelectToken("minutes").ToString() + "\n" +
									"    Seconds: " + condChild.SelectToken("seconds").ToString() + "\n    Miliseconds: " + condChild.SelectToken("miliseconds").ToString()
								);

							if (condChild != null && condChild?.SelectToken("$type")?.ToString() == "questVarComparison_ConditionType")
								b.Add("    " + condChild.SelectToken("factName").ToString() + " - " + condChild.SelectToken("comparisonType").ToString() + " - " + condChild.SelectToken("value").ToString());
						}

						if (type == "questFactsDBManagerNodeDefinition")
							b.Add("    " + questNode.SelectToken("Data.type.Data.factName").ToString() + " - Exact: " + questNode.SelectToken("Data.type.Data.setExactValue").ToString() + " - Value: " + questNode.SelectToken("Data.type.Data.value").ToString());
					}

					return b;
				}

				if (fileName.EndsWith(".scene.json"))
				{
					Dictionary<int, string> NotablePoints = new();
					var jsonNotable = jsonData.SelectTokens("Data.RootChunk.notablePoints.[*]");
					foreach (var ntb in jsonNotable)
					{
						NotablePoints.Add(ntb.SelectToken("nodeId.id").ToObject<int>(), ntb.SelectToken("name.$value").ToString());
					}

					var graph = jsonData.SelectToken("Data.RootChunk.sceneGraph.Data.graph");
					foreach (var g in graph)
					{
						var it = g.SelectToken("Data");

						var idObj = it?.SelectToken("nodeId")?.SelectToken("id");
						Console.WriteLine("" + idObj);

						if (idObj == null) continue;

						int id = int.Parse(idObj.ToString());

						List<ItemOutput> itemOuts = new();

						var outScksMapp = it.SelectToken("osockMappings");
						var outScks = it.SelectToken("outputSockets");
						if (outScks != null)
							foreach (var sck in outScks)
							{
								Dictionary<string, ItemOutputStamp> a = new();

								var dsts = sck.SelectToken("destinations");
								foreach (var dst in dsts)
								{
									var destId = dst.SelectToken("nodeId.id");
									Console.WriteLine("-" + destId);

									if (!a.ContainsKey(destId.ToString()))
										a.Add(destId.ToString(), new() { Name = dst.SelectToken("isockStamp.name").ToString(), Ordinal = dst.SelectToken("isockStamp.ordinal").ToString() } );
								}

								itemOuts.Add(new()
								{
									OutParams = (outScksMapp != null ? (outScksMapp[itemOuts.Count].SelectToken("$value").ToString() + " -> ") : "") + "name: " + sck.SelectToken("stamp.name").ToString() + ", ordinal: " + sck.SelectToken("stamp.ordinal").ToString(),
									OutDests = a
								});
							}

						List<string> b = getNodeParams(it.SelectToken("questNode"));

						var events = it.SelectToken("events");
						if (events != null)
						{
							int index = 0;
							foreach (var ev in events)
							{
								var type = ev.SelectToken("Data.$type").ToString();

								string strToDisplay = $"#{index} - " + ev.SelectToken("Data.startTime").ToString() + " -> " + type;

								if (type == "scneventsSocket")
								{
									strToDisplay += ", name: " + ev.SelectToken("Data.osockStamp.name").ToString() +
										", ordinal: " + ev.SelectToken("Data.osockStamp.ordinal").ToString();
								}

								if (type == "scneventsVFXEvent")
								{
									var eff = ev.SelectToken("Data.effectEntry.effectName.$value")?.ToString();
									strToDisplay += ", action: " + ev.SelectToken("Data.action").ToString() + (eff != null ? ", effect: " + eff : "");
								}

								b.Add(strToDisplay);
								index++;
							}
						}

						var duration = it.SelectToken("sectionDuration");
						if (duration != null)
							b.Add("    Duration: " + duration.SelectToken("stu").ToString());

						var ntbName = "";
						if (NotablePoints.ContainsKey(id)) ntbName = " - " + NotablePoints[id];

						var inScksMapp = it.SelectToken("isockMappings");
						List<ItemInput> itemInps = new();
						if (inScksMapp != null)
							foreach (var inSck in inScksMapp)
							{
								itemInps.Add(new()
								{
									InParams = inSck.SelectToken("$value").ToString(),
									Ins = itemInps.Count.ToString()
								});
							}
						else
						{
							itemInps.Add(new()
							{
								InParams = "In",
								Ins = "gen_in"
							});
							itemInps.Add(new()
							{
								InParams = "CutDestination",
								Ins = "gen_cut"
							});
						}

						Items.Add(id.ToString(), new() { Outputs = itemOuts, Inputs = itemInps, Draw = false, Name = it.SelectToken("$type").ToString() + ntbName, Params = b });
					}
				}
				if (fileName.EndsWith(".questphase.json"))
				{
					var graph = jsonData.SelectToken("Data.RootChunk.graph.Data.nodes");

					string findSocket(string handleID)
					{
						foreach (var fsG in graph)
						{
							var fsSockets = fsG.SelectToken("Data.sockets");
							foreach (var fsSocket in fsSockets)
							{
								var fsSocketHandleID = fsSocket.SelectToken("HandleId")?.ToString();
								if (fsSocketHandleID == handleID)
								{
									return fsG.SelectToken("Data.id").ToString();
								}

								var fsSocketHandleRefID = fsSocket.SelectToken("HandleRefId")?.ToString();
								if (fsSocketHandleRefID == handleID)
								{
									return fsG.SelectToken("Data.id").ToString();
								}
							}
						}

						return "";
					}

					Dictionary<string, string> nodesSckNames = [];
					string findParentSocketName(JToken jToken, string srcHandleID)
					{
						var socketsDefs = (graph as JArray).Descendants().Where(a => a.SelectToken("Data.$type")?.ToString() == "questSocketDefinition");
						foreach (var socketsDef in socketsDefs)
						{
							var handleID = socketsDef.SelectToken("HandleId").ToString();
							if (handleID == srcHandleID)
							{
								var sckName = socketsDef.SelectToken("Data.name.$value").ToString();
								var sckType = socketsDef.SelectToken("Data.type").ToString();

								if (!nodesSckNames.ContainsKey(handleID))
									nodesSckNames.Add(handleID, "(" + handleID + ") " + sckName); // + sckType + ": "

								return handleID;
							}
						}

						return "";
					}

					Dictionary<string, Dictionary<string, Dictionary<string, ItemOutputStamp>>> nodesConns = [];
					Dictionary<string, List<string>> nodesDstConns = [];

					var connDefs = (graph as JArray).Descendants().Where(a => a.SelectToken("Data.$type")?.ToString() == "graphGraphConnectionDefinition");
					foreach (var connDef in connDefs)
					{
						var dest = "";
						var src = "";

						var destHandleRef = connDef.SelectToken("Data.destination.HandleRefId")?.ToString();
						if (destHandleRef != null) dest = destHandleRef;
						var destHandle = connDef.SelectToken("Data.destination.HandleId")?.ToString();
						if (destHandle != null) dest = destHandle;

						var srcHandleRef = connDef.SelectToken("Data.source.HandleRefId")?.ToString();
						if (srcHandleRef != null) src = srcHandleRef;
						var srcHandle = connDef.SelectToken("Data.source.HandleId")?.ToString();
						if (srcHandle != null) src = srcHandle;

						var destNode = findSocket(dest);
						var srcNode = findSocket(src);

						if (destNode != "" && srcNode != "")
						{
							var srcSckHandleID = findParentSocketName(connDef, src);
							var dstSckHandleID = findParentSocketName(connDef, dest);

							if (!nodesConns.TryGetValue(srcNode, out Dictionary<string, Dictionary<string, ItemOutputStamp>> value))
							{
								nodesConns.Add(srcNode, new() { { srcSckHandleID, new() { { destNode, new() { Name = dstSckHandleID } } } } });
							}
							else
							{
								if (!value.TryGetValue(srcSckHandleID, out Dictionary<string, ItemOutputStamp> value2))
								{
									value.Add(srcSckHandleID, new() { { destNode, new() { Name = dstSckHandleID } } });
								}
								else
								{
									value2.Add(destNode, new() { Name = dstSckHandleID });
								}
							}

							if (!nodesDstConns.TryGetValue(destNode, out List<string> value3))
								nodesDstConns.Add(destNode, [dstSckHandleID]);
							else if (!value3.Contains(dstSckHandleID))
								value3.Add(dstSckHandleID);
						}

						Console.WriteLine("Nodes: " + srcNode + " > " + destNode + ", Handles: " + src + " > " + dest);
					}

					foreach (var g in graph)
					{
						var it = g.SelectToken("Data");

						var idObj = it?.SelectToken("id");

						if (idObj == null) continue;

						int id = int.Parse(idObj.ToString());

						List<ItemOutput> itemOuts = new();
						List<ItemInput> itemInps = new();
						List<string> b = getNodeParams(g);

						if (nodesConns.ContainsKey(idObj.ToString()))
						{
							var nodesConn = nodesConns[idObj.ToString()];
							foreach (var connSck in nodesConn)
							{
								itemOuts.Add(new()
								{
									OutParams = nodesSckNames[connSck.Key],
									OutDests = connSck.Value
								});
							}
						}

						foreach (var nodesDstConn in nodesDstConns)
						{
							if (nodesDstConn.Key == idObj.ToString())
							{
								foreach (var sckHandleID in nodesDstConn.Value)
								{
									itemInps.Add(new()
									{
										InParams = nodesSckNames[sckHandleID],
										Ins = sckHandleID
									});
								}
							}
						}

						Items.Add(id.ToString(), new() { Outputs = itemOuts, Inputs = itemInps, Draw = false, Name = it.SelectToken("$type").ToString(), Params = b });
					}
				}

				int x = 0;
				int y = 0;

				List<string> ItemsDraw = new();

				int dr(string id, Item item, int xs = 0)
				{
					int thisH = 0;

					if (!item.Draw)
					{
						var w = new Widget();
						w.ID = w.Header.Text = id + " - " + item.Name;
						w.Header.Foreground = Brushes.White;
						w.Width = boxWidth;
						w.HeaderRectangle.Fill = item.Name == "scnEndNode" ? Brushes.Red : Brushes.Green;
						canvas.Children.Add(w);
						Canvas.SetLeft(w, xs);
						Canvas.SetTop(w, y);

						for (int i = 0; i < item.Inputs.Count; i++)
							w.list.Children.Add(new TextBlock() { Text = item.Inputs[i].InParams, Background = Brushes.DarkOrange, Foreground = Brushes.Black });

						for (int i = 0; i < item.Outputs.Count; i++)
							w.list.Children.Add(new TextBlock() { Text = item.Outputs[i].OutParams, Background = Brushes.DarkRed });

						foreach (var p in item.Params)
							w.list.Children.Add(new TextBlock() { Text = p });

						item.X = xs;
						item.Y = y;
						item.Draw = true;

						ItemsDraw.Add(id);

						y += 100;
						thisH = 100;

						int childH = 0;
						foreach (var sub in item.Outputs)
						{
							foreach (var sub2 in sub.OutDests)
							{
								var p = Items[sub2.Key];

								childH += dr(sub2.Key, p, xs + boxWidth + space);
							}
						}

						thisH += childH;

						var tmp = 0;
						for (int i = 0; i < item.Outputs.Count; i++)
						{
							tmp += 17;
							thisH += 17;
						}
						foreach (var p in item.Params)
						{
							tmp += 17;
							thisH += 17;
						}

						if (childH < tmp + 100)
							y = y - childH + tmp;
					}

					return thisH;
				}

				if (fileName.EndsWith(".scene.json"))
				{
					var entryPoints = jsonData.SelectToken("Data.RootChunk.entryPoints");
					foreach (var ep in entryPoints)
					{
						string start = ep.SelectToken("nodeId").SelectToken("id").ToString();
						var startItem = Items[start];
						dr(start, startItem, x);
					}
				}
				if (fileName.EndsWith(".questphase.json"))
				{
					var graph = jsonData.SelectToken("Data.RootChunk.graph.Data.nodes");
					foreach (var g in graph)
					{
						if (g.SelectToken("Data.$type").ToString() == "questInputNodeDefinition")
						{
							string start = g.SelectToken("Data.id").ToString();
							var startItem = Items[start];
							dr(start, startItem, x);
						}
					}
				}

				/*foreach (var item in Items)
                {
                    dr(item.Key, item.Value);
                }*/

				int selClr = 0;
				foreach (var item in Items)
				{
					if (item.Value.Draw)
						for (int i = 0; i < item.Value.Outputs.Count; i++)
						{
							foreach (var sub in item.Value.Outputs[i].OutDests)
							{
								var p = Items[sub.Key];
								if (p.Draw)
								{
									for (int j = 0; j < p.Inputs.Count; j++)
									{
										if (sub.Value.Ordinal == p.Inputs[j].Ins || (sub.Value.Name == "0" && p.Inputs[j].Ins == "gen_in") || (sub.Value.Name == "1" && p.Inputs[j].Ins == "gen_cut"))
										{
											ArrowLineNew l = new()
											{
												StrokeThickness = 2,
												Stroke = new SolidColorBrush(linesColors[selClr]),
												X1 = item.Value.X + boxWidth,
												Y1 = item.Value.Y + 40 + (16 * i) + (item.Value.Inputs.Count * 16),
												X2 = p.X,
												Y2 = p.Y + 40 + (j * 16)
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

								/*var p = Items[sub];
								if (p.Draw)
								{
									ArrowLineNew l = new()
									{
										StrokeThickness = 2,
										Stroke = new SolidColorBrush(linesColors[selClr]),
										X1 = item.Value.X + boxWidth,
										Y1 = item.Value.Y + 40 + (16 * i),
										X2 = p.X,
										Y2 = p.Y + 15
									};
									l.MakeBezierAlt = true;
									l.MakePoly = false;
									canvas.Children.Add(l);

									selClr++;
									if (selClr >= linesColors.Count)
										selClr = 0;
								}*/
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
													 //linesColors.Add(Color.Parse("#3e2723")); // brown
													 //linesColors.Add(Color.Parse("#212121")); // grey
													 //linesColors.Add(Color.Parse("#263238")); // blue grey

			/*var a = typeof(Colors).GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public)
                                .Select(c => (Color)c.GetValue(null, null))
                                .ToList();

            foreach (var c in a)
                if (!linesColors.Contains(c))
                    linesColors.Add(c);*/
		}
	}

	class Item
	{
		public int X { get; set; }

		public int Y { get; set; }

		public bool Draw { get; set; }

		public string Name { get; set; }

		public List<ItemOutput> Outputs = new();

		public List<ItemInput> Inputs = new();

		public List<string> Params = new();
	}

	class ItemOutput
	{
		public string OutParams { get; set; }

		public Dictionary<string, ItemOutputStamp> OutDests { get; set; }
	}

	class ItemOutputStamp
	{
		public string Name { get; set; }
		
		public string Ordinal { get; set; }
	}

	class ItemInput
	{
		public string InParams { get; set; }

		public string Ins { get; set; }
	}
}

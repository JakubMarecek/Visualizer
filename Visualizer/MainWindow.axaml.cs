using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WpfPanAndZoom.CustomControls;

namespace Visualizer
{
	public partial class MainWindow : Window
	{
		const int boxWidth = 500;
		const int space = 200;

		public static MainWindow MainWnd;
		Dictionary<string, Item> Items = new();
		bool isQuest = false;

		public static SolidColorBrush brushBlue = new SolidColorBrush(Color.Parse("#1e90ff"));

		public MainWindow()
		{
			InitializeComponent();
			MainWnd = this;
			AddColors();

			canvas.Moving += new PanAndZoomCanvas.MovingEventHandler(HandleMoving);
			canvas.Zoomed += new PanAndZoomCanvas.ZoomEventHandler(HandleZoomed);
			canvas.PointerPressed += W_MouseDoubleClick;
			canvas.Moved += new PanAndZoomCanvas.MovedEventHandler(HandleMoved);
			canvas.Debug += new PanAndZoomCanvas.DebugEventHandler(HandleDebug);
		}

		private void HandleMoving(int id, double x, double y)
		{
			UpdateLine(id);
		}

		private void HandleZoomed(int zoom)
		{
		}

		private void W_MouseDoubleClick(object sender, PointerPressedEventArgs e)
		{
		}

		private void HandleMoved()
		{
		}

		private void HandleDebug(string txt)
		{
			debugTxt.Content += txt + Environment.NewLine;
		}

		private void searchTB_KeyUp(object sender, KeyEventArgs e)
		{
			foreach (var child in canvas.Children)
			{
				if (child is not Avalonia.Controls.Shapes.Line)
					child.Opacity = searchTB.Text == "" ? 1 : 0.25;

				if (searchTB.Text != "")
					if (child is Widget w)
					{
						if (w.ID.ToString() == searchTB.Text)
						{
							w.Opacity = 1;
						}
					}

				if (child is ArrowLineNew line)
				{
					if (line.ToBoxUI.ID.ToString() == searchTB.Text || line.FromBoxUI.ID.ToString() == searchTB.Text)
					{
						line.Opacity = 1;
					}
				}
			}
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				FilePickerOpenOptions opts = new();
				opts.AllowMultiple = false;
				opts.FileTypeFilter = new FilePickerFileType[] { new("Json") { Patterns = new[] { "*.scene.json", "*.questphase.json" } } };
				opts.Title = "Select json";

				var d = await StorageProvider.OpenFilePickerAsync(opts);
				if (d != null && d.Count > 0)
				{
					var fileName = d[0].Path.LocalPath;
					Title = Path.GetFileName(fileName) + " - " + fileName;

					var text = File.ReadAllText(fileName);
					var jsonData = (JObject)JsonConvert.DeserializeObject(text);

					/*List<string> getNodeParams(JToken questNode)
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
					}*/

					List<string> foundHandleIDs = [];
					var handleIDs = jsonData.Descendants().OfType<JProperty>().Where(a => a.Name.ToString() == "HandleId");
					foreach (var handleID in handleIDs)
					{
						foundHandleIDs.Add(handleID.Value.ToString());
					}
					var qs = foundHandleIDs.GroupBy(x => x)
						.Select(x => new
						{
							Count = x.Count(),
							Name = x.Key
						})
						.OrderByDescending(x => x.Count);
					foreach (var q in qs)
					{
						if (q.Count > 1)
						{
							var t = "Duplicate HandleID: " + q.Name + " (" + q.Count + "x)";
							Console.WriteLine(t);
							HandleDebug(t);
						}
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
							string nodeType = it.SelectToken("$type").ToString();

							List<ItemOutput> itemOuts = new();

							var outScksMapp = it.SelectToken("osockMappings");
							var outScks = it.SelectToken("outputSockets");
							if (outScks != null)
								foreach (var sck in outScks)
								{
									List<ItemConnector> a = new();

									var dsts = sck.SelectToken("destinations");
									foreach (var dst in dsts)
									{
										var destId = dst.SelectToken("nodeId.id");
										Console.WriteLine("-" + destId);

										//if (!a.ContainsKey(destId.ToString()))
										a.Add(new() { SourceID = idObj.ToString(), DestinationID = destId.ToString(), Name = dst.SelectToken("isockStamp.name").ToString(), Ordinal = dst.SelectToken("isockStamp.ordinal").ToString() });
									}

									var outputName = "";
									if (outScksMapp == null) outputName = GetOutputsNames(nodeType, sck.SelectToken("stamp.name").ToObject<int>());

									itemOuts.Add(new()
									{
										OutputName = (outScksMapp != null ? outScksMapp[itemOuts.Count].SelectToken("$value").ToString() : "") + outputName,
										Name = sck.SelectToken("stamp.name").ToString(),
										Ordinal = sck.SelectToken("stamp.ordinal").ToString(),
										Connections = a
									});
								}

							/*List<string> b = getNodeParams(it.SelectToken("questNode"));

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
							*/

							var rootChunk = jsonData.SelectToken("Data.RootChunk");

							Dictionary<string, string> prms = [];
							if (nodeType == "scnQuestNode")
								prms = NodeProperties.GetPropertiesForQuestNode(it.SelectToken("questNode.Data"), rootChunk);
							else
								prms = NodeProperties.GetPropertiesForSectionNode(it, rootChunk);
							//if (nodeType == "scnSectionNode" || nodeType == "scnRewindableSectionNode") prms = NodeProperties.GetPropertiesForSectionNode(it);
							//if (nodeType == "scnStartNode" || nodeType == "scnEndNode") prms = NodeProperties.GetPropertiesForSectionNode(it, rootChunk);

							var ntbName = "";
							if (NotablePoints.TryGetValue(id, out string value)) ntbName = " - " + value;

							int highestName = 0;
							int highestOrdinal = 0;

							var inScksMapp = it.SelectToken("isockMappings");
							List<ItemInput> itemInps = new();
							if (inScksMapp != null)
							{
								foreach (var inSck in inScksMapp)
								{
									itemInps.Add(new()
									{
										InputName = inSck.SelectToken("$value").ToString(),
										Name = "0",
										Ordinal = itemInps.Count.ToString()
									});
								}

								highestOrdinal = itemInps.Count - 1;
							}
							else
							{
								var inputVarCount = it.SelectToken("numInSockets")?.Value<int>() ?? 1;

								itemInps = GetInputsNames(nodeType, inputVarCount);

								foreach (var itemInp in itemInps)
								{
									highestName = Math.Max(highestName, int.Parse(itemInp.Name));
									highestOrdinal = Math.Max(highestOrdinal, int.Parse(itemInp.Ordinal));
								}
							}

							Items.Add(id.ToString(), new()
							{
								Outputs = itemOuts,
								Inputs = itemInps,
								Draw = false,
								Name = nodeType + ntbName,
								Params = prms,
								HighestName = highestName,
								HighestOrdinal = highestOrdinal
							});
						}
					}
					if (fileName.EndsWith(".questphase.json"))
					{
						isQuest = true;

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

						/*Dictionary<string, string> nodesSckNames = [];
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
						}*/

						//Dictionary<string, Dictionary<string, Dictionary<string, ItemConnector>>> nodesConns = [];
						//Dictionary<string, List<string>> nodesDstConns = [];
						List<QuestConnector> connections = [];
						List<QuestSocket> sockets = [];

						var socketDefs = (graph as JArray).Descendants().Where(a => a.SelectToken("Data.$type")?.ToString() == "questSocketDefinition");
						foreach (var socketDef in socketDefs)
						{
							sockets.Add(new()
							{
								Name = socketDef.SelectToken("Data.name.$value").Value<string>(),
								Type = socketDef.SelectToken("Data.type").Value<string>(),
								HandleID = socketDef.SelectToken("HandleId").Value<string>()
							});
						}

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
								//var srcSckHandleID = findParentSocketName(connDef, src);
								//var dstSckHandleID = findParentSocketName(connDef, dest);

								connections.Add(new()
								{
									SourceID = srcNode,
									DestinationID = destNode,
									SourceHandleID = src,
									DestinationHandleID = dest
								});

								/*if (!nodesConns.TryGetValue(srcNode, out Dictionary<string, Dictionary<string, ItemConnector>> value))
								{
									nodesConns.Add(srcNode, new() { { srcSckHandleID, new() { { destNode, new() { Name = dstSckHandleID } } } } });
								}
								else
								{
									if (!value.TryGetValue(srcSckHandleID, out Dictionary<string, ItemConnector> value2))
									{
										value.Add(srcSckHandleID, new() { { destNode, new() { Name = dstSckHandleID } } });
									}
									else
									{
										var duplicatedConnection = value2.TryAdd(destNode, new() { Name = dstSckHandleID });
										if (!duplicatedConnection)
										{
											var t = "Duplicated connection: " + srcNode + " > " + destNode + ", Handles: " + src + " > " + dest;
											Console.WriteLine(t);
											HandleDebug(t);
										}
									}
								}

								if (!nodesDstConns.TryGetValue(destNode, out List<string> value3))
									nodesDstConns.Add(destNode, [dstSckHandleID]);
								else if (!value3.Contains(dstSckHandleID))
									value3.Add(dstSckHandleID);*/
							}

							Console.WriteLine("Nodes: " + srcNode + " > " + destNode + ", Handles: " + src + " > " + dest);
						}

						var dupes = connections.GroupBy(x => new { x.SourceID, x.DestinationID, x.SourceHandleID, x.DestinationHandleID }).Where(x => x.Skip(1).Any());
						foreach (var dup in dupes)
						{
							var t = "Duplicated connection: " + dup.Key.SourceID + " > " + dup.Key.DestinationID + ", Handles: " + dup.Key.SourceHandleID + " > " + dup.Key.DestinationHandleID;
							Console.WriteLine(t);
							HandleDebug(t);
						}

						foreach (var g in graph)
						{
							var it = g.SelectToken("Data");
							string nodeType = it.SelectToken("$type").ToString();

							var idObj = it?.SelectToken("id");

							if (idObj == null) continue;

							int id = int.Parse(idObj.ToString());

							List<ItemOutput> itemOuts = new();
							List<ItemInput> itemInps = new();

							Dictionary<string, string> prms = NodeProperties.GetPropertiesForQuestNode(it);

							foreach (var socket in it.SelectToken("sockets"))
							{
								var handleId = socket.SelectToken("HandleId")?.Value<string>();
								var handleRefId = socket.SelectToken("HandleRefId")?.Value<string>();

								var socketDef = sockets.SingleOrDefault(a => a.HandleID == handleId || a.HandleID == handleRefId);
								if (socketDef.Type == "Input" || socketDef.Type == "CutDestination")
								{
									itemInps.Add(new()
									{
										InputName = socketDef.Name,
										HandleID = socketDef.HandleID
									});
								}
								if (socketDef.Type == "Output" || socketDef.Type == "CutSource")
								{
									List<ItemConnector> c = [];

									foreach (var conn in connections)
									{
										if (conn.SourceHandleID == socketDef.HandleID)
											c.Add(new()
											{
												DestinationID = conn.DestinationID,
												HandleID = conn.DestinationHandleID
											});
									}

									itemOuts.Add(new()
									{
										OutputName = socketDef.Name,
										HandleID = socketDef.HandleID,
										Connections = c
									});
								}
							}




							/*if (nodesConns.ContainsKey(idObj.ToString()))
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
							}*/

							Items.Add(id.ToString(), new() { Outputs = itemOuts, Inputs = itemInps, Draw = false, Name = nodeType, Params = prms });
						}
					}

					/*
										if (fileName.EndsWith(".questphase.json"))
										{
											isQuest = true;

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
															var duplicatedConnection = value2.TryAdd(destNode, new() { Name = dstSckHandleID });
															if (!duplicatedConnection)
															{
																var t = "Duplicated connection: " + srcNode + " > " + destNode + ", Handles: " + src + " > " + dest;
																Console.WriteLine(t);
																HandleDebug(t);
															}
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
												string nodeType = it.SelectToken("$type").ToString();

												var idObj = it?.SelectToken("id");

												if (idObj == null) continue;

												int id = int.Parse(idObj.ToString());

												List<ItemOutput> itemOuts = new();
												List<ItemInput> itemInps = new();

												Dictionary<string, string> prms = NodeProperties.GetPropertiesForQuestNode(it);

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

												Items.Add(id.ToString(), new() { Outputs = itemOuts, Inputs = itemInps, Draw = false, Name = nodeType, Params = prms });
											}
										}
					*/






					/*bool checkInput(string nodeType, string name, string ins)
					{
						Dictionary<string, string> insNames = Data.GetInputsNames(nodeType);

						var nameInt = int.Parse(name);

						if (insNames.Count > nameInt && insNames.Count > 0)
							return insNames.ElementAt(nameInt).Key == ins;
						else
							return false;
					}*/

					foreach (var item in Items)
					{
						for (int i = 0; i < item.Value.Outputs.Count; i++)
						{
							foreach (var sub in item.Value.Outputs[i].Connections)
							{
								var p = Items[sub.DestinationID];
								{
									for (int j = 0; j < p.Inputs.Count; j++)
									{
										if (!isQuest)
										{
											var ord = int.Parse(sub.Ordinal);
											if ((p.Name == "scnHubNode" || p.Name == "scnXorNode") && ord > p.HighestOrdinal)
											{
												p.Inputs.Add(new() { InputName = "In", Name = "0", Ordinal = sub.Ordinal });
												p.HighestOrdinal++;
											}
										}

										if ((!isQuest && sub.Name == p.Inputs[j].Name && sub.Ordinal == p.Inputs[j].Ordinal) || (isQuest && sub.HandleID == p.Inputs[j].HandleID))
										{
											p.Inputs[j].IsUsed = true;
										}
									}
								}
							}
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
							w.ID = int.Parse(id);
							w.Header.Text = "[" + id + "] " + item.Name;
							w.Header.Foreground = Brushes.White;
							w.Width = boxWidth;

							if (item.Name == "scnStartNode" || item.Name == "questInputNodeDefinition") w.HeaderRectangle.Fill = new SolidColorBrush(Color.Parse("#FF076C00"));
							if (item.Name == "scnEndNode" || item.Name == "questOutputNodeDefinition") w.HeaderRectangle.Fill = new SolidColorBrush(Color.Parse("#81FF0004"));

							w.ZIndex = 10;
							canvas.Children.Add(w);
							Canvas.SetLeft(w, xs);
							Canvas.SetTop(w, y);

							for (int i = 0; i < item.Inputs.Count; i++)
							{
								var g = new Grid();
								if (!item.Inputs[i].IsUsed) g.Children.Add(new Border() { BorderThickness = new(2), CornerRadius = new(10), BorderBrush = brushBlue, Width = 15, Height = 15, Margin = new(5, 0, 0, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left });
								if (item.Inputs[i].IsUsed) g.Children.Add(new Avalonia.Controls.Shapes.Ellipse() { Fill = brushBlue, Width = 15, Height = 15, Margin = new(5, 0, 0, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left });
								g.Children.Add(new TextBlock() { Text = item.Inputs[i].DisplayName, Margin = new(25, 0, 0, 0) });
								w.listInputs.Children.Add(g);
							}

							for (int i = 0; i < item.Outputs.Count; i++)
							{
								var g = new Grid();
								if (item.Outputs[i].Connections.Count == 0) g.Children.Add(new Border() { BorderThickness = new(2), CornerRadius = new(10), BorderBrush = brushBlue, Width = 15, Height = 15, Margin = new(0, 0, 5, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right });
								if (item.Outputs[i].Connections.Count > 0) g.Children.Add(new Avalonia.Controls.Shapes.Ellipse() { Fill = brushBlue, Width = 15, Height = 15, Margin = new(0, 0, 5, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right });
								g.Children.Add(new TextBlock() { Text = item.Outputs[i].DisplayName, Margin = new(0, 0, 25, 0), TextAlignment = TextAlignment.Right });
								w.listOutputs.Children.Add(g);
							}

							foreach (var p in item.Params)
							{
								var sp = new StackPanel();
								sp.Orientation = Avalonia.Layout.Orientation.Horizontal;
								sp.Children.Add(new TextBlock() { Text = p.Key, Foreground = new SolidColorBrush(Color.Parse("#999999")), Margin = new(0, 0, 4, 0) });
								sp.Children.Add(new TextBlock() { Text = p.Value, Foreground = Brushes.White });
								w.list.Children.Add(sp);
							}
							if (item.Params.Count > 0)
								w.list.Margin = new(5);

							item.Draw = true;
							item.UI = w;

							ItemsDraw.Add(id);

							y += 100;
							thisH = 100;

							int childH = 0;
							foreach (var sub in item.Outputs)
							{
								foreach (var sub2 in sub.Connections)
								{
									var p = Items[sub2.DestinationID];

									childH += dr(sub2.DestinationID, p, xs + boxWidth + space);
								}
							}

							thisH += childH;

							var tmpI = 0;
							var tmpO = 0;
							for (int i = 0; i < item.Outputs.Count; i++)
							{
								tmpO += 17;
							}
							for (int i = 0; i < item.Inputs.Count; i++)
							{
								tmpI += 17;
							}
							var tmp = Math.Max(tmpI, tmpO);
							thisH += tmp;
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
								foreach (var sub in item.Value.Outputs[i].Connections)
								{
									var p = Items[sub.DestinationID];
									if (p.Draw)
									{
										for (int j = 0; j < p.Inputs.Count; j++)
										{
											if (
												//(!isQuest && sub.Value.Ordinal == p.Inputs[j].Ins || (sub.Value.Name == "0" && p.Inputs[j].Ins == "gen_in") || (sub.Value.Name == "1" && p.Inputs[j].Ins == "gen_cut")) ||
												//!isQuest && sub.Ordinal == p.Inputs[j].Ins ||
												//checkInput(item.Value.Name, sub.Name, p.Inputs[j].Ins) ||
												//(isQuest && sub.Name == p.Inputs[j].Ins)
												(!isQuest && sub.Name == p.Inputs[j].Name && sub.Ordinal == p.Inputs[j].Ordinal) ||
												(isQuest && sub.HandleID == p.Inputs[j].HandleID)
											)
											{
												ArrowLineNew l = new()
												{
													StrokeThickness = 2,
													Stroke = new SolidColorBrush(linesColors[selClr]),
													ZIndex = 20,
													MakeBezierAlt = true,
													MakePoly = false,
													ToBoxUI = p.UI,
													ToBoxSecID = j,
													FromBoxUI = item.Value.UI,
													FromBoxSecID = i,
													FromBoxInputs = item.Value.Inputs.Count
												};
												canvas.Children.Add(l);

												selClr++;
												if (selClr >= linesColors.Count)
													selClr = 0;
											}
										}

										if (!isQuest)
										{
											if (int.Parse(sub.Name) > p.HighestName || int.Parse(sub.Ordinal) > p.HighestOrdinal)
											{
												var t = "Bad connection: " + sub.SourceID + " > " + sub.DestinationID;
												Console.WriteLine(t);
												HandleDebug(t);
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
					w.ID = -1;
					w.Header.Text = "Not included";
					w.Header.Foreground = Brushes.White;
					w.Width = 300;
					w.HeaderRectangle.Fill = Brushes.Orange;
					w.ZIndex = 10;
					w.DisableMove = true;
					canvas.Children.Add(w);
					Canvas.SetLeft(w, -500);
					Canvas.SetTop(w, 0);

					foreach (var p in Items)
						if (!ItemsDraw.Contains(p.Key))
							w.list.Children.Add(new TextBlock() { Text = p.Key });
						else
							UpdateLine(p.Value.UI.ID);

					canvas.RedrawGrid();
				}
				else
					Environment.Exit(0);
			}
			catch (Exception ex)
			{
				HandleDebug(ex.ToString());
			}
		}

		private void UpdateLine(int boxID)
		{
			foreach (var child in canvas.Children)
			{
				if (child is ArrowLineNew line)
				{
					if (line.ToBoxUI.ID == boxID || line.FromBoxUI.ID == boxID)
					{
						var a = canvas.Transform2(new(Canvas.GetLeft(line.FromBoxUI), Canvas.GetTop(line.FromBoxUI)));
						var b = canvas.Transform2(new(Canvas.GetLeft(line.ToBoxUI), Canvas.GetTop(line.ToBoxUI)));

						line.X1 = a.X + boxWidth;
						line.Y1 = a.Y + 40 + (16 * line.FromBoxSecID); // + (line.FromBoxInputs * 16)
						line.X2 = b.X;
						line.Y2 = b.Y + 40 + (line.ToBoxSecID * 16);
					}
				}
			}
		}

		public static string GetOutputsNames(string nodeType, int index)
		{
			var name = "";

			if (nodeType == "scnChoiceNode")
			{
				if (index == 0) name = "Option";
				if (index == 1) name = "AnyOption";
				if (index == 2) name = "Immediate";
				if (index == 3) name = "CancelFwd";
				if (index == 4) name = "NoOption";
				if (index == 5) name = "WhenDisplayed";
				if (index == 6) name = "Reminder";
			}
			else if (nodeType == "scnSectionNode")
			{
				if (index == 0) name = "Out";
				if (index == 1) name = "CancelForward";
				//if (index == 2) name = "TransmitSignal";
				//if (index == 3) name = "StopWork";
			}
			else if (nodeType == "scnStartNode")
			{
				if (index == 0) name = "Out";
			}
			else if (nodeType == "scnRewindableSectionNode")
			{
				if (index == 0) name = "CancelFwd";
				if (index == 1) name = "TransmitSignal";
			}
			else if (nodeType == "scnCutControlNode")
			{
				if (index == 0) name = "Out";
				if (index == 1) name = "CutSource";
			}
			else
				if (index == 0) name = "Out";

			return name != "" ? name + " " : "";
		}

		public static List<ItemInput> GetInputsNames(string nodeType, int inputVarCount = 1)
		{
			List<ItemInput> names = [];

			if (nodeType == "scnChoiceNode")
			{
				names.Add(new() { InputName = "In", Name = "0", Ordinal = "0" });
				names.Add(new() { InputName = "Cancel", Name = "1", Ordinal = "0" });
				names.Add(new() { InputName = "ReactivateGroup", Name = "2", Ordinal = "0" });
				names.Add(new() { InputName = "TimeLimitedFinish", Name = "3", Ordinal = "0" });
			}
			else if (nodeType == "scnEndNode")
			{
				names.Add(new() { InputName = "In", Name = "0", Ordinal = "0" });
			}
			else if (nodeType == "scnStartNode")
			{
			}
			else if (nodeType == "scnRewindableSectionNode")
			{
				names.Add(new() { InputName = "In", Name = "0", Ordinal = "0" });
				names.Add(new() { InputName = "Cancel", Name = "1", Ordinal = "0" });
				names.Add(new() { InputName = "Pause", Name = "2", Ordinal = "0" });
				names.Add(new() { InputName = "ForwardNormal", Name = "3", Ordinal = "0" });
				names.Add(new() { InputName = "ForwardSlow", Name = "4", Ordinal = "0" });
				names.Add(new() { InputName = "ForwardFast", Name = "5", Ordinal = "0" });
				names.Add(new() { InputName = "BackwardNormal", Name = "6", Ordinal = "0" });
				names.Add(new() { InputName = "BackwardSlow", Name = "7", Ordinal = "0" });
				names.Add(new() { InputName = "BackwardFast", Name = "8", Ordinal = "0" });
				names.Add(new() { InputName = "ForwardVeryFast", Name = "9", Ordinal = "0" });
				names.Add(new() { InputName = "BackwardVeryFast", Name = "10", Ordinal = "0" });
			}
			else if (nodeType == "scnCutControlNode")
			{
				names.Add(new() { InputName = "In", Name = "0", Ordinal = "0" });
			}
			else if (nodeType == "scnAndNode")
			{
				for (int i = 0; i < inputVarCount; i++)
					names.Add(new() { InputName = "In", Name = "0", Ordinal = i.ToString() });
				names.Add(new() { InputName = "Cancel", Name = "1", Ordinal = "0" });
			}
			else
			{
				names.Add(new() { InputName = "In", Name = "0", Ordinal = "0" });
				names.Add(new() { InputName = "Cancel", Name = "1", Ordinal = "0" });
			}

			return names;
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
		public bool Draw { get; set; }

		public string Name { get; set; }

		public Widget UI { get; set; }

		public List<ItemOutput> Outputs = new();

		public List<ItemInput> Inputs = new();

		public Dictionary<string, string> Params = new();

		public int HighestName { get; set; }

		public int HighestOrdinal { get; set; }

		public int HasInput(string name, string ordinal)
		{
			int uiIndex = 0;

			for (int j = 0; j < Inputs.Count; j++)
			{
				if (name == Inputs[j].Name && ordinal == Inputs[j].Ordinal)
				{
					return j;
				}
			}

			return uiIndex;
		}
	}

	class ItemOutput
	{
		public string DisplayName
		{
			get
			{
				return HandleID != "" ? OutputName + " [H:" + HandleID + "]" : OutputName + " [N:" + Name + ", O:" + Ordinal + "]";
			}
		}

		public string OutputName { get; set; }

		public string Name { get; set; }

		public string Ordinal { get; set; }

		public string HandleID { get; set; } = "";

		public List<ItemConnector> Connections { get; set; }
	}

	class ItemConnector
	{
		public string SourceID { get; set; }

		public string DestinationID { get; set; }

		public string Name { get; set; }

		public string Ordinal { get; set; }

		public string HandleID { get; set; }
	}

	public class ItemInput
	{
		public string DisplayName
		{
			get
			{
				return HandleID != "" ? "[H:" + HandleID + "] " + InputName : "[N:" + Name + ", O:" + Ordinal + "] " + InputName;
			}
		}

		public string InputName { get; set; }

		public string Name { get; set; }

		public string Ordinal { get; set; }

		public bool IsUsed { get; set; } = false;

		public string HandleID { get; set; } = "";
	}

	public class QuestSocket
	{
		public string Name { get; set; }

		public string HandleID { get; set; }

		public string Type { get; set; }
	}

	public class QuestConnector
	{
		public string SourceID { get; set; }

		public string DestinationID { get; set; }

		public string SourceHandleID { get; set; }

		public string DestinationHandleID { get; set; }
	}
}

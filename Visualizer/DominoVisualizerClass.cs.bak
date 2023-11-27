using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using DominoVisualizer.CustomControls;
using Gibbed.Dunia2.FileFormats;
using Gibbed.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnluacNET;
using WpfPanAndZoom.CustomControls;
using static WpfPanAndZoom.CustomControls.PanAndZoomCanvas;

namespace DominoVisualizer
{
    public class DVClass
	{
        /* TODO
		 * -param lua file open							ok / maybe
		 * -all delete ask dialog						ok
		 * -styles comboboxes							ok
		 * -unique box ID - no matter self en			ok
		 * -add array bug - adds to all items			ok
		 * -toggle box local global						ok
		 * -two exec boxes to same box -> check if exist -> add same params automatically, same with edit		ok
		 * -connections - subconnetion name				ok
		 * -create box - empty combobox err				ok
		 * -remove box - remove from registerbox		ok
		 * -comments									ok
		 *		-maybe colors							ok
		 * -boxes - with dashed border - own color - under all items		ok
		 * -save, export dialogs						ok
		 * -z indexes									ok
		 * -box with custom resources -> bnk			ok
		 * -add connector -> option with add new		ok
		 * -move bulk (border?)							ok
		 * -unsaved note								ok
		 * -border lock moving childs					ok
		 * -rename connector							ok
		 * -export fc5									ok
		 * -save alone box warn							ok
		 * -border childs make duplicate				ok
		 * -adding exec box, editing exec box - check AnchorDynType			ok
		 * -new, saved - wnd title upd					ok
		 * -change close unsaved behav					canc
		 * -settings - line style, bezier curve			canc
		 * -edit exec box - add row resets data			ok
		 * -box - return data list - variable			ok
		 * -swap box - vars rename						ok
		 * -future - custom boxes                       ok
		 * -auto add dynint - check numbering - 1		ok
		 * -add box - order list						ok
		 * -swap error - lines not move, can't save		ok
		 * -export params getdataval					ok
		 * -rename connector - btn tag					ok
		 * -exec box add box - sort						ok
		 * -adding box - if has out delayed, force global		canc
		 * -if it's NOT stateless - do not allow non global		ok
		 * -add execbox default selected				ok
		 * -own execbox color							-
		 * -duplicate - instances						ok
		 * -load all box names from graphs              ok
		 * -check if box exists in other graphs         ok
		 * -new graph save                              ok
		 * -graph boxes - disable open                  ok
		 * -workspace name UI pos                       ok
		 * -rename graph (must not be used), rename workspace		ok
		 * -add box new ID								ok
		 * -connector dyn int num - ignore non dyn int	ok
		 * -add exec box same color?					ok
		 * -border select ignore inside					ok
		 * -graphs sort									ok
		 * -dyn int sel tweak - find first free			ok
		 * -duplicate border box ID - list				ok
		 * -selection - right side						ok
		 * -edit conn var - arrays						ok
		 * -clipboard - verify before process			ok
		 * -copy box - wrong conn copy?					ok
		 * -in data type select							ok
		 * -setting box param - show type				ok
		 * -saving new doc - make name from workspace	ok
		 * -copy paste check game						ok
		 * -edit wnd header *							ok
		 * -comments bg color							maybe
		 * -edit border - load transparent checkbox		ok
		 * -save wnd file name							ok
		 * -new execbox - str missing					not err
		 * -list height add								ok
		 * -clean up arrowline							ok
		 * -selected - delete key						ok
		 * -delete line - delete points					ok
		 * -select - grid overlay						ok
		 * -delete sel - conn execbox point pos			ok
		 * -selection - points - copy					ok
		 * -stick to grid								ok		https://stackoverflow.com/questions/1892474/c-sharp-create-snap-to-grid-functionality
		 * -setting window - open in workspace			ok
		 * -adding new box, execbox -> missing in debug	ok
		 * -stick to grid - borders						ok
		 * -lines points bezier							ok
		 * -export - markers as saved					ok

		 * -global vars - show info about undefined in init		ok
		 * -edit top - change UI, not remove add
		 * -edit controlin, controlout disable
		 * -edit data in - same name err				not
		 * -bezier lines - points no bez				ok
		 * -settings - colored boxes
		 */

        string workspaceName = "";
		int selGraph = 0;
		List<DominoGraph> dominoGraphs = new();
		string datPath = "";

		Dictionary<string, DominoBox> dominoBoxes = new();
		Dictionary<string, DominoConnector> dominoConnectors = new();
		//SortedDictionary<string, DominoBoxMetadata> regBoxes = new();
		SortedDictionary<string, DominoBoxMetadata> regBoxesAll = new();
		Dictionary<ulong, string> regBoxesCRC64 = new();
		List<DominoDict> globalVariables = new();
		Dictionary<string, string> lastBoxesAssign = new();
		List<DominoDict> dominoResources = new();
		Dictionary<string, DominoComment> dominoComments = new();
		Dictionary<string, DominoBorder> dominoBorders = new();
		//DominoBoxMetadata thisMetadata = new();
		Dictionary<int, bool> testUniqueBoxID = new();
		List<LinePointCl> loadPoints = new();
		Dictionary<string, object> settings = new();

		byte[] fileBytes = null;
		string runPath = "";

        string game = "";
		string file = "";
		MemoryStream luaFile = null;
		StreamReader reader = null;
		PanAndZoomCanvas canvas;
		MainWindow wnd;

		DominoBox tempBox = null;
		List<string> processedBoxes = new();
		string errFiles = "Can't find / open these files:" + Environment.NewLine + Environment.NewLine;
		bool errFilesB = false;
		bool wasEdited = false;

        [DllImport("luac51", EntryPoint = "Process", CallingConvention = CallingConvention.Cdecl)]
        static extern int LuacLibProcess(string inPath, string outPath, string bytecodePath, out IntPtr error);

        [DllImport("luac51", EntryPoint = "ProcessBytes", CallingConvention = CallingConvention.Cdecl)]
        static extern int LuacLibProcessBytes(byte[] inBuffer, int inSize, out IntPtr outBuffer, out int outSize, string bytecodePath, out IntPtr error);

        [DllImport("luac51", EntryPoint = "FreeMem", CallingConvention = CallingConvention.Cdecl)]
        static extern void LuacLibFreeMem(IntPtr obj);

        List<string> resourcesTypes = new()
        {
            "CSoundResource",
            "CGeometryResource",
            "CTextureResource",
            "CAnimationResource",
            "CBinkResource",
            "CBinkUIResource",
            "CSequenceResource",
            "CEntityArchetypeRes",
            "WolfskinItemResource",
            "WolfskinConfigResource",
            "CFireUIResource",
        };

        List<string> dataTypes = new()
        {
            "string",
            "int",
            "float",
            "bool",
            "entity",
            "list",
            "group",
            "SoundType",
            "Sound",
            "Video",
            "genericdb",
            "sequence",
            "oasis",
            "oasiseditor",
            "database",
            "archetype",
            "animation",
            "GSF",
            "SoundMixing",
        };

        //Random r = new();

        public string GetWorkspaceName
        {
            get
            {
                return workspaceName;
            }
        }

        public string GetGraphName
        {
            get
            {
                return dominoGraphs.Count > selGraph ? dominoGraphs[selGraph].Name : "";
            }
        }

        public string CurrentFile { get { return file; } }

		public string CurrentDatPath { get { return datPath; } }

		public Dictionary<string, object> GetSettings { get { return settings; } }

		public DVClass(MainWindow window, PanAndZoomCanvas canvas, string game)
		{
			this.wnd = window;
			this.canvas = canvas;
			this.game = game;

			runPath = AppContext.BaseDirectory;
            //using var processModule = Process.GetCurrentProcess().MainModule;
            //runPath = Path.GetDirectoryName(processModule?.FileName);
            //runPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

			LoadSettings();
            ParseAllBoxes();
			AddColors();
		}

		public DVClass(MainWindow window, string dominoPath, PanAndZoomCanvas canvas)
        {
            this.wnd = window;
            file = dominoPath;
			this.canvas = canvas;

			runPath = AppContext.BaseDirectory;
            //using var processModule = Process.GetCurrentProcess().MainModule;
            //runPath = Path.GetDirectoryName(processModule?.FileName);
            //runPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            LoadSettings();
            AddColors();
		}

		public DVClass(MainWindow window, string dominoPath, PanAndZoomCanvas canvas, string game)
        {
            this.wnd = window;
            file = dominoPath;
			this.canvas = canvas;
			this.game = game;

			luaFile = new MemoryStream(File.ReadAllBytes(file));

			runPath = AppContext.BaseDirectory;
            //using var processModule = Process.GetCurrentProcess().MainModule;
            //runPath = Path.GetDirectoryName(processModule?.FileName);
            //runPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            LoadSettings();
            ParseAllBoxes();
			AddColors();
		}

		public DVClass(MainWindow window, string dominoPath, string dominoSearchFolder, PanAndZoomCanvas canvas, string game)
        {
            this.wnd = window;
            file = dominoSearchFolder;
			this.canvas = canvas;
			this.game = game;

			luaFile = new MemoryStream(File.ReadAllBytes(dominoPath));

			File.Delete(dominoPath);

			runPath = AppContext.BaseDirectory;
            //using var processModule = Process.GetCurrentProcess().MainModule;
            //runPath = Path.GetDirectoryName(processModule?.FileName);
            //runPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            LoadSettings();
            ParseAllBoxes();
			AddColors();
		}

        public void Create(string workspace, string graph, string dat)
		{
			workspaceName = workspace;
			datPath = dat;
			selGraph = 0;

			if (!datPath.EndsWith("\\"))
				datPath += "\\";

            DominoGraph g = new();
			g.Name = graph;
			g.IsDefault = true;
			g.Metadata = new();
			dominoGraphs.Add(g);

			SetWorkspaceNameAndGraphs();

			Draw();
		}

		public (bool, string) Parse()
		{
			DominoGraph g = new();
			g.Name = "Default";
			g.IsDefault = true;
			dominoGraphs.Add(g);

			string[] fileName = Path.GetFileNameWithoutExtension(file).Replace("_", " ").Split('.');
			workspaceName = fileName[0];
			g.Name = fileName.Length > 1 ? fileName[1] : fileName[0];

			SetWorkspaceNameAndGraphs();

			uint luaType = luaFile.ReadValueU32();
			if (luaType != 0x4341554C)
			{
				return (true, "Unknown LUA version. Can't open the file.");
			}

			uint luaLen = luaFile.ReadValueU32();
			byte[] luaBytesLuaq = luaFile.ReadBytes((int)luaLen);
			//byte[] luaBytesLuac = luaFile.ReadBytes((int)(luaFile.Length - luaLen - (sizeof(int) * 2)));

			// read xml and get input connectors
			dominoGraphs[selGraph].Metadata = ImportDominoMetadata();
			if (dominoGraphs[selGraph].Metadata.IsSystem)
				return (true, "System Domino box can't be opened.");

			int ctrlPosY = 0;
			foreach (var ctrl in dominoGraphs[selGraph].Metadata.ControlsIn)
			{
				DominoConnector inConn = new();
				inConn.ID = ctrl.Name;
				inConn.DrawY = ctrlPosY;
				dominoConnectors.Add(inConn.ID, inConn);
				ctrlPosY += 300;
			}

			luaFile.Close();

			var stream = new MemoryStream();

			MemoryStream luaMS = new(luaBytesLuaq);
			luaMS.ReadByte();
			uint luaqKey = luaMS.ReadValueU32();
			if (luaqKey == 0x5161754C)
			{
				luaMS.Seek(0, SeekOrigin.Begin);

				var header = new BHeader(luaMS);
				LFunction lMain = header.Function.Parse(luaMS, header);

				var d = new Decompiler(lMain);
				d.Decompile();

				var writer = new StreamWriter(stream);
				d.Print(new Output(writer));
				writer.Flush();

				fileBytes = stream.ToArray();
			}
			else
			{
				luaMS.Seek(0, SeekOrigin.Begin);

				fileBytes = luaMS.ToArray();

				stream = new MemoryStream(fileBytes);
			}

			//File.WriteAllBytes("a.lua", fileBytes);return;

			stream.Seek(0, SeekOrigin.Begin);

			reader = new StreamReader(stream, System.Text.Encoding.UTF8);

			string l;
			bool allowNewBoxes = false;
			bool externalBoxDef = false;
			bool isRealBox = false;
			bool isExtFuncs = false;
			string func = "";
			while ((l = reader.ReadLine()) != null)
			{
				l = l.Trim();

				if (l.StartsWith("cboxRes:RegisterBox("))
				{
					LoadReqBoxes(l);
				}

				if (l.StartsWith("cboxRes:LoadResource("))
				{
					string[] pp = l.Replace("cboxRes:LoadResource(", "").Replace(")", "").Replace("\"", "").Split(',');

					string file = pp[0].Trim();
					string type = pp[1].Trim();

					dominoResources.Add(new() { Name = file, Value = type });
				}

				/*if (l.Contains("l0:GetDataOutValue(") && !l.StartsWith("[") && externalBoxDef)
				{
				}
				else*/
				if (l.StartsWith("self.") && !externalBoxDef && !l.Contains("l0:GetDataOutValue(") && !isExtFuncs && l.Contains('='))
				{
					DominoDict rgv()
					{
						string[] var = l.Replace("self.", "").TrimEnd(',').Split("=");

						if (l.EndsWith("= {"))
						{
							List<DominoDict> vals = new();
							while ((l = reader.ReadLine().Trim()) != "}")
							{
								vals.Add(rgv());
							}
							return new() { Name = var[0].Trim(), ValueArray = vals };
						}
						else
						{
							return new() { Name = var[0].Trim(), Value = var[1].Trim() };
						}
					}

					globalVariables.Add(rgv());

					/*
					string[] var = l.Replace("self.", "").Split("=");

					if (l.EndsWith("= {"))
					{
						List<DominoDict> vals = new();
						while ((l = reader.ReadLine().Trim()) != "}")
						{
							var aa = l.TrimEnd(',').Split('=');
							vals.Add(new() { Name = aa[0].Trim(), Value = aa[1].Trim() });
						}
						globalVariables.Add(new() { Name = var[0].Trim(), ValueArray = vals });
					}
					else
					{
						globalVariables.Add(new() { Name = var[0].Trim(), Value = var[1].Trim() });
					}*/
				}

				if (l.StartsWith("self[") && (l.Contains("cbox:CreateBox(") || l.Contains("cbox:CreateBox_PathID(")))
				{
					if (tempBox != null)
					{
						dominoBoxes.Add(tempBox.ID, tempBox);
					}

					Regex regex = new Regex(@"self\[(.*?)\]");
					var v = regex.Match(l);
					string tempBoxID = "self[" + v.Groups[1].ToString() + "]";

					regex = new Regex(@"(cbox:CreateBox\(|cbox:CreateBox_PathID\()""(.*?)""\)");
					v = regex.Match(l);
					string tempBoxName = v.Groups[2].ToString().ToLower();

					if (l.Contains("cbox:CreateBox_PathID(") && regBoxesCRC64.ContainsKey(ulong.Parse(tempBoxName)))
					{
						tempBoxName = regBoxesCRC64[ulong.Parse(tempBoxName)];
					}

					tempBox = new();
					tempBox.ID = tempBoxID;
					tempBox.Name = tempBoxName;
				}

				if (l == "end" && tempBox != null)
				{
					dominoBoxes.Add(tempBox.ID, tempBox);
					tempBox = null;
				}

				if (tempBox != null)
				{
					if (l.StartsWith("l0:SetConnections(") && !l.EndsWith(")"))
					{
						while ((l = reader.ReadLine().Trim()) != "})")
						{
							ParseConnectionsArray(l, null);
						}
					}
				}

				if (l.StartsWith("function export:en_"))
				{
					func = l.Replace("function export:", "").Replace("()", "");
					string[] funcParts = func.Split("_");
					func = funcParts[0] + "_" + funcParts[1];
					allowNewBoxes = true;
				}

				if (l.StartsWith("function export:ex_"))
				{
					allowNewBoxes = false;
					isExtFuncs = true;
				}

				if ((l.StartsWith("l0 = Boxes[GetPathID(") || l.StartsWith("l0 = Boxes[")) && allowNewBoxes && tempBox == null)
				{
					string[] funcParts = func.Split("_");
					func = funcParts[0] + "_" + funcParts[1];

					if (dominoBoxes.ContainsKey(func))
					{
						externalBoxDef = true;
					}
					else
					{
						var regex = new Regex(@"Boxes\[(.*?)\]");
						var v = regex.Match(l);
						string tempBoxName = v.Groups[1].ToString().ToLower();
						tempBoxName = tempBoxName.Replace("\"", "");

						if (!l.Contains("GetPathID(") && regBoxesCRC64.ContainsKey(ulong.Parse(tempBoxName)))
						{
							tempBoxName = regBoxesCRC64[ulong.Parse(tempBoxName)];
						}
						else
							tempBoxName = tempBoxName.Replace("getpathid(", "").Replace(")", "");

						tempBox = new();
						tempBox.ID = func; //newBoxID.ToString();
						tempBox.Name = tempBoxName;
					}
				}
				else if (l.StartsWith("l0 = self[") && allowNewBoxes)
				{
					if (dominoBoxes.ContainsKey(l.Replace("l0 = ", "")))
					{
						func = l.Replace("l0 = ", "");
						externalBoxDef = true;
					}
				}

				if (l.Contains(":SetParentGraph(self._cbox)") && tempBox != null && allowNewBoxes)
				{
					isRealBox = true;
				}

				if (l.StartsWith("params = {") && tempBox != null && allowNewBoxes)
				{
					if (isRealBox)
						dominoBoxes.Add(tempBox.ID, tempBox);

					tempBox = null;
					func = "";
					isRealBox = false;
				}
			}

			reader.Close();
			stream.Dispose();

			foreach (DominoConnector inCtrl in dominoConnectors.Values)
			{
				if (dominoGraphs[selGraph].Metadata.ControlsIn.Where(a => a.Name == inCtrl.ID).Any())
				{
					ProcessExecBoxes(inCtrl.ID);
				}
			}

			regBoxesCRC64.Clear();

			Draw();

			if (errFilesB)
				return (false, errFiles);

			return (false, "");

			/*stream.Position = 0;
			reader.DiscardBufferedData();

			//Dictionary<string, string> externalCalls = new();

			while ((l = reader.ReadLine()) != null)
			{
				l = l.Trim();

				string connName = l.Replace("function export:", "").Replace("()", "");

				if (dominoConnectors.ContainsKey(connName))
				{
					DominoConnector conn = dominoConnectors[connName];

					Dictionary<int, string> tempParams = null;
					string boxID = "";
					string paramsFunc = "";

					while ((l = reader.ReadLine().Trim()) != "end")
					{
						if (l.StartsWith("self:"))
						{
							paramsFunc = l.Replace("self:", "").Replace("()", "");

							if (paramsFunc.StartsWith('_')) paramsFunc = paramsFunc[1..];

							if (outFuncs.Contains(paramsFunc))
							{
								conn.OutFuncName.Add(paramsFunc);
							}

							paramsFunc = "";
						}

						if (l.StartsWith("self:ex_"))
						{
							paramsFunc = l.Replace("self:", "").Replace("()", "");

							//externalCalls.Add(paramsFunc, connName);
							GetExternalFuncData(paramsFunc, connName);
						}

						if (l.StartsWith("params = self:"))
						{
							paramsFunc = l.Replace("params = self:", "").Replace("()", "");

							tempParams = GetParamsFuncData(paramsFunc);
						}

						if (l.StartsWith("l0 = "))
						{
							string boxCall = l.Replace("l0 = ", "");

							if (boxCall.StartsWith("Boxes"))
							{
								boxID = paramsFunc; //newBoxIDAssigned[paramsFunc].ToString();
							}

							if (boxCall.StartsWith("self["))
							{
								boxID = boxCall; //.Replace("self[", "").Replace("]", "");
							}
						}

						if (l.StartsWith("l0:Exec(") || l.StartsWith("l0:ExecDynInt("))
						{
							string[] execParams = l.Replace("l0:Exec(", "").Replace("l0:ExecDynInt(", "").Replace(")", "").Split(",");

							ExecBox execBox = new();
							execBox.Exec = int.Parse(execParams[0]);
							execBox.Params = tempParams;
							execBox.Box = dominoBoxes[boxID];

							if (l.Contains(":Exec(")) execBox.Type = ExecType.Exec;
							if (l.Contains(":ExecDynInt("))
							{
								execBox.Type = ExecType.ExecDynInt;
								execBox.DynIntExec = int.Parse(execParams[2]);
							}

							conn.ExecBoxes.Add(execBox);

							tempParams = null;
							boxID = "";
							paramsFunc = "";
						}
					}
				}
			}*/

			/*stream.Position = 0;
			reader.DiscardBufferedData();

			while ((l = reader.ReadLine()) != null && false)
			{
				l = l.Trim();

				string funcOrig = "";
				if (l.StartsWith("function export:ex_"))
				{
					func = l.Replace("function export:", "").Replace("()", "");
					funcOrig = func;
					string[] funcParts = func.Split("_");
					func = funcParts[0] + "_" + funcParts[1];

					Dictionary<string, string> assigns = new();
					//Dictionary<string, string> vars = new();
					while ((l = reader.ReadLine().Trim()) != "end")
					{
						if (l.StartsWith('l') && l.Contains('=') && !l.Contains(':') && !l.Contains("GetDataOutValue"))
						{
							string[] sp = l.Split('=');
							string vn = sp[0].Trim();

							if (assigns.ContainsKey(vn))
								assigns[vn] = sp[1].Trim();
							else
								assigns.Add(vn, sp[1].Trim());
						}

						if (l.Contains("GetDataOutValue"))
						{
							string[] sp = l.Split('=');

							string box = sp[1].Trim().Split(':')[0];
							string boxName = assigns[box];
							if (boxName.StartsWith("Boxes["))
								boxName = func.Replace("ex_", "en_");

							string var = sp[0].Trim();
							string varAssign = sp[1].Trim();

							foreach (var kvAssigns in assigns)
							{
								string t = kvAssigns.Value;

								if (kvAssigns.Value.StartsWith("Boxes["))
									t = func.Replace("ex_", "en_");

								var = var.Replace(kvAssigns.Key, t);
								varAssign = varAssign.Replace(kvAssigns.Key, t);
							}

							//dominoBoxes[boxName].SetVariables.Add(var, varAssign);
							dominoConnectors[externalCalls[funcOrig]].SetVariables.Add(var, varAssign);

							/*if (!vars.ContainsKey(sp[0].Trim()))
								vars.Add(sp[0].Trim(), sp[1].Trim());*

							// remove vars? - direct process - maybe fix twice l1 var assign
							// dict<box ID, last func> assigning in params l0
						}

						/*if (l.StartsWith("l0 = self["))
							if (dominoBoxes.ContainsKey(l.Replace("l0 = ", "")))
								func = l.Replace("l0 = ", "");

						if (func != "")
						{
							string[] v = l.Replace("self.", "").Split("=");
							string k = v[0].Trim();
							if (!dominoBoxes[func].SetVariables.ContainsKey(k))
								dominoBoxes[func].SetVariables.Add(k, v[1].Trim());
						}*
					}

					/*foreach (var kv in vars)
					{
						string box = kv.Value.Split(':')[0];
						string boxName = assigns[box];
						if (boxName.StartsWith("Boxes["))
							boxName = func.Replace("ex_", "en_");

						string var = kv.Key;
						string varAssign = kv.Value;

						foreach (var kvAssigns in assigns)
						{
							string t = kvAssigns.Value;

							if (kvAssigns.Value.StartsWith("Boxes["))
								t = func.Replace("ex_", "en_");

							var = var.Replace(kvAssigns.Key, t);
							varAssign = varAssign.Replace(kvAssigns.Key, t);
						}

						//dominoBoxes[boxName].SetVariables.Add(var, varAssign);
						dominoConnectors[externalCalls[funcOrig]].SetVariables.Add(var, varAssign);
					}*
				}
			}*/
		}

		private void LoadReqBoxes(string line)
		{
			string f = line.Replace("cboxRes:RegisterBox(", "").Replace(")", "").Replace("\"", "").ToLower();
			var fcrc = CRC64.Hash(f.Replace("/", "\\"));

            if (!regBoxesCRC64.ContainsKey(fcrc))
				regBoxesCRC64.Add(fcrc, f);

			if (f.ToLower().StartsWith("domino/user/"))
            {
                // parse nebo conv - ziskat xml
                //string ff = runPath + "\\lib" + f.ToLower().Replace("domino", "").Replace("/", "\\");
                string ff2 = file.Split("domino\\")[0] + f.ToLower().Replace('/', Helpers.DS);
                string ff3 = file.Replace(Path.GetFileName(file), "") + Path.GetFileName(f);
				ff3 = ff3.Replace('/', Helpers.DS).Replace('\\', Helpers.DS);

                Stream fs = new MemoryStream();

                if (File.Exists(ff2)) fs = File.Open(ff2, FileMode.Open, FileAccess.Read, FileShare.Read);
                else if (File.Exists(ff3)) fs = File.Open(ff3, FileMode.Open, FileAccess.Read, FileShare.Read);

                if (fs.Length > 0)
                {
                    var m = ParseLuaBoxFile(fs);

                    regBoxesAll.Add(f, m);
                }
                else
                {
                    errFiles += f + Environment.NewLine;
                    errFilesB = true;
                }
            }
		}

		private void ParseAllBoxes()
		{
			void a(string file)
            {
                ZipArchive zip = ZipFile.OpenRead(runPath + Helpers.DS + file);

                string zipFileStr = "";
                if (game == "fc5") zipFileStr = "FC5";
                if (game == "fcnd") zipFileStr = "FCND";
                if (game == "fc6") zipFileStr = "FC6";

                foreach (var l in zip.Entries)
                {
                    if (l.FullName.StartsWith(zipFileStr) && l.FullName.EndsWith(".lua"))
                    {
                        MemoryStream fs = new MemoryStream();

                        Stream tmpStream = l.Open();
                        tmpStream.CopyTo(fs);

                        byte[] byteBuffer = fs.ToArray();
                        string byteBufferAsString = System.Text.Encoding.UTF8.GetString(byteBuffer);
                        int offset = byteBufferAsString.IndexOf("DominoMetadata");

                        if (offset > 0)
                        {
                            var m = ParseLuaBoxFile(fs);
                            regBoxesAll.Add(l.FullName.Replace(zipFileStr + "/", ""), m);
                        }
                    }
                }
            }

			a("DominoLib.bin");
			a("DominoLibCustom.zip");
        }

		private DominoBoxMetadata ParseLuaBoxFile(Stream fs)
		{
			fs.Seek(0, SeekOrigin.Begin);

			byte[] bytes = fs.ReadBytes((int)fs.Length);

			fs.Seek(0, SeekOrigin.Begin);

			fs.ReadValueU32();
			uint len = fs.ReadValueU32();
			fs.ReadBytes((int)len);
			//byte[] xml = fs.ReadBytes((int)(fs.Length - fs.Position));
			//MemoryStream ms = new(xml);

			XDocument meta = XDocument.Load(fs);

			var m = ImportDominoMetadata(meta.Root);
			m.LuaBytes = bytes;

			/*
			m.IsStateless = meta.Element("DominoMetadata").Attribute("IsStateless").Value == "1";
			m.IsSystem = meta.Element("DominoMetadata").Attribute("IsSystem").Value == "1";
			
			var a = meta.Descendants("ControlIn");
			foreach (var b in a)
				m.ControlsIn.Add(new(b.Attribute("Name").Value, int.Parse(b.Attribute("AnchorDynType").Value), b.Attribute("HostExecFunc").Value));

			a = meta.Descendants("ControlOut");
			foreach (var b in a)
				m.ControlsOut.Add(new(b.Attribute("Name").Value, int.Parse(b.Attribute("AnchorDynType").Value), b.Attribute("IsDelayed").Value == "1"));

			a = meta.Descendants("DataIn");
			foreach (var b in a)
				m.DatasIn.Add(new(b.Attribute("Name").Value, int.Parse(b.Attribute("AnchorDynType").Value), b.Attribute("DataTypeID").Value));

			a = meta.Descendants("DataOut");
			foreach (var b in a)
				m.DatasOut.Add(new(b.Attribute("Name").Value, int.Parse(b.Attribute("AnchorDynType").Value), b.Attribute("DataTypeID").Value));
			*/
			fs.Close();

			return m;
		}

		private XElement ExportDominoMetadata()
		{
			XElement rci = new("ControlsIn");
			XElement rco = new("ControlsOut");
			XElement rdi = new("DatasIn");
			XElement rdo = new("DatasOut");

			foreach (var a in dominoGraphs[selGraph].Metadata.ControlsIn)
				rci.Add(new XElement("ControlIn", new XAttribute("Name", a.Name), new XAttribute("AnchorDynType", a.AnchorDynType), new XAttribute("HostExecFunc", a.HostExecFunc)));

			foreach (var a in dominoGraphs[selGraph].Metadata.ControlsOut)
				rco.Add(new XElement("ControlOut", new XAttribute("Name", a.Name), new XAttribute("AnchorDynType", a.AnchorDynType), new XAttribute("IsDelayed", a.IsDelayed ? "1" : "0")));

			foreach (var a in dominoGraphs[selGraph].Metadata.DatasIn)
				rdi.Add(new XElement("DataIn", new XAttribute("Name", a.Name), new XAttribute("AnchorDynType", a.AnchorDynType), new XAttribute("DataTypeID", a.DataTypeID)));

			foreach (var a in dominoGraphs[selGraph].Metadata.DatasOut)
				rdo.Add(new XElement("DataOut", new XAttribute("Name", a.Name), new XAttribute("AnchorDynType", a.AnchorDynType), new XAttribute("DataTypeID", a.DataTypeID)));

			XElement root = new("DominoMetadata", new XAttribute("IsStateless", dominoGraphs[selGraph].Metadata.IsStateless ? "1" : "0"), new XAttribute("IsSystem", dominoGraphs[selGraph].Metadata.IsSystem ? "1" : "0"));
			root.Add(rci);
			root.Add(rco);
			root.Add(rdi);
			root.Add(rdo);

			return root;
		}

		private DominoBoxMetadata ImportDominoMetadata(XElement elDM = null)
		{
			XElement root;

			DominoBoxMetadata meta = new();

			if (elDM == null)
			{
				XDocument mainMetadata = XDocument.Load(luaFile);
				root = mainMetadata.Element("DominoMetadata");
			}
			else
				root = elDM;

			meta.IsStateless = root.Attribute("IsStateless").Value == "1";
			meta.IsSystem = root.Attribute("IsSystem").Value == "1";
			
			var ctrlsIn = root.Descendants("ControlIn");
			foreach (var ctrl in ctrlsIn)
			{
				meta.ControlsIn.Add(new(ctrl.Attribute("Name").Value, int.Parse(ctrl.Attribute("AnchorDynType").Value), ctrl.Attribute("HostExecFunc").Value));
			}
			var ctrlsOut = root.Descendants("ControlOut");
			foreach (var ctrl in ctrlsOut)
			{
				meta.ControlsOut.Add(new(ctrl.Attribute("Name").Value, int.Parse(ctrl.Attribute("AnchorDynType").Value), ctrl.Attribute("IsDelayed").Value == "1"));
			}
			var datasIn = root.Descendants("DataIn");
			foreach (var data in datasIn)
			{
				meta.DatasIn.Add(new(data.Attribute("Name").Value, int.Parse(data.Attribute("AnchorDynType").Value), data.Attribute("DataTypeID").Value));
			}
			var datasOut = root.Descendants("DataOut");
			foreach (var data in datasOut)
			{
				meta.DatasOut.Add(new(data.Attribute("Name").Value, int.Parse(data.Attribute("AnchorDynType").Value), data.Attribute("DataTypeID").Value));
			}

			return meta;
		}

		private void ProcessExecBoxes(string connName)
		{
			var readerParams = new StreamReader(new MemoryStream(fileBytes), System.Text.Encoding.UTF8);

			string l = "";

			while (l != "function export:" + connName + "()" && l != null)
			{
				l = readerParams.ReadLine();
			}

			if (readerParams.EndOfStream) return;

			DominoConnector conn = dominoConnectors[connName];

			List<DominoDict> tempParams = new();
			string boxID = "";
			string paramsFunc = "";

			while ((l = readerParams.ReadLine().Trim()) != "end")
			{
				if (l.StartsWith("self:"))
				{
					paramsFunc = l.Replace("self:", "").Replace("()", "");

					if (paramsFunc.StartsWith('_')) paramsFunc = paramsFunc[1..];

					if (dominoGraphs[selGraph].Metadata.ControlsOut.Any(a => a.Name == paramsFunc))
					{
						conn.OutFuncName.Add(paramsFunc);
					}

					paramsFunc = "";
				}

				if (l.StartsWith("self:ex_"))
				{
					paramsFunc = l.Replace("self:", "").Replace("()", "");

					//externalCalls.Add(paramsFunc, connName);
					GetExternalFuncData(paramsFunc, connName);
				}

				if (l.StartsWith("params = self:"))
				{
					paramsFunc = l.Replace("params = self:", "").Replace("()", "");

					tempParams = GetParamsFuncData(paramsFunc);
				}

				if (l.StartsWith("l0 = "))
				{
					string boxCall = l.Replace("l0 = ", "");

					if (boxCall.StartsWith("Boxes"))
					{
						boxID = paramsFunc; //newBoxIDAssigned[paramsFunc].ToString();

						if (lastBoxesAssign.ContainsKey(boxCall))
							lastBoxesAssign[boxCall] = boxID;
						else
							lastBoxesAssign.Add(boxCall, boxID);
					}

					if (boxCall.StartsWith("self["))
					{
						boxID = boxCall; //.Replace("self[", "").Replace("]", "");
					}
				}

				if (l.StartsWith("l0:Exec(") || l.StartsWith("l0:ExecDynInt("))
				{
					string[] execParams = l.Replace("l0:Exec(", "").Replace("l0:ExecDynInt(", "").Replace(")", "").Split(",");

					ExecBox execBox = new();
					execBox.Exec = int.Parse(execParams[0]);
					execBox.Params = tempParams;
					execBox.Box = dominoBoxes[boxID];

					if (regBoxesAll.ContainsKey(execBox.Box.Name))
						execBox.ExecStr = regBoxesAll[execBox.Box.Name].ControlsIn[execBox.Exec].Name;

					if (l.Contains(":Exec(")) execBox.Type = ExecType.Exec;
					if (l.Contains(":ExecDynInt("))
					{
						execBox.Type = ExecType.ExecDynInt;
						execBox.DynIntExec = int.Parse(execParams[2]);
					}

					conn.ExecBoxes.Add(execBox);

					tempParams = new();
					//boxID = "";
					paramsFunc = "";
				}
			}

			readerParams.Close();

			// mozna pres isdelayed

			// linq order select

			// contains exec str , isdelayed=1 , ostatni

			/*foreach (var execBox in conn.ExecBoxes)
			{
				if (!execBox.Box.Processed)
				{
					foreach (var connection in execBox.Box.Connections)
					{
						if (connection.FromBoxConnectIDStr.StartsWith(execBox.ExecStr))
						{
							execBox.Box.Processed = true;

							if (connection.ID != null)
								ProcessExecBoxes(connection.ID);

							foreach (var subConn in connection.SubConnections)
								ProcessExecBoxes(subConn.ID);
						}
					}

					foreach (var connection in execBox.Box.Connections)
					{
						if (!connection.FromBoxConnectIDStr.StartsWith(execBox.ExecStr))
						{
							execBox.Box.Processed = true;

							if (connection.ID != null)
								ProcessExecBoxes(connection.ID);

							foreach (var subConn in connection.SubConnections)
								ProcessExecBoxes(subConn.ID);
						}
					}
				}
			}*/


			foreach (var execBox in conn.ExecBoxes)
			{
				if (!processedBoxes.Contains(execBox.Box.ID))
				{
					processedBoxes.Add(execBox.Box.ID);

					//var cns = execBox.Box.Connections.OrderBy(a => (a.FromBoxConnectIDStr.StartsWith(execBox.ExecStr) || a.IsDelayed || a.FromBoxConnectIDStr.ToLower().Contains("open") || a.FromBoxConnectIDStr.ToLower().Contains("start")) ? 0 : 1).ToList();
					var cns = execBox.Box.Connections
						.OrderByDescending(a => a.FromBoxConnectIDStr.StartsWith(execBox.ExecStr))
						.ThenByDescending(a => regBoxesAll.ContainsKey(execBox.Box.Name) ? regBoxesAll[execBox.Box.Name].ControlsOut[a.FromBoxConnectID].IsDelayed : false)
						.ThenByDescending(a => a.FromBoxConnectIDStr.ToLower().Contains("open"))
						.ThenByDescending(a => a.FromBoxConnectIDStr.ToLower().Contains("start"))
						.ThenByDescending(a => a.FromBoxConnectIDStr.ToLower() == "loaded")
						.ToList();

					foreach (var connection in cns)
					{
						if (connection.ID != null)
							ProcessExecBoxes(connection.ID);

						foreach (var subConn in connection.SubConnections)
							ProcessExecBoxes(subConn.ID);
					}
				}
			}
		}

		private void GetExternalFuncData(string funcName, string parentFunc)
		{
			var readerParams = new StreamReader(new MemoryStream(fileBytes), System.Text.Encoding.UTF8);

			string l = "";

			while (l != "function export:" + funcName + "()")
			{
				l = readerParams.ReadLine();
			}

			string func = funcName;
			string[] funcParts = func.Split("_");
			func = funcParts[0] + "_" + funcParts[1];

			Dictionary<string, string> assigns = new();
			while ((l = readerParams.ReadLine().Trim()) != "end")
			{
				if (l.StartsWith('l') && l.Contains('=') && !l.Contains(':') && !l.Contains("GetDataOutValue"))
				{
					string[] sp = l.Split('=');
					string vn = sp[0].Trim();

					if (assigns.ContainsKey(vn))
						assigns[vn] = sp[1].Trim();
					else
						assigns.Add(vn, sp[1].Trim());
				}

				if (l.Contains("GetDataOutValue"))
				{
					string[] sp = l.Split('=');

					string box = sp[1].Trim().Split(':')[0];
					/*string boxName = assigns[box];
					if (boxName.StartsWith("Boxes["))
					{
						if (lastBoxesAssign.ContainsKey(boxName))
							lastBoxesAssign[boxName] = func;
						else
							lastBoxesAssign.Add(boxName, func);

						//boxName = func.Replace("ex_", "en_");
					}*/

					string var = sp[0].Trim();
					string varAssign = sp[1].Trim();

					foreach (var kvAssigns in assigns)
					{
						string t = kvAssigns.Value;

						if (kvAssigns.Value.StartsWith("Boxes["))
							t = func.Replace("ex_", "en_");

						var = var.Replace(kvAssigns.Key, t);
						varAssign = varAssign.Replace(kvAssigns.Key, t);
					}

					if (!dominoConnectors[parentFunc].SetVariables.Any(a => a.Name == var))
						dominoConnectors[parentFunc].SetVariables.Add(new() { Name = var, Value = varAssign } /*+ GetSetVarOutName(varAssign)*/);
				}
			}

			readerParams.Close();
		}

		private List<DominoDict> GetParamsFuncData(string funcName)
		{
			List<DominoDict> prm = new();

			var readerParams = new StreamReader(new MemoryStream(fileBytes), System.Text.Encoding.UTF8);

			string l = "";

			while (l != "function export:" + funcName + "()")
			{
				l = readerParams.ReadLine();
			}

			Dictionary<string, string> assigns = new();
			while ((l = readerParams.ReadLine()) != "end")
			{
				l = l.Trim();

				if (l.StartsWith('l') && l.Contains('=') && !l.Contains(':') && !l.Contains("GetDataOutValue"))
				{
					string[] sp = l.Split('=');
					string vv = sp[0].Trim();
					string vp = sp[1].Trim();

					if (vp.StartsWith("Boxes["))
					{
						if (lastBoxesAssign.ContainsKey(vp))
							vp = lastBoxesAssign[vp].Replace("ex_", "en_");
					}

					if (assigns.ContainsKey(vv))
						assigns[vv] = vp;
					else
						assigns.Add(vv, vp);
				}
				else
				if (l.StartsWith("params = {") && l != "params = {}")
				{
					//while (!(l = readerParams.ReadLine().Trim()).StartsWith("}"))

					/*if (l.Contains("GetDataOutValue"))
						foreach (var kvAssigns in assigns)
						{
							string t = kvAssigns.Value;

							if (kvAssigns.Value.StartsWith("Boxes["))
								t = parentFunc.Replace("ex_", "en_");

							l = l.Replace(kvAssigns.Key, t);
						}
					*/

					DominoDict parsePart()
					{
						if (l.Contains("GetDataOutValue"))
							foreach (var kvAssigns in assigns)
							{
								l = l.Replace(kvAssigns.Key, kvAssigns.Value);
							}

						string num = "";
						string val;

						if (l.Contains('='))
						{
							string[] cData = l.Split('=');
							num = cData[0].Replace("[", "").Replace("]", "").Trim();
							val = cData[1].Trim();
						}
						else
							val = l.Trim();

						if (val.EndsWith("{"))
						{
							List<DominoDict> tmpPrm = new();

							bool metEnding = false;

							while (!metEnding)
							{
								l = readerParams.ReadLine().Trim().TrimEnd(',');

								/*if (!l.Contains("{}"))
								{
									if (l.Contains('{')) mB++;
									if (l.StartsWith('}')) mB--;
									if (l.StartsWith('}') && mB == 0) metEnding = true;
								}*/

								if (l == "}") metEnding = true;

								if (l != "{" && l != "}")
									tmpPrm.Add(parsePart());
							}

							return new() { Name = num, ValueArray = tmpPrm };
						}
						else if (val.EndsWith("{}"))
						{
							return new() { Name = num, ValueArray = new() };
						}
						else
						{
							val = val.Replace(",", "");
							return new() { Name = num, Value = val };
						}
					}

					prm = parsePart().ValueArray;

					/*if (l.Contains("GetDataOutValue"))
						foreach (var kvAssigns in assigns)
						{
							l = l.Replace(kvAssigns.Key, kvAssigns.Value);
						}

					string[] cData = l.Split('=');
					int num = int.Parse(cData[0].Replace("[", "").Replace("]", "").Trim());

					if (cData[0].Trim().Contains("[") && cData[1].Trim() == "{")
					{
						int mB = 0;
						bool metEnding = false;

						List<string> entr = new();
						while (!metEnding)
						{
							l = readerParams.ReadLine().Trim().TrimEnd(',');

							if (l != "{" && l != "}")
								entr.Add(l);

							if (!l.Contains("{}"))
							{
								if (l.Contains('{')) mB++;
								if (l.StartsWith('}')) mB--;
								if (l.StartsWith('}') && mB == -1) metEnding = true;
							}
						}
						prm.Add(num, entr.ToArray());
					}
					else
					{
						cData[1] = cData[1].Replace(",", "").Trim();

						//if (cData[1].Contains("GetDataOutValue"))
						//	cData[1] += GetSetVarOutName(cData[1]);

						prm.Add(new() { Name = num.ToString(), Value = cData[1] });
					}*/
				}
			}

			readerParams.Close();

			return prm;
		}

		private string GetSetVarOutName(string inputStr)
		{
			if (inputStr.Contains("GetDataOutValue"))
			{
				string[] varAssignSp = inputStr.Split(':');

				if (!dominoBoxes.ContainsKey(varAssignSp[0]))
					return "BOX NOT ASSIGNED HERE";

				int dataOutParam = int.Parse(varAssignSp[1].Replace("GetDataOutValue(", "").Replace(")", ""));
				var boxNameP = dominoBoxes[varAssignSp[0]].Name;

				if (!regBoxesAll.ContainsKey(boxNameP))
					return "";

				string varBoxOutData = inputStr + " - " + regBoxesAll[boxNameP].DatasOut[dataOutParam].Name;

				return varBoxOutData;
			}
			else
				return inputStr;
		}

		private string ParamsAsString(DominoDict p, bool export = false, int ind = 1)
		{
			string v = "";

			string indd = "";
			for (int i = 0; i < ind; i++)
				indd += "  ";

			if (p.ValueArray.Any())
			{
				v += "{" + (export ? Environment.NewLine + "  " + indd : "");

				bool a = false;
				foreach (var s in p.ValueArray)
				{
					v += (a ? ("," + (export ? Environment.NewLine + "  " + indd : " ")) : "") + (s.Name == "" ? "" : s.Name + " = ");

					v += ParamsAsString(s, export, ind++);

					a = true;
				}

				v += (export ? Environment.NewLine + indd : "") + "}";
			}
			else if (p.Value == null && !p.ValueArray.Any())
			{
				v = "{}";
			}
			else
			{
				v = export ? p.Value : GetSetVarOutName(p.Value);
			}

			return v;
		}

		private void ParseConnectionsArray(string l, DominoConnector parentConn)
		{
			if (l.Contains("[") && l.Contains("=") && !l.Contains("{") && !l.Contains("}"))
			{
				string[] cData = l.Split('=');
				int num = int.Parse(cData[0].Replace("[", "").Replace("]", "").Trim());
				cData[1] = cData[1].Replace(",", "").Replace("self.", "").Trim();

				DominoConnector conn = new();
				conn.ID = cData[1];
				conn.FromBoxConnectID = num;
				//conn.FromBoxes.Add(tempBox);

				if (regBoxesAll.ContainsKey(tempBox.Name) && parentConn == null)
				{
					conn.FromBoxConnectIDStr = regBoxesAll[tempBox.Name].ControlsOut[num].Name;
				}

				if (parentConn == null)
					tempBox.Connections.Add(conn);
				else
					parentConn.SubConnections.Add(conn);

				dominoConnectors.Add(conn.ID, conn);
			}

			else if (l.Contains("[") && l.Contains("=") && l.Contains("{"))
			{
				string[] cData = l.Split('=');
				int num = int.Parse(cData[0].Replace("[", "").Replace("]", "").Trim());

				DominoConnector conn = new();
				conn.FromBoxConnectID = num;
				//conn.FromBoxes.Add(tempBox);

				if (regBoxesAll.ContainsKey(tempBox.Name) && parentConn == null)
				{
					conn.FromBoxConnectIDStr = regBoxesAll[tempBox.Name].ControlsOut[num].Name;
				}

				if (parentConn == null)
					tempBox.Connections.Add(conn);
				else
					parentConn.SubConnections.Add(conn);

				while (!(l = reader.ReadLine().Trim()).StartsWith("}") && !l.EndsWith("}"))
				{
					ParseConnectionsArray(l, conn);
				}
			}

			else if (l.StartsWith("string = "))
			{
				string fncName = l.Replace("string = ", "").Replace(",", "").Replace("\"", "").Trim();
				parentConn.FromBoxConnectIDStr = fncName;
			}

			// parse name of func
			else if (l.StartsWith("value = "))
			{
				parentConn.ID = l.Replace("value = ", "").Replace(",", "").Replace("self.", "").Trim();
				dominoConnectors.Add(parentConn.ID, parentConn);
			}
		}

        private readonly List<LinesVal> lines = new();
        private readonly int width = 300;
        private readonly int spaceX = 400;
		private readonly List<Color> linesColors = new();

		private void AddColors()
		{
			// Colors.Red, Colors.Yellow, Colors.DodgerBlue, Colors.Green, Colors.White, Colors.Orange
			/*var a = typeof(Colors).GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public)
								.Select(c => (Color)c.GetValue(null, null))
								.ToList();

			foreach (var c in a)
				if (!linesColors.Contains(c))
					linesColors.Add(c);*/

			// https://www.materialpalette.com/colors

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

		private Color GetLight(Color source, double amount = 0.9)
		{
			HslColor hsl = new HslColor(source);
			return HslColor.ToRgb(hsl.H, hsl.S, amount);
		}

		private void HandleMoving(string id, double x, double y)
        {
            dominoConnectors.TryGetValue(id, out var conn);
            if (conn != null)
                AddConnectorLines(conn, 1);

            dominoBoxes.TryGetValue(id, out var box);
			if (box != null)
				AddBoxLines(box, 1);

			AddControlOutLines(1);

			foreach (var line in lines)
			{
				if (line.UI.Points != null)
					foreach (var point in line.UI.Points)
					{
						if (point.ID == id)
                        {
							point.Point = new(x + 7.5, y + 7.5);

                            line.UI.InvalidateVisual();
                        }
					}
			}

            WasEdited();
        }

        private void HandleZoomed(int zoom)
		{
			foreach (var b in dominoBoxes.Values)
			{
                b.Widget.list.IsVisible = zoom < -15 ? false : true;
				//b.Widget.delBtn.Visibility = zoom < -15 ? Visibility.Hidden : Visibility.Visible;
				//b.Widget.swapBtn.Visibility = zoom < -15 ? Visibility.Hidden : Visibility.Visible;
				b.Widget.HeaderGrid.IsVisible = zoom < -15 ? false : true;
				b.Widget.Border.IsVisible = zoom < -15 ? false : true;
			}

			foreach (var b in dominoConnectors.Values)
			{
				b.Widget.list.IsVisible = zoom < -15 ? false : true;
				//b.Widget.delBtn.Visibility = zoom < -15 ? Visibility.Hidden : Visibility.Visible;
				//b.Widget.editBtn.Visibility = zoom < -15 ? Visibility.Hidden : Visibility.Visible;
				b.Widget.HeaderGrid.IsVisible = zoom < -15 ? false : true;
				b.Widget.Border.IsVisible = zoom < -15 ? false : true;
			}

			foreach (var b in dominoComments.Values)
				b.ContainerUI.IsVisible = zoom < -30 ? false : true;

			// done directly in canvas
			/*foreach (var b in lines)
				b.UI.IsVisible = zoom < -30 ? false : true;*/

			/*foreach (var b in dominoBorders)
				b.ContainerUI.Visibility = zoom < -20 ? Visibility.Hidden : Visibility.Visible;*/
		}

		private void HandleMoved()
		{
			void setGrid(int x, int y)
			{
				if (x < canvas.MinX)
					canvas.MinX = x;

				if (x > canvas.MaxX)
					canvas.MaxX = x;

				if (y < canvas.MinY)
					canvas.MinY = y;

				if (y > canvas.MaxY)
					canvas.MaxY = y;

				canvas.MinX = (int)Math.Round(canvas.MinX / 100d, 0) * 100;
				canvas.MaxX = (int)Math.Round(canvas.MaxX / 100d, 0) * 100;
				canvas.MinY = (int)Math.Round(canvas.MinY / 100d, 0) * 100;
				canvas.MaxY = (int)Math.Round(canvas.MaxY / 100d, 0) * 100;
			}

			canvas.ResetGridArea();

			foreach (var b in dominoBoxes.Values)
			{
				var a = canvas.Transform2(new(Canvas.GetLeft(b.Widget), Canvas.GetTop(b.Widget)));
				setGrid((int)a.X, (int)a.Y);
			}

			foreach (var b in dominoConnectors.Values)
			{
				var a = canvas.Transform2(new(Canvas.GetLeft(b.Widget), Canvas.GetTop(b.Widget)));
				setGrid((int)a.X, (int)a.Y);
			}

			canvas.MakeGrid();
			canvas.RefreshChilds();
		}

		private void MarkBox(string boxID)
		{
			foreach (var line in lines)
			{
				if (line.Point1.Contains(boxID) || line.Point2.Contains(boxID))
				{
					line.UI.Opacity = 1;

					foreach (var child2 in canvas.Children)
						if (child2 is Widget chW2)
							if (line.Point1.Contains(chW2.ID) || line.Point2.Contains(chW2.ID))
								chW2.Opacity = 1;
				}
			}
		}

		private void W_MouseDoubleClick(object sender, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(canvas).Properties;

            if (e.ClickCount == 2 && props.IsLeftButtonPressed)
			{
				var except = (Control)e.Source;

				foreach (var child in canvas.Children)
					((Control)child).Opacity = 1;

				if (except is Widget || except is ArrowLineNew)
					foreach (var child in canvas.Children)
						if (child != except && (child is Widget || child is ArrowLineNew))
							((Control)child).Opacity = 0.25;

				if (e.Source is Widget)
					foreach (var child in canvas.Children)
						if (child == (Control)e.Source && child is Widget chW)
							MarkBox(chW.ID);

				if (e.Source is ArrowLineNew)
				{
					foreach (var line in lines)
					{
						if (line.UI == e.Source)
						{
							foreach (var child in canvas.Children)
								if (child is Widget chW)
									if (line.Point1.Contains(chW.ID) || line.Point2.Contains(chW.ID))
										chW.Opacity = 1;
						}
					}
				}
			}

            if (e.ClickCount == 1 && (props.IsMiddleButtonPressed || (props.IsLeftButtonPressed && e.KeyModifiers == KeyModifiers.Shift)))
            {
                if (e.Source is ArrowLineNew)
                {
                    foreach (var line in lines)
                    {
                        if (line.UI == e.Source)
                        {
							var mp = e.GetPosition(canvas);
                            var a = canvas.Transform3(mp);
                            var b = canvas.Transform(mp);

							DrawLinePoint(line, b.X, b.Y, a.X, a.Y, true);

                            canvas.RefreshChilds();

                            WasEdited();
                        }
                    }
                }

				if (e.Source is DominoUILinePoint)
                {
					var lp = e.Source as DominoUILinePoint;

                    RemoveLinePoint(lp);

                    WasEdited();
                }
            }
        }

		public string Search(string input)
		{
			bool found = false;

			/*foreach (var child in canvas.Children)
				if (child is Widget w)
					if (w.ID == input)
					{
						CleanChilds(w);
						MarkBox(w.ID);
						found = true;
					}*/

			foreach (var child in canvas.Children)
				if (!(child is Avalonia.Controls.Shapes.Line))
					((Control)child).Opacity = 0.25;

			foreach (var b in dominoBoxes.Values)
				if (b.ID.Contains(input))
				{
					MarkBox(b.ID);
					found = true;
				}

			foreach (var c in dominoConnectors.Values)
				if (c.ID.Contains(input))
				{
					MarkBox(c.ID);
					found = true;
				}

			if (!found)
				return input + " was not found.";

			return "";
		}

		private void OnOpenClick(object sender, RoutedEventArgs e)
		{
			string openParam = (string)((Button)sender).Tag;

			AskDialog("Open", "Open the selected Domino script?", () =>
			{
				File.WriteAllBytes(runPath + "\\tmp", regBoxesAll[openParam].LuaBytes);

				ProcessStartInfo startInfo = new ProcessStartInfo(runPath + "\\DominoVisualizer.exe");
				startInfo.UseShellExecute = true;
				startInfo.Arguments = "-fcver=" + game + " -fileFolder=" + file.Replace(Path.GetFileName(file), runPath + "\\tmp") + " -bytes=" + runPath + "\\tmp" + " -fn=" + openParam;
				Process.Start(startInfo);
			});
		}

		public void SelectedDelete()
		{
			bool del = false;

			foreach (var ctrl in canvas.SelectedItems)
			{
				if (ctrl is DominoUIBorder bUI)
				{
					RemoveBorder(dominoBorders[bUI.ID]);
					del = true;
				}

				if (ctrl is DominoUIComment cUI)
				{
					RemoveComment(dominoComments[cUI.ID]);
					del = true;
				}

				if (ctrl is DominoUIBox boxUI)
				{
					RemoveBox(dominoBoxes[boxUI.ID]);
					del = true;
				}

				if (ctrl is DominoUIConnector connUI)
				{
					RemoveConector(dominoConnectors[connUI.ID]);
					del = true;
				}

				if (ctrl is DominoUILinePoint lpUI)
				{
					RemoveLinePoint(lpUI);
					del = true;
				}
			}

			if (del)
			{
				WasEdited();
				canvas.ResetSelection();
			}
		}

		private Widget wiMetaDataIn = null;
		private Widget wiMetaDataOut = null;
		private Widget wiMetaControlIn = null;
		private Widget wiMetaControlOut = null;
		private Widget wiGlobalVars = null;
		private Widget wiResources = null;

		private void Draw(bool workspace = false)
		{
			canvas.Moving += new MovingEventHandler(HandleMoving);
			canvas.Zoomed += new ZoomEventHandler(HandleZoomed);
			canvas.PointerPressed += W_MouseDoubleClick;
			canvas.Moved += new MovedEventHandler(HandleMoved);

			List<Point> selectedPoints = new();

			int staticBoxesHeight = 0;
			int staticBoxesHeightT = 0;

			// ==============================================================================

			wiMetaControlIn = new Widget();
			wiMetaControlIn.ID = wiMetaControlIn.Header.Text = "ControlsIn";
			wiMetaControlIn.Header.Foreground = Brushes.White;
			wiMetaControlIn.Width = width;
			wiMetaControlIn.HeaderRectangle.Fill = Brushes.Green;
			wiMetaControlIn.delBtn.IsVisible = false;
			wiMetaControlIn.DisableMove = true;
			canvas.Children.Add(wiMetaControlIn);
			Canvas.SetLeft(wiMetaControlIn, 0);
			Canvas.SetTop(wiMetaControlIn, 0);
			//Panel.SetZIndex(wiMetaControlIn, 30);
			wiMetaControlIn.ZIndex = 30;

            staticBoxesHeightT = 30;
			foreach (var inControl in dominoGraphs[selGraph].Metadata.ControlsIn)
			{
				DrawMetaControlIn(inControl);
				staticBoxesHeightT += 50;
			}
			staticBoxesHeight = Math.Max(staticBoxesHeight, staticBoxesHeightT);

			// ==============================================================================

			wiMetaControlOut = new Widget();
			wiMetaControlOut.ID = wiMetaControlOut.Header.Text = "ControlsOut";
			wiMetaControlOut.Header.Foreground = Brushes.White;
			wiMetaControlOut.Width = width;
			wiMetaControlOut.HeaderRectangle.Fill = Brushes.Green;
			wiMetaControlOut.delBtn.IsVisible = false;
			wiMetaControlOut.DisableMove = true;
			canvas.Children.Add(wiMetaControlOut);
			Canvas.SetLeft(wiMetaControlOut, spaceX);
			Canvas.SetTop(wiMetaControlOut, 0);
			//Panel.SetZIndex(wiMetaControlOut, 30);
			wiMetaControlOut.ZIndex = 30;

            staticBoxesHeightT = 30;
			foreach (var inControl in dominoGraphs[selGraph].Metadata.ControlsOut)
			{
				DrawMetaControlOut(inControl);
				staticBoxesHeightT += 50;
			}
			staticBoxesHeight = Math.Max(staticBoxesHeight, staticBoxesHeightT);

			// ==============================================================================

			wiMetaDataIn = new Widget();
			wiMetaDataIn.ID = wiMetaDataIn.Header.Text = "DatasIn";
			wiMetaDataIn.Header.Foreground = Brushes.White;
			wiMetaDataIn.Width = width;
			wiMetaDataIn.HeaderRectangle.Fill = Brushes.Green;
			wiMetaDataIn.delBtn.IsVisible = false;
			wiMetaDataIn.DisableMove = true;
			canvas.Children.Add(wiMetaDataIn);
			Canvas.SetLeft(wiMetaDataIn, spaceX * 2);
			Canvas.SetTop(wiMetaDataIn, 0);
            //Panel.SetZIndex(wiMetaDataIn, 30);
            wiMetaDataIn.ZIndex = 30;

            wiMetaDataIn.list.Children.Add(DrawBtn("Add new", "datain", AddMetadataInfo));

			staticBoxesHeightT = 50;
			foreach (var inData in dominoGraphs[selGraph].Metadata.DatasIn)
			{
				DrawMetaDataIn(inData, true);
				staticBoxesHeightT += 50;
			}
			staticBoxesHeight = Math.Max(staticBoxesHeight, staticBoxesHeightT);

			// ==============================================================================

			wiGlobalVars = new Widget();
			wiGlobalVars.ID = wiGlobalVars.Header.Text = "Global variables";
			wiGlobalVars.Header.Foreground = Brushes.White;
			wiGlobalVars.Width = width;
			wiGlobalVars.HeaderRectangle.Fill = Brushes.Green;
			wiGlobalVars.delBtn.IsVisible = false;
			wiGlobalVars.DisableMove = true;
			canvas.Children.Add(wiGlobalVars);
			Canvas.SetLeft(wiGlobalVars, spaceX * 3);
			Canvas.SetTop(wiGlobalVars, 0);
            //Panel.SetZIndex(wiGlobalVars, 30);
            wiGlobalVars.ZIndex = 30;

            wiGlobalVars.list.Children.Add(DrawBtn("Add new", "globalvar", AddMetadataInfo));

			staticBoxesHeightT = 50;
			foreach (var var in globalVariables)
			{
				DrawGlobalVar(var);
				staticBoxesHeightT += 40;
			}
			staticBoxesHeight = Math.Max(staticBoxesHeight, staticBoxesHeightT);

			// ==============================================================================

			wiMetaDataOut = new Widget();
			wiMetaDataOut.ID = wiMetaDataOut.Header.Text = "DatasOut";
			wiMetaDataOut.Header.Foreground = Brushes.White;
			wiMetaDataOut.Width = width;
			wiMetaDataOut.HeaderRectangle.Fill = Brushes.Green;
			wiMetaDataOut.delBtn.IsVisible = false;
			wiMetaDataOut.DisableMove = true;
			canvas.Children.Add(wiMetaDataOut);
			Canvas.SetLeft(wiMetaDataOut, spaceX * 4);
			Canvas.SetTop(wiMetaDataOut, 0);
            //Panel.SetZIndex(wiMetaDataOut, 30);
            wiMetaDataOut.ZIndex = 30;

            wiMetaDataOut.list.Children.Add(DrawBtn("Add new", "dataout", AddMetadataInfo));

			staticBoxesHeightT = 50;
			foreach (var data in dominoGraphs[selGraph].Metadata.DatasOut)
			{
				DrawMetaDataIn(data, false);
				staticBoxesHeightT += 50;
			}
			staticBoxesHeight = Math.Max(staticBoxesHeight, staticBoxesHeightT);

			// ==============================================================================

			wiResources = new Widget();
			wiResources.ID = wiResources.Header.Text = "Resources";
			wiResources.Header.Foreground = Brushes.White;
			wiResources.Width = width;
			wiResources.HeaderRectangle.Fill = Brushes.Green;
			wiResources.delBtn.IsVisible = false;
			wiResources.DisableMove = true;
			canvas.Children.Add(wiResources);
			Canvas.SetLeft(wiResources, spaceX * 5);
			Canvas.SetTop(wiResources, 0);
            //Panel.SetZIndex(wiResources, 30);
            wiResources.ZIndex = 30;

            wiResources.list.Children.Add(DrawBtn("Add new", "", AddResource));

			staticBoxesHeightT = 50;
			foreach (var data in dominoResources)
			{
				DrawResource(data);
				staticBoxesHeightT += 35;
			}
			staticBoxesHeight = Math.Max(staticBoxesHeight, staticBoxesHeightT);

			// ==============================================================================

			staticBoxesHeight = staticBoxesHeight + (300 - (staticBoxesHeight % 300));

            var inConns = dominoConnectors.Values.Where(c => dominoGraphs[selGraph].Metadata.ControlsIn.Where(a => a.Name == c.ID).Any());
            foreach (var inConn in inConns)
                DrawChilds(inConn, 0, staticBoxesHeight + inConn.DrawY);

            foreach (var c in dominoConnectors.Values)
                AddConnectorLines(c, 0);

            foreach (var b in dominoBoxes.Values)
                AddBoxLines(b, 0);

            AddControlInLines(0);
            AddControlOutLines(0);

            HandleMoved();

			// ==============================================================================

			Point GetFreePos(double x, double y)
			{
				var sameX = selectedPoints.Where(p => p.X == x);

				Point newPoint = new(x, y);

				if (sameX != null && sameX.Any())
				{
					var maxY = sameX.MaxBy(p => p.Y);

					double newY = y;

					while (maxY.Y > newY)
					{
						newY += 300;
					}

					newPoint = new(x, newY);
				}

				selectedPoints.Add(newPoint);

				return newPoint;
			}

			void DrawChilds(DominoConnector conn, double currX, double currY)
			{
				bool exists = false;
				//Widget w = null;
				if (conn.Widget == null)
				{
					if (workspace)
					{
						currX = conn.DrawX;
						currY = conn.DrawY;
					}
					else
					{
						var pos = GetFreePos(currX, currY);
						currX = (int)pos.X;
						currY = (int)pos.Y;
					}

                    //DrawConnector(conn, currX, currY);
                    NewDrawConnector(conn, currX, currY);
				}
				else
				{
					//w = conn.Widget;
					exists = true;
				}

				if (!workspace)
					currX += spaceX;

				double currYBox = currY;
				int colorBoxSel = 0;
				foreach (var execBox in conn.ExecBoxes)
				{
					if (!exists)
					{
						//colorBoxSel = r.Next(0, 16);

						//DrawExecBoxContainerUI(conn, execBox, colorBoxSel);

						// ===

						bool execBoxNull = false;

						if (execBox.Box.Widget == null)
						{
							if (workspace)
							{
								currX = execBox.Box.DrawX;
								currYBox = execBox.Box.DrawY;
							}
							else
							{
								var pos = GetFreePos(currX, currYBox);
								currX = (int)pos.X;
								currYBox = (int)pos.Y;
							}

                            //DrawBox(execBox.Box, currX, currYBox);
                            NewDrawBox(execBox.Box, currX, currYBox, true);

                            execBoxNull = true;

							foreach (var subConn in execBox.Box.Connections)
							{
								//DrawBoxConnectors(execBox.Box, subConn, "");

								/*if (subConn.ID != null && subConn.ID != "")
								{
									StackPanel sp2 = new();

									Grid g = new() { Height = 18 };
									g.Children.Add(new TextBox() { Text = "(" + subConn.FromBoxConnectID.ToString() + ") " + subConn.FromBoxConnectIDStr + " = " + subConn.ID, Margin = new(0, 0, 0, 0), FontWeight = FontWeights.Bold, Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });
									sp2.Children.Add(g);

									Button btnDel = new Button() { Tag = execBox.Box.ID + "|" + subConn.ID, Style = (Application.Current.FindResource("DelBtn") as Style) };
									btnDel.Click += RemoveBoxConn;
									sp2.Children.Add(btnDel);

									Border b2 = new() { BorderBrush = new SolidColorBrush(linesColors[colorConnSel]), BorderThickness = new(2, 2, 2, 2), Child = sp2 };
									wb.list.Children.Add(b2);
									
									subConn.ContainerUI = b2;

									execBox.Box.Height += 20;

									linesJoin.Add(new(execBox.Box.ID + "-P2", subConn.ID + "-P2", colorConnSel));
								}

								foreach (var subSubConn in subConn.SubConnections)
								{
									//if (aaa > 4) continue;

									StackPanel sp3 = new();

									Grid g3 = new() { Height = 18 };
									g3.Children.Add(new TextBox() { Text = "(" + subConn.FromBoxConnectID.ToString() + ") " + subConn.FromBoxConnectIDStr + " > (" + subSubConn.FromBoxConnectID.ToString() + ") " + subSubConn.FromBoxConnectIDStr + " = " + subSubConn.ID, Margin = new(0, 0, 0, 0), FontWeight = FontWeights.Bold, Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });
									sp3.Children.Add(g3);

									Border b3 = new() { BorderBrush = new SolidColorBrush(linesColors[colorConnSel]), BorderThickness = new(2, 2, 2, 2), Child = sp3 };
									wb.list.Children.Add(b3);

									execBox.Box.Height += 20;

									linesJoin.Add(new(execBox.Box.ID + "-P2", subSubConn.ID + "-P2", colorConnSel));

									colorConnSel++;
								}

								colorConnSel++;*/
							}

							//currYBox += /*(int)wb.Height +*/ 100;


							//var cns = execBox.Box.Connections;
							var cns = execBox.Box.Connections
								.OrderByDescending(a => a.FromBoxConnectIDStr.StartsWith(execBox.ExecStr))
								.ThenByDescending(a => regBoxesAll.ContainsKey(execBox.Box.Name) ? (a.FromBoxConnectID < regBoxesAll[execBox.Box.Name].ControlsOut.Count ? regBoxesAll[execBox.Box.Name].ControlsOut[a.FromBoxConnectID].IsDelayed : false) : false)
								.ThenByDescending(a => a.FromBoxConnectIDStr.ToLower().Contains("open"))
								.ThenByDescending(a => a.FromBoxConnectIDStr.ToLower().Contains("start"))
								.ThenByDescending(a => a.FromBoxConnectIDStr.ToLower() == "loaded")
								.ToList();

							foreach (var subConn in cns)
							{
								if (subConn.ID == null)
								{
									foreach (var subSubConn in subConn.SubConnections)
									{
										if (subSubConn.ID != null)
											DrawChilds(subSubConn, execBox.Box.DrawX + spaceX, execBox.Box.DrawY + 20);
										//currY += 300;
									}
								}
								else
								{
									DrawChilds(subConn, execBox.Box.DrawX + spaceX, execBox.Box.DrawY + 20);
									//currY += 300;
								}
							}
						}
						/*else
						{
							hadX = execBox.Box.DrawX;
							hadY = execBox.Box.DrawY;
						}*/

						if (execBoxNull)
							selectedPoints.Add(new(execBox.Box.DrawX, execBox.Box.DrawY + execBox.Box.Height));

						/*if (execBoxNull)
							if (execBox.Box.Height > 300)
							{
								for (int i = 0; i < execBox.Box.Height; i += 300)
								{
									selectedPoints.Add(new(execBox.Box.DrawX, execBox.Box.DrawY + i));
								}
							}*/

						colorBoxSel++;
					}
				}

				if (!exists)
					selectedPoints.Add(new(conn.DrawX, conn.DrawY + conn.Height));

				/*if (!exists)
					if (conn.Height > 300)
					{
						for (int i = 0; i < conn.Height; i += 300)
						{
							selectedPoints.Add(new(conn.DrawX, conn.DrawY + i));
						}
					}*/

				/*if (hadBox)
					currX += spaceX;

				if (hadX > 0 && hadY > 0)
				{
					currX = hadX + spaceX;
					currY = hadY;
				}

				//if (aaa > 4) return;

				if (!exists)
					foreach (var execBox in conn.ExecBoxes)
					{
						//var cns = execBox.Box.Connections;
						var cns = execBox.Box.Connections
							.OrderByDescending(a => a.FromBoxConnectIDStr.StartsWith(execBox.ExecStr))
							.ThenByDescending(a => a.IsDelayed)
							.ThenByDescending(a => a.FromBoxConnectIDStr.ToLower().Contains("open"))
							.ThenByDescending(a => a.FromBoxConnectIDStr.ToLower().Contains("start"))
							.ThenByDescending(a => a.FromBoxConnectIDStr.ToLower() == "loaded")
							.ToList();

						foreach (var subConn in cns)
						{
							if (subConn.ID == null)
							{
								foreach (var subSubConn in subConn.SubConnections)
								{
									if (subSubConn.ID != null)
										DrawConnector(subSubConn, currX, currY + 20);
									//currY += 300;
								}
							}
							else
							{
								DrawConnector(subConn, currX, currY + 20);
								//currY += 300;
							}
						}
					}*/
			}
		}

		private Button DrawBtn(string name, string tag, EventHandler<RoutedEventArgs> act)
		{
			Button btn = new Button() { Content = name, Tag = tag, FontWeight = FontWeight.Bold };
			btn.Classes.Add("BoxBtn");
			btn.Click += act;
			return btn;
		}

		private void DrawLine(double x1, double y1, double x2, double y2, string startIndex, string endIndex, int clrIndx)
		{
            ArrowLineNew l = new()
			{
				StrokeThickness = 2,
				Stroke = new SolidColorBrush(clrIndx == -1 ? Color.FromArgb(150, 150, 150, 150) : linesColors[clrIndx]),
				X1 = x1,
				Y1 = y1,
				X2 = x2,
				Y2 = y2
			};

            l.MakeBezierAlt = (bool)settings["useBezier"];
            l.MakePoly = !(bool)settings["useBezier"];

            l.PointBezier = (bool)settings["linePointsBezier"];

            l.Cursor = new Cursor(StandardCursorType.Hand);
			l.Points = new();

			canvas.Children.Add(l);
			//Panel.SetZIndex(l, 40);
			l.ZIndex = 40;

			canvas.RefreshChild(l);

			lines.Add(new(startIndex, endIndex, l));
		}

		/*private void DrawExecBoxContainerUI(DominoConnector c, ExecBox eb, int clr)
		{
			StackPanel sp = new();
			DrawExecBoxChildren(c, eb, sp);
			eb.MainUI = sp;
			eb.INT_clr = clr;
			Border b = new() { BorderBrush = new SolidColorBrush(linesColors[clr]), BorderThickness = new(2, 2, 2, 2), Child = sp, CornerRadius = new(5), Background = (bool)settings["coloredBoxes"] ? new SolidColorBrush(GetLight(linesColors[clr])) : Brushes.LightGray };
			c.Widget.list.Children.Add(b);
			eb.ContainerUI = b;
		}

		private void DrawExecBoxChildren(DominoConnector conn, ExecBox execBox, StackPanel sp)
		{
			string name = execBox.ExecStr;
			//if (regBoxesAll.ContainsKey(execBox.Box.Name))
			//	name = execBox.Exec < regBoxesAll[execBox.Box.Name].ControlsIn.Count ? regBoxesAll[execBox.Box.Name].ControlsIn[execBox.Exec].Name : "EXEC DOESN'T EXIST";

			string execStr = "Exec: " + "(" + execBox.Exec.ToString() + ") " + name;
			if (execBox.Type == ExecType.ExecDynInt) execStr += ", DynInt: " + execBox.DynIntExec.ToString();

			Grid gh = new() { Height = 18 };
			gh.Children.Add(new TextBox() { Text = execStr + " > " + execBox.Box.ID, Margin = new(0, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeight.Black, Width = double.NaN });

			Button btn = new Button() { Tag = conn.ID + "|" + execBox.Box.ID };
			btn.Classes.Add("EditBtn");
			btn.Click += EditExecBox;
			gh.Children.Add(btn);

			Button btn2 = new Button() { Tag = conn.ID + "|" + execBox.Box.ID };
            btn2.Classes.Add("EditBtn");
            btn2.Classes.Add("DelBtn");
			btn2.Click += RemoveExecBox;
            gh.Children.Add(btn2);

			sp.Children.Add(gh);

			conn.Height += 22;

            if (execBox.Params != null && execBox.Params.Count > 0)
			{
				foreach (var param in execBox.Params)
				{
					string pv = ParamsAsString(param);

					/*if (param.ValueArray.Count > 1)
						pv = "{" + string.Join(", ", param.ValueArra) + "}";
					else
						pv = GetSetVarOutName(param.Value);*

					var paramName = "PARAM DOESN'T EXIST";
					var paramType = "";

					if (regBoxesAll.ContainsKey(execBox.Box.Name))
					{
						if (int.Parse(param.Name) < regBoxesAll[execBox.Box.Name].DatasIn.Count)
                        {
                            paramName = regBoxesAll[execBox.Box.Name].DatasIn[int.Parse(param.Name)].Name;
                            paramType = " (" + regBoxesAll[execBox.Box.Name].DatasIn[int.Parse(param.Name)].DataTypeID + ")";
                        }
                    }

                    Grid g = new() { Height = 30 };
					g.Children.Add(new TextBox() { Text = "(" + param.Name + ") " + (regBoxesAll.ContainsKey(execBox.Box.Name) ? paramName : "") + paramType, Margin = new(0, 0, 0, 0), FontWeight = FontWeight.Bold, Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });
					g.Children.Add(new TextBox() { Text = pv, Margin = new(10, 13, 0, 0), Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });
					
					Border b = null;
					CheckDictVarState(ref b, param, false);
					b.Child = g;
					sp.Children.Add(b);

					conn.Height += 30;
                }
			}
		}

		private void DrawBoxConnectors(DominoBox box, DominoConnector c, string parentName = "", int selClr = -1)
		{
			string name = parentName + "(" + c.FromBoxConnectID.ToString() + ") " + c.FromBoxConnectIDStr;
			int colorConnSel = selClr >= 0 ? selClr : box.Widget.list.Children.Count - 1; // r.Next(0, 16);

			//colorConnSel = r.Next(0, linesColors.Count);

			if (c.ID != null && c.ID != "")
			{
				c.INT_clr = colorConnSel;

				StackPanel sp2 = new();

				Grid g = new() { Height = 18 };
				g.Children.Add(new TextBox() { Text = name + " = " + c.ID, Margin = new(0, 0, 20, 0), FontWeight = FontWeight.Bold, Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });

				Button btnDel = new Button() { Tag = box.ID + "|" + c.ID, Margin = new(0) };
                btnDel.Classes.Add("EditBtn");
                btnDel.Classes.Add("DelBtn");
                btnDel.Click += RemoveBoxConn;
				g.Children.Add(btnDel);

				sp2.Children.Add(g);

				Border b2 = new() { BorderBrush = new SolidColorBrush(linesColors[colorConnSel]), BorderThickness = new(2, 2, 2, 2), Child = sp2, CornerRadius = new(5), Background = (bool)settings["coloredBoxes"] ? new SolidColorBrush(GetLight(linesColors[colorConnSel])) : Brushes.LightGray };
				box.Widget.list.Children.Add(b2);

				c.ContainerUI = b2;
			}

			foreach (var sc in c.SubConnections)
			{
				DrawBoxConnectors(box, sc, name + " > ");
			}
		}

		private void DrawConnector(DominoConnector conn, double currX, double currY)
		{
			conn.INT_isIn = dominoGraphs[selGraph].Metadata.ControlsIn.Where(a => a.Name == conn.ID).Any();
			conn.INT_isOut = dominoGraphs[selGraph].Metadata.ControlsOut.Where(a => conn.OutFuncName.Contains(a.Name)).Any();

			/*var brsh = Brushes.Yellow;
			if (conn.INT_isIn) brsh = Brushes.Red;
			if (conn.INT_isOut) brsh = Brushes.Orange;*

			var brsh = new SolidColorBrush(Colors.Yellow).ToImmutable();

			var w = new DominoUIConnector();
			w.Header.Text = (conn.INT_isIn ? "ControlIn - " : "") + conn.ID;
			//w.Height = 30;
			w.Width = width;
			w.HeaderRectangle.Fill = brsh;
			w.ID = conn.ID;
			canvas.Children.Add(w);

			w.delBtn.Tag = conn.ID;
			w.delBtn.Click += (sender, e) => {

				AskDialog("Remove connector", "Do you want to remove the connector?", () =>
				{
					string tag = (string)(sender as Button).Tag;
					var conn = dominoConnectors[tag];
					RemoveConector(conn);
				});
			};

			w.editBtn.IsVisible = conn.INT_isIn || conn.INT_isOut ? false : true;
			w.editBtn.Tag = conn.ID;
			w.editBtn.Click += EditConnectorDialog;

			Canvas.SetLeft(w, currX);
			Canvas.SetTop(w, currY);
			//Panel.SetZIndex(w, 30);
			w.ZIndex = 30;
			conn.DrawX = currX;
			conn.DrawY = currY;
			conn.Widget = w;
			conn.Height = 32;

			if (!conn.INT_isOut)
			{
				w.list.Children.Add(DrawBtn("Add new exec box", conn.ID, AddExecBox));
                conn.Height += 20;
            }

			if (!conn.INT_isIn)
			{
				w.list.Children.Add(DrawBtn("Add new set variable", conn.ID, AddConnVar));
                conn.Height += 20;
            }

			foreach (var setVar in conn.SetVariables)
			{
				DrawConnVariable(conn, setVar);
				conn.Height += 40;
            }

			foreach (string outFunc in conn.OutFuncName)
			{
				StackPanel sp2 = new();

				Grid g = new() { Height = 30 };
				g.Children.Add(new TextBox() { Text = "ControlOut", FontWeight = FontWeight.Bold, Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });
				g.Children.Add(new TextBox() { Text = outFunc, Margin = new(10, 13, 0, 0), Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });
				sp2.Children.Add(g);

				Border b2 = new() { BorderBrush = new SolidColorBrush(Colors.Orange), BorderThickness = new(2, 2, 2, 2), Child = sp2, CornerRadius = new(5), Background = (bool)settings["coloredBoxes"] ? new SolidColorBrush(GetLight(Colors.Orange)) : Brushes.LightGray };
				w.list.Children.Add(b2);

				conn.Height += 55;
            }
		}

		private void DrawBox(DominoBox box, double currX, double currYBox)
		{
			var wb = new DominoUIBox();
			wb.Header.Text = box.ID + " - " + box.Name;
			wb.Header.Margin = new(0, 0, 40, 0);
			//wb.Height = 30;
			wb.Width = width;
			wb.HeaderRectangle.Fill = Brushes.White;
			wb.ID = box.ID;
			canvas.Children.Add(wb);

			wb.delBtn.Tag = box.ID;
			wb.delBtn.Click += (sender, e) => {
				AskDialog("Remove box", "Do you want to remove the box?", () =>
				{
					string tag = (string)(sender as Button).Tag;
					var b = dominoBoxes[tag];
					RemoveBox(b);
				});
			};

			wb.swapBtn.IsVisible = true;
			wb.swapBtn.Tag = box.ID;
			wb.swapBtn.Click += SwapBox;

			Canvas.SetLeft(wb, currX);
			Canvas.SetTop(wb, currYBox);
			//Panel.SetZIndex(wb, 30);
			wb.ZIndex = 30;
			box.DrawX = currX;
			box.DrawY = currYBox;
			box.Widget = wb;
			box.Height = 40;

			if (regBoxesAll.ContainsKey(box.Name) && !regBoxesAll[box.Name].IsSystem && !regBoxesAll[box.Name].INT_Graph)
			{
				wb.list.Children.Add(DrawBtn("Open in new window >>>", box.Name, OnOpenClick));
				box.Height += 20;
				box.INT_open = true;
			}

			wb.list.Children.Add(DrawBtn("Add new output connector", box.ID, AddBoxConnector));
		}*/

		private void DrawMetaDataIn(DominoBoxMetadataDatasIn inData, bool indt, bool updateUI = false)
		{
			StackPanel sp2 = new();

			Grid g2 = new() { Height = 18 };

			g2.Children.Add(new TextBox() { Text = inData.Name, FontWeight = FontWeight.Bold });

			string tag = (indt ? "datain" : "dataout") + "|" + inData.UniqueID;

			Button btn = new Button() { Tag = tag };
            btn.Classes.Add("EditBtn");
            btn.Click += EditMetadataInfo;
			g2.Children.Add(btn);

			Button btnDel = new Button() { Tag = tag };
            btnDel.Classes.Add("EditBtn");
            btnDel.Classes.Add("DelBtn");
            btnDel.Click += RemoveMetadataInfo;
			g2.Children.Add(btnDel);

			sp2.Children.Add(g2);


			sp2.Children.Add(new TextBox() { Text = "AnchorDynType: " + inData.AnchorDynType, Margin = new(10, 0, 0, 0) });
			sp2.Children.Add(new TextBox() { Text = "DataTypeID: " + inData.DataTypeID, Margin = new(10, 0, 0, 0) });

			if (updateUI)
			{
                inData.ContainerUI.Child = sp2;
            }
			else
            {
                Border b2 = new() { BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new(2, 2, 2, 2), Child = sp2, CornerRadius = new(5), Background = Brushes.LightGray };
                inData.ContainerUI = b2;
                if (indt) wiMetaDataIn.list.Children.Add(inData.ContainerUI);
                else wiMetaDataOut.list.Children.Add(inData.ContainerUI);
            }
		}

		private void DrawMetaControlIn(DominoBoxMetadataControlsIn inCtrl, bool updateUI = false)
		{
			StackPanel sp2 = new();

			Grid g2 = new() { Height = 18 };

			g2.Children.Add(new TextBox() { Text = inCtrl.Name, FontWeight = FontWeight.Bold });

			Button btn = new Button() { Tag = "controlin|" + inCtrl.Name };
            btn.Classes.Add("EditBtn");
            btn.Click += EditMetadataInfo;
			g2.Children.Add(btn);

			sp2.Children.Add(g2);

			sp2.Children.Add(new TextBox() { Text = "AnchorDynType: " + inCtrl.AnchorDynType, Margin = new(10, 0, 0, 0) });
			sp2.Children.Add(new TextBox() { Text = "HostExecFunc: " + inCtrl.HostExecFunc, Margin = new(10, 0, 0, 0) });
			
			if (updateUI)
			{
                inCtrl.ContainerUI.Child = sp2;
            }
			else
            {
				Border b2 = new() { BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new(2, 2, 2, 2), Child = sp2, Height = 60, CornerRadius = new(5), Background = Brushes.LightGray };
				inCtrl.ContainerUI = b2;
				wiMetaControlIn.list.Children.Add(inCtrl.ContainerUI);
            }
		}

		private void DrawMetaControlOut(DominoBoxMetadataControlsOut outCtrl, bool updateUI = false)
		{
			StackPanel sp2 = new();

			Grid g2 = new() { Height = 18 };

			g2.Children.Add(new TextBox() { Text = outCtrl.Name, FontWeight = FontWeight.Bold });

			Button btn = new Button() { Tag = "controlout|" + outCtrl.Name };
            btn.Classes.Add("EditBtn");
            btn.Click += EditMetadataInfo;
			g2.Children.Add(btn);

			sp2.Children.Add(g2);

			sp2.Children.Add(new TextBox() { Text = "AnchorDynType: " + outCtrl.AnchorDynType, Margin = new(10, 0, 0, 0) });
			sp2.Children.Add(new TextBox() { Text = "IsDelayed: " + (outCtrl.IsDelayed ? "true" : "false"), Margin = new(10, 0, 0, 0) });

			if (updateUI)
			{
                outCtrl.ContainerUI.Child = sp2;
            }
			else
            {
				Border b2 = new() { BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new(2, 2, 2, 2), Child = sp2, Height = 60, CornerRadius = new(5), Background = Brushes.LightGray };
				outCtrl.ContainerUI = b2;
				wiMetaControlOut.list.Children.Add(outCtrl.ContainerUI);
            }
		}

		private void RemoveLine(string a)
		{
			foreach (var i in lines)
				if (i.Point1 == a || i.Point2 == a)
				{
					canvas.Children.Remove(i.UI);

					if (i.UI.Points != null)
						foreach (var lp in i.UI.Points)
						{
							canvas.Children.Remove(lp);
						}

					break;
				}

			lines.RemoveAll(aa => aa.Point1 == a || aa.Point2 == a);
		}

        private void RemoveConector(DominoConnector conn)
		{
			foreach (var execBox in conn.ExecBoxes)
				RemoveLine(conn.ID + "-OUT-" + execBox.Box.ID);

			foreach (var box in dominoBoxes.Values)
			{
				RemoveBoxConnS(box, box.Connections, conn.ID);
			}

			var ci = dominoGraphs[selGraph].Metadata.ControlsIn.Where(a => a.Name == conn.ID).SingleOrDefault();
			if (ci != null)
			{
				RemoveLine(conn.ID + "-IN");
				wiMetaControlIn.list.Children.Remove(ci.ContainerUI);
				dominoGraphs[selGraph].Metadata.ControlsIn.Remove(ci);

				AddControlInLines(1);
			}

			foreach (var sof in conn.OutFuncName)
			{
				var co = dominoGraphs[selGraph].Metadata.ControlsOut.Where(a => a.Name == sof).SingleOrDefault();
				if (co != null)
				{
					RemoveLine("MetadataControlOut-IN-" + conn.ID);
					wiMetaControlOut.list.Children.Remove(co.ContainerUI);
					dominoGraphs[selGraph].Metadata.ControlsOut.Remove(co);

					AddControlOutLines(1);
				}
			}

			canvas.Children.Remove(conn.Widget);
			dominoConnectors.Remove(conn.ID);

			AddConnectorLines(conn, 1);
		}

        /*private Border DrawConnVariableChild(DominoConnector conn, DominoDict setVar)
        {
            string pv = ParamsAsString(setVar);

            Grid g = new() { Height = 18 };
            g.Children.Add(new TextBox() { Text = setVar.Name, FontWeight = FontWeight.Bold, Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });

            Button btn = new Button() { Tag = "connvar" + "|" + conn.ID + "|" + setVar.UniqueID };
            btn.Classes.Add("EditBtn");
            btn.Click += EditMetadataInfo;
            g.Children.Add(btn);

            Button btn2 = new Button() { Tag = conn.ID + "|" + setVar.UniqueID };
            btn2.Classes.Add("EditBtn");
            btn2.Classes.Add("DelBtn");
            btn2.Click += RemoveConnVar;
            g.Children.Add(btn2);

            StackPanel sp2 = new();
            sp2.Children.Add(g);
            sp2.Children.Add(new TextBox() { Text = pv, Margin = new(10, 0, 0, 0), Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });

			Border b = null;
			CheckDictVarState(ref b, setVar, true);
			b.Child = sp2;

			return b;
        }

        private void DrawConnVariable(DominoConnector conn, DominoDict setVar)
		{
			var sp2 = DrawConnVariableChild(conn, setVar);
            Border b2 = new() { BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new(2, 2, 2, 2), Child = sp2, Height = 40 };
			setVar.ContainerUI = b2;

			int pos = 1;
			if (!conn.INT_isOut) pos++;

			conn.Widget.list.Children.Insert(pos, b2);
		}*/

		private void DrawGlobalVar(DominoDict var, bool updateUI = false)
		{
			string pv = ParamsAsString(var);

			/*if (var.Value.Length > 1)
				pv = "{" + string.Join(", ", var.Value) + "}";
			else
				pv = GetSetVarOutName(var.Value[0]);*/

			StackPanel sp2 = new();

			Grid g = new() { Height = 18 };
			g.Children.Add(new TextBox() { Text = var.Name, FontWeight = FontWeight.Bold });

			Button btn = new Button() { Tag = "globalvar|" + var.UniqueID };
            btn.Classes.Add("EditBtn");
            btn.Click += EditMetadataInfo;
			g.Children.Add(btn);

			Button btnDel = new Button() { Tag = "globalvar|" + var.UniqueID };
            btnDel.Classes.Add("EditBtn");
            btnDel.Classes.Add("DelBtn");
            btnDel.Click += RemoveMetadataInfo;
			g.Children.Add(btnDel);

			sp2.Children.Add(g);

			sp2.Children.Add(new TextBox() { Text = pv, Margin = new(10, 0, 0, 0) });

			if (updateUI)
			{
                var.ContainerUI.Child = sp2;
            }
			else
            {
				Border b2 = new() { BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new(2, 2, 2, 2), Child = sp2, CornerRadius = new(5), Background = Brushes.LightGray };
				var.ContainerUI = b2;
				wiGlobalVars.list.Children.Add(var.ContainerUI);
            }
		}

		private void RemoveBox(DominoBox box)
		{
			foreach (var c in dominoConnectors.Values)
			{
				var eb = c.ExecBoxes.Where(a => a.Box.ID == box.ID).ToList();
				if (eb != null)
				{
					foreach (var ebf in eb)
                    {
                        //c.Widget.list.Children.Remove(ebf.ContainerUI);
                        c.ExecBoxes.RemoveAll(a => a.Box.ID == box.ID);

                        RemoveLine(c.ID + "-OUT-" + ebf.Box.ID);
                    }

					NewDrawConnector(c, c.DrawX, c.DrawY, true);

                	AddConnectorLines(c, 1);
				}
			}

			void a(List<DominoConnector> c)
			{
				foreach (var cc in c)
				{
					cc.FromBoxConnectID = -1;
					cc.FromBoxConnectIDStr = "MISSING BOX";
					RemoveLine(cc.ID + "-IN");
				}

				foreach (var cc in c)
					a(cc.SubConnections);
			}

			a(box.Connections);

			canvas.Children.Remove(box.Widget);
			dominoBoxes.Remove(box.ID);
		}

		private void SwapBox(object sender, RoutedEventArgs e)
		{
			string tag = (string)(sender as Button).Tag;

			var b = dominoBoxes[tag];

			if (!regBoxesAll.ContainsKey(b.Name))
			{
				openInfoDialog("Swap box", "Can't find metadata of the box.");
				return;
			}

			var isStateless = regBoxesAll[b.Name].IsStateless;
			if (!isStateless && b.ID.StartsWith("self["))
			{
				openInfoDialog("Swap box", "Selected box is not stateless and must be defined as global.");
				return;
			}

			string num = b.ID.Replace("self[", "").Replace("]", "").Replace("en_", "");

			if (b.ID.StartsWith("self["))
				b.ID = "en_" + num;
			else if (b.ID.StartsWith("en_"))
				b.ID = "self[" + num + "]";

			b.Widget.Header.Text = b.ID + " - " + b.Name;
			b.Widget.ID = b.ID;
			b.Widget.delBtn.Tag = b.ID;
			b.Widget.swapBtn.Tag = b.ID;

			foreach (var btn in b.Widget.list.Children)
				if (btn is Button button)
				{
					button.Tag = b.ID;
				}

			dominoBoxes[b.ID] = b;
			dominoBoxes.Remove(tag);

			for (int i = 0; i < lines.Count; i++)
			{
				if (lines[i].Point1.StartsWith(tag + "-"))
					lines[i].Point1 = lines[i].Point1.Replace(tag, b.ID);

				if (lines[i].Point2.StartsWith(tag + "-"))
					lines[i].Point2 = lines[i].Point2.Replace(tag, b.ID);

                if (lines[i].Point1.EndsWith("-" + tag))
                    lines[i].Point1 = lines[i].Point1.Replace(tag, b.ID);

                if (lines[i].Point2.EndsWith("-" + tag))
                    lines[i].Point2 = lines[i].Point2.Replace(tag, b.ID);
            }

			foreach (var c in dominoConnectors.Values)
			{
				/*var eb = c.ExecBoxes.Where(a => a.Box.ID == b.ID).SingleOrDefault();
				if (eb != null)
				{
					c.Widget.list.Children.Remove(eb.ContainerUI);
					DrawExecBoxContainerUI(c, eb, eb.INT_clr);
				}

				bool a(DominoDict v)
				{
					bool changed = false;

					if (v.Value != null)
						if (v.Value.Contains(tag))
						{
							v.Value = v.Value.Replace(tag, b.ID);
							changed = true;
						}

					foreach (var sv in v.ValueArray)
						if (a(sv))
							changed = true;

					return changed;
				}

				foreach (var v in c.SetVariables)
					if (a(v))
					{
                        v.ContainerUI.Child = DrawConnVariableChild(c, v);
					}

				foreach (var ee in c.ExecBoxes)
				{
					foreach (var p in ee.Params)
						if (a(p))
						{
							c.Widget.list.Children.Remove(ee.ContainerUI);
							DrawExecBoxContainerUI(c, ee, ee.INT_clr);
						}
				}*/

				NewDrawConnector(c, c.DrawX, c.DrawY, true);
			}

            WasEdited();
        }

		private void DrawComment(DominoComment c, double currX, double currY)
		{
			Grid g = new();
			g.Children.Add(new TextBlock() { Text = c.Name, Foreground = new SolidColorBrush(Colors.White), Margin = new Thickness(0, 0, 50, 0) });

			Button btn = new Button() { Tag = "edit|" + c.UniqueID };
            btn.Classes.Add("EditBtn");
            btn.Classes.Add("EditBtnWhite");
            btn.Click += EditCommentDialog;
			g.Children.Add(btn);

			Button btnDel = new Button() { Tag = "delete|" + c.UniqueID };
            btnDel.Classes.Add("EditBtn");
            btnDel.Classes.Add("DelBtn");
            btnDel.Classes.Add("DelBtnWhite");
            btnDel.Click += EditCommentDialog;
			g.Children.Add(btnDel);

			DominoUIComment b2 = new() { BorderBrush = new SolidColorBrush(linesColors[c.Color]), Background = new SolidColorBrush(Color.FromArgb(150, 150, 150, 150)), Padding = new Thickness(10, 5, 5, 5), BorderThickness = new(2, 2, 2, 2), Child = g, CornerRadius = new(5) };
			b2.ID = c.UniqueID;
			c.ContainerUI = b2;

			canvas.Children.Add(b2);
			Canvas.SetLeft(b2, currX);
			Canvas.SetTop(b2, currY);
			//Panel.SetZIndex(b2, 40);
			b2.ZIndex = 40;

			canvas.RefreshChild(b2);
		}

		private void DrawBorder(DominoBorder b, double currX, double currY, double w, double h, bool? moveChilds)
		{
			AvaloniaList<double> lineStyle = null;

			if (b.Style == 0) lineStyle = new(new double[] { 1, 1 });           // . . . . dotted line
			if (b.Style == 1) lineStyle = new(new double[] { 4, 1, 2, 1 });     // - . - . dash-dotted line
			if (b.Style == 2) lineStyle = new(new double[] { 4, 4 });           // --  --  dashed line
			if (b.Style == 3) lineStyle = new(new double[] { 1, 0 });           // ------- solid line

			Grid g = new();

			Button btn = new Button() { Tag = "edit|" + b.UniqueID, VerticalAlignment = VerticalAlignment.Top, Margin = new(0, 4, 4, 0) };
            btn.Classes.Add("EditBtn");
            btn.Classes.Add("EditBtnWhite");
            btn.Click += EditBorderDialog;
			g.Children.Add(btn);

			Button btnDel = new Button() { Tag = "delete|" + b.UniqueID, VerticalAlignment = VerticalAlignment.Top, Margin = new(0, 4, 24, 0) };
            btnDel.Classes.Add("EditBtn");
            btnDel.Classes.Add("DelBtn");
            btnDel.Classes.Add("DelBtnWhite");
            btnDel.Click += EditBorderDialog;
			g.Children.Add(btnDel);

			/*Button btnDup = new Button() { Tag = b.UniqueID, Style = (Application.Current.FindResource("DuplBtnWhite") as Style), VerticalAlignment = VerticalAlignment.Top, Margin = new(0, 4, 44, 0) };
			btnDup.Click += DuplicateBorder;
			g.Children.Add(btnDup);*/

			Avalonia.Controls.Shapes.Rectangle r = new();
			r.StrokeDashArray = lineStyle;
			r.Stroke = new SolidColorBrush(linesColors[b.Color]);
			r.StrokeThickness = 2;
            r.RadiusX = 5;
            r.RadiusY = 5;
			g.Children.Add(r);

			SolidColorBrush clr = new SolidColorBrush(Colors.Transparent);
            if (b.BackgroundColor != -1)
            {
                clr = new SolidColorBrush(linesColors[b.BackgroundColor]);
                clr.Color = Color.FromArgb(50, clr.Color.R, clr.Color.G, clr.Color.B);
            }

			DominoUIBorder b2 = new()
			{
				//BorderBrush = new SolidColorBrush(linesColors[b.Color]),
				Background = clr,
                BackgroundColor = clr,
                //BorderThickness = new(2, 2, 2, 2),
                Height = h,
				Width = w,
				EnableMove = true,
				Child = g,
				EnableMovingChilds = moveChilds == true,
				ID = b.UniqueID
			};
			b.ContainerUI = b2;

			canvas.Children.Add(b2);
			Canvas.SetLeft(b2, currX);
			Canvas.SetTop(b2, currY);
			//Panel.SetZIndex(b2, 10);
			b2.ZIndex = 10;

			canvas.RefreshChild(b2);
		}

		private void DrawResource(DominoDict res)
		{
			StackPanel sp2 = new();

			Grid g2 = new() { Height = 18 };

			g2.Children.Add(new TextBox() { Text = res.Name, FontWeight = FontWeight.Bold });

			Button btn = new Button() { Tag = "edit|" + res.UniqueID };
            btn.Classes.Add("EditBtn");
            btn.Click += EditResourceDialog;
			g2.Children.Add(btn);

			Button btnDel = new Button() { Tag = "delete|" + res.UniqueID };
            btnDel.Classes.Add("EditBtn");
            btnDel.Classes.Add("DelBtn");
            btnDel.Click += EditResourceDialog;
			g2.Children.Add(btnDel);

			sp2.Children.Add(g2);

			sp2.Children.Add(new TextBox() { Text = res.Value, Margin = new(10, 0, 0, 0) });

			Border b2 = new() { BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new(2, 2, 2, 2), Child = sp2, CornerRadius = new(5), Background = Brushes.LightGray };
			res.ContainerUI = b2;
			wiResources.list.Children.Add(b2);
		}

		private void DrawLinePoint(LinesVal line, double pntX, double pntY, double drawX, double drawY, bool insert = false)
		{
            var c = canvas.Transform4(new(-7.5, -7.5));

            var ui = new DominoUILinePoint()
            {
                Point = new(pntX, pntY),
                Background = line.UI.Stroke,
                Height = 15,
                Width = 15
			};

            if (insert)
            {
                int nIdx = line.UI.Points.Count;

                for (int i = 0; i < line.UI.Points.Count; i++)
                {
                    Point p1 = i == 0 ? new Point(line.UI.X1, line.UI.Y1) : line.UI.Points[i - 1].Point;
                    Point p2 = line.UI.Points[i].Point;
                    Point pn = new(pntX, pntY);

                    double angleEx = Math.Atan2(p2.X - p1.X, p2.Y - p1.Y) * (180 / Math.PI);
                    double angleNe = Math.Atan2(pn.X - p1.X, pn.Y - p1.Y) * (180 / Math.PI);

                    double distanceEx = Vector.Subtract(p2, p1).Length;
                    double distanceNe = Vector.Subtract(pn, p1).Length;

                    if (
                        (angleEx < angleNe + 1 && angleEx > angleNe - 1) &&
                        (distanceEx > distanceNe)
                        )
                    {
                        nIdx = i;
                    }
                }

                line.UI.Points.Insert(nIdx, ui);
            }
			else
				line.UI.Points.Add(ui);

            line.UI.InvalidateVisual();

            canvas.Children.Add(ui);
            Canvas.SetLeft(ui, drawX + c.X);
            Canvas.SetTop(ui, drawY + c.Y);
			//Panel.SetZIndex(ui, 50);
			ui.ZIndex = 50;
        }

		private void RemoveComment(DominoComment c)
		{
			canvas.Children.Remove(c.ContainerUI);
			dominoComments.Remove(c.UniqueID);
		}

		private void RemoveBorder(DominoBorder b)
		{
			canvas.Children.Remove(b.ContainerUI);
			dominoBorders.Remove(b.UniqueID);
		}

		private void RemoveLinePoint(DominoUILinePoint lp)
		{
            canvas.Children.Remove(lp);

            foreach (var line in lines)
            {
				if (line.UI.Points != null)
                {
                    int rem = line.UI.Points.RemoveAll(a => a.ID == lp.ID);
					if (rem > 0)
					{
                        line.UI.InvalidateVisual();
                    }
                }
            }
		}


        private void AddControlInLines(int draw)
        {
            double posYCo = 32;

            foreach (var inCtrl in dominoGraphs[selGraph].Metadata.ControlsIn)
            {
				var conn = dominoConnectors.Values.Where(a => a.ID == inCtrl.Name).Single();

                if (conn.Widget != null)
                {
                    var a = canvas.Transform2(new(Canvas.GetLeft(wiMetaControlIn), Canvas.GetTop(wiMetaControlIn)));
                    var b = canvas.Transform2(new(Canvas.GetLeft(conn.Widget), Canvas.GetTop(conn.Widget)));

					string p1 = "MetadataControlIn-OUT-" + conn.ID;
					string p2 = conn.ID + "-IN";

                    if (draw == 0)
                    {
						if (!lines.Any(x => x.Point1 == p1 && x.Point2 == p2))
							DrawLine(
								0,
								0,
								0,
								0,
								p1,
								p2,
								-1
							);
					}

                    {
                        foreach (var line in lines)
                        {
                            if (line.Point1 == p1 && line.Point2 == p2)
                            {
                                line.UI.X1 = a.X + width;
                                line.UI.Y1 = a.Y + posYCo + 11;
                                line.UI.X2 = b.X;
                                line.UI.Y2 = b.Y + 17;
                            }
                        }
                    }

                    posYCo += 60;
                }
            }
        }

        private void AddControlOutLines(int draw)
        {
            double posYCo = 32;

            foreach (var outCtrl in dominoGraphs[selGraph].Metadata.ControlsOut)
            {
				var conns = dominoConnectors.Values.Where(a => a.OutFuncName.Contains(outCtrl.Name));

				if (conns != null)
                {
					foreach (var conn in conns)
                    {
                        double posYCoC = 52;

                        for (int aaa = 0; aaa < conn.SetVariables.Count; aaa++)
                            posYCoC += 40;

                        if (conn.Widget != null)
                        {
                            var a = canvas.Transform2(new(Canvas.GetLeft(wiMetaControlOut), Canvas.GetTop(wiMetaControlOut)));
                            var b = canvas.Transform2(new(Canvas.GetLeft(conn.Widget), Canvas.GetTop(conn.Widget)));

                            string p1 = conn.ID + "-OUT-" + outCtrl.Name;
                            string p2 = "MetadataControlOut-IN-" + conn.ID;

                            if (draw == 0)
                            {
                                if (!lines.Any(x => x.Point1 == p1 && x.Point2 == p2))
                                    DrawLine(
                                        0,
                                        0,
                                        0,
                                        0,
                                        p1,
                                        p2,
                                        -1
                                    );
                            }

                            {
                                foreach (var line in lines)
                                {
                                    if (line.Point1 == p1 && line.Point2 == p2)
                                    {
                                        line.UI.X1 = b.X + width;
                                        line.UI.Y1 = b.Y + posYCoC + 11;
                                        line.UI.X2 = a.X;
                                        line.UI.Y2 = a.Y + posYCo + 11;
                                    }
                                }
                            }
                        }
                    }

                    posYCo += 60;
                }
            }
        }

        private void AddConnectorLines(DominoConnector conn, int draw)
        {
            double posYCo = 32;

            if (!conn.INT_isIn)
				posYCo += 20;

            if (!conn.INT_isOut)
                posYCo += 20;

            for (int aaa = 0; aaa < conn.SetVariables.Count; aaa++)
                posYCo += 40;

            foreach (var execBox in conn.ExecBoxes)
            {
                if (conn.Widget != null)
                {
                    var a = canvas.Transform2(new(Canvas.GetLeft(execBox.Box.Widget), Canvas.GetTop(execBox.Box.Widget)));
                    var b = canvas.Transform2(new(Canvas.GetLeft(conn.Widget), Canvas.GetTop(conn.Widget)));

					string p1 = conn.ID + "-OUT-" + execBox.Box.ID;
					string p2 = execBox.Box.ID + "-IN";

					if (draw == 0 || draw == 2)
                    {
						if (!lines.Any(x => x.Point1 == p1 && x.Point2 == p2))
							DrawLine(
								b.X + width,
								b.Y + posYCo + 11,
								a.X,
								a.Y + 17,
								p1,
								p2,
								execBox.INT_clr
							);
                    }
					
					if (draw == 1 || draw == 2)
                    {
                        foreach (var line in lines)
                        {
                            if (line.Point1 == p1 && line.Point2 == p2)
                            {
                                line.UI.X1 = b.X + width;
                                line.UI.Y1 = b.Y + posYCo + 11;
								line.UI.Stroke = new SolidColorBrush(execBox.INT_clr == -1 ? Color.FromArgb(150, 150, 150, 150) : linesColors[execBox.INT_clr]);
                            }
                        }
                    }

                    posYCo += 22;
                    for (int bbb = 0; bbb < execBox.Params.Count; bbb++)
                        posYCo += 30;
                }
            }

            if (draw == 1 || draw == 2)
            {
                var b = canvas.Transform2(new(Canvas.GetLeft(conn.Widget), Canvas.GetTop(conn.Widget)));

                foreach (var line in lines)
                {
                    if (line.Point2 == conn.ID + "-IN")
                    {
                        line.UI.X2 = b.X;
                        line.UI.Y2 = b.Y + 17;
                    }
                }
            }
        }

        private void AddBoxLines(DominoBox box, int draw)
        {
            double posYCo = 52;

			if (box.INT_open)
				posYCo += 20;

            foreach (var conn in box.Connections)
            {
                if (box.Widget != null)
                {
					void aa(DominoConnector c)
                    {
						if (c.Widget != null)
                        {
                            var a = canvas.Transform2(new(Canvas.GetLeft(c.Widget), Canvas.GetTop(c.Widget)));
                            var b = canvas.Transform2(new(Canvas.GetLeft(box.Widget), Canvas.GetTop(box.Widget)));

                            string p1 = box.ID + "-OUT" + "-" + c.ID;
                            string p2 = c.ID + "-IN";

							if (draw == 0 || draw == 2)
                            {
								if (!lines.Any(x => x.Point1 == p1 && x.Point2 == p2))
									DrawLine(
										b.X + width,
										b.Y + posYCo + 11,
										a.X,
										a.Y + 17,
										p1,
										p2,
										c.INT_clr
									);
                            }
							
							if (draw == 1 || draw == 2)
                            {
                                foreach (var line in lines)
                                {
                                    if (line.Point1 == p1 && line.Point2 == p2)
                                    {
                                        line.UI.X1 = b.X + width;
                                        line.UI.Y1 = b.Y + posYCo + 11;
                                    }
                                }
                            }

                            posYCo += 22;
                        }

                        foreach (var subC in c.SubConnections)
                            aa(subC);
                    }

					aa(conn);
                }
            }

            if (draw == 1 || draw == 2)
            {
                var b = canvas.Transform2(new(Canvas.GetLeft(box.Widget), Canvas.GetTop(box.Widget)));

                foreach (var line in lines)
                {
                    if (line.Point2 == box.ID + "-IN")
                    {
                        line.UI.X2 = b.X;
                        line.UI.Y2 = b.Y + 17;
                        //line.UI.Margin = line.UI.Margin == new Thickness(0, 0, 0, 0) ? new Thickness(1, 1, 0, 0) : new Thickness(0, 0, 0, 0);
                    }
                }
            }
        }

        private readonly Color varNotDefClr = Color.Parse("#f44336");
        private readonly Color varIsDefClr = Color.Parse("#4caf50");
		private bool CheckDictVarState(ref Border borderInst, DominoDict dict, bool isVar, bool baseIt = true)
		{
			bool CheckVarState(ref Border borderInstS, string var, bool isSetter)
			{
				if (
					!globalVariables.Any(a => "self." + a.Name == var) &&
					!dominoGraphs[selGraph].Metadata.DatasIn.Any(a => "self." + a.Name == var) &&
					!dominoGraphs[selGraph].Metadata.DatasOut.Any(a => "self." + a.Name == var)
					)
				{
					borderInstS = new()
					{
						Background = new SolidColorBrush(isSetter ? GetLight(varNotDefClr, 0.8) : varNotDefClr),
						CornerRadius = new(5)
					};
					ToolTip.SetTip(borderInstS, "Variable \"" + var + "\" is NOT defined in Global variables or DatasIn or DatasOut.");
					return true;
				}
				else
				{
					borderInstS ??= new()
					{
						Background = new SolidColorBrush(isSetter ? GetLight(varIsDefClr, 0.8) : varIsDefClr),
						CornerRadius = new(5)
					};
					ToolTip.SetTip(borderInstS, "Variable \"" + var + "\" IS defined in Global variables or DatasIn or DatasOut.");
				}

				return false;
			}

			if (dict.Name != null && dict.Name.StartsWith("self."))
			{
				if (CheckVarState(ref borderInst, dict.Name, true))
					return true;
			}

			if (dict.Value != null && dict.Value.StartsWith("self."))
			{
				if (CheckVarState(ref borderInst, dict.Value, false))
					return true;
			}

			foreach (var si in dict.ValueArray)
				if (CheckDictVarState(ref borderInst, si, isVar, false))
					return true;

			if (baseIt && borderInst == null)
			{
				borderInst = new();
				if (isVar)
				{
					borderInst.Background = Brushes.LightGray;
                    borderInst.CornerRadius = new(5);
                }
            }
				
			return false;
		}

		private void RefreshConnectorsVariables(string oldVarName, string newVarName)
		{
			foreach (var c in dominoConnectors.Values)
			{
				bool found = false;

				foreach (var v in c.SetVariables)
					if (v.Name == "self." + oldVarName || v.Name == "self." + newVarName)
						found = true;
                        //v.ContainerUI.Child = DrawConnVariableChild(c, v);

				foreach (var eb in c.ExecBoxes)
					foreach (var p in eb.Params)
					{
						void f(DominoDict dominoDict)
						{
							if (dominoDict.Name == "self." + oldVarName || dominoDict.Name == "self." + newVarName)
								found = true;

							if (dominoDict.Value != null)
								if (dominoDict.Value == "self." + oldVarName || dominoDict.Value == "self." + newVarName)
									found = true;

							foreach (var si in dominoDict.ValueArray)
								f(si);
						}
						f(p);

						/*if (found)
						{
							eb.MainUI.Children.Clear();
							DrawExecBoxChildren(c, eb, eb.MainUI);
						}*/
					}

				if (found)
					NewDrawConnector(c, c.DrawX, c.DrawY, true);
			}
		}




		private void NewDrawBox(DominoBox box, double currX, double currYBox, bool renew = false)
        {
            if (box.Widget != null)
            {
            	if (renew)
            	{
            	    currX = Canvas.GetLeft(box.Widget);
            	    currYBox = Canvas.GetTop(box.Widget);
            	}

                canvas.Children.Remove(box.Widget);
            }

            var wb = new DominoUIBox();
			wb.Header.Text = box.ID + " - " + box.Name;
			wb.Header.Margin = new(0, 0, 40, 0);
			//wb.Height = 30;
			wb.Width = width;
			wb.HeaderRectangle.Fill = Brushes.White;
			wb.ID = box.ID;
			canvas.Children.Add(wb);

			wb.delBtn.Tag = box.ID;
			wb.delBtn.Click += (sender, e) => {
				AskDialog("Remove box", "Do you want to remove the box?", () =>
				{
					string tag = (string)(sender as Button).Tag;
					var b = dominoBoxes[tag];
					RemoveBox(b);
				});
			};

			wb.swapBtn.IsVisible = true;
			wb.swapBtn.Tag = box.ID;
			wb.swapBtn.Click += SwapBox;

			Canvas.SetLeft(wb, currX);
			Canvas.SetTop(wb, currYBox);
			//Panel.SetZIndex(wb, 30);
			wb.ZIndex = 30;
			box.DrawX = currX;
			box.DrawY = currYBox;
			box.Widget = wb;
			box.Height = 40;

			if (regBoxesAll.ContainsKey(box.Name) && !regBoxesAll[box.Name].IsSystem && !regBoxesAll[box.Name].INT_Graph)
			{
				wb.list.Children.Add(DrawBtn("Open in new window >>>", box.Name, OnOpenClick));
				box.Height += 20;
				box.INT_open = true;
			}

			wb.list.Children.Add(DrawBtn("Add new output connector", box.ID, AddBoxConnector));
		
			void drawBoxConnectors(DominoBox box, DominoConnector c, string parentName = "", int selClr = -1)
			{
				string name = parentName + "(" + c.FromBoxConnectID.ToString() + ") " + c.FromBoxConnectIDStr;
				int colorConnSel = selClr >= 0 ? selClr : box.Widget.list.Children.Count - 1; // r.Next(0, 16);

				//colorConnSel = r.Next(0, linesColors.Count);

				if (c.ID != null && c.ID != "")
				{
					c.INT_clr = colorConnSel;

					StackPanel sp2 = new();

					Grid g = new() { Height = 18 };
					g.Children.Add(new TextBox() { Text = name + " = " + c.ID, Margin = new(0, 0, 20, 0), FontWeight = FontWeight.Bold, Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });

					Button btnDel = new Button() { Tag = box.ID + "|" + c.ID, Margin = new(0) };
        	        btnDel.Classes.Add("EditBtn");
        	        btnDel.Classes.Add("DelBtn");
        	        btnDel.Click += RemoveBoxConn;
					g.Children.Add(btnDel);

					sp2.Children.Add(g);

					Border b2 = new() { BorderBrush = new SolidColorBrush(linesColors[colorConnSel]), BorderThickness = new(2, 2, 2, 2), Child = sp2, CornerRadius = new(5), Background = (bool)settings["coloredBoxes"] ? new SolidColorBrush(GetLight(linesColors[colorConnSel])) : Brushes.LightGray };
					box.Widget.list.Children.Add(b2);

					//c.ContainerUI = b2;
				}

				foreach (var sc in c.SubConnections)
				{
					drawBoxConnectors(box, sc, name + " > ");
				}
			}
		
			foreach (var c in box.Connections)
			{
				drawBoxConnectors(box, c);
            }

            if (renew)
            {
                canvas.RefreshChild(box.Widget);
            }
        }

		private void NewDrawConnector(DominoConnector conn, double currX, double currY, bool renew = false)
		{
            if (conn.Widget != null)
            {
				if (renew)
				{
					currX = Canvas.GetLeft(conn.Widget);
					currY = Canvas.GetTop(conn.Widget);
            	}

                canvas.Children.Remove(conn.Widget);
            }

            conn.INT_isIn = dominoGraphs[selGraph].Metadata.ControlsIn.Where(a => a.Name == conn.ID).Any();
			conn.INT_isOut = dominoGraphs[selGraph].Metadata.ControlsOut.Where(a => conn.OutFuncName.Contains(a.Name)).Any();

			/*var brsh = Brushes.Yellow;
			if (conn.INT_isIn) brsh = Brushes.Red;
			if (conn.INT_isOut) brsh = Brushes.Orange;*/

			var brsh = new SolidColorBrush(Colors.Yellow).ToImmutable();

			var w = new DominoUIConnector();
			w.Header.Text = (conn.INT_isIn ? "ControlIn - " : "") + conn.ID;
			//w.Height = 30;
			w.Width = width;
			w.HeaderRectangle.Fill = brsh;
			w.ID = conn.ID;
			canvas.Children.Add(w);

			w.delBtn.Tag = conn.ID;
			w.delBtn.Click += (sender, e) => {

				AskDialog("Remove connector", "Do you want to remove the connector?", () =>
				{
					string tag = (string)(sender as Button).Tag;
					var conn = dominoConnectors[tag];
					RemoveConector(conn);
				});
			};

			w.editBtn.IsVisible = conn.INT_isIn || conn.INT_isOut ? false : true;
			w.editBtn.Tag = conn.ID;
			w.editBtn.Click += EditConnectorDialog;

			Canvas.SetLeft(w, currX);
			Canvas.SetTop(w, currY);
			//Panel.SetZIndex(w, 30);
			w.ZIndex = 30;
			conn.DrawX = currX;
			conn.DrawY = currY;
			conn.Widget = w;
			conn.Height = 32;

			if (!conn.INT_isOut)
			{
				w.list.Children.Add(DrawBtn("Add new exec box", conn.ID, AddExecBox));
                conn.Height += 20;
            }

			if (!conn.INT_isIn)
			{
				w.list.Children.Add(DrawBtn("Add new set variable", conn.ID, AddConnVar));
                conn.Height += 20;
            }

			foreach (var setVar in conn.SetVariables)
			{
            	string pv = ParamsAsString(setVar);

            	Grid g = new() { Height = 18 };
            	g.Children.Add(new TextBox() { Text = setVar.Name, FontWeight = FontWeight.Bold, Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });

            	Button btn = new Button() { Tag = "connvar" + "|" + conn.ID + "|" + setVar.UniqueID };
            	btn.Classes.Add("EditBtn");
            	btn.Click += EditMetadataInfo;
            	g.Children.Add(btn);

            	Button btn2 = new Button() { Tag = conn.ID + "|" + setVar.UniqueID };
            	btn2.Classes.Add("EditBtn");
            	btn2.Classes.Add("DelBtn");
            	btn2.Click += RemoveConnVar;
            	g.Children.Add(btn2);

            	StackPanel sp2 = new();
            	sp2.Children.Add(g);
            	sp2.Children.Add(new TextBox() { Text = pv, Margin = new(10, 0, 0, 0), Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });

				Border b = null;
				CheckDictVarState(ref b, setVar, true);
				b.Child = sp2;
	
				Border b2 = new() { BorderBrush = new SolidColorBrush(Colors.Black), BorderThickness = new(2, 2, 2, 2), Child = b, Height = 40 };
				setVar.ContainerUI = b2;

				int pos = 1;
				if (!conn.INT_isOut) pos++;

				w.list.Children.Insert(pos, b2);
				conn.Height += 40;
            }

			foreach (string outFunc in conn.OutFuncName)
			{
				StackPanel sp2 = new();

				Grid g = new() { Height = 30 };
				g.Children.Add(new TextBox() { Text = "ControlOut", FontWeight = FontWeight.Bold, Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });
				g.Children.Add(new TextBox() { Text = outFunc, Margin = new(10, 13, 0, 0), Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });
				sp2.Children.Add(g);

				Border b2 = new() { BorderBrush = new SolidColorBrush(Colors.Orange), BorderThickness = new(2, 2, 2, 2), Child = sp2, CornerRadius = new(5), Background = (bool)settings["coloredBoxes"] ? new SolidColorBrush(GetLight(Colors.Orange)) : Brushes.LightGray };
				w.list.Children.Add(b2);

				conn.Height += 55;
            }

			int c = 0;
			foreach (var eb in conn.ExecBoxes)
			{
				eb.INT_clr = c % linesColors.Count;
				c++;

				StackPanel sp = new();

				string name = eb.ExecStr;
				//if (regBoxesAll.ContainsKey(execBox.Box.Name))
				//	name = execBox.Exec < regBoxesAll[execBox.Box.Name].ControlsIn.Count ? regBoxesAll[execBox.Box.Name].ControlsIn[execBox.Exec].Name : "EXEC DOESN'T EXIST";

				string execStr = "Exec: " + "(" + eb.Exec.ToString() + ") " + name;
				if (eb.Type == ExecType.ExecDynInt) execStr += ", DynInt: " + eb.DynIntExec.ToString();

				Grid gh = new() { Height = 18 };
				gh.Children.Add(new TextBox() { Text = execStr + " > " + eb.Box.ID, Margin = new(0, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeight.Black, Width = double.NaN });

				Button btn = new Button() { Tag = conn.ID + "|" + eb.Box.ID };
				btn.Classes.Add("EditBtn");
				btn.Click += EditExecBox;
				gh.Children.Add(btn);

				Button btn2 = new Button() { Tag = conn.ID + "|" + eb.Box.ID };
            	btn2.Classes.Add("EditBtn");
            	btn2.Classes.Add("DelBtn");
				btn2.Click += RemoveExecBox;
            	gh.Children.Add(btn2);

				sp.Children.Add(gh);

				conn.Height += 22;

            	if (eb.Params != null && eb.Params.Count > 0)
				{
					foreach (var param in eb.Params)
					{
						string pv = ParamsAsString(param);

						/*if (param.ValueArray.Count > 1)
							pv = "{" + string.Join(", ", param.ValueArra) + "}";
						else
							pv = GetSetVarOutName(param.Value);*/

						var paramName = "PARAM DOESN'T EXIST";
						var paramType = "";

						if (regBoxesAll.ContainsKey(eb.Box.Name))
						{
							if (int.Parse(param.Name) < regBoxesAll[eb.Box.Name].DatasIn.Count)
            	            {
            	                paramName = regBoxesAll[eb.Box.Name].DatasIn[int.Parse(param.Name)].Name;
            	                paramType = " (" + regBoxesAll[eb.Box.Name].DatasIn[int.Parse(param.Name)].DataTypeID + ")";
            	            }
            	        }

            	        Grid g = new() { Height = 30 };
						g.Children.Add(new TextBox() { Text = "(" + param.Name + ") " + (regBoxesAll.ContainsKey(eb.Box.Name) ? paramName : "") + paramType, Margin = new(0, 0, 0, 0), FontWeight = FontWeight.Bold, Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });
						g.Children.Add(new TextBox() { Text = pv, Margin = new(10, 13, 0, 0), Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Left });

						Border brd = null;
						CheckDictVarState(ref brd, param, false);
						brd.Child = g;
						sp.Children.Add(brd);

						conn.Height += 30;
            	    }
				}
				
				//eb.MainUI = sp;
				//eb.INT_clr = clr;
				Border b = new() { BorderBrush = new SolidColorBrush(linesColors[eb.INT_clr]), BorderThickness = new(2, 2, 2, 2), Child = sp, CornerRadius = new(5), Background = (bool)settings["coloredBoxes"] ? new SolidColorBrush(GetLight(linesColors[eb.INT_clr])) : Brushes.LightGray };
				w.list.Children.Add(b);
				//eb.ContainerUI = b;
			}

            if (renew)
            {
				canvas.RefreshChild(conn.Widget);
            }
        }













        public delegate void OpenAskDialog(string name, string desc);
		public OpenAskDialog openAskDialog;
		private Action dialogAskAction;
		private Action dialogAskActionCancel;
		private void AskDialog(string name, string desc, Action action, Action actionCancel = null)
		{
			dialogAskAction = action;
			dialogAskActionCancel = actionCancel;
			openAskDialog(name, desc);
		}
		public void AskDialogAct()
		{
			dialogAskAction();
		}
		public void AskDialogActCancel()
		{
			if (dialogAskActionCancel != null)
				dialogAskActionCancel();
		}

		public delegate void OpenInfoDialog(string name, string val);
		public OpenInfoDialog openInfoDialog;

		public void InfoDialogAct()
		{
		}

		private int FindBoxFreeID(int nextID = 0, List<DominoBox> secondList = null)
        {
			var tmpList = new List<DominoBox>();

			tmpList.AddRange(dominoBoxes.Values.ToList());

			if (secondList != null)
				tmpList.AddRange(secondList);

            var a = tmpList.Any(a => a.ID == "self[" + nextID.ToString() + "]" || a.ID == "en_" + nextID.ToString());
			if (a)
				return FindBoxFreeID(nextID + 1, secondList);

			return nextID;
        }

		private string FindConnectorFreeID(string boxOutName, List<DominoConnector> secondList = null, int inc = -1)
        {
			var tmpList = new List<DominoConnector>();

			tmpList.AddRange(dominoConnectors.Values.ToList());

			if (secondList != null)
				tmpList.AddRange(secondList);

            var a = inc >= 0 ? "_" + inc.ToString() : "";
            if (tmpList.Any(b => b.ID == "f_" + boxOutName + a))
                return FindConnectorFreeID(boxOutName, secondList, inc + 1);
            else
                return boxOutName + a;
        }

		private int FindDynIntFreeNum(string reqID, List<DominoConnector> secondList = null)
		{
			List<int> lst = new List<int>();

			foreach (var aa in dominoConnectors.Values)
				foreach (var bb in aa.ExecBoxes)
					if (bb.Type == ExecType.ExecDynInt && bb.Box.ID == reqID)
						lst.Add(bb.DynIntExec);

			if (secondList != null)
				foreach (var aa in secondList)
					foreach (var bb in aa.ExecBoxes)
						if (bb.Type == ExecType.ExecDynInt && bb.Box.ID == reqID)
							lst.Add(bb.DynIntExec);

			int find(int n = 0)
			{
				if (lst.Contains(n))
					return find(n + 1);
				return n;
			}

			return find();
        }

        public delegate void OpenEditExecBoxDialog(List<ParamEntry> wndData, List<ExecEntry> execs, int selType, int selExec, int selDynInt);
		public OpenEditExecBoxDialog openEditExecBoxDialog;
		DominoConnector connEdit = null;
		ExecBox execBoxEdit = null;
		List<DominoDict> paramsEdit = new();
		bool paramsEditSingle = false;

		private void EditExecBox(object sender, RoutedEventArgs e)
		{
			string tag = (string)(sender as Button).Tag;
			string[] ids = tag.Split('|');

			connEdit = dominoConnectors[ids[0]];
			execBoxEdit = connEdit.ExecBoxes.Where(e => e.Box.ID == ids[1]).Single();

			if (!regBoxesAll.ContainsKey(execBoxEdit.Box.Name))
			{
				openInfoDialog("Edit exec box", "Can't find metadata of the box.");
				return;
			}

			List<ExecEntry> execs = new();
			var b = regBoxesAll[execBoxEdit.Box.Name].ControlsIn;
			for (int i = 0; i < b.Count; i++)
			{
				execs.Add(new() { Name = "(" + i.ToString() + ") " + b[i].Name + (b[i].AnchorDynType > 0 ? " - DynInt" : ""), Num = i.ToString() });
			}

			paramsEdit = Helpers.CopyList(execBoxEdit.Params);
			paramsEditSingle = false;
			openEditExecBoxDialog(EditExecBoxUISetParams(), execs, (int)execBoxEdit.Type, execBoxEdit.Exec, execBoxEdit.DynIntExec);
		}

		private List<ParamEntry> EditExecBoxUISetParams()
		{
			List<ParamEntry> wndData = new();

			void aa(DominoDict prmVal, string uniqueIDParent, int arrayLeft = 0, string paramName = null, bool isBase = false, int baseID = -1)
			{
				if (prmVal != null && prmVal.ValueArray.Count == 0 && prmVal.Value != null)
				{
					wndData.Add(new()
					{
						ParamName = paramName ?? prmVal.Name,
						ParamNameRaw = prmVal.Name,
						UniqueID = prmVal.UniqueID,
						UniqueIDParent = uniqueIDParent,
						ParamValue = prmVal.Value ?? "",
						ParamUsed = true,
						ParamHasArray = false,
						ParamIsBase = isBase,
						AddArrayVs = true,
						AddVs = false,
						RemoveVs = isBase ? false : true,
						ArrayBulletVs = arrayLeft > 0 ? true : false,
						GetDataVs = true,
						NameMargin = new(arrayLeft, 0, 5, 0)
					});
				}
				else if (prmVal != null && prmVal.ValueArray.Count >= 0 && prmVal.Value == null)
				{
					wndData.Add(new()
					{
						ParamName = paramName ?? prmVal.Name,
						ParamNameRaw = prmVal.Name,
						UniqueID = prmVal.UniqueID,
						UniqueIDParent = uniqueIDParent,
						ParamValue = "Array",
						ParamUsed = true,
						ParamHasArray = true,
						ParamIsBase = isBase,
						AddArrayVs = true,
						AddVs = true,
						RemoveVs = isBase ? false : true,
						ArrayBulletVs = arrayLeft > 0 ? true : false,
						GetDataVs = false,
						NameMargin = new(arrayLeft, 0, 5, 0)
					});

					for (int i = 0; i < prmVal.ValueArray.Count; i++)
					{
						aa(prmVal.ValueArray[i], prmVal.UniqueID, arrayLeft + 10);
					}
				}
				else
					wndData.Add(new()
					{
						ParamName = paramName ?? prmVal.Name,
						ParamNameRaw = baseID != -1 ? baseID.ToString() : prmVal.Name,
						UniqueID = prmVal != null ? prmVal.UniqueID : Helpers.RandomString(),
						UniqueIDParent = uniqueIDParent,
						ParamValue = "",
						ParamUsed = false,
						ParamHasArray = false,
						ParamIsBase = isBase,
						AddArrayVs = false,
						AddVs = false,
						RemoveVs = isBase ? false : true,
						ArrayBulletVs = false,
						GetDataVs = true,
						NameMargin = new(0, 0, 0, 0)
					});
			}

			if (paramsEditSingle)
			{
				aa(paramsEdit[0], "", 0, null, true, 0);
			}
			else
			{
				var a = regBoxesAll[execBoxEdit.Box.Name].DatasIn;
				for (int i = 0; i < a.Count; i++)
				{
					string paramName = "(" + i.ToString() + ") " + a[i].Name + " (" + a[i].DataTypeID + ")";

					var prmVal = paramsEdit.Where(a => a.Name == i.ToString()).SingleOrDefault();

					/*if (prmVal == null)
					{
						prmVal = new();
						prmVal.Name = i.ToString();
					}*/

					//aa(prmVal, "", 0, paramName, true, i);

					aa(prmVal, "", 0, paramName, true, i);
				}
			}

			return wndData;
		}

		private void EditExecBoxUIGetParams(List<ParamEntry> paramsList)
		{
			DominoDict aa(ParamEntry param, string assign)
			{
				if (param.ParamUsed)
				{
					DominoDict p = new();
					p.UniqueID = param.UniqueID;

					if (param.ParamIsBase)
						p.Name = param.ParamNameRaw;
					else
						p.Name = param.ParamName.Replace(" ", "");

					if (!param.ParamHasArray)
						p.Value = param.ParamValue;
					else
					{
						var su = paramsList.Where(a => a.UniqueIDParent == param.UniqueID);

						foreach (var s in su)
						{
							var ax = aa(s, assign + "-" + param.ParamNameRaw);

							if (ax != null)
								p.ValueArray.Add(ax);
						}
					}

					return p;
				}
				else
					return null;
			}

			paramsEdit.Clear();

			var baseParams = paramsList.Where(a => a.ParamIsBase);
			foreach (var param in baseParams)
			{
				var ax = aa(param, "");

				if (ax != null)
					paramsEdit.Add(ax);
			}

			/*if (paramsEditSingle)
				paramsEdit.Add(aa(paramsList[0], ""));*/
		}

		public List<ParamEntry> EditExecBoxParamsAddRow(List<ParamEntry> paramsList, string uniqueID, bool makeArray)
		{
			EditExecBoxUIGetParams(paramsList);

			void a(List<DominoDict> aa)
			{
				foreach (var c in aa)
				{
					if (c.UniqueID == uniqueID)
					{
						//if (makeArray) c.Value = null;
						//c.ValueArray.Add(new() { Name = "New Param", Value = "A Value" });

						if (makeArray)
						{
							if (c.Value != null)
							{
								c.Value = null;
							}
							else if (c.Value == null)
							{
								c.Value = "";
								c.ValueArray.Clear();
							}
						}
						else
						{
							c.ValueArray.Add(new() { Name = "NewParam", Value = "\"A Value\"" });
						}
					}

					if (c.ValueArray.Count > 0)
						a(c.ValueArray);
				}
			}
			a(paramsEdit);

			return EditExecBoxUISetParams();
		}

		public List<ParamEntry> EditExecBoxParamsRemoveRow(string uniqueID)
		{
			void a(List<DominoDict> aa)
			{
				aa.RemoveAll(a => a.UniqueID == uniqueID);

				foreach (var c in aa)
					if (c.ValueArray.Count > 0)
						a(c.ValueArray);
			}
			a(paramsEdit);

			return EditExecBoxUISetParams();
		}

		public string EditExecBoxSave(List<ParamEntry> paramsList, int editExecBoxType, int editExecBoxExec, string editExecBoxDynInt)
        {
            if (editExecBoxExec == -1)
            {
                return "Please select exec.";
            }

            var tt = (ExecType)editExecBoxType;

			if (tt == ExecType.Exec)
			{
				var ctrlMeta = regBoxesAll[execBoxEdit.Box.Name].ControlsIn[editExecBoxExec].AnchorDynType > 0;
				if (ctrlMeta)
					return "Selected exec is dynamic, so you must select type 'Exec Dyn Int' and set Dyn Int value.";
			}
			if (tt == ExecType.ExecDynInt)
			{
				var ctrlMeta = regBoxesAll[execBoxEdit.Box.Name].ControlsIn[editExecBoxExec].AnchorDynType == 0;
				if (ctrlMeta)
					return "Selected exec is not dynamic, so you must select type 'Exec'.";
			}

			execBoxEdit.Type = tt;
			execBoxEdit.Exec = editExecBoxExec;
			execBoxEdit.DynIntExec = int.Parse(editExecBoxDynInt);
            execBoxEdit.ExecStr = regBoxesAll[execBoxEdit.Box.Name].ControlsIn[editExecBoxExec].Name;

            EditExecBoxUIGetParams(paramsList);
			execBoxEdit.Params = paramsEdit;

			//execBoxEdit.MainUI.Children.Clear();
			//DrawExecBoxChildren(connEdit, execBoxEdit, execBoxEdit.MainUI);

			foreach (var c in dominoConnectors.Values)
			{
				foreach (var ceb in c.ExecBoxes)
				{
					if (ceb.Box.ID == execBoxEdit.Box.ID)
					{
						ceb.Params = paramsEdit;
						//ceb.MainUI.Children.Clear();
						//DrawExecBoxChildren(c, ceb, ceb.MainUI);
						AddConnectorLines(c, 1);
					}
				}

				NewDrawConnector(c, c.DrawX, c.DrawY, true);
			}

            WasEdited();
            return "";
		}

		private void RemoveExecBox(object sender, RoutedEventArgs e)
		{
			AskDialog("Remove exec box", "Do you want to remove the exec box?", () =>
			{
				string tag = (string)(sender as Button).Tag;
				string[] ids = tag.Split('|');

				var c = dominoConnectors[ids[0]];
				var eb = c.ExecBoxes.Where(a => a.Box.ID == ids[1]).Single();
				//c.Widget.list.Children.Remove(eb.ContainerUI);
				c.ExecBoxes.RemoveAll(a => a.Box.ID == ids[1]);

				NewDrawConnector(c, c.DrawX, c.DrawY, true);

				RemoveLine(ids[0] + "-OUT-" + eb.Box.ID);

                AddConnectorLines(c, 1);

                WasEdited();
            });
		}

		public delegate void OpenAddExecBoxDialog(List<ExecEntry> boxes);
		public OpenAddExecBoxDialog openAddExecBoxDialog;

		private void AddExecBox(object sender, RoutedEventArgs e)
		{
			string tag = (string)(sender as Button).Tag;

			connEdit = dominoConnectors[tag];

			List<ExecEntry> boxes = new();
			foreach (var b in dominoBoxes)
			{
				if (!connEdit.ExecBoxes.Any(a => a.Box.ID == b.Value.ID))
					boxes.Add(new() { Name = b.Value.ID + " - " + b.Value.Name, Num = b.Value.ID });
			}

			boxes = boxes.OrderBy(a => a.Name).ToList();

			openAddExecBoxDialog(boxes);
		}

		public void AddExecBoxCreate(string selBox)
		{
			//canvas.ResetZoom();

			List<DominoDict> ep = new();
			foreach (var c in dominoConnectors.Values)
			{
				foreach (var ceb in c.ExecBoxes)
				{
					if (ceb.Box.ID == selBox)
					{
						ep = ceb.Params;
					}
				}
			}

			ExecBox eb = new();
			eb.Box = dominoBoxes.Values.Where(a => a.ID == selBox).Single();
			eb.Exec = 0;
			eb.DynIntExec = 0;
			eb.Params = ep;

			if (!regBoxesAll.ContainsKey(eb.Box.Name))
			{
				openInfoDialog("Add exec box", "Can't find metadata of the box.");
				return;
			}

			var ctrlMeta = regBoxesAll[eb.Box.Name].ControlsIn[0].AnchorDynType > 0;
			if (ctrlMeta)
			{
				eb.Type = ExecType.ExecDynInt;
				eb.DynIntExec = FindDynIntFreeNum(eb.Box.ID);
			}
			else
				eb.Type = ExecType.Exec;

            eb.ExecStr = regBoxesAll[eb.Box.Name].ControlsIn[0].Name;
            
			connEdit.ExecBoxes.Add(eb);

			var clr = connEdit.ExecBoxes.Count - 1;

			//DrawExecBoxContainerUI(connEdit, eb, clr);
			NewDrawConnector(connEdit, connEdit.DrawX, connEdit.DrawY, true);

			AddConnectorLines(connEdit, 2);

			//canvas.RefreshChilds();

            WasEdited();
        }

		private void RemoveBoxConnS(DominoBox box, List<DominoConnector> c, string connID)
		{
			var conn = c.Where(a => a.ID == connID).SingleOrDefault();
			if (conn != null)
			{
				conn.FromBoxConnectID = -1;
				conn.FromBoxConnectIDStr = "MISSING BOX";
				//box.Widget.list.Children.Remove(conn.ContainerUI);
				c.RemoveAll(a => a.ID == connID);

				NewDrawConnector(conn, conn.DrawX, conn.DrawY, true);

				RemoveLine(conn.ID + "-IN");
			}

			foreach (var cc in c)
				RemoveBoxConnS(box, cc.SubConnections, connID);

			c.RemoveAll(a => a.ID == null && !a.SubConnections.Any());

            AddBoxLines(box, 1);
        }

		private void RemoveBoxConn(object sender, RoutedEventArgs e)
		{
			AskDialog("Remove connection", "Do you want to remove the connection?", () =>
			{
				string tag = (string)(sender as Button).Tag;
				string[] ids = tag.Split('|');

				var box = dominoBoxes[ids[0]];
				RemoveBoxConnS(box, box.Connections, ids[1]);

                //RemoveLine(ids[0] + "-P2", ids[1] + "-P2");

                WasEdited();
            });
		}

		public delegate void OpenAddBoxConnectorDialog(List<ExecEntry> boxFuncs, List<ExecEntry> connectors);
		public OpenAddBoxConnectorDialog openAddBoxConnectorDialog;
		DominoBox boxEdit = null;

		private void AddBoxConnector(object sender, RoutedEventArgs e)
		{
			string tag = (string)(sender as Button).Tag;

			boxEdit = dominoBoxes[tag];

			List<ExecEntry> boxFuncs = new();

			void aa(DominoConnector conn, int num, string name, string parentName, string prntUniqID = "")
			{
				string n = parentName + "(" + num.ToString() + ") " + name;

				DominoConnector bc = conn;

				if (bc == null)
					bc = boxEdit.Connections.Where(b => b.FromBoxConnectID == num).SingleOrDefault();

				if (bc != null)
				{
					if (bc.SubConnections.Any())
						n += " (Array)";

					if (bc.SubConnections.Any())
						boxFuncs.Add(new() { Name = n, Num = bc.UniqueID });

					foreach (var sb in bc.SubConnections)
						aa(sb, sb.FromBoxConnectID, sb.FromBoxConnectIDStr, n + " > ");
				}
				else
					boxFuncs.Add(new() { Name = n, Num = prntUniqID });
			}

			if (!regBoxesAll.ContainsKey(boxEdit.Name))
			{
				openInfoDialog("Add connector", "Can't find metadata of the box.");
				return;
			}

			var a = regBoxesAll[boxEdit.Name].ControlsOut;
			for (int i = 0; i < a.Count; i++)
			{
				aa(null, i, a[i].Name, "", a[i].UniqueID);
			}

			List<ExecEntry> connectors = new();
			connectors.Add(new() { Name = "<<<Create new connector>>>" });
			foreach (var b in dominoConnectors.Where(a => a.Value.FromBoxConnectID == -1))
			{
				connectors.Add(new() { Name = b.Value.ID });
			}

			openAddBoxConnectorDialog(boxFuncs, connectors);
		}

		public string AddBoxConnectorCreate(string selBoxFnc, string selConn, bool? addAsArray, string arrayKey)
		{
			//canvas.ResetZoom();

			string acname = "";
			DominoConnector connSel(string name)
			{
				if (selConn == "<<<Create new connector>>>")
				{
					name = boxEdit.ID.Replace("self[", "").Replace("]", "").Replace("en_", "") + name;
					name = FindConnectorFreeID(name);

					AddConnectorCreate(500, 500, name, false, false, "");
					return dominoConnectors["f_" + name];
				}
				else
					return dominoConnectors[selConn];
			}

			bool added = false;

			DominoConnector add(int index, string name, string parentName = "")
			{
				DominoConnector o = null;

				if (addAsArray == true)
				{
					o = new();
					o.ID = null;
					o.FromBoxConnectID = index;
					o.FromBoxConnectIDStr = name;

					DominoConnector c = connSel(acname + "_" + name + (arrayKey != null && arrayKey != "" ? "_" : "") + arrayKey);
					c.FromBoxConnectID = 0;
					c.FromBoxConnectIDStr = arrayKey;
					o.SubConnections.Add(c);

					//DrawBoxConnectors(boxEdit, o, parentName);
					added = true;
				}
				else
				{
					var a = name == "" ? arrayKey : name;
					o = connSel(acname + (a != null && a != "" ? "_" : "") + a);
					o.FromBoxConnectID = index;
					o.FromBoxConnectIDStr = a;

					//DrawBoxConnectors(boxEdit, o, parentName);
                    added = true;
				}

				return o;
			}

			void subs(List<DominoConnector> list, string p = "")
			{
				foreach (var ss in list)
				{
					string n = "(" + ss.FromBoxConnectID.ToString() + ") " + ss.FromBoxConnectIDStr + " > ";

					if (ss.UniqueID == selBoxFnc)
					{
						acname += "_" + ss.FromBoxConnectIDStr;
						ss.SubConnections.Add(add(getFreeNum(ss.SubConnections), "", p + n));
						return;
					}

					subs(ss.SubConnections, n);
				}
			}

			int getFreeNum(List<DominoConnector> list)
			{
				for (int i = 0; i < list.Count; i++)
				{
					var a = list.Any(a => a.FromBoxConnectID == i);
					if (!a)
						return i;
				}
				return list.Count;
			}

			var a = regBoxesAll[boxEdit.Name].ControlsOut;
			for (int i = 0; i < a.Count; i++)
			{
				var ec = boxEdit.Connections.Where(a => a.UniqueID == selBoxFnc && a.FromBoxConnectID == i).SingleOrDefault();
				if (a[i].UniqueID == selBoxFnc && ec == null)
				{
					if (a[i].AnchorDynType > 0 && addAsArray != true)
					{
						return "Selected output is dynamic, so you must select \"Add as array value\".";
					}

					boxEdit.Connections.Add(add(i, a[i].Name)); //add new root
				}
				if (ec != null)
				{
					acname += "_" + ec.FromBoxConnectIDStr;
					ec.SubConnections.Add(add(getFreeNum(ec.SubConnections), "", "(" + ec.FromBoxConnectID.ToString() + ") " + ec.FromBoxConnectIDStr + " > ")); //add to existing root array
				}
			}

			if (!added)
				subs(boxEdit.Connections); //add to any subconnections

			if (added)
                NewDrawBox(boxEdit, boxEdit.DrawX, boxEdit.DrawY, true);

            AddBoxLines(boxEdit, 2);

            //canvas.RefreshChilds();

            WasEdited();

			return "";
        }


		public string AddConnectorCreate(int widthA, int height, string name, bool? isIn, bool? isOut, string outName)
		{
			name = name.Replace(" ", "");
			outName = outName.Replace(" ", "");

			if (name == "")
				return "You must set a name.";

			if (isIn == false)
				name = "f_" + name;

			if (isOut == true)
				isIn = false;

			if (isOut == true && outName == "")
				return "Control out name is empty.";

			var e = dominoConnectors.Any(a => a.Value.ID == name);
			if (e)
				return "A connector with this name already exists. Select another name.";

			//canvas.ResetZoom();

			DominoConnector c = new();
			c.ID = name;
			c.FromBoxConnectID = isIn == true ? 0 : -1;
			c.FromBoxConnectIDStr = "MISSING BOX";
			dominoConnectors.Add(c.ID, c);

			if (isIn == true)
			{
				var m = new DominoBoxMetadataControlsIn(name, 0, "");
				dominoGraphs[selGraph].Metadata.ControlsIn.Add(m);
				DrawMetaControlIn(m);
			}
			if (isOut == true)
			{
				var m = new DominoBoxMetadataControlsOut(outName, 0, false);
				dominoGraphs[selGraph].Metadata.ControlsOut.Add(m);
				DrawMetaControlOut(m);
				c.OutFuncName.Add(outName);
			}

			Point pnt = new(widthA / 2, height / 2);

			var pntc = canvas.Transform3(pnt);
			//DrawConnector(c, (int)pntc.X, (int)pntc.Y);
			NewDrawConnector(c, (int)pntc.X, (int)pntc.Y, true);

			if (isIn == true)
				AddControlInLines(0);

			if (isOut == true)
				AddControlOutLines(0);

			//canvas.RefreshChilds();

            WasEdited();

            return "";
		}

		public delegate void OpenEditConnectorDialog(string name);
		public OpenEditConnectorDialog openEditConnectorDialog;
		DominoConnector editConnector = null;

		private void EditConnectorDialog(object sender, RoutedEventArgs e)
		{
			string tag = (string)(sender as Button).Tag;

			editConnector = dominoConnectors[tag];

			openEditConnectorDialog(editConnector.ID[2..]);
		}

		public string EditConnectorDialogAct(string name)
		{
			name = "f_" + name.Replace(" ", "");

			var e = dominoConnectors.Any(a => a.Value.ID == name);
			if (e)
				return "A connector with this name already exists. Select another name.";

			dominoConnectors.Remove(editConnector.ID);
			dominoConnectors.Add(name, editConnector);

            bool findBox(List<DominoConnector> c)
			{
				var conn = c.Where(a => a.ID == editConnector.ID).SingleOrDefault();
				if (conn != null)
					return true;

				foreach (var cc in c)
					if (findBox(cc.SubConnections))
						return true;

				return false;
			}

			var b = dominoBoxes.Values.Where(a => findBox(a.Connections)).SingleOrDefault();
			/*if (b != null)
			{
				b.Widget.list.Children.Remove(editConnector.ContainerUI);
			}*/

			foreach (var l in lines)
			{
				l.Point1 = l.Point1.Replace(editConnector.ID, name);
				l.Point2 = l.Point2.Replace(editConnector.ID, name);
            }

			editConnector.ID = name;
			editConnector.Widget.Header.Text = name;
			editConnector.Widget.delBtn.Tag = name;
			editConnector.Widget.editBtn.Tag = name;
            //editConnector.Widget.swapBtn.Tag = b.ID;
			editConnector.Widget.ID = name;

            foreach (var btn in editConnector.Widget.list.Children)
				if (btn is Button)
				{
					((Button)btn).Tag = name;
				}

			string findParentName(string currName, List<DominoConnector> c)
			{
				var conn = c.Where(a => a.ID == editConnector.ID).SingleOrDefault();
				if (conn != null)
					return currName;

				foreach (var cc in c)
				{
					var a = findParentName(currName + "(" + cc.FromBoxConnectID.ToString() + ") " + cc.FromBoxConnectIDStr + " > ", cc.SubConnections);
					if (a != "")
						return a;
				}

				return "";
			}

			if (b != null)
			{
				//DrawBoxConnectors(b, editConnector, findParentName("", b.Connections), editConnector.INT_clr);
				NewDrawConnector(editConnector, editConnector.DrawX, editConnector.DrawY, true);
				NewDrawBox(b, b.DrawX, b.DrawY, true);
			}

            WasEdited();

            return "";
		}


		public List<ExecEntry> AddBoxNames()
		{
			List<ExecEntry> boxNames = new();

			foreach (var rb in regBoxesAll)
				boxNames.Add(new() { Name = rb.Key });

			return boxNames;
		}

		public string AddBoxCreate(int width, int height, bool global, string name)
		{
			string newID = FindBoxFreeID().ToString();
			
			var isStateless = regBoxesAll[name].IsStateless;
			if (!isStateless && !global)
				return "Selected box is not stateless and must be defined as global.";

			if (global)
				newID = "self[" + newID + "]";
			else
				newID = "en_" + newID;

			//canvas.ResetZoom();

			if (!regBoxesAll.ContainsKey(name))
			{
				var meta = regBoxesAll[name];
                regBoxesAll.Add(name, meta);
			}

			DominoBox b = new();
			b.ID = newID;
			b.Name = name;
			dominoBoxes.Add(b.ID, b);

			Point pnt = new(width / 2, height / 2);
			pnt = canvas.Transform3(pnt);

			NewDrawBox(b, (int)pnt.X, (int)pnt.Y, true);
			//canvas.RefreshChilds();

            WasEdited();

            return "";
		}


		private void RemoveMetadataInfo(object sender, RoutedEventArgs e)
		{
			AskDialog("Remove metadata", "Do you want to remove the metadata?", () =>
			{
				string[] tag = ((string)((Button)sender).Tag).Split('|');

				if (tag[0] == "datain")
				{
					var m = dominoGraphs[selGraph].Metadata.DatasIn.Where(a => a.UniqueID == tag[1]).Single();
					wiMetaDataIn.list.Children.Remove(m.ContainerUI);
					dominoGraphs[selGraph].Metadata.DatasIn.RemoveAll(a => a.UniqueID == tag[1]);
					
					RefreshConnectorsVariables(m.Name, "");
				}
				if (tag[0] == "dataout")
				{
					var m = dominoGraphs[selGraph].Metadata.DatasOut.Where(a => a.UniqueID == tag[1]).Single();
					wiMetaDataOut.list.Children.Remove(m.ContainerUI);
					dominoGraphs[selGraph].Metadata.DatasOut.RemoveAll(a => a.UniqueID == tag[1]);
					
					RefreshConnectorsVariables(m.Name, "");
				}
				if (tag[0] == "globalvar")
				{
					var m = globalVariables.Where(a => a.UniqueID == tag[1]).Single();
					wiGlobalVars.list.Children.Remove(m.ContainerUI);
					globalVariables.RemoveAll(a => a.UniqueID == tag[1]);
					
					RefreshConnectorsVariables(m.Name, "");
				}
                /*if (tag[0] == "controlin")
				{
					var m = thisMetadata.ControlsIn.Where(a => a.Name == tag[1]).Single();
					wiMetaControlIn.list.Children.Remove(m.ContainerUI);
					thisMetadata.ControlsIn.RemoveAll(a => a.Name == tag[1]);

					RemoveLine("ControlsIn-P1", m.Name + "-P2");
				}
				if (tag[0] == "controlout")
				{
					var m = thisMetadata.ControlsOut.Where(a => a.Name == tag[1]).Single();
					wiMetaControlOut.list.Children.Remove(m.ContainerUI);
					thisMetadata.ControlsOut.RemoveAll(a => a.Name == tag[1]);

					var c = dominoConnectors.Values.Where(a => a.OutFuncName.Contains(m.Name)).SingleOrDefault();
					if (c != null)
						RemoveLine(c.ID + "-P1", "ControlsOut-P2");
				}*/

                WasEdited();
            });
		}

		private void AddMetadataInfo(object sender, RoutedEventArgs e)
		{
			string tag = (string)((Button)sender).Tag;

			if (tag == "datain")
			{
				var m = new DominoBoxMetadataDatasIn("NewDataIn", 0, "string");
				dominoGraphs[selGraph].Metadata.DatasIn.Add(m);
				DrawMetaDataIn(m, true);
			}
			if (tag == "dataout")
			{
				var m = new DominoBoxMetadataDatasOut("NewDataOut", 0, "string");
				dominoGraphs[selGraph].Metadata.DatasOut.Add(m);
				DrawMetaDataIn(m, false);
			}
			if (tag == "globalvar")
			{
				var m = new DominoDict();
				m.Name = "NewGlobalVariable";
				m.Value = "\"A value\"";
				globalVariables.Add(m);
				DrawGlobalVar(m);
			}
            /*if (tag == "controlin")
			{
				var m = new DominoBoxMetadataControlsIn("New Control In", 0, "");
				thisMetadata.ControlsIn.Add(m);
				DrawMetaControlIn(m);
			}
			if (tag == "controlout")
			{
				var m = new DominoBoxMetadataControlsOut("New Control Out", 0, false);
				thisMetadata.ControlsOut.Add(m);
				DrawMetaControlOut(m);
			}*/

            WasEdited();
        }

		public delegate void OpenEditDataDialog(string name, string desc, string metaName, string anchorDynType, string dataTypeID, string hostExecFunc, bool? isDelayed, List<ParamEntry> dataList);
		public OpenEditDataDialog openEditDataDialog;
		string[] editMetadataDialogData;

		public List<string> GetDataTypes()
		{
			return dataTypes;
		}

        private void EditMetadataInfo(object sender, RoutedEventArgs e)
		{
			string[] tag = ((string)((Button)sender).Tag).Split('|');
			editMetadataDialogData = tag;

			if (tag[0] == "datain")
			{
				var m = dominoGraphs[selGraph].Metadata.DatasIn.Where(a => a.UniqueID == tag[1]).Single();
				openEditDataDialog("Edit data in", "Edit the input data of this Domino:", m.Name, m.AnchorDynType.ToString(), m.DataTypeID, null, null, null);
			}
			if (tag[0] == "dataout")
			{
				var m = dominoGraphs[selGraph].Metadata.DatasOut.Where(a => a.UniqueID == tag[1]).Single();
				openEditDataDialog("Edit data out", "Edit the output data of this Domino:", m.Name, m.AnchorDynType.ToString(), m.DataTypeID, null, null, null);
			}
			if (tag[0] == "controlin")
			{
				var m = dominoGraphs[selGraph].Metadata.ControlsIn.Where(a => a.Name == tag[1]).Single();
				openEditDataDialog("Edit controls in", "Edit the input controls of this Domino:", null, m.AnchorDynType.ToString(), null, m.HostExecFunc, null, null);
			}
			if (tag[0] == "controlout")
			{
				var m = dominoGraphs[selGraph].Metadata.ControlsOut.Where(a => a.Name == tag[1]).Single();
				openEditDataDialog("Edit controls out", "Edit the output controls of this Domino:", null, m.AnchorDynType.ToString(), null, null, m.IsDelayed, null);
			}
			if (tag[0] == "globalvar")
			{
				var m = globalVariables.Where(a => a.UniqueID == tag[1]).Single();

				paramsEdit = Helpers.CopyList(new() { m });
				paramsEditSingle = true;
				openEditDataDialog("Edit global variables", "Edit global variable:", m.Name, null, null, null, null, EditExecBoxUISetParams());
			}
			if (tag[0] == "connvar")
			{
				editConnVar = tag;

				var c = dominoConnectors[tag[1]];
				var v = c.SetVariables.Where(a => a.UniqueID == tag[2]).Single();

				paramsEdit = Helpers.CopyList(new() { v });
				paramsEditSingle = true;

				openEditDataDialog("Edit variable", "Set variable name and value:", v.Name, null, null, null, null, EditExecBoxUISetParams());
			}
		}

		public void EditMetadataInfoCreate(string name, string anchorDynType, string dataTypeID, string hostExecFunc, bool? isDelayed, List<ParamEntry> paramsList)
		{
			if (editMetadataDialogData[0] == "datain")
			{
				var m = dominoGraphs[selGraph].Metadata.DatasIn.Where(a => a.UniqueID == editMetadataDialogData[1]).Single();

				string oldName = m.Name;

				m.Name = name;
				m.AnchorDynType = int.Parse(anchorDynType);
				m.DataTypeID = dataTypeID;

                //wiMetaDataIn.list.Children.Remove(m.ContainerUI);
                //DrawMetaDataIn(m, true);
                DrawMetaDataIn(m, true, true);

				RefreshConnectorsVariables(oldName, name);
            }
            if (editMetadataDialogData[0] == "dataout")
			{
				var m = dominoGraphs[selGraph].Metadata.DatasOut.Where(a => a.UniqueID == editMetadataDialogData[1]).Single();

				string oldName = m.Name;

				m.Name = name;
				m.AnchorDynType = int.Parse(anchorDynType);
				m.DataTypeID = dataTypeID;

				//wiMetaDataOut.list.Children.Remove(m.ContainerUI);
				DrawMetaDataIn(m, false, true);

				RefreshConnectorsVariables(oldName, name);
			}
			if (editMetadataDialogData[0] == "controlin")
			{
				var m = dominoGraphs[selGraph].Metadata.ControlsIn.Where(a => a.Name == editMetadataDialogData[1]).Single();
				m.AnchorDynType = int.Parse(anchorDynType);
				m.HostExecFunc = hostExecFunc;

				//wiMetaControlIn.list.Children.Remove(m.ContainerUI);
				DrawMetaControlIn(m, true);
			}
			if (editMetadataDialogData[0] == "controlout")
			{
				var m = dominoGraphs[selGraph].Metadata.ControlsOut.Where(a => a.Name == editMetadataDialogData[1]).Single();
				m.AnchorDynType = int.Parse(anchorDynType);
				m.IsDelayed = isDelayed == true;

				//wiMetaControlOut.list.Children.Remove(m.ContainerUI);
				DrawMetaControlOut(m, true);
			}
			if (editMetadataDialogData[0] == "globalvar")
			{
				name = name.Replace(" ", "");

				var m = globalVariables.Where(a => a.UniqueID == editMetadataDialogData[1]).Single();

				//wiGlobalVars.list.Children.Remove(m.ContainerUI);

				string oldName = m.Name;

				EditExecBoxUIGetParams(paramsList);
				//m = paramsEdit[0]; // loose instance
				m.Name = name;
				m.Value = paramsEdit[0].Value;
				m.ValueArray = paramsEdit[0].ValueArray;
				m.UniqueID = paramsEdit[0].UniqueID;

				DrawGlobalVar(m, true);

				RefreshConnectorsVariables(oldName, name);
			}
			if (editMetadataDialogData[0] == "connvar")
			{
				name = name.Replace(" ", "");
				//val = val.Replace(" ", "");

				var c = dominoConnectors[editConnVar[1]];
				var v = c.SetVariables.Where(a => a.UniqueID == editConnVar[2]).Single();

				v.Name = name;

				EditExecBoxUIGetParams(paramsList);
				v.Name = name;
				v.Value = paramsEdit[0].Value;
				v.ValueArray = paramsEdit[0].ValueArray;
				v.UniqueID = paramsEdit[0].UniqueID;
                //v.ContainerUI.Child = DrawConnVariableChild(c, v);

				NewDrawConnector(c, c.DrawX, c.DrawY, true);

				AddConnectorLines(c, 1);
				AddControlOutLines(1);
			}

            WasEdited();
        }

		private void RemoveConnVar(object sender, RoutedEventArgs e)
		{
			AskDialog("Remove variable", "Do you want to remove the variable?", () =>
			{
				string[] tags = ((string)((Button)sender).Tag).Split('|');

				var c = dominoConnectors[tags[0]];

				var m = c.SetVariables.Where(a => a.UniqueID == tags[1]).Single();
				//c.Widget.list.Children.Remove(m.ContainerUI);
				c.SetVariables.RemoveAll(a => a.UniqueID == tags[1]);

				NewDrawConnector(c, c.DrawX, c.DrawY, true);

                AddConnectorLines(c, 1);
				AddControlOutLines(1);

                WasEdited();
            });
		}

		string[] editConnVar;

		private void AddConnVar(object sender, RoutedEventArgs e)
		{
			var c = dominoConnectors[(string)((Button)sender).Tag];
			var v = new DominoDict() { Name = "NewVariable", Value = "\"A value\"" };
			c.SetVariables.Add(v);
			//DrawConnVariable(c, v);
			
			NewDrawConnector(c, c.DrawX, c.DrawY, true);

			AddConnectorLines(c, 1);
			AddControlOutLines(1);

            WasEdited();
        }

		public void AddComment(int width, int height)
		{
			//canvas.ResetZoom();

			var c = new DominoComment();
			c.Name = "A comment";
			c.Color = 0;

			Point pnt = new(width / 2, height / 2);
			pnt = canvas.Transform3(pnt);

			DrawComment(c, (int)pnt.X, (int)pnt.Y);
			dominoComments.Add(c.UniqueID, c);

			//canvas.RefreshChilds();

            WasEdited();
        }

		public delegate void OpenAddCommentDialog(string name, int selClr, List<ColorEntry> colors);
		public OpenAddCommentDialog openAddCommentDialog;
		private DominoComment editComment = null;

		public void EditCommentDialog(object sender, RoutedEventArgs e)
		{
			string[] tags = ((string)((Button)sender).Tag).Split('|');

			editComment = dominoComments[tags[1]];

			if (tags[0] == "edit")
			{
				List<ColorEntry> colors = new();

				foreach (var c in linesColors)
					colors.Add(new() { Color = new(c) });

				openAddCommentDialog(editComment.Name, editComment.Color, colors);
			}
			if (tags[0] == "delete")
			{
				AskDialog("Remove comment", "Do you want to remove the comment?", () =>
				{
					RemoveComment(editComment);

                    WasEdited();
                });
			}
		}

		public void EditCommentDialogAct(string name, int selClr)
		{
			//canvas.ResetZoom();

			editComment.Name = name;
			editComment.Color = selClr;

			var x = Canvas.GetLeft(editComment.ContainerUI);
			var y = Canvas.GetTop(editComment.ContainerUI);

			canvas.Children.Remove(editComment.ContainerUI);

			DrawComment(editComment, x, y);

			//canvas.RefreshChilds();

            WasEdited();
        }

		public void AddBorder(int width, int height)
		{
			//canvas.ResetZoom();

			var b = new DominoBorder();
			b.Color = 0;
			b.Style = 0;
			b.BackgroundColor = -1;

			Point pnt = new(width / 2, height / 2);
			pnt = canvas.Transform3(pnt);

			DrawBorder(b, (int)pnt.X, (int)pnt.Y, 50, 50, true);
			dominoBorders.Add(b.UniqueID, b);

			//canvas.RefreshChilds();

            WasEdited();
        }

		public delegate void OpenAddBorderDialog(int selStyle, int selClr, int selBgClr, List<ColorEntry> colors, bool moveChilds);
		public OpenAddBorderDialog openAddBorderDialog;
		private DominoBorder editBorder = null;

		public void EditBorderDialog(object sender, RoutedEventArgs e)
		{
			string[] tags = ((string)((Button)sender).Tag).Split('|');

			editBorder = dominoBorders[tags[1]];

			if (tags[0] == "edit")
			{
				List<ColorEntry> colors = new();

				foreach (var c in linesColors)
					colors.Add(new() { Color = new(c) });

				openAddBorderDialog(editBorder.Style, editBorder.Color, editBorder.BackgroundColor, colors, editBorder.ContainerUI.EnableMovingChilds);
			}
			if (tags[0] == "delete")
			{
				AskDialog("Remove border", "Do you want to remove the border?", () =>
				{
					RemoveBorder(editBorder);

                    WasEdited();
                });
			}
		}

		public void EditBorderDialogAct(int selClr, int selBgClr, int selStyle, bool? moveChilds)
		{
			//canvas.ResetZoom();

			editBorder.Style = selStyle;
			editBorder.Color = selClr;
			editBorder.BackgroundColor = selBgClr;

			var x = Canvas.GetLeft(editBorder.ContainerUI);
			var y = Canvas.GetTop(editBorder.ContainerUI);
			var w = editBorder.ContainerUI.Width;
			var h = editBorder.ContainerUI.Height;

			canvas.Children.Remove(editBorder.ContainerUI);

			DrawBorder(editBorder, x, y, w, h, moveChilds);

			//canvas.RefreshChilds();

            WasEdited();
        }

		public void AddResource(object sender, RoutedEventArgs e)
		{
			var b = new DominoDict();
			b.Name = "NewResource";
			b.Value = "Type";

			DrawResource(b);
			dominoResources.Add(b);

            WasEdited();
        }

		public delegate void OpenEditResourceDialog(string name, int selType, List<string> types);
		public OpenEditResourceDialog openEditResourceDialog;
		private DominoDict editResource = null;

		public void EditResourceDialog(object sender, RoutedEventArgs e)
		{
			string[] tags = ((string)((Button)sender).Tag).Split('|');

			editResource = dominoResources.Where(a => a.UniqueID == tags[1]).Single();

			if (tags[0] == "edit")
			{
				int s = resourcesTypes.FindIndex(a => a == editResource.Value);
				openEditResourceDialog(editResource.Name, s == -1 ? 0 : s, resourcesTypes);
			}
			if (tags[0] == "delete")
			{
				AskDialog("Remove resource", "Do you want to remove the resource?", () =>
				{
					wiResources.list.Children.Remove(editResource.ContainerUI);
					dominoResources.Remove(editResource);

                    WasEdited();
                });
			}
		}

		public void EditResourceDialogAct(string name, string type)
		{
			editResource.Name = name;
			editResource.Value = type;

			wiResources.list.Children.Remove(editResource.ContainerUI);

			DrawResource(editResource);

            WasEdited();
        }



		public delegate void OpenGetDataFromBoxDialog(List<ExecEntry> boxes);
		public OpenGetDataFromBoxDialog openGetDataFromBoxDialog;
		public Action<string> getDataFromBoxAction;

		public void GetDataFromBox(Action<string> action)
		{
			getDataFromBoxAction = action;

			List<ExecEntry> boxes = new();
			foreach (var b in dominoBoxes.Values)
			{
				if (regBoxesAll.ContainsKey(b.Name))
				{
					var m = regBoxesAll[b.Name].DatasOut.Any();
					if (m)
						boxes.Add(new() { Name = b.ID + " - " + b.Name, Num = b.ID });
				}
			}

			boxes = boxes.OrderBy(a => a.Name).ToList();

			openGetDataFromBoxDialog(boxes);
		}

		public List<ExecEntry> GetDataFromBoxDatas(string selBox)
		{
			var b = dominoBoxes[selBox];
			var m = regBoxesAll[b.Name].DatasOut;

			List<ExecEntry> entries = new();
			for (int i = 0; i < m.Count; i++)
				entries.Add(new() { Name = m[i].Name, Num = i.ToString() });

			return entries;
		}

		public void GetDataFromBoxCreate(string selBox, string selData)
		{
			var d = selBox + $":GetDataOutValue({selData})";
			getDataFromBoxAction(d);
		}



		private void LoadSettings()
		{
            settings.Add("useBezier", true);
            settings.Add("linePointsBezier", true);
            settings.Add("snapToGrid", false);
            settings.Add("bytecode", false);
            settings.Add("bytecodeDebug", false);
            settings.Add("coloredBoxes", true);
        }

        public void UseSettings(bool saved)
		{
			canvas.SnapToGrid = (bool)settings["snapToGrid"];

			foreach (var l in lines)
			{
                l.UI.MakeBezierAlt = (bool)settings["useBezier"];
                l.UI.MakePoly = !(bool)settings["useBezier"];

                l.UI.PointBezier = (bool)settings["linePointsBezier"];

                l.UI.InvalidateVisual();
            }

			foreach (var c in dominoConnectors.Values)
				NewDrawConnector(c, c.DrawX, c.DrawY, true);

			foreach (var b in dominoBoxes.Values)
				NewDrawBox(b, b.DrawX, b.DrawY, true);

			if (saved)
				WasEdited();
        }

		public string RenameWorkspace(string type, string name)
        {
            if (type == "workspace")
            {
				workspaceName = name;
            }
            if (type == "graph")
            {
                if (dominoGraphs.Any(a => a.Name == name))
                    return "Graph with this name already exists. Please select another name.";

                dominoGraphs[selGraph].Name = name;

				foreach (var b in dominoBoxes.Values)
				{
					if (b.Name.StartsWith("GRAPH: "))
					{
						b.Name = "GRAPH: " + name;
                        //b.Widget.Header.Text = b.ID + " - " + b.Name; - not needed cuz current graph can't contain itself
                    }
				}
            }
            if (type == "path")
            {
				datPath = name;

                if (!datPath.EndsWith("\\"))
                    datPath += "\\";
            }

            SetWorkspaceNameAndGraphs();

			return "";
        }

		/*private void DuplicateBorder(object sender, RoutedEventArgs e)
		{
			AskDialog("Duplicate border", $"This will duplicate selected border with all its boxes and connectors.{Environment.NewLine}Also make sure there is free space under the border, because the border will appear under this one.{Environment.NewLine}Continue?", () =>
			{
				string tag = (string)((Button)sender).Tag;

				DominoBorder bd = dominoBorders.Where(a => a.UniqueID == tag).Single();

				DominoBorder bdNew = new();
				bdNew.Color = bd.Color;
				bdNew.Style = bd.Style;

				var pos = new Point(Canvas.GetLeft(bd.ContainerUI), Canvas.GetTop(bd.ContainerUI));
				var add = new Point(bd.ContainerUI.Width, bd.ContainerUI.Height + 20);
				add = canvas.Transform4(add);

				var newPosY = add.Y + 20;

				DrawBorder(bdNew, pos.X, pos.Y + newPosY, bd.ContainerUI.Width, bd.ContainerUI.Height, bd.ContainerUI.EnableMovingChilds);

				dominoBorders.Add(bdNew);

				List<DominoComment> newComments = new();
				foreach (var c in dominoComments)
				{
					var cx = Canvas.GetLeft(c.ContainerUI);
					var cy = Canvas.GetTop(c.ContainerUI);

					if (
						cx > pos.X &&
						cy > pos.Y &&
						cx < pos.X + add.X &&
						cy < pos.Y + add.Y
						)
					{
						DominoComment newComm = new();
						newComm.Name = c.Name;
						newComm.Color = c.Color;

						DrawComment(newComm, cx, cy + newPosY);

						newComments.Add(newComm);
					}
				}

				foreach (var c in newComments)
					dominoComments.Add(c);

				Dictionary<string, DominoBox> newBoxes = new();
				Dictionary<string, DominoConnector> newConnectors = new();
				Dictionary<string, string> oldNewBoxes = new();
				foreach (var b in dominoBoxes.Values)
				{
					var bx = Canvas.GetLeft(b.Widget);
					var by = Canvas.GetTop(b.Widget);

					if (
						bx > pos.X &&
						by > pos.Y &&
						bx < pos.X + add.X &&
						by < pos.Y + add.Y
						)
                    {
                        int newBoxID = FindBoxFreeID(0, newBoxes.Values.ToList());

                        string ni = "";
						if (b.ID.StartsWith("self["))
							ni = $"self[{newBoxID}]";
						else
							ni = $"en_{newBoxID}";

						oldNewBoxes.Add(b.ID, ni);

						DominoBox boxNew = new();
						boxNew.ID = ni;
						boxNew.Name = b.Name;
						DrawBox(boxNew, bx, by + newPosY);
						newBoxes.Add(ni, boxNew);
					}
				}

				foreach (var a in newBoxes)
				{
					dominoBoxes.Add(a.Key, a.Value);

					var origBox = dominoBoxes[oldNewBoxes.Single(aa => aa.Value == a.Key).Key];

					DominoConnector addConn(DominoConnector orig)
					{
						string oldID = origBox.ID.Replace("self[", "").Replace("]", "").Replace("en_", "");
						string newID = a.Key.Replace("self[", "").Replace("]", "").Replace("en_", "");

						string newConnID = null;

						if (orig.ID != null)
                        {
                            newConnID = orig.ID.Replace("f_" + oldID + "_", newID.ToString() + "_");
							newConnID = "f_" + FindConnectorFreeID(newConnID);
                        }

						DominoConnector newConn = new();
						newConn.ID = newConnID;
						newConn.SetVariables = Helpers.CopyList(orig.SetVariables);
						newConn.FromBoxConnectID = orig.FromBoxConnectID;
						newConn.FromBoxConnectIDStr = orig.FromBoxConnectIDStr;
						newConn.OutFuncName = orig.OutFuncName;

						foreach (var sc in orig.SubConnections)
							newConn.SubConnections.Add(addConn(sc));

						if (orig.ID != null)
						{
							newConnectors.Add(newConnID, newConn);

							var cx = Canvas.GetLeft(orig.Widget);
							var cy = Canvas.GetTop(orig.Widget);

							DrawConnector(newConn, cx, cy + newPosY);
							DrawBoxConnectors(a.Value, newConn);
						}

						foreach (var e in orig.ExecBoxes)
						{
							int clr = newConn.ExecBoxes.Count;

							int cc = e.DynIntExec;
							DominoBox execBox = null;

							if (oldNewBoxes.TryGetValue(e.Box.ID, out string value))
								execBox = newBoxes[value];
							else
							{
								execBox = dominoBoxes[e.Box.ID];
                                cc = FindDynIntFreeNum(e.Box.ID);
							}

							ExecBox execBoxNew = new();
							execBoxNew.Exec = e.Exec;
							execBoxNew.DynIntExec = cc;
							execBoxNew.ExecStr = e.ExecStr;
							execBoxNew.Type = e.Type;
							execBoxNew.Params = Helpers.CopyList(e.Params);
							execBoxNew.Box = execBox;
							newConn.ExecBoxes.Add(execBoxNew);

							DrawExecBoxContainerUI(newConn, execBoxNew, linesColors[clr]);

							var a = canvas.Transform2(new(Canvas.GetLeft(newConn.Widget), Canvas.GetTop(newConn.Widget)));
							var b = canvas.Transform2(new(Canvas.GetLeft(execBoxNew.Box.Widget), Canvas.GetTop(execBoxNew.Box.Widget)));

							DrawLine(
								a.X + width,
								a.Y,
								b.X,
								b.Y,
								newConn.ID + "-P1",
								execBoxNew.Box.ID + "-P1",
								clr
							);
						}

						return newConn;
					}

					foreach (var c in origBox.Connections)
						a.Value.Connections.Add(addConn(c));
				}

				foreach (var c in newConnectors)
					dominoConnectors.Add(c.Key, c.Value);

                WasEdited();

                canvas.RefreshChilds();
			});
		}*/

		public void CopyingMakeCopy()
		{
			var d = DataToXML(false, canvas.SelectedItems);

            XDocument doc = new();
            doc.Add(d);

            StringBuilder builder = new StringBuilder();
            using (TextWriter writer = new StringWriter(builder))
            {
                doc.Save(writer);
            }

			
			wnd.Clipboard.SetTextAsync(builder.ToString());

			canvas.ResetSelection();
		}

		public async Task CopyingPaste()
		{
			string strData = await wnd.Clipboard.GetTextAsync();

			XElement xData = null;

			try
			{
				xData = XElement.Parse(strData);
			}
			catch (Exception)
			{
				return;
			}

			XMLToData(xData, true);

			HandleMoved();

            WasEdited();

            canvas.RefreshChilds();
		}

		private string MakeDebugFunction(string luaFile)
		{
			// write box input data - as array, table via func param
			// param - from box name + out func, to box name + exec box func

			string nl = Environment.NewLine;
			string func =
                $"function export:WriteDebugToFile(params, frombox, tobox, infunc, vars)" + nl +
                $"  local buffer = \"\"" + nl +
                $"  buffer = buffer .. \"[\" .. tostring(os.date()) .. \"] \"" + nl +
                $"  buffer = buffer .. \"Workspace: {workspaceName}, \"" + nl +
                $"  buffer = buffer .. \"Graph: {dominoGraphs[selGraph].Name}, \"" + nl +
                $"  buffer = buffer .. \"Script: {luaFile}, \"" + nl +
                $"" + nl +
                $"  if frombox ~= \"\" then" + nl +
                $"    buffer = buffer .. \"FromBox: \" .. frombox .. \", \"" + nl +
                $"  end" + nl +
                $"" + nl +
                $"  if tobox ~= \"\" then" + nl +
                $"    buffer = buffer .. \"ToBox: \" .. tobox .. \", \"" + nl +
                $"  end" + nl +
                $"" + nl +
                $"  buffer = buffer .. \"InFunc: \" .. infunc .. \", \"" + nl +
                $"" + nl +
                $"  if tobox ~= \"\" then" + nl +
                $"    buffer = buffer .. \"ExecBoxParams: \" .. self:WriteDebugToFileParams(params)" + nl +
                $"  end" + nl +
                $"" + nl +
                $"  if vars ~= \"\" then" + nl +
                $"    buffer = buffer .. \"Set to variable \" .. vars .. \": \" .. self:WriteDebugToFileParams(params)" + nl +
                $"  end" + nl +
                $"" + nl +
                $"  local file, err = io.open(\"DominoDebug.log\", \"a+\")" + nl +
                $"  if file ~= nil and err == nil then" + nl +
                $"    io.output(file)" + nl +
                $"    io.write(buffer .. \"\\n\")" + nl +
                $"    file:flush()" + nl +
                $"    file:close()" + nl +
                $"    file = nil" + nl +
                $"  end" + nl +
                $"end" + nl +
                $"function export:WriteDebugToFileVal(value)" + nl +
                $"  local isStr = type(value) == \"string\"" + nl +
                $"  local buffer = (isStr and \"\\\"\" or \"\") .. tostring(value) .. (isStr and \"\\\"\" or \"\")" + nl +
                $"  if isStr then" + nl +
                $"    if (value):match(\"^%-?%d+$\") ~= nil then" + nl +
                $"      if tonumber(value) > 10000000000 then" + nl +
                $"        buffer = buffer .. \" (\" .. CAPI_Entity.GetName(value) .. \")\"" + nl +
                $"      end" + nl +
                $"    end" + nl +
                $"  end" + nl +
                $"  return buffer" + nl +
                $"end" + nl +
                $"function export:WriteDebugToFileParams(params)" + nl +
                $"  local buffer = \"\"" + nl +
                $"" + nl +
                $"  if type(params) == \"table\" then" + nl +
                $"    buffer = buffer .. \"{{\"" + nl +
                $"    for key,value in pairs(params) do" + nl +
                $"      local kn = \"\"" + nl +
                $"      if type(key) == \"number\" then" + nl +
                $"        kn = string.format(\"%.0f\",key)" + nl +
                $"      else" + nl +
                $"        kn = \"\\\"\" .. key .. \"\\\"\"" + nl +
                $"      end" + nl +
                $"      if type(value) == \"table\" then" + nl +
                $"        buffer = buffer .. \"[\" .. kn .. \"] = \" .. self:WriteDebugToFileParams(value)" + nl +
                $"      else" + nl +
                $"        buffer = buffer .. \"[\" .. kn .. \"] = \" .. self:WriteDebugToFileVal(value)" + nl +
                $"      end" + nl +
                $"      buffer = buffer .. \", \"" + nl +
                $"    end" + nl +
                $"    buffer = buffer .. \"}}\"" + nl +
                $"  else" + nl +
                $"    buffer = buffer .. self:WriteDebugToFileVal(params)" + nl +
                $"  end" + nl +
                $"" + nl +
                $"  return buffer" + nl +
                $"end";

            return func;
		}

		private void WasEdited(bool saved = false)
		{
			wasEdited = !saved;
			wnd.SetTitle(false, file, wasEdited);
        }

		public delegate void SetWorkspaceName(string workspace, List<DominoGraph> graphs, int selGraph, string forceReload);
		public SetWorkspaceName setWorkspaceName;

		private void SetWorkspaceNameAndGraphs(string forceReload = "")
		{
			var graphsSorted = dominoGraphs.OrderBy(a => a.Name).ToList();
			setWorkspaceName(workspaceName, graphsSorted, graphsSorted.FindIndex(a => a.UniqueID == dominoGraphs[selGraph].UniqueID), forceReload);
		}

		public string AddGraph(string name)
		{
			if (dominoGraphs.Any(a => a.Name == name))
				return "Graph with this name already exists. Please select another name.";

			DominoGraph g = new();
			g.Name = name;
			g.IsDefault = false;
			dominoGraphs.Add(g);

			SetWorkspaceNameAndGraphs();

            WasEdited();

            return "";
		}

		public void DeleteGraph(string graphID)
		{
			AskDialog("Delete graph", "Delete this graph? You can't undo this action. Workspace will be saved.", () =>
			{
				var g = dominoGraphs.Where(a => a.UniqueID == graphID).Single();
				if (g.IsDefault)
				{
					openInfoDialog("Delete graph", "You can't delete default graph.");
					return;
				}

				// todo check if box exists in other graphs
				var gbp = BuildGraphName(g, true, false);
				var ge = dominoGraphs.Any(a => a.ContainsBoxes.Contains(gbp));
				if (ge)
				{
					openInfoDialog("Delete graph", "Can't delete the graph because it is used as box in another graph in this workspace.");
					return;
				}

				Save(true);

				dominoGraphs.RemoveAll(a => a.UniqueID == graphID);

				var dg = dominoGraphs.Where(a => a.IsDefault).Single();

				selGraph = dominoGraphs.FindIndex(a => a.UniqueID == dg.UniqueID);
				SetWorkspaceNameAndGraphs(dg.UniqueID);
			});
		}

		private string BuildGraphName(DominoGraph graph, bool inDatPath, bool debug)
		{
			string f = workspaceName.Replace(" ", "_").ToLowerInvariant() + "." + graph.Name.Replace(" ", "_").ToLowerInvariant() + (debug ? ".dvdebug" : "") + ".lua";

			if (inDatPath)
				f = datPath + f;

			f = f.Replace("\\", "/").ToLower();

			return f;
		}

		private string BuildGraphName(string boxName, bool debug)
		{
			if (regBoxesAll.ContainsKey(boxName))
				if (regBoxesAll[boxName].INT_Graph)
				{
					//boxName = BuildGraphName(boxName);

					DominoGraph graph = dominoGraphs.Where(a => a.Name == boxName.Replace("GRAPH: ", "")).Single();

					boxName = datPath + workspaceName.Replace(" ", "_").ToLowerInvariant() + "." + graph.Name.Replace(" ", "_").ToLowerInvariant() + (debug ? ".dvdebug" : "") + ".lua";
					boxName = boxName.Replace("\\", "/").ToLower();
				}
			
			return boxName;
		}

		private void AddGraphBox(DominoGraph graph)
		{
			string boxName = "GRAPH: " + graph.Name; // BuildGraphName(graph, true);

			if (dominoGraphs[selGraph].UniqueID != graph.UniqueID)
				regBoxesAll.Add(boxName, graph.Metadata);
		}

		public void CheckEdited(Action afterAction)
		{
			if (wasEdited)
			{
				AskDialog("Unsaved changes", "There are some unsaved changes. Do you want to save before exit?", () =>
				{
					Save();
					afterAction();
				},
				() =>
				{
					afterAction();
				});
			}
			else
				afterAction();
		}

		private string CheckConnBox()
		{
			List<string> execBoxes = new();
			foreach (var c in dominoConnectors.Values)
			{
				if (c.FromBoxConnectID == -1)
				{
					return "There is a connector which isn't connected to any output from a box - " + c.ID;
				}

				foreach (var e in c.ExecBoxes)
				{
					execBoxes.Add(e.Box.ID);
				}
			}
			foreach (var b in dominoBoxes.Keys)
			{
				if (!execBoxes.Contains(b))
				{
					return "There is a box which isn't connected to any exec from a connector - " + b;
				}
			}
			return "";
		}

        public delegate void OpenNotice(string text);
        public OpenNotice openNotice;

		public void Export()
		{
			bool su = false;

			var a = ExportWrite(false);
			if (a == "r")
			{
			}
			else if (a != "")
				openInfoDialog("Export", a);
			else if (a == "")
                su = true;

            a = ExportWrite(true);
            if (a == "r")
			{
			}
            else if (a != "")
            {
                openInfoDialog("Export", a);
                su = false;
            }
            else if (a == "")
                su = true;

            if (su)
                openNotice("Domino has been successfully exported to LUA.");
		}

		private string ExportWrite(bool debug)
		{
			var acb = CheckConnBox();
			if (acb != "")
				return acb;

			string exportPath = "";
			string exportPathDep = "";
			string fileName = BuildGraphName(dominoGraphs[selGraph], false, debug);

			if (file == "" || file.EndsWith(".lua"))
            {
                FolderPickerOpenOptions opts = new();
                opts.AllowMultiple = false;
                opts.Title = "Select output folder.";
                var d = wnd.StorageProvider.OpenFolderPickerAsync(opts).Result;

				if (d != null && d.Count > 0)
				{
					string path = d[0].Path.LocalPath;

                    exportPath = path + "\\" + fileName;
                    exportPathDep = path + "\\" + workspaceName.Replace(" ", "_").ToLowerInvariant();
                }
                else
                    return "r";

                /*System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
				{
					Description = "Select output folder.",
					AutoUpgradeEnabled = false
				};
				if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					exportPath = folderBrowserDialog.SelectedPath + "\\" + fileName;
					exportPathDep = folderBrowserDialog.SelectedPath + "\\" + workspaceName.Replace(" ", "_").ToLowerInvariant();
				}
				else
					return "r";*/

				/*SaveFileDialog sfd = new();
				sfd.Title = "Export Domino Workspace to LUA";
				sfd.Filter = "Compiled Domino Workspace|*.lua";
				if (sfd.ShowDialog() == true)
				{
					exportPath = sfd.FileName;
				}
				else
					return "";*/
			}
			else
			{
				exportPath = file.Replace(Path.GetFileName(file), "") + fileName;
				exportPathDep = file.Replace(Path.GetFileName(file), "") + workspaceName.Replace(" ", "_").ToLowerInvariant();
			}

			exportPathDep += "_depload.xml";
			
			Dictionary<string, string> exportDepData = new();

			string nl = Environment.NewLine;

			var luaData = new MemoryStream();
			var streamWriter = new StreamWriter(luaData);

			streamWriter.WriteLine("");
			streamWriter.WriteLine("-- Created with " + MainWindow.appName);
			streamWriter.WriteLine("--   Domino script: " + Path.GetFileName(file));
			streamWriter.WriteLine("--   Workspace:     " + workspaceName);
			streamWriter.WriteLine("--   Graph:         " + dominoGraphs[selGraph].Name);
			streamWriter.WriteLine("--");

			if (debug)
			{
				streamWriter.WriteLine("-- This is a debug file. It's not recommended to use it in production,");
				streamWriter.WriteLine("-- because it prints a lot of data, it can slow down the game.");
				streamWriter.WriteLine("--");
			}

			streamWriter.WriteLine("-- DO NOT EDIT MANUALLY THIS FILE OR YOUR CHANGES WILL BE LOST!");
			streamWriter.WriteLine("-- Please modify the original Domino script instead. You have been warned.");
            streamWriter.WriteLine("");
			streamWriter.WriteLine("export = {}");
			streamWriter.WriteLine("function export:LuaDependencies()");
			streamWriter.WriteLine("  local luaDepTable = {}");
			streamWriter.WriteLine("  return luaDepTable");
			streamWriter.WriteLine("end");
			streamWriter.WriteLine("function export:Create(cboxRes)");
			streamWriter.WriteLine("  --if cboxRes:ShouldLoadResources() == true then");

			/*foreach (var b in regBoxes)
			{
				bool bu = dominoBoxes.Any(b => b.Value.Name == b.Key);
                if (!b.Key.EndsWith(dominoGraphs[selGraph].Name) && bu)// != BuildGraphName(dominoGraphs[selGraph], true)
                    streamWriter.WriteLine($"    cboxRes:RegisterBox(\"{BuildGraphName(b.Key)}\")");
            }*/

			var usedBoxesN = dominoBoxes.Select(a => a.Value.Name).ToList().Distinct();
			var usedBoxes = regBoxesAll.Where(a => usedBoxesN.Contains(a.Key)).ToList().Distinct();
			foreach (var usedBox in usedBoxes)
            {
				var bn = BuildGraphName(usedBox.Key, debug);
                streamWriter.WriteLine($"    cboxRes:RegisterBox(\"{bn}\")");
				exportDepData.Add(bn, "CDominoBoxResource");
            }

			dominoResources = dominoResources.OrderBy(a => a.Name).ToList();
			foreach (var res in dominoResources)
            {
                streamWriter.WriteLine($"    cboxRes:LoadResource(\"{res.Name}\", \"{res.Value}\")");
                exportDepData.Add(res.Name, res.Value);
            }

			streamWriter.WriteLine("  --end");
			streamWriter.WriteLine("end");

			if (game != "fc6")
			{
				streamWriter.WriteLine("function export:RegisterCppMetadata()");

				foreach (var regBox in usedBoxes)
				{
					streamWriter.WriteLine($"  metadataTable[GetPathID(\"{BuildGraphName(regBox.Key, debug)}\")] = {{");
					streamWriter.WriteLine($"    stateless = {(regBox.Value.IsStateless ? "true" : "false")},");

					streamWriter.Write($"    controlIn = {{");
					if (regBox.Value.ControlsIn.Any())
					{
						streamWriter.WriteLine("");
						int cm = 0;
						foreach (var m in regBox.Value.ControlsIn)
						{
							streamWriter.WriteLine($"      [{cm}] = {{name = \"{m.Name}\"{(m.AnchorDynType > 0 ? ", dynamicType = " + m.AnchorDynType.ToString() : "")}}}{(cm != regBox.Value.ControlsIn.Count() ? "," : "")}");
							cm++;
						}
						streamWriter.WriteLine("    },");
					}
					else
						streamWriter.WriteLine("},");
					streamWriter.WriteLine($"    controlInCount = {regBox.Value.ControlsIn.Count()},");

					streamWriter.Write($"    controlOut = {{");
					if (regBox.Value.ControlsOut.Any())
					{
						streamWriter.WriteLine("");
						int cm = 0;
						foreach (var m in regBox.Value.ControlsOut)
						{
							streamWriter.WriteLine($"      [{cm}] = {{name = \"{m.Name}\"{(m.AnchorDynType > 0 ? ", dynamicType = " + m.AnchorDynType.ToString() : "")}, delayed = {(m.IsDelayed ? "true" : "false")}}}{(cm != regBox.Value.ControlsOut.Count() ? "," : "")}");
							cm++;
						}
						streamWriter.WriteLine("    },");
					}
					else
						streamWriter.WriteLine("},");
					streamWriter.WriteLine($"    controlOutCount = {regBox.Value.ControlsOut.Count()},");

					streamWriter.Write($"    dataIn = {{");
					if (regBox.Value.DatasIn.Any())
					{
						streamWriter.WriteLine("");
						int cm = 0;
						foreach (var m in regBox.Value.DatasIn)
						{
							streamWriter.WriteLine($"      [{cm}] = {{name = \"{m.Name}\", type = \"{m.DataTypeID}\"{(m.AnchorDynType > 0 ? ", dynamicType = " + m.AnchorDynType.ToString() : "")}}}{(cm != regBox.Value.DatasIn.Count() ? "," : "")}");
							cm++;
						}
						streamWriter.WriteLine("    },");
					}
					else
						streamWriter.WriteLine("},");
					streamWriter.WriteLine($"    dataInCount = {regBox.Value.DatasIn.Count()},");

					streamWriter.Write($"    dataOut = {{");
					if (regBox.Value.DatasOut.Any())
					{
						streamWriter.WriteLine("");
						int cm = 0;
						foreach (var m in regBox.Value.DatasOut)
						{
							streamWriter.WriteLine($"      [{cm}] = {{name = \"{m.Name}\", type = \"{m.DataTypeID}\"{(m.AnchorDynType > 0 ? ", dynamicType = " + m.AnchorDynType.ToString() : "")}}}{(cm != regBox.Value.DatasOut.Count() ? "," : "")}");
							cm++;
						}
						streamWriter.WriteLine("    },");
					}
					else
						streamWriter.WriteLine("},");
					streamWriter.WriteLine($"    dataOutCount = {regBox.Value.DatasOut.Count()},");

					streamWriter.WriteLine("  }");
				}

				streamWriter.WriteLine("end");
			}

			streamWriter.WriteLine("function export:Init(cbox)");
			streamWriter.WriteLine("  local l0");

			foreach (var var in globalVariables)
			{
				var vs = ParamsAsString(var, true);
				streamWriter.WriteLine($"  self.{var.Name} = {vs}");

				/*if (var.ValueArray.Count > 0)
				{
					string v = "";
					foreach (var a in var.ValueArray)
						v += (v != "" ? "," + nl : "") + "    " + a.Name + " = " + a.Value;

					streamWriter.WriteLine($"  self.{var.Name} = {{{nl}{v}{nl}  }}");
				}
				else
					streamWriter.WriteLine($"  self.{var.Name} = {var.Value}");*/
			}

			foreach (var box in dominoBoxes)
			{
				if (box.Value.ID.StartsWith("self["))
				{
					string bn = BuildGraphName(box.Value.Name, debug);

					if (game == "fc6")
						streamWriter.WriteLine($"  {box.Value.ID} = cbox:CreateBox_PathID(\"{CRC64.Hash(bn.ToLower().Replace("/", "\\"))}\")");
					else
						streamWriter.WriteLine($"  {box.Value.ID} = cbox:CreateBox(\"{bn}\")");

					streamWriter.WriteLine($"  l0 = {box.Value.ID}");
					streamWriter.WriteLine($"  l0:SetParentGraph(self._cbox)");
					streamWriter.WriteLine(ExportConns(box.Value));
				}
			}

			streamWriter.WriteLine("end");

			List<string> outFncs = new();

			foreach (var conn in dominoConnectors)
			{
				streamWriter.WriteLine($"function export:{conn.Value.ID}()");

				if (conn.Value.ExecBoxes.Any())
				{
					bool hasPa = conn.Value.ExecBoxes.Any(a => a.Params.Any() || a.Box.ID.StartsWith("en_"));
					streamWriter.WriteLine($"  local {(hasPa ? "params, " : "")}l0");
				}
				//else

				if (conn.Value.SetVariables.Count > 0)
					streamWriter.WriteLine($"  self:{conn.Value.ID.Replace("f_", "ex_")}()");

				foreach (var exec in conn.Value.ExecBoxes)
				{
					//if (conn.Value.SetVariables.Count > 0)
					//	streamWriter.WriteLine($"  self:{conn.Value.ID.Replace("f_", "ex_")}()");

					bool wasLocalBox = false;

					if (exec.Params.Count > 0 || exec.Box.ID.StartsWith("en_"))
					{
						if (exec.Box.ID.StartsWith("en_"))
							streamWriter.WriteLine($"  params = self:{exec.Box.ID}()");
						else
							streamWriter.WriteLine($"  params = self:en_{exec.Box.ID.Replace("self[", "").Replace("]", "")}()");
					}

					if (exec.Box.ID.StartsWith("en_"))
					{
						wasLocalBox = true;

						string bn = BuildGraphName(exec.Box.Name, debug);

						if (game == "fc6")
							streamWriter.WriteLine($"  l0 = Boxes[\"{CRC64.Hash(bn.ToLower().Replace("/", "\\"))}\"]");
						else
							streamWriter.WriteLine($"  l0 = Boxes[GetPathID(\"{bn}\")]");
					}
					else
						streamWriter.WriteLine($"  l0 = {exec.Box.ID}");

					if (debug)
					{
						string frombox = "";
                    	(bool, string) findBox(List<DominoConnector> cns, string s = "")
                    	{
                    	    foreach (var c in cns)
                    	    {
                    	        if (c.ID == conn.Value.ID)
                    	            return (true, s + (c.FromBoxConnectIDStr != "" ? ("." + c.FromBoxConnectIDStr) : ("[" + c.FromBoxConnectID.ToString() + "]")));
                    	    }
                    	    foreach (var c in cns)
                    	    {
                    	        if (c.SubConnections != null)
								{
									var a = findBox(c.SubConnections, s + (c.FromBoxConnectIDStr != "" ? ("." + c.FromBoxConnectIDStr) : ("[" + c.FromBoxConnectID.ToString() + "]")));
									if (a.Item1)
										return a;
                    	        }
                    	    }
                    	    return (false, s);
						}
                    	foreach (var db in dominoBoxes.Values)
                    	{
							var rd = findBox(db.Connections);
                    	    if (rd.Item1)
								frombox = db.ID + rd.Item2;
                    	}

                    	streamWriter.WriteLine($"  self:WriteDebugToFile({(exec.Params.Count > 0 || wasLocalBox ? "params" : "{}")}, \"{frombox}\", \"{exec.Box.ID}.{exec.ExecStr}{(exec.Type == ExecType.ExecDynInt ? "[" + exec.DynIntExec.ToString() + "]" : "")}\", \"{conn.Value.ID}\", \"\")");
					}

                    streamWriter.WriteLine($"  l0:Exec{(exec.Type == ExecType.ExecDynInt ? "DynInt" : "")}({exec.Exec}, {(exec.Params.Count > 0 || wasLocalBox ? "params" : "{}")}{(exec.Type == ExecType.ExecDynInt ? ", " + exec.DynIntExec : "")})");

					if (wasLocalBox)
						streamWriter.WriteLine($"  l0:SetParentGraph(nil)");
				}

				foreach (var otf in conn.Value.OutFuncName)
                {
                    if (debug)
                        streamWriter.WriteLine($"  self:WriteDebugToFile(\"\", \"\", \"\", \"{otf}\", \"\")");
                    
					streamWriter.WriteLine($"  self:{otf}()");

					if (!outFncs.Contains(otf))
						outFncs.Add(otf);
				}

				streamWriter.WriteLine("end");
			}

			List<string> alreadyProccessed = new();
			foreach (var conn in dominoConnectors)
			{
				foreach (var exec in conn.Value.ExecBoxes)
				{
					if ((exec.Params.Count > 0 || exec.Box.ID.StartsWith("en_")) && !alreadyProccessed.Contains(exec.Box.ID))
					{
						bool wasLocalBox = false;
						string fN = "";

						if (exec.Box.ID.StartsWith("en_"))
						{
							wasLocalBox = true;
							fN = exec.Box.ID;
						}
						else
							fN = "en_" + exec.Box.ID.Replace("self[", "").Replace("]", "");

						streamWriter.WriteLine($"function export:{fN}()");

						Dictionary<string, string> tmpVaBAs = new();
						int tmpIdx = 0;

						string processParamsOutVal(List<DominoDict> listParams)
						{
							string outData = "";

							if (listParams.Any())
							{
								foreach (var ppm in listParams)
								{
									if (ppm.Value != null && ppm.Value.Contains("GetDataOutValue"))
									{
										string bN = ppm.Value.Split(':')[0];
										tmpVaBAs.Add(bN, "l" + tmpIdx.ToString());

										if (dominoBoxes.ContainsKey(bN) && bN.StartsWith("en_"))
										{
											string bn = BuildGraphName(dominoBoxes[bN].Name, debug);

											if (game == "fc6")
												bN = "Boxes[\"" + CRC64.Hash(bn.ToLower().Replace("/", "\\")) + "\"]";
											else
												bN = "Boxes[GetPathID(\"" + bn + "\")]";
										}

										outData += $"  l{tmpIdx} = {bN}{Environment.NewLine}";
										tmpIdx++;
									}

									outData += processParamsOutVal(ppm.ValueArray);
								}
							}

							return outData;
						}

						var varSetBoxes = processParamsOutVal(exec.Params);

						string locDefs = "";
						for (int i = 0; i < tmpIdx + (wasLocalBox && tmpIdx == 0 ? 1 : 0); i++)
							locDefs += ", l" + i.ToString();
						streamWriter.WriteLine($"  local params{locDefs}");

						if (wasLocalBox)
						{
							string bn = BuildGraphName(exec.Box.Name, debug);

							if (game == "fc6")
								streamWriter.WriteLine($"  l0 = Boxes[\"{CRC64.Hash(bn.ToLower().Replace("/", "\\"))}\"]");
							else
								streamWriter.WriteLine($"  l0 = Boxes[GetPathID(\"{bn}\")]");

							streamWriter.WriteLine("  l0:SetParentGraph(self._cbox)");
							streamWriter.WriteLine(ExportConns(exec.Box));
						}

						streamWriter.Write(varSetBoxes);

						string a(string param)
						{
							if (param.Contains("GetDataOutValue"))
							{
								string bN = param.Split(':')[0];
								if (tmpVaBAs.ContainsKey(bN))
									return param.Replace(bN, tmpVaBAs[bN]);
							}

							return param;
						}

						void writeParams(List<DominoDict> listParams, int indent, bool root = false)
						{
							string indentSpaces = "";
							for (int i = 0; i < indent; i++)
								indentSpaces += "  ";

							if (root)
								streamWriter.Write(indentSpaces + "params = {");

							if (listParams.Any())
							{
								indentSpaces += "  ";
								streamWriter.WriteLine("");

								int cnt = 0;
								foreach (var ppm in listParams)
								{
									string cc = cnt != listParams.Count - 1 ? "," : "";

									if (ppm.Name != "")
									{
										bool dIsNum = int.TryParse(ppm.Name, out _);
										streamWriter.Write($"{indentSpaces}{(dIsNum ? "[" : "")}{ppm.Name}{(dIsNum ? "]" : "")} = ");
									}
									else if (ppm.Name == "")
										streamWriter.Write($"{indentSpaces}");

									if (ppm.Value != null)
										streamWriter.WriteLine($"{a(ppm.Value)}{cc}");

									if (ppm.ValueArray.Any())
									{
										streamWriter.Write("{");

										writeParams(ppm.ValueArray, indent + 1);

										streamWriter.WriteLine(indentSpaces + "}" + cc);
									}

									if (!ppm.ValueArray.Any() && ppm.Value == null)
										streamWriter.WriteLine("{}" + cc);

									cnt++;
								}
							}

							if (root)
								streamWriter.WriteLine((listParams.Any() ? "  " : "") + "}");
						}

						writeParams(exec.Params, 1, true);

						/*
						if (exec.Params.Any())
						{
							streamWriter.WriteLine("  params = {");

							for (int j = 0; j < exec.Params.Count; j++)
							{
								if (exec.Params.ElementAt(j).Value.Count() > 1)
								{
									string ov = "";

									for (int i = 0; i < exec.Params.ElementAt(j).Value.Count(); i++)
										ov += (ov != "" ? $"{nl}" : "") + "      " + a(exec.Params.ElementAt(j).Value[i]);

									streamWriter.WriteLine($"    [{exec.Params.ElementAt(j).Key}] = {{{nl}{ov}{nl}    }}{(j != (exec.Params.Count - 1) ? "," : "")}");
								}
								else
									streamWriter.WriteLine($"    [{exec.Params.ElementAt(j).Key}] = {a(exec.Params.ElementAt(j).Value[0])}{(j != (exec.Params.Count - 1) ? "," : "")}");
							}

							streamWriter.WriteLine("  }");
						}
						else
							streamWriter.WriteLine("  params = {}");
						*/

						streamWriter.WriteLine("  return params");
						streamWriter.WriteLine("end");

						alreadyProccessed.Add(exec.Box.ID);
					}
				}
			}

			foreach (var conn in dominoConnectors)
			{
				if (conn.Value.SetVariables.Count > 0)
				{
					var ncf = conn.Value.ID.Replace("f_", "ex_");
                    streamWriter.WriteLine($"function export:{ncf}()");

					Dictionary<string, string> tmpVars = new();
					List<string> tmpVarsSets = new();
					int vi = 0;

					List<string> writeArray(List<DominoDict> arr)
                    {
                        foreach (var svar in arr)
                        {
                            if (svar.Name.Contains(':'))
                            {
                                tmpVars.Add(svar.Name.Split(':')[0], "l" + vi.ToString());
                                vi++;
                            }

                            if (svar.Value != null)
                            {
                                if (svar.Value.Contains(':'))
                                {
                                    var a = tmpVars.TryAdd(svar.Value.Split(':')[0], "l" + vi.ToString());
                                    if (a) vi++;
                                }
                            }
                        }

                        List<string> outVal = new();

                        foreach (var svar in arr)
                        {
                            if (svar.Value != null)
                            {
                                string dSet = (svar.Name == "" ? "" : svar.Name + " = ") + svar.Value;

                                foreach (var tmpVar in tmpVars)
                                    dSet = dSet.Replace(tmpVar.Key, tmpVar.Value);

                                outVal.Add(dSet);
                            }

                            if (svar.ValueArray.Any())
							{
								string v = (svar.Name == "" ? "" : svar.Name + " = ") + "{" + nl + "    " + string.Join("," + nl + "    ", writeArray(svar.ValueArray)) + nl + "  }";
                                outVal.Add(v);
                            }
                        }

						return outVal;
                    }

                    tmpVarsSets.AddRange(writeArray(conn.Value.SetVariables));

					/*foreach (var svar in conn.Value.SetVariables)
					{
						if (svar.Name.Contains(':'))
						{
							tmpVars.Add(svar.Name.Split(':')[0], "l" + vi.ToString());
							vi++;
						}

						if (svar.Value.Contains(':'))
						{
							var a = tmpVars.TryAdd(svar.Value.Split(':')[0], "l" + vi.ToString());
							if (a) vi++;
						}
					}

					foreach (var svar in conn.Value.SetVariables)
					{
						string dSet = svar.Name + " = " + svar.Value;

						foreach (var tmpVar in tmpVars)
							dSet = dSet.Replace(tmpVar.Key, tmpVar.Value);

						tmpVarsSets.Add(dSet);
					}*/
					
					string outVDef = "";

					foreach (var tmpVar in tmpVars)
						outVDef += (outVDef != "" ? ", " : "") + tmpVar.Value;

					if (outVDef != "")
						streamWriter.WriteLine("  local " + outVDef);

					foreach (var tmpVar in tmpVars)
					{
						string bb = tmpVar.Key;

						if (dominoBoxes.ContainsKey(tmpVar.Key) && tmpVar.Key.StartsWith("en_"))
						{
							string bn = BuildGraphName(dominoBoxes[tmpVar.Key].Name, debug);

							if (game == "fc6")
								bb = "Boxes[\"" + CRC64.Hash(bn.ToLower().Replace("/", "\\")) + "\"]";
							else
								bb = "Boxes[GetPathID(\"" + bn + "\")]";
						}

						streamWriter.WriteLine($"  {tmpVar.Value} = {bb}");
                    }

                    foreach (var tmpVarSet in tmpVarsSets)
                    {
                        streamWriter.WriteLine($"  {tmpVarSet}");

						if (debug)
						{
							var aaa = tmpVarSet.Split('=', 2);
                       		streamWriter.WriteLine($"  self:WriteDebugToFile({aaa[0].Trim()}, \"\", \"\", \"{ncf}\", \"{aaa[0].Trim()}\")");
						}
                    }

					streamWriter.WriteLine("end");
				}
			}

			foreach (var otf in outFncs)
			{
				streamWriter.WriteLine($"function export:{otf}()");
				streamWriter.WriteLine("end");
            }

			if (debug)
            	streamWriter.WriteLine(MakeDebugFunction(Path.GetFileName(exportPath)));

            streamWriter.WriteLine("_compilerVersion = 60");
			streamWriter.Close();

			XDocument doc = new();
			doc.Declaration = null;
			doc.Add(ExportDominoMetadata());

			var docData = new MemoryStream();
			XmlWriterSettings xws = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, IndentChars = "\t", Encoding = Encoding.ASCII };
			using (XmlWriter xw = XmlWriter.Create(docData, xws))
				doc.Save(xw);

			byte[] lua = luaData.ToArray();

			if (((bool)settings["bytecode"] && !debug) || ((bool)settings["bytecodeDebug"] && debug))
			{
				byte[] bytecode;

				int ret = LuacLibProcessBytes(lua, lua.Length, out IntPtr buffer, out int bufferSize, exportPath, out IntPtr error);
				if (ret != 0)
                {
                    string str = Marshal.PtrToStringAnsi(error);
					return "Error during parsing Lua:" + Environment.NewLine + str;
                }

                bytecode = new byte[bufferSize];
                Marshal.Copy(buffer, bytecode, 0, bufferSize);

                LuacLibFreeMem(buffer);

				lua = bytecode;
            }

			byte[] meta = docData.ToArray();

			var fileStream = File.Create(exportPath);
			fileStream.Write(BitConverter.GetBytes(0x4341554c), 0, 4);
			fileStream.Write(BitConverter.GetBytes(lua.Length), 0, sizeof(int));
			fileStream.WriteBytes(lua);
			fileStream.WriteBytes(meta);
			fileStream.Close();

			XDocument xDepload;
			XElement xRoot;

			if (File.Exists(exportPathDep))
			{
				xDepload = XDocument.Load(exportPathDep);
				xRoot = xDepload.Element("Root");
			}
			else
			{
				xDepload = new();
				xRoot = new("Root");
				xDepload.Add(xRoot);
			}

			string depFile = datPath + Path.GetFileName(exportPath);

			XElement xCResE = xRoot.Elements("CBinaryResourceContainer").Where(a => a.Attribute("ID").Value == depFile).SingleOrDefault();
			if (xCResE != null)
				xCResE.Remove();

			XElement xCRes = new("CBinaryResourceContainer", new XAttribute("ID", depFile), new XAttribute("addNode", "1"));
			
			foreach (var a in exportDepData)
			{
				string v = a.Key;

				if (v.EndsWith(".bnk"))
					v = "soundbinary\\" + v;

                xCRes.Add(new XElement(a.Value, new XAttribute("ID", v.Replace("/", "\\"))));
            }

			xRoot.Add(xCRes);
            xDepload.Save(exportPathDep);

			return "";
		}

		private string ExportConns(DominoBox box)
		{
			string nl = Environment.NewLine;

			bool SetDynAnchorCountAdd = false;

			string SetDynAnchorCount = "  l0:SetDynAnchorCount({" + nl;
			var execs = dominoConnectors.Values.SelectMany(a => a.ExecBoxes.Where(b => b.Box.ID == box.ID));
			var medi = execs.Where(a => a.Type == ExecType.ExecDynInt).GroupBy(x => x.Exec)
				.Select(x => new
				{
					Count = x.Count(),
					Exec = x.Key,
				}).OrderByDescending(x => x.Count).ToList();

			if (medi.Any())
			{
				SetDynAnchorCountAdd = true;

				SetDynAnchorCount += "    controlIn = {" + nl;

				for (int i = 0; i < medi.Count; i++)
					SetDynAnchorCount += $"      [{medi[i].Exec}] = {medi[i].Count}{(i != (medi.Count - 1) ? "," : "")}{nl}";

				SetDynAnchorCount += "    }," + nl;
			}
			else
				SetDynAnchorCount += "    controlIn = {}," + nl;

			string SetConnections = "";
			if (box.Connections.Count == 0)
				SetConnections += "  l0:SetConnections({})";
			else
			{
				SetConnections += "  l0:SetConnections({" + nl;

				List<string> dynac = new();

				for (int i = 0; i < box.Connections.Count; i++)
				{
					var c = box.Connections[i];

					if (c.SubConnections.Count > 0)
					{
						SetConnections += $"    [{c.FromBoxConnectID}] = {{{nl}";
						SetConnections += $"      connections = {{{nl}";

						for (int j = 0; j < c.SubConnections.Count; j++)
						{
							if (c.SubConnections[j].FromBoxConnectIDStr != "")
							{
								SetConnections += $"        [{c.SubConnections[j].FromBoxConnectID}] = {{{nl}";
								SetConnections += $"          string = \"{c.SubConnections[j].FromBoxConnectIDStr}\",{nl}";
								SetConnections += $"          value = self.{c.SubConnections[j].ID}{nl}";
								SetConnections += $"        }}{(j != (c.SubConnections.Count - 1) ? "," : "")}{nl}";
							}
							else
								SetConnections += $"        [{c.SubConnections[j].FromBoxConnectID}] = self.{c.SubConnections[j].ID}{(j != (c.SubConnections.Count - 1) ? "," : "")}{nl}";
						}

						SetConnections += $"      }},{nl}";
						SetConnections += $"      count = {c.SubConnections.Count}{nl}";
						SetConnections += $"    }}{(i != (box.Connections.Count - 1) ? "," : "")}{nl}";

						dynac.Add($"      [{c.FromBoxConnectID}] = {c.SubConnections.Count}{nl}");
					}
					else
						SetConnections += $"    [{c.FromBoxConnectID}] = self.{c.ID}{(i != (box.Connections.Count - 1) ? "," : "")}{nl}";
				}

				if (dynac.Any())
				{
					SetDynAnchorCountAdd = true;

					SetDynAnchorCount += "    controlOut = {" + nl;

					for (int i = 0; i < dynac.Count; i++)
						SetDynAnchorCount += $"{dynac[i]}{(i != (dynac.Count - 1) ? "," : "")}";

					SetDynAnchorCount += "    }," + nl;
				}
				else
					SetDynAnchorCount += "    controlOut = {}," + nl;

				SetConnections += "  })";
			}

			var vrs = execs.SelectMany(a => a.Params.Where(b => b.ValueArray.Any(c => c.Name.Contains("count"))));
			if (vrs.Any())
			{
				SetDynAnchorCountAdd = true;

				SetDynAnchorCount += "    dataIn = {" + nl;

				for (int i = 0; i < vrs.Count(); i++)
					foreach (var scd in vrs.ElementAt(i).ValueArray)
						if (scd.Name == "count")
							SetDynAnchorCount += $"      [{vrs.ElementAt(i).Name}] = {scd.Value}{(i != (vrs.Count() - 1) ? "," : "")}{nl}";

				SetDynAnchorCount += "    }," + nl;
			}
			else
				SetDynAnchorCount += "    dataIn = {}," + nl;

			SetDynAnchorCount += "    dataOut = {}" + nl;

			SetDynAnchorCount += "  })" + nl;

			string output = "";

			if (SetDynAnchorCountAdd)
				output += SetDynAnchorCount;

			output += SetConnections;

			return output;
		}

		private XElement DataToXML(bool afterDelete, List<Control> UIList = null)
		{
            static XElement saveConn(DominoConnector c)
			{
				XElement xcnt = new("Connector");
				xcnt.Add(new XAttribute("FromBoxConnectID", c.FromBoxConnectID.ToString()));
				xcnt.Add(new XAttribute("FromBoxConnectIDStr", c.FromBoxConnectIDStr.ToString()));

				if (c.SubConnections.Any())
				{
					XElement xsc = new("SubConnections");

					foreach (var sc in c.SubConnections)
						if (sc.ID != null || sc.SubConnections.Any())
							xsc.Add(saveConn(sc));

					xcnt.Add(xsc);
				}
				else
				{
					xcnt.Add(new XAttribute("ID", c.ID));
				}

				return xcnt;
			}

			XElement writeParams(List<DominoDict> prms, string pn, string vn)
			{
				if (prms.Any())
				{
					XElement xprms = new(pn);

					foreach (var prm in prms)
					{
						XElement xPrm = new(vn, new XAttribute("Name", prm.Name));

						if (prm.Value != null)
							xPrm.Add(new XAttribute("Value", prm.Value));

						xPrm.Add(writeParams(prm.ValueArray, pn, vn));

						xprms.Add(xPrm);
					}

					return xprms;
				}

				return null;
			}

			XElement xGraph = new("Graph");

			if (UIList != null)
				xGraph.Add(new XElement("Game", game));

			if (!afterDelete)
			{
				double sX = double.MaxValue;
				double sY = double.MaxValue;

				if (UIList == null)
				{
					xGraph.Add(new XAttribute("Name", dominoGraphs[selGraph].Name));
					xGraph.Add(new XAttribute("UniqueID", dominoGraphs[selGraph].UniqueID));
					xGraph.Add(new XAttribute("IsDefault", dominoGraphs[selGraph].IsDefault ? "true" : "false"));

					xGraph.Add(ExportDominoMetadata());

					XElement xResources = new("Resources");
					foreach (var c in dominoResources)
						xResources.Add(new XElement("Resource", new XAttribute("File", c.Name), new XAttribute("Type", c.Value)));
					xGraph.Add(xResources);

					xGraph.Add(writeParams(globalVariables, "Variables", "Variable"));
				}

				XElement xComments = new("Comments");
				foreach (var c in dominoComments.Values)
				{
					if (UIList != null && !UIList.Contains(c.ContainerUI))
						continue;

					var a = canvas.Transform2(new(Canvas.GetLeft(c.ContainerUI), Canvas.GetTop(c.ContainerUI)));
					xComments.Add(new XElement("Comment", new XAttribute("Name", c.Name), new XAttribute("Color", c.Color), new XAttribute("DrawX", a.X.ToString(CultureInfo.InvariantCulture)), new XAttribute("DrawY", a.Y.ToString(CultureInfo.InvariantCulture))));

					sX = Math.Min(sX, a.X);
					sY = Math.Min(sY, a.Y);
                }
				xGraph.Add(xComments);

				XElement xBorders = new("Borders");
				foreach (var b in dominoBorders.Values)
				{
					if (UIList != null && !UIList.Contains(b.ContainerUI))
						continue;

					var a = canvas.Transform2(new(Canvas.GetLeft(b.ContainerUI), Canvas.GetTop(b.ContainerUI)));
					xBorders.Add(new XElement("Border",
						new XAttribute("Style", b.Style),
						new XAttribute("Color", b.Color),
						new XAttribute("BackgroundColor", b.BackgroundColor),
						new XAttribute("EnableMovingChilds", b.ContainerUI.EnableMovingChilds ? "true" : "false"),
						new XAttribute("DrawX", a.X.ToString(CultureInfo.InvariantCulture)),
						new XAttribute("DrawY", a.Y.ToString(CultureInfo.InvariantCulture)),
						new XAttribute("DrawW", b.ContainerUI.Width.ToString(CultureInfo.InvariantCulture)),
						new XAttribute("DrawH", b.ContainerUI.Height.ToString(CultureInfo.InvariantCulture))
						));

                    sX = Math.Min(sX, a.X);
                    sY = Math.Min(sY, a.Y);
                }
				xGraph.Add(xBorders);

                XElement xPoints = new("Points");
                foreach (var line in lines)
                {
					if (line.UI.Points != null)
                        foreach (var point in line.UI.Points)
                        {
                            if (UIList != null && !UIList.Contains(point))
                                continue;

                            xPoints.Add(new XElement("Point",
                                new XAttribute("From", line.Point1),
                                new XAttribute("To", line.Point2),
                                new XAttribute("DrawX", point.Point.X.ToString(CultureInfo.InvariantCulture)),
                                new XAttribute("DrawY", point.Point.Y.ToString(CultureInfo.InvariantCulture))
                                ));
                        }
                }
                xGraph.Add(xPoints);

                XElement xBoxes = new("Boxes");
				foreach (var c in dominoBoxes)
				{
					if (UIList != null && !UIList.Contains(c.Value.Widget))
						continue;

					var a = canvas.Transform2(new(Canvas.GetLeft(c.Value.Widget), Canvas.GetTop(c.Value.Widget)));

					XElement xBox = new("Box");
					xBox.Add(new XAttribute("ID", c.Value.ID));
					xBox.Add(new XAttribute("Name", c.Value.Name));
					xBox.Add(new XAttribute("DrawX", a.X.ToString(CultureInfo.InvariantCulture)));
					xBox.Add(new XAttribute("DrawY", a.Y.ToString(CultureInfo.InvariantCulture)));

					if (c.Value.Connections.Any())
					{
						XElement xsc = new("Connections");

						foreach (var sc in c.Value.Connections)
						{
							xsc.Add(saveConn(sc));
						}

						xBox.Add(xsc);
					}

					xBoxes.Add(xBox);

                    sX = Math.Min(sX, a.X);
                    sY = Math.Min(sY, a.Y);
                }
				xGraph.Add(xBoxes);

				XElement xConns = new("Connectors");
				foreach (var c in dominoConnectors)
				{
					if (UIList != null && !UIList.Contains(c.Value.Widget))
						continue;

					var a = canvas.Transform2(new(Canvas.GetLeft(c.Value.Widget), Canvas.GetTop(c.Value.Widget)));

					XElement xc = new("Connector");
					xc.Add(new XAttribute("ID", c.Value.ID));
					xc.Add(new XAttribute("DrawX", a.X.ToString(CultureInfo.InvariantCulture)));
					xc.Add(new XAttribute("DrawY", a.Y.ToString(CultureInfo.InvariantCulture)));

					if (c.Value.ExecBoxes.Any())
					{
						XElement xsc = new("ExecBoxes");

						foreach (var sc in c.Value.ExecBoxes)
						{
							XElement xeb = new("ExecBox");
							xeb.Add(new XAttribute("Type", sc.Type == ExecType.Exec ? "Exec" : "DynIntExec"));
							xeb.Add(new XAttribute("Exec", sc.Exec.ToString()));
							xeb.Add(new XAttribute("DynIntExec", sc.DynIntExec.ToString()));
							xeb.Add(new XAttribute("Box", sc.Box.ID));

							xeb.Add(writeParams(sc.Params, "Params", "Param"));

							xsc.Add(xeb);
						}

						xc.Add(xsc);
					}

					if (c.Value.OutFuncName.Any())
					{
						XElement xsc = new("OutFuncName");

						foreach (var sc in c.Value.OutFuncName)
							xsc.Add(new XElement("Function", new XAttribute("Name", sc)));

						xc.Add(xsc);
					}

					/*if (c.Value.FromBoxes.Any())
					{
						XElement xsc = new("FromBoxes");

						foreach (var sc in c.Value.FromBoxes)
							xsc.Add(new XElement("Box", new XAttribute("Name", sc.ID)));

						xc.Add(xsc);
					}*/

					xc.Add(writeParams(c.Value.SetVariables, "Variables", "Variable"));

					/*if (c.SubConnections.Any())
					{
						XElement xsc = new("SubConnections");

						foreach (var sc in c.SubConnections)
							xsc.Add(saveConn(sc));

						xc.Add(xsc);

					}*/

					xConns.Add(xc);

                    sX = Math.Min(sX, a.X);
                    sY = Math.Min(sY, a.Y);
                }
				xGraph.Add(xConns);

                if (UIList != null)
                {
					xGraph.Add(new XElement("SX", sX.ToString(CultureInfo.InvariantCulture)));
					xGraph.Add(new XElement("SY", sY.ToString(CultureInfo.InvariantCulture)));
                }
            }

            return xGraph;
		}

		public string Save(bool afterDelete = false)
		{
			var acb = CheckConnBox();
			if (acb != "")
				return acb;

			if (file == "" || file.EndsWith(".lua"))
			{
                FilePickerSaveOptions opts = new();
                opts.FileTypeChoices = new FilePickerFileType[] { new("Domino Workspace") { Patterns = new[] { "*.domino.xml", "*.domino" } } };
                opts.SuggestedFileName = workspaceName.Replace(" ", "_").ToLower();
                opts.Title = "Save Domino Workspace";

                var d = wnd.StorageProvider.SaveFilePickerAsync(opts).Result;

				if (d != null)
				{
					file = d.Path.LocalPath;
                }
                else
                    return "r";

                /*SaveFileDialog sfd = new();
				sfd.Title = "Save Domino Workspace";
				sfd.Filter = "Domino Workspace|*.domino.xml;*.domino";
				sfd.FileName = workspaceName.Replace(" ", "_").ToLower();
                if (sfd.ShowDialog() == true)
				{
					file = sfd.FileName;
				}
				else
					return "r";*/
			}

			if (wnd != null)
				wnd.Title = MainWindow.appName + " - " + file;

			//canvas.ResetZoom();
			WasEdited(true);

            static XElement saveConn(DominoConnector c)
			{
				XElement xcnt = new("Connector");
				xcnt.Add(new XAttribute("FromBoxConnectID", c.FromBoxConnectID.ToString()));
				xcnt.Add(new XAttribute("FromBoxConnectIDStr", c.FromBoxConnectIDStr.ToString()));

				if (c.SubConnections.Any())
				{
					XElement xsc = new("SubConnections");

					foreach (var sc in c.SubConnections)
						if (sc.ID != null || sc.SubConnections.Any())
							xsc.Add(saveConn(sc));

					xcnt.Add(xsc);
				}
				else
				{
					xcnt.Add(new XAttribute("ID", c.ID));
				}

				return xcnt;
			}

			XElement writeParams(List<DominoDict> prms, string pn, string vn)
			{
				if (prms.Any())
				{
					XElement xprms = new(pn);

					foreach (var prm in prms)
					{
						XElement xPrm = new(vn, new XAttribute("Name", prm.Name));

						if (prm.Value != null)
							xPrm.Add(new XAttribute("Value", prm.Value));

						xPrm.Add(writeParams(prm.ValueArray, pn, vn));

						xprms.Add(xPrm);
					}

					return xprms;
				}

				return null;
			}

			XElement xGraph = DataToXML(afterDelete);

			XElement addEmptyGraph(DominoGraph g)
			{
				XElement xGrN = new("Graph");
				xGrN.Add(new XAttribute("Name", g.Name));
				xGrN.Add(new XAttribute("UniqueID", g.UniqueID));
				xGrN.Add(new XAttribute("IsDefault", g.IsDefault ? "true" : "false"));
				xGrN.Add(new XElement("DominoMetadata", new XAttribute("IsStateless", "0"), new XAttribute("IsSystem", "0"), new XElement("ControlsIn"), new XElement("ControlsOut"), new XElement("DatasIn"), new XElement("DatasOut")));
				xGrN.Add(new XElement("Resources"));
				xGrN.Add(new XElement("Comments"));
				xGrN.Add(new XElement("Borders"));
				xGrN.Add(new XElement("Boxes"));
				xGrN.Add(new XElement("Connectors"));
				return xGrN;
			}

			void saveSett(XElement xRoot)
			{
				XElement xSettings = xRoot.Element("Settings");

                if (xSettings == null)
				{
                    xSettings = new XElement("Settings");
					xRoot.Add(xSettings);
                }

                xSettings.Elements().Remove();
                xSettings.Add(new XElement("SnapToGrid", (bool)settings["snapToGrid"] ? "true" : "false"));
                xSettings.Add(new XElement("UseBezier", (bool)settings["useBezier"] ? "true" : "false"));
                xSettings.Add(new XElement("LinePointsBezier", (bool)settings["linePointsBezier"] ? "true" : "false"));
                xSettings.Add(new XElement("Bytecode", (bool)settings["bytecode"] ? "true" : "false"));
                xSettings.Add(new XElement("BytecodeDebug", (bool)settings["bytecodeDebug"] ? "true" : "false"));
                xSettings.Add(new XElement("ColoredBoxes", (bool)settings["coloredBoxes"] ? "true" : "false"));
            }

            if (File.Exists(file))
			{
				XDocument doc = XDocument.Load(file);
                XElement xRoot = doc.Element("DominoDocument");

				saveSett(xRoot);

                XElement xGraphs = xRoot.Element("Graphs");
				var e = xGraphs.Elements("Graph").Where(a => a.Attribute("UniqueID").Value == dominoGraphs[selGraph].UniqueID).SingleOrDefault();
				e?.Remove();

				if (!afterDelete)
					xGraphs.Add(xGraph);

				foreach (var g in dominoGraphs)
					if (g.UniqueID != dominoGraphs[selGraph].UniqueID && !xGraphs.Elements("Graph").Any(a => a.Attribute("UniqueID").Value == g.UniqueID))
						xGraphs.Add(addEmptyGraph(g));

				doc.Save(file);
			}
			else
			{
				XElement xRoot = new("DominoDocument");
				xRoot.Add(new XElement("Game", game));
				xRoot.Add(new XElement("Name", workspaceName));
				xRoot.Add(new XElement("Path", datPath));

                saveSett(xRoot);

                var xGraphs = new XElement("Graphs");
				xRoot.Add(xGraphs);

				if (!afterDelete)
					xGraphs.Add(xGraph);

				foreach (var g in dominoGraphs)
					if (g.UniqueID != dominoGraphs[selGraph].UniqueID && !xGraphs.Elements("Graph").Any(a => a.Attribute("UniqueID").Value != g.UniqueID))
						xGraphs.Add(addEmptyGraph(g));

				XDocument doc = new();
				doc.Add(xRoot);
				doc.Save(file);
			}

			return "";
		}

		private void XMLToData(XElement xGraph, bool asNew = false)
		{
			var g = xGraph.Element("Game");
			if (g != null)
				if (g.Value != game)
				{
					openInfoDialog("Paste", "Copied data are from different game version. It's not possible to copy from different game versions due to different Domino boxes.");
					return;
				}

			Dictionary<string, string> oldNewBoxes = new();
			Dictionary<string, string> oldNewConns = new();
            Dictionary<string, DominoBox> boxes = new();
			Dictionary<string, DominoConnector> connectors = new();

			List<string> xConnsIDs = new();

			DominoBox getFromBox(string cID)
			{
                bool findFromBox(DominoBox box, List<DominoConnector> conns)
                {
                    foreach (var c in conns)
					{
						string id = cID; //oldNewConns.ContainsKey(cID) ? oldNewConns[cID] : cID;

                        if (c.ID == id)
                            return true;
                        else
                            if (c.SubConnections.Any())
								return findFromBox(box, c.SubConnections);
                    }

                    return false;
                }

                return boxes.Values.Where(a => findFromBox(a, a.Connections)).SingleOrDefault();
			}

			string replaceConnID(string cID, DominoBox parentBox)
			{
				DominoBox bf = null;

                if (asNew)
                {
					string boxID = "";

					bf = getFromBox(cID);

                    if (bf != null)
                    {
						boxID = bf.ID;
					}
					else if (parentBox != null)
                    {
						boxID = parentBox.ID;
                    }

                    string newID = boxID.Replace("self[", "").Replace("]", "").Replace("en_", "");
					var newCID = cID;

                    if (cID != null)
                    {
						if (newCID.Contains('_') && newCID.StartsWith("f_") && newCID.Count(c => c == '_') > 1)
                        	newCID = string.Concat(newID.ToString(), cID.AsSpan(cID.IndexOf('_', cID.IndexOf('_') + 1)));
						else if (newCID.Contains("_") && newCID.StartsWith("f_") && newCID.Count(c => c == '_') == 1)
                            newCID = newID.ToString() + cID.Substring(cID.IndexOf('_', cID.IndexOf('_')));

                        newCID = "f_" + FindConnectorFreeID(newCID, connectors.Values.ToList());

						if (!oldNewConns.ContainsKey(cID))
							oldNewConns.Add(cID, newCID);
                    }

					cID = newCID;
                }

				return cID;
            }

			void loadConn(XElement parent, DominoBox box, DominoConnector parentConn)
			{
				var xConns = parent.Elements("Connector");
				foreach (var xConn in xConns)
				{
					string id = xConn.Attribute("ID")?.Value;

                    DominoConnector conn = new();
					conn.FromBoxConnectID = int.Parse(xConn.Attribute("FromBoxConnectID").Value);
					conn.FromBoxConnectIDStr = xConn.Attribute("FromBoxConnectIDStr").Value;
					conn.ID = replaceConnID(id, box);

					if (!asNew || (asNew && xConnsIDs.Contains(id)) || (asNew && id == null))
                    {
                        if (conn.ID != null)
                            connectors.Add(conn.ID, conn);

                        if (parentConn == null)
                            box.Connections.Add(conn);
                        else
                            parentConn.SubConnections.Add(conn);
                    }

                    var xSubConnections = xConn.Element("SubConnections");
					if (xSubConnections != null)
					{
						loadConn(xSubConnections, box, conn);
					}
				}
			}

			List<DominoDict> readParams(XElement parent, string pn, string vn)
			{
				List<DominoDict> prms = new();

				var xParams = parent.Element(pn)?.Elements(vn);
				if (xParams != null)
				{
					foreach (var xParam in xParams)
					{
						DominoDict prm = new();
						prm.Name = xParam.Attribute("Name").Value;

						string val = xParam.Attribute("Value")?.Value;
						if (val != null)
						{
							if (val.Contains(":GetDataOutValue(") && asNew)
							{
								string p = val.Split(':')[0];
								if (oldNewBoxes.ContainsKey(p))
                                {
                                    var oldNewID = oldNewBoxes[p];
                                    val = val.Replace(p, oldNewID);
                                }
							}

                            prm.Value = val;
                        }

						prm.ValueArray = readParams(xParam, pn, vn);

						prms.Add(prm);
					}
				}

				return prms;
			}

            if (!asNew)
                globalVariables = readParams(xGraph, "Variables", "Variable");

			double sX = 0;
			double sY = 0;
			double mX = 0;
			double mY = 0;

			if (asNew)
			{
				sX = double.Parse(xGraph.Element("SX").Value, CultureInfo.InvariantCulture);
				sY = double.Parse(xGraph.Element("SY").Value, CultureInfo.InvariantCulture);

                var mp = canvas.Transform3(canvas.CurrentMousePos);
				mX = mp.X;
				mY = mp.Y;
            }

			var xComments = xGraph.Element("Comments").Elements("Comment");
			foreach (var xC in xComments)
			{
				var c = new DominoComment();
				c.Name = xC.Attribute("Name").Value;
				c.Color = int.Parse(xC.Attribute("Color").Value);
                dominoComments.Add(c.UniqueID, c);

				double x = double.Parse(xC.Attribute("DrawX").Value, CultureInfo.InvariantCulture);
				double y = double.Parse(xC.Attribute("DrawY").Value, CultureInfo.InvariantCulture);

				if (asNew)
				{
					x -= sX;
					y -= sY;
				}

                var np = canvas.Transform4(new(x, y));

                if (asNew)
                {
					np += new Point(mX, mY);
                }

                DrawComment(c, np.X, np.Y);
			}

			var xBorders = xGraph.Element("Borders").Elements("Border");
			foreach (var xB in xBorders)
			{
				var b = new DominoBorder();
				b.Style = int.Parse(xB.Attribute("Style").Value);
				b.Color = int.Parse(xB.Attribute("Color").Value);
				b.BackgroundColor = int.Parse(xB.Attribute("BackgroundColor").Value);
                dominoBorders.Add(b.UniqueID, b);

				bool moveChilds = xB.Attribute("EnableMovingChilds").Value == "true";

				double x = double.Parse(xB.Attribute("DrawX").Value, CultureInfo.InvariantCulture);
				double y = double.Parse(xB.Attribute("DrawY").Value, CultureInfo.InvariantCulture);
				double w = double.Parse(xB.Attribute("DrawW").Value, CultureInfo.InvariantCulture);
				double h = double.Parse(xB.Attribute("DrawH").Value, CultureInfo.InvariantCulture);

                if (asNew)
                {
                    x -= sX;
                    y -= sY;
                }

                var np = canvas.Transform4(new(x, y));

                if (asNew)
                {
                    np += new Point(mX, mY);
                }

                DrawBorder(b, np.X, np.Y, w, h, moveChilds);
			}

            var xConnectors = xGraph.Element("Connectors").Elements("Connector");
			foreach (var xConn in xConnectors)
				xConnsIDs.Add(xConn.Attribute("ID").Value);

            var xBoxes = xGraph.Element("Boxes").Elements("Box");
			foreach (var xBox in xBoxes)
			{
				string origID = xBox.Attribute("ID").Value;

				if (asNew)
				{
					int newBoxID = FindBoxFreeID(0, boxes.Values.ToList());

					string ni = "";
					if (origID.StartsWith("self["))
						ni = $"self[{newBoxID}]";
					else
						ni = $"en_{newBoxID}";

					oldNewBoxes.Add(origID, ni);

					origID = ni;
				}

				DominoBox box = new();
				box.ID = origID;
				box.Name = xBox.Attribute("Name").Value;
				box.DrawX = double.Parse(xBox.Attribute("DrawX").Value, CultureInfo.InvariantCulture);
				box.DrawY = double.Parse(xBox.Attribute("DrawY").Value, CultureInfo.InvariantCulture);
				boxes.Add(box.ID, box);

                XElement xConnections = xBox.Element("Connections");
				if (xConnections != null)
				{
					loadConn(xConnections, box, null);
				}

				/*if (asNew)
                {
                    box.DrawX -= sX;
                    box.DrawY -= sY;

                    var np = canvas.Transform4(new(box.DrawX, box.DrawY));

                    np += new Point(mX, mY);

                    DrawBox(box, np.X, np.Y);
                }*/
            }

            foreach (var a in boxes)
                dominoBoxes.Add(a.Key, a.Value);

			foreach (var xConnector in xConnectors)
			{
				DominoConnector conn = null;

				string gotID = xConnector.Attribute("ID").Value;
				string cID = "";

				if (oldNewConns.ContainsKey(gotID))
					cID = oldNewConns[gotID];
				else
					cID = replaceConnID(gotID, null);

                if (connectors.ContainsKey(cID))
					conn = connectors[cID];
				else
                {
                    conn = new();

					if (asNew)
					{
                        conn.FromBoxConnectID = -1;
                        conn.FromBoxConnectIDStr = "";
                    }
                }

				conn.ID = cID;
				conn.DrawX = double.Parse(xConnector.Attribute("DrawX").Value, CultureInfo.InvariantCulture);
				conn.DrawY = double.Parse(xConnector.Attribute("DrawY").Value, CultureInfo.InvariantCulture);

                conn.SetVariables = readParams(xConnector, "Variables", "Variable");

                var xOutFuncs = xConnector.Element("OutFuncName")?.Elements("Function");
                if (xOutFuncs != null)
                {
                    foreach (XElement xOutFunc in xOutFuncs)
                        conn.OutFuncName.Add(xOutFunc.Attribute("Name").Value);
                }
                
				if (!connectors.ContainsKey(cID))
                {
                    connectors.Add(conn.ID, conn);
                }

                /*if (asNew)
                {
                    conn.DrawX -= sX;
                    conn.DrawY -= sY;

                    var np = canvas.Transform4(new(conn.DrawX, conn.DrawY));

                    np += new Point(mX, mY);

                    DrawConnector(conn, np.X, np.Y);
                }*/

                var xExecBoxes = xConnector.Element("ExecBoxes")?.Elements("ExecBox");
				if (xExecBoxes != null)
				{
					foreach (XElement xExecBox in xExecBoxes)
					{
						string id = xExecBox.Attribute("Box").Value;

						if (asNew)
						{
							if (oldNewBoxes.ContainsKey(id))
								id = oldNewBoxes[id];
						}

						var box = dominoBoxes.Values.Where(a => a.ID == id).SingleOrDefault();
						if (box != null)
						{
                            int cc = int.Parse(xExecBox.Attribute("DynIntExec").Value);

                            if (!oldNewBoxes.ContainsKey(box.ID) && asNew)
                            {
                                cc = FindDynIntFreeNum(box.ID, connectors.Values.ToList());
                            }

                            ExecBox execBox = new();
							execBox.Type = xExecBox.Attribute("Type").Value == "Exec" ? ExecType.Exec : ExecType.ExecDynInt;
							execBox.Exec = int.Parse(xExecBox.Attribute("Exec").Value);
							execBox.DynIntExec = cc;
							execBox.Box = box;
							if (asNew) execBox.ExecStr = regBoxesAll[box.Name].ControlsIn[execBox.Exec].Name;
                            conn.ExecBoxes.Add(execBox);

							execBox.Params = readParams(xExecBox, "Params", "Param");
							if (asNew)
                            {
                                int clr = conn.ExecBoxes.Count - 1;

                                //DrawExecBoxContainerUI(conn, execBox, clr);
                            }
						}
					}
				}
			}

            foreach (var a in connectors)
                dominoConnectors.Add(a.Key, a.Value);

			if (asNew)
            {
				/*foreach (var b in boxes.Values)
				{
					foreach (var c in b.Connections)
                    {
                        DrawBoxConnectors(b, c);
                    }
				}*/

            	foreach (var conn in connectors.Values)
				{
                    conn.DrawX -= sX;
                    conn.DrawY -= sY;

                    var np = canvas.Transform4(new(conn.DrawX, conn.DrawY));

                    np += new Point(mX, mY);

					NewDrawConnector(conn, np.X, np.Y);
				}

            	foreach (var box in boxes.Values)
                {
                    box.DrawX -= sX;
                    box.DrawY -= sY;

                    var np = canvas.Transform4(new(box.DrawX, box.DrawY));

                    np += new Point(mX, mY);

                    NewDrawBox(box, np.X, np.Y);
                }

                foreach (var c in dominoConnectors.Values)
                    AddConnectorLines(c, 0);

                foreach (var b in dominoBoxes.Values)
                    AddBoxLines(b, 0);
            }

            var xPoints = xGraph.Element("Points")?.Elements("Point");
			if (xPoints != null)
				foreach (var xPoint in xPoints)
				{
					string from = xPoint.Attribute("From").Value;
					string to = xPoint.Attribute("To").Value;

					if (asNew)
                    {
                        foreach (var a in oldNewBoxes)
                        {
                            from = from.Replace(a.Key, a.Value);
                            to = to.Replace(a.Key, a.Value);
                        }

                        foreach (var a in oldNewConns)
                        {
                            from = from.Replace(a.Key, a.Value);
                            to = to.Replace(a.Key, a.Value);
                        }
                    }

                    loadPoints.Add(new()
					{
						From = from,
						To = to,
						DrawX = double.Parse(xPoint.Attribute("DrawX").Value, CultureInfo.InvariantCulture),
						DrawY = double.Parse(xPoint.Attribute("DrawY").Value, CultureInfo.InvariantCulture)
					});
                }

			if (asNew)
            {
                foreach (var lp in loadPoints)
                {
                    var np = canvas.Transform4(new(lp.DrawX - sX, lp.DrawY - sY));

                    np += new Point(mX, mY);

                    var npp = canvas.Transform2(np);
	
                    foreach (var line in lines)
                        if (line.Point1 == lp.From && line.Point2 == lp.To)
                            DrawLinePoint(line, npp.X, npp.Y, np.X, np.Y);
                }

                loadPoints.Clear();
            }
        }

        public string Load(string loadGraphID = "")
		{
			XDocument xDoc = XDocument.Load(file);
			XElement xRoot = xDoc.Element("DominoDocument");

			game = xRoot.Element("Game").Value;
			workspaceName = xRoot.Element("Name").Value;
			datPath = xRoot.Element("Path").Value;

            XElement xSettings = xRoot.Element("Settings");
			if (xSettings != null)
            {
                settings["snapToGrid"] = xSettings.Element("SnapToGrid").Value == "true";
                settings["useBezier"] = xSettings.Element("UseBezier").Value == "true";
                settings["linePointsBezier"] = xSettings.Element("LinePointsBezier").Value == "true";
                settings["bytecode"] = xSettings.Element("Bytecode").Value == "true";
                settings["bytecodeDebug"] = xSettings.Element("BytecodeDebug").Value == "true";
                settings["coloredBoxes"] = xSettings.Element("ColoredBoxes")?.Value == "true";

                UseSettings(false);
            }

			IEnumerable<XElement> xGraphs = xRoot.Element("Graphs").Elements("Graph");
			XElement xGraph = null;

			int cnt = 0;
			foreach (var g in xGraphs)
			{
				DominoGraph graph = new();
				graph.Name = g.Attribute("Name").Value;
				graph.UniqueID = g.Attribute("UniqueID").Value;
				graph.IsDefault = g.Attribute("IsDefault").Value == "true";
				graph.Metadata = ImportDominoMetadata(g.Element("DominoMetadata"));
				graph.Metadata.INT_Graph = true;
				dominoGraphs.Add(graph);

				if (loadGraphID != "" && graph.UniqueID == loadGraphID)
				{
					xGraph = g;
					selGraph = cnt;
				}
				
				if (graph.IsDefault && loadGraphID == "")
				{
					xGraph = g;
					selGraph = cnt;

					if (graph.Metadata.IsSystem)
						return "System Domino box can't be opened.";
				}

				/*if (!graph.IsDefault)
					AddGraphBox(graph);*/

				IEnumerable<XElement> boxes = g.Element("Boxes")?.Elements("Box");
				if (boxes != null)
					foreach (var b in boxes)
					{
						graph.ContainsBoxes.Add(b.Attribute("Name").Value);
					}

				cnt++;
			}

			ParseAllBoxes();

			/*string dmla = ImportDominoMetadata(xGraph.Element("DominoMetadata"));
			if (dmla != "")
				return dmla;*/

			/*dominoGraphs[selGraph].Metadata = ImportDominoMetadata(xGraph.Element("DominoMetadata"));
			if (dominoGraphs[selGraph].Metadata.IsSystem)
				return "System Domino box can't be opened.";*/

			/*int ctrlPosY = 0;
			foreach (var ctrl in dominoGraphs[selGraph].Metadata.ControlsIn)
			{
				DominoConnector inConn = new();
				inConn.ID = ctrl.Name;
				inConn.DrawY = ctrlPosY;
				dominoConnectors.Add(inConn.ID, inConn);
				ctrlPosY += 300;
			}*/

			var xRess = xGraph.Element("Resources").Elements("Resource");
			foreach (var xRe in xRess)
				dominoResources.Add(new() { Name = xRe.Attribute("File").Value, Value = xRe.Attribute("Type").Value });

			XMLToData(xGraph);

            foreach (var graph in dominoGraphs)
            {
                if (!graph.IsDefault)
                    AddGraphBox(graph);
            }

            foreach (var box in dominoBoxes)
			{
				if (!regBoxesAll.ContainsKey(box.Value.Name))
					LoadReqBoxes(box.Value.Name);

				testUniqueBoxID.Add(int.Parse(box.Value.ID.Replace("self[", "").Replace("]", "").Replace("en_", "")), true);
            }
			testUniqueBoxID.Clear();

            foreach (var c in dominoConnectors)
			{
				foreach (var e in c.Value.ExecBoxes)
				{
					if (regBoxesAll.ContainsKey(e.Box.Name))
						e.ExecStr = e.Exec < regBoxesAll[e.Box.Name].ControlsIn.Count ? regBoxesAll[e.Box.Name].ControlsIn[e.Exec].Name : "EXEC DOESN'T EXIST";
				}
			}

            Draw(true);

			foreach (var lp in loadPoints)
                foreach (var line in lines)
                    if (line.Point1 == lp.From && line.Point2 == lp.To)
						DrawLinePoint(line, lp.DrawX, lp.DrawY, lp.DrawX, lp.DrawY);

			loadPoints.Clear();

			canvas.RefreshChilds();

            SetWorkspaceNameAndGraphs();

			if (errFilesB)
				return errFiles;

			return "";
		}
	}
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ME3Explorer.ActorNodes;
using ME3Explorer.Packages;
using ME3Explorer.PathfindingNodes;
using ME3Explorer.SharedUI;
using ME3Explorer.SplineNodes;
using ME3Explorer.Unreal;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UMD.HCIL.PathingGraphEditor;
using UMD.HCIL.Piccolo;
using UMD.HCIL.Piccolo.Event;
using UMD.HCIL.Piccolo.Nodes;
using static ME3Explorer.PathfindingEditor;

namespace ME3Explorer.Pathfinding_Editor
{
    /// <summary>
    /// Interaction logic for PathfindingEditorWPF.xaml
    /// </summary>
    public partial class PathfindingEditorWPF : WPFBase
    {

        public static string[] pathfindingNodeClasses = { "PathNode", "SFXEnemySpawnPoint", "PathNode_Dynamic", "SFXNav_HarvesterMoveNode", "SFXNav_LeapNodeHumanoid", "MantleMarker", "TargetPoint", "BioPathPoint", "SFXNav_LargeBoostNode", "SFXNav_LargeMantleNode", "SFXNav_InteractionStandGuard", "SFXNav_TurretPoint", "CoverLink", "SFXDynamicCoverLink", "SFXDynamicCoverSlotMarker", "SFXNav_SpawnEntrance", "SFXNav_LadderNode", "SFXDoorMarker", "SFXNav_JumpNode", "SFXNav_JumpDownNode", "NavigationPoint", "CoverSlotMarker", "SFXOperation_ObjectiveSpawnPoint", "SFXNav_BoostNode", "SFXNav_LargeClimbNode", "SFXNav_LargeMantleNode", "SFXNav_ClimbWallNode",
                "SFXNav_InteractionHenchOmniTool", "SFXNav_InteractionHenchOmniToolCrouch", "SFXNav_InteractionHenchBeckonFront", "SFXNav_InteractionHenchBeckonRear", "SFXNav_InteractionHenchCustom", "SFXNav_InteractionHenchCover", "SFXNav_InteractionHenchCrouch", "SFXNav_InteractionHenchInteractLow", "SFXNav_InteractionHenchManual", "SFXNav_InteractionHenchStandIdle", "SFXNav_InteractionHenchStandTyping", "SFXNav_InteractionUseConsole", "SFXNav_InteractionStandGuard", "SFXNav_InteractionHenchOmniToolCrouch", "SFXNav_InteractionInspectWeapon", "SFXNav_InteractionOmniToolScan" };
        public static string[] actorNodeClasses = { "BlockingVolume", "DynamicBlockingVolume", "StaticMeshActor", "SFXMedStation", "InterpActor", "SFXDoor", "BioTriggerVolume", "SFXArmorNode", "BioTriggerStream", "SFXTreasureNode", "SFXPointOfInterest", "SFXPlaceable_Generator", "SFXPlaceable_ShieldGenerator", "SFXBlockingVolume_Ledge", "SFXAmmoContainer_Simulator", "SFXAmmoContainer", "SFXGrenadeContainer", "SFXCombatZone", "BioStartLocation", "BioStartLocationMP", "SFXStuntActor", "SkeletalMeshActor", "WwiseAmbientSound", "WwiseAudioVolume" };
        public static string[] splineNodeClasses = { "SplineActor" };
        public static string[] ignoredobjectnames = { "PREFAB_Ladders_3M_Arc0", "PREFAB_Ladders_3M_Arc1" }; //These come up as parsed classes but aren't actually part of the level, only prefabs. They should be ignored

        //Layers
        public bool PathfindingNodesActive = true;
        public bool ActorNodesActive;
        public bool StaticMeshCollectionActorNodesActive;
        private bool SplineNodesActive;
        private bool EverythingElseActive;

        private readonly PathingGraphEditor graphEditor;
        private bool AllowRefresh;
        private PathingZoomController zoomController;
        private readonly string classDatabasePath;
        public static Dictionary<string, Dictionary<string, string>> importclassdb = new Dictionary<string, Dictionary<string, string>>(); //SFXGame.Default__SFXEnemySpawnPoint -> class, packagefile (can infer link and name)
        public static Dictionary<string, Dictionary<string, string>> exportclassdb = new Dictionary<string, Dictionary<string, string>>(); //SFXEnemy SpawnPoint -> class, name, ...etc
        public ObservableCollectionExtended<IExportEntry> ActiveNodes { get; set; } = new ObservableCollectionExtended<IExportEntry>();
        public ObservableCollectionExtended<string> TagsList { get; set; } = new ObservableCollectionExtended<string>();

        private List<IExportEntry> AllLevelObjects = new List<IExportEntry>();
        public string CurrentFile;
        private PathfindingMouseListener pathfindingMouseListener;

        public PathfindingEditorWPF()
        {
            ME3ExpMemoryAnalyzer.MemoryAnalyzer.AddTrackedMemoryItem("Pathfinding Editor WPF", new WeakReference(this));
            DataContext = this;

            StatusText = "Select package file to load";
            LoadCommands();
            InitializeComponent();

            graphEditor = (PathingGraphEditor)GraphHost.Child;
            graphEditor.BackColor = System.Drawing.Color.FromArgb(130, 130, 130);
            AllowRefresh = true;



            classDatabasePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "exec", "pathfindingclassdb.json");

            //pathfindingMouseListener = new PathfindingMouseListener(this); //Must be member so we can release reference

            //Stuff that can't be done in designer view easily
            //showVolumesInsteadOfNodesToolStripMenuItem.DropDown.Closing += new ToolStripDropDownClosingEventHandler(DropDown_Closing);
            //ViewingModesMenuItem.DropDown.Closing += new ToolStripDropDownClosingEventHandler(DropDown_Closing);
            //sFXCombatZonesToolStripMenuItem.DropDown.Closing += new ToolStripDropDownClosingEventHandler(DropDown_Closing);
            //staticMeshCollectionActorsToolStripMenuItem.DropDown.Closing += new ToolStripDropDownClosingEventHandler(DropDown_Closing);

            //

            LoadRecentList();
            RefreshRecent(false);
            //pathfindingNodeInfoPanel.PassPathfindingNodeEditorIn(this);

            //graphEditor.AddInputEventListener(pathfindingMouseListener);

            //graphEditor.Click += graphEditor_Click;
            //graphEditor.DragDrop += PathfindingEditor_DragDrop;
            //graphEditor.DragEnter += PathfindingEditor_DragEnter;

            zoomController = new PathingZoomController(graphEditor);
            //CurrentFilterType = HeightFilterForm.FILTER_Z_NONE;
            //CurrentZFilterValue = 0;


            if (importclassdb.Count() == 0 || exportclassdb.Count() == 0)
            {

                if (File.Exists(classDatabasePath))
                {

                    string raw = File.ReadAllText(classDatabasePath);
                    JObject o = JObject.Parse(raw);
                    JToken exportjson = o.SelectToken("exporttypes");
                    JToken importjson = o.SelectToken("importtypes");
                    exportclassdb = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(exportjson.ToString());
                    importclassdb = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(importjson.ToString());
                }

            }

            InitializeComponent();
            PathfindingEditorWPF_ReachSpecsPanel.ExportsDB = exportclassdb;
            pathfindingMouseListener = new PathfindingMouseListener(this); //Must be member so we can release reference
            graphEditor.AddInputEventListener(pathfindingMouseListener);
        }

        #region Properties and Bindings

        public ICommand RefreshCommand { get; set; }
        public ICommand FocusGotoCommand { get; set; }
        public ICommand FocusFindCommand { get; set; }
        public ICommand OpenCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand SaveAsCommand { get; set; }

        private void LoadCommands()
        {
            RefreshCommand = new RelayCommand(RefreshGraph, PackageIsLoaded);
            FocusGotoCommand = new RelayCommand(FocusGoto, PackageIsLoaded);
            FocusFindCommand = new RelayCommand(FocusFind, PackageIsLoaded);
            OpenCommand = new RelayCommand(OpenPackage, (o) => { return true; });
            SaveCommand = new RelayCommand(SavePackage, PackageIsLoaded);
            SaveAsCommand = new RelayCommand(SavePackageAs, PackageIsLoaded);
        }

        private void SavePackageAs(object obj)
        {
            string extension = System.IO.Path.GetExtension(Pcc.FileName);
            SaveFileDialog d = new SaveFileDialog { Filter = $"*{extension}|*{extension}" };
            if (d.ShowDialog() == true)
            {
                Pcc.save(d.FileName);
                MessageBox.Show("Done.");
            }
        }

        public void FocusNode(IExportEntry node, bool select)
        {
            PathfindingNodeMaster s = GraphNodes.FirstOrDefault(o => o.UIndex == node.UIndex); //Will change to uindex eventually
            if (s != null)
            {
                if (select)
                {
                    var selectedNodeCurrently = ActiveNodes_ListBox.SelectedItem;
                    ActiveNodes_ListBox.SelectedItem = node;
                    if (selectedNodeCurrently == node)
                    {
                        ActiveNodesList_SelectedItemChanged(null, null); //Animate
                    }
                }
                else
                {
                    graphEditor.Camera.AnimateViewToCenterBounds(s.GlobalFullBounds, false, 1000);
                }
            }
        }

        private void SavePackage(object obj)
        {
            Pcc.save();
        }

        private void OpenPackage(object obj)
        {
            OpenFileDialog d = new OpenFileDialog { Filter = App.FileFilter };
            if (d.ShowDialog() == true)
            {
#if !DEBUG
                try
                {
#endif
                LoadFile(d.FileName);
#if !DEBUG
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to open file:\n" + ex.Message);
                }
#endif
            }
        }

        private bool PackageIsLoaded(object obj)
        {
            return Pcc != null;
        }

        private void RefreshGraph(object obj)
        {
            RefreshGraph();
        }

        private void RefreshGraph()
        {
            if (AllowRefresh)
            {
                IExportEntry currentSelectedItem = (IExportEntry)ActiveNodes_ListBox.SelectedItem;
                graphEditor.nodeLayer.RemoveAllChildren();
                graphEditor.edgeLayer.RemoveAllChildren();
                ActiveNodes.ClearEx();
                LoadPathingNodesFromLevel();
                GenerateGraph();
            }
        }

        private void FocusGoto(object obj)
        {
            FindByNumber_TextBox.Focus();
        }

        private void FocusFind(object obj)
        {
            FindByTag_ComboBox.Focus();
        }

        private string _nodeName;
        public string NodeName
        {
            get => _nodeName;
            set => SetProperty(ref _nodeName, value);
        }

        private string _nodeNameSubText;
        public string NodeNameSubText
        {
            get => _nodeNameSubText;
            set => SetProperty(ref _nodeNameSubText, value);
        }

        public string NodeTypeDescriptionText
        {
            get
            {
                if (ActiveNodes_ListBox != null && ActiveNodes_ListBox.SelectedItem is IExportEntry CurrentLoadedExport)
                {
                    if (exportclassdb.TryGetValue(CurrentLoadedExport.ClassName, out var classinfo) && classinfo.TryGetValue("description", out var description))
                    {
                        return description;
                    }
                    switch (CurrentLoadedExport.ClassName)
                    {
                        case "PathNode": return "A basic pathing node that all basic movement can use.";
                        case "SFXNav_LargeBoostNode": return "A node that allows large creatures to boost to another LargeBoostNode, such as a Banshee floating up or down vertical distances.";
                        case "SFXNav_TurretPoint": return "A basic pathing node that a Cerberus Engineer can place a turret at.";

                        case "CoverSlotMarker": return "A node where AI can take cover. It is owned by a CoverLink and is part of a chain of continuous CoverSlotMarkers.";
                        case "BioPathPoint": return "A basic pathing node that can be enabled or disabled in Kismet.";
                        case "SFXEnemySpawnPoint": return "A basic pathing node that can be used as a spawn point for Mass Effect 3 Multiplayer enemies. It contains a list of required sync actions that using this spawn point will require to enter the main area of the map.";
                        case "SFXNav_LargeMantleNode": return "A node that can be large mantled over to reach another large mantle node. This action is used when climbing over large cover by AI.";
                        default: return "This node type does not have any information detailed about it's purpose.";
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        /// <summary>
        /// Called from winforms graph
        /// </summary>
        public void OpenContextMenu()
        {
            ContextMenu contextMenu = this.FindResource("nodeContextMenu") as ContextMenu;
            contextMenu.IsOpen = true;
            graphEditor.DisableDragging();
        }

        private void LoadFile(string fileName)
        {
            LoadMEPackage(fileName);
            ActiveNodes.ClearEx();

            PersisentLevelExport = Pcc.Exports.FirstOrDefault(x => x.ClassName == "Level" && x.ObjectName == "PersistentLevel");
            if (PersisentLevelExport == null)
            {
                Pcc.Release();
                Pcc = null;
                MessageBox.Show("This file does not contain a Level export.");
                return;
            }

            StatusText = System.IO.Path.GetFileName(fileName);
            graphEditor.nodeLayer.RemoveAllChildren();
            graphEditor.edgeLayer.RemoveAllChildren();

            if (LoadPathingNodesFromLevel())
            {
                PointF graphcenter = GenerateGraph();
                ChangingSelectionByGraphClick = true;
                ActiveNodes_ListBox.SelectedIndex = 0;
                CurrentFile = System.IO.Path.GetFileName(fileName);
                RectangleF panToRectangle = new RectangleF(graphcenter, new SizeF(2000, 2000));
                graphEditor.Camera.AnimateViewToCenterBounds(panToRectangle, false, 1000);
                ChangingSelectionByGraphClick = false;

                AddRecent(fileName, false);
                SaveRecentList();
                RefreshRecent(true, RFiles);
                Title = "Pathfinding Editor WPF - " + fileName;

                //ActiveNodesList_SelectedItemChanged(null, null);
            }
            else
            {
                CurrentFile = null;
            }
        }

        private bool LoadPathingNodesFromLevel()
        {
            if (Pcc == null || PersisentLevelExport == null)
            {
                return false;
            }

            /*staticMeshCollectionActorsToolStripMenuItem.DropDownItems.Clear();
            staticMeshCollectionActorsToolStripMenuItem.Enabled = false;
            staticMeshCollectionActorsToolStripMenuItem.ToolTipText = "No StaticMeshCollectionActors found in this file";
            sFXCombatZonesToolStripMenuItem.DropDownItems.Clear();
            sFXCombatZonesToolStripMenuItem.Enabled = false;
            sFXCombatZonesToolStripMenuItem.ToolTipText = "No SFXCombatZones found in this file";
            sfxCombatZones = new List<int>();
            CurrentObjects = new List<int>();
            activeExportsListbox.Items.Clear();
            AllLevelObjects.Clear();*/
            //Read persistent level binary
            byte[] data = PersisentLevelExport.Data;

            //find start of class binary (end of props)
            int start = PersisentLevelExport.propsEnd();

            //Console.WriteLine("Found start of binary at " + start.ToString("X8"));

            uint exportid = BitConverter.ToUInt32(data, start);
            start += 4;
            uint numberofitems = BitConverter.ToUInt32(data, start);
            int countoffset = start;

            start += 4;
            int bioworldinfoexportid = BitConverter.ToInt32(data, start);

            IExportEntry bioworldinfo = Pcc.getUExport(bioworldinfoexportid);
            if (bioworldinfo.ObjectName != "BioWorldInfo")
            {
                //INVALID!!
                return false;
            }
            AllLevelObjects.Add(bioworldinfo);

            start += 4;
            uint shouldbezero = BitConverter.ToUInt32(data, start);
            if (shouldbezero != 0 && Pcc.Game != MEGame.ME1)
            {
                //INVALID!!!
                return false;
            }
            int itemcount = 1; //Skip bioworldinfo and Class
            if (Pcc.Game != MEGame.ME1)
            {
                start += 4;
                itemcount = 2;
            }
            List<IExportEntry> bulkActiveNodes = new List<IExportEntry>();
            while (itemcount < numberofitems)
            {
                //get header.
                int itemexportid = BitConverter.ToInt32(data, start);
                if (itemexportid - 1 < Pcc.Exports.Count)
                {
                    IExportEntry exportEntry = Pcc.getUExport(itemexportid);
                    AllLevelObjects.Add(exportEntry);

                    if (ignoredobjectnames.Contains(exportEntry.ObjectName))
                    {
                        start += 4;
                        itemcount++;
                        continue;
                    }

                    bool isParsedByExistingLayer = false;

                    if (pathfindingNodeClasses.Contains(exportEntry.ClassName))
                    {
                        isParsedByExistingLayer = true;
                        if (PathfindingNodesActive)
                        {
                            bulkActiveNodes.Add(exportEntry);
                        }
                    }

                    if (actorNodeClasses.Contains(exportEntry.ClassName))
                    {
                        isParsedByExistingLayer = true;
                        if (ActorNodesActive)
                        {
                            bulkActiveNodes.Add(exportEntry);
                        }
                    }

                    if (splineNodeClasses.Contains(exportEntry.ClassName))
                    {
                        isParsedByExistingLayer = true;

                        if (SplineNodesActive)
                        {
                            bulkActiveNodes.Add(exportEntry);
                            ArrayProperty<StructProperty> connectionsProp = exportEntry.GetProperty<ArrayProperty<StructProperty>>("Connections");
                            if (connectionsProp != null)
                            {
                                foreach (StructProperty connectionProp in connectionsProp)
                                {
                                    ObjectProperty splinecomponentprop = connectionProp.GetProp<ObjectProperty>("SplineComponent");
                                    bulkActiveNodes.Add(Pcc.getUExport(splinecomponentprop.Value));
                                }
                            }
                        }
                    }

                    //SFXCombatZone 
                    //if (exportEntry.ClassName == "SFXCombatZone")
                    //{
                    //    isParsedByExistingLayer = true;
                    //    sfxCombatZones.Add(exportEntry.Index);
                    //    ToolStripMenuItem combatZoneItem = new ToolStripMenuItem(exportEntry.Index + " " + exportEntry.ObjectName + "_" + exportEntry.indexValue);
                    //    combatZoneItem.ImageScaling = ToolStripItemImageScaling.None;
                    //    if (exportEntry.Index == ActiveCombatZoneExportIndex)
                    //    {
                    //        combatZoneItem.Checked = true;
                    //    }
                    //    combatZoneItem.Click += (object o, EventArgs args) =>
                    //    {
                    //        setSFXCombatZoneBGActive(combatZoneItem, exportEntry, combatZoneItem.Checked);
                    //    };
                    //    sFXCombatZonesToolStripMenuItem.DropDown.Items.Add(combatZoneItem);
                    //    sFXCombatZonesToolStripMenuItem.Enabled = true;
                    //    sFXCombatZonesToolStripMenuItem.ToolTipText = "Select a SFXCombatZone to highlight coverslots that are part of it";
                    //}




                    //if (exportEntry.ObjectName == "StaticMeshCollectionActor")
                    //{
                    //    isParsedByExistingLayer = true;
                    //    ToolStripMenuItem collectionItem = new ToolStripMenuItem(exportEntry.Index + " " + exportEntry.ObjectName + "_" + exportEntry.indexValue);
                    //    collectionItem.ImageScaling = ToolStripItemImageScaling.None;
                    //    collectionItem.Click += (object o, EventArgs args) =>
                    //    {
                    //        staticMeshCollectionActor_ToggleVisibility(collectionItem, exportEntry, collectionItem.Checked);
                    //    };
                    //    if (VisibleActorCollections.Contains(exportEntry.Index))
                    //    {
                    //        byte[] smacData = exportEntry.Data;
                    //        collectionItem.Checked = true;
                    //        //Make new nodes for each item...
                    //        ArrayProperty<ObjectProperty> smacItems = exportEntry.GetProperty<ArrayProperty<ObjectProperty>>("StaticMeshComponents");
                    //        if (smacItems != null)
                    //        {
                    //            int binarypos = findEndOfProps(exportEntry);

                    //            //Read exports...
                    //            foreach (ObjectProperty obj in smacItems)
                    //            {
                    //                if (obj.Value > 0)
                    //                {
                    //                    CurrentObjects.Add(obj.Value - 1);
                    //                    activeExportsListbox.Items.Add("#" + (exportEntry.Index) + " " + exportEntry.ObjectName + " - Class: " + exportEntry.ClassName);

                    //                    //Read location and put in position map
                    //                    int offset = binarypos + 12 * 4;
                    //                    float x = BitConverter.ToSingle(smacData, offset);
                    //                    float y = BitConverter.ToSingle(smacData, offset + 4);
                    //                    //Debug.WriteLine(offset.ToString("X4") + " " + x + "," + y);
                    //                    smacCoordinates[obj.Value - 1] = new PointF(x, y);
                    //                }
                    //                binarypos += 64;
                    //            }
                    //        }
                    //    }
                    //    //staticMeshCollectionActorsToolStripMenuItem.DropDown.Items.Add(collectionItem);
                    //    //staticMeshCollectionActorsToolStripMenuItem.Enabled = true;
                    //    //staticMeshCollectionActorsToolStripMenuItem.ToolTipText = "Select a StaticMeshCollectionActor to add it to the editor";

                    //}

                    if (EverythingElseActive && !isParsedByExistingLayer)
                    {
                        bulkActiveNodes.Add(exportEntry);
                    }

                    //}
                    start += 4;
                    itemcount++;
                }
                else
                {
                    //INVALID ITEM ENCOUNTERED!
                    /*
                    Console.WriteLine("0x" + start.ToString("X8") + "\t0x" + itemexportid.ToString("X8") + "\tInvalid item. Ensure the list is the correct length. (Export " + itemexportid + ")");
                    TreeNode node = new TreeNode();
                    node.Tag = nodeType.ArrayLeafObject;
                    node.Text = start.ToString("X4") + " Invalid item.Ensure the list is the correct length. (Export " + itemexportid + ")";
                    node.Name = start.ToString();
                    topLevelTree.Nodes.Add(node);*/
                    start += 4;
                    itemcount++;
                }
            }

            ActiveNodes.ReplaceAll(bulkActiveNodes);

            for (int i = 0; i < Pcc.ExportCount; i++)
            {
                IExportEntry exportEntry = Pcc.getExport(i);
            }

            bool oneViewActive = PathfindingNodesActive || ActorNodesActive || EverythingElseActive;
            if (oneViewActive && ActiveNodes.Count == 0)
            {
                //Change to non-modal TODO
                MessageBox.Show("No nodes visible with current view options.\nChange view options to see if there are any viewable nodes.");
                graphEditor.Enabled = true;
                graphEditor.UseWaitCursor = false;
                return true; //file still loaded.
            }
            graphEditor.Enabled = true;
            graphEditor.UseWaitCursor = false;
            return true;
        }

        public PointF GenerateGraph()
        {
            graphEditor.nodeLayer.RemoveAllChildren();
            graphEditor.edgeLayer.RemoveAllChildren();
            GraphNodes = new List<PathfindingNodeMaster>();

            double fullx = 0;
            double fully = 0;
            int currentcount = ActiveNodes.Count(); //Some objects load additional objects. We need to count before we iterate over the graphsnode list as it may be appended to during this loop.
            for (int i = 0; i < currentcount; i++)
            {
                PointF pos = LoadObject(ActiveNodes[i]);
                fullx += pos.X;
                fully += pos.Y;
            }
            PointF centerpoint = new PointF((float)(fullx / GraphNodes.Count), (float)(fully / GraphNodes.Count));
            CreateConnections();

            NodeTagListLoading = true;
            //allTagsCombobox.Items.Clear();
            //List<string> tags = new List<string>();
            //foreach (PathfindingNodeMaster n in GraphNodes)
            //{
            //    if (n.NodeTag != null && n.NodeTag != "")
            //    {
            //        tags.Add(n.NodeTag);
            //    }
            //}
            //tags = tags.Distinct().ToList();
            //tags.Sort();
            //tags.Insert(0, "Node tags list");
            //allTagsCombobox.Items.AddRange(tags.ToArray());
            //allTagsCombobox.SelectedIndex = 0;
            TagsList.ClearEx();
            NodeTagListLoading = false;
            foreach (var node in GraphNodes)
            {
                node.MouseDown += node_MouseDown;
                if (node.NodeTag != null && node.NodeTag != "" && !TagsList.Contains(node.NodeTag))
                {
                    TagsList.Add(node.NodeTag);
                }
            }
            TagsList.Sort(x => x);
            if (TagsList.Count > 0)
            {
                FindByTag_ComboBox.SelectedIndex = 0;
            }
            return centerpoint;
        }

        private void node_MouseDown(object sender, PInputEventArgs e)
        {
            PathfindingNodeMaster node = (PathfindingNodeMaster)sender;
            //int n = node.Index;

            ChangingSelectionByGraphClick = true;

            ActiveNodes_ListBox.SelectedItem = node.export;
            if ((node is SplinePoint0Node) || (node is SplinePoint1Node))
            {
                node.Select();
            }
            //CurrentlySelectedSplinePoint = null;
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                Debug.WriteLine("Opening right mouse menu");
                OpenContextMenu();
            }
            ChangingSelectionByGraphClick = false;

        }

        public PointF LoadObject(IExportEntry exporttoLoad)
        {
            PointF smacPos;

            //Todo: impement SMAC
            bool found = false; // smacCoordinates.TryGetValue(index, out smacPos);
            if (found)
            {
                //SMAC_ActorNode smac = new SMAC_ActorNode(index, smacPos.X, smacPos.Y, Pcc, graphEditor);
                //GraphNodes.Add(smac);
                return new PointF(0, 0); //development mode only, fix later
            }
            else
            {
                string s = exporttoLoad.ObjectName;
                int uindex = exporttoLoad.UIndex;
                int x = 0, y = 0, z = int.MinValue;
                var props = exporttoLoad.GetProperties();
                StructProperty prop = props.GetProp<StructProperty>("location");
                if (prop != null)
                {
                    Point3D position = GetLocation(exporttoLoad);
                    x = (int)position.X;
                    y = (int)position.Y;
                    z = (int)position.Z;

                    //if (CurrentFilterType != HeightFilterForm.FILTER_Z_NONE)
                    //{
                    //    if (CurrentFilterType == HeightFilterForm.FILTER_Z_BELOW && z < CurrentZFilterValue)
                    //    {
                    //        return;
                    //    }
                    //    else if (CurrentFilterType == HeightFilterForm.FILTER_Z_ABOVE && z > CurrentZFilterValue)
                    //    {
                    //        return;
                    //    }
                    //}

                    //IExportEntry export = pcc.getExport(index);
                    if (pathfindingNodeClasses.Contains(exporttoLoad.ClassName))
                    {
                        PathfindingNode pathNode = null;
                        switch (exporttoLoad.ClassName)
                        {
                            case "PathNode":
                                pathNode = new PathfindingNodes.PathNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXEnemySpawnPoint":
                                pathNode = new PathfindingNodes.SFXEnemySpawnPoint(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXNav_JumpNode":
                                pathNode = new PathfindingNodes.SFXNav_JumpNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXNav_LeapNodeHumanoid":
                                pathNode = new PathfindingNodes.SFXNav_LeapNodeHumanoid(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXDoorMarker":
                                pathNode = new PathfindingNodes.SFXDoorMarker(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXNav_LargeMantleNode":
                                pathNode = new PathfindingNodes.SFXNav_LargeMantleNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "BioPathPoint":
                                pathNode = new PathfindingNodes.BioPathPoint(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "PathNode_Dynamic":
                                pathNode = new PathfindingNodes.PathNode_Dynamic(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXNav_LargeBoostNode":
                                pathNode = new PathfindingNodes.SFXNav_LargeBoostNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXNav_TurretPoint":
                                pathNode = new PathfindingNodes.SFXNav_TurretPoint(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "CoverLink":
                                pathNode = new PathfindingNodes.CoverLink(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXNav_JumpDownNode":
                                pathNode = new PathfindingNodes.SFXNav_JumpDownNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXNav_LadderNode":
                                pathNode = new PathfindingNodes.SFXNav_LadderNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXDynamicCoverLink":
                                pathNode = new PathfindingNodes.SFXDynamicCoverLink(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;

                            case "CoverSlotMarker":
                                pathNode = new PathfindingNodes.CoverSlotMarker(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXDynamicCoverSlotMarker":
                                pathNode = new PathfindingNodes.SFXDynamicCoverSlotMarker(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "MantleMarker":
                                pathNode = new PathfindingNodes.MantleMarker(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "TargetPoint":
                                pathNode = new PathfindingNodes.TargetPoint(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;

                            case "SFXNav_HarvesterMoveNode":
                                pathNode = new PathfindingNodes.SFXNav_HarvesterMoveNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXOperation_ObjectiveSpawnPoint":
                                pathNode = new PathfindingNodes.SFXObjectiveSpawnPoint(uindex, x, y, exporttoLoad.FileRef, graphEditor);

                                //Create annex node if required
                                var annexZoneLocProp = props.GetProp<ObjectProperty>("AnnexZoneLocation");
                                if (annexZoneLocProp != null)
                                {
                                    if (Pcc.isUExport(annexZoneLocProp.Value))
                                    {
                                        pathNode.comment.Text += "\nBAD ANNEXZONELOC!";
                                        pathNode.comment.TextBrush = new SolidBrush(System.Drawing.Color.Red);
                                    }
                                }

                                break;
                            case "SFXNav_BoostNode":
                                pathNode = new PathfindingNodes.SFXNav_BoostNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            default:
                                pathNode = new PathfindingNodes.PendingNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                        }

                        //if (ActiveCombatZoneExportIndex >= 0 && exporttoLoad.ClassName == "CoverSlotMarker")
                        //{
                        //    ArrayProperty<StructProperty> volumes = props.GetProp<ArrayProperty<StructProperty>>("Volumes");
                        //    if (volumes != null)
                        //    {
                        //        foreach (StructProperty volume in volumes)
                        //        {
                        //            ObjectProperty actorRef = volume.GetProp<ObjectProperty>("Actor");
                        //            if (actorRef != null)
                        //            {
                        //                if (actorRef.Value == ActiveCombatZoneExportIndex + 1)
                        //                {
                        //                    Debug.WriteLine("FOUND ACTIVE COMBAT NODE!");
                        //                    pathNode.shape.Brush = PathfindingNodeMaster.sfxCombatZoneBrush;
                        //                    break;
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                        GraphNodes.Add(pathNode);
                        return new PointF(x, y);
                    } //End if Pathnode Class 

                    else if (actorNodeClasses.Contains(exporttoLoad.ClassName))
                    {
                        ActorNode actorNode = null;
                        switch (exporttoLoad.ClassName)
                        {
                            case "BlockingVolume":
                                actorNode = new BlockingVolumeNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "DynamicBlockingVolume":
                                actorNode = new DynamicBlockingVolume(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "InterpActor":
                                actorNode = new InterpActorNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "BioTriggerVolume":
                                actorNode = new ActorNodes.BioTriggerVolume(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "BioTriggerStream":
                                actorNode = new ActorNodes.BioTriggerStream(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXGrenadeContainer":
                                actorNode = new ActorNodes.SFXGrenadeContainer(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXAmmoContainer":
                                actorNode = new ActorNodes.SFXAmmoContainer(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXAmmoContainer_Simulator":
                                actorNode = new ActorNodes.SFXAmmoContainer_Simulator(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXBlockingVolume_Ledge":
                                actorNode = new ActorNodes.SFXBlockingVolume_Ledge(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXCombatZone":
                                actorNode = new ActorNodes.SFXCombatZone(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "BioStartLocation":
                            case "BioStartLocationMP":
                                actorNode = new ActorNodes.BioStartLocation(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "StaticMeshActor":
                                actorNode = new ActorNodes.StaticMeshActorNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXStuntActor":
                                actorNode = new ActorNodes.SFXStuntActor(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SkeletalMeshActor":
                                actorNode = new ActorNodes.SkeletalMeshActor(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXPlaceable_Generator":
                            case "SFXPlaceable_ShieldGenerator":
                                actorNode = new ActorNodes.SFXPlaceable(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "WwiseAmbientSound":
                                actorNode = new ActorNodes.WwiseAmbientSound(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "WwiseAudioVolume":
                                actorNode = new ActorNodes.WwiseAudioVolume(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXArmorNode":
                            case "SFXTreasureNode":
                                actorNode = new ActorNodes.SFXTreasureNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            case "SFXMedStation":
                                actorNode = new ActorNodes.SFXMedStation(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                            default:
                                actorNode = new PendingActorNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                        }



                        /* if (ActiveCombatZoneExportIndex >= 0)
                         {
                             ArrayProperty<StructProperty> volumes = props.GetProp<ArrayProperty<StructProperty>>("Volumes");
                             if (volumes != null)
                             {
                                 foreach (StructProperty volume in volumes)
                                 {
                                     ObjectProperty actorRef = volume.GetProp<ObjectProperty>("Actor");
                                     if (actorRef != null && actorRef.Value == ActiveCombatZoneExportIndex - 1)
                                     {
                                         //Debug.WriteLine("FOUND ACTIVE COMBAT NODE!");
                                         actorNode.shape.Brush = PathfindingNodeMaster.sfxCombatZoneBrush;
                                         break;
                                     }
                                 }
                             }
                         }*/
                        GraphNodes.Add(actorNode);
                        return new PointF(x, y);
                    }

                    else if (splineNodeClasses.Contains(exporttoLoad.ClassName))
                    {
                        SplineNode splineNode = null;
                        switch (exporttoLoad.ClassName)
                        {
                            case "SplineActor":
                                splineNode = new SplineActorNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);

                                ArrayProperty<StructProperty> connectionsProp = exporttoLoad.GetProperty<ArrayProperty<StructProperty>>("Connections");
                                if (connectionsProp != null)
                                {
                                    foreach (StructProperty connectionProp in connectionsProp)
                                    {
                                        ObjectProperty splinecomponentprop = connectionProp.GetProp<ObjectProperty>("SplineComponent");
                                        IExportEntry splineComponentExport = Pcc.getUExport(splinecomponentprop.Value);
                                        //Debug.WriteLine(splineComponentExport.GetFullPath + " " + splinecomponentprop.Value);
                                        StructProperty splineInfo = splineComponentExport.GetProperty<StructProperty>("SplineInfo");
                                        if (splineInfo != null)
                                        {
                                            ArrayProperty<StructProperty> pointsProp = splineInfo.GetProp<ArrayProperty<StructProperty>>("Points");
                                            StructProperty point1 = pointsProp[0].GetProp<StructProperty>("OutVal");
                                            double xf = point1.GetProp<FloatProperty>("X");
                                            double yf = point1.GetProp<FloatProperty>("Y");
                                            //double zf = point1.GetProp<FloatProperty>("Z");
                                            //Point3D point1_3d = new Point3D(xf, yf, zf);
                                            SplinePoint0Node point0node = new SplinePoint0Node(splinecomponentprop.Value, Convert.ToInt32(xf), Convert.ToInt32(yf), exporttoLoad.FileRef, graphEditor);
                                            StructProperty point2 = pointsProp[1].GetProp<StructProperty>("OutVal");
                                            xf = point2.GetProp<FloatProperty>("X");
                                            yf = point2.GetProp<FloatProperty>("Y");
                                            //zf = point2.GetProp<FloatProperty>("Z");
                                            //Point3D point2_3d = new Point3D(xf, yf, zf);
                                            SplinePoint1Node point1node = new SplinePoint1Node(splinecomponentprop.Value, Convert.ToInt32(xf), Convert.ToInt32(yf), exporttoLoad.FileRef, graphEditor);
                                            point0node.SetDestinationPoint(point1node);

                                            GraphNodes.Add(point0node);
                                            GraphNodes.Add(point1node);

                                            StructProperty reparamProp = splineComponentExport.GetProperty<StructProperty>("SplineReparamTable");
                                            ArrayProperty<StructProperty> reparamPoints = reparamProp.GetProp<ArrayProperty<StructProperty>>("Points");
                                        }
                                    }
                                }
                                break;
                            default:
                                splineNode = new PendingSplineNode(uindex, x, y, exporttoLoad.FileRef, graphEditor);
                                break;
                        }
                        GraphNodes.Add(splineNode);
                        return new PointF(x, y);
                    }

                    else
                    {
                        //everything else
                        GraphNodes.Add(new EverythingElseNode(uindex, x, y, exporttoLoad.FileRef, graphEditor));
                        return new PointF(x, y);
                    }
                }
            }
            //Hopefully we don't see this.
            return new PointF(0, 0);
        }

        public void CreateConnections()
        {
            if (GraphNodes != null && GraphNodes.Count != 0)
            {
                for (int i = 0; i < GraphNodes.Count; i++)
                {
                    graphEditor.addNode(GraphNodes[i]);
                }
                foreach (PathfindingNodeMaster o in graphEditor.nodeLayer)
                {
                    o.CreateConnections(ref GraphNodes);
                }

                foreach (PPath edge in graphEditor.edgeLayer)
                {
                    if (edge.BezierPoints != null)
                    {
                        //Currently not implemented, will hopefully come in future update
                        PathingGraphEditor.UpdateEdgeBezier(edge);
                    }
                    else
                    {
                        PathingGraphEditor.UpdateEdgeStraight(edge);
                    }
                }
            }
        }

        public override void handleUpdate(List<PackageUpdate> updates)
        {
            List<PackageChange> changes = updates.Select(x => x.change).ToList();
            bool exportNonDataChanges = changes.Contains(PackageChange.ExportHeader) || changes.Contains(PackageChange.ExportAdd);
            bool exportsAdded = changes.Contains(PackageChange.ExportAdd);

            var activeNode = ActiveNodes_ListBox.SelectedItem as IExportEntry;
            //we might need to identify parent depths and add those first
            List<PackageUpdate> addedChanges = updates.Where(x => x.change == PackageChange.ExportAdd || x.change == PackageChange.ImportAdd).OrderBy(x => x.index).ToList();
            List<int> headerChanges = updates.Where(x => x.change == PackageChange.ExportHeader).Select(x => x.index).OrderBy(x => x).ToList();
            if (exportsAdded || exportNonDataChanges) //may optimize by checking if chagnes include anything we care about
            {
                //Do a full refresh
                RefreshGraph();
                return;
            }

            var loadedincices = ActiveNodes.Select(x => x.Index).ToList(); //Package updates are 0 based
            var nodesToUpdate = updates.Where(x => x.change == PackageChange.ExportData && loadedincices.Contains(x.index)).Select(x => x.index).ToList();

            if (nodesToUpdate.Count > 0)
            {
                foreach (var node in ActiveNodes)
                {
                    if (nodesToUpdate.Contains(node.Index))
                    {
                        //Reposition the node
                        var newlocation = GetLocation(node);
                        PathfindingNodeMaster s = GraphNodes.FirstOrDefault(o => o.UIndex == node.UIndex);
                        s.SetOffset((float)newlocation.X, (float)newlocation.Y);
                        foreach (PNode i in s.AllNodes)
                        {
                            ArrayList edges = (ArrayList)i.Tag;
                            if (edges != null)
                            {
                                foreach (PPath edge in edges)
                                {
                                    PathingGraphEditor.UpdateEdgeStraight(edge);
                                }
                            }
                        }

                        if (node == activeNode)
                        {
                            ActiveNodesList_SelectedItemChanged(null, null); //Reselect object
                        }
                    }
                }
                graphEditor.Refresh(); //repaint invalidated areas
            }
        }

        #region Bindings
        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, CurrentFile + " " + value);
        }
        #endregion

        #region Recents
        private readonly List<Button> RecentButtons = new List<Button>();
        public List<string> RFiles;
        private List<PathfindingNodeMaster> GraphNodes;
        private bool NodeTagListLoading;
        private bool isFirstLoad = true;
        private bool ChangingSelectionByGraphClick;
        private IExportEntry PersisentLevelExport;
        public static readonly string PathfindingEditorDataFolder = System.IO.Path.Combine(App.AppDataFolder, @"PathfindingEditor\");
        private readonly string RECENTFILES_FILE = "RECENTFILES";

        private void LoadRecentList()
        {
            RecentButtons.AddRange(new[] { RecentButton1, RecentButton2, RecentButton3, RecentButton4, RecentButton5, RecentButton6, RecentButton7, RecentButton8, RecentButton9, RecentButton10 });
            Recents_MenuItem.IsEnabled = false;
            RFiles = new List<string>();
            RFiles.Clear();
            string path = PathfindingEditorDataFolder + RECENTFILES_FILE;
            if (File.Exists(path))
            {
                string[] recents = File.ReadAllLines(path);
                foreach (string recent in recents)
                {
                    if (File.Exists(recent))
                    {
                        AddRecent(recent, true);
                    }
                }
            }
        }

        private void SaveRecentList()
        {
            if (!Directory.Exists(PathfindingEditorDataFolder))
            {
                Directory.CreateDirectory(PathfindingEditorDataFolder);
            }
            string path = PathfindingEditorDataFolder + RECENTFILES_FILE;
            if (File.Exists(path))
                File.Delete(path);
            File.WriteAllLines(path, RFiles);
        }

        public void RefreshRecent(bool propogate, List<string> recents = null)
        {
            if (propogate && recents != null)
            {
                //we are posting an update to other instances of packed

                //This code can be removed when non-WPF package editor is removed.
                var forms = System.Windows.Forms.Application.OpenForms;
                foreach (System.Windows.Forms.Form form in forms)
                {
                    if (form is PathfindingEditor editor) //it will never be "this"
                    {
                        editor.RefreshRecent(false, RFiles);
                    }
                }
                foreach (var form in App.Current.Windows)
                {
                    if (form is PathfindingEditorWPF wpf && this != wpf)
                    {
                        wpf.RefreshRecent(false, RFiles);
                    }
                }
            }
            else if (recents != null)
            {
                //we are receiving an update
                RFiles = new List<string>(recents);
            }
            Recents_MenuItem.Items.Clear();
            if (RFiles.Count <= 0)
            {
                Recents_MenuItem.IsEnabled = false;
                return;
            }
            Recents_MenuItem.IsEnabled = true;

            int i = 0;
            foreach (string filepath in RFiles)
            {
                MenuItem fr = new MenuItem()
                {
                    Header = filepath.Replace("_", "__"),
                    Tag = filepath
                };
                RecentButtons[i].Visibility = Visibility.Visible;
                RecentButtons[i].Content = System.IO.Path.GetFileName(filepath.Replace("_", "__"));
                RecentButtons[i].Click -= RecentFile_click;
                RecentButtons[i].Click += RecentFile_click;
                RecentButtons[i].Tag = filepath;
                RecentButtons[i].ToolTip = filepath;
                fr.Click += RecentFile_click;
                Recents_MenuItem.Items.Add(fr);
                i++;
            }
            while (i < 10)
            {
                RecentButtons[i].Visibility = Visibility.Collapsed;
                i++;
            }
        }

        private void RecentFile_click(object sender, EventArgs e)
        {
            string s = ((FrameworkElement)sender).Tag.ToString();
            if (File.Exists(s))
            {
                LoadFile(s);
            }
            else
            {
                MessageBox.Show("File does not exist: " + s);
            }
        }

        public void AddRecent(string s, bool loadingList)
        {
            RFiles = RFiles.Where(x => !x.Equals(s, StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (loadingList)
            {
                RFiles.Add(s); //in order
            }
            else
            {
                RFiles.Insert(0, s); //put at front
            }
            if (RFiles.Count > 10)
            {
                RFiles.RemoveRange(10, RFiles.Count - 10);
            }
            Recents_MenuItem.IsEnabled = true;
        }

        #endregion

        private void PathfinderEditorWPF_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            graphEditor.RemoveInputEventListener(pathfindingMouseListener);
            graphEditor.DebugEventHandlers();
            graphEditor.Dispose();
            GraphHost.Child = null; //This seems to be required to clear OnChildGotFocus handler from WinFormsHost
            graphEditor.DebugEventHandlers();
            GraphHost.Dispose();
            ActiveNodes.ClearEx();
            GraphNodes?.Clear();
            Properties_InterpreterWPF.Dispose();
            PathfindingEditorWPF_ReachSpecsPanel.Dispose();
            zoomController.Dispose();
            graphEditor.DebugEventHandlers();
        }

        private void ActiveNodesList_SelectedItemChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (PathfindingNodeMaster pfm in GraphNodes)
            {
                pfm.Deselect();
            }

            if (ActiveNodes_ListBox.SelectedItem != null)
            {
                IExportEntry export = (IExportEntry)ActiveNodes_ListBox.SelectedItem;
                NodeName = $"{export.ObjectName}_{export.indexValue}";
                NodeNameSubText = $"Export {export.UIndex}";
                ActiveNodes_ListBox.ScrollIntoView(export);
                Properties_InterpreterWPF.LoadExport(export);
                PathfindingEditorWPF_ReachSpecsPanel.LoadExport(export);

#if DEBUG
                //Populate the export/import database
                if (!exportclassdb.ContainsKey(export.ClassName))
                {
                    Dictionary<string, string> data = new Dictionary<string, string>();
                    data["class"] = export.FileRef.getEntry(export.idxClass).GetFullPath;
                    data["name"] = export.ClassName;
                    var collisioncomponent = export.GetProperty<ObjectProperty>("CollisionComponent");
                    if (collisioncomponent != null)
                    {
                        IExportEntry collisionComp = export.FileRef.getUExport(collisioncomponent.Value);

                        data["cylindercomponentarchetype"] = collisionComp.FileRef.getEntry(collisionComp.idxArchtype).GetFullPath;

                        ////Add imports
                        if (!importclassdb.ContainsKey(data["cylindercomponentarchetype"]) && collisionComp.idxArchtype < 0)
                        {
                            //X.Default.CollisionCylinder
                            Dictionary<string, string> cylindercompimp = new Dictionary<string, string>();
                            ImportEntry collisionCylinderArchetype = collisionComp.FileRef.getEntry(collisionComp.idxArchtype) as ImportEntry;

                            cylindercompimp["class"] = collisionCylinderArchetype.ClassName == "Class" ? "Class" : collisionCylinderArchetype.PackageFileNoExtension + "." + collisionCylinderArchetype.ClassName;
                            cylindercompimp["packagefile"] = collisionCylinderArchetype.PackageFileNoExtension;
                            importclassdb[data["cylindercomponentarchetype"]] = cylindercompimp;

                            //X.Default
                            Dictionary<string, string> nodetypeimp = new Dictionary<string, string>();
                            ImportEntry collisionCylinderArchetypeDefault = collisionCylinderArchetype.FileRef.getEntry(collisionCylinderArchetype.idxLink) as ImportEntry;
                            nodetypeimp["class"] = collisionCylinderArchetypeDefault.ClassName == "Class" ? "Class" : collisionCylinderArchetypeDefault.PackageFileNoExtension + "." + collisionCylinderArchetypeDefault.ClassName;
                            nodetypeimp["packagefile"] = collisionCylinderArchetypeDefault.PackageFileNoExtension;
                            importclassdb[collisionCylinderArchetypeDefault.GetFullPath] = nodetypeimp;
                        }
                    }
                    data["description"] = "No data about this node type has been entered yet";
                    exportclassdb[export.ClassName] = data;

                    Dictionary<string, string> nodeclassimport = new Dictionary<string, string>();
                    ImportEntry classImport = export.FileRef.getEntry(export.idxClass) as ImportEntry;

                    nodeclassimport["class"] = classImport.ClassName == "Class" ? "Class" : classImport.PackageFileNoExtension + "." + classImport.ClassName;
                    nodeclassimport["packagefile"] = classImport.PackageFileNoExtension;

                    importclassdb[classImport.GetFullPath] = nodeclassimport;

                    //Rename vars - debug only
                    var exporttypes = exportclassdb;
                    var importtypes = importclassdb;

                    Debug.WriteLine("Adding to pathfinding database file: " + export.ClassName);
                    File.WriteAllText(classDatabasePath,
                JsonConvert.SerializeObject(new { exporttypes, importtypes }, Formatting.Indented));

                }

#endif
                //Clear coverlinknode highlighting.
                /*foreach (PathfindingNodeMaster pnm in Objects)
                {
                    if (CurrentlyHighlightedCoverlinkNodes.Contains(pnm.export.Index))
                    {
                        pnm.shape.Brush = pathfindingNodeBrush;
                    }
                }*/
                PathfindingNodeMaster s = GraphNodes.FirstOrDefault(o => o.UIndex == export.UIndex);
                if (s != null)
                {
                    //if (selectedIndex != -1)
                    //{
                    //    PathfindingNodeMaster d = Objects.FirstOrDefault(o => o.Index == CurrentObjects[selectedIndex]);
                    //    if (d != null)
                    //        d.Deselect();
                    //}
                    s.Select();
                    if (!ChangingSelectionByGraphClick)
                    {
                        graphEditor.Camera.AnimateViewToCenterBounds(s.GlobalFullBounds, false, 1000);
                    }
                    Point3D position = GetLocation(export);
                    NodePositionX_TextBox.Text = position.X.ToString();
                    NodePositionY_TextBox.Text = position.Y.ToString();
                    NodePositionZ_TextBox.Text = position.Z.ToString();

                    //switch (s.export.ClassName)
                    //{
                    //    case "CoverLink":
                    //        HighlightCoverlinkSlots(s.export);
                    //        break;
                    //    case "CoverSlotMarker":
                    //        StructProperty sp = s.export.GetProperty<StructProperty>("OwningSlot");
                    //        if (sp != null)
                    //        {
                    //            ObjectProperty op = sp.GetProp<ObjectProperty>("Link");
                    //            if (op != null && op.Value - 1 < pcc.ExportCount)
                    //            {
                    //                HighlightCoverlinkSlots(pcc.Exports[op.Value - 1]);
                    //            }
                    //        }
                    //        break;
                    //}
                }

                //GetProperties(pcc.getExport(CurrentObjects[n]));
                //selectedIndex = n;
                //selectedByNode = false;

                //Refresh binding
                OnPropertyChanged(nameof(NodeTypeDescriptionText));

                graphEditor.Refresh();
            }
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            graphEditor.AllowDragging();
        }

        private void FindByTag_Click(object sender, RoutedEventArgs e)
        {
            FindByTag();
        }

        private void FindByTag()
        {
            int currentIndex = ActiveNodes_ListBox.SelectedIndex;
            var currnentSelectedItem = ActiveNodes_ListBox.SelectedItem as IExportEntry;
            if (currentIndex < 0 || currentIndex >= ActiveNodes.Count - 1) currentIndex = -1; //nothing selected or the final item is selected
            currentIndex++; //search next item

            string nodeTagToFind = FindByTag_ComboBox.SelectedItem as string;
            if (nodeTagToFind is null) return; //empty

            for (int i = 0; i < ActiveNodes.Count(); i++) //activenodes size should match graphnodes size... in theory of course.
            {
                PathfindingNodeMaster ci = GraphNodes[(i + currentIndex) % GraphNodes.Count()];
                if (ci.NodeTag == nodeTagToFind)
                {
                    ActiveNodes_ListBox.SelectedItem = ci.export;
                    if (ci.export == currnentSelectedItem)
                    {
                        ActiveNodesList_SelectedItemChanged(null, null); //refocus node
                    }
                    break;
                }
            }
        }

        private void FindByNumber_Click(object sender, RoutedEventArgs e)
        {
            FindByNumber();
        }

        private void FindByNumber()
        {
            if (int.TryParse(FindByNumber_TextBox.Text, out int result) && Pcc.isUExport(result))
            {
                var export = Pcc.getUExport(result);
                int index = ActiveNodes.IndexOf(export);
                if (index >= 0)
                {
                    ActiveNodes_ListBox.SelectedIndex = index;
                }
            }
        }

        private void FindByNumber_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                FindByNumber();
            }
        }

        private void FindByTag_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                FindByTag();
            }
        }

        private void PositionBoxes_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                IExportEntry export = ActiveNodes_ListBox.SelectedItem as IExportEntry;
                if (export != null && int.TryParse(NodePositionX_TextBox.Text, out int x) && int.TryParse(NodePositionY_TextBox.Text, out int y) && int.TryParse(NodePositionZ_TextBox.Text, out int z))
                {
                    SetLocation(export, x, y, z);
                    PathfindingNodeMaster s = GraphNodes.FirstOrDefault(o => o.UIndex == export.UIndex);
                    s.SetOffset(x, y);
                    foreach (PNode node in s.AllNodes)
                    {
                        ArrayList edges = (ArrayList)node.Tag;
                        if (edges != null)
                            foreach (PPath edge in edges)
                            {
                                PathingGraphEditor.UpdateEdgeStraight(edge);
                            }
                    }
                    graphEditor.Refresh(); //repaint invalidated areas
                }
            }
        }

        private void SetLocation(IExportEntry export, float x, float y, float z)
        {
            StructProperty prop = export.GetProperty<StructProperty>("location");
            prop.GetProp<FloatProperty>("X").Value = x;
            prop.GetProp<FloatProperty>("Y").Value = y;
            prop.GetProp<FloatProperty>("Z").Value = z;
            export.WriteProperty(prop);
        }

        private void SetLocation(IExportEntry export, int x, int y, int z)
        {
            StructProperty prop = export.GetProperty<StructProperty>("location");
            prop.GetProp<FloatProperty>("X").Value = x;
            prop.GetProp<FloatProperty>("Y").Value = y;
            prop.GetProp<FloatProperty>("Z").Value = z;
            export.WriteProperty(prop);
        }

        public static Point3D GetLocation(IExportEntry export)
        {
            int x = 0, y = 0, z = int.MinValue;
            var props = export.GetProperties();
            StructProperty prop = props.GetProp<StructProperty>("location");
            if (prop != null)
            {
                PropertyCollection nodelocprops = (prop as StructProperty).Properties;
                foreach (var locprop in nodelocprops)
                {
                    switch (locprop.Name)
                    {
                        case "X":
                            x = Convert.ToInt32((locprop as FloatProperty).Value);
                            break;
                        case "Y":
                            y = Convert.ToInt32((locprop as FloatProperty).Value);
                            break;
                        case "Z":
                            z = Convert.ToInt32((locprop as FloatProperty).Value);
                            break;
                    }
                }
                return new Point3D(x, y, z);
            }
            return null;
        }

        private void SetGraphXY_Clicked(object sender, RoutedEventArgs e)
        {
            //Find node
            IExportEntry export = ActiveNodes_ListBox.SelectedItem as IExportEntry;
            if (export != null)
            {
                PathfindingNodeMaster s = GraphNodes.FirstOrDefault(o => o.UIndex == export.UIndex); 
                var currentlocation = GetLocation(export);
                SetLocation(export, s.GlobalBounds.X, s.GlobalBounds.Y, (float)currentlocation.Z);
                MessageBox.Show($"Location set to {s.GlobalBounds.X}, { s.GlobalBounds.Y}");
            }
            else
            {
                MessageBox.Show("No location property on this export.");

            }
            //Need to update
        }

        private void OpenInPackageEditor_Clicked(object sender, RoutedEventArgs e)
        {
            IExportEntry export = ActiveNodes_ListBox.SelectedItem as IExportEntry;
            if (export != null)
            {
                PackageEditorWPF p = new PackageEditorWPF();
                p.Show();
                p.LoadFile(export.FileRef.FileName);
                p.GoToNumber(export.UIndex);
            }
        }

        private void CloneNode_Clicked(object sender, RoutedEventArgs e)
        {
            var newNode = cloneNode(ActiveNodes_ListBox.SelectedItem as IExportEntry);
            if (newNode != null)
            {
                ActiveNodes_ListBox.SelectedItem = newNode;
            }
        }

        private IExportEntry cloneNode(IExportEntry nodeEntry)
        {
            if (nodeEntry != null)
            {
                ObjectProperty collisionComponentProperty = nodeEntry.GetProperty<ObjectProperty>("CollisionComponent");
                IExportEntry collisionEntry = nodeEntry.FileRef.getUExport(collisionComponentProperty.Value);


                IExportEntry newNodeEntry = nodeEntry.Clone();
                nodeEntry.FileRef.addExport(newNodeEntry);
                IExportEntry newCollisionEntry = collisionEntry.Clone();
                nodeEntry.FileRef.addExport(newCollisionEntry);
                newCollisionEntry.idxLink = newNodeEntry.UIndex;

                //Update the cloned nodes to be new items
                bool changed = false;

                //empty the pathlist
                PropertyCollection newExportProps = newNodeEntry.GetProperties();

                ArrayProperty<ObjectProperty> PathList = newExportProps.GetProp<ArrayProperty<ObjectProperty>>("PathList");
                if (PathList != null && PathList.Count > 0)
                {
                    changed = true;
                    PathList.Clear();
                }

                foreach (UProperty prop in newExportProps)
                {
                    if (prop is ObjectProperty)
                    {
                        var objProp = prop as ObjectProperty;
                        if (objProp.Value == collisionEntry.UIndex)
                        {
                            objProp.Value = newCollisionEntry.UIndex;
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    newNodeEntry.WriteProperties(newExportProps);
                }

                var oldloc = GetLocation(newNodeEntry);
                SetLocation(newNodeEntry, (float)oldloc.X + 50, (float)oldloc.Y + 50, (float)oldloc.Z);
                /*
                collisionComponentProperty = newNodeEntry.GetProperty<ObjectProperty>("CollisionComponent");
                if (collisionComponentProperty != null)
                {
                    collisionComponentProperty.Value = newCollisionEntry.UIndex;
                    newNodeEntry.WriteProperty(collisionComponentProperty);
                }

                collisionComponentProperty = newNodeEntry.GetProperty<ObjectProperty>("CylinderComponent");
                if (collisionComponentProperty != null)
                {
                    collisionComponentProperty.Value = newCollisionEntry.UIndex;
                    newNodeEntry.WriteProperty(collisionComponentProperty);

                }*/

                SharedPathfinding.GenerateNewRandomGUID(newNodeEntry);
                //Add cloned node to persistentlevel
                //Read persistent level binary
                byte[] data = PersisentLevelExport.Data;

                //find start of class binary (end of props)
                int start = PersisentLevelExport.propsEnd();

                uint exportid = BitConverter.ToUInt32(data, start);
                start += 4;
                uint numberofitems = BitConverter.ToUInt32(data, start);
                numberofitems++;
                SharedPathfinding.WriteMem(data, start, BitConverter.GetBytes(numberofitems));
                int insertoffset = (int)(numberofitems * 4) + start;
                List<byte> memList = data.ToList();
                memList.InsertRange(insertoffset, BitConverter.GetBytes(newNodeEntry.UIndex));
                data = memList.ToArray();
                PersisentLevelExport.Data = data;

                reindexMatchingObjects(newNodeEntry);
                reindexMatchingObjects(newCollisionEntry);
                return newNodeEntry;
            }
            return null;
        }

        /// <summary>
        /// Reindexes all objects in this pcc that have the same full path.
        /// USE WITH CAUTION!
        /// </summary>
        /// <param name="exportToReindex">Export that contains the path you want to reindex</param>
        private void reindexMatchingObjects(IExportEntry exportToReindex)
        {
            string fullpath = exportToReindex.GetFullPath;
            int index = 1; //we'll start at 1.
            foreach (IExportEntry export in Pcc.Exports)
            {
                if (fullpath == export.GetFullPath && export.ClassName != "Class")
                {
                    export.indexValue = index;
                    index++;
                }
            }
        }
    }
}
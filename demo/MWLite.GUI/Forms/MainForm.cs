﻿using AxMapWinGIS;
using MWLite.Symbology.LegendControl;
using MapWinGIS;
using MWLite.Core;
using MWLite.GUI.Classes;
using MWLite.GUI.Helpers;
using MWLite.GUI.MapLegend;
using MWLite.GUI.Properties;
using MWLite.ShapeEditor;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace MWLite.GUI.Forms
{
    public partial class MainForm : DockContent, IMapApp
    {
        private const string WINDOW_TITLE = "MapWinGIS Demo";
        private readonly AppDispatcher _dispatcher = new AppDispatcher();
        private static MainForm _form = null;
        private MapForm _mapForm = null;
        private LegendDockForm _legendForm = null;
        private MapCallback _callback = null;

        public MainForm()
        {
            InitializeComponent();

            _callback = new MapCallback(statusStrip1, progressBar1, lblProgressMessage);

            _form = this;

            Init();
        }
        
        #region Initialization

        private void Init()
        {
            InitDockLayout();

            PluginHelper.Init(this);

            ToolStripManager.LoadSettings(this);

            TilesHelper.Init(mnuTiles);

            InitLegend();

            this.InitMenus();

            RefreshUI();

            var gs = new GlobalSettings();
            gs.ApplicationCallback = _callback;

            Shown += (s, e) => Map.Focus();

            FormClosing += MainForm_FormClosing;

            App.Project.ProjectChanged += (s, e) => RefreshUI();

            /*var ext = new Extents();
            ext.SetBounds(2.52, 50.64, 0, 5.94, 51.51, 10);

            var wmslayer = new WmsLayer
            {
                BaseUrl = "http://geoservices.informatievlaanderen.be/raadpleegdiensten/GRB-basiskaart/wms",
                Epsg = 4326,
                Layers = "GRB_BSK",
                Format = "image/png",
                Name = "GRB BSK",
                BoundingBox = ext
            };
            App.Map.Projection = tkMapProjection.PROJECTION_WGS84;
            App.Legend.Layers.Add(wmslayer, true);*/
            //?REQUEST = GetMap & HEIGHT = 256 & WIDTH = 256 & LAYERS = GRB_BSK & OUTPUT_FORMAT = image / png & bbox = 51.51,5.94,50.64,2.52



            var ds = new OgrDatasource();
            //ds.Open("MSSQL:server=192.168.1.204;database=COMMON;trusted_connection=no;UID=marlin;pwd=marlin007$");
            //var sf = ds.GetLayerByName("ROUTE");

            ds.Open("WFS:http://geoservices.informatievlaanderen.be/overdrachtdiensten/GRB/wfs");
            var layers = new string[]
            {
                "GRB:ADP",
                "GRB:GBA",
                "GRB:GBG",
            };
            foreach (var nm in layers)
            {
                var sf = ds.GetLayerByName(nm);
                App.Legend.Layers.Add(sf, true);
            }

            App.Project.Load(AppSettings.Instance.LastProject);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            string lastProject = App.Project.GetPath();

            if (!App.Map.ShapeEditor.SaveChanges())
            {
                e.Cancel = true;
                return;
            }

            if (!App.Project.TryClose())
            {
                e.Cancel = true;
                return;
            }
            
            AppSettings.Instance.LastProject = lastProject;
            AppSettings.Save();

            ToolStripManager.SaveSettings(this);
        }

        private void InitLegend()
        {
            Legend.Map = Map.GetOcx() as Map;
            Legend.AssignOrphanLayersToNewGroup(ProjectBase.DEFAULT_GROUP_NAME);
            Legend.LayerSelected += (handle) =>
            {
                RefreshUI();
            };
        }

        private void InitMenus()
        {
            Dispatcher.InitMenu(mnuFile.DropDownItems);
            Dispatcher.InitMenu(mnuMap.DropDownItems);
            Dispatcher.InitMenu(_mainToolStrip.Items);
            Dispatcher.InitMenu(_toolStripLayer.Items);
        }

        private void InitDockLayout()
        {
            _legendForm = new LegendDockForm();
            _legendForm.Show(dockPanel1, DockState.DockLeft);

            _mapForm = new MapForm();
            _mapForm.Show(dockPanel1, DockState.Document);
            _mapForm.SelectionChanged += (s, e) => RefreshUI();
            _mapForm.CloseButton = false;
            

            var form = new DonationForm();
            form.Show(dockPanel1, DockState.Document);
            form.CloseButton = false;

            _mapForm.Activate();

           
        }

        #endregion
       
        #region Properties

        public static MainForm Instance
        {
            get { return _form; }
        }

        internal AppDispatcher Dispatcher
        {
            get { return _dispatcher; }
        }

        public MapForm MapForm
        {
            get { return _mapForm; }
        }

        #endregion

        #region IMapApp interface

        public IProject Project
        {
            get { return App.Project; }
        }

        public void RunCommand(AppCommand command)
        {
            _dispatcher.Run(command);
        }

        public Legend Legend
        {
            get { return _legendForm.Legend; }
        }

        public AxMap Map
        {
            get { return _mapForm.Map; }
        }

        public void LoadMapState(string filename)
        {
            _mapForm.LoadMapState(filename);
        }

        public void AddMenu(ToolStripMenuItem menu)
        {
            MainMenuStrip.Items.Add(menu);
        }

        public void AddToolbar(ToolStrip toolbar)
        {
            toolStripContainer1.TopToolStripPanel.Join(toolbar, _mainToolStrip.Left + _mainToolStrip.Width, 0);
        }

        public void RefreshUI()
        {
            Text = WINDOW_TITLE;
            if (!App.Project.IsEmpty)
                 Text += " - " + App.Project.GetPath();

            toolSetProjection.Enabled = App.Map.NumLayers == 0;
            toolSetProjection.Text = App.Map.NumLayers == 0
                ? "Set coordinate system and projection"
                : "It's not allowed to change projection when layers are already added to the map.";

            toolSearch.Enabled = true;
            toolSearch.Text = "Find location";
            if (App.Map.NumLayers > 0 && !App.Map.Measuring.IsUsingEllipsoid)
            {
                toolSearch.Enabled = false;
                toolSearch.Text = "Unsupported projection. Search isn't allowed.";
            }

            toolZoomIn.Checked = Map.CursorMode == tkCursorMode.cmZoomIn;
            toolZoomOut.Checked = Map.CursorMode == tkCursorMode.cmZoomOut;
            toolPan.Checked = Map.CursorMode == tkCursorMode.cmPan;
            toolSelect.Checked = Map.CursorMode == tkCursorMode.cmSelection;
            toolSelectByPolygon.Checked = Map.CursorMode == tkCursorMode.cmSelectByPolygon;
            toolIdentify.Checked = Map.CursorMode == tkCursorMode.cmIdentify;

            bool distance = Map.Measuring.MeasuringType == tkMeasuringType.MeasureDistance;
            toolMeasure.Checked = Map.CursorMode == tkCursorMode.cmMeasure && distance;
            toolMeasureArea.Checked = Map.CursorMode == tkCursorMode.cmMeasure && !distance;

            if (Map.CursorMode != tkCursorMode.cmIdentify)
            {
                MapForm.HideTooltip();
            }

            bool hasShapefile = false;
            int layerHandle = App.Legend.SelectedLayer;
            bool hasLayer = layerHandle != -1;
            if (hasLayer)
            {
                var sf = App.Map.get_Shapefile(layerHandle);
                if (sf != null)
                {
                    statusSelectedCount.Text = string.Format("Shapes: {0}; selected: {1}", sf.NumShapes, sf.NumSelected);
                    toolClearSelection.Enabled = sf.NumSelected > 0;
                    toolZoomToSelected.Enabled = sf.NumSelected > 0;
                    hasShapefile = true;
                }
            }

            if (!hasShapefile)
            {
                statusSelectedCount.Text = "";
                toolClearSelection.Enabled = false;
                toolZoomToSelected.Enabled = false;
            }

            toolRemoveLayer.Enabled = hasLayer;
            Editor.RefreshUI();

            Map.Focus();
        }

        #endregion
    }
}

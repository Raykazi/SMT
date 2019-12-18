﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SMT
{
    /// <summary>
    /// Interaction logic for UniverseControl.xaml
    /// </summary>
    public partial class UniverseControl : UserControl, INotifyPropertyChanged
    {


        private class VisualHost : FrameworkElement
        {
            // Create a collection of child visual objects.
            private VisualCollection Children;

            private Dictionary<Visual, Object> DataContextData;

            public void AddChild(Visual vis, object dataContext)
            {
                Children.Add(vis);
                DataContextData.Add(vis, dataContext);
            }



            public void RemoveChild(Visual vis, object dataContext)
            {
                Children.Remove(vis);
                DataContextData.Remove(vis);
            }

            public void ClearAllChildren()
            {
                Children.Clear();
                DataContextData.Clear();
            }


            public bool HitTestEnabled
            {
                get;
                set;
            }

            public VisualHost()
            {
                Children = new VisualCollection(this);
                DataContextData = new Dictionary<Visual, object>();

                HitTestEnabled = false;


                MouseRightButtonUp += VisualHost_MouseButtonUp;
            }

            private void VisualHost_MouseButtonUp(object sender, MouseButtonEventArgs e)
            {
                // Retreive the coordinates of the mouse button event.
                Point pt = e.GetPosition((UIElement)sender);

                if (HitTestEnabled)
                {

                    // Initiate the hit test by setting up a hit test result callback method.
                    VisualTreeHelper.HitTest(this, null, HitTestCheck, new PointHitTestParameters(pt));
                }
            }

            // Provide a required override for the VisualChildrenCount property.
            protected override int VisualChildrenCount => Children.Count;

            // Provide a required override for the GetVisualChild method.
            protected override Visual GetVisualChild(int index)
            {
                if (index < 0 || index >= Children.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return Children[index];
            }


            // If a child visual object is hit, toggle its opacity to visually indicate a hit.
            public HitTestResultBehavior HitTestCheck(HitTestResult result)
            {
                System.Windows.Media.DrawingVisual dv = null;
                if (result.VisualHit.GetType() == typeof(System.Windows.Media.DrawingVisual))
                {
                    dv = (System.Windows.Media.DrawingVisual)result.VisualHit;
                    dv.Opacity = dv.Opacity == 1.0 ? 0.4 : 1.0;
                }

                if (dv != null && DataContextData.ContainsKey(dv))
                {
                    RoutedEventArgs newEventArgs = new RoutedEventArgs(MouseClickedEvent, DataContextData[dv]);
                    RaiseEvent(newEventArgs);
                }


                // Stop the hit test enumeration of objects in the visual tree.
                return HitTestResultBehavior.Stop;
            }


            public static readonly RoutedEvent MouseClickedEvent = EventManager.RegisterRoutedEvent("MouseClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VisualHost));

            public event RoutedEventHandler MouseClicked
            {
                add { AddHandler(MouseClickedEvent, value); }
                remove { RemoveHandler(MouseClickedEvent, value); }
            }


        }



        private double m_ESIOverlayScale = 1.0f;
        private bool m_ShowNPCKills = false;
        private bool m_ShowPodKills = false;
        private bool m_ShowShipKills = false;
        private bool m_ShowShipJumps = false;
        private bool m_ShowJumpBridges = true;
        





        public UniverseControl()
        {
            InitializeComponent();
        }


        private struct GateHelper
        {
            public EVEData.System from { get; set; }
            public EVEData.System to { get; set; }
        }


        public bool ShowJumpBridges
        {
            get
            {
                return m_ShowJumpBridges;
            }
            set
            {
                m_ShowJumpBridges = value;
                OnPropertyChanged("ShowJumpBridges");
            }
        }


        public double ESIOverlayScale
        {
            get
            {
                return m_ESIOverlayScale;
            }
            set
            {
                m_ESIOverlayScale = value;
                OnPropertyChanged("ESIOverlayScale");
            }
        }


        public bool ShowNPCKills
        {
            get
            {
                return m_ShowNPCKills;
            }

            set
            {
                m_ShowNPCKills = value;

                if (m_ShowNPCKills)
                {
                    ShowPodKills = false;
                    ShowShipKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowNPCKills");
            }
        }

        public bool ShowPodKills
        {
            get
            {
                return m_ShowPodKills;
            }

            set
            {
                m_ShowPodKills = value;
                if (m_ShowPodKills)
                {
                    ShowNPCKills = false;
                    ShowShipKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowPodKills");
            }
        }

        public bool ShowShipKills
        {
            get
            {
                return m_ShowShipKills;
            }

            set
            {
                m_ShowShipKills = value;
                if (m_ShowShipKills)
                {
                    ShowNPCKills = false;
                    ShowPodKills = false;
                    ShowShipJumps = false;
                }

                OnPropertyChanged("ShowShipKills");
            }
        }

        public bool ShowShipJumps
        {
            get
            {
                return m_ShowShipJumps;
            }

            set
            {
                m_ShowShipJumps = value;
                if (m_ShowShipJumps)
                {
                    ShowNPCKills = false;
                    ShowPodKills = false;
                    ShowShipKills = false;
                }

                OnPropertyChanged("ShowShipJumps");
            }
        }



        private List<GateHelper> universeSysLinksCache;
        private double universeWidth;
        private double universeDepth;
        private double universeXMin;
        private double universeXMax;
        private double universeScale;

        private double universeZMin;
        private double universeZMax;

        private EVEData.EveManager EM;


        private VisualHost VHSystems;
        private VisualHost VHLinks;
        private VisualHost VHNames;
        private VisualHost VHRegionNames;
        private VisualHost VHRangeSpheres;
        private VisualHost VHDataSpheres;


        // Timer to Re-draw the map
        private System.Windows.Threading.DispatcherTimer uiRefreshTimer;


        public void Init()
        {
            EM = EVEData.EveManager.Instance;



            universeSysLinksCache = new List<GateHelper>();


            universeXMin = 0.0;
            universeXMax = 336522971264518000.0;

            universeZMin = -484452845697854000;
            universeZMax = 472860102256057000.0;

            VHSystems = new VisualHost();
//            VHSystems.HitTestEnabled = true;
//            VHSystems.MouseClicked += VHSystems_MouseClicked;

            VHLinks = new VisualHost();
            VHNames = new VisualHost();
            VHRegionNames = new VisualHost();
            VHRangeSpheres = new VisualHost();
            VHDataSpheres = new VisualHost();

            UniverseMainCanvas.Children.Add(VHDataSpheres);
            UniverseMainCanvas.Children.Add(VHRangeSpheres);

            UniverseMainCanvas.Children.Add(VHLinks);
            UniverseMainCanvas.Children.Add(VHSystems);

            uiRefreshTimer = new System.Windows.Threading.DispatcherTimer();
            uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            uiRefreshTimer.Interval = new TimeSpan(0, 0, 10);
            uiRefreshTimer.Start();

            PropertyChanged += UniverseControl_PropertyChanged;



            DataContext = this;

            foreach (EVEData.System sys in EM.Systems)
            {
                foreach (string jumpTo in sys.Jumps)
                {
                    EVEData.System to = EM.GetEveSystem(jumpTo);

                    bool NeedsAdd = true;
                    foreach (GateHelper gh in universeSysLinksCache)
                    {
                        if (((gh.from == sys) || (gh.to == sys)) && ((gh.from == to) || (gh.to == to)))
                        {
                            NeedsAdd = false;
                            break;
                        }
                    }

                    if (NeedsAdd)
                    {
                        GateHelper g = new GateHelper();
                        g.from = sys;
                        g.to = to;
                        universeSysLinksCache.Add(g);
                    }
                }

                if (sys.ActualX < universeXMin)
                {
                    universeXMin = sys.ActualX;
                }

                if (sys.ActualX > universeXMax)
                {
                    universeXMax = sys.ActualX;
                }

                if (sys.ActualZ < universeZMin)
                {
                    universeZMin = sys.ActualZ;
                }

                if (sys.ActualZ > universeZMax)
                {
                    universeZMax = sys.ActualZ;
                }

            }


            universeWidth = universeXMax - universeXMin;
            universeDepth = universeZMax - universeZMin;

            ReDrawMap(true);
        }

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            ReDrawMap(false);
        }

        private void VHSystems_MouseClicked(object sender, RoutedEventArgs e)
        {
            EVEData.System sys = (EVEData.System)e.OriginalSource;

            VHRangeSpheres.ClearAllChildren();

            double Radius = 9460730472580800.0 * 7.0 * universeScale;
            Brush rangeCol = new SolidColorBrush(Colors.WhiteSmoke);
            Brush sysCentreCol = new SolidColorBrush(Colors.Purple);
            Brush sysRangeCol = new SolidColorBrush(Colors.CornflowerBlue);

            double X = (sys.ActualX - universeXMin) * universeScale; ;
            double Z = (universeDepth - (sys.ActualZ - universeZMin)) * universeScale;



            // Create an instance of a DrawingVisual.
            System.Windows.Media.DrawingVisual rangeCircleDV = new System.Windows.Media.DrawingVisual();

            // Retrieve the DrawingContext from the DrawingVisual.
            DrawingContext drawingContext = rangeCircleDV.RenderOpen();

            drawingContext.DrawEllipse(rangeCol, new Pen(rangeCol, 1), new Point(X, Z), Radius, Radius);
            drawingContext.DrawRectangle(sysCentreCol, new Pen(sysCentreCol, 1), new Rect(X - 5, Z - 5, 10, 10));



            // Close the DrawingContext to persist changes to the DrawingVisual.
            drawingContext.Close();



            VHRangeSpheres.AddChild(rangeCircleDV, "Sphere");

            foreach (EVEData.System es in EM.Systems)
            {

                double Distance = EM.GetRangeBetweenSystems(sys.Name, es.Name);
                Distance = Distance / 9460730472580800.0;

                double Max = 7.0;

                if (Distance < Max && Distance > 0.0)
                {
                    double irX = (es.ActualX - universeXMin) * universeScale; ;
                    double irZ = (universeDepth - (es.ActualZ - universeZMin)) * universeScale;

                    System.Windows.Media.DrawingVisual rangeSquareDV = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext from the DrawingVisual.
                    DrawingContext dcR = rangeSquareDV.RenderOpen();

                    dcR.DrawRectangle(sysRangeCol, new Pen(sysRangeCol, 1), new Rect(irX - 5, irZ - 5, 10, 10));
                    dcR.Close();

                    VHRangeSpheres.AddChild(rangeSquareDV, "SysH");
                }
            }

        }

        private void UniverseControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ReDrawMap(false);
        }







        /// <summary>
        /// Redraw the map
        /// </summary>
        /// <param name="FullRedraw">Clear all the static items or not</param>
        public void ReDrawMap(bool FullRedraw = false)
        {

            double textXOffset = 5;
            double textYOffset = 5;

            double XScale = (5000) / universeWidth;
            double ZScale = (5000) / universeDepth;
            universeScale = Math.Min(XScale, ZScale);



            Brush SysCol = new SolidColorBrush(Colors.Black);
            Brush ConstGateCol = new SolidColorBrush(Colors.LightGray);
            Brush TextCol = new SolidColorBrush(Colors.DarkGray);
            Brush RegionTextCol = new SolidColorBrush(Colors.Black);
            Brush GateCol = new SolidColorBrush(Colors.Gray);
            Brush JBCol = new SolidColorBrush(Colors.Blue);
            Brush DataCol = new SolidColorBrush(Colors.LightPink);

            SysCol.Freeze();
            ConstGateCol.Freeze();
            TextCol.Freeze();
            GateCol.Freeze();
            JBCol.Freeze();


            System.Windows.FontStyle fontStyle = FontStyles.Normal;
            FontWeight fontWeight = FontWeights.Medium;

            if (FullRedraw)
            {

                VHLinks.ClearAllChildren();
                VHNames.ClearAllChildren();
                VHRegionNames.ClearAllChildren();




                foreach (EVEData.MapRegion mr in EM.Regions)
                {
                    double X = (mr.RegionX - universeXMin) * universeScale; ;
                    double Z = (universeDepth - (mr.RegionZ - universeZMin)) * universeScale;

                    // Create an instance of a DrawingVisual.
                    System.Windows.Media.DrawingVisual SystemTextVisual = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext from the DrawingVisual.
                    DrawingContext drawingContext = SystemTextVisual.RenderOpen();

#pragma warning disable CS0618 // 'FormattedText.FormattedText(string, CultureInfo, FlowDirection, Typeface, double, Brush)' is obsolete: 'Use the PixelsPerDip override'
                    // Draw a formatted text string into the DrawingContext.



                    FormattedText ft = new FormattedText(mr.Name, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Verdana"), 60, RegionTextCol);
                    ft.TextAlignment = TextAlignment.Center;

                    drawingContext.DrawText(ft, new Point(X + textXOffset, Z + textYOffset));
#pragma warning restore CS0618 // 'FormattedText.FormattedText(string, CultureInfo, FlowDirection, Typeface, double, Brush)' is obsolete: 'Use the PixelsPerDip override'

                    // Close the DrawingContext to persist changes to the DrawingVisual.
                    drawingContext.Close();

                    VHRegionNames.AddChild(SystemTextVisual, mr.Name);

                }




                foreach (GateHelper gh in universeSysLinksCache)
                {
                    double X1 = (gh.from.ActualX - universeXMin) * universeScale;
                    double Y1 = (universeDepth - (gh.from.ActualZ - universeZMin)) * universeScale;

                    double X2 = (gh.to.ActualX - universeXMin) * universeScale;
                    double Y2 = (universeDepth - (gh.to.ActualZ - universeZMin)) * universeScale;
                    Brush Col = GateCol;

                    if (gh.from.Region != gh.to.Region || gh.from.ConstellationID != gh.to.ConstellationID)
                    {
                        Col = ConstGateCol;
                    }


                    System.Windows.Media.DrawingVisual sysLinkVisual = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext in order to create new drawing content.
                    DrawingContext drawingContext = sysLinkVisual.RenderOpen();

                    // Create a rectangle and draw it in the DrawingContext.
                    drawingContext.DrawLine(new Pen(Col, 1), new Point(X1, Y1), new Point(X2, Y2));

                    drawingContext.Close();

                    VHLinks.AddChild(sysLinkVisual, "link");
                }

                if(ShowJumpBridges)
                {
                    foreach (EVEData.JumpBridge jb in EM.JumpBridges)
                    {
                        Line jbLink = new Line();

                        EVEData.System from = EM.GetEveSystem(jb.From);
                        EVEData.System to = EM.GetEveSystem(jb.To);


                        double X1 = (from.ActualX - universeXMin) * universeScale; ;
                        double Y1 = (universeDepth - (from.ActualZ - universeZMin)) * universeScale;

                        double X2 = (to.ActualX - universeXMin) * universeScale;
                        double Y2 = (universeDepth - (to.ActualZ - universeZMin)) * universeScale;


                        System.Windows.Media.DrawingVisual sysLinkVisual = new System.Windows.Media.DrawingVisual();

                        // Retrieve the DrawingContext in order to create new drawing content.
                        DrawingContext drawingContext = sysLinkVisual.RenderOpen();

                        Pen p = new Pen(JBCol, 1);
                        p.DashStyle = DashStyles.Dot;

                        // Create a rectangle and draw it in the DrawingContext.
                        drawingContext.DrawLine(p, new Point(X1, Y1), new Point(X2, Y2));

                        drawingContext.Close();

                        VHLinks.AddChild(sysLinkVisual, "JB");
                    }
                }


                foreach (EVEData.System sys in EM.Systems)
                {
  
                    double X = (sys.ActualX - universeXMin) * universeScale;

                    // need to invert Z
                    double Z = (universeDepth - (sys.ActualZ - universeZMin)) * universeScale;


                    System.Windows.Media.DrawingVisual systemShapeVisual = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext in order to create new drawing content.
                    DrawingContext drawingContext = systemShapeVisual.RenderOpen();

                    // Create a rectangle and draw it in the DrawingContext.
                    Rect rect = new Rect(new Point(X - 3, Z - 3), new Size(6, 6));
                    drawingContext.DrawRectangle(SysCol, null, rect);

                    // Persist the drawing content.
                    drawingContext.Close();
                    VHSystems.AddChild(systemShapeVisual, sys);

                    // add text


                    // Create an instance of a DrawingVisual.
                    System.Windows.Media.DrawingVisual SystemTextVisual = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext from the DrawingVisual.
                    drawingContext = SystemTextVisual.RenderOpen();

#pragma warning disable CS0618 // 'FormattedText.FormattedText(string, CultureInfo, FlowDirection, Typeface, double, Brush)' is obsolete: 'Use the PixelsPerDip override'
                    // Draw a formatted text string into the DrawingContext.
                    drawingContext.DrawText(
                        new FormattedText(sys.Name,
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Verdana"),
                            6, TextCol),
                        new Point(X + textXOffset, Z + textYOffset));
#pragma warning restore CS0618 // 'FormattedText.FormattedText(string, CultureInfo, FlowDirection, Typeface, double, Brush)' is obsolete: 'Use the PixelsPerDip override'

                    // Close the DrawingContext to persist changes to the DrawingVisual.
                    drawingContext.Close();

                    VHNames.AddChild(SystemTextVisual, sys.Name);

                }
            }




            // update the data
            VHDataSpheres.ClearAllChildren();
            foreach (EVEData.System sys in EM.Systems)
            {

                double X = (sys.ActualX - universeXMin) * universeScale;

                // need to invert Z
                double Z = (universeDepth - (sys.ActualZ - universeZMin)) * universeScale;

                double DataScale = 0;


                if (ShowNPCKills)
                {
                    DataScale = sys.NPCKillsLastHour * ESIOverlayScale * 0.05f;
                }

                if (ShowPodKills)
                {
                    DataScale = sys.PodKillsLastHour * ESIOverlayScale * 2f;
                }

                if (ShowShipKills)
                {
                    DataScale = sys.ShipKillsLastHour * ESIOverlayScale * 8f;
                }

                if (ShowShipJumps)
                {
                    DataScale = sys.JumpsLastHour * ESIOverlayScale * 0.1f;
                }


                if (DataScale > 3)
                {
                    System.Windows.Media.DrawingVisual dataDV = new System.Windows.Media.DrawingVisual();

                    // Retrieve the DrawingContext in order to create new drawing content.
                    DrawingContext drawingContext = dataDV.RenderOpen();


                    // Create a rectangle and draw it in the DrawingContext.
                    drawingContext.DrawEllipse(DataCol, new Pen(DataCol, 1), new Point(X, Z), DataScale, DataScale);

                    drawingContext.Close();

                    VHDataSpheres.AddChild(dataDV, "DATA");

                }
            }


        }

        private void MainZoomControl_ZoomChanged(object sender, RoutedEventArgs e)
        {
            if (MainZoomControl.Zoom < 0.8)
            {
                if (UniverseMainCanvas.Children.Contains(VHNames))
                {
                    UniverseMainCanvas.Children.Remove(VHNames);
                }

                if (!UniverseMainCanvas.Children.Contains(VHRegionNames))
                {
                    UniverseMainCanvas.Children.Add(VHRegionNames);
                }



            }
            else
            {
                if (!UniverseMainCanvas.Children.Contains(VHNames))
                {
                    UniverseMainCanvas.Children.Add(VHNames);
                }

                if (UniverseMainCanvas.Children.Contains(VHRegionNames))
                {
                    UniverseMainCanvas.Children.Remove(VHRegionNames);
                }

            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

    }
}


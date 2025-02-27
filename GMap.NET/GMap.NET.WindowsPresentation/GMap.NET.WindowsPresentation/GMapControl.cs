﻿using System.Threading.Tasks;

namespace GMap.NET.WindowsPresentation
{
	using GMap.NET.Internals;
	using GMap.NET.MapProviders;
	using GMap.NET.Projections;
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Shapes;
	using System.Windows.Threading;

	/// <summary>
	///  GMap.NET control for Windows Presentation
	/// </summary>
	public partial class GMapControl : ItemsControl, Interface, IDisposable
	{
		#region DependencyProperties and related stuff

		/// <summary>
		///  type of map
		/// </summary>
		[Browsable(false)]
		public GMapProvider MapProvider
		{
			get { return GetValue(MapProviderProperty) as GMapProvider; }
			set { SetValue(MapProviderProperty, value); }
		}

		public Point MapPoint
		{
			get { return (Point)GetValue(MapPointProperty); }
			set { SetValue(MapPointProperty, value); }
		}

		public static readonly DependencyProperty CenterPositionProperty = DependencyProperty.Register("CenterPosition",
			typeof(PointLatLng), typeof(GMapControl), new UIPropertyMetadata(new PointLatLng(),
				new PropertyChangedCallback(CenterPositionPropertyChanged)));

		// Using a DependencyProperty as the backing store for point. This enables animation,
		// styling, binding, etc...
		public static readonly DependencyProperty MapPointProperty = DependencyProperty.Register("MapPoint",
			typeof(Point), typeof(GMapControl), new PropertyMetadata(new Point(),
				OnMapPointPropertyChanged));

		private static void OnMapPointPropertyChanged(DependencyObject source,
		   DependencyPropertyChangedEventArgs e)
		{
			Point temp = (Point)e.NewValue;
			(source as GMapControl).Position = new PointLatLng(temp.X, temp.Y);
		}

		public static readonly DependencyProperty MapProviderProperty = DependencyProperty.Register("MapProvider", typeof(GMapProvider),
			typeof(GMapControl), new UIPropertyMetadata(EmptyProvider.Instance,
				new PropertyChangedCallback(MapProviderPropertyChanged)));

		public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register("Zoom", typeof(double),
		   typeof(GMapControl), new UIPropertyMetadata(0.0, new PropertyChangedCallback(ZoomPropertyChanged),
			  new CoerceValueCallback(OnCoerceZoom)));

		/// <summary>
		///  The zoom x property
		/// </summary>
		public static readonly DependencyProperty ZoomXProperty = DependencyProperty.Register("ZoomX", typeof(double),
			typeof(GMapControl), new UIPropertyMetadata(0.0, new PropertyChangedCallback(ZoomXPropertyChanged),
				new CoerceValueCallback(OnCoerceZoom)));

		/// <summary>
		///  The zoom y property
		/// </summary>
		public static readonly DependencyProperty ZoomYProperty = DependencyProperty.Register("ZoomY",
			typeof(double), typeof(GMapControl), new UIPropertyMetadata(0.0, new PropertyChangedCallback(ZoomYPropertyChanged),
				new CoerceValueCallback(OnCoerceZoom)));

		/// <summary>
		///  The multi touch enabled property
		/// </summary>
		public static readonly DependencyProperty MultiTouchEnabledProperty = DependencyProperty.Register("MultiTouchEnabled",
			typeof(bool), typeof(GMapControl), new PropertyMetadata(false, OnMultiTouchEnabledChanged));

		/// <summary>
		///  The touch enabled property
		/// </summary>
		public static readonly DependencyProperty TouchEnabledProperty = DependencyProperty.Register("TouchEnabled",
			typeof(bool), typeof(GMapControl), new PropertyMetadata(false));

		/// <summary>
		///  map zoom
		/// </summary>
		[Category("GMap.NET")]
		public double Zoom
		{
			get { return (double)(GetValue(ZoomProperty)); }
			set { SetValue(ZoomProperty, value); }
		}

		/// <summary>
		///  Map Zoom X
		/// </summary>
		[Category("GMap.NET")]
		public double ZoomX
		{
			get { return (double)(GetValue(ZoomXProperty)); }
			set { SetValue(ZoomXProperty, value); }
		}

		/// <summary>
		///  Map Zoom Y
		/// </summary>
		[Category("GMap.NET")]
		public double ZoomY
		{
			get { return (double)(GetValue(ZoomYProperty)); }
			set { SetValue(ZoomYProperty, value); }
		}

		[Category("GMap.NET")]
		public PointLatLng CenterPosition
		{
			get { return (PointLatLng)GetValue(CenterPositionProperty); }
			set { SetValue(CenterPositionProperty, value); }
		}

		/// <summary>
		///  Specifies, if a floating map scale is displayed using a stretched, or a narrowed map. If
		///  <code>ScaleMode </code>
		///  is
		///  <code>ScaleDown </code>
		///  , then a scale of 12.3 is displayed using a map zoom level of 13 resized to the lower
		///  level. If the parameter is
		///  <code>ScaleUp </code>
		///  , then the same scale is displayed using a zoom level of 12 with an enlarged scale. If
		///  the value is
		///  <code>Dynamic </code>
		///  , then until a remainder of 0.25
		///  <code>ScaleUp </code>
		///  is applied, for bigger remainders
		///  <code>ScaleDown </code>
		///  .
		/// </summary>
		[Category("GMap.NET")]
		[Description("map scale type")]
		public ScaleModes ScaleMode
		{
			get { return scaleMode; }
			set { scaleMode = value; InvalidateVisual(); }
		}

		/// <summary>
		///  Gets or sets a value indicating whether [multi touch enabled].
		/// </summary>
		/// <value>
		///  <c> true </c> if [multi touch enabled]; otherwise, <c> false </c>.
		/// </value>
		[Category("GMap.NET")]
		[Description("Enable pinch map zoom")]
		public bool MultiTouchEnabled
		{
			get { return (bool)(GetValue(MultiTouchEnabledProperty)); }
			set { SetValue(MultiTouchEnabledProperty, value); }
		}

		private static object OnCoerceZoom(DependencyObject o, object value)
		{
			GMapControl map = o as GMapControl;

			if (map != null)
			{
				double result = (double)value;

				if (result > map.MaxZoom)
					result = map.MaxZoom;

				if (result < map.MinZoom)
					result = map.MinZoom;

				return result;
			}
			else
			{
				return value;
			}
		}

		/// <summary>
		///  Centers the position property changed.
		/// </summary>
		/// <param name="obj">
		///  The object.
		/// </param>
		/// <param name="e">
		///  The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.
		/// </param>
		private static void CenterPositionPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var gmapControl = obj as GMapControl;

			if (gmapControl != null && e.NewValue is PointLatLng)
			{
				gmapControl.CenterPosition = gmapControl.Position = (PointLatLng)e.NewValue;
			}
		}

		private static void MapProviderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			GMapControl map = (GMapControl)d;

			if (map != null && e.NewValue != null)
			{
				Debug.WriteLine("MapType: " + e.OldValue + " -> " + e.NewValue);

				RectLatLng viewarea = map.SelectedArea;

				if (viewarea != RectLatLng.Empty)
				{
					map.Position = new PointLatLng(viewarea.Lat - viewarea.HeightLat / 2, viewarea.Lng + viewarea.WidthLng / 2);
				}
				else
				{
					viewarea = map.ViewArea;
				}

				map.Core.Provider = e.NewValue as GMapProvider;

				map.Copyright = null;

				if (!string.IsNullOrEmpty(map.Core.Provider.Copyright))
				{
					map.Copyright = new FormattedText(map.Core.Provider.Copyright, CultureInfo.CurrentUICulture,
					   FlowDirection.LeftToRight, new Typeface("GenericSansSerif"), 9, Brushes.Navy);
				}

				if (map.Core.IsStarted && map.Core.zoomToArea)
				{
					// restore zoomrect as close as possible
					if (viewarea != RectLatLng.Empty && viewarea != map.ViewArea)
					{
						int bestZoom = map.Core.GetMaxZoomToFitRect(viewarea);

						if (bestZoom > 0 && map.Zoom != bestZoom)
							map.Zoom = bestZoom;
					}
				}
			}
		}

		private static void ZoomPropertyChanged(GMapControl mapControl, double value, double oldValue, ZoomMode zoomMode)
		{
			if (mapControl != null && mapControl.MapProvider.Projection != null)
			{
				Debug.WriteLine("Zoom: " + oldValue + " -> " + value);

				double remainder = value % 1;

				if (mapControl.ScaleMode != ScaleModes.Integer && remainder != 0 && mapControl.ActualWidth > 0)
				{
					bool scaleDown;

					switch (mapControl.ScaleMode)
					{
						case ScaleModes.ScaleDown:
							scaleDown = true;
							break;

						case ScaleModes.Dynamic:
							scaleDown = remainder > 0.25;
							break;

						default:
							scaleDown = false;
							break;
					}

					if (scaleDown)
						remainder--;

					double scaleValue = Math.Pow(2d, remainder);
					{
						if (mapControl.MapScaleTransform == null)
						{
							mapControl.MapScaleTransform = mapControl.lastScaleTransform;
						}

						if (zoomMode == ZoomMode.XY || zoomMode == ZoomMode.X)
						{
							mapControl.MapScaleTransform.ScaleX = scaleValue;
							mapControl.Core.scaleX = 1 / scaleValue;
							mapControl.MapScaleTransform.CenterX = mapControl.ActualWidth / 2;
						}

						if (zoomMode == ZoomMode.XY || zoomMode == ZoomMode.Y)
						{
							mapControl.MapScaleTransform.ScaleY = scaleValue;
							mapControl.Core.scaleY = 1 / scaleValue;
							mapControl.MapScaleTransform.CenterY = mapControl.ActualHeight / 2;
						}
					}

					mapControl.Core.Zoom = Convert.ToInt32(scaleDown ? Math.Ceiling(value) : value - remainder);
				}
				else
				{
					mapControl.MapScaleTransform = null;

					if (zoomMode == ZoomMode.XY || zoomMode == ZoomMode.X)
						mapControl.Core.scaleX = 1;

					if (zoomMode == ZoomMode.XY || zoomMode == ZoomMode.Y)
						mapControl.Core.scaleY = 1;

					mapControl.Core.Zoom = (int)Math.Floor(value);
				}

				if (mapControl.IsLoaded)
				{
					mapControl.ForceUpdateOverlays();
					mapControl.InvalidateVisual(true);
				}
			}
		}

		/// <summary>
		///  Zooms the property changed.
		/// </summary>
		/// <param name="d">
		///  The d.
		/// </param>
		/// <param name="e">
		///  The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.
		/// </param>
		private static void ZoomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ZoomPropertyChanged((GMapControl)d, (double)e.NewValue, (double)e.OldValue, ZoomMode.XY);
		}

		/// <summary>
		///  Zooms the x property changed.
		/// </summary>
		/// <param name="d">
		///  The d.
		/// </param>
		/// <param name="e">
		///  The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.
		/// </param>
		private static void ZoomXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ZoomPropertyChanged((GMapControl)d, (double)e.NewValue, (double)e.OldValue, ZoomMode.X);
		}

		/// <summary>
		///  Zooms the y property changed.
		/// </summary>
		/// <param name="d">
		///  The d.
		/// </param>
		/// <param name="e">
		///  The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.
		/// </param>
		private static void ZoomYPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ZoomPropertyChanged((GMapControl)d, (double)e.NewValue, (double)e.OldValue, ZoomMode.Y);
		}

		/// <summary>
		///  Handles the <see cref="E:MultiTouchEnabledChanged" /> event.
		/// </summary>
		/// <param name="d">
		///  The d.
		/// </param>
		/// <param name="e">
		///  The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.
		/// </param>
		private static void OnMultiTouchEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var mapControl = (GMapControl)d;
			mapControl.MultiTouchEnabled = (bool)e.NewValue;
			mapControl.IsManipulationEnabled = (bool)e.NewValue;
		}

		/// <summary>
		///  is touch control enabled
		/// </summary>
		/// <value>
		///  <c> true </c> if [touch enabled]; otherwise, <c> false </c>.
		/// </value>
		[Obsolete("Touch Enabled is deprecated, please use MultiTouchEnabled")]
		public bool TouchEnabled
		{
			get { return (bool)(GetValue(TouchEnabledProperty)); }
			set { SetValue(TouchEnabledProperty, value); }
		}

		private readonly ScaleTransform lastScaleTransform = new ScaleTransform();

		private ScaleModes scaleMode = ScaleModes.Integer;

		#endregion DependencyProperties and related stuff

		private readonly Core Core = new Core();

		//GRect region;
		private delegate void MethodInvoker();

		private PointLatLng selectionStart;
		private PointLatLng selectionEnd;
		private Typeface tileTypeface = new Typeface("Arial");
		private bool showTileGridLines = false;

		private FormattedText Copyright;

		/// <summary>
		///  enables filling empty tiles using lower level images
		/// </summary>
		[Browsable(false)]
		public bool FillEmptyTiles
		{
			get { return Core.fillEmptyTiles; }
			set { Core.fillEmptyTiles = value; }
		}

		/// <summary>
		///  max zoom
		/// </summary>
		[Category("GMap.NET")]
		[Description("maximum zoom level of map")]
		public int MaxZoom
		{
			get { return Core.maxZoom; }
			set { Core.maxZoom = value; }
		}

		/// <summary>
		///  min zoom
		/// </summary>
		[Category("GMap.NET")]
		[Description("minimum zoom level of map")]
		public int MinZoom
		{
			get { return Core.minZoom; }
			set { Core.minZoom = value; }
		}

		/// <summary>
		///  pen for empty tile borders
		/// </summary>
		public Pen EmptyTileBorders = new Pen(Brushes.White, 1.0);

		/// <summary>
		///  pen for Selection
		/// </summary>
		public Pen SelectionPen = new Pen(Brushes.Blue, 2.0);

		/// <summary>
		///  background of selected area
		/// </summary>
		public Brush SelectedAreaFill = new SolidColorBrush(Color.FromArgb(33, Colors.RoyalBlue.R, Colors.RoyalBlue.G, Colors.RoyalBlue.B));

		/// <summary> /// <summary> pen for empty tile background </summary>
		public Brush EmptytileBrush = Brushes.Navy;

		/// <summary>
		///  text on empty tiles
		/// </summary>
		public FormattedText EmptyTileText =
		   new FormattedText("We are sorry, but we don't\nhave imagery at this zoom\n     level for this region.",
			  System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Arial"), 16,
			  Brushes.Blue);

		/// <summary>
		///  map zooming type for mouse wheel
		/// </summary>
		[Category("GMap.NET")]
		[Description("map zooming type for mouse wheel")]
		public MouseWheelZoomType MouseWheelZoomType
		{
			get { return Core.MouseWheelZoomType; }
			set { Core.MouseWheelZoomType = value; }
		}

		/// <summary>
		///  enable map zoom on mouse wheel
		/// </summary>
		[Category("GMap.NET")]
		[Description("enable map zoom on mouse wheel")]
		public bool MouseWheelZoomEnabled
		{
			get { return Core.MouseWheelZoomEnabled; }
			set { Core.MouseWheelZoomEnabled = value; }
		}

		/// <summary>
		///  map dragg button
		/// </summary>
		[Category("GMap.NET")]
		public MouseButton DragButton = MouseButton.Right;

		/// <summary>
		///  use circle for selection
		/// </summary>
		public bool SelectionUseCircle = false;

		/// <summary>
		///  shows tile gridlines
		/// </summary>
		[Category("GMap.NET")]
		public bool ShowTileGridLines
		{
			get { return showTileGridLines; }
			set { showTileGridLines = value; InvalidateVisual(); }
		}

		/// <summary>
		///  retry count to get tile
		/// </summary>
		[Browsable(false)]
		public int RetryLoadTile
		{
			get { return Core.RetryLoadTile; }
			set { Core.RetryLoadTile = value; }
		}

		/// <summary>
		///  how many levels of tiles are staying decompresed in memory
		/// </summary>
		[Browsable(false)]
		public int LevelsKeepInMemmory
		{
			get { return Core.LevelsKeepInMemmory; }
			set { Core.LevelsKeepInMemmory = value; }
		}

		/// <summary>
		///  current selected area in map
		/// </summary>
		private RectLatLng selectedArea;

		[Browsable(false)]
		public RectLatLng SelectedArea
		{
			get { return selectedArea; }
			set { selectedArea = value; InvalidateVisual(); }
		}

		/// <summary>
		///  map boundaries
		/// </summary>
		public RectLatLng? BoundsOfMap = null;

		/// <summary>
		///  occurs when mouse selection is changed
		/// </summary>
		public event SelectionChange OnSelectionChange;

		private static readonly DependencyPropertyKey MarkersKey
		   = DependencyProperty.RegisterReadOnly("Markers", typeof(ObservableCollection<GMapMarker>),
			  typeof(GMapControl),
			  new FrameworkPropertyMetadata(null,
				 FrameworkPropertyMetadataOptions.None,
				 OnMarkersPropChanged));

		public static readonly DependencyProperty MarkersProperty = MarkersKey.DependencyProperty;

		/// <summary>
		///  List of markers
		/// </summary>
		public ObservableCollection<GMapMarker> Markers
		{
			get { return (ObservableCollection<GMapMarker>)GetValue(MarkersProperty); }
			private set { SetValue(MarkersKey, value); }
		}

		private static void OnMarkersPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((GMapControl)d).OnMarkersPropChanged(e);
		}

		private void OnMarkersPropChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		/// <summary>
		///  current markers overlay offset
		/// </summary>
		internal readonly TranslateTransform MapTranslateTransform = new TranslateTransform();

		internal readonly TranslateTransform MapOverlayTranslateTransform = new TranslateTransform();

		internal ScaleTransform MapScaleTransform = new ScaleTransform();
		internal RotateTransform MapRotateTransform = new RotateTransform();

		protected bool DesignModeInConstruct
		{
			get { return System.ComponentModel.DesignerProperties.GetIsInDesignMode(this); }
		}

		private Canvas mapCanvas = null;

		/// <summary>
		///  markers overlay
		/// </summary>
		internal Canvas MapCanvas
		{
			get
			{
				if (mapCanvas == null)
				{
					if (this.VisualChildrenCount > 0)
					{
						Border border = VisualTreeHelper.GetChild(this, 0) as Border;
						ItemsPresenter items = border.Child as ItemsPresenter;
						DependencyObject target = VisualTreeHelper.GetChild(items, 0);
						mapCanvas = target as Canvas;

						mapCanvas.RenderTransform = MapTranslateTransform;
					}
				}

				return mapCanvas;
			}
		}

		public GMaps Manager
		{
			get { return GMaps.Instance; }
		}

		private static DataTemplate DataTemplateInstance;
		private static ItemsPanelTemplate ItemsPanelTemplateInstance;
		private static Style StyleInstance;

		public GMapControl()
		{
			if (!DesignModeInConstruct)
			{
				#region -- templates --

				#region -- xaml --

				//  <ItemsControl Name="figures">
				//    <ItemsControl.ItemTemplate>
				//        <DataTemplate>
				//            <ContentPresenter Content="{Binding Path=Shape}" />
				//        </DataTemplate>
				//    </ItemsControl.ItemTemplate>
				//    <ItemsControl.ItemsPanel>
				//        <ItemsPanelTemplate>
				//            <Canvas />
				//        </ItemsPanelTemplate>
				//    </ItemsControl.ItemsPanel>
				//    <ItemsControl.ItemContainerStyle>
				//        <Style>
				//            <Setter Property="Canvas.Left" Value="{Binding Path=LocalPositionX}"/>
				//            <Setter Property="Canvas.Top" Value="{Binding Path=LocalPositionY}"/>
				//        </Style>
				//    </ItemsControl.ItemContainerStyle>
				//</ItemsControl>

				#endregion -- xaml --

				if (DataTemplateInstance == null)
				{
					DataTemplateInstance = new DataTemplate(typeof(GMapMarker));
					{
						FrameworkElementFactory fef = new FrameworkElementFactory(typeof(ContentPresenter));
						fef.SetBinding(ContentPresenter.ContentProperty, new Binding("Shape"));
						DataTemplateInstance.VisualTree = fef;
					}
				}
				ItemTemplate = DataTemplateInstance;

				if (ItemsPanelTemplateInstance == null)
				{
					var factoryPanel = new FrameworkElementFactory(typeof(Canvas));
					{
						factoryPanel.SetValue(Canvas.IsItemsHostProperty, true);

						ItemsPanelTemplateInstance = new ItemsPanelTemplate();
						{
							ItemsPanelTemplateInstance.VisualTree = factoryPanel;
						}
					}
				}
				ItemsPanel = ItemsPanelTemplateInstance;

				if (StyleInstance == null)
				{
					StyleInstance = new Style();
					{
						StyleInstance.Setters.Add(new Setter(Canvas.LeftProperty, new Binding("LocalPositionX")));
						StyleInstance.Setters.Add(new Setter(Canvas.TopProperty, new Binding("LocalPositionY")));
						StyleInstance.Setters.Add(new Setter(Canvas.ZIndexProperty, new Binding("ZIndex")));
					}
				}
				ItemContainerStyle = StyleInstance;

				#endregion -- templates --

				Markers = new ObservableCollection<GMapMarker>();

				ClipToBounds = true;
				SnapsToDevicePixels = true;

				Core.SystemType = "WindowsPresentation";

				Core.RenderMode = GMap.NET.RenderMode.WPF;

				Core.OnMapZoomChanged += new MapZoomChanged(ForceUpdateOverlays);
				Core.OnCurrentPositionChanged += CoreOnCurrentPositionChanged;
				Loaded += new RoutedEventHandler(GMapControl_Loaded);
				Dispatcher.ShutdownStarted += new EventHandler(Dispatcher_ShutdownStarted);
				SizeChanged += new SizeChangedEventHandler(GMapControl_SizeChanged);

				// by default its internal property, feel free to use your own
				if (ItemsSource == null)
					ItemsSource = Markers;

				Core.Zoom = (int)((double)ZoomProperty.DefaultMetadata.DefaultValue);
			}
		}

		private void CoreOnCurrentPositionChanged(PointLatLng pointLatLng)
		{
			Position = pointLatLng;
		}

		static GMapControl()
		{
			GMapImageProxy.Enable();
#if !PocketPC
			GMaps.Instance.SQLitePing();
#endif
		}

		private void invalidatorEngage(object sender, ProgressChangedEventArgs e)
		{
			base.InvalidateVisual();
		}

		/// <summary>
		///  enque built-in thread safe invalidation
		/// </summary>
		public new void InvalidateVisual()
		{
			if (Core.Refresh != null)
				Core.Refresh.Set();
		}

		/// <summary>
		///  Invalidates the rendering of the element, and forces a complete new layout pass.
		///  System.Windows.UIElement.OnRender(System.Windows.Media.DrawingContext) is called after
		///  the layout cycle is completed. If not forced enques built-in thread safe invalidation
		/// </summary>
		/// <param name="forced">
		/// </param>
		public void InvalidateVisual(bool forced)
		{
			if (forced)
			{
				lock (Core.invalidationLock)
				{
					Core.lastInvalidation = DateTime.Now;
				}

				base.InvalidateVisual();
			}
			else
			{
				InvalidateVisual();
			}
		}

		protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);

			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				ForceUpdateOverlays(e.NewItems);
			}
			else
			{
				InvalidateVisual();
			}
		}

		public void InitializeForBackgroundRendering(int width, int height)
		{
			Width = width;
			Height = height;

			if (!Core.IsStarted)
			{
				if (lazyEvents)
				{
					lazyEvents = false;

					if (lazySetZoomToFitRect.HasValue)
					{
						SetZoomToFitRect(lazySetZoomToFitRect.Value);
						lazySetZoomToFitRect = null;
					}
				}

				Core.OnMapOpen();
				ForceUpdateOverlays();
			}

			Core.OnMapSizeChanged(width, height);

			if (Core.IsStarted)
			{
				if (IsRotated)
				{
					UpdateRotationMatrix();
				}

				ForceUpdateOverlays();
			}

			UpdateLayout();
		}

		/// <summary>
		///  inits core system
		/// </summary>
		/// <param name="sender">
		/// </param>
		/// <param name="e">
		/// </param>
		private void GMapControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (!Core.IsStarted)
			{
				if (lazyEvents)
				{
					lazyEvents = false;

					if (lazySetZoomToFitRect.HasValue)
					{
						SetZoomToFitRect(lazySetZoomToFitRect.Value);
						lazySetZoomToFitRect = null;
					}
				}

				Core.OnMapOpen().ProgressChanged += new ProgressChangedEventHandler(invalidatorEngage);
				ForceUpdateOverlays();

				if (Application.Current != null)
				{
					loadedApp = Application.Current;

					loadedApp.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle,
						new Action(delegate ()
						{
							loadedApp.SessionEnding += new SessionEndingCancelEventHandler(Current_SessionEnding);
						}
						));
				}
			}
		}

		private Application loadedApp;

		private void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
		{
			GMaps.Instance.CancelTileCaching();
		}

		private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
		{
			Dispose();
		}

		/// <summary>
		///  recalculates size
		/// </summary>
		/// <param name="sender">
		/// </param>
		/// <param name="e">
		/// </param>
		private void GMapControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Size constraint = e.NewSize;

			// 50px outside control
			//region = new GRect(-50, -50, (int)constraint.Width + 100, (int)constraint.Height + 100);

			Core.OnMapSizeChanged((int)constraint.Width, (int)constraint.Height);

			if (Core.IsStarted)
			{
				if (IsRotated)
				{
					UpdateRotationMatrix();
				}

				ForceUpdateOverlays();
			}
		}

		private void ForceUpdateOverlays()
		{
			ForceUpdateOverlays(ItemsSource);
		}

		/// <summary>
		///  regenerates shape of route
		/// </summary>
		public virtual void RegenerateShape(IShapable s)
		{
			var marker = s as GMapMarker;

			if (s.Points != null && s.Points.Count > 1)
			{
				marker.Position = s.Points[0];
				var localPath = new List<System.Windows.Point>(s.Points.Count);
				var offset = FromLatLngToLocal(s.Points[0]);

				foreach (var i in s.Points)
				{
					var p = FromLatLngToLocal(i);
					localPath.Add(new System.Windows.Point(p.X - offset.X, p.Y - offset.Y));
				}

				var shape = s.CreatePath(localPath, true);

				if (marker.Shape is Path)
				{
					(marker.Shape as Path).Data = shape.Data;
				}
				else
				{
					marker.Shape = shape;
				}
			}
			else
			{
				marker.Shape = null;
			}
		}

		private void ForceUpdateOverlays(System.Collections.IEnumerable items)
		{
			using (Dispatcher.DisableProcessing())
			{
				UpdateMarkersOffset();

				foreach (GMapMarker i in items)
				{
					if (i != null)
					{
						i.ForceUpdateLocalPosition(this);

						if (i is IShapable)
							RegenerateShape(i as IShapable);
					}
				}
			}
			InvalidateVisual();
		}

		/// <summary>
		///  updates markers overlay offset
		/// </summary>
		private void UpdateMarkersOffset()
		{
			if (MapCanvas != null)
			{
				if (MapScaleTransform != null)
				{
					var tp = MapScaleTransform.Transform(new System.Windows.Point(Core.renderOffset.X, Core.renderOffset.Y));
					MapOverlayTranslateTransform.X = tp.X;
					MapOverlayTranslateTransform.Y = tp.Y;

					// map is scaled already
					MapTranslateTransform.X = Core.renderOffset.X;
					MapTranslateTransform.Y = Core.renderOffset.Y;
				}
				else
				{
					MapTranslateTransform.X = Core.renderOffset.X;
					MapTranslateTransform.Y = Core.renderOffset.Y;

					MapOverlayTranslateTransform.X = MapTranslateTransform.X;
					MapOverlayTranslateTransform.Y = MapTranslateTransform.Y;
				}
			}
		}

		public Brush EmptyMapBackground = Brushes.WhiteSmoke;

		/// <summary>
		///  render map in WPF
		/// </summary>
		/// <param name="g">
		/// </param>
		private void DrawMap(DrawingContext g)
		{
			if (MapProvider == EmptyProvider.Instance || MapProvider == null)
			{
				return;
			}

			Core.tileDrawingListLock.AcquireReaderLock();
			Core.Matrix.EnterReadLock();

			try
			{
				foreach (var tilePoint in Core.tileDrawingList)
				{
					Core.tileRect.Location = tilePoint.PosPixel;
					Core.tileRect.OffsetNegative(Core.compensationOffset);

					//if(region.IntersectsWith(Core.tileRect) || IsRotated)
					{
						bool found = false;

						Tile t = Core.Matrix.GetTileWithNoLock(Core.Zoom, tilePoint.PosXY);

						if (t.NotEmpty)
						{
							foreach (GMapImage img in t.Overlays)
							{
								if (img != null && img.Img != null)
								{
									if (!found)
										found = true;

									var imgRect = new Rect(Core.tileRect.X + 0.6, Core.tileRect.Y + 0.6,
									   Core.tileRect.Width + 0.6, Core.tileRect.Height + 0.6);

									if (!img.IsParent)
									{
										g.DrawImage(img.Img, imgRect);
									}
									else
									{
										// TODO: move calculations to loader thread
										var geometry = new RectangleGeometry(imgRect);
										var parentImgRect =
										   new Rect(Core.tileRect.X - Core.tileRect.Width * img.Xoff + 0.6,
											  Core.tileRect.Y - Core.tileRect.Height * img.Yoff + 0.6,
											  Core.tileRect.Width * img.Ix + 0.6, Core.tileRect.Height * img.Ix + 0.6);

										g.PushClip(geometry);
										g.DrawImage(img.Img, parentImgRect);
										g.Pop();
										geometry = null;
									}
								}
							}
						}
						else if (FillEmptyTiles && MapProvider.Projection is MercatorProjection)
						{
							#region -- fill empty tiles --

							int zoomOffset = 1;
							Tile parentTile = Tile.Empty;
							long Ix = 0;

							while (!parentTile.NotEmpty && zoomOffset < Core.Zoom && zoomOffset <= LevelsKeepInMemmory)
							{
								Ix = (long)Math.Pow(2, zoomOffset);
								parentTile = Core.Matrix.GetTileWithNoLock(Core.Zoom - zoomOffset++,
								   new GMap.NET.GPoint((int)(tilePoint.PosXY.X / Ix), (int)(tilePoint.PosXY.Y / Ix)));
							}

							if (parentTile.NotEmpty)
							{
								long Xoff = Math.Abs(tilePoint.PosXY.X - (parentTile.Pos.X * Ix));
								long Yoff = Math.Abs(tilePoint.PosXY.Y - (parentTile.Pos.Y * Ix));

								var geometry =
								   new RectangleGeometry(new Rect(Core.tileRect.X + 0.6, Core.tileRect.Y + 0.6,
									  Core.tileRect.Width + 0.6, Core.tileRect.Height + 0.6));
								var parentImgRect = new Rect(Core.tileRect.X - Core.tileRect.Width * Xoff + 0.6,
								   Core.tileRect.Y - Core.tileRect.Height * Yoff + 0.6, Core.tileRect.Width * Ix + 0.6,
								   Core.tileRect.Height * Ix + 0.6);

								// render tile
								{
									foreach (GMapImage img in parentTile.Overlays)
									{
										if (img != null && img.Img != null && !img.IsParent)
										{
											if (!found)
												found = true;

											g.PushClip(geometry);
											g.DrawImage(img.Img, parentImgRect);
											g.DrawRectangle(SelectedAreaFill, null, geometry.Bounds);
											g.Pop();
										}
									}
								}

								geometry = null;
							}

							#endregion -- fill empty tiles --
						}

						// add text if tile is missing
						if (!found)
						{
							lock (Core.FailedLoads)
							{
								var lt = new LoadTask(tilePoint.PosXY, Core.Zoom);

								if (Core.FailedLoads.ContainsKey(lt))
								{
									g.DrawRectangle(EmptytileBrush, EmptyTileBorders,
									   new Rect(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height));

									var ex = Core.FailedLoads[lt];

									FormattedText TileText = new FormattedText("Exception: " + ex.Message,
									   CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, tileTypeface, 14, Brushes.Red);

									TileText.MaxTextWidth = Core.tileRect.Width - 11;

									g.DrawText(TileText, new Point(Core.tileRect.X + 11, Core.tileRect.Y + 11));

									g.DrawText(EmptyTileText,
									   new Point(
										  Core.tileRect.X + Core.tileRect.Width / 2 - EmptyTileText.Width / 2,
										  Core.tileRect.Y + Core.tileRect.Height / 2 - EmptyTileText.Height / 2));
								}
							}
						}

						if (ShowTileGridLines)
						{
							g.DrawRectangle(null, EmptyTileBorders,
							   new Rect(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height));

							if (tilePoint.PosXY == Core.centerTileXYLocation)
							{
								FormattedText TileText = new FormattedText("CENTER:" + tilePoint.ToString(),
								   System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
								   tileTypeface, 16, Brushes.Red);
								TileText.MaxTextWidth = Core.tileRect.Width;
								g.DrawText(TileText,
								   new System.Windows.Point(
									  Core.tileRect.X + Core.tileRect.Width / 2 - EmptyTileText.Width / 2,
									  Core.tileRect.Y + Core.tileRect.Height / 2 - TileText.Height / 2));
							}
							else
							{
								FormattedText TileText = new FormattedText("TILE: " + tilePoint.ToString(),
								   System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
								   tileTypeface, 16, Brushes.Red);
								TileText.MaxTextWidth = Core.tileRect.Width;
								g.DrawText(TileText,
								   new System.Windows.Point(
									  Core.tileRect.X + Core.tileRect.Width / 2 - EmptyTileText.Width / 2,
									  Core.tileRect.Y + Core.tileRect.Height / 2 - TileText.Height / 2));
							}
						}
					}
				}
			}
			finally
			{
				Core.Matrix.LeaveReadLock();
				Core.tileDrawingListLock.ReleaseReaderLock();
			}
		}

		/// <summary>
		///  gets image of the current view
		/// </summary>
		/// <returns>
		/// </returns>
		public ImageSource ToImageSource()
		{
			FrameworkElement obj = this;

			// Save current canvas transform
			Transform transform = obj.LayoutTransform;
			obj.LayoutTransform = null;

			// fix margin offset as well
			Thickness margin = obj.Margin;
			obj.Margin = new Thickness(0, 0,
			   margin.Right - margin.Left, margin.Bottom - margin.Top);

			// Get the size of canvas
			Size size = new Size(obj.ActualWidth, obj.ActualHeight);

			// force control to Update
			obj.Measure(size);
			obj.Arrange(new Rect(size));

			RenderTargetBitmap bmp = new RenderTargetBitmap(
			   (int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);

			bmp.Render(obj);

			if (bmp.CanFreeze)
			{
				bmp.Freeze();
			}

			// return values as they were before
			obj.LayoutTransform = transform;
			obj.Margin = margin;

			return bmp;
		}

		/// <summary>
		///  sets zoom to max to fit rect
		/// </summary>
		/// <param name="rect">
		///  area
		/// </param>
		/// <returns>
		/// </returns>
		public bool SetZoomToFitRect(RectLatLng rect)
		{
			if (lazyEvents)
			{
				lazySetZoomToFitRect = rect;
			}
			else
			{
				int maxZoom = Core.GetMaxZoomToFitRect(rect);
				if (maxZoom > 0)
				{
					PointLatLng center = new PointLatLng(rect.Lat - (rect.HeightLat / 2), rect.Lng + (rect.WidthLng / 2));
					Position = center;

					if (maxZoom > MaxZoom)
					{
						maxZoom = MaxZoom;
					}

					if (Core.Zoom != maxZoom)
					{
						Zoom = maxZoom;
					}

					return true;
				}
			}
			return false;
		}

		private RectLatLng? lazySetZoomToFitRect = null;
		private bool lazyEvents = true;

		/// <summary>
		///  sets to max zoom to fit all markers and centers them in map
		/// </summary>
		/// <param name="ZIndex">
		///  z index or null to check all
		/// </param>
		/// <returns>
		/// </returns>
		public bool ZoomAndCenterMarkers(int? ZIndex)
		{
			RectLatLng? rect = GetRectOfAllMarkers(ZIndex);
			if (rect.HasValue)
			{
				return SetZoomToFitRect(rect.Value);
			}

			return false;
		}

		/// <summary>
		///  gets rectangle with all objects inside
		/// </summary>
		/// <param name="ZIndex">
		///  z index or null to check all
		/// </param>
		/// <returns>
		/// </returns>
		public RectLatLng? GetRectOfAllMarkers(int? ZIndex)
		{
			RectLatLng? ret = null;

			double left = double.MaxValue;
			double top = double.MinValue;
			double right = double.MinValue;
			double bottom = double.MaxValue;
			IEnumerable<GMapMarker> Overlays;

			if (ZIndex.HasValue)
			{
				Overlays = ItemsSource.Cast<GMapMarker>().Where(p => p != null && p.ZIndex == ZIndex);
			}
			else
			{
				Overlays = ItemsSource.Cast<GMapMarker>();
			}

			if (Overlays != null)
			{
				foreach (var m in Overlays)
				{
					if (m.Shape != null && m.Shape.Visibility == System.Windows.Visibility.Visible)
					{
						// left
						if (m.Position.Lng < left)
						{
							left = m.Position.Lng;
						}

						// top
						if (m.Position.Lat > top)
						{
							top = m.Position.Lat;
						}

						// right
						if (m.Position.Lng > right)
						{
							right = m.Position.Lng;
						}

						// bottom
						if (m.Position.Lat < bottom)
						{
							bottom = m.Position.Lat;
						}
					}
				}
			}

			if (left != double.MaxValue && right != double.MinValue && top != double.MinValue &&
				bottom != double.MaxValue)
			{
				ret = RectLatLng.FromLTRB(left, top, right, bottom);
			}

			return ret;
		}

		/// <summary>
		///  offset position in pixels
		/// </summary>
		/// <param name="x">
		/// </param>
		/// <param name="y">
		/// </param>
		public void Offset(int x, int y)
		{
			if (IsLoaded)
			{
				if (IsRotated)
				{
					Point p = new Point(x, y);
					p = rotationMatrixInvert.Transform(p);
					x = (int)p.X;
					y = (int)p.Y;

					Core.DragOffset(new GPoint(x, y));

					ForceUpdateOverlays();
				}
				else
				{
					Core.DragOffset(new GPoint(x, y));

					UpdateMarkersOffset();
					InvalidateVisual(true);
				}
			}
		}

		private readonly RotateTransform rotationMatrix = new RotateTransform();
		private GeneralTransform rotationMatrixInvert = new RotateTransform();

		/// <summary>
		///  updates rotation matrix
		/// </summary>
		private void UpdateRotationMatrix()
		{
			System.Windows.Point center = new System.Windows.Point(ActualWidth / 2.0, ActualHeight / 2.0);

			rotationMatrix.Angle = -Bearing;
			rotationMatrix.CenterY = center.Y;
			rotationMatrix.CenterX = center.X;

			rotationMatrixInvert = rotationMatrix.Inverse;
		}

		/// <summary>
		///  returs true if map bearing is not zero
		/// </summary>
		public bool IsRotated
		{
			get { return Core.IsRotated; }
		}

		/// <summary>
		///  bearing for rotation of the map
		/// </summary>
		[Category("GMap.NET")]
		public float Bearing
		{
			get { return Core.bearing; }
			set
			{
				if (Core.bearing != value)
				{
					bool resize = Core.bearing == 0;
					Core.bearing = value;

					UpdateRotationMatrix();

					if (value != 0 && value % 360 != 0)
					{
						Core.IsRotated = true;

						if (Core.tileRectBearing.Size == Core.tileRect.Size)
						{
							Core.tileRectBearing = Core.tileRect;
							Core.tileRectBearing.Inflate(1, 1);
						}
					}
					else
					{
						Core.IsRotated = false;
						Core.tileRectBearing = Core.tileRect;
					}

					if (resize)
					{
						Core.OnMapSizeChanged((int)ActualWidth, (int)ActualHeight);
					}

					if (Core.IsStarted)
					{
						ForceUpdateOverlays();
					}
				}
			}
		}

		/// <summary>
		///  apply transformation if in rotation mode
		/// </summary>
		private System.Windows.Point ApplyRotation(double x, double y)
		{
			System.Windows.Point ret = new System.Windows.Point(x, y);

			if (IsRotated)
			{
				ret = rotationMatrix.Transform(ret);
			}

			return ret;
		}

		/// <summary>
		///  apply transformation if in rotation mode
		/// </summary>
		private System.Windows.Point ApplyRotationInversion(double x, double y)
		{
			System.Windows.Point ret = new System.Windows.Point(x, y);

			if (IsRotated)
			{
				ret = rotationMatrixInvert.Transform(ret);
			}

			return ret;
		}

		#region UserControl Events

		protected override void OnRender(DrawingContext drawingContext)
		{
			if (!Core.IsStarted)
				return;

			drawingContext.DrawRectangle(EmptyMapBackground, null, new Rect(RenderSize));

			if (IsRotated)
			{
				drawingContext.PushTransform(rotationMatrix);

				if (MapScaleTransform != null)
				{
					drawingContext.PushTransform(MapScaleTransform);
					drawingContext.PushTransform(MapTranslateTransform);
					{
						DrawMap(drawingContext);
					}
					drawingContext.Pop();
					drawingContext.Pop();
				}
				else
				{
					drawingContext.PushTransform(MapTranslateTransform);
					{
						DrawMap(drawingContext);
					}
					drawingContext.Pop();
				}

				drawingContext.Pop();
			}
			else
			{
				if (MapScaleTransform != null)
				{
					drawingContext.PushTransform(MapScaleTransform);
					drawingContext.PushTransform(MapTranslateTransform);
					{
						DrawMap(drawingContext);

#if DEBUG
                        drawingContext.DrawLine(VirtualCenterCrossPen, new Point(-20, 0), new Point(20, 0));
                        drawingContext.DrawLine(VirtualCenterCrossPen, new Point(0, -20), new Point(0, 20));
#endif
					}
					drawingContext.Pop();
					drawingContext.Pop();
				}
				else
				{
					drawingContext.PushTransform(MapTranslateTransform);
					{
						DrawMap(drawingContext);
#if DEBUG
                        drawingContext.DrawLine(VirtualCenterCrossPen, new Point(-20, 0), new Point(20, 0));
                        drawingContext.DrawLine(VirtualCenterCrossPen, new Point(0, -20), new Point(0, 20));
#endif
					}
					drawingContext.Pop();
				}
			}

			// selection
			if (!SelectedArea.IsEmpty)
			{
				GPoint p1 = FromLatLngToLocal(SelectedArea.LocationTopLeft);
				GPoint p2 = FromLatLngToLocal(SelectedArea.LocationRightBottom);

				long x1 = p1.X;
				long y1 = p1.Y;
				long x2 = p2.X;
				long y2 = p2.Y;

				if (SelectionUseCircle)
				{
					drawingContext.DrawEllipse(SelectedAreaFill, SelectionPen,
					   new System.Windows.Point(x1 + (x2 - x1) / 2, y1 + (y2 - y1) / 2), (x2 - x1) / 2, (y2 - y1) / 2);
				}
				else
				{
					drawingContext.DrawRoundedRectangle(SelectedAreaFill, SelectionPen,
					   new Rect(x1, y1, x2 - x1, y2 - y1), 5, 5);
				}
			}

			if (ShowCenter)
			{
				drawingContext.DrawLine(CenterCrossPen, new System.Windows.Point((ActualWidth / 2) - 5, ActualHeight / 2),
				   new System.Windows.Point((ActualWidth / 2) + 5, ActualHeight / 2));
				drawingContext.DrawLine(CenterCrossPen, new System.Windows.Point(ActualWidth / 2, (ActualHeight / 2) - 5),
				   new System.Windows.Point(ActualWidth / 2, (ActualHeight / 2) + 5));
			}

			if (renderHelperLine)
			{
				var p = Mouse.GetPosition(this);

				drawingContext.DrawLine(HelperLinePen, new Point(p.X, 0), new Point(p.X, ActualHeight));
				drawingContext.DrawLine(HelperLinePen, new Point(0, p.Y), new Point(ActualWidth, p.Y));
			}

			#region -- copyright --

			if (Copyright != null)
			{
				drawingContext.DrawText(Copyright, new System.Windows.Point(5, ActualHeight - Copyright.Height - 5));
			}

			#endregion -- copyright --

			base.OnRender(drawingContext);
		}

		public Pen CenterCrossPen = new Pen(Brushes.Red, 1);
		public bool ShowCenter = true;

#if DEBUG
        readonly Pen VirtualCenterCrossPen = new Pen(Brushes.Blue, 1);
#endif

		private HelperLineOptions helperLineOption = HelperLineOptions.DontShow;

		/// <summary>
		///  draw lines at the mouse pointer position
		/// </summary>
		[Browsable(false)]
		public HelperLineOptions HelperLineOption
		{
			get { return helperLineOption; }
			set
			{
				helperLineOption = value;
				renderHelperLine = (helperLineOption == HelperLineOptions.ShowAlways);
				if (Core.IsStarted)
				{
					InvalidateVisual();
				}
			}
		}

		public Pen HelperLinePen = new Pen(Brushes.Blue, 1);
		private bool renderHelperLine = false;

		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);

			if (HelperLineOption == HelperLineOptions.ShowOnModifierKey)
			{
				renderHelperLine = !(e.IsUp && (e.Key == Key.LeftShift || e.SystemKey == Key.LeftAlt));
				if (!renderHelperLine)
				{
					InvalidateVisual();
				}
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (HelperLineOption == HelperLineOptions.ShowOnModifierKey)
			{
				renderHelperLine = e.IsDown && (e.Key == Key.LeftShift || e.SystemKey == Key.LeftAlt);
				if (renderHelperLine)
				{
					InvalidateVisual();
				}
			}
		}

		/// <summary>
		///  Reverses MouseWheel zooming direction
		/// </summary>
		public static readonly DependencyProperty InvertedMouseWheelZoomingProperty = DependencyProperty.Register(
		   "InvertedMouseWheelZooming", typeof(bool), typeof(GMapControl), new PropertyMetadata(false));

		public bool InvertedMouseWheelZooming
		{
			get { return (bool)GetValue(InvertedMouseWheelZoomingProperty); }
			set { SetValue(InvertedMouseWheelZoomingProperty, value); }
		}

		/// <summary>
		///  Lets you zoom by MouseWheel even when pointer is in area of marker
		/// </summary>
		public static readonly DependencyProperty IgnoreMarkerOnMouseWheelProperty = DependencyProperty.Register(
		   "IgnoreMarkerOnMouseWheel", typeof(bool), typeof(GMapControl), new PropertyMetadata(false));

		public bool IgnoreMarkerOnMouseWheel
		{
			get { return (bool)GetValue(IgnoreMarkerOnMouseWheelProperty); }
			set { SetValue(IgnoreMarkerOnMouseWheelProperty, value); }
		}

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);

			if (MouseWheelZoomEnabled && (IsMouseDirectlyOver || IgnoreMarkerOnMouseWheel) && !Core.IsDragging)
			{
				System.Windows.Point p = e.GetPosition(this);

				if (MapScaleTransform != null)
				{
					p = MapScaleTransform.Inverse.Transform(p);
				}

				p = ApplyRotationInversion(p.X, p.Y);

				if (Core.mouseLastZoom.X != (int)p.X && Core.mouseLastZoom.Y != (int)p.Y)
				{
					if (MouseWheelZoomType == MouseWheelZoomType.MousePositionAndCenter)
					{
						Core.position = FromLocalToLatLng((int)p.X, (int)p.Y);
					}
					else if (MouseWheelZoomType == MouseWheelZoomType.ViewCenter)
					{
						Core.position = FromLocalToLatLng((int)ActualWidth / 2, (int)ActualHeight / 2);
					}
					else if (MouseWheelZoomType == MouseWheelZoomType.MousePositionWithoutCenter)
					{
						Core.position = FromLocalToLatLng((int)p.X, (int)p.Y);
					}

					Core.mouseLastZoom.X = (int)p.X;
					Core.mouseLastZoom.Y = (int)p.Y;
				}

				// set mouse position to map center
				if (MouseWheelZoomType != MouseWheelZoomType.MousePositionWithoutCenter)
				{
					System.Windows.Point ps = PointToScreen(new System.Windows.Point(ActualWidth / 2, ActualHeight / 2));
					Stuff.SetCursorPos((int)ps.X, (int)ps.Y);
				}

				Core.MouseWheelZooming = true;

				if (e.Delta > 0)
				{
					if (!InvertedMouseWheelZooming)
					{
						Zoom = ((int)Zoom) + 1;
					}
					else
					{
						Zoom = ((int)(Zoom + 0.99)) - 1;
					}
				}
				else
				{
					if (InvertedMouseWheelZooming)
					{
						Zoom = ((int)Zoom) + 1;
					}
					else
					{
						Zoom = ((int)(Zoom + 0.99)) - 1;
					}
				}

				Core.MouseWheelZooming = false;
			}
		}

		private bool isSelected = false;

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);

			if (CanDragMap && e.ChangedButton == DragButton)
			{
				Point p = e.GetPosition(this);

				if (MapScaleTransform != null)
				{
					p = MapScaleTransform.Inverse.Transform(p);
				}

				p = ApplyRotationInversion(p.X, p.Y);

				Core.mouseDown.X = (int)p.X;
				Core.mouseDown.Y = (int)p.Y;

				InvalidateVisual();
			}
			else
			{
				if (!isSelected)
				{
					Point p = e.GetPosition(this);
					isSelected = true;
					SelectedArea = RectLatLng.Empty;
					selectionEnd = PointLatLng.Empty;
					selectionStart = FromLocalToLatLng((int)p.X, (int)p.Y);
				}
			}
		}

		private int onMouseUpTimestamp = 0;

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);

			if (isSelected)
			{
				isSelected = false;
			}

			if (Core.IsDragging)
			{
				if (isDragging)
				{
					onMouseUpTimestamp = e.Timestamp & Int32.MaxValue;
					isDragging = false;
					Debug.WriteLine("IsDragging = " + isDragging);
					Cursor = cursorBefore;
					Mouse.Capture(null);
				}
				Core.EndDrag();

				if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
				{
					if (Core.LastLocationInBounds.HasValue)
					{
						Position = Core.LastLocationInBounds.Value;
					}
				}
			}
			else
			{
				if (e.ChangedButton == DragButton)
				{
					Core.mouseDown = GPoint.Empty;
				}

				if (!selectionEnd.IsEmpty && !selectionStart.IsEmpty)
				{
					bool zoomtofit = false;

					if (!SelectedArea.IsEmpty && Keyboard.Modifiers == ModifierKeys.Shift)
					{
						zoomtofit = SetZoomToFitRect(SelectedArea);
					}

					OnSelectionChange?.Invoke(SelectedArea, zoomtofit);
				}
				else
				{
					InvalidateVisual();
				}
			}
		}

		private Cursor cursorBefore = Cursors.Arrow;

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			// wpf generates to many events if mouse is over some visual and OnMouseUp is fired,
			// wtf, anyway... http://greatmaps.codeplex.com/workitem/16013
			if ((e.Timestamp & Int32.MaxValue) - onMouseUpTimestamp < 55)
			{
				Debug.WriteLine("OnMouseMove skipped: " + ((e.Timestamp & Int32.MaxValue) - onMouseUpTimestamp) + "ms");
				return;
			}

			if (!Core.IsDragging && !Core.mouseDown.IsEmpty)
			{
				Point p = e.GetPosition(this);

				if (MapScaleTransform != null)
				{
					p = MapScaleTransform.Inverse.Transform(p);
				}

				p = ApplyRotationInversion(p.X, p.Y);

				// cursor has moved beyond drag tolerance
				if (Math.Abs(p.X - Core.mouseDown.X) * 2 >= SystemParameters.MinimumHorizontalDragDistance ||
					Math.Abs(p.Y - Core.mouseDown.Y) * 2 >= SystemParameters.MinimumVerticalDragDistance)
				{
					if (Math.Abs(p.X - Core.mouseDown.X) * 2 >= SystemParameters.MinimumHorizontalDragDistance || Math.Abs(p.Y - Core.mouseDown.Y) * 2 >= SystemParameters.MinimumVerticalDragDistance)
					{
						Core.BeginDrag(Core.mouseDown);
					}
				}
			}

			if (Core.IsDragging)
			{
				if (!isDragging)
				{
					isDragging = true;
					Debug.WriteLine("IsDragging = " + isDragging);
					cursorBefore = Cursor;
					Cursor = Cursors.SizeAll;
					Mouse.Capture(this);
				}

				if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
				{
					// ...
				}
				else
				{
					Point p = e.GetPosition(this);

					if (MapScaleTransform != null)
					{
						p = MapScaleTransform.Inverse.Transform(p);
					}

					p = ApplyRotationInversion(p.X, p.Y);

					Core.mouseCurrent.X = (int)p.X;
					Core.mouseCurrent.Y = (int)p.Y;
					{
						Core.Drag(Core.mouseCurrent);
					}

					if (IsRotated || scaleMode != ScaleModes.Integer)
					{
						ForceUpdateOverlays();
					}
					else
					{
						UpdateMarkersOffset();
					}
				}
				InvalidateVisual(true);
			}
			else
			{
				if (isSelected && !selectionStart.IsEmpty &&
					(Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Alt ||
					 DisableAltForSelection))
				{
					System.Windows.Point p = e.GetPosition(this);
					selectionEnd = FromLocalToLatLng((int)p.X, (int)p.Y);
					{
						GMap.NET.PointLatLng p1 = selectionStart;
						GMap.NET.PointLatLng p2 = selectionEnd;

						double x1 = Math.Min(p1.Lng, p2.Lng);
						double y1 = Math.Max(p1.Lat, p2.Lat);
						double x2 = Math.Max(p1.Lng, p2.Lng);
						double y2 = Math.Min(p1.Lat, p2.Lat);

						SelectedArea = new RectLatLng(y1, x1, x2 - x1, y1 - y2);
					}
				}

				if (renderHelperLine)
				{
					InvalidateVisual(true);
				}
			}
		}

		/// <summary>
		///  if true, selects area just by holding mouse and moving
		/// </summary>
		public bool DisableAltForSelection = false;

		protected override void OnStylusDown(StylusDownEventArgs e)
		{
			base.OnStylusDown(e);

			if (TouchEnabled && CanDragMap && !e.InAir)
			{
				Point p = e.GetPosition(this);

				if (MapScaleTransform != null)
				{
					p = MapScaleTransform.Inverse.Transform(p);
				}

				p = ApplyRotationInversion(p.X, p.Y);

				Core.mouseDown.X = (int)p.X;
				Core.mouseDown.Y = (int)p.Y;

				InvalidateVisual();
			}
		}

		protected override void OnStylusUp(StylusEventArgs e)
		{
			base.OnStylusUp(e);

			if (TouchEnabled)
			{
				if (isSelected)
				{
					isSelected = false;
				}

				if (Core.IsDragging)
				{
					if (isDragging)
					{
						onMouseUpTimestamp = e.Timestamp & Int32.MaxValue;
						isDragging = false;
						Debug.WriteLine("IsDragging = " + isDragging);
						Cursor = cursorBefore;
						Mouse.Capture(null);
					}
					Core.EndDrag();

					if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
					{
						if (Core.LastLocationInBounds.HasValue)
						{
							Position = Core.LastLocationInBounds.Value;
						}
					}
				}
				else
				{
					Core.mouseDown = GPoint.Empty;
					InvalidateVisual();
				}
			}
		}

		protected override void OnStylusMove(StylusEventArgs e)
		{
			base.OnStylusMove(e);

			if (TouchEnabled)
			{
				// wpf generates to many events if mouse is over some visual and OnMouseUp is fired,
				// wtf, anyway... http://greatmaps.codeplex.com/workitem/16013
				if ((e.Timestamp & Int32.MaxValue) - onMouseUpTimestamp < 55)
				{
					Debug.WriteLine("OnMouseMove skipped: " + ((e.Timestamp & Int32.MaxValue) - onMouseUpTimestamp) +
									"ms");
					return;
				}

				if (!Core.IsDragging && !Core.mouseDown.IsEmpty)
				{
					Point p = e.GetPosition(this);

					if (MapScaleTransform != null)
					{
						p = MapScaleTransform.Inverse.Transform(p);
					}

					p = ApplyRotationInversion(p.X, p.Y);

					// cursor has moved beyond drag tolerance
					if (Math.Abs(p.X - Core.mouseDown.X) * 2 >= SystemParameters.MinimumHorizontalDragDistance ||
						Math.Abs(p.Y - Core.mouseDown.Y) * 2 >= SystemParameters.MinimumVerticalDragDistance)
					{
						Core.BeginDrag(Core.mouseDown);
					}
				}

				if (Core.IsDragging)
				{
					if (!isDragging)
					{
						isDragging = true;
						Debug.WriteLine("IsDragging = " + isDragging);
						cursorBefore = Cursor;
						Cursor = Cursors.SizeAll;
						Mouse.Capture(this);
					}

					if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
					{
						// ...
					}
					else
					{
						Point p = e.GetPosition(this);

						if (MapScaleTransform != null)
						{
							p = MapScaleTransform.Inverse.Transform(p);
						}

						p = ApplyRotationInversion(p.X, p.Y);

						Core.mouseCurrent.X = (int)p.X;
						Core.mouseCurrent.Y = (int)p.Y;
						{
							Core.Drag(Core.mouseCurrent);
						}

						if (IsRotated)
						{
							ForceUpdateOverlays();
						}
						else
						{
							UpdateMarkersOffset();
						}
					}
					InvalidateVisual();
				}
			}
		}

		/// <summary>
		///  Called when the <see cref="E:System.Windows.UIElement.ManipulationDelta" /> event occurs.
		/// </summary>
		/// <param name="e">
		///  The data for the event.
		/// </param>
		protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
		{
			base.OnManipulationDelta(e);

			if (MultiTouchEnabled && !TouchEnabled)
			{
				var touchPoints = e.Manipulators.ToArray();
				var element = e.Source as FrameworkElement;

				if (element != null)
				{
					var delta = e.DeltaManipulation;

					if (touchPoints.Length == 1)
					{
						SingleTouchPanMap(new Point(delta.Translation.X, delta.Translation.Y));
					}
					else if (touchPoints.Length >= 2)
					{
						Point centerOfTouchPoints = e.ManipulationOrigin;
						ZoomX *= delta.Scale.X;
						ZoomY *= delta.Scale.Y;
					}

					e.Handled = true;
				}
			}
		}

		/// <summary>
		///  Singles the touch pan map.
		/// </summary>
		/// <param name="deltaPoint">
		///  The delta point.
		/// </param>
		protected virtual void SingleTouchPanMap(Point deltaPoint)
		{
			if (MultiTouchEnabled && !TouchEnabled) // redundent check in case this is invoked outside of the manipulation events
			{
				if (!Core.IsDragging)
				{
					deltaPoint = ApplyRotationInversion(deltaPoint.X, deltaPoint.Y);

					// cursor has moved beyond drag tolerance
					if (Math.Abs(deltaPoint.X - Core.mouseDown.X) * 2 >= SystemParameters.MinimumHorizontalDragDistance || Math.Abs(deltaPoint.Y - Core.mouseDown.Y) * 2 >= SystemParameters.MinimumVerticalDragDistance)
					{
						Core.BeginDrag(Core.mouseDown);
					}
				}

				if (Core.IsDragging)
				{
					if (!isDragging)
					{
						isDragging = true;
						Debug.WriteLine("IsDragging = " + isDragging);
						cursorBefore = Cursor;
						Cursor = Cursors.SizeAll;
						Mouse.Capture(this);
					}

					if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
					{
						// ...
					}
					else
					{
						deltaPoint = ApplyRotationInversion(deltaPoint.X, deltaPoint.Y);

						Core.mouseCurrent.X += (int)deltaPoint.X;
						Core.mouseCurrent.Y += (int)deltaPoint.Y;
						{
							Core.Drag(Core.mouseCurrent);
						}

						if (IsRotated)
						{
							ForceUpdateOverlays();
						}
						else
						{
							UpdateMarkersOffset();
						}
					}
					InvalidateVisual();
				}
			}
		}

		/// <summary>
		///  Called when the <see cref="E:System.Windows.UIElement.ManipulationCompleted" /> event occurs.
		/// </summary>
		/// <param name="e">
		///  The data for the event.
		/// </param>
		protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
		{
			base.OnManipulationCompleted(e);

			if (MultiTouchEnabled && !TouchEnabled)
			{
				var touchPoints = e.Manipulators.ToArray();
				if (true)
				{ // add bool to starting for single touch vs multi touch
					if (isSelected)
					{
						isSelected = false;
					}

					if (Core.IsDragging)
					{
						if (isDragging)
						{
							onMouseUpTimestamp = e.Timestamp & Int32.MaxValue;
							isDragging = false;
							Debug.WriteLine("IsDragging = " + isDragging);
							Cursor = cursorBefore;
							Mouse.Capture(null);
						}
						Core.EndDrag();

						if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
						{
							if (Core.LastLocationInBounds.HasValue)
							{
								Position = Core.LastLocationInBounds.Value;
							}
						}
					}
					else
					{
						Core.mouseDown = GPoint.Empty;
						InvalidateVisual();
					}
				}
			}
		}

		private int change = 0;
		private Dictionary<int, Point> movingPoints = new Dictionary<int, Point>();

		private double calcdist(double x1, double y1, double x2, double y2)
		{
			return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
		}

		protected override void OnTouchDown(TouchEventArgs e)
		{
			base.OnTouchDown(e);

			if (MultiTouchEnabled)
			{
				TouchPoint touchpoint = e.GetTouchPoint(this);
				Point point = new Point();
				point.X = touchpoint.Position.X;
				point.Y = touchpoint.Position.Y;
				movingPoints[e.TouchDevice.Id] = point;

				if (movingPoints.Count == 0)
				{
					if (MapScaleTransform != null)
					{
						point = MapScaleTransform.Inverse.Transform(point);
					}

					point = ApplyRotationInversion(point.X, point.Y);
					Core.mouseDown.X = (int)point.X;
					Core.mouseDown.Y = (int)point.Y;
					InvalidateVisual();
				}
			}
		}

		protected override void OnTouchMove(TouchEventArgs e)
		{
			base.OnTouchMove(e);

			if (MultiTouchEnabled)
			{
				if (movingPoints.Count == 1)
				{
					if (movingPoints.Keys.Contains(e.TouchDevice.Id))
					{
						if ((e.Timestamp & Int32.MaxValue) - onMouseUpTimestamp < 55)
						{
							Debug.WriteLine("OnMouseMove skipped: " + ((e.Timestamp & Int32.MaxValue) - onMouseUpTimestamp) + "ms");
							return;
						}

						if (!Core.IsDragging)
						{
							TouchPoint touchpoint = e.GetTouchPoint(this);
							Point p = new Point();
							p.X = touchpoint.Position.X;
							p.Y = touchpoint.Position.Y;

							if (MapScaleTransform != null)
							{
								p = MapScaleTransform.Inverse.Transform(p);
							}

							p = ApplyRotationInversion(p.X, p.Y);

							// cursor has moved beyond drag tolerance
							if (Math.Abs(p.X - movingPoints[e.TouchDevice.Id].X) * 2 >= SystemParameters.MinimumHorizontalDragDistance || Math.Abs(p.Y - movingPoints[e.TouchDevice.Id].Y) * 2 >= SystemParameters.MinimumVerticalDragDistance)
							{
								GPoint gp = new GPoint();
								gp.X = (int)movingPoints[e.TouchDevice.Id].X;
								gp.Y = (int)movingPoints[e.TouchDevice.Id].Y;
								Core.BeginDrag(gp);
							}
						}

						if (Core.IsDragging)
						{
							if (!isDragging)
							{
								isDragging = true;
								Debug.WriteLine("IsDragging = " + isDragging);
								cursorBefore = Cursor;
								Cursor = Cursors.SizeAll;
								Mouse.Capture(this);
							}

							if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
							{
								// ...
							}
							else
							{
								TouchPoint touchpoint = e.GetTouchPoint(this);
								Point p = new Point();
								p.X = touchpoint.Position.X;
								p.Y = touchpoint.Position.Y;

								if (MapScaleTransform != null)
								{
									p = MapScaleTransform.Inverse.Transform(p);
								}

								p = ApplyRotationInversion(p.X, p.Y);

								Core.mouseCurrent.X = (int)p.X;
								Core.mouseCurrent.Y = (int)p.Y;
								{
									Core.Drag(Core.mouseCurrent);
								}

								if (IsRotated)
								{
									ForceUpdateOverlays();
								}
								else
								{
									UpdateMarkersOffset();
								}
							}
							InvalidateVisual();
						}
					}
				}
				else if (movingPoints.Count == 2)
				{
					if (movingPoints.Keys.Contains(e.TouchDevice.Id))
					{
						Point point1 = new Point();
						Point point2 = new Point();
						double nowdistance = 0;
						double predistance = 0;
						int count = 0;

						foreach (var item in movingPoints)
						{
							if (count == 0)
								point1 = item.Value;
							else
								point2 = item.Value;
							count++;
						}

						predistance = calcdist(point1.X, point1.Y, point2.X, point2.Y);
						TouchPoint touchpoint = e.GetTouchPoint(this);
						Point npoint = new Point();
						npoint.X = touchpoint.Position.X;
						npoint.Y = touchpoint.Position.Y;

						if (movingPoints[e.TouchDevice.Id] == point1)
						{
							nowdistance = calcdist(npoint.X, npoint.Y, point2.X, point2.Y);
						}
						else
						{
							nowdistance = calcdist(npoint.X, npoint.Y, point1.X, point1.Y);
						}

						//movingPoints[e.TouchDevice.Id] = npoint;
						if (change <= 2)
						{
							if (nowdistance - predistance > 10)
							{
								Zoom += 0.5;
								change++;
							}
							else if (nowdistance - predistance < -10)
							{
								Zoom -= 0.5;
								change++;
							}
						}
					}
				}
			}
		}

		protected override void OnTouchUp(TouchEventArgs e)
		{
			base.OnTouchUp(e);

			if (MultiTouchEnabled && !TouchEnabled)
			{
				change = 0;
				movingPoints.Remove(e.TouchDevice.Id);

				if (true) // add bool to starting for single touch vs multi touch
				{
					if (isSelected)
					{
						isSelected = false;
					}

					if (Core.IsDragging)
					{
						if (isDragging)
						{
							onMouseUpTimestamp = e.Timestamp & Int32.MaxValue;
							isDragging = false;
							Debug.WriteLine("IsDragging = " + isDragging);
							Cursor = cursorBefore;
							Mouse.Capture(null);
						}

						Core.EndDrag();

						if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
						{
							if (Core.LastLocationInBounds.HasValue)
							{
								Position = Core.LastLocationInBounds.Value;
							}
						}
					}
					else
					{
						Core.mouseDown = GPoint.Empty;
						InvalidateVisual();
					}
				}
			}
		}

		#endregion UserControl Events

		#region IGControl Members

		/// <summary> Call it to empty tile cache & reload tiles </summary>
		public void ReloadMap()
		{
			Core.ReloadMap();
		}

		public Task ReloadMapAsync()
		{
			return Core.ReloadMapAsync();
		}

		/// <summary>
		///  sets position using geocoder
		/// </summary>
		/// <param name="keys">
		/// </param>
		/// <returns>
		/// </returns>
		public GeoCoderStatusCode SetPositionByKeywords(string keys)
		{
			GeoCoderStatusCode status = GeoCoderStatusCode.UNKNOWN_ERROR;

			GeocodingProvider gp = MapProvider as GeocodingProvider;
			if (gp == null)
			{
				gp = GMapProviders.OpenStreetMap as GeocodingProvider;
			}

			if (gp != null)
			{
				var pt = gp.GetPoint(keys, out status);
				if (status == GeoCoderStatusCode.OK && pt.HasValue)
				{
					Position = pt.Value;
				}
			}

			return status;
		}

		public PointLatLng FromLocalToLatLng(int x, int y)
		{
			if (MapScaleTransform != null)
			{
				var tp = MapScaleTransform.Inverse.Transform(new System.Windows.Point(x, y));
				x = (int)tp.X;
				y = (int)tp.Y;
			}

			if (IsRotated)
			{
				var f = rotationMatrixInvert.Transform(new System.Windows.Point(x, y));

				x = (int)f.X;
				y = (int)f.Y;
			}

			return Core.FromLocalToLatLng(x, y);
		}

		public GPoint FromLatLngToLocal(PointLatLng point)
		{
			GPoint ret = Core.FromLatLngToLocal(point);

			if (MapScaleTransform != null)
			{
				var tp = MapScaleTransform.Transform(new System.Windows.Point(ret.X, ret.Y));
				ret.X = (int)tp.X;
				ret.Y = (int)tp.Y;
			}

			if (IsRotated)
			{
				var f = rotationMatrix.Transform(new System.Windows.Point(ret.X, ret.Y));

				ret.X = (int)f.X;
				ret.Y = (int)f.Y;
			}

			return ret;
		}

		public bool ShowExportDialog()
		{
			var dlg = new Microsoft.Win32.SaveFileDialog();
			{
				dlg.CheckPathExists = true;
				dlg.CheckFileExists = false;
				dlg.AddExtension = true;
				dlg.DefaultExt = "gmdb";
				dlg.ValidateNames = true;
				dlg.Title = "GMap.NET: Export map to db, if file exsist only new data will be added";
				dlg.FileName = "DataExp";
				dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				dlg.Filter = "GMap.NET DB files (*.gmdb)|*.gmdb";
				dlg.FilterIndex = 1;
				dlg.RestoreDirectory = true;

				if (dlg.ShowDialog() == true)
				{
					bool ok = GMaps.Instance.ExportToGMDB(dlg.FileName);
					if (ok)
					{
						MessageBox.Show("Complete!", "GMap.NET", MessageBoxButton.OK, MessageBoxImage.Information);
					}
					else
					{
						MessageBox.Show("  Failed!", "GMap.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
					}

					return ok;
				}
			}

			return false;
		}

		public bool ShowImportDialog()
		{
			var dlg = new Microsoft.Win32.OpenFileDialog();
			{
				dlg.CheckPathExists = true;
				dlg.CheckFileExists = false;
				dlg.AddExtension = true;
				dlg.DefaultExt = "gmdb";
				dlg.ValidateNames = true;
				dlg.Title = "GMap.NET: Import to db, only new data will be added";
				dlg.FileName = "DataImport";
				dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				dlg.Filter = "GMap.NET DB files (*.gmdb)|*.gmdb";
				dlg.FilterIndex = 1;
				dlg.RestoreDirectory = true;

				if (dlg.ShowDialog() == true)
				{
					Cursor = Cursors.Wait;

					bool ok = GMaps.Instance.ImportFromGMDB(dlg.FileName);
					if (ok)
					{
						MessageBox.Show("Complete!", "GMap.NET", MessageBoxButton.OK, MessageBoxImage.Information);
						ReloadMap();
					}
					else
					{
						MessageBox.Show("  Failed!", "GMap.NET", MessageBoxButton.OK, MessageBoxImage.Warning);
					}

					Cursor = Cursors.Arrow;

					return ok;
				}
			}

			return false;
		}

		public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
		   "Position", typeof(PointLatLng), typeof(GMapControl),
		   new FrameworkPropertyMetadata(default(PointLatLng), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
			  PositionChangedCallBack));

		/// <summary>
		///  Current coordinates of the map center
		/// </summary>
		[Browsable(false)]
		public PointLatLng Position
		{
			get { return (PointLatLng)GetValue(PositionProperty); }
			set { SetValue(PositionProperty, value); }
		}

		private static void PositionChangedCallBack(DependencyObject source,
		   DependencyPropertyChangedEventArgs e)
		{
			((GMapControl)source).PositionChanged(e);
		}

		private void PositionChanged(DependencyPropertyChangedEventArgs e)
		{
			Core.Position = Position;
			if (Core.IsStarted)
			{
				ForceUpdateOverlays();
			}
		}

		[Browsable(false)]
		public GPoint PositionPixel
		{
			get { return Core.PositionPixel; }
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public string CacheLocation
		{
			get { return CacheLocator.Location; }
			set { CacheLocator.Location = value; }
		}

		private bool isDragging = false;

		[Browsable(false)]
		public bool IsDragging
		{
			get { return isDragging; }
		}

		[Browsable(false)]
		public RectLatLng ViewArea
		{
			get
			{
				if (!IsRotated)
				{
					return Core.ViewArea;
				}
				else if (Core.Provider.Projection != null)
				{
					var p = FromLocalToLatLng(0, 0);
					var p2 = FromLocalToLatLng((int)Width, (int)Height);

					return RectLatLng.FromLTRB(p.Lng, p.Lat, p2.Lng, p2.Lat);
				}
				return RectLatLng.Empty;
			}
		}

		[Category("GMap.NET")]
		public bool CanDragMap
		{
			get { return Core.CanDragMap; }
			set { Core.CanDragMap = value; }
		}

		public GMap.NET.RenderMode RenderMode
		{
			get { return GMap.NET.RenderMode.WPF; }
		}

		#endregion IGControl Members

		#region IGControl event Members

		public event PositionChanged OnPositionChanged
		{
			add { Core.OnCurrentPositionChanged += value; }
			remove { Core.OnCurrentPositionChanged -= value; }
		}

		public event TileLoadComplete OnTileLoadComplete
		{
			add { Core.OnTileLoadComplete += value; }
			remove { Core.OnTileLoadComplete -= value; }
		}

		public event TileLoadStart OnTileLoadStart
		{
			add { Core.OnTileLoadStart += value; }
			remove { Core.OnTileLoadStart -= value; }
		}

		public event MapDrag OnMapDrag
		{
			add { Core.OnMapDrag += value; }
			remove { Core.OnMapDrag -= value; }
		}

		public event MapZoomChanged OnMapZoomChanged
		{
			add { Core.OnMapZoomChanged += value; }
			remove { Core.OnMapZoomChanged -= value; }
		}

		/// <summary>
		///  occures on map type changed
		/// </summary>
		public event MapTypeChanged OnMapTypeChanged
		{
			add { Core.OnMapTypeChanged += value; }
			remove { Core.OnMapTypeChanged -= value; }
		}

		/// <summary>
		///  occurs on empty tile displayed
		/// </summary>
		public event EmptyTileError OnEmptyTileError
		{
			add { Core.OnEmptyTileError += value; }
			remove { Core.OnEmptyTileError -= value; }
		}

		#endregion IGControl event Members

		#region IDisposable Members

		public virtual void Dispose()
		{
			if (Core.IsStarted)
			{
				Core.OnMapZoomChanged -= new MapZoomChanged(ForceUpdateOverlays);
				Loaded -= new RoutedEventHandler(GMapControl_Loaded);
				Dispatcher.ShutdownStarted -= new EventHandler(Dispatcher_ShutdownStarted);
				SizeChanged -= new SizeChangedEventHandler(GMapControl_SizeChanged);
				if (loadedApp != null)
				{
					loadedApp.SessionEnding -= new SessionEndingCancelEventHandler(Current_SessionEnding);
				}
				Core.OnMapClose();
			}
		}

		#endregion IDisposable Members
	}

	public enum HelperLineOptions
	{
		DontShow = 0,
		ShowAlways = 1,
		ShowOnModifierKey = 2
	}

	public enum ScaleModes
	{
		/// <summary>
		///  no scaling
		/// </summary>
		Integer,

		/// <summary>
		///  scales to fractional level using a stretched tiles, any issues -&gt; http://greatmaps.codeplex.com/workitem/16046
		/// </summary>
		ScaleUp,

		/// <summary>
		///  scales to fractional level using a narrowed tiles, any issues -&gt; http://greatmaps.codeplex.com/workitem/16046
		/// </summary>
		ScaleDown,

		/// <summary>
		///  scales to fractional level using a combination both stretched and narrowed tiles, any
		///  issues -&gt; http://greatmaps.codeplex.com/workitem/16046
		/// </summary>
		Dynamic
	}

	/// <summary>
	///  Enum ZoomMode
	/// </summary>
	public enum ZoomMode
	{
		/// <summary>
		///  Only update X coordinates
		/// </summary>
		X,

		/// <summary>
		///  Only update Y coordinates
		/// </summary>
		Y,

		/// <summary>
		///  Updates both the X and Y coordinates
		/// </summary>
		XY
	}

	public delegate void SelectionChange(RectLatLng Selection, bool ZoomToFit);
}
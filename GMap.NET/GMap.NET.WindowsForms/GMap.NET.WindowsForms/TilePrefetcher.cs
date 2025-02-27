﻿namespace GMap.NET
{
	using GMap.NET.Internals;
	using GMap.NET.MapProviders;
	using GMap.NET.WindowsForms;
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Drawing;
	using System.Threading;
	using System.Windows.Forms;

	/// <summary>
	///  form helping to prefetch tiles on local db
	/// </summary>
	public partial class TilePrefetcher : Form
	{
		private BackgroundWorker worker = new BackgroundWorker();
		private List<GPoint> list;
		private int zoom;
		private GMapProvider provider;
		private int sleep;
		private int all;
		public bool ShowCompleteMessage = false;
		private RectLatLng area;
		private GMap.NET.GSize maxOfTiles;
		public GMapOverlay Overlay;
		private int retry;
		public bool Shuffle = true;

		public TilePrefetcher()
		{
			InitializeComponent();

			GMaps.Instance.OnTileCacheComplete += new TileCacheComplete(OnTileCacheComplete);
			GMaps.Instance.OnTileCacheStart += new TileCacheStart(OnTileCacheStart);
			GMaps.Instance.OnTileCacheProgress += new TileCacheProgress(OnTileCacheProgress);

			worker.WorkerReportsProgress = true;
			worker.WorkerSupportsCancellation = true;
			worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
			worker.DoWork += new DoWorkEventHandler(worker_DoWork);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
		}

		private readonly AutoResetEvent done = new AutoResetEvent(true);

		private void OnTileCacheComplete()
		{
			if (!IsDisposed)
			{
				done.Set();

				MethodInvoker m = delegate
				{
					label2.Text = "all tiles saved";
				};
				Invoke(m);
			}
		}

		private void OnTileCacheStart()
		{
			if (!IsDisposed)
			{
				done.Reset();

				MethodInvoker m = delegate
				{
					label2.Text = "saving tiles...";
				};
				Invoke(m);
			}
		}

		private void OnTileCacheProgress(int left)
		{
			if (!IsDisposed)
			{
				MethodInvoker m = delegate
				{
					label2.Text = left + " tile to save...";
				};
				Invoke(m);
			}
		}

		public void Start(RectLatLng area, int zoom, GMapProvider provider, int sleep, int retry)
		{
			if (!worker.IsBusy)
			{
				this.label1.Text = "...";
				this.progressBarDownload.Value = 0;

				this.area = area;
				this.zoom = zoom;
				this.provider = provider;
				this.sleep = sleep;
				this.retry = retry;

				GMaps.Instance.UseMemoryCache = false;
				GMaps.Instance.CacheOnIdleRead = false;
				GMaps.Instance.BoostCacheEngine = true;

				if (Overlay != null)
				{
					Overlay.Markers.Clear();
				}

				worker.RunWorkerAsync();

				this.ShowDialog();
			}
		}

		public void Stop()
		{
			GMaps.Instance.OnTileCacheComplete -= new TileCacheComplete(OnTileCacheComplete);
			GMaps.Instance.OnTileCacheStart -= new TileCacheStart(OnTileCacheStart);
			GMaps.Instance.OnTileCacheProgress -= new TileCacheProgress(OnTileCacheProgress);

			done.Set();

			if (worker.IsBusy)
			{
				worker.CancelAsync();
			}

			GMaps.Instance.CancelTileCaching();

			done.Close();
		}

		private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (ShowCompleteMessage)
			{
				if (!e.Cancelled)
				{
					MessageBox.Show(this, "Prefetch Complete! => " + ((int)e.Result).ToString() + " of " + all);
				}
				else
				{
					MessageBox.Show(this, "Prefetch Canceled! => " + ((int)e.Result).ToString() + " of " + all);
				}
			}

			list.Clear();

			GMaps.Instance.UseMemoryCache = true;
			GMaps.Instance.CacheOnIdleRead = true;
			GMaps.Instance.BoostCacheEngine = false;

			worker.Dispose();

			this.Close();
		}

		private bool CacheTiles(int zoom, GPoint p)
		{
			foreach (var pr in provider.Overlays)
			{
				Exception ex;
				PureImage img;

				// tile number inversion(BottomLeft -> TopLeft)
				if (pr.InvertedAxisY)
				{
					img = GMaps.Instance.GetImageFrom(pr, new GPoint(p.X, maxOfTiles.Height - p.Y), zoom, out ex);
				}
				else // ok
				{
					img = GMaps.Instance.GetImageFrom(pr, p, zoom, out ex);
				}

				if (img != null)
				{
					img.Dispose();
					img = null;
				}
				else
				{
					return false;
				}
			}
			return true;
		}

		public readonly Queue<GPoint> CachedTiles = new Queue<GPoint>();

		private void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			if (list != null)
			{
				list.Clear();
				list = null;
			}
			list = provider.Projection.GetAreaTileList(area, zoom, 0);
			maxOfTiles = provider.Projection.GetTileMatrixMaxXY(zoom);
			all = list.Count;

			int countOk = 0;
			int retryCount = 0;

			if (Shuffle)
			{
				Stuff.Shuffle<GPoint>(list);
			}

			lock (this)
			{
				CachedTiles.Clear();
			}

			for (int i = 0; i < all; i++)
			{
				if (worker.CancellationPending)
					break;

				GPoint p = list[i];
				{
					if (CacheTiles(zoom, p))
					{
						if (Overlay != null)
						{
							lock (this)
							{
								CachedTiles.Enqueue(p);
							}
						}
						countOk++;
						retryCount = 0;
					}
					else
					{
						if (++retryCount <= retry) // retry only one
						{
							i--;
							System.Threading.Thread.Sleep(1111);
							continue;
						}
						else
						{
							retryCount = 0;
						}
					}
				}

				worker.ReportProgress((int)((i + 1) * 100 / all), i + 1);

				if (sleep > 0)
				{
					System.Threading.Thread.Sleep(sleep);
				}
			}

			e.Result = countOk;

			if (!IsDisposed)
			{
				done.WaitOne();
			}
		}

		private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			this.label1.Text = "Fetching tile at zoom (" + zoom + "): " + ((int)e.UserState).ToString() + " of " + all + ", complete: " + e.ProgressPercentage.ToString() + "%";
			this.progressBarDownload.Value = e.ProgressPercentage;

			if (Overlay != null)
			{
				GPoint? l = null;

				lock (this)
				{
					if (CachedTiles.Count > 0)
					{
						l = CachedTiles.Dequeue();
					}
				}

				if (l.HasValue)
				{
					var px = Overlay.Control.MapProvider.Projection.FromTileXYToPixel(l.Value);
					var p = Overlay.Control.MapProvider.Projection.FromPixelToLatLng(px, zoom);

					var r1 = Overlay.Control.MapProvider.Projection.GetGroundResolution(zoom, p.Lat);
					var r2 = Overlay.Control.MapProvider.Projection.GetGroundResolution((int)Overlay.Control.Zoom, p.Lat);
					var sizeDiff = r2 / r1;

					GMapMarkerTile m = new GMapMarkerTile(p, (int)(Overlay.Control.MapProvider.Projection.TileSize.Width / sizeDiff));
					Overlay.Markers.Add(m);
				}
			}
		}

		private void Prefetch_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				this.Close();
			}
		}

		private void Prefetch_FormClosed(object sender, FormClosedEventArgs e)
		{
			this.Stop();
		}
	}

	internal class GMapMarkerTile : GMapMarker
	{
		private static Brush Fill = new SolidBrush(Color.FromArgb(155, Color.Blue));

		public GMapMarkerTile(PointLatLng p, int size) : base(p)
		{
			Size = new System.Drawing.Size(size, size);
		}

		public override void OnRender(Graphics g)
		{
			g.FillRectangle(Fill, new System.Drawing.Rectangle(LocalPosition.X, LocalPosition.Y, Size.Width, Size.Height));
		}
	}
}
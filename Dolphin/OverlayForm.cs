﻿using System;
using System.Text;
using System.Windows.Forms;
using SharpDX.Direct2D1;
using Factory = SharpDX.Direct2D1.Factory;
using FontFactory = SharpDX.DirectWrite.Factory;
using Format = SharpDX.DXGI.Format;
using SharpDX;
using System.Threading;
using System.Diagnostics;

using Dolphin.Classes;
using static Dolphin.Classes.GlobalVariables;
using static hazedumper.signatures;

namespace Dolphin
{
    public partial class OverlayForm : Form
    {
        public OverlayForm()
        {
            InitializeComponent();
            IsRunning = GetProcessAndHandles();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;
            this.Width = 64;// set your own size
            this.Height = 64;
            this.Location = new System.Drawing.Point(0, 0);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |// this reduce the flicker
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.Opaque |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            this.TopMost = true;
            this.Visible = true;

            GlobalVariables.Factory = new Factory();
            GlobalVariables.FontFactory = new FontFactory();

            while (!IsRunning)
            {
                IsRunning = GetProcessAndHandles();
                Thread.Sleep(1);
            }

            DeviceRenderProperties = new HwndRenderTargetProperties()
            {
                Hwnd = this.Handle,
                PixelSize = new Size2(1920, 1080),
                PresentOptions = PresentOptions.None
            };

            //Init DirectX
            GlobalVariables.Device = new WindowRenderTarget(GlobalVariables.Factory, new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied)), DeviceRenderProperties);

            LoopThread = new Thread(new ParameterizedThreadStart(SDXThread));

            LoopThread.Priority = ThreadPriority.Highest;
            LoopThread.IsBackground = true;
            LoopThread.Start();

            MainForm mf = new MainForm(this);
            mf.ShowDialog();
        }

        // draw the overlay
        protected override void OnPaint(PaintEventArgs e)
        {
            int[] marg = new int[] { 0, 0, Width, Height };
            DwmExtendFrameIntoClientArea(this.Handle, ref marg);
        }

        public void SDXThread(object sender)
        {
            float progress = 0.0f;

            while (IsRunning)
            {
                GameHandle = GameProcess.MainWindowHandle;

                if (GameProcess.HasExited)
                    IsRunning = false;

                // get the coords of the csgo window
                GetWindowRect(GameHandle, out WindowBounds);

                // set the location of the form overlay
                try
                {
                    if (WindowBounds.X != Location.X || WindowBounds.Width != Size.Width)
                    {
                        Location = new System.Drawing.Point(WindowBounds.X, WindowBounds.Y);
                        Size = new System.Drawing.Size(WindowBounds.Width, WindowBounds.Height);
                    }
                }
                catch { }
                
                // set the size of the form overlay
                WindowSize = new Size2(WindowBounds.Width, WindowBounds.Height);

                GlobalVariables.Device.BeginDraw();
                GlobalVariables.Device.Clear(Color.Transparent);
                GlobalVariables.Device.TextAntialiasMode = TextAntialiasMode.Aliased; // you can set another text mode
                
                //place your rendering things here
                if (GetActiveWindowTitle() == "Counter-Strike: Global Offensive" || GetActiveWindowTitle() == "Dolphin")
                {
                    Mem.StartProcess();

                    // Create Local Entity
                    LocalEntity LE = new LocalEntity();

                    // get current mapname
                    CurrentMapName = MapInfo.getCurrentMapName(Mem);

                    // check for map change to change map image for radar
                    if(CurrentMapName != LastMapName)
                    {
                        LastMapName = CurrentMapName;
                        MapImage = drawing2d.LoadFromFile(GlobalVariables.Device, MapInfo.getCurrentMapImage(CurrentMapName));
                    }
                    
                    // radar bounds
                    SharpDX.Mathematics.Interop.RawRectangleF radarBounds = new SharpDX.Mathematics.Interop.RawRectangleF()
                    {
                        Top = RadarTopLeftPosition.Y, Bottom = RadarTopLeftPosition.Y + RadarSize,
                        Left = RadarTopLeftPosition.X, Right = RadarTopLeftPosition.X + RadarSize
                    };

                    // draw radar bounds and map image
                    if (RadarEnabled)
                    {
                        GlobalVariables.Device.DrawBitmap(MapImage, radarBounds, RadarOpacity, BitmapInterpolationMode.Linear);
                        GlobalVariables.Device.DrawRectangle(radarBounds, drawing2d.getBrush(Color.White, GlobalVariables.Device));
                    }

                    // update viewmatrix
                    ViewMatrix = Matrix4x4.ReadMatrix(Mem, dwClient + dwViewMatrix);

                    // perform this loop for every entity
                    for (int i = 0; i < 64; i++)
                    {
                        Entity Entity = new Entity(i);

                        if(GlowEnabledOpposition || GlowEnabledFriendly)
                        {
                            Glow.DoGlow(Mem, Entity, LE, progress);
                            progress += 0.00001f;
                        }

                        // translate entity 3d coords into 2d radar coords
                        Vector3 normalisedRadarPos = drawing2d.normaliseCoords((int)radarBounds.Left, (int)radarBounds.Right, (int)radarBounds.Top, (int)radarBounds.Bottom, MapInfo.getCurrentMapInfo(CurrentMapName), Entity.Entity_Position_3D);
                        
                        // draw radar blips
                        if (Entity.Entity_isAlive() && RadarEnabled)
                        {
                            if (Entity.Entity_Team != LE.LocalEntity_Team)
                                drawing2d.DrawRadarBlip(new Vector2(normalisedRadarPos.X, normalisedRadarPos.Y), RadarBlipSize, Color.Red, GlobalVariables.Device);
                            else if (Entity.Entity_Base == LE.LocalEntity_Base)
                            {
                                drawing2d.DrawRadarBlip(new Vector2(normalisedRadarPos.X, normalisedRadarPos.Y), RadarBlipSize, Color.Yellow, GlobalVariables.Device);
                                GlobalVariables.Device.DrawEllipse(new Ellipse(new Vector2(normalisedRadarPos.X, normalisedRadarPos.Y), 50, 50), drawing2d.getBrush(Color.White, GlobalVariables.Device));
                            }
                            else
                                drawing2d.DrawRadarBlip(new Vector2(normalisedRadarPos.X, normalisedRadarPos.Y), RadarBlipSize, Color.LimeGreen, GlobalVariables.Device);

                            if (Entity.Entity_Team != LE.LocalEntity_Team)
                            {
                                Vector2 posx = new Vector2(normalisedRadarPos.X, normalisedRadarPos.Y);
                                Vector2 posy = new Vector2(radarBounds.Right, radarBounds.Top + (i * 20));
                                drawing2d.DrawLine(posx, posy, GlobalVariables.Device, Color.Red);
                                drawing2d.DrawShadowText((int)posy.X, (int)posy.Y - 5, 20, Color.White, "HP: " + Entity.Entity_Health, GlobalVariables.Device, GlobalVariables.FontFactory, WindowBounds);
                            }
                        }
                    }
                    Thread.Sleep(1);
                }

                GlobalVariables.Device.EndDraw();
            }
        }
    }
}
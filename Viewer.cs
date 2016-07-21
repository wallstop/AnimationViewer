using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace AnimationViewer
{
    public class Viewer : Form
    {
        private static readonly float SCALE_SCALAR = 0.01f;

        private readonly AnimationDescriptor animationDescriptor_;

        private NumericUpDown fps_;
        private TrackBar scale_;
        private NumericTextBox frameWidth_;

        private readonly DoubleBufferedPanel animationArea_;
        private readonly Thread fpsTicker_;

        public new float Scale => scale_.Value * SCALE_SCALAR;

        public Viewer()
        {
            animationDescriptor_ = new AnimationDescriptor();
            animationArea_ = new DoubleBufferedPanel();
            animationArea_.Paint += PaintAnimationArea;

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Setup();

            fpsTicker_ = new Thread(Tick);
            fpsTicker_.Start();
        }

        protected override void Dispose(bool disposing)
        {
            fpsTicker_.Abort();
            base.Dispose(disposing);
        }

        private void Tick()
        {
            Stopwatch timer = Stopwatch.StartNew();
            while(true)
            {
                TimeSpan startTime = timer.Elapsed;
                int targetFps;
                lock(fps_)
                {
                    targetFps = (int) fps_.Value;
                }
                TimeSpan targetFramerate = TimeSpan.FromSeconds(1.0 / targetFps);
                TimeSpan endTime;
                do
                {
                    endTime = timer.Elapsed;
                } while(endTime < startTime + targetFramerate);
                lock(animationDescriptor_)
                {
                    animationDescriptor_.Tick();
                }
                Redraw();
            }
        }

        private void Setup()
        {
            /* Pause while we set stuff up */
            SuspendLayout();
            SetupMenu();
            fps_ = new NumericUpDown {Maximum = 60, Minimum = 1, Location = new Point(20, 24), Size = new Size(40, 10), Value = 6};
            Controls.Add(fps_);
            frameWidth_ = new NumericTextBox {Location = new Point(60, 24), Size = new Size(40, 10), Text=@"0"};
            Controls.Add(frameWidth_);
            scale_ = new TrackBar {Maximum = 2500, Minimum = 100, Location = new Point(100, 24), Value = 100, Size = new Size(200, 20), TickStyle = TickStyle.None};
            Controls.Add(scale_);
            animationArea_.Location = new Point(20, 70);
            Controls.Add(animationArea_);

            ResumeLayout(false);
            PerformLayout();

            //DoubleBuffered = true;
        }

        private void SetupMenu()
        {
            Text = @"AnimationViewer";
            MenuStrip mainMenu = new MenuStrip {ShowItemToolTips = true};
            ToolStripMenuItem load = new ToolStripMenuItem();

            mainMenu.Items.Add(load);

            mainMenu.Location = new Point(20, 0);
            mainMenu.Size = new Size(160, 14);
            
            load.Text = @"Load Animation";
            load.ToolTipText = @"pls click";
            load.Click += HandleLoad;

            Controls.Add(mainMenu);
            MainMenuStrip = mainMenu;
            mainMenu.ResumeLayout(false);
            mainMenu.PerformLayout();
        }

        private void PaintAnimationArea(object sender, PaintEventArgs paintEvents)
        {
            lock (animationDescriptor_)
            {
                if(ReferenceEquals(animationDescriptor_.Image, null))
                {
                    return;
                }
                paintEvents.Graphics.SmoothingMode = SmoothingMode.None;
                paintEvents.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                
                paintEvents.Graphics.CompositingQuality = CompositingQuality.AssumeLinear;
                
                paintEvents.Graphics.Clear(Color.Gray);
                paintEvents.Graphics.DrawImage(animationDescriptor_.Image, new Rectangle(0, 0, (int) (Scale*  animationDescriptor_.FrameWidth), (int) (Scale * animationDescriptor_.FrameHeight)), animationDescriptor_.View, GraphicsUnit.Pixel);
            }
        }

        private void HandleLoad(object sender, EventArgs args)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = @"Animation Files(*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp;|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true,
                CheckPathExists = true
            };

            DialogResult dialogResult = openFileDialog.ShowDialog();
            switch(dialogResult)
            {
                case DialogResult.OK:
                {
                    string imageName = openFileDialog.FileName;
                    lock(animationDescriptor_)
                    {
                        animationDescriptor_.Load(imageName);
                        frameWidth_.Text = animationDescriptor_.FrameWidth.ToString();
                    }
                }
                    break;
            }
        }

        private void Redraw()
        {
            lock(animationDescriptor_)
            {
                int oldHeight = animationArea_.Height;
                int oldWidth = animationArea_.Width;
                animationDescriptor_.FrameWidth = frameWidth_.IntValue;
                animationArea_.Height = Math.Max((int)(Scale * animationDescriptor_.FrameHeight), 10);
                animationArea_.Width = Math.Max((int)(Scale * animationDescriptor_.FrameWidth), 10);
                if(oldHeight != animationArea_.Height || oldWidth != animationArea_.Width)
                {
                    Invalidate();
                    Update();
                }
            }
            animationArea_.Invalidate();
            animationArea_.Update();
        }
    }
}

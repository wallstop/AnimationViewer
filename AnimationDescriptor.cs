using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnimationViewer
{
    public class AnimationDescriptor
    {
        public string Path { get; private set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; private set; }
        public Image Image { get; private set; }
        public int CurrentFrame { get; private set; }
        private int Frames => (Image?.Width ?? 0) / Math.Max(1, FrameWidth);

        public Rectangle View => new Rectangle(CurrentFrame * FrameWidth, 0, FrameWidth, FrameHeight);

        public void Tick()
        {
            if(Frames != 0)
            {
                CurrentFrame = (CurrentFrame + 1) % Frames;
            }
        }

        public void Load(string path)
        {
            Image?.Dispose();
            Image = Image.FromFile(path);
            Path = path;
            FrameHeight = Image.Height;
            /* Super rough, assume square-ish */
            FrameWidth = FrameHeight;
            CurrentFrame = 0;
        }
    }
}

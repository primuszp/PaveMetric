using System;

namespace PaveMetric
{
    [Serializable]
    public class SurfaceError
    {
        public ErrorCodes ErrorCode { get; set; }
        public double StartSection { get; set; }
        public double EndSection { get; set; }
        public double Left { get; set; }
        public double Right { get; set; }

        public SurfaceError Clone()
        {
            return new SurfaceError
            {
                ErrorCode = ErrorCode,
                StartSection = StartSection,
                EndSection = EndSection,
                Left = Left,
                Right = Right
            };
        }

        public void CopyBoundsFrom(SurfaceError source)
        {
            StartSection = source.StartSection;
            EndSection = source.EndSection;
            Left = source.Left;
            Right = source.Right;
        }
    }
}

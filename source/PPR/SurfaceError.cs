using System;

namespace PPR
{
    [Serializable]
    public class SurfaceError
    {
        public ErrorCodes ErrorCode { get; set; }
        public double StartSection { get; set; }
        public double EndSection { get; set; }
        public double Left { get; set; }
        public double Right { get; set; }
    }
}
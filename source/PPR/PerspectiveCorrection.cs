using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml.Serialization;

namespace PPR
{
    [Serializable]
    public class PerspectiveCorrection
    {
        public double PavementWidth { get; set; }
        public double Length { get; set; }
        public double FarDistance { get; set; }
        public double NearDistance { get; set; }
        public double Alpha { get; set; }
        public double Beta { get; set; }
        public Line LeftEdge { get; set; }
        public Line RightEdge { get; set; }
        public bool Normalized { get; set; }
        public int RowCount { get; set; }
        public int ColCount { get; set; }
        [XmlIgnore] public double TopViewNearRealY { get; private set; }
        [XmlIgnore] public double TopViewFarRealY { get; private set; }

        public PerspectiveCorrection()
        {
            LeftEdge = new Line();
            RightEdge = new Line();
        }

        public Line[] GetNet()
        {
            return GetNet(RowCount, ColCount);
        }

        public Line[] GetNet(int nRows, int nCols)
        {
            List<Line> lines = new List<Line>();

            if (!HasValidGeometry() || nRows < 1 || nCols < 1)
                return lines.ToArray();

            double netDeltaX = PavementWidth / (double)nCols;

            for (int i = 1; i < nCols; i++)
            {
                double x = i * netDeltaX;

                Line newLine = new Line();
                newLine.LineWidth = 1.5;
                newLine.LineColor = Color.FromArgb(128, 255, 255, 255);
                newLine.P0 = GetScreenPosition(new Pos(x, 0.0));
                newLine.P1 = GetScreenPosition(new Pos(x, Length));

                lines.Add(newLine);
            }

            double netDeltaY = Length / (double)nRows;

            for (int i = 1; i < nRows; i++)
            {
                double y = i * netDeltaY;

                Line newLine = new Line();
                newLine.LineWidth = 1.5;
                newLine.LineColor = Color.FromArgb(128, 255, 255, 255);
                newLine.P0 = GetScreenPosition(new Pos(0.0, y));
                newLine.P1 = GetScreenPosition(new Pos(PavementWidth, y));

                lines.Add(newLine);
            }

            return lines.ToArray();
        }

        public void Normalize(Line[] lines, double length, double width)
        {
            Normalized = false;
            if (lines == null || lines.Length < 4 || length <= 0.0 || width <= 0.0)
                return;

            Line farLine = lines[0];
            Line nearLine = lines[1];
            Line leftLine = lines[2];
            Line rightLine = lines[3];

            double nearDistance = nearLine.P0.Y;
            double farDistance = farLine.P0.Y;
            if (Math.Abs(nearDistance - farDistance) < 1e-9
                || Math.Abs(leftLine.DeltaY) < 1e-9
                || Math.Abs(rightLine.DeltaY) < 1e-9)
                return;

            Length = length;
            PavementWidth = width;
            NearDistance = nearDistance;
            FarDistance = farDistance;

            if (leftLine.P0.X > leftLine.P1.X)
            {
                Pos temp = new Pos(leftLine.P1.X, leftLine.P1.Y);
                leftLine.P1 = leftLine.P0;
                leftLine.P0 = temp;
            }
            if (rightLine.P0.X < rightLine.P1.X)
            {
                Pos temp = new Pos(rightLine.P1.X, rightLine.P1.Y);
                rightLine.P1 = rightLine.P0;
                rightLine.P0 = temp;
            }

            double dx = leftLine.P1.X - leftLine.P0.X;
            double dy = leftLine.P0.Y - leftLine.P1.Y;

            double dyFar = leftLine.P1.Y - farDistance;
            double dxFar = dyFar * dx / dy;

            double xFarLeft = leftLine.P1.X + dxFar;
            leftLine.P1.X = xFarLeft;
            leftLine.P1.Y = farDistance;
            farLine.P0.X = xFarLeft;

            double dyNear = leftLine.P1.Y - nearDistance;
            double dxNear = dyNear * dx / dy;

            double xNearLeft = leftLine.P1.X + dxNear;
            leftLine.P0.X = xNearLeft;
            leftLine.P0.Y = nearDistance;
            nearLine.P0.X = xNearLeft;

            dx = rightLine.P1.X - rightLine.P0.X;
            dy = rightLine.P0.Y - rightLine.P1.Y;

            dyFar = rightLine.P1.Y - farDistance;
            dxFar = dyFar * dx / dy;

            double xFarRight = rightLine.P1.X + dxFar;
            rightLine.P1.X = xFarRight;
            rightLine.P1.Y = farDistance;
            farLine.P1.X = xFarRight;

            dyNear = rightLine.P1.Y - nearDistance;
            dxNear = dyNear * dx / dy;

            double xNearRight = rightLine.P1.X + dxNear;
            rightLine.P0.X = xNearRight;
            rightLine.P0.Y = nearDistance;
            nearLine.P1.X = xNearRight;

            LeftEdge.P0.X = leftLine.P0.X;
            LeftEdge.P0.Y = nearDistance;
            LeftEdge.P1.X = leftLine.P1.X;
            LeftEdge.P1.Y = farDistance;

            RightEdge.P0.X = rightLine.P0.X;
            RightEdge.P0.Y = nearDistance;
            RightEdge.P1.X = rightLine.P1.X;
            RightEdge.P1.Y = farDistance;

            Normalized = true;
        }

        public Pos GetRealPosition(Pos ScreenPosition)
        {
            if (!HasValidGeometry())
                return new Pos();

            double t = (NearDistance - ScreenPosition.Y) / (NearDistance - FarDistance);
            double leftX = Lerp(LeftEdge.P0.X, LeftEdge.P1.X, t);
            double rightX = Lerp(RightEdge.P0.X, RightEdge.P1.X, t);
            double screenWidth = rightX - leftX;
            double farWidth = RightEdge.P1.X - LeftEdge.P1.X;

            if (Math.Abs(screenWidth) < 1e-9)
                return new Pos();

            return new Pos(
                PavementWidth * (ScreenPosition.X - leftX) / screenWidth,
                farWidth * Length * t / screenWidth);
        }

        public Pos GetScreenPosition(Pos RealPosition)
        {
            if (!HasValidGeometry())
                return new Pos();

            double nearWidth = RightEdge.P0.X - LeftEdge.P0.X;
            double farWidth = RightEdge.P1.X - LeftEdge.P1.X;
            double denominator = farWidth * Length - RealPosition.Y * (farWidth - nearWidth);

            if (Math.Abs(denominator) < 1e-9)
                return new Pos();

            double t = RealPosition.Y * nearWidth / denominator;
            double leftX = Lerp(LeftEdge.P0.X, LeftEdge.P1.X, t);
            double rightX = Lerp(RightEdge.P0.X, RightEdge.P1.X, t);

            return new Pos(
                Lerp(leftX, rightX, RealPosition.X / PavementWidth),
                Lerp(NearDistance, FarDistance, t));
        }

        private bool HasValidGeometry()
        {
            return PavementWidth > 0.0
                && Length > 0.0
                && Math.Abs(NearDistance - FarDistance) > 1e-9
                && Math.Abs(RightEdge.P0.X - LeftEdge.P0.X) > 1e-9
                && Math.Abs(RightEdge.P1.X - LeftEdge.P1.X) > 1e-9;
        }

        private static double Lerp(double start, double end, double amount)
        {
            return start + (end - start) * amount;
        }

        public Bitmap CreateTopView(Bitmap source, int pixelsPerMeter = 100)
        {
            if (!TryGetOrderedScreenCorners(source, out Pos[] screenCorners))
                return null;

            int width = Math.Max(1, (int)Math.Round(PavementWidth * pixelsPerMeter));
            double visibleLength = TopViewFarRealY - TopViewNearRealY;
            int height = Math.Max(1, (int)Math.Round(visibleLength * pixelsPerMeter));
            double[] topViewToScreen = CreateUnitSquareToScreenHomography(screenCorners);
            if (topViewToScreen == null)
                return null;

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using Bitmap source32 = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(source32))
                graphics.DrawImage(source, new Rectangle(0, 0, source32.Width, source32.Height));

            Rectangle sourceRect = new Rectangle(0, 0, source32.Width, source32.Height);
            Rectangle resultRect = new Rectangle(0, 0, width, height);
            BitmapData sourceData = source32.LockBits(sourceRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData resultData = result.LockBits(resultRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* sourceBase = (byte*)sourceData.Scan0;
                    byte* resultBase = (byte*)resultData.Scan0;

                    for (int y = 0; y < height; y++)
                    {
                        double v = (double)y / Math.Max(1, height - 1);
                        byte* resultRow = resultBase + y * resultData.Stride;

                        for (int x = 0; x < width; x++)
                        {
                            double u = (double)x / Math.Max(1, width - 1);
                            Pos screen = ApplyHomography(topViewToScreen, u, v);
                            SampleBilinear(sourceBase, sourceData.Stride, source32.Width, source32.Height, screen.X, screen.Y, resultRow + x * 4);
                        }
                    }
                }
            }
            finally
            {
                source32.UnlockBits(sourceData);
                result.UnlockBits(resultData);
            }

            return result;
        }

        private bool TryGetOrderedScreenCorners(Bitmap source, out Pos[] corners)
        {
            corners = null;
            if (!HasValidGeometry() || source == null)
                return false;

            double visibleFarY = Math.Max(0.0, Math.Min(FarDistance, NearDistance));
            double visibleNearY = Math.Min(source.Height - 1.0, Math.Max(FarDistance, NearDistance));
            if (visibleNearY <= visibleFarY)
                return false;

            Pos farLeft = GetLinePositionAtY(LeftEdge, visibleFarY);
            Pos farRight = GetLinePositionAtY(RightEdge, visibleFarY);
            Pos nearLeft = GetLinePositionAtY(LeftEdge, visibleNearY);
            Pos nearRight = GetLinePositionAtY(RightEdge, visibleNearY);

            SortLeftRight(ref nearLeft, ref nearRight);
            SortLeftRight(ref farLeft, ref farRight);

            Pos farCenter = new Pos((farLeft.X + farRight.X) / 2.0, visibleFarY);
            Pos nearCenter = new Pos((nearLeft.X + nearRight.X) / 2.0, visibleNearY);
            TopViewFarRealY = GetRealPosition(farCenter).Y;
            TopViewNearRealY = GetRealPosition(nearCenter).Y;
            if (TopViewFarRealY < TopViewNearRealY)
            {
                double temporary = TopViewFarRealY;
                TopViewFarRealY = TopViewNearRealY;
                TopViewNearRealY = temporary;
            }

            corners = new Pos[]
            {
                farLeft,
                farRight,
                nearRight,
                nearLeft
            };

            foreach (Pos corner in corners)
            {
                if (!double.IsFinite(corner.X) || !double.IsFinite(corner.Y))
                    return false;
            }

            double area = 0.0;
            for (int i = 0; i < corners.Length; i++)
            {
                Pos current = corners[i];
                Pos next = corners[(i + 1) % corners.Length];
                area += current.X * next.Y - next.X * current.Y;
            }

            return Math.Abs(area) >= source.Width * source.Height * 0.005;
        }

        private static double[] CreateUnitSquareToScreenHomography(Pos[] destination)
        {
            Pos p0 = destination[0];
            Pos p1 = destination[1];
            Pos p2 = destination[2];
            Pos p3 = destination[3];

            double dx1 = p1.X - p2.X;
            double dx2 = p3.X - p2.X;
            double dx3 = p0.X - p1.X + p2.X - p3.X;
            double dy1 = p1.Y - p2.Y;
            double dy2 = p3.Y - p2.Y;
            double dy3 = p0.Y - p1.Y + p2.Y - p3.Y;
            double denominator = dx1 * dy2 - dx2 * dy1;
            if (Math.Abs(denominator) < 1e-12)
                return null;

            double g = (dx3 * dy2 - dx2 * dy3) / denominator;
            double h = (dx1 * dy3 - dx3 * dy1) / denominator;

            return new double[]
            {
                p1.X - p0.X + g * p1.X,
                p3.X - p0.X + h * p3.X,
                p0.X,
                p1.Y - p0.Y + g * p1.Y,
                p3.Y - p0.Y + h * p3.Y,
                p0.Y,
                g,
                h
            };
        }

        private static Pos GetLinePositionAtY(Line line, double y)
        {
            double deltaY = line.P1.Y - line.P0.Y;
            if (Math.Abs(deltaY) < 1e-9)
                return new Pos(line.P0.X, y);

            double amount = (y - line.P0.Y) / deltaY;
            return new Pos(Lerp(line.P0.X, line.P1.X, amount), y);
        }

        private static double GetXCrossY(Line line, double targetX)
        {
            double deltaX = line.P1.X - line.P0.X;
            if (Math.Abs(deltaX) < 1e-9)
                return line.P0.Y;
            double amount = (targetX - line.P0.X) / deltaX;
            return line.P0.Y + (line.P1.Y - line.P0.Y) * amount;
        }

        private static void SortLeftRight(ref Pos first, ref Pos second)
        {
            if (first.X <= second.X)
                return;

            Pos temporary = first;
            first = second;
            second = temporary;
        }

        private static Pos ApplyHomography(double[] h, double u, double v)
        {
            double denominator = h[6] * u + h[7] * v + 1.0;
            return new Pos(
                (h[0] * u + h[1] * v + h[2]) / denominator,
                (h[3] * u + h[4] * v + h[5]) / denominator);
        }

        private static unsafe void SampleBilinear(
            byte* source,
            int stride,
            int width,
            int height,
            double x,
            double y,
            byte* target)
        {
            if (x < 0.0 || y < 0.0 || x >= width || y >= height)
            {
                target[0] = 0;
                target[1] = 0;
                target[2] = 0;
                target[3] = 255;
                return;
            }

            int ix = Math.Min((int)x, width - 1);
            int iy = Math.Min((int)y, height - 1);
            double fx = x - ix;
            double fy = y - iy;
            int x0 = Math.Min(ix, Math.Max(0, width - 2));
            int y0 = Math.Min(iy, Math.Max(0, height - 2));
            int x1 = Math.Min(x0 + 1, width - 1);
            int y1 = Math.Min(y0 + 1, height - 1);
            byte* p00 = source + y0 * stride + x0 * 4;
            byte* p10 = source + y0 * stride + x1 * 4;
            byte* p01 = source + y1 * stride + x0 * 4;
            byte* p11 = source + y1 * stride + x1 * 4;

            if (ix == width - 1)
                fx = 1.0;
            if (iy == height - 1)
                fy = 1.0;

            for (int channel = 0; channel < 4; channel++)
            {
                double top = p00[channel] + (p10[channel] - p00[channel]) * fx;
                double bottom = p01[channel] + (p11[channel] - p01[channel]) * fx;
                target[channel] = (byte)(top + (bottom - top) * fy);
            }
        }
    }
}

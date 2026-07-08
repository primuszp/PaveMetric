using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
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
        public List<Pos> LeftEdgePoints { get; set; }
        public List<Pos> RightEdgePoints { get; set; }
        public bool Normalized { get; set; }
        public int RowCount { get; set; }
        public int ColCount { get; set; }
        [XmlIgnore] public double TopViewNearRealY { get; private set; }
        [XmlIgnore] public double TopViewFarRealY { get; private set; }

        public PerspectiveCorrection()
        {
            LeftEdge = new Line();
            RightEdge = new Line();
            LeftEdgePoints = new List<Pos>();
            RightEdgePoints = new List<Pos>();
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

                AddScreenPolyline(lines, x, 0.0, x, Length, Color.FromArgb(128, 255, 255, 255), 1.5);
            }

            double netDeltaY = Length / (double)nRows;

            for (int i = 1; i < nRows; i++)
            {
                double y = i * netDeltaY;

                AddScreenPolyline(lines, 0.0, y, PavementWidth, y, Color.FromArgb(128, 255, 255, 255), 1.5);
            }

            return lines.ToArray();
        }

        public Pos[] GetScreenAreaPolygon(double leftReal, double rightReal, double nearReal, double farReal, int segmentCount = 24)
        {
            int segments = HasCurvedGeometry() ? Math.Max(2, segmentCount) : 1;
            List<Pos> points = new List<Pos>((segments + 1) * 2);

            for (int i = 0; i <= segments; i++)
            {
                double amount = (double)i / segments;
                double y = Lerp(nearReal, farReal, amount);
                points.Add(GetScreenPosition(new Pos(leftReal, y)));
            }

            for (int i = segments; i >= 0; i--)
            {
                double amount = (double)i / segments;
                double y = Lerp(nearReal, farReal, amount);
                points.Add(GetScreenPosition(new Pos(rightReal, y)));
            }

            return points.ToArray();
        }

        public Line[] GetScreenPolyline(double x0, double y0, double x1, double y1, Color color, double lineWidth, int segmentCount = 24)
        {
            List<Line> lines = new List<Line>();
            AddScreenPolyline(lines, x0, y0, x1, y1, color, lineWidth, segmentCount);
            return lines.ToArray();
        }

        private void AddScreenPolyline(List<Line> lines, double x0, double y0, double x1, double y1, Color color, double lineWidth, int segmentCount = 24)
        {
            int segments = HasCurvedGeometry() ? Math.Max(2, segmentCount) : 1;
            Pos previous = GetScreenPosition(new Pos(x0, y0));
            for (int i = 1; i <= segments; i++)
            {
                double amount = (double)i / segments;
                Pos current = GetScreenPosition(new Pos(Lerp(x0, x1, amount), Lerp(y0, y1, amount)));
                lines.Add(new Line
                {
                    LineColor = color,
                    LineWidth = lineWidth,
                    P0 = previous,
                    P1 = current
                });
                previous = current;
            }
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

            LeftEdgePoints.Clear();
            RightEdgePoints.Clear();
            Normalized = true;
        }

        public void NormalizeCurved(List<Pos> leftEdgePoints, List<Pos> rightEdgePoints, double length, double width)
        {
            NormalizeCurved(leftEdgePoints, rightEdgePoints, null, null, length, width);
        }

        public void NormalizeCurved(List<Pos> leftEdgePoints, List<Pos> rightEdgePoints, Line nearLine, Line farLine, double length, double width)
        {
            Normalized = false;
            if (!HasValidEdgePoints(leftEdgePoints) || !HasValidEdgePoints(rightEdgePoints) || length <= 0.0 || width <= 0.0)
                return;

            Length = length;
            PavementWidth = width;
            LeftEdgePoints = nearLine == null || farLine == null
                ? ClonePoints(leftEdgePoints)
                : TrimEdgeByBoundaries(GetSmoothEdgePoints(leftEdgePoints), nearLine, farLine);
            RightEdgePoints = nearLine == null || farLine == null
                ? ClonePoints(rightEdgePoints)
                : TrimEdgeByBoundaries(GetSmoothEdgePoints(rightEdgePoints), nearLine, farLine);
            if (!HasValidEdgePoints(LeftEdgePoints) || !HasValidEdgePoints(RightEdgePoints))
                return;

            LeftEdge.P0 = ClonePoint(LeftEdgePoints[0]);
            LeftEdge.P1 = ClonePoint(LeftEdgePoints[LeftEdgePoints.Count - 1]);
            RightEdge.P0 = ClonePoint(RightEdgePoints[0]);
            RightEdge.P1 = ClonePoint(RightEdgePoints[RightEdgePoints.Count - 1]);
            NearDistance = (LeftEdge.P0.Y + RightEdge.P0.Y) / 2.0;
            FarDistance = (LeftEdge.P1.Y + RightEdge.P1.Y) / 2.0;

            Normalized = HasValidGeometry();
        }

        private static List<Pos> ClonePoints(List<Pos> points)
        {
            List<Pos> result = new List<Pos>(points.Count);
            foreach (Pos point in points)
                result.Add(ClonePoint(point));
            return result;
        }

        private static List<Pos> GetSmoothEdgePoints(List<Pos> points)
        {
            if (points.Count < 3)
                return ClonePoints(points);

            const int samplesPerSegment = 12;
            List<Pos> result = new List<Pos>((points.Count - 1) * samplesPerSegment + 1);
            result.Add(ClonePoint(points[0]));

            for (int i = 0; i < points.Count - 1; i++)
            {
                Pos p0 = points[Math.Max(0, i - 1)];
                Pos p1 = points[i];
                Pos p2 = points[i + 1];
                Pos p3 = points[Math.Min(points.Count - 1, i + 2)];

                for (int j = 1; j <= samplesPerSegment; j++)
                {
                    double t = (double)j / samplesPerSegment;
                    result.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }

            return result;
        }

        private static Pos CatmullRom(Pos p0, Pos p1, Pos p2, Pos p3, double t)
        {
            double t2 = t * t;
            double t3 = t2 * t;
            return new Pos(
                0.5 * ((2.0 * p1.X)
                    + (-p0.X + p2.X) * t
                    + (2.0 * p0.X - 5.0 * p1.X + 4.0 * p2.X - p3.X) * t2
                    + (-p0.X + 3.0 * p1.X - 3.0 * p2.X + p3.X) * t3),
                0.5 * ((2.0 * p1.Y)
                    + (-p0.Y + p2.Y) * t
                    + (2.0 * p0.Y - 5.0 * p1.Y + 4.0 * p2.Y - p3.Y) * t2
                    + (-p0.Y + 3.0 * p1.Y - 3.0 * p2.Y + p3.Y) * t3));
        }

        private static Pos ClonePoint(Pos point)
        {
            return new Pos(point.X, point.Y);
        }

        private static List<Pos> TrimEdgeByBoundaries(List<Pos> edgePoints, Line nearLine, Line farLine)
        {
            if (nearLine == null || farLine == null)
                return ClonePoints(edgePoints);

            double nearDistance = FindBoundaryDistance(edgePoints, nearLine);
            double farDistance = FindBoundaryDistance(edgePoints, farLine);
            if (!double.IsFinite(nearDistance) || !double.IsFinite(farDistance) || Math.Abs(nearDistance - farDistance) < 1e-9)
                return ClonePoints(edgePoints);

            if (nearDistance > farDistance)
            {
                double temporary = nearDistance;
                nearDistance = farDistance;
                farDistance = temporary;
            }

            return SlicePolyline(edgePoints, nearDistance, farDistance);
        }

        private static double FindBoundaryDistance(List<Pos> points, Line boundary)
        {
            double traversed = 0.0;
            double bestDistance = 0.0;
            double bestLineDistance = double.MaxValue;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Pos a = points[i];
                Pos b = points[i + 1];
                if (TryIntersectSegments(a, b, boundary.P0, boundary.P1, out double amount))
                    return traversed + SegmentLength(a, b) * amount;

                double closestAmount = ClosestPointAmountToLine(a, b, boundary);
                Pos closest = new Pos(Lerp(a.X, b.X, closestAmount), Lerp(a.Y, b.Y, closestAmount));
                double lineDistance = PointLineDistanceSquared(closest, boundary);
                if (lineDistance < bestLineDistance)
                {
                    bestLineDistance = lineDistance;
                    bestDistance = traversed + SegmentLength(a, b) * closestAmount;
                }

                traversed += SegmentLength(a, b);
            }

            return bestDistance;
        }

        private static List<Pos> SlicePolyline(List<Pos> points, double startDistance, double endDistance)
        {
            List<Pos> result = new List<Pos>();
            result.Add(GetPolylinePointAtDistance(points, startDistance));

            double traversed = 0.0;
            for (int i = 1; i < points.Count - 1; i++)
            {
                traversed += SegmentLength(points[i - 1], points[i]);
                if (traversed > startDistance && traversed < endDistance)
                    result.Add(ClonePoint(points[i]));
            }

            result.Add(GetPolylinePointAtDistance(points, endDistance));
            return result;
        }

        private static Pos GetPolylinePointAtDistance(List<Pos> points, double targetDistance)
        {
            targetDistance = Math.Clamp(targetDistance, 0.0, GetPolylineLength(points));
            double traversed = 0.0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Pos a = points[i];
                Pos b = points[i + 1];
                double length = SegmentLength(a, b);
                if (traversed + length >= targetDistance)
                {
                    double amount = length <= 1e-9 ? 0.0 : (targetDistance - traversed) / length;
                    return new Pos(Lerp(a.X, b.X, amount), Lerp(a.Y, b.Y, amount));
                }

                traversed += length;
            }

            return ClonePoint(points[points.Count - 1]);
        }

        private static bool TryIntersectSegments(Pos a, Pos b, Pos c, Pos d, out double amount)
        {
            amount = 0.0;
            double rx = b.X - a.X;
            double ry = b.Y - a.Y;
            double sx = d.X - c.X;
            double sy = d.Y - c.Y;
            double denominator = rx * sy - ry * sx;
            if (Math.Abs(denominator) < 1e-9)
                return false;

            double qpx = c.X - a.X;
            double qpy = c.Y - a.Y;
            double t = (qpx * sy - qpy * sx) / denominator;
            double u = (qpx * ry - qpy * rx) / denominator;
            if (t < -1e-9 || t > 1.0 + 1e-9 || u < -1e-9 || u > 1.0 + 1e-9)
                return false;

            amount = Math.Clamp(t, 0.0, 1.0);
            return true;
        }

        private static double ClosestPointAmountToLine(Pos a, Pos b, Line line)
        {
            double abx = b.X - a.X;
            double aby = b.Y - a.Y;
            double lengthSquared = abx * abx + aby * aby;
            if (lengthSquared < 1e-9)
                return 0.0;

            double lx = line.P1.X - line.P0.X;
            double ly = line.P1.Y - line.P0.Y;
            double denominator = abx * ly - aby * lx;
            if (Math.Abs(denominator) < 1e-9)
                return 0.0;

            double t = ((line.P0.X - a.X) * ly - (line.P0.Y - a.Y) * lx) / denominator;
            return Math.Clamp(t, 0.0, 1.0);
        }

        private static double PointLineDistanceSquared(Pos point, Line line)
        {
            double dx = line.P1.X - line.P0.X;
            double dy = line.P1.Y - line.P0.Y;
            double lengthSquared = dx * dx + dy * dy;
            if (lengthSquared < 1e-9)
                return DistanceSquared(point, line.P0);

            double cross = (point.X - line.P0.X) * dy - (point.Y - line.P0.Y) * dx;
            return cross * cross / lengthSquared;
        }

        public Pos GetRealPosition(Pos ScreenPosition)
        {
            if (!HasValidGeometry())
                return new Pos();

            if (HasCurvedGeometry())
                return GetRealPositionCurved(ScreenPosition);

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

            if (HasCurvedGeometry())
                return GetScreenPositionCurved(RealPosition);

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
            bool hasBaseGeometry = PavementWidth > 0.0
                && Length > 0.0
                && Math.Abs(NearDistance - FarDistance) > 1e-9
                && Math.Abs(RightEdge.P0.X - LeftEdge.P0.X) > 1e-9
                && Math.Abs(RightEdge.P1.X - LeftEdge.P1.X) > 1e-9;

            if (!hasBaseGeometry)
                return false;

            if (!HasCurvedGeometry())
                return true;

            return HasValidEdgePoints(LeftEdgePoints)
                && HasValidEdgePoints(RightEdgePoints)
                && GetCurveWidth(0.0) > 1e-9
                && GetCurveWidth(1.0) > 1e-9;
        }

        private bool HasCurvedGeometry()
        {
            return LeftEdgePoints != null
                && RightEdgePoints != null
                && LeftEdgePoints.Count >= 2
                && RightEdgePoints.Count >= 2;
        }

        private static bool HasValidEdgePoints(List<Pos> points)
        {
            if (points == null || points.Count < 2)
                return false;

            foreach (Pos point in points)
            {
                if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
                    return false;
            }

            return true;
        }

        private Pos GetScreenPositionCurved(Pos realPosition)
        {
            double station = Length <= 0.0 ? 0.0 : Math.Clamp(realPosition.Y / Length, 0.0, 1.0);
            double lateral = PavementWidth <= 0.0 ? 0.0 : realPosition.X / PavementWidth;
            Pos left = GetEdgePoint(LeftEdgePoints, station);
            Pos right = GetEdgePoint(RightEdgePoints, station);

            return new Pos(
                Lerp(left.X, right.X, lateral),
                Lerp(left.Y, right.Y, lateral));
        }

        private Pos GetRealPositionCurved(Pos screenPosition)
        {
            double bestStation = FindClosestCurveStation(screenPosition);
            Pos left = GetEdgePoint(LeftEdgePoints, bestStation);
            Pos right = GetEdgePoint(RightEdgePoints, bestStation);
            double dx = right.X - left.X;
            double dy = right.Y - left.Y;
            double widthSquared = dx * dx + dy * dy;
            if (widthSquared < 1e-9)
                return new Pos();

            double lateral = ((screenPosition.X - left.X) * dx + (screenPosition.Y - left.Y) * dy) / widthSquared;
            return new Pos(PavementWidth * lateral, Length * bestStation);
        }

        private double FindClosestCurveStation(Pos screenPosition)
        {
            const int samples = 80;
            double bestStation = 0.0;
            double bestDistance = double.MaxValue;

            for (int i = 0; i <= samples; i++)
            {
                double station = (double)i / samples;
                double distance = DistanceToCrossSectionSquared(screenPosition, station);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestStation = station;
                }
            }

            double left = Math.Max(0.0, bestStation - 1.0 / samples);
            double right = Math.Min(1.0, bestStation + 1.0 / samples);
            for (int i = 0; i < 24; i++)
            {
                double m1 = left + (right - left) / 3.0;
                double m2 = right - (right - left) / 3.0;
                if (DistanceToCrossSectionSquared(screenPosition, m1) < DistanceToCrossSectionSquared(screenPosition, m2))
                    right = m2;
                else
                    left = m1;
            }

            return (left + right) / 2.0;
        }

        private double DistanceToCrossSectionSquared(Pos screenPosition, double station)
        {
            Pos left = GetEdgePoint(LeftEdgePoints, station);
            Pos right = GetEdgePoint(RightEdgePoints, station);
            double dx = right.X - left.X;
            double dy = right.Y - left.Y;
            double widthSquared = dx * dx + dy * dy;
            if (widthSquared < 1e-9)
                return double.MaxValue;

            double lateral = ((screenPosition.X - left.X) * dx + (screenPosition.Y - left.Y) * dy) / widthSquared;
            lateral = Math.Clamp(lateral, 0.0, 1.0);
            double x = Lerp(left.X, right.X, lateral);
            double y = Lerp(left.Y, right.Y, lateral);
            double ex = screenPosition.X - x;
            double ey = screenPosition.Y - y;
            return ex * ex + ey * ey;
        }

        private double GetCurveWidth(double station)
        {
            Pos left = GetEdgePoint(LeftEdgePoints, station);
            Pos right = GetEdgePoint(RightEdgePoints, station);
            double dx = right.X - left.X;
            double dy = right.Y - left.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static Pos GetEdgePoint(List<Pos> points, double station)
        {
            station = Math.Clamp(station, 0.0, 1.0);
            List<Pos> edge = GetSmoothEdgePoints(points);
            return GetPolylinePointAtDistance(edge, GetPolylineLength(edge) * station);
        }

        private static double GetPolylineLength(List<Pos> points)
        {
            double length = 0.0;
            for (int i = 0; i < points.Count - 1; i++)
                length += SegmentLength(points[i], points[i + 1]);

            return length;
        }

        private static double SegmentLength(Pos a, Pos b)
        {
            return Math.Sqrt(DistanceSquared(a, b));
        }

        private static double DistanceSquared(Pos a, Pos b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return dx * dx + dy * dy;
        }

        private static double Lerp(double start, double end, double amount)
        {
            return start + (end - start) * amount;
        }

        public Bitmap CreateTopView(Bitmap source, int pixelsPerMeter = 0, bool sharpen = true)
        {
            if (HasCurvedGeometry())
                return CreateCurvedTopView(source, pixelsPerMeter, sharpen);

            if (!TryGetOrderedScreenCorners(source, out Pos[] screenCorners))
                return null;

            double visibleLength = TopViewFarRealY - TopViewNearRealY;
            if (pixelsPerMeter <= 0)
            {
                double sourcePixelCount = (double)source.Width * source.Height;
                pixelsPerMeter = Math.Max(1, (int)Math.Ceiling(
                    Math.Sqrt(sourcePixelCount / (PavementWidth * visibleLength))));
            }

            int width = Math.Max(1, (int)Math.Round(PavementWidth * pixelsPerMeter));
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
            int sourceWidth = sourceRect.Width;
            int sourceHeight = sourceRect.Height;
            int sourceStride = sourceData.Stride;
            int resultStride = resultData.Stride;

            try
            {
                unsafe
                {
                    nint sourceAddress = sourceData.Scan0;
                    nint resultAddress = resultData.Scan0;
                    Parallel.For(0, height, y =>
                    {
                        byte* sourceBase = (byte*)sourceAddress;
                        byte* resultBase = (byte*)resultAddress;
                        double v = (double)y / Math.Max(1, height - 1);
                        byte* resultRow = resultBase + y * resultStride;

                        for (int x = 0; x < width; x++)
                        {
                            double u = (double)x / Math.Max(1, width - 1);
                            Pos screen = ApplyHomography(topViewToScreen, u, v);
                            SampleBilinear(sourceBase, sourceStride, sourceWidth, sourceHeight, screen.X, screen.Y, resultRow + x * 4);
                        }
                    });
                }
            }
            finally
            {
                source32.UnlockBits(sourceData);
                result.UnlockBits(resultData);
            }

            if (sharpen)
                Sharpen(result);

            return result;
        }

        private Bitmap CreateCurvedTopView(Bitmap source, int pixelsPerMeter, bool sharpen)
        {
            if (!HasValidGeometry() || source == null)
                return null;

            TopViewNearRealY = 0.0;
            TopViewFarRealY = Length;
            if (pixelsPerMeter <= 0)
            {
                double sourcePixelCount = (double)source.Width * source.Height;
                pixelsPerMeter = Math.Max(1, (int)Math.Ceiling(
                    Math.Sqrt(sourcePixelCount / (PavementWidth * Length))));
            }

            int width = Math.Max(1, (int)Math.Round(PavementWidth * pixelsPerMeter));
            int height = Math.Max(1, (int)Math.Round(Length * pixelsPerMeter));
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
                    nint sourceAddress = sourceData.Scan0;
                    nint resultAddress = resultData.Scan0;
                    int sourceWidth = sourceRect.Width;
                    int sourceHeight = sourceRect.Height;
                    int sourceStride = sourceData.Stride;
                    int resultStride = resultData.Stride;

                    Parallel.For(0, height, y =>
                    {
                        byte* sourceBase = (byte*)sourceAddress;
                        byte* resultBase = (byte*)resultAddress;
                        byte* resultRow = resultBase + y * resultStride;
                        double realY = Length - Length * y / Math.Max(1, height - 1);

                        for (int x = 0; x < width; x++)
                        {
                            double realX = PavementWidth * x / Math.Max(1, width - 1);
                            Pos screen = GetScreenPositionCurved(new Pos(realX, realY));
                            SampleBilinear(sourceBase, sourceStride, sourceWidth, sourceHeight, screen.X, screen.Y, resultRow + x * 4);
                        }
                    });
                }
            }
            finally
            {
                source32.UnlockBits(sourceData);
                result.UnlockBits(resultData);
            }

            if (sharpen)
                Sharpen(result);

            return result;
        }

        private static void Sharpen(Bitmap bitmap)
        {
            using Bitmap original = bitmap.Clone(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                PixelFormat.Format32bppArgb);
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData sourceData = original.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData targetData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int width = rect.Width;
            int height = rect.Height;
            int sourceStride = sourceData.Stride;
            int targetStride = targetData.Stride;

            try
            {
                unsafe
                {
                    nint sourceAddress = sourceData.Scan0;
                    nint targetAddress = targetData.Scan0;
                    Parallel.For(1, height - 1, y =>
                    {
                        byte* source = (byte*)sourceAddress;
                        byte* target = (byte*)targetAddress;
                        byte* sourceRow = source + y * sourceStride;
                        byte* targetRow = target + y * targetStride;

                        for (int x = 1; x < width - 1; x++)
                        {
                            byte* center = sourceRow + x * 4;
                            byte* left = center - 4;
                            byte* right = center + 4;
                            byte* top = center - sourceStride;
                            byte* bottom = center + sourceStride;
                            byte* output = targetRow + x * 4;

                            for (int channel = 0; channel < 3; channel++)
                            {
                                int sharpened = center[channel] * 6
                                    - left[channel] - right[channel] - top[channel] - bottom[channel];
                                output[channel] = (byte)Math.Clamp(sharpened / 2, 0, 255);
                            }
                        }
                    });
                }
            }
            finally
            {
                original.UnlockBits(sourceData);
                bitmap.UnlockBits(targetData);
            }
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

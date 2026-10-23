using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PaveMetric
{
    [Serializable]
    public class PerspectiveCorrection
    {
        private double _pavementWidth;
        private double _length;
        // The cached curved model is scaled by these, so a change must rebuild it.
        public double PavementWidth
        {
            get => _pavementWidth;
            set { _pavementWidth = value; InvalidateCurvedGeometry(); }
        }
        public double Length
        {
            get => _length;
            set { _length = value; InvalidateCurvedGeometry(); }
        }
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

        [XmlIgnore] private CurvedGeometry _curvedGeometry;
        private int _roadModelVersion;
        // Missing from older XML files: retain their original coordinate interpretation
        // until the user explicitly normalizes the photo again.
        public int RoadModelVersion
        {
            get => _roadModelVersion;
            set { _roadModelVersion = value; InvalidateCurvedGeometry(); }
        }
        [XmlIgnore] public bool UsesMetricRoadPlane => HasCurvedGeometry() && GetCurvedGeometry()?.HasMetricPlane == true;
        [XmlIgnore] public bool HasUsableCoordinates => HasValidGeometry();

        /// <summary>Area of a preserved photo boundary in the current physical plane.</summary>
        public double GetImagePolygonArea(IReadOnlyList<Pos> outline)
        {
            if (outline == null || outline.Count < 3 || !HasValidGeometry()) return 0;
            CurvedGeometry geometry = HasCurvedGeometry() ? GetCurvedGeometry() : null;
            Pos Map(Pos p) => geometry?.HasMetricPlane == true ? geometry.ToMetric(p) : GetRealPosition(p);
            Pos origin = Map(outline[0]), previous = origin;
            if (!double.IsFinite(origin.X + origin.Y)) return 0;
            double twiceArea = 0;
            for (int i = 1; i < outline.Count; i++)
            {
                Pos current = Map(outline[i]);
                if (!double.IsFinite(current.X + current.Y)) return 0;
                twiceArea += (previous.X - origin.X) * (current.Y - origin.Y)
                    - (current.X - origin.X) * (previous.Y - origin.Y);
                previous = current;
            }
            return Math.Abs(twiceArea) / 2;
        }

        /// <summary>Physical area of a road-coordinate rectangle, including inner/outer curve scale.</summary>
        public double GetRoadArea(double left, double right, double near, double far)
        {
            if (!double.IsFinite(left + right + near + far) || right <= left || far <= near)
                return 0;
            CurvedGeometry geometry = HasCurvedGeometry() ? GetCurvedGeometry() : null;
            if (geometry?.HasMetricPlane != true)
                return (right - left) * (far - near);
            near = Math.Clamp(near, 0, Length);
            far = Math.Clamp(far, 0, Length);
            if (far <= near) return 0;
            double area = 0;
            double a = left / PavementWidth, b = right / PavementWidth;
            int segments = Math.Max(1, (int)Math.Ceiling((far - near) / Length * (geometry.Count - 1)));
            geometry.GetCrossSectionAtRealY(near, out Pos l0, out Pos r0);
            Pos p0 = new Pos(Lerp(l0.X, r0.X, a), Lerp(l0.Y, r0.Y, a));
            Pos q0 = new Pos(Lerp(l0.X, r0.X, b), Lerp(l0.Y, r0.Y, b));
            for (int i = 1; i <= segments; i++)
            {
                geometry.GetCrossSectionAtRealY(Lerp(near, far, (double)i / segments), out Pos l, out Pos r);
                Pos p = new Pos(Lerp(l.X, r.X, a), Lerp(l.Y, r.Y, a));
                Pos q = new Pos(Lerp(l.X, r.X, b), Lerp(l.Y, r.Y, b));
                area += Math.Abs((p0.X * q0.Y - q0.X * p0.Y) + (q0.X * q.Y - q.X * q0.Y)
                    + (q.X * p.Y - p.X * q.Y) + (p.X * p0.Y - p0.X * p.Y)) / 2;
                p0 = p; q0 = q;
            }
            return area;
        }

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

        /// <summary>
        /// A new, normalized correction with the same photo edges but other real dimensions.
        /// Use it with <see cref="PavementPhoto.TryReplaceCorrection"/> so recorded errors are
        /// reprojected instead of silently moving on the photo.
        /// </summary>
        public PerspectiveCorrection WithDimensions(double length, double width)
        {
            var copy = new PerspectiveCorrection { RowCount = RowCount, ColCount = ColCount };
            if (LeftEdgePoints?.Count > 2 || RightEdgePoints?.Count > 2)
            {
                copy.NormalizeCurved(LeftEdgePoints, RightEdgePoints, length, width);
            }
            else
            {
                Line Between(Pos a, Pos b) => new Line { P0 = ClonePoint(a), P1 = ClonePoint(b) };
                copy.Normalize(new[]
                {
                    Between(LeftEdge.P1, RightEdge.P1),   // far
                    Between(LeftEdge.P0, RightEdge.P0),   // near
                    Between(LeftEdge.P0, LeftEdge.P1),
                    Between(RightEdge.P0, RightEdge.P1),
                }, length, width);
            }
            return copy;
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

            if (!TryGetLineIntersection(nearLine, leftLine, out Pos nearLeft)
                || !TryGetLineIntersection(nearLine, rightLine, out Pos nearRight)
                || !TryGetLineIntersection(farLine, leftLine, out Pos farLeft)
                || !TryGetLineIntersection(farLine, rightLine, out Pos farRight))
                return;

            Length = length;
            PavementWidth = width;
            NearDistance = (nearLeft.Y + nearRight.Y) / 2.0;
            FarDistance = (farLeft.Y + farRight.Y) / 2.0;
            if (Math.Abs(NearDistance - FarDistance) < 1e-9)
                return;

            LeftEdge.P0 = ClonePoint(nearLeft);
            LeftEdge.P1 = ClonePoint(farLeft);
            RightEdge.P0 = ClonePoint(nearRight);
            RightEdge.P1 = ClonePoint(farRight);
            LeftEdgePoints = new List<Pos> { ClonePoint(nearLeft), ClonePoint(farLeft) };
            RightEdgePoints = new List<Pos> { ClonePoint(nearRight), ClonePoint(farRight) };
            RoadModelVersion = 1;
            InvalidateCurvedGeometry();
            Normalized = true;
        }

        private static bool TryGetLineIntersection(Line first, Line second, out Pos intersection)
        {
            double ax = first.P1.X - first.P0.X;
            double ay = first.P1.Y - first.P0.Y;
            double bx = second.P1.X - second.P0.X;
            double by = second.P1.Y - second.P0.Y;
            double determinant = ax * by - ay * bx;
            if (Math.Abs(determinant) < 1e-9)
            {
                intersection = null;
                return false;
            }

            double dx = second.P0.X - first.P0.X;
            double dy = second.P0.Y - first.P0.Y;
            double amount = (dx * by - dy * bx) / determinant;
            intersection = new Pos(first.P0.X + ax * amount, first.P0.Y + ay * amount);
            return double.IsFinite(intersection.X) && double.IsFinite(intersection.Y);
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

            RoadModelVersion = 1;
            InvalidateCurvedGeometry();
            TopViewNearRealY = 0.0;
            TopViewFarRealY = Length;
            Normalized = HasValidGeometry();
        }

        private static List<Pos> ClonePoints(List<Pos> points)
        {
            List<Pos> result = new List<Pos>(points.Count);
            foreach (Pos point in points)
                result.Add(ClonePoint(point));
            return result;
        }

        public static List<Pos> GetSmoothEdgePoints(List<Pos> points)
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
                    result.Add(CentripetalCatmullRom(p0, p1, p2, p3, t));
                }
            }

            return result;
        }

        private static Pos CentripetalCatmullRom(Pos p0, Pos p1, Pos p2, Pos p3, double amount)
        {
            double t0 = 0.0;
            double t1 = t0 + CentripetalKnot(p0, p1);
            double t2 = t1 + CentripetalKnot(p1, p2);
            double t3 = t2 + CentripetalKnot(p2, p3);
            double t = Lerp(t1, t2, amount);
            Pos a1 = InterpolateAt(p0, p1, t0, t1, t);
            Pos a2 = InterpolateAt(p1, p2, t1, t2, t);
            Pos a3 = InterpolateAt(p2, p3, t2, t3, t);
            Pos b1 = InterpolateAt(a1, a2, t0, t2, t);
            Pos b2 = InterpolateAt(a2, a3, t1, t3, t);
            return InterpolateAt(b1, b2, t1, t2, t);
        }

        private static double CentripetalKnot(Pos first, Pos second)
        {
            return Math.Max(1e-6, Math.Sqrt(Math.Sqrt(DistanceSquared(first, second))));
        }

        private static Pos InterpolateAt(Pos first, Pos second, double start, double end, double value)
        {
            double fraction = (value - start) / (end - start);
            return new Pos(Lerp(first.X, second.X, fraction), Lerp(first.Y, second.Y, fraction));
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

        /// <summary>
        /// Calibrates a photo reference while updating the metric plane as its width changes.
        /// This avoids using a plane fitted to an arbitrary initial width as a fixed ruler.
        /// </summary>
        public bool TryCalibrateWidthFromImageReference(Pos start, Pos end, double referenceLength, out double width)
        {
            width = 0;
            if (!Normalized || start == null || end == null || !double.IsFinite(referenceLength)
                || referenceLength <= 0 || !HasValidGeometry()) return false;
            PerspectiveCorrection trial = this;
            double candidate = PavementWidth;
            for (int iteration = 0; iteration < 8; iteration++)
            {
                Pos a = trial.GetRealPosition(start), b = trial.GetRealPosition(end);
                double fraction = Math.Abs(b.X - a.X) / trial.PavementWidth;
                if (!double.IsFinite(fraction) || fraction <= 1e-6 || fraction > 1.000001) return false;
                double next = referenceLength / fraction;
                if (!double.IsFinite(next) || next <= 0) return false;
                if (RoadModelVersion == 0 || !HasCurvedGeometry() || Math.Abs(next - candidate) < 0.0005)
                    return trial.TryGetWidthFromTransverseReference(a, b, referenceLength, out width);
                candidate = next;
                trial = new PerspectiveCorrection();
                trial.NormalizeCurved(LeftEdgePoints, RightEdgePoints, Length, candidate);
            }
            return trial.TryGetWidthFromTransverseReference(trial.GetRealPosition(start),
                trial.GetRealPosition(end), referenceLength, out width);
        }

        /// <summary>
        /// Infers constant pavement width from a known transverse reference. Endpoints are
        /// road coordinates obtained from the current correction, and referenceLength is in
        /// metres. The result inherits any errors in the underlying curved road model.
        /// </summary>
        public bool TryGetWidthFromTransverseReference(Pos start, Pos end, double referenceLength, out double width)
        {
            width = 0.0;
            if (!Normalized || !HasValidGeometry() || start == null || end == null
                || !double.IsFinite(referenceLength) || referenceLength <= 0.0
                || !double.IsFinite(PavementWidth) || !double.IsFinite(Length)
                || !double.IsFinite(start.X) || !double.IsFinite(start.Y)
                || !double.IsFinite(end.X) || !double.IsFinite(end.Y))
                return false;

            double epsilon = 1e-6;
            if (start.X < -epsilon || start.X > PavementWidth + epsilon
                || end.X < -epsilon || end.X > PavementWidth + epsilon
                || start.Y < -epsilon || start.Y > Length + epsilon
                || end.Y < -epsilon || end.Y > Length + epsilon)
                return false;

            // Allow small endpoint-placement errors, but do not treat a longitudinal or
            // diagonal base as a transverse reference. This is a UI tolerance, not a
            // statement of the geometric model's accuracy.
            if (Math.Abs(end.Y - start.Y) > Math.Max(0.02, referenceLength * 0.05))
                return false;

            double fraction = Math.Abs(end.X - start.X) / PavementWidth;
            if (!double.IsFinite(fraction) || fraction <= 1e-6)
                return false;

            width = referenceLength / fraction;
            return double.IsFinite(width) && width > 0.0;
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
            CurvedGeometry geometry = GetCurvedGeometry();
            if (geometry == null)
                return new Pos();

            double lateral = PavementWidth <= 0.0 ? 0.0 : realPosition.X / PavementWidth;
            geometry.GetCrossSectionAtRealY(realPosition.Y, out Pos left, out Pos right);
            return geometry.ToScreen(new Pos(
                Lerp(left.X, right.X, lateral),
                Lerp(left.Y, right.Y, lateral)));
        }

        private Pos GetRealPositionCurved(Pos screenPosition)
        {
            CurvedGeometry geometry = GetCurvedGeometry();
            if (geometry == null)
                return new Pos();

            screenPosition = geometry.ToMetric(screenPosition);
            double index = geometry.FindClosestCrossSection(screenPosition);
            geometry.GetCrossSectionAt(index, out Pos left, out Pos right);
            double dx = right.X - left.X;
            double dy = right.Y - left.Y;
            double widthSquared = dx * dx + dy * dy;
            if (widthSquared < 1e-9)
                return new Pos();

            double lateral = ((screenPosition.X - left.X) * dx + (screenPosition.Y - left.Y) * dy) / widthSquared;
            return new Pos(PavementWidth * lateral, geometry.GetRealYAt(index));
        }

        private double GetCurveWidth(double station)
        {
            CurvedGeometry geometry = GetCurvedGeometry();
            if (geometry == null)
                return 0.0;

            geometry.GetCrossSectionAt(station * (geometry.Count - 1), out Pos left, out Pos right);
            double dx = right.X - left.X;
            double dy = right.Y - left.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private void InvalidateCurvedGeometry()
        {
            _curvedGeometry = null;
        }

        private CurvedGeometry GetCurvedGeometry()
        {
            if (!HasCurvedGeometry() || !HasValidEdgePoints(LeftEdgePoints) || !HasValidEdgePoints(RightEdgePoints)
                || PavementWidth <= 0.0 || Length <= 0.0)
                return null;

            if (_curvedGeometry == null || _curvedGeometry.IsStale(LeftEdgePoints, RightEdgePoints, PavementWidth, Length))
                _curvedGeometry = CurvedGeometry.Build(LeftEdgePoints, RightEdgePoints, PavementWidth, Length, RoadModelVersion >= 1);

            return _curvedGeometry;
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
                            SampleBicubic(sourceBase, sourceStride, sourceWidth, sourceHeight, screen.X, screen.Y, resultRow + x * 4);
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
            CurvedGeometry geometry = GetCurvedGeometry();
            if (!HasValidGeometry() || source == null || geometry == null)
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
                        geometry.GetCrossSectionAtRealY(realY, out Pos left, out Pos right);
                        double stepX = (right.X - left.X) / Math.Max(1, width - 1);
                        double stepY = (right.Y - left.Y) / Math.Max(1, width - 1);

                        for (int x = 0; x < width; x++)
                        {
                            geometry.ToScreen(left.X + stepX * x, left.Y + stepY * x,
                                out double imageX, out double imageY);
                            SampleBicubic(
                                sourceBase, sourceStride, sourceWidth, sourceHeight,
                                imageX, imageY, resultRow + x * 4);
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
                                // A small cross-shaped blur is subtracted from the original.
                                // This unsharp mask avoids the harsh halos of the former
                                // Laplacian-style filter while restoring some local contrast.
                                double blur = (center[channel] * 4.0 + left[channel] + right[channel]
                                    + top[channel] + bottom[channel]) / 8.0;
                                double sharpened = center[channel] + (center[channel] - blur) * 0.6;
                                output[channel] = (byte)Math.Clamp((int)Math.Round(sharpened), 0, 255);
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

        private static unsafe void SampleBicubic(
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

            int ix = (int)Math.Floor(x);
            int iy = (int)Math.Floor(y);
            double fx = x - ix;
            double fy = y - iy;

            for (int channel = 0; channel < 4; channel++)
            {
                double row0 = SampleCubicRow(source, stride, width, Math.Clamp(iy - 1, 0, height - 1), ix, fx, channel);
                double row1 = SampleCubicRow(source, stride, width, Math.Clamp(iy, 0, height - 1), ix, fx, channel);
                double row2 = SampleCubicRow(source, stride, width, Math.Clamp(iy + 1, 0, height - 1), ix, fx, channel);
                double row3 = SampleCubicRow(source, stride, width, Math.Clamp(iy + 2, 0, height - 1), ix, fx, channel);

                target[channel] = (byte)Math.Clamp(
                    (int)Math.Round(CubicInterpolate(row0, row1, row2, row3, fy)), 0, 255);
            }
        }

        private static unsafe double SampleCubicRow(byte* source, int stride, int width, int y, int x, double amount, int channel)
        {
            byte* row = source + y * stride;
            double p0 = row[Math.Clamp(x - 1, 0, width - 1) * 4 + channel];
            double p1 = row[Math.Clamp(x, 0, width - 1) * 4 + channel];
            double p2 = row[Math.Clamp(x + 1, 0, width - 1) * 4 + channel];
            double p3 = row[Math.Clamp(x + 2, 0, width - 1) * 4 + channel];
            return CubicInterpolate(p0, p1, p2, p3, amount);
        }

        private static double CubicInterpolate(double p0, double p1, double p2, double p3, double amount)
        {
            return p1 + 0.5 * amount * (p2 - p0 + amount *
                (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3 + amount *
                (3.0 * (p1 - p2) + p3 - p0)));
        }

        /// <summary>
        /// Cross-sections paired in a fitted metric pavement plane, parameterized by
        /// centerline arc length. Incompatible geometry and legacy projects retain the
        /// original approximate inverse-width-squared stationing as a compatibility fallback.
        /// Index 0 is the near end (real Y = 0), the last index is the far end (real Y = Length).
        /// </summary>
        private sealed class CurvedGeometry
        {
            private const int SectionCount = 513;
            private RoadPlaneModel _plane;
            private bool _extendCoordinates;
            public bool HasMetricPlane => _plane != null;
            public Pos ToScreen(Pos point) => _plane?.ToImage(point) ?? point;
            public Pos ToMetric(Pos point) => _plane?.ToGround(point) ?? point;
            public void ToScreen(double x, double y, out double screenX, out double screenY)
            {
                if (_plane != null) _plane.ToImage(x, y, out screenX, out screenY);
                else { screenX = x; screenY = y; }
            }

            private Pos[] _left;
            private Pos[] _right;
            private double[] _realY;

            private List<Pos> _sourceLeft;
            private List<Pos> _sourceRight;
            private int _sourceLeftCount;
            private int _sourceRightCount;
            private double _sourceWidth;
            private double _sourceLength;

            public int Count => _realY.Length;

            public bool IsStale(List<Pos> leftPoints, List<Pos> rightPoints, double width, double length)
            {
                return !ReferenceEquals(_sourceLeft, leftPoints)
                    || !ReferenceEquals(_sourceRight, rightPoints)
                    || _sourceLeftCount != leftPoints.Count
                    || _sourceRightCount != rightPoints.Count
                    || _sourceWidth != width
                    || _sourceLength != length;
            }

            public static CurvedGeometry Build(List<Pos> leftPoints, List<Pos> rightPoints, double width, double length, bool useMetric)
            {
                List<Pos> smoothLeft = GetSmoothEdgePoints(leftPoints);
                List<Pos> smoothRight = GetSmoothEdgePoints(rightPoints);
                RoadPlaneModel plane = useMetric ? RoadPlaneModel.Fit(smoothLeft, smoothRight, width, length) : null;
                if (plane != null)
                {
                    if (smoothLeft.Count == 2 && smoothRight.Count == 2)
                        return new CurvedGeometry { _plane = plane, _extendCoordinates = useMetric,
                            _left = new[] { plane.ToGround(smoothLeft[0]), plane.ToGround(smoothLeft[1]) },
                            _right = new[] { plane.ToGround(smoothRight[0]), plane.ToGround(smoothRight[1]) },
                            _realY = new[] { 0.0, length }, _sourceLeft = leftPoints, _sourceRight = rightPoints,
                            _sourceLeftCount = leftPoints.Count, _sourceRightCount = rightPoints.Count,
                            _sourceWidth = width, _sourceLength = length };
                    Pos[] leftGround = RoadPlaneModel.Resample(smoothLeft.ConvertAll(plane.ToGround), SectionCount);
                    Pos[] rightGround = RoadPlaneModel.Resample(smoothRight.ConvertAll(plane.ToGround), SectionCount * 2 - 1);
                    Pos[] rightPaired = RoadPlaneModel.PairOffsets(leftGround, rightGround);
                    double[] station = new double[SectionCount];
                    Pos previousCenter = null;
                    for (int i = 0; i < SectionCount; i++)
                    {
                        Pos center = new Pos((leftGround[i].X + rightPaired[i].X) / 2,
                            (leftGround[i].Y + rightPaired[i].Y) / 2);
                        if (previousCenter != null) station[i] = station[i - 1] + SegmentLength(previousCenter, center);
                        previousCenter = center;
                    }
                    for (int i = 1; i < SectionCount; i++) station[i] *= length / station[SectionCount - 1];
                    return new CurvedGeometry { _plane = plane, _extendCoordinates = useMetric, _left = leftGround, _right = rightPaired, _realY = station,
                        _sourceLeft = leftPoints, _sourceRight = rightPoints, _sourceLeftCount = leftPoints.Count,
                        _sourceRightCount = rightPoints.Count, _sourceWidth = width, _sourceLength = length };
                }
                SampledPolyline leftSmooth = new SampledPolyline(smoothLeft);
                SampledPolyline rightSmooth = new SampledPolyline(smoothRight);

                // Approximate station parameterization using inverse squared nearest-edge
                // distance. For converging straight edges this follows the projective model
                // under the assumptions above. Curved edges do not generally satisfy that
                // relationship; see the independent --measurement-audit camera projection.
                // The edges are resampled densely so the density integral converges.
                const int integrationSamples = 1024;
                SampledPolyline leftEdge = leftSmooth.ResampleUniform(integrationSamples);
                SampledPolyline rightEdge = rightSmooth.ResampleUniform(integrationSamples);
                double[] leftStation = leftEdge.BuildCorrectedStations(rightSmooth);
                double[] rightStation = rightEdge.BuildCorrectedStations(leftSmooth);

                const int legacyCount = 257;
                Pos[] left = new Pos[legacyCount];
                Pos[] right = new Pos[legacyCount];
                double[] realY = new double[legacyCount];
                for (int i = 0; i < legacyCount; i++)
                {
                    double fraction = (double)i / (legacyCount - 1);
                    left[i] = leftEdge.PointAtStation(leftStation, fraction);
                    right[i] = rightEdge.PointAtStation(rightStation, fraction);
                    realY[i] = fraction * length;
                }

                return new CurvedGeometry
                {
                    _extendCoordinates = useMetric,
                    _left = left,
                    _right = right,
                    _realY = realY,
                    _sourceLeft = leftPoints,
                    _sourceRight = rightPoints,
                    _sourceLeftCount = leftPoints.Count,
                    _sourceRightCount = rightPoints.Count,
                    _sourceWidth = width,
                    _sourceLength = length
                };
            }

            /// <summary>Cross-section endpoints; version 1 also extends beyond the ends.</summary>
            public void GetCrossSectionAt(double index, out Pos left, out Pos right)
            {
                if (!_extendCoordinates) index = Math.Clamp(index, 0.0, Count - 1);
                int i0 = Math.Clamp((int)index, 0, Count - 2);
                double amount = index - i0;
                if (_extendCoordinates && Count > 2 && (index < 0 || index > Count - 1))
                {
                    int end = index < 0 ? 0 : Count - 1;
                    double dx = (_left[i0 + 1].X - _left[i0].X + _right[i0 + 1].X - _right[i0].X) / 2;
                    double dy = (_left[i0 + 1].Y - _left[i0].Y + _right[i0 + 1].Y - _right[i0].Y) / 2;
                    // Continue the terminal tangent at fixed width. Continuing the
                    // change of width as well creates a fan with a remote singularity.
                    left = new Pos(_left[end].X + (index - end) * dx, _left[end].Y + (index - end) * dy);
                    right = new Pos(_right[end].X + (index - end) * dx, _right[end].Y + (index - end) * dy);
                    return;
                }
                left = new Pos(
                    Lerp(_left[i0].X, _left[i0 + 1].X, amount),
                    Lerp(_left[i0].Y, _left[i0 + 1].Y, amount));
                right = new Pos(
                    Lerp(_right[i0].X, _right[i0 + 1].X, amount),
                    Lerp(_right[i0].Y, _right[i0 + 1].Y, amount));
            }

            public double GetRealYAt(double index)
            {
                if (!_extendCoordinates) index = Math.Clamp(index, 0.0, Count - 1);
                int i0 = Math.Clamp((int)index, 0, Count - 2);
                return Lerp(_realY[i0], _realY[i0 + 1], index - i0);
            }

            public void GetCrossSectionAtRealY(double realY, out Pos left, out Pos right)
            {
                if (!_extendCoordinates) realY = Math.Clamp(realY, _realY[0], _realY[Count - 1]);
                int low = 0;
                int high = Count - 1;
                while (high - low > 1)
                {
                    int middle = (low + high) / 2;
                    if (_realY[middle] <= realY)
                        low = middle;
                    else
                        high = middle;
                }

                double span = _realY[high] - _realY[low];
                double amount = span < 1e-12 ? 0.0 : (realY - _realY[low]) / span;
                GetCrossSectionAt(low + amount, out left, out right);
            }

            /// <summary>Fractional table index of the cross-section closest to a screen point.</summary>
            public double FindClosestCrossSection(Pos screenPosition)
            {
                if (_extendCoordinates) return FindExtendedCrossSection(screenPosition);
                int bestIndex = 0;
                double bestDistance = double.MaxValue;
                for (int i = 0; i < Count; i++)
                {
                    double distance = DistanceToCrossSectionSquared(screenPosition, i);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }

                double best = bestIndex;
                // The distance can have a kink at table vertices. Refine each neighboring
                // strip separately instead of assuming the combined interval is unimodal.
                for (int segment = Math.Max(0, bestIndex - 2); segment <= Math.Min(Count - 2, bestIndex + 1); segment++)
                {
                    double low = segment, high = segment + 1;
                    for (int i = 0; i < 40; i++)
                    {
                        double m1 = low + (high - low) / 3.0;
                        double m2 = high - (high - low) / 3.0;
                        if (DistanceToCrossSectionSquared(screenPosition, m1) < DistanceToCrossSectionSquared(screenPosition, m2)) high = m2;
                        else low = m1;
                    }
                    double candidate = (low + high) / 2;
                    double distance = DistanceToCrossSectionSquared(screenPosition, candidate);
                    if (distance < bestDistance) { bestDistance = distance; best = candidate; }
                }
                return best;
            }

            // Invert each bilinear strip exactly, including transverse extensions and
            // the two end strips. Minimizing distance to sampled finite cross-sections
            // can miss the correct strip for a footprint beyond a curved road edge.
            private double FindExtendedCrossSection(Pos point)
            {
                double best = double.NaN, bestDistance = double.PositiveInfinity;
                static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;
                for (int strip = -1; strip < Count; strip++)
                {
                    bool terminal = Count > 2 && (strip == -1 || strip == Count - 1);
                    if (!terminal && (strip < 0 || strip >= Count - 1)) continue;
                    int i = Math.Clamp(strip, 0, Count - 2);
                    Pos l = _left[i], r = _right[i];
                    double lx = _left[i + 1].X - l.X, ly = _left[i + 1].Y - l.Y;
                    double wx = r.X - l.X, wy = r.Y - l.Y;
                    double vx = _right[i + 1].X - _left[i + 1].X - wx;
                    double vy = _right[i + 1].Y - _left[i + 1].Y - wy;
                    int origin = i;
                    if (terminal)
                    {
                        origin = strip == -1 ? 0 : Count - 1;
                        lx += vx / 2; ly += vy / 2;
                        l = _left[origin]; r = _right[origin];
                        wx = r.X - l.X; wy = r.Y - l.Y; vx = vy = 0;
                    }
                    double px = point.X - l.X, py = point.Y - l.Y;
                    double a = -Cross(lx, ly, vx, vy);
                    double b = Cross(px, py, vx, vy) - Cross(lx, ly, wx, wy);
                    double c = Cross(px, py, wx, wy);
                    void Consider(double t)
                    {
                        if (!double.IsFinite(t)) return;
                        if (terminal ? (strip == -1 ? t > 0 : t < 0)
                            : Count > 2 && (t < -1e-8 || t > 1 + 1e-8)) return;
                        double dx = wx + t * vx, dy = wy + t * vy;
                        double square = dx * dx + dy * dy;
                        if (square < 1e-12) return;
                        double lateral = ((px - t * lx) * dx + (py - t * ly) * dy) / square;
                        double ex = px - t * lx - lateral * dx, ey = py - t * ly - lateral * dy;
                        if (ex * ex + ey * ey > 1e-8) return;
                        double index = origin + t;
                        // Where extended strips overlap, prefer the root nearest the
                        // calibrated road, rather than a distant extrapolated branch.
                        double distance = DistanceToCrossSectionSquared(point, Math.Clamp(index, 0, Count - 1), true);
                        if (distance < bestDistance) { bestDistance = distance; best = index; }
                    }
                    if (Math.Abs(a) < 1e-12 * Math.Max(1, Math.Abs(b)))
                    {
                        if (Math.Abs(b) > 1e-20) Consider(-c / b);
                    }
                    else
                    {
                        double discriminant = b * b - 4 * a * c;
                        if (discriminant < 0) continue;
                        double q = -.5 * (b + Math.CopySign(Math.Sqrt(discriminant), b));
                        Consider(q / a);
                        if (q != 0) Consider(c / q);
                    }
                }
                return best;
            }

            private double DistanceToCrossSectionSquared(Pos screenPosition, double index, bool finite = false)
            {
                GetCrossSectionAt(index, out Pos left, out Pos right);
                double dx = right.X - left.X;
                double dy = right.Y - left.Y;
                double widthSquared = dx * dx + dy * dy;
                if (widthSquared < 1e-9)
                    return double.MaxValue;

                double lateral = ((screenPosition.X - left.X) * dx + (screenPosition.Y - left.Y) * dy) / widthSquared;
                // Inverse station lookup must use the whole transverse line. A saved
                // footprint can lie beyond a newly traced edge; the nearest point on
                // the finite road strip has a different station and cannot round-trip.
                if (finite || !_extendCoordinates) lateral = Math.Clamp(lateral, 0.0, 1.0);
                double x = Lerp(left.X, right.X, lateral);
                double y = Lerp(left.Y, right.Y, lateral);
                double ex = screenPosition.X - x;
                double ey = screenPosition.Y - y;
                return ex * ex + ey * ey;
            }
        }

        /// <summary>Polyline with cached cumulative arc lengths for O(log n) point lookup.</summary>
        private sealed class SampledPolyline
        {
            private readonly List<Pos> _points;
            private readonly double[] _cumulative;

            public SampledPolyline(List<Pos> points)
            {
                _points = points;
                _cumulative = new double[points.Count];
                for (int i = 1; i < points.Count; i++)
                    _cumulative[i] = _cumulative[i - 1] + SegmentLength(points[i - 1], points[i]);
            }

            public double TotalLength => _cumulative[_cumulative.Length - 1];

            public SampledPolyline ResampleUniform(int pointCount)
            {
                List<Pos> points = new List<Pos>(pointCount);
                for (int i = 0; i < pointCount; i++)
                    points.Add(PointAtArcLength(TotalLength * i / (pointCount - 1)));

                return new SampledPolyline(points);
            }

            private Pos PointAtArcLength(double target)
            {
                target = Math.Clamp(target, 0.0, TotalLength);
                int low = 0;
                int high = _cumulative.Length - 1;
                while (high - low > 1)
                {
                    int middle = (low + high) / 2;
                    if (_cumulative[middle] <= target)
                        low = middle;
                    else
                        high = middle;
                }

                double span = _cumulative[high] - _cumulative[low];
                double amount = span < 1e-12 ? 0.0 : (target - _cumulative[low]) / span;
                return new Pos(
                    Lerp(_points[low].X, _points[high].X, amount),
                    Lerp(_points[low].Y, _points[high].Y, amount));
            }

            /// <summary>
            /// Cumulative perspective-corrected station of every vertex: screen arc length
            /// weighted by 1/width^2, where width is the distance to the other edge. The result
            /// is normalized to [0, 1] and strictly increasing wherever the width is finite.
            /// </summary>
            public double[] BuildCorrectedStations(SampledPolyline otherEdge)
            {
                double[] stations = new double[_points.Count];
                double previousDensity = InverseWidthSquared(otherEdge.DistanceToPoint(_points[0]));
                for (int i = 1; i < _points.Count; i++)
                {
                    double density = InverseWidthSquared(otherEdge.DistanceToPoint(_points[i]));
                    double segment = _cumulative[i] - _cumulative[i - 1];
                    stations[i] = stations[i - 1] + segment * (previousDensity + density) / 2.0;
                    previousDensity = density;
                }

                double total = stations[_points.Count - 1];
                if (total > 1e-12)
                {
                    for (int i = 0; i < stations.Length; i++)
                        stations[i] /= total;
                }
                else
                {
                    for (int i = 0; i < stations.Length; i++)
                        stations[i] = TotalLength <= 1e-12 ? 0.0 : _cumulative[i] / TotalLength;
                }

                return stations;
            }

            /// <summary>Point at the given fraction of a monotonic per-vertex station table.</summary>
            public Pos PointAtStation(double[] stations, double fraction)
            {
                fraction = Math.Clamp(fraction, 0.0, 1.0);
                int low = 0;
                int high = stations.Length - 1;
                while (high - low > 1)
                {
                    int middle = (low + high) / 2;
                    if (stations[middle] <= fraction)
                        low = middle;
                    else
                        high = middle;
                }

                double span = stations[high] - stations[low];
                double amount = span < 1e-12 ? 0.0 : (fraction - stations[low]) / span;
                return new Pos(
                    Lerp(_points[low].X, _points[high].X, amount),
                    Lerp(_points[low].Y, _points[high].Y, amount));
            }

            public double DistanceToPoint(Pos point)
            {
                double best = double.MaxValue;
                int lastSegment = _points.Count - 2;
                for (int i = 0; i <= lastSegment; i++)
                {
                    Pos a = _points[i];
                    Pos b = _points[i + 1];
                    double abx = b.X - a.X;
                    double aby = b.Y - a.Y;
                    double lengthSquared = abx * abx + aby * aby;
                    double t = lengthSquared < 1e-12
                        ? 0.0
                        : ((point.X - a.X) * abx + (point.Y - a.Y) * aby) / lengthSquared;

                    // The first and last segments extend outward as rays, so widths measured
                    // near the ends of the other edge are not distorted by endpoint clamping.
                    double lower = i == 0 ? double.NegativeInfinity : 0.0;
                    double upper = i == lastSegment ? double.PositiveInfinity : 1.0;
                    t = Math.Clamp(t, lower, upper);

                    double dx = point.X - (a.X + abx * t);
                    double dy = point.Y - (a.Y + aby * t);
                    double distanceSquared = dx * dx + dy * dy;
                    if (distanceSquared < best)
                        best = distanceSquared;
                }

                return Math.Sqrt(best);
            }

            private static double InverseWidthSquared(double width)
            {
                return width < 1e-6 ? 0.0 : 1.0 / (width * width);
            }
        }
    }
}

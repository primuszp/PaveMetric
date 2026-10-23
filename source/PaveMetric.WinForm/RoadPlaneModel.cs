using System;
using System.Collections.Generic;

namespace PaveMetric
{
    /// <summary>Metric plane fitted to constant-width offset curves and a known centerline length.</summary>
    internal sealed class RoadPlaneModel
    {
        private double[] _imageToUnit, _unitToImage, _parameters;
        private double _width, _length;

        public Pos ToGround(Pos image) => Upgrade(Map(_imageToUnit, image), _parameters, _width, _length);

        public Pos ToImage(Pos ground)
        {
            ToImage(ground.X, ground.Y, out double x, out double y);
            return new Pos(x, y);
        }

        public void ToImage(double groundX, double groundY, out double imageX, out double imageY)
        {
            double b = _parameters[0], c = _parameters[1], p = _parameters[2], q = _parameters[3];
            double y = groundY / (_length * c);
            double x = (groundX / _width - b * y) / (1 + p);
            double d = 1 - p * x - q * y;
            double denominator = _unitToImage[6] * x + _unitToImage[7] * y + _unitToImage[8] * d;
            imageX = (_unitToImage[0] * x + _unitToImage[1] * y + _unitToImage[2] * d) / denominator;
            imageY = (_unitToImage[3] * x + _unitToImage[4] * y + _unitToImage[5] * d) / denominator;
        }

        public static RoadPlaneModel Fit(List<Pos> left, List<Pos> right, double width, double length)
        {
            Pos[] corners = { left[0], right[0], right[right.Count - 1], left[left.Count - 1] };
            double[] forward = SquareToQuad(corners);
            if (forward != null && (1 + forward[6] <= 1e-8 || 1 + forward[7] <= 1e-8
                || 1 + forward[6] + forward[7] <= 1e-8)) return null;
            double[] inverse = forward == null ? null : Invert(forward);
            if (inverse == null) return null;
            if (left.Count == 2 && right.Count == 2)
                return new RoadPlaneModel { _imageToUnit = inverse, _unitToImage = forward,
                    _parameters = new[] { 0.0, 1.0, 0.0, 0.0 }, _width = width, _length = length };
            Pos[] a = Resample(left, 65), b = Resample(right, 65);
            for (int i = 0; i < a.Length; i++) { a[i] = Map(inverse, a[i]); b[i] = Map(inverse, b[i]); }
            double[] parameters = { 0, 1, 0, 0 };
            double[] residual = Residual(a, b, parameters, width, length);
            if (residual == null) return null;
            double cost = Norm(residual), damping = 0.001;
            for (int iteration = 0; iteration < 100; iteration++)
            {
                var jacobian = new double[residual.Length, 4];
                for (int j = 0; j < 4; j++)
                {
                    double step = 1e-5 * (1 + Math.Abs(parameters[j]));
                    double[] shifted = (double[])parameters.Clone(); shifted[j] += step;
                    double[] r = Residual(a, b, shifted, width, length);
                    if (r == null) return null;
                    for (int i = 0; i < r.Length; i++) jacobian[i, j] = (r[i] - residual[i]) / step;
                }
                var normal = new double[4, 4]; var rhs = new double[4];
                for (int j = 0; j < 4; j++)
                {
                    for (int i = 0; i < residual.Length; i++)
                    {
                        rhs[j] -= jacobian[i, j] * residual[i];
                        for (int k = 0; k < 4; k++) normal[j, k] += jacobian[i, j] * jacobian[i, k];
                    }
                    normal[j, j] += damping * (normal[j, j] + 1e-6);
                }
                double[] delta = Solve(normal, rhs);
                if (delta == null) return null;
                double[] candidate = (double[])parameters.Clone();
                for (int j = 0; j < 4; j++) candidate[j] += delta[j];
                double[] next = Residual(a, b, candidate, width, length);
                double nextCost = next == null ? double.PositiveInfinity : Norm(next);
                if (nextCost < cost)
                {
                    double improvement = cost - nextCost;
                    parameters = candidate; residual = next; cost = nextCost;
                    damping = Math.Max(1e-9, damping / 3);
                    if (improvement < 1e-13 || Norm(delta) < 1e-16) break;
                }
                else damping *= 10;
                if (damping > 1e10) break;
            }
            // Incompatible edges must not silently be treated as an accurate metric fit.
            if (Math.Sqrt(cost / residual.Length) > 0.02) return null;
            foreach (double value in residual) if (Math.Abs(value) > 0.08) return null;
            return new RoadPlaneModel
            {
                _imageToUnit = inverse,
                _unitToImage = forward,
                _parameters = parameters,
                _width = width,
                _length = length
            };
        }

        private static double[] Residual(Pos[] a, Pos[] b, double[] k, double width, double length)
        {
            if (k[1] <= 0.02 || 1 + k[2] <= 0.02) return null;
            var left = new Pos[a.Length]; var right = new Pos[b.Length];
            for (int i = 0; i < a.Length; i++)
            {
                if (1 + k[2] * a[i].X + k[3] * a[i].Y < 0.03
                    || 1 + k[2] * b[i].X + k[3] * b[i].Y < 0.03) return null;
                left[i] = Upgrade(a[i], k, width, length); right[i] = Upgrade(b[i], k, width, length);
                if (!double.IsFinite(left[i].X + left[i].Y + right[i].X + right[i].Y)) return null;
            }
            var residual = new double[a.Length + b.Length + 2];
            double arcLength = 0;
            for (int i = 0; i < a.Length; i++)
            {
                residual[i] = Distance(left[i], Closest(right, left[i])) / width - 1;
                residual[a.Length + i] = Distance(right[i], Closest(left, right[i])) / width - 1;
                if (i > 0) arcLength += (Distance(left[i - 1], left[i]) + Distance(right[i - 1], right[i])) / 2;
            }
            residual[residual.Length - 2] = 8 * (arcLength / length - 1);
            residual[residual.Length - 1] = 4 * (Distance(left[left.Length - 1], right[right.Length - 1]) / width - 1);
            return residual;
        }

        private static Pos Upgrade(Pos u, double[] k, double width, double length)
        {
            double d = 1 + k[2] * u.X + k[3] * u.Y;
            return new Pos(width * ((1 + k[2]) * u.X + k[0] * u.Y) / d, length * k[1] * u.Y / d);
        }

        internal static Pos Closest(Pos[] line, Pos point)
        {
            Pos best = line[0]; double bestSquared = double.PositiveInfinity;
            for (int i = 1; i < line.Length; i++)
            {
                Pos a = line[i - 1], b = line[i];
                double dx = b.X - a.X, dy = b.Y - a.Y, square = dx * dx + dy * dy;
                double t = square <= 1e-20 ? 0 : Math.Clamp(((point.X - a.X) * dx + (point.Y - a.Y) * dy) / square, 0, 1);
                double x = a.X + t * dx, y = a.Y + t * dy;
                double distance = (point.X - x) * (point.X - x) + (point.Y - y) * (point.Y - y);
                if (distance < bestSquared) { bestSquared = distance; best = new Pos(x, y); }
            }
            return best;
        }

        // Pair offsets using the mean of their tangents. A nearest point on a polygon
        // sticks to a vertex for a finite interval, collapsing stations at the inner edge.
        internal static Pos[] PairOffsets(Pos[] left, Pos[] right)
        {
            Pos Tangent(Pos[] line, int i)
            {
                Pos a = line[Math.Max(0, i - 1)], b = line[Math.Min(line.Length - 1, i + 1)];
                double distance = Distance(a, b);
                return new Pos((b.X - a.X) / Math.Max(1e-12, distance), (b.Y - a.Y) / Math.Max(1e-12, distance));
            }
            var tangents = new Pos[right.Length];
            for (int j = 0; j < right.Length; j++) tangents[j] = Tangent(right, j);
            var paired = new Pos[left.Length];
            paired[0] = right[0]; paired[paired.Length - 1] = right[right.Length - 1];
            for (int i = 1; i < left.Length - 1; i++)
            {
                Pos p = left[i], tangent = Tangent(left, i);
                double best = double.PositiveInfinity;
                for (int j = 0; j < right.Length - 1; j++)
                {
                    double DotAt(double t)
                    {
                        double x = right[j].X + t * (right[j + 1].X - right[j].X) - p.X;
                        double y = right[j].Y + t * (right[j + 1].Y - right[j].Y) - p.Y;
                        return x * (tangent.X + tangents[j].X + t * (tangents[j + 1].X - tangents[j].X))
                            + y * (tangent.Y + tangents[j].Y + t * (tangents[j + 1].Y - tangents[j].Y));
                    }
                    double f0 = DotAt(0), f1 = DotAt(1);
                    if (f0 * f1 > 0) continue;
                    double low = 0, high = 1;
                    for (int k = 0; k < 25; k++)
                    {
                        double middle = (low + high) / 2;
                        if (f0 * DotAt(middle) <= 0) high = middle;
                        else low = middle;
                    }
                    double amount = (low + high) / 2;
                    Pos candidate = new Pos(right[j].X + amount * (right[j + 1].X - right[j].X),
                        right[j].Y + amount * (right[j + 1].Y - right[j].Y));
                    double distance = Distance(p, candidate);
                    if (distance < best) { best = distance; paired[i] = candidate; }
                }
                if (paired[i] == null) paired[i] = Closest(right, p);
            }
            return paired;
        }

        internal static Pos[] Resample(IReadOnlyList<Pos> line, int count)
        {
            var cumulative = new double[line.Count];
            for (int i = 1; i < line.Count; i++) cumulative[i] = cumulative[i - 1] + Distance(line[i - 1], line[i]);
            var result = new Pos[count]; int segment = 1;
            for (int i = 0; i < count; i++)
            {
                double target = cumulative[cumulative.Length - 1] * i / (count - 1);
                while (segment < line.Count - 1 && cumulative[segment] < target) segment++;
                double span = cumulative[segment] - cumulative[segment - 1];
                double t = span <= 1e-20 ? 0 : (target - cumulative[segment - 1]) / span;
                result[i] = new Pos(line[segment - 1].X + t * (line[segment].X - line[segment - 1].X),
                    line[segment - 1].Y + t * (line[segment].Y - line[segment - 1].Y));
            }
            return result;
        }

        internal static double Distance(Pos a, Pos b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        private static double Norm(double[] values) { double sum = 0; foreach (double v in values) sum += v * v; return sum; }

        private static Pos Map(double[] h, Pos p)
        {
            double d = h[6] * p.X + h[7] * p.Y + h[8];
            return new Pos((h[0] * p.X + h[1] * p.Y + h[2]) / d, (h[3] * p.X + h[4] * p.Y + h[5]) / d);
        }

        private static double[] SquareToQuad(Pos[] p)
        {
            double dx1 = p[1].X - p[2].X, dx2 = p[3].X - p[2].X, dx3 = p[0].X - p[1].X + p[2].X - p[3].X;
            double dy1 = p[1].Y - p[2].Y, dy2 = p[3].Y - p[2].Y, dy3 = p[0].Y - p[1].Y + p[2].Y - p[3].Y;
            double det = dx1 * dy2 - dx2 * dy1;
            if (Math.Abs(det) < 1e-12) return null;
            double g = (dx3 * dy2 - dx2 * dy3) / det, h = (dx1 * dy3 - dx3 * dy1) / det;
            return new[] { p[1].X - p[0].X + g * p[1].X, p[3].X - p[0].X + h * p[3].X, p[0].X,
                p[1].Y - p[0].Y + g * p[1].Y, p[3].Y - p[0].Y + h * p[3].Y, p[0].Y, g, h, 1.0 };
        }

        private static double[] Invert(double[] m)
        {
            double a = m[0], b = m[1], c = m[2], d = m[3], e = m[4], f = m[5], g = m[6], h = m[7], i = m[8];
            double det = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
            if (Math.Abs(det) < 1e-16) return null;
            return new[] { (e*i-f*h)/det,(c*h-b*i)/det,(b*f-c*e)/det,(f*g-d*i)/det,(a*i-c*g)/det,(c*d-a*f)/det,
                (d*h-e*g)/det,(b*g-a*h)/det,(a*e-b*d)/det };
        }

        private static double[] Solve(double[,] matrix, double[] rhs)
        {
            int n = rhs.Length;
            for (int j = 0; j < n; j++)
            {
                int pivot = j;
                for (int i = j + 1; i < n; i++) if (Math.Abs(matrix[i, j]) > Math.Abs(matrix[pivot, j])) pivot = i;
                if (Math.Abs(matrix[pivot, j]) < 1e-18) return null;
                for (int k = j; k < n; k++) (matrix[j, k], matrix[pivot, k]) = (matrix[pivot, k], matrix[j, k]);
                (rhs[j], rhs[pivot]) = (rhs[pivot], rhs[j]);
                double diagonal = matrix[j, j];
                for (int k = j; k < n; k++) matrix[j, k] /= diagonal;
                rhs[j] /= diagonal;
                for (int i = 0; i < n; i++) if (i != j)
                {
                    double multiplier = matrix[i, j];
                    for (int k = j; k < n; k++) matrix[i, k] -= multiplier * matrix[j, k];
                    rhs[i] -= multiplier * rhs[j];
                }
            }
            return rhs;
        }
    }
}

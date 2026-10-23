using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace PaveMetric
{
    [Serializable]
    public class SurfaceError
    {
        [XmlIgnore]
        public int ErrorCode { get; set; }

        /// <summary>
        /// XML form of <see cref="ErrorCode"/>. Written as a number; older projects stored the
        /// <see cref="ErrorCodes"/> enum name, which is still accepted on load.
        /// </summary>
        [XmlElement("ErrorCode")]
        public string ErrorCodeXml
        {
            get => ErrorCode.ToString();
            set
            {
                if (int.TryParse(value, out var code))
                    ErrorCode = code;
                else if (Enum.TryParse<ErrorCodes>(value, out var legacy))
                    ErrorCode = (int)legacy;
            }
        }

        public double StartSection { get; set; }
        public double EndSection { get; set; }
        public double Left { get; set; }
        public double Right { get; set; }

        /// <summary>Photo-pixel boundary retained when changing coordinate models.</summary>
        public List<Pos> ImageOutline { get; set; }
        public bool ShouldSerializeImageOutline() => ImageOutline?.Count >= 3;

        public Pos[] GetImageOutline(PerspectiveCorrection correction, double section)
        {
            if (ImageOutline?.Count >= 3)
                return ImageOutline.Select(p => new Pos(p.X, p.Y)).ToArray();
            return correction.GetScreenAreaPolygon(Left + correction.PavementWidth / 2,
                Right + correction.PavementWidth / 2, StartSection - section, EndSection - section);
        }

        public double GetArea(PerspectiveCorrection correction, double section) =>
            ImageOutline?.Count >= 3 ? correction.GetImagePolygonArea(ImageOutline)
                : correction.GetRoadArea(Left + correction.PavementWidth / 2,
                    Right + correction.PavementWidth / 2, StartSection - section, EndSection - section);

        public bool Contains(PerspectiveCorrection correction, double section, Pos imagePoint)
        {
            Pos[] polygon = GetImageOutline(correction, section);
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Pos a = polygon[i], b = polygon[j];
                if ((a.Y > imagePoint.Y) != (b.Y > imagePoint.Y)
                    && imagePoint.X < (b.X - a.X) * (imagePoint.Y - a.Y) / (b.Y - a.Y) + a.X)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>Apply the handle's road-coordinate affine change to the preserved shape.</summary>
        public void TransformOutlineFrom(SurfaceError before, PerspectiveCorrection correction, double section)
        {
            if (before?.ImageOutline == null || before.ImageOutline.Count < 3) return;
            double oldWidth = before.Right - before.Left, oldLength = before.EndSection - before.StartSection;
            if (oldWidth <= 0 || oldLength <= 0) return;
            double halfWidth = correction.PavementWidth / 2;
            ImageOutline = before.ImageOutline.Select(point =>
            {
                Pos real = correction.GetRealPosition(point);
                double x = Left + (real.X - halfWidth - before.Left) / oldWidth * (Right - Left);
                double y = StartSection + (real.Y + section - before.StartSection) / oldLength * (EndSection - StartSection);
                return correction.GetScreenPosition(new Pos(x + halfWidth, y - section));
            }).ToList();
        }

        public SurfaceError Clone()
        {
            return new SurfaceError
            {
                ErrorCode = ErrorCode,
                StartSection = StartSection,
                EndSection = EndSection,
                Left = Left,
                Right = Right,
                ImageOutline = ImageOutline?.Select(p => new Pos(p.X, p.Y)).ToList()
            };
        }

        public void CopyBoundsFrom(SurfaceError source)
        {
            StartSection = source.StartSection;
            EndSection = source.EndSection;
            Left = source.Left;
            Right = source.Right;
            ImageOutline = source.ImageOutline?.Select(p => new Pos(p.X, p.Y)).ToList();
        }
    }
}

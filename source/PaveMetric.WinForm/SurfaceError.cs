using System;
using System.Xml.Serialization;

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

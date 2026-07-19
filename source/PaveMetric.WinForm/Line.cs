using System;
using System.Drawing;
using System.Xml.Serialization;

namespace PaveMetric
{
    [Serializable]
    public class Pos
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Pos()
        {
            X = 0.0;
            Y = 0.0;
        }

        public Pos(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    [Serializable]
    public class Line
    {
        public Pos P0 { get; set; }
        public Pos P1 { get; set; }

        
        [XmlIgnore]
        public Color LineColor { get; set; }

        public string LineColorString
        {
            get => LineColor.ToKnownColor().ToString();
            set => LineColor = Color.FromName(value);
        }

        public double LineWidth { get; set; }

        public double DeltaX => P1.X - P0.X;

        public double DeltaY => P1.Y - P0.Y;

        public Line()
        {
            P0 = new Pos();
            P1 = new Pos();
            LineColor = Color.Black;
            LineWidth = 1.0;
        }
    }
}
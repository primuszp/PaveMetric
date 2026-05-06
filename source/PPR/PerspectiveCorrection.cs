using System;
using System.Collections.Generic;
using System.Drawing;

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

            double nearPixelWidth = RightEdge.P0.X - LeftEdge.P0.X;
            double farPixelWidth = RightEdge.P1.X - LeftEdge.P1.X;
            double pixelLength = Math.Abs(LeftEdge.DeltaY);
            double netDeltaX = PavementWidth / (double)nCols;

            for (int i = 1; i < nCols; i++)
            {
                double x = i * netDeltaX;

                Line newLine = new Line();
                newLine.LineWidth = 1.5;
                newLine.LineColor = Color.FromArgb(128, 255, 255, 255);
                newLine.P0.X = LeftEdge.P0.X + x * nearPixelWidth / PavementWidth;
                newLine.P0.Y = NearDistance;
                newLine.P1.X = LeftEdge.P1.X + x * farPixelWidth / PavementWidth;
                newLine.P1.Y = FarDistance;

                lines.Add(newLine);
            }

            double ctga = Math.Abs(LeftEdge.DeltaX / LeftEdge.DeltaY);
            double ctgb = Math.Abs(RightEdge.DeltaX / RightEdge.DeltaY);
            double c = (farPixelWidth / PavementWidth) / (pixelLength / Length);
            double netDeltaY = Length / (double)nRows;

            for (int i = 1; i < nRows; i++)
            {
                double y = i * netDeltaY;
                double l = nearPixelWidth / (c * PavementWidth / y + ctga + ctgb);

                Line newLine = new Line();
                newLine.LineWidth = 1.5;
                newLine.LineColor = Color.FromArgb(128, 255, 255, 255);
                newLine.P0.X = LeftEdge.P0.X + l * ctga;
                newLine.P1.X = RightEdge.P0.X - l * ctgb;

                newLine.P0.Y = NearDistance - l;
                newLine.P1.Y = NearDistance - l;

                lines.Add(newLine);
            }

            return lines.ToArray();
        }

        public void Normalize(Line[] lines, double length, double width)
        {
            Line farLine = lines[0];
            Line nearLine = lines[1];
            Line leftLine = lines[2];
            Line rightLine = lines[3];

            double nearDistance = nearLine.P0.Y;
            double farDistance = farLine.P0.Y;
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
            Pos realPos = new Pos();

            double dxFar = RightEdge.P1.X - LeftEdge.P1.X;
            double dxNear = RightEdge.P0.X - LeftEdge.P0.X;
            double dySection = NearDistance - FarDistance;
            double c = (dxFar * Length) / (PavementWidth * dySection);

            double dy = NearDistance - ScreenPosition.Y;
            double dxLeft = LeftEdge.DeltaX * dy / dySection;
            double dxRight = RightEdge.DeltaX * dy / dySection;
            double dx = dxNear - dxLeft + dxRight;

            double dxx = ScreenPosition.X - (LeftEdge.P0.X + dxLeft);
            realPos.X = PavementWidth * dxx / dx;
            realPos.Y = c * dy * PavementWidth / dx;

            return realPos;
        }

        public Pos GetScreenPosition(Pos RealPosition)
        {
            Pos screenPos = new Pos();

            double dxFar = RightEdge.P1.X - LeftEdge.P1.X;
            double dxNear = RightEdge.P0.X - LeftEdge.P0.X;
            double dySection = NearDistance - FarDistance;

            double ctga = Math.Abs(LeftEdge.DeltaX / LeftEdge.DeltaY);
            double ctgb = Math.Abs(RightEdge.DeltaX / RightEdge.DeltaY);
            double c = (dxFar / PavementWidth) / (dySection / Length);

            double dy = dxNear / (c * PavementWidth / RealPosition.Y + ctga + ctgb);
            screenPos.Y = NearDistance - dy;

            double dxLeft = LeftEdge.DeltaX * dy / dySection;
            double dxRight = RightEdge.DeltaX * dy / dySection;
            double dx = dxNear - dxLeft + dxRight;

            double dxx = dx * RealPosition.X / PavementWidth;
            screenPos.X = LeftEdge.P0.X + dxLeft + dxx;

            return screenPos;
        }
    }
}

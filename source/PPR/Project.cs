using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace PPR
{
    [Serializable]
    public class Project
    {
        [XmlIgnore] public string ProjectPath { get; set; }
        [XmlIgnore] public string ProjectFileName { get; set; }

        List<PavementPhoto> _photos = new List<PavementPhoto>();

        [XmlIgnore]
        public List<ErrorLayerControl> ErrorLayers = new List<ErrorLayerControl>();

        public List<PavementPhoto> Photos => _photos;

        int _actualPhotoIndex = -1;

        public PavementPhoto ActualPhoto
        {
            get
            {
                if (_actualPhotoIndex == -1 || _photos.Count == 0) return null;
                return _photos[_actualPhotoIndex];
            }
        }

        public void MoveToFirstPhoto()
        {
            if (_photos.Count == 0) _actualPhotoIndex = -1;
            _actualPhotoIndex = 0;
        }

        public void MoveToLastPhoto()
        {
            if (_photos.Count == 0) _actualPhotoIndex = -1;
            _actualPhotoIndex = _photos.Count - 1;
        }

        public void MoveToNextPhoto()
        {
            if (_photos.Count == 0)
            {
                _actualPhotoIndex = -1;
                return;
            }

            if (_actualPhotoIndex == -1) _actualPhotoIndex = 0;
            if (_actualPhotoIndex < _photos.Count - 1) _actualPhotoIndex++;
        }

        public void MoveToPreviousPhoto()
        {
            if (_photos.Count == 0)
            {
                _actualPhotoIndex = -1;
                return;
            }

            if (_actualPhotoIndex == -1) _actualPhotoIndex = 0;
            if (_actualPhotoIndex > 0) _actualPhotoIndex--;
        }

        public bool MoveToPhotoAt(int index)
        {
            if (index < 0 || index > _photos.Count - 1) return false;

            _actualPhotoIndex = index;
            return true;
        }

        public void AddPhoto(string photoFileName)
        {
            var newPhoto = new PavementPhoto();
            _photos.Add(newPhoto);
            newPhoto.PhotoFileName = photoFileName;

            var sectionString = photoFileName.Replace("+", "");
            double.TryParse(sectionString, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var section);
            newPhoto.Section = section;
        }

        public void LoadPhotos(string path)
        {
            _photos.Clear();
            _actualPhotoIndex = -1;

            if (!Directory.Exists(path)) return;

            ProjectPath = path;
            var files = Directory.GetFiles(path, "*.jpg");
            foreach (var fileName in files)
            {
                var file = Path.GetFileNameWithoutExtension(fileName);
                AddPhoto(file);
            }

            MoveToFirstPhoto();
        }

        private class LoadedError
        {
            public int ErrorCode = 1;
            public int SideCode = 1;
            public int Section = 0;
            public double Width = 0.0;
            public double Length = 0.0;
        }

        public void ImportFRODO(string fileName)
        {
            var invariant = System.Globalization.CultureInfo.InvariantCulture;
            var myErrorCode = 5;
            using var reader = new StreamReader(fileName);
            var loadedErrors = new List<LoadedError>();

            string line;

            while ((line = reader.ReadLine()) != null)
            {
                var words = GetWords(line);
                var newError = new LoadedError
                {
                    Section = Convert.ToInt32(words[0], invariant),
                    ErrorCode = Convert.ToInt32(words[1], invariant),
                    SideCode = Convert.ToInt32(words[2], invariant),
                    Width = Convert.ToDouble(words[3], invariant),
                    Length = Convert.ToDouble(words[4], invariant)
                };
                if (newError.ErrorCode == myErrorCode)
                    loadedErrors.Add(newError);
            }

            var errors = new List<LoadedError>[3];
            errors[0] = new List<LoadedError>();
            errors[1] = new List<LoadedError>();
            errors[2] = new List<LoadedError>();
            var errorStarted = new bool[3];

            for (var i = 0; i < 3; i++) errorStarted[i] = false;

            foreach (var myError in loadedErrors)
            {
                var side = myError.SideCode;

                if (errorStarted[side])
                {
                    var n = errors[side].Count - 1;
                    var lastError = errors[side][n];
                    lastError.Length = myError.Section - lastError.Section;
                    errorStarted[side] = false;
                }
                else
                {
                    var nError = new LoadedError
                    {
                        Section = myError.Section,
                        ErrorCode = myError.ErrorCode,
                        SideCode = side
                    };
                    errors[side].Add(nError);
                    errorStarted[side] = true;
                }
            }

            for (var side = 0; side < 3; side++)
            {
                foreach (var myError in errors[side])
                {
                    var startSection = myError.Section;
                    var endSection = startSection + (int)myError.Length;

                    foreach (var myPhoto in _photos)
                    {
                        if (myPhoto.Section <= endSection &&
                            myPhoto.Section + myPhoto.PerspectiveCorrection.Length >= startSection)
                        {
                            double photoErrorStart = 0;
                            var photoErrorEnd = myPhoto.PerspectiveCorrection.Length;
                            if (startSection > myPhoto.Section)
                            {
                                photoErrorStart = startSection - myPhoto.Section;
                            }

                            if (endSection < myPhoto.Section + myPhoto.PerspectiveCorrection.Length)
                            {
                                photoErrorEnd = myPhoto.PerspectiveCorrection.Length -
                                                (myPhoto.Section + myPhoto.PerspectiveCorrection.Length - endSection);
                            }

                            if (photoErrorEnd > photoErrorStart)
                            {
                                var left = 0.0;
                                var right = 3.0;

                                if (fileName.Contains("Left"))
                                {
                                    switch (side)
                                    {
                                        case 0:
                                            left = -1.0;
                                            right = 0.0;
                                            break;
                                        case 1:
                                            left = -3.0;
                                            right = -2.0;
                                            break;
                                        case 2:
                                            left = -2.0;
                                            right = -1.0;
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (side)
                                    {
                                        case 0:
                                            left = 0.0;
                                            right = 1.0;
                                            break;
                                        case 1:
                                            left = 2.0;
                                            right = 3.0;
                                            break;
                                        case 2:
                                            left = 1.0;
                                            right = 2.0;
                                            break;
                                    }
                                }

                                var newSurfaceError = new SurfaceError
                                {
                                    StartSection = myPhoto.Section + photoErrorStart,
                                    EndSection = myPhoto.Section + photoErrorEnd,
                                    Left = left,
                                    Right = right
                                };

                                switch (myErrorCode)
                                {
                                    case 5:
                                        newSurfaceError.ErrorCode = ErrorCodes.MapCrack;
                                        break;
                                    case 6:
                                        newSurfaceError.ErrorCode = ErrorCodes.AlligatorCrack;
                                        break;
                                }

                                myPhoto.Errors.Add(newSurfaceError);
                            }
                        }
                    }
                }
            }
        }

        public static List<string> GetWords(string line)
        {
            var values = new List<string>();

            line += "\t";
            var word = "";
            var lastCharWasSeparator = false;

            for (var i = 0; i < line.Length; i++)
            {
                switch (line[i])
                {
                    case '\t':
                    case ' ':
                    case '=':
                        if (!lastCharWasSeparator)
                        {
                            values.Add(word);
                            word = "";
                        }
                        lastCharWasSeparator = true;
                        break;
                    default:
                        lastCharWasSeparator = false;
                        word += line[i];
                        break;
                }
            }

            return values;
        }

        public void ExportErrors(string fileName)
        {
            var index = 0;
            using var writer = new StreamWriter(fileName, false, Encoding.Default);

            writer.Write("Start\tEnd\t");
            for (var i = 10; i <= 17; i++)
            {
                writer.Write((ErrorCodes)i);
                if (i < 17) writer.Write('\t');
                else writer.WriteLine();
            }

            foreach (var myPhoto in _photos)
            {
                var pavementArea = myPhoto.PerspectiveCorrection.PavementWidth * myPhoto.PerspectiveCorrection.Length;
                var correctedPavementArea = pavementArea;

                if (index > 0)
                {
                    var width1 = _photos[index - 1].PerspectiveCorrection.PavementWidth;
                    var width2 = myPhoto.PerspectiveCorrection.PavementWidth;
                    correctedPavementArea = 0.5 * (width1 + width2) * myPhoto.PerspectiveCorrection.Length;
                }

                var errorAreaList = new double[18];
                var errorAreaPercentList = new double[18];

                foreach (var myError in myPhoto.Errors)
                {
                    var errorCode = (int)myError.ErrorCode;
                    var errorArea = (myError.Right - myError.Left) * (myError.EndSection - myError.StartSection);
                    double errorAreaPercent = errorArea / pavementArea;
                    double correctedErrorArea = correctedPavementArea * errorAreaPercent;

                    errorAreaList[errorCode] += correctedErrorArea;
                    errorAreaPercentList[errorCode] += errorAreaPercent;
                }

                writer.Write(myPhoto.Section.ToString("0.00") + '\t' + (myPhoto.Section + myPhoto.PerspectiveCorrection.Length).ToString("0.00") + '\t');

                for (var i = 10; i <= 17; i++)
                {
                    writer.Write(errorAreaList[i].ToString("0"));
                    if (i < 17) writer.Write('\t');
                    else writer.WriteLine();
                }
                index++;
            }
            writer.Close();
        }

        public void ExportTechnology(string fileName)
        {
            using var writer = new StreamWriter(fileName, false, Encoding.Default);

            for (int i = 0; i < _photos.Count; i++)
            {
                var pavementArea = _photos[i].PerspectiveCorrection.PavementWidth * _photos[i].PerspectiveCorrection.Length;
                var correctedPavementArea = pavementArea;

                if (i > 0)
                {
                    var width0 = _photos[i + 0].PerspectiveCorrection.PavementWidth;
                    var width1 = _photos[i - 1].PerspectiveCorrection.PavementWidth;
                    correctedPavementArea = 0.5 * (width0 + width1) * _photos[i].PerspectiveCorrection.Length;
                }

                var errorAreaPercentList = new double[18];

                foreach (var error in _photos[i].Errors)
                {
                    var errorCode = (int)error.ErrorCode;
                    var errorArea = (error.Right - error.Left) * (error.EndSection - error.StartSection);

                    double errorAreaPercent = errorArea / pavementArea;
                    double correctedErrorArea = correctedPavementArea * errorAreaPercent;

                    errorAreaPercentList[errorCode] += errorAreaPercent;
                }

                writer.Write(_photos[i].Section.ToString("0.00") + '\t' + (_photos[i].Section +
                    _photos[i].PerspectiveCorrection.Length).ToString("0.00") + '\t');

                // Technology decision

                var errorAC = errorAreaPercentList[(int)ErrorCodes.AlligatorCrack];
                var errorFP = errorAreaPercentList[(int)ErrorCodes.FilledPothole];
                var errorMC = errorAreaPercentList[(int)ErrorCodes.MapCrack];
                var errorPH = errorAreaPercentList[(int)ErrorCodes.Pothole];

                var technoDecisionList = new double[4];

                if (errorAC == 0.0)
                {
                    if (errorMC + errorFP + errorPH >= 0.45)
                    {
                        // Felületi bevonás
                        technoDecisionList[0] = 0.5;
                    }
                    else
                    {
                        technoDecisionList[0] = 0;
                    }
                }
                else
                {
                    if (errorAC + errorFP + errorMC + errorPH >= 0.50)
                    {
                        // Aszfaltszőnyeg
                        technoDecisionList[1] = 1;
                    }
                    else
                    {
                        technoDecisionList[1] = 0;
                    }
                }

                if(errorPH > 0)
                {
                    // Kátyúzás
                    technoDecisionList[2] = 1.5;
                }

                // Terület
                technoDecisionList[3] = correctedPavementArea;

                for (var j = 0; j < technoDecisionList.Length; j++)
                {
                    writer.Write(technoDecisionList[j].ToString("0.00"));
                    if (j < technoDecisionList.Length - 1) writer.Write('\t');
                    else writer.WriteLine();
                }
            }
            writer.Close();
        }

        private void DrawLine(Line line, Graphics graphics)
        {
            using var pen = new Pen(line.LineColor, (float)line.LineWidth);

            var x1 = (float)line.P0.X;
            var y1 = (float)line.P0.Y;
            var x2 = (float)line.P1.X;
            var y2 = (float)line.P1.Y;

            graphics.DrawLine(pen, x1, y1, x2, y2);
        }

        private void DrawLines(PavementPhoto photo, Graphics graphics)
        {
            var pc = photo.PerspectiveCorrection;
            var lines = pc.GetNet().ToList();
            lines.AddRange(pc.GetScreenPolyline(0.0, pc.Length, pc.PavementWidth, pc.Length, Color.LightGreen, 4.0));
            lines.AddRange(pc.GetScreenPolyline(0.0, 0.0, pc.PavementWidth, 0.0, Color.LightGreen, 4.0));
            lines.AddRange(pc.GetScreenPolyline(0.0, 0.0, 0.0, pc.Length, Color.Pink, 4.0));
            lines.AddRange(pc.GetScreenPolyline(pc.PavementWidth, 0.0, pc.PavementWidth, pc.Length, Color.Pink, 4.0));

            foreach (var line in lines)
            {
                DrawLine(line, graphics);
            }
        }

        public void RenderErrorsToBitmap()
        {
            foreach (var photo in Photos)
            {
                var pavementWidth2 = photo.PerspectiveCorrection.PavementWidth / 2.0;
                var startSection = photo.Section;

                var dinf = Directory.CreateDirectory(Path.Combine(ProjectPath, "Errors"));
                var file = Path.Combine(ProjectPath, photo.PhotoFileName + ".jpg");

                using var bmp = new Bitmap(file);
                using var graphics = Graphics.FromImage(bmp);
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                DrawLines(photo, graphics);
                var hasVisibleErrors = false;

                foreach (var myError in photo.Errors)
                {
                    var leftReal = myError.Left + pavementWidth2;
                    var rightReal = myError.Right + pavementWidth2;
                    var nearReal = myError.StartSection - startSection;
                    var farReal = myError.EndSection - startSection;

                    var screenPositions = photo.PerspectiveCorrection.GetScreenAreaPolygon(leftReal, rightReal, nearReal, farReal);

                    var points = new Point[screenPositions.Length];

                    for (var i = 0; i < screenPositions.Length; i++)
                    {
                        points[i].X = (int)screenPositions[i].X;
                        points[i].Y = (int)screenPositions[i].Y;
                    }

                    var areaColor = Color.FromArgb(100, 128, 128, 128);
                    var on = false;

                    foreach (var myLayer in ErrorLayers)
                    {
                        if (myLayer.ErrorCode == myError.ErrorCode)
                        {
                            areaColor = Color.FromArgb(128, myLayer.LayerColor.R, myLayer.LayerColor.G,
                                myLayer.LayerColor.B);
                            on = myLayer.IsVisible;
                            break;
                        }
                    }

                    if (on)
                    {
                        using var brush = new SolidBrush(areaColor);
                        graphics.FillPolygon(brush, points);
                        hasVisibleErrors = true;
                    }
                }

                if (hasVisibleErrors)
                    bmp.Save(Path.Combine(dinf.FullName, photo.PhotoFileName + ".jpg"), ImageFormat.Jpeg);
            }
        }
    }
}

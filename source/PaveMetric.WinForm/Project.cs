using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace PaveMetric
{
    [Serializable]
    public class Project
    {
        [XmlIgnore] public string ProjectPath { get; set; }
        [XmlIgnore] public string ProjectFileName { get; set; }

        List<PavementPhoto> _photos = new List<PavementPhoto>();

        [XmlIgnore]
        public List<ErrorLayerControl> ErrorLayers = new List<ErrorLayerControl>();

        /// <summary>
        /// Codes of the catalog types offered in this project's palette.
        /// Null (new project, or one saved before this setting existed) means every type.
        /// </summary>
        [XmlIgnore]
        public List<int> ErrorTypeCodes { get; set; }

        /// <summary>
        /// XML form of <see cref="ErrorTypeCodes"/>. An array (not the list) so that a missing
        /// element stays null ("all types") instead of deserializing as an empty selection.
        /// </summary>
        [XmlArray("ErrorTypeCodes")]
        public int[] ErrorTypeCodesXml
        {
            get => ErrorTypeCodes?.ToArray();
            set => ErrorTypeCodes = value?.ToList();
        }

        public bool IsErrorTypeEnabled(int code) => ErrorTypeCodes == null || ErrorTypeCodes.Contains(code);

        public void SetErrorTypeEnabled(int code, bool enabled)
        {
            if (ErrorTypeCodes == null)
            {
                if (enabled) return;
                // switching from "all" to an explicit list: start from the known types
                ErrorTypeCodes = ErrorLayers.Select(l => l.ErrorCode).Distinct().ToList();
            }
            if (enabled && !ErrorTypeCodes.Contains(code))
                ErrorTypeCodes.Add(code);
            else if (!enabled)
                ErrorTypeCodes.Remove(code);
        }

        /// <summary>Types that must stay in the palette: enabled, or already used by a recorded error.</summary>
        public bool IsErrorTypeShown(int code) =>
            IsErrorTypeEnabled(code) || _photos.Any(p => p.Errors.Any(e => e.ErrorCode == code));

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
                                        newSurfaceError.ErrorCode = (int)ErrorCodes.MapCrack;
                                        break;
                                    case 6:
                                        newSurfaceError.ErrorCode = (int)ErrorCodes.AlligatorCrack;
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

        /// <summary>Photos whose section length or pavement width is not set (area would be zero).</summary>
        public List<PavementPhoto> PhotosWithoutDimensions() =>
            _photos.Where(p => !HasDimensions(p)).ToList();

        static bool HasDimensions(PavementPhoto photo) =>
            photo.PerspectiveCorrection.PavementWidth > 0 && photo.PerspectiveCorrection.Length > 0;

        /// <summary>
        /// Pavement area of the photo, with the width averaged with the previous photo's when
        /// that one is usable. Returns 0 when the photo itself has no dimensions.
        /// </summary>
        double CorrectedPavementArea(int index)
        {
            var photo = _photos[index];
            if (!HasDimensions(photo))
                return 0.0;

            var width = photo.PerspectiveCorrection.PavementWidth;
            if (index > 0 && HasDimensions(_photos[index - 1]))
                width = 0.5 * (width + _photos[index - 1].PerspectiveCorrection.PavementWidth);
            return width * photo.PerspectiveCorrection.Length;
        }

        public void ExportErrors(string fileName)
        {
            var index = 0;
            using var writer = new StreamWriter(fileName, false, Encoding.Default);

            // one column per type selected for this project, plus any code that occurs in the data
            var codes = ErrorLayers.Select(l => l.ErrorCode).Where(IsErrorTypeEnabled)
                .Concat(_photos.SelectMany(p => p.Errors).Select(e => e.ErrorCode))
                .Distinct().OrderBy(c => c).ToList();

            writer.Write("Start\tEnd");
            foreach (var code in codes)
            {
                var layer = ErrorLayers.FirstOrDefault(l => l.ErrorCode == code);
                writer.Write('\t' + (layer != null ? $"{code} {layer.LayerName}" : code.ToString()));
            }
            writer.WriteLine();

            foreach (var myPhoto in _photos)
            {
                var pavementArea = myPhoto.PerspectiveCorrection.PavementWidth * myPhoto.PerspectiveCorrection.Length;
                var correctedPavementArea = CorrectedPavementArea(index);

                var errorAreaList = new Dictionary<int, double>();

                // without dimensions the error ratio is undefined: leave the row at zero
                foreach (var myError in HasDimensions(myPhoto) ? myPhoto.Errors : new List<SurfaceError>())
                {
                    var errorCode = myError.ErrorCode;
                    var errorArea = (myError.Right - myError.Left) * (myError.EndSection - myError.StartSection);
                    double errorAreaPercent = errorArea / pavementArea;
                    double correctedErrorArea = correctedPavementArea * errorAreaPercent;

                    errorAreaList[errorCode] = errorAreaList.GetValueOrDefault(errorCode) + correctedErrorArea;
                }

                writer.Write(myPhoto.Section.ToString("0.00") + '\t' + (myPhoto.Section + myPhoto.PerspectiveCorrection.Length).ToString("0.00"));

                foreach (var code in codes)
                    writer.Write('\t' + errorAreaList.GetValueOrDefault(code).ToString("0"));
                writer.WriteLine();
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
                var correctedPavementArea = CorrectedPavementArea(i);

                var errorAreaPercentList = new Dictionary<int, double>();

                // without dimensions the error ratio is undefined: no technology is proposed
                foreach (var error in HasDimensions(_photos[i]) ? _photos[i].Errors : new List<SurfaceError>())
                {
                    var errorCode = error.ErrorCode;
                    var errorArea = (error.Right - error.Left) * (error.EndSection - error.StartSection);

                    double errorAreaPercent = errorArea / pavementArea;
                    double correctedErrorArea = correctedPavementArea * errorAreaPercent;

                    errorAreaPercentList[errorCode] = errorAreaPercentList.GetValueOrDefault(errorCode) + errorAreaPercent;
                }

                writer.Write(_photos[i].Section.ToString("0.00") + '\t' + (_photos[i].Section +
                    _photos[i].PerspectiveCorrection.Length).ToString("0.00") + '\t');

                // Technology decision

                var errorAC = errorAreaPercentList.GetValueOrDefault((int)ErrorCodes.AlligatorCrack);
                var errorFP = errorAreaPercentList.GetValueOrDefault((int)ErrorCodes.FilledPothole);
                var errorMC = errorAreaPercentList.GetValueOrDefault((int)ErrorCodes.MapCrack);
                var errorPH = errorAreaPercentList.GetValueOrDefault((int)ErrorCodes.Pothole);

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static chess.BoardOcr;
using Tesseract;
using OpenCvSharp;

namespace chess
{
#if TESSERACT_OCR_DID_NOT_WORK
    public static class PixConverter
    {
        public static Pix ToPix(Mat mat)
        {
            using (var ms = new MemoryStream())
            {
                // Encode the Mat to a memory stream
                mat.WriteToStream(ms);
                // Load the Pix from the memory stream
                return Pix.LoadFromMemory(ms.ToArray());
            }
        }

        public static void WriteToStream(this Mat mat, Stream stream)
        {
            // Encode the Mat to a PNG format in memory
            Cv2.ImEncode(".jpg", mat, out var imgBytes);
            // Write the bytes to the stream
            stream.Write(imgBytes, 0, imgBytes.Length);
        }
    }

    internal class GameNumOcr
    {

        static int ExtractPageNumber(string filePath)
        {
            // Regular expression to match the pattern "page_<number>.png"
            string pattern = @"page_(\d+)_(.*)\.png";
            Match match = Regex.Match(filePath, pattern);

            if (match.Success && int.TryParse(match.Groups[1].Value, out int pageNumber))
            {
                return pageNumber;
            }

            return -1; // Return -1 if the page number could not be extracted
        }

        public static void RunOcr()
        {

            string inputDirectory = "c:\\github\\chess\\tiny";
            string fenDir = "c:\\github\\chess\\fen\\";

            var engine = new TesseractEngine("c:\\temp\\tessdata", "eng", EngineMode.Default);
            engine.SetVariable("tessedit_char_whitelist", "0123456789"); // Only allow digits
            engine.SetVariable("tessedit_single_characters", true);

            try
            {
                using (FileStream fs = new FileStream(fenDir, FileMode.Open))
                {
                    fs.SetLength(0);
                }
            }
            catch (Exception ex) { }

            const string EMPTY_FEN = "8/8/8/8/8/8/8/8";

            string[] files = Directory.GetFiles(inputDirectory, "page_*.png");
            var sorted = files.OrderBy(f => ExtractPageNumber(f)).ToArray();
            int last = 0;
            foreach (string file in sorted)
            {
                
                var nt = GetBoardNumberAndWhoToMove(file, engine, last);
                if (nt.Number >= 1 && nt.Number <= 4462)
                {
                    string fen = DoOcrOnPage(file);
                }
                last = nt.Number;
            }
        }

        public record NumberAndType
        {
            public int Number;
            public string MateIn;
        }

        

        public static NumberAndType GetBoardNumberAndWhoToMove(string imagePath, TesseractEngine engine, int last)
        {

            NumberAndType ret = new NumberAndType();
            string pattern = @"page_(\d+)_part_(\d)_(\d)\.png";
            Match match = Regex.Match(imagePath, pattern);
            if (match.Success)
            {
                int baseProblemNum; ;
                int page = int.Parse(match.Groups[1].Value);
                int col = int.Parse(match.Groups[2].Value);
                int row = int.Parse(match.Groups[3].Value);

                const int SKIP_PAGES = 7;
                if (page >= 16 && page <= 66)
                {
                    ret.MateIn = "Mate in 1";
                    baseProblemNum = 1;

                    // Load and preprocess the image
                    // Load and preprocess the image
                    Mat src = Cv2.ImRead(imagePath, ImreadModes.Grayscale);
                    Cv2.Threshold(src, src, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
                    Cv2.ImWrite("c:\\github\\chess\\processed_image.jpg", src);

                    // Convert the cropped Mat to a Pix format compatible with Tesseract
                    var jpg = Pix.LoadFromFile("c:\\github\\chess\\processed_image.jpg");
                    var roi2 = new Tesseract.Rect(50, 2, 150, 21);

                    // OCR seems more accurate 
                    using (var processedImage = engine.Process(jpg, roi2, PageSegMode.SingleBlock))
                    {
                        var text = processedImage.GetText();
                        if (text != "")
                        {
                            Console.WriteLine("Recognized Text: " + text);
                            var newNum = int.Parse(text.Trim());
                            if( newNum != last + 1)
                            {
                                // Unexpected
                            }
                            if( last > 5000)
                            {

                            }
                            ret.Number = last+1;
                        }
                    }

                }
                return ret;
            }
            else
            {
                throw new Exception("Could not parse file");
            }

        }
    }
#endif
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace chess
{
    internal class BoardOcr
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

        public record NumberAndType {
            public int Number;
            public string MateIn;
        }

        public static NumberAndType GetProblemType(int problemNumber)
        {
            NumberAndType ret = new NumberAndType();
            ret.Number = problemNumber;

            if (problemNumber <= 306)
            {
                ret.MateIn = "#1";
            }
            else if (problemNumber <= 3718)
            {
                ret.MateIn = "#2";
            }
            else if (problemNumber <= 4462)
            {
                ret.MateIn = "Combinations #3";
            }
            else if (problemNumber <= 4562)
            {
                ret.MateIn = "f3 (f6) combinations";
            }
            else if (problemNumber <= 4762) // This number wrong in TOC
            {
                ret.MateIn = "f3 (f6) combinations"; // TITLE??? TODO
            }
            else if (problemNumber <= 4862)
            {
                ret.MateIn = "f2 (f7) Combinations";
            }
            else if (problemNumber <= 4962)
            {
                ret.MateIn = "g2 (g7) combinations";
            }
            else if (problemNumber <= 5062)
            {
                ret.MateIn = "h2 (h7) combinations";
            }
            else if (problemNumber <= 5104)
            {
                ret.MateIn = "Simple endgames - white draws";
            }
            else if (problemNumber <= 5206)
            {
                ret.MateIn = "Simple endgames - white wins";
            }
            else if (problemNumber <= 5334)
            {
                ret.MateIn = "Polgar sisters combinations";
            }
            else
            {
                throw new Exception("Unknown game number");
            }
            return ret;

        }
    
        public static void RunOcr()
        {

            string inputDirectory = "c:\\github\\chess\\tiny";
            string fenFolder = "c:\\github\\chess\\fen\\";
            string pgnFile = fenFolder + "\\5334.pgn";

            try
            {
                using (FileStream fs = new FileStream(pgnFile, FileMode.Open))
                {
                    fs.SetLength(0);
                }
            }
            catch (Exception ex) { }

            const string EMPTY_FEN = "8/8/8/8/8/8/8/8 w";

            // Load piece templates
            Dictionary<string, Mat> digitTemplates = LoadDigitTemplates();

            Dictionary<string, Mat> blackToMoveTemplates = LoadBlackToMoveTemplates();

            // Load piece templates
            Dictionary<string, Mat> pieceTemplates = LoadPieceTemplates();





            string[] files = Directory.GetFiles(inputDirectory, "page_*.png");
            var sorted = files.OrderBy(f => ExtractPageNumber(f)).ToArray();
            int num = 0;
            foreach (string file in sorted)
            {
                int problemNumber = DoProblemNumberOcrOnPage(file, digitTemplates);


                var nt = GetProblemType(problemNumber);
                if (nt.Number >= 1 && nt.Number <= 5334)
                {
                    string fen = DoOcrOnPage(file, pieceTemplates, blackToMoveTemplates);

                    if ( fen == EMPTY_FEN)
                    {
                        // SKIP
                        continue;
                    }
                    num = num + 1;
                    if(problemNumber != num)
                    {
                        // Compare OCR to the count. Should be same.
                        // TODO: OCR of problem number probably un-necessary.
                        nt.Number = problemNumber;
                    }
                    
                    AppendToFile(pgnFile, fen, nt);

                    // I want a copy of the images, numbered, in the fen folder with the PGN.
                    File.Copy(file, fenFolder + "/p" + nt.Number + ".png", true);
                }
            }
        }

        /*
        * 
        * [Event "Example Position"]
            [FEN "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"]
            [Annotator "Page 123 - Mate in 1"]                    */
        static void AppendToFile(string filePath, string fen, NumberAndType nt)
        {
            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.WriteLine($"[Event \"Polgar 5334\"]");
                sw.WriteLine($"[FEN \"{fen}\"]");
                sw.WriteLine($"[Annotator \"Problem {nt.Number} - {nt.MateIn}\"]");
                sw.WriteLine();
            }
        }

        public static int DoProblemNumberOcrOnPage( string imagePath, Dictionary<string, Mat> digitTemplates)
        {
            string ret = "";

            // Load the image
            Mat image = Cv2.ImRead(imagePath, ImreadModes.Grayscale);

            // Calculate dimensions
            int digitWidth = 14;
            int digitHeight = 20;
            int maxDigits = 4;

            // Initialize an empty board


            // Identify pieces in each cell
            for (int i = 0; i < maxDigits; i++)
            {
                // Digits are right justified.
                Mat cell = new Mat(image, new Rect(88 - ((i+1) * digitWidth) - 5, 2, digitWidth+3, digitHeight + 6));

                // TEST ONLY.
                // Cv2.ImWrite("c:\\github\\chess\\digit.png", cell);

                string digitstring = IdentifyDigit(cell, digitTemplates);
                if(digitstring == "")
                {
                    // Starting right -> left, so when we hit a space, we are done.
                    break;
                }
                ret = digitstring + ret;

            }

            // Convert problem number
            return int.Parse(ret);
        }

        public static string DoOcrOnPage( string imagePath, Dictionary<string, Mat> pieceTemplates, Dictionary<string,Mat> blackToMoveTemplates) { 

            // Load the image
            Mat image = Cv2.ImRead(imagePath, ImreadModes.Grayscale);

            // Calculate dimensions
            int boardSize = 8;
            int cellWidth = 30;
            int cellHeight = 30;

            // Initialize an empty board
            string[,] board = new string[boardSize, boardSize];

            // Identify pieces in each cell
            for (int i = 0; i < boardSize; i++)
            {
                for (int j = 0; j < boardSize; j++)
                {
                    // Find roughly where each square is supposed to be an look for piece.
                    Mat cell = new Mat(image, new Rect(20 + (j * cellWidth)-4, 29 + (i * cellHeight)-4, cellWidth+8, cellHeight+8));
                    bool isLightSquare = (i + j) % 2 == 0;
                    string piece = IdentifyPiece(cell, pieceTemplates, isLightSquare);
                    board[i, j] = piece;
                }
            }

            // Convert board to FEN string
            string fenString = ConvertBoardToFEN(board);


            Mat btmCell = new Mat(image, new Rect(261, 36, 17, 33));
            bool blackToMove = IdentifyBlackToMove(btmCell, blackToMoveTemplates);


            fenString = fenString + " " + (blackToMove ? "b" : "w");

            return fenString;
        }

        static Dictionary<string, Mat> LoadPieceTemplates()
        {
            
            // Load templates for each chess piece on light and dark squares
            Dictionary<string, Mat> templates = new Dictionary<string, Mat>
        {
            { "empty_dark", Cv2.ImRead("c:\\github\\chess\\templates\\empty_dark.png", ImreadModes.Grayscale) },
            { "empty_light", Cv2.ImRead("c:\\github\\chess\\templates\\empty_light.png", ImreadModes.Grayscale) },
            
            // TODO: Would be more optimial to binarize first, and not worry about background color of square, but this worked.
            { "wP_dark", Cv2.ImRead("c:\\github\\chess\\templates\\wP_dark.png", ImreadModes.Grayscale) },
            { "wP_light", Cv2.ImRead("c:\\github\\chess\\templates\\wP_light.png", ImreadModes.Grayscale) },
            { "wR_dark", Cv2.ImRead("c:\\github\\chess\\templates\\wR_dark.png", ImreadModes.Grayscale) },
            { "wR_light", Cv2.ImRead("c:\\github\\chess\\templates\\wR_light.png", ImreadModes.Grayscale) },
            { "wN_dark", Cv2.ImRead("c:\\github\\chess\\templates\\wN_dark.png", ImreadModes.Grayscale) },
            { "wN_light", Cv2.ImRead("c:\\github\\chess\\templates\\wN_light.png", ImreadModes.Grayscale) },
            { "wB_dark", Cv2.ImRead("c:\\github\\chess\\templates\\wB_dark.png", ImreadModes.Grayscale) },
            { "wB_light", Cv2.ImRead("c:\\github\\chess\\templates\\wB_light.png", ImreadModes.Grayscale) },
            { "wQ_light", Cv2.ImRead("c:\\github\\chess\\templates\\wQ_light.png", ImreadModes.Grayscale) },
            { "wQ_dark", Cv2.ImRead("c:\\github\\chess\\templates\\wQ_dark.png", ImreadModes.Grayscale) },
            { "wK_light", Cv2.ImRead("c:\\github\\chess\\templates\\wK_light.png", ImreadModes.Grayscale) },
            { "wK_dark", Cv2.ImRead("c:\\github\\chess\\templates\\wK_dark.png", ImreadModes.Grayscale) },
            
            { "bP_dark", Cv2.ImRead("c:\\github\\chess\\templates\\bP_dark.png", ImreadModes.Grayscale) },
            { "bP_light", Cv2.ImRead("c:\\github\\chess\\templates\\bP_light.png", ImreadModes.Grayscale) },
            { "bR_dark", Cv2.ImRead("c:\\github\\chess\\templates\\bR_dark.png", ImreadModes.Grayscale) },
            { "bR_light", Cv2.ImRead("c:\\github\\chess\\templates\\bR_light.png", ImreadModes.Grayscale) },
            { "bN_dark", Cv2.ImRead("c:\\github\\chess\\templates\\bN_dark.png", ImreadModes.Grayscale) },
            { "bN_light", Cv2.ImRead("c:\\github\\chess\\templates\\bN_light.png", ImreadModes.Grayscale) },
            { "bB_dark", Cv2.ImRead("c:\\github\\chess\\templates\\bB_dark.png", ImreadModes.Grayscale) },
            { "bB_light", Cv2.ImRead("c:\\github\\chess\\templates\\bB_light.png", ImreadModes.Grayscale) },
            { "bQ_dark", Cv2.ImRead("c:\\github\\chess\\templates\\bQ_dark.png", ImreadModes.Grayscale) },
            { "bQ_light", Cv2.ImRead("c:\\github\\chess\\templates\\bQ_light.png", ImreadModes.Grayscale) },
            { "bK_light", Cv2.ImRead("c:\\github\\chess\\templates\\bK_light.png", ImreadModes.Grayscale) },
            { "bK_dark", Cv2.ImRead("c:\\github\\chess\\templates\\bK_dark.png", ImreadModes.Grayscale) },
            
        };
            return templates;
        }

        static Dictionary<string, Mat> LoadDigitTemplates()
        {

            // Load templates for each digit in the problem number
            Dictionary<string, Mat> templates = new Dictionary<string, Mat>
        {
            //{ "", Cv2.ImRead("c:\\github\\chess\\templates\\empty_digit.png", ImreadModes.Grayscale) },
            { "0", Cv2.ImRead("c:\\github\\chess\\templates\\0.png", ImreadModes.Grayscale) },

            { "1", Cv2.ImRead("c:\\github\\chess\\templates\\1.png", ImreadModes.Grayscale) },
            { "2", Cv2.ImRead("c:\\github\\chess\\templates\\2.png", ImreadModes.Grayscale) },
            { "3", Cv2.ImRead("c:\\github\\chess\\templates\\3.png", ImreadModes.Grayscale) },
            { "4", Cv2.ImRead("c:\\github\\chess\\templates\\4.png", ImreadModes.Grayscale) },
            { "5", Cv2.ImRead("c:\\github\\chess\\templates\\5.png", ImreadModes.Grayscale) },
            { "6", Cv2.ImRead("c:\\github\\chess\\templates\\6.png", ImreadModes.Grayscale) },
            { "7", Cv2.ImRead("c:\\github\\chess\\templates\\7.png", ImreadModes.Grayscale) },
            { "8", Cv2.ImRead("c:\\github\\chess\\templates\\8.png", ImreadModes.Grayscale) },
            { "9", Cv2.ImRead("c:\\github\\chess\\templates\\9.png", ImreadModes.Grayscale) },


        };
            return templates;
        }

        static Dictionary<string, Mat> LoadBlackToMoveTemplates()
        {

            // Load templates for each digit in the problem number
            Dictionary<string, Mat> templates = new Dictionary<string, Mat>
        {
            { "black", Cv2.ImRead("c:\\github\\chess\\templates\\black-to-play.png", ImreadModes.Grayscale) },
        };
            return templates;
        }

        // returns true if Black to move
        static bool IdentifyBlackToMove( Mat cell, Dictionary<string, Mat> blackToMoveTemplate)
        {
            string bestMatch = "";
            double maxVal = 0;

            foreach (var template in blackToMoveTemplate)
            {
                Mat result = new Mat();
                Cv2.MatchTemplate(cell, template.Value, result, TemplateMatchModes.CCoeffNormed);

                Cv2.MinMaxLoc(result, out double minVal, out double maxValLocal);
                if (maxValLocal > 0.5 && maxValLocal > maxVal)
                {
                    maxVal = maxValLocal;
                    bestMatch = "black";
                }
            }

            return bestMatch == "black";
        }

        static string IdentifyPiece(Mat cell, Dictionary<string, Mat> templates, bool isLightSquare)
        {
            string bestMatch = "";
            double maxVal = 0;

            foreach (var template in templates)
            {
                // Skip templates that don't match the square color
                if ((isLightSquare && !template.Key.Contains("_light")) ||
                    (!isLightSquare && !template.Key.Contains("_dark")))
                {
                    continue;
                }

                Mat result = new Mat();
                Cv2.MatchTemplate(cell, template.Value, result, TemplateMatchModes.CCoeffNormed);

                Cv2.MinMaxLoc(result, out double minVal, out double maxValLocal);
                if (maxValLocal > 0.5 && maxValLocal > maxVal)
                {
                    maxVal = maxValLocal;
                    bestMatch = template.Key.Substring(0, 2); // Strip the "_light" or "_dark" suffix
                }
            }

            // Map the matched template key to the correct FEN character
            Dictionary<string, string> fenMapping = new Dictionary<string, string>
        {
            { "wK", "K" }, { "wQ", "Q" }, { "wR", "R" }, { "wB", "B" }, { "wN", "N" }, { "wP", "P" },
            { "bK", "k" }, { "bQ", "q" }, { "bR", "r" }, { "bB", "b" }, { "bN", "n" }, { "bP", "p" }
        };

            if (fenMapping.ContainsKey(bestMatch))
            {
                return fenMapping[bestMatch];
            }

            return ""; // Return empty string if no match is found
        }

        static string ConvertBoardToFEN(string[,] board)
        {
            string fen = "";
            for (int i = 0; i < 8; i++)
            {
                int emptyCount = 0;
                for (int j = 0; j < 8; j++)
                {
                    if (string.IsNullOrEmpty(board[i, j]))
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            fen += emptyCount.ToString();
                            emptyCount = 0;
                        }
                        fen += board[i, j];
                    }
                }
                if (emptyCount > 0)
                {
                    fen += emptyCount.ToString();
                }
                if (i < 7)
                {
                    fen += "/";
                }
            }
            return fen;
        }


        static string IdentifyDigit(Mat cell, Dictionary<string, Mat> templates)
        {
            string bestMatch = "0";
            double maxVal = 0;

            foreach (var template in templates)
            {
                Mat result = new Mat();
                Cv2.MatchTemplate(cell, template.Value, result, TemplateMatchModes.CCoeffNormed);

                Cv2.MinMaxLoc(result, out double minVal, out double maxValLocal);
                if (maxValLocal > 0.5 && maxValLocal > maxVal)
                {
                    maxVal = maxValLocal;
                    string val = template.Key;
                    bestMatch = val;
                }
            }

            return bestMatch; // Return empty string if no match is found
        }



    }

}

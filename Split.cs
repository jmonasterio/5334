using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;

class SplitClass
{

    static int ExtractPageNumber(string filePath)
    {
        // Regular expression to match the pattern "page_<number>.png"
        string pattern = @"page_(\d+)\.png";
        Match match = Regex.Match(filePath, pattern);

        if (match.Success && int.TryParse(match.Groups[1].Value, out int pageNumber))
        {
            return pageNumber;
        }

        return -1; // Return -1 if the page number could not be extracted
    }

    public static void Split(string[] args)
    {
        string inputDirectory = "c:\\github\\chess\\out";
        string outputDirectory = "c:\\github\\chess\\tiny";

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string[] files = Directory.GetFiles(inputDirectory, "page_*.png");
        foreach (string file in files)
        {
            var num = ExtractPageNumber(file);
            if (num >= 16 && num <= 957 + 16)
            {
                SplitImage(file, outputDirectory);
            }
        }

        Console.WriteLine("Images have been split and saved.");
    }

    static void SplitImage(string filePath, string outputDirectory)
    {
        using (Image originalImage = Image.FromFile(filePath))
        {
            int originalWidth = originalImage.Width;
            int originalHeight = originalImage.Height;

            int pieceWidth = 285;
            int pieceHeight = 285;

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    int x = 30 + col * (20 + pieceWidth);
                    int y = 80 + row * (6 + pieceHeight);

                    using (Bitmap piece = new Bitmap(pieceWidth, pieceHeight))
                    {
                        using (Graphics g = Graphics.FromImage(piece))
                        {
                            g.DrawImage(originalImage, new Rectangle(0, 0, pieceWidth, pieceHeight), new Rectangle(x, y, pieceWidth, pieceHeight), GraphicsUnit.Pixel);
                        }

                        string outputFilePath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(filePath) + $"_part_{col}_{row}.png");
                        piece.Save(outputFilePath, ImageFormat.Png);
                    }
                }
            }
        }
    }
}

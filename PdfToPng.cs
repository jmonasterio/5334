

using PdfiumViewer;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

class Pdf2PngClass
{
    public static void Pdf2Png(string[] args)
    {
        string pdfPath = "c:\\github\\chess\\5334.pdf";
        string outputDir = "c:\\github\\chess\\out\\";

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        using (var document = PdfDocument.Load(pdfPath))
        {
            if (document == null)
            {
                throw new Exception("Could not load document");
            }
            for (int i = 0; i < document.PageCount; i++)
            {
                using (var image = document.Render(i, 300, 300, true))
                {
                    string outputPath = Path.Combine(outputDir, $"page_{i + 1}.png");
                    image.Save(outputPath, ImageFormat.Png );

                    /*
                     * 
                     * BITONAL was not readable by the other thing.
                     * 
                    // Convert the image to a bitmap
                    using (var bitmap = new Bitmap(image))
                    {
                        // Convert to grayscale
                        Bitmap grayscale = ConvertToGrayscale(bitmap);

                        // Convert to bitonal
                        //Bitmap bitonal = ConvertToBitonal(grayscale);


                        // Save the bitonal image
                        grayscale.Save(outputPath, ImageFormat.Png);
                    }
                    */
                }
            }

            Console.WriteLine(document.PageCount + "PDF pages have been converted to PNG files.");
        }
    }

    static Bitmap ConvertToGrayscale(Bitmap original)
    {
        var grayscale = new Bitmap(original.Width, original.Height);

        using (Graphics g = Graphics.FromImage(grayscale))
        {
            var colorMatrix = new ColorMatrix(new float[][]
            {
                    new float[] { 0.3f, 0.3f, 0.3f, 0, 0 },
                    new float[] { 0.59f, 0.59f, 0.59f, 0, 0 },
                    new float[] { 0.11f, 0.11f, 0.11f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
            });

            var attributes = new ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);

            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
        }

        return grayscale;
    }

    static Bitmap ConvertToBitonal(Bitmap grayscale)
    {
        var bitonal = new Bitmap(grayscale.Width, grayscale.Height);

        for (int y = 0; y < grayscale.Height; y++)
        {
            for (int x = 0; x < grayscale.Width; x++)
            {
                Color color = grayscale.GetPixel(x, y);
                int gray = (color.R + color.G + color.B) / 3;
                Color bitonalColor = gray > 128 ? Color.White : Color.Black;
                bitonal.SetPixel(x, y, bitonalColor);
            }
        }

        return bitonal;
    }
}


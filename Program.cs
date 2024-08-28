using chess;

class Chess
{

    static void Main(string[] args)
    {
        Pdf2PngClass.Pdf2Png(args);
        SplitClass.Split(args);
        //GameNumOcr.RunOcr();
        BoardOcr.RunOcr();
    }

}
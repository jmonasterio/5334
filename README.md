Simple tool to convert Polgar's 5334 book into PGN file.

IMPORTANT NOTE: I own the book. I am using this program for my own personal purposes. I am not including copyrighted content from the book here.

# Basic plan.

0. OCR the book into a PDF. 
1. In the template directory, put PNGs for each piece and digit, so I we can match against image. Not included here, sorry.
2. Program splits the PDF into PGN files, one for each page, in the [project]/out folder
3. Program splits each PGN file into a 6 images (there are six problems per page) in the [project]/tiny
4. For each tiny file, we use openCvSharp to:
	a. Detect the position of all the pieces and convert to a FEN
	b. If it looks like there is no position on the tiny image, skip.
	c. Detect the PROBLEM number 
	d. Detect whether it is white to move, by looking at little symbol on the right.
	e. Figure out if it is mate in 1, 2, or 3, using the problem # .
	f. Append everything to the [project]/fen/5334.pgn
	g. Copy the original tiny image (renamed to problem number) to [project]/fen folder, because I wanted to keep those.
		
NOTE: Code was generated iteratively using Chatgpt

NOTE: I don't really plan to clean this up. It worked once for me, and I tested. Just a tool.

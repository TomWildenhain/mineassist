using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows;
using wMedia = System.Windows.Media;

namespace MinesweeperHacker
{
    class MineSquare
    {
        MainWindow myHandle;
        Rectangle myFillRectangle;
        TextBlock myTextHolder;
        Line[,] myPointingLines;
        List<MineSquare> possibleAdjacentBombs;
        List<MineSquare> certainAdjacentBombs;
        MineSquare[,] myNeighbors;
        public int adjacentBombsNumber;
        public bool flagged;
        public bool uncovered;
        public bool unknown;

        private bool markFlagged;
        private bool markUncovered;
        private bool awaitingUpdate;
        private bool used;
        int MyXCord;
        int MyYCord;
        public MineSquare(int xCord, int yCord, Rectangle fillRectangle, TextBlock textHolder, List<Line> pointingLines, MainWindow handle)
        {
            myFillRectangle = fillRectangle;
            myTextHolder = textHolder;
            MyXCord = xCord;
            MyYCord = yCord;
            myPointingLines = new Line[3, 3];
            myPointingLines[0, 0] = pointingLines[0];
            myPointingLines[0, 1] = pointingLines[1];
            myPointingLines[0, 2] = pointingLines[2];
            myPointingLines[1, 0] = pointingLines[3];
            myPointingLines[1, 1] = pointingLines[4];
            myPointingLines[1, 2] = pointingLines[5];
            myPointingLines[2, 0] = pointingLines[6];
            myPointingLines[2, 1] = pointingLines[7];
            myPointingLines[2, 2] = pointingLines[8];

            flagged = false;
            uncovered = false;
            unknown = true;
            used = false;
            markFlagged = false;
            markUncovered = false;
            awaitingUpdate = true;
            adjacentBombsNumber = 0;
            updateAppearance();
            myHandle = handle;
        }
        public void updateNeighbors(MineSquare[,] neighbors)
        {
            myNeighbors = neighbors;
        }
        public void updateAppearance()
        {
            if (unknown)
            {
                myFillRectangle.Fill = getBlue(MyXCord, MyYCord);
                myTextHolder.Text = "?";
                myTextHolder.Foreground = wMedia.Brushes.Yellow;
            }
            else if (!uncovered)
            {
                myFillRectangle.Fill = getBlue(MyXCord, MyYCord);
                if (flagged)
                {
                    myTextHolder.Text = "!";
                    myTextHolder.Foreground = wMedia.Brushes.Red;
                }
                else
                {
                    myTextHolder.Text = " ";
                }
            }
            else
            {
                myFillRectangle.Fill = getWhite(MyXCord, MyYCord);
                myTextHolder.Text = adjacentBombsNumber + "";
                if (adjacentBombsNumber == 0)
                {
                    myTextHolder.Text = " ";
                }
                myTextHolder.Foreground = wMedia.Brushes.Black;
            }
        }
        public void countAdjacentBombs()
        {
            if (uncovered && !flagged && !used && adjacentBombsNumber != 0)
            {
                possibleAdjacentBombs = new List<MineSquare>();
                certainAdjacentBombs = new List<MineSquare>();
                for (int xshift = -1; xshift <= 1; xshift++)
                {
                    for (int yshift = -1; yshift <= 1; yshift++)
                    {
                        MineSquare currentNeighbor = myNeighbors[xshift + 1, yshift + 1];
                        if ((xshift != 0 || yshift != 0) && currentNeighbor != null)
                        {
                            Line currentLine = myPointingLines[xshift + 1, yshift + 1];
                            if (currentNeighbor.flagged)
                            {
                                certainAdjacentBombs.Add(currentNeighbor);
                                setLine(currentLine, 2);
                            }
                            else if (!currentNeighbor.uncovered)
                            {
                                possibleAdjacentBombs.Add(currentNeighbor);
                                setLine(currentLine, 1);
                            }
                            else
                            {
                                setLine(currentLine, 0);
                            }
                        }
                    }
                }
            }
            
        }
        public void markAdjacentBombs()
        {
            if (uncovered && !flagged && !used && adjacentBombsNumber != 0)
            {
                if (possibleAdjacentBombs.Count + certainAdjacentBombs.Count == adjacentBombsNumber)
                {
                    foreach (MineSquare currentSquare in possibleAdjacentBombs)
                    {
                        currentSquare.markForFlagging();
                    }
                    used = true;
                }
                if (certainAdjacentBombs.Count == adjacentBombsNumber)
                {
                    foreach (MineSquare currentSquare in possibleAdjacentBombs)
                    {
                        currentSquare.markForUncovering();
                    }
                    used = true;
                }
            }
        }
        public void setBG(wMedia.Color bgColor)
        {
            myFillRectangle.Fill = new wMedia.SolidColorBrush(bgColor);
            myTextHolder.Text = " ";
        }
        public void changeType(int type)
        {
            if (type == -1)
            {
                unknown = true;
            }
            else if (type == 9)
            {
                unknown = false;
                uncovered = false;
            }
            else
            {
                unknown = false;
                uncovered = true;
                adjacentBombsNumber = type;
            }
            awaitingUpdate = false;
        }
        public void clearSquare()
        {
            flagged = false;
            uncovered = false;
            unknown = true;
            used = false;
            markFlagged = false;
            markUncovered = false;
            awaitingUpdate = true;
            adjacentBombsNumber = 0;
            foreach(Line currentLine in myPointingLines)
            {
                if (currentLine != null)
                {
                    setLine(currentLine, 0);
                }
            }
        }
        public void setLine(Line currentLine, int state)
        {
            if (state == 0)
            {
                currentLine.Visibility = Visibility.Collapsed;
            }
            else if (state == 1)
            {
                currentLine.Visibility = Visibility.Visible;
                currentLine.Stroke = wMedia.Brushes.Gray;
            }
            else if (state == 2)
            {
                currentLine.Visibility = Visibility.Visible;
                currentLine.Stroke = wMedia.Brushes.Red;
            }
        }
        public void markForFlagging()
        {
            markFlagged = true;
        }
        public void markForUncovering()
        {
            markUncovered = true;
        }
        public void updateStatus()
        {
            if (markFlagged && !flagged)
            {
                flagSelf();
            }
            else if (markUncovered && !uncovered)
            {
                uncoverSelf();
            }
        }
        private void flagSelf()
        {
            myHandle.flagCell(MyXCord, MyYCord);
            flagged = true;
        }
        private void uncoverSelf()
        {
            myHandle.clickCell(MyXCord, MyYCord);
            awaitingUpdate = true;
        }
        public bool isAwaitingUpdate()
        {
            //return awaitingUpdate;
            return (!uncovered && !flagged) || (uncovered && adjacentBombsNumber == 0);
        }
        private static wMedia.Brush getWhite(int x, int y)
        {
            if ((x + y) % 2 == 0)
            {
                return wMedia.Brushes.White;
            }
            else
            {
                return wMedia.Brushes.WhiteSmoke;
            }
        }
        private static wMedia.Brush getBlue(int x, int y)
        {
            if ((x + y) % 2 == 0)
            {
                return wMedia.Brushes.MediumBlue;
            }
            else
            {
                return wMedia.Brushes.Blue;
            }
        }
    }
}

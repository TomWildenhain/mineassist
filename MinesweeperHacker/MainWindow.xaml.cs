using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using wMedia = System.Windows.Media;
using fCursor = System.Windows.Forms.Cursor;
using sDrawing = System.Drawing;
using Imaging = System.Drawing.Imaging;
using intImaging = System.Windows.Interop.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MinesweeperHacker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int LEFTDOWN = 0x02;
        private const int LEFTUP = 0x04;
        private const int RIGHTDOWN = 0x08;
        private const int RIGHTUP = 0x10;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);


        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        int x1 = 0;
        int y1 = 0;
        int x2 = 0;
        int y2 = 0;
        bool timerModeReadNotDo = true;
        bool point1Assigned = false;
        bool point2Assigned = false;
        int mineWidth = 16;//30
        int mineHeight = 16;//16
        const int cellSize = 20;
        bool gridCreated = false;
        const int tolerance = 8;
        wMedia.Color[,] ColorGrid;
        MineSquare[,] mineSquareGrid;
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        Grid mineGrid;

        private const int scanDelay = 0;
        public MainWindow()
        {
            InitializeComponent();
            buildMineGrid(mineWidth, mineHeight);
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.D1)
            {
                setTopLeft();
            }
            else if (e.Key == Key.D2)
            {
                setBottomRight();
            }
        }
        private void buildMineGrid(int width, int height)
        {
            if (gridCreated)
            {
                destroyMineGrid(mineWidth, mineHeight);
            }
            gridCreated = true;
            mineWidth = width;
            mineHeight = height;
            ColorGrid = new wMedia.Color[mineWidth, mineHeight];
            mineGrid = new Grid();
            mineGridHolder.Child = mineGrid;
            mineSquareGrid = new MineSquare[mineWidth, mineHeight];
            mineGrid.Width = mineWidth * cellSize;
            mineGrid.Height = mineHeight * cellSize;
            for (int x = 0; x < mineWidth; x++)
            {
                mineGrid.ColumnDefinitions.Add(new ColumnDefinition());
            } 
            for (int y = 0; y < mineHeight; y++)
            {
                mineGrid.RowDefinitions.Add(new RowDefinition());
            }

            for (int x = 0; x < mineWidth; x++)
            {
                for (int y = 0; y < mineHeight; y++)
                {
                    Rectangle currentRectangle = new Rectangle();
                    currentRectangle.Name = "R" + x + "x" + y;
                    RegisterName(currentRectangle.Name, currentRectangle);
                    mineGrid.Children.Add(currentRectangle);

                    List<Line> pointingLines = new List<Line>();
                    for (int shiftx = -1; shiftx <= 1; shiftx++)
                    {
                        for (int shifty = -1; shifty <= 1; shifty++)
                        {
                            if (shiftx != 0 || shifty != 0)
                            {
                                Line currentLine = new Line();
                                currentLine.Visibility = Visibility.Collapsed;
                                currentLine.Stroke = wMedia.Brushes.Gray;
                                currentLine.StrokeThickness = 2;
                                //currentLine.SnapsToDevicePixels = true;
                                currentLine.Width = cellSize;
                                currentLine.Height = cellSize;
                                int halfCell = cellSize / 2;
                                currentLine.X1 = halfCell;
                                currentLine.Y1 = halfCell;
                                currentLine.X2 = halfCell + halfCell * shiftx;
                                currentLine.Y2 = halfCell + halfCell * shifty;
                                currentLine.Name = "L" + x + "x" + y + "shift" + (shiftx + 1) + "x" + (shifty + 2);
                                RegisterName(currentLine.Name, currentLine);
                                mineGrid.Children.Add(currentLine);
                                Grid.SetColumn(currentLine, x);
                                Grid.SetRow(currentLine, y);
                                pointingLines.Add(currentLine);
                            }
                            else
                            {
                                pointingLines.Add(null);
                            }
                        }
                    }

                    TextBlock currentTextBlock = new TextBlock();
                    currentTextBlock.Name = "T" + x + "x" + y;
                    RegisterName(currentTextBlock.Name, currentTextBlock);

                    currentTextBlock.FontWeight = FontWeights.Bold;
                    currentTextBlock.Text = " ";
                    currentTextBlock.TextAlignment = TextAlignment.Center;
                    currentTextBlock.VerticalAlignment = VerticalAlignment.Center;
                    
                    mineGrid.Children.Add(currentTextBlock);
                    Grid.SetColumn(currentRectangle, x);
                    Grid.SetRow(currentRectangle, y);
                    Grid.SetColumn(currentTextBlock, x);
                    Grid.SetRow(currentTextBlock, y);

                    mineSquareGrid[x, y] = new MineSquare(x, y, currentRectangle, currentTextBlock, pointingLines,this);
                }
            }

            for (int x = 0; x < mineWidth; x++)
            {
                for (int y = 0; y < mineHeight; y++)
                {
                    MineSquare[,] neighbors = new MineSquare[3, 3];
                    for (int xshift = -1; xshift <= 1; xshift++)
                    {
                        for (int yshift = -1; yshift <= 1; yshift++)
                        {
                            if (x + xshift >= 0 && x + xshift < mineWidth && y + yshift >= 0 && y + yshift < mineHeight)
                            {
                                neighbors[xshift + 1, yshift + 1] = mineSquareGrid[x + xshift, y + yshift];
                            }
                            else
                            {
                                neighbors[xshift + 1, yshift + 1] = null;
                            }
                        }
                    }
                    mineSquareGrid[x, y].updateNeighbors(neighbors);
                }
            }
        }
        private void destroyMineGrid(int width, int height)
        {
            for (int x = 0; x < mineWidth; x++)
            {
                for (int y = 0; y < mineHeight; y++)
                {
                    UnregisterName("R" + x + "x" + y);
                    UnregisterName("T" + x + "x" + y);
                    for (int shiftx = -1; shiftx <= 1; shiftx++)
                    {
                        for (int shifty = -1; shifty <= 1; shifty++)
                        {
                            if (shiftx != 0 || shifty != 0)
                            {
                                UnregisterName("L" + x + "x" + y + "shift" + (shiftx + 1) + "x" + (shifty + 2));
                            }
                        }
                    }
                }
            }
        }
        public void flagCell(int x, int y)
        {
            int unitx = (x2 - x1) / mineWidth;
            int halfUnitx = unitx / 2 + x1;
            int unity = (y2 - y1) / mineHeight;
            int halfUnity = unity / 2 + y1;
            int currentx = System.Windows.Forms.Cursor.Position.X;
            int currenty = System.Windows.Forms.Cursor.Position.Y;
            SetCursorPos(x * unitx + halfUnitx, y * unity + halfUnity);
            System.Threading.Thread.Sleep(20);
            mouse_event(RIGHTDOWN, x * unitx + halfUnitx, y * unity + halfUnity, 0, 0);
            System.Threading.Thread.Sleep(20);
            mouse_event(RIGHTUP, x * unitx + halfUnitx, y * unity + halfUnity, 0, 0);
            System.Threading.Thread.Sleep(20);
            //SetCursorPos(currentx, currenty);
        }
        public void clickCell(int x, int y)
        {
            int unitx = (x2 - x1) / mineWidth;
            int halfUnitx = unitx / 2 + x1;
            int unity = (y2 - y1) / mineHeight;
            int halfUnity = unity / 2 + y1;
            int currentx = System.Windows.Forms.Cursor.Position.X;
            int currenty = System.Windows.Forms.Cursor.Position.Y;
            SetCursorPos(x * unitx + halfUnitx, y * unity + halfUnity);
            System.Threading.Thread.Sleep(20);
            mouse_event(LEFTDOWN, x * unitx + halfUnitx, y * unity + halfUnity, 0, 0);
            System.Threading.Thread.Sleep(20);
            mouse_event(LEFTUP, x * unitx + halfUnitx, y * unity + halfUnity, 0, 0);
            System.Threading.Thread.Sleep(20);
            //SetCursorPos(currentx, currenty);
        }
        private void clearBoard()
        {
            for (int x = 0; x < mineWidth; x++)
            {
                for (int y = 0; y < mineHeight; y++)
                {
                    mineSquareGrid[x, y].clearSquare();
                }
            }
        }
        private void captureTypes()
        {
            if (System.Windows.Forms.Control.MouseButtons == System.Windows.Forms.MouseButtons.Left)
            {
                return;
            }
            using (sDrawing.Bitmap screenBmp = new sDrawing.Bitmap(x2 - x1, y2 - y1, sDrawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (sDrawing.Graphics bmpGraphics = sDrawing.Graphics.FromImage(screenBmp))
                {
                    bmpGraphics.CopyFromScreen(x1, y1, 0, 0, screenBmp.Size);
                }

                for (int x = 0; x < mineWidth; x++)
                {
                    for (int y = 0; y < mineHeight; y++)
                    {
                        if (mineSquareGrid[x, y].isAwaitingUpdate())
                        {
                            int xToRead = (int)((x2 - x1) / mineWidth * (x + 0.5));
                            int yToRead = (int)((y2 - y1) / mineHeight * (y + 0.5));
                            //sDrawing.Color outputColor = screenBmp.GetPixel(xToRead, yToRead);
                            //ColorGrid[x, y] = wMedia.Color.FromArgb(outputColor.A, outputColor.R, outputColor.G, outputColor.B);
                            int type = getTypeFromArea(screenBmp, xToRead, yToRead);
                            mineSquareGrid[x, y].changeType(type);
                            if (type == -1)
                            {

                            }
                            if(type != 9)
                            {

                            }
                        }
                    }
                }
            }
        }
        private void updateAppearance()
        {
            foreach (MineSquare currentSquare in mineSquareGrid)
            {
                currentSquare.updateAppearance();
            }
        }
        private int getTypeFromArea(sDrawing.Bitmap screenBmp, int x, int y)
        {
            
            int[] results = new int[10];
            wMedia.Color[] colors = new wMedia.Color[10];
            colors[0] = get0Gray(x, y);
            colors[1] = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#4050BE");
            colors[2] = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#2E741A");
            colors[3] = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#A60505");
            colors[4] = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#000182");
            colors[5] = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#7D0100");
            colors[6] = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#02797A");
            colors[7] = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#A70605");
            colors[8] = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#AF0806");
            colors[9] = getUncoveredBlue(x, y);

            for (int c = 0; c < 10; c++)
            {
                for (int xOffset = -4; xOffset <= 6; xOffset++)
                {
                    for (int yOffset = -4; yOffset <= 6; yOffset++)
                    {
                        int smartTolerance = tolerance;
                        if (c == 0)
                        {
                            smartTolerance = 15;
                        }
                        else if (c == 9)
                        {
                            smartTolerance = 40;
                        }
                        sDrawing.Color currentPixel = screenBmp.GetPixel(x + xOffset, y + yOffset);
                        wMedia.Color currentColor = wMedia.Color.FromArgb(currentPixel.A, currentPixel.R, currentPixel.G, currentPixel.B);
                        int dist = colorDist(currentColor, colors[c]);
                        if (dist < smartTolerance)
                        {
                            results[c] = 1;
                        }
                    }
                }
            }
            int[] order = { 2, 3, 4, 5, 6, 7, 8, 0, 9 };
            if (results[1] == 1 && results[0] == 1)
            {
                return 1;
            }
            foreach (int o in order)
            {
                if (results[o] == 1)
                {
                    return o;
                }
            }
            if (results[0] == 1)
            {
                return 0;
            }
            return -1;
        }
        private int colorDist(wMedia.Color color1, wMedia.Color color2)
        {
            double sumR = Math.Abs(color1.R - color2.R);
            double sumG = Math.Abs(color1.G - color2.G);
            double sumB = Math.Abs(color1.B - color2.B);
            return (int)Math.Max(sumR, Math.Max(sumG, sumB));
        }
        private bool betweenColorOLD(wMedia.Color color1, wMedia.Color color2, wMedia.Color colorTest)
        {
            int R1 = color1.R;
            int G1 = color1.G;
            int B1 = color1.B;
            int R2 = color2.R;
            int G2 = color2.G;
            int B2 = color2.B;
            if (R1 < R2)
            {
                int temp = R1;
                R1 = R2;
                R2 = temp;
            }
            if (G1 < G2)
            {
                int temp = G1;
                G1 = G2;
                G2 = temp;
            }
            if (B1 < B2)
            {
                int temp = B1;
                B1 = B2;
                B2 = temp;
            }
            if (colorTest.R > R1 || colorTest.G > G1 || colorTest.B > B1)
            {
                return false;
            }
            if (colorTest.R < R2 || colorTest.G < G2 || colorTest.B < B2)
            {
                return true;
            }
            return true;
        }
        private wMedia.Color getUncoveredBlue(int x, int y)
        {
            int boardHeight = y2 - y1;
            int boardWidth = x2 - x1;

            wMedia.Color colorTopLeft = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#C0E9FD");
            wMedia.Color colorBottomRight = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#4C60D3");
            wMedia.Color colorMiddle = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#5C7CDB");
            int maxSum = boardWidth + boardHeight;
            int currentSum = x + y;
            int middleSum = maxSum / 2;
            if (currentSum < middleSum)
            {
                double fractionMiddle = currentSum / (double)middleSum;
                double fractionTopLeft = 1 - fractionMiddle;
                int red = (int)(fractionMiddle * colorMiddle.R + fractionTopLeft * colorTopLeft.R);
                int green = (int)(fractionMiddle * colorMiddle.G + fractionTopLeft * colorTopLeft.G);
                int blue = (int)(fractionMiddle * colorMiddle.B + fractionTopLeft * colorTopLeft.B);
                return wMedia.Color.FromRgb((byte)red, (byte)green, (byte)blue);
            }
            else
            {
                int overshoot = currentSum - middleSum;
                double fractionBottomRight = overshoot / (double)middleSum;
                double fractionMiddle = 1 - fractionBottomRight;
                int red = (int)(fractionMiddle * colorMiddle.R + fractionBottomRight * colorBottomRight.R);
                int green = (int)(fractionMiddle * colorMiddle.G + fractionBottomRight * colorBottomRight.G);
                int blue = (int)(fractionMiddle * colorMiddle.B + fractionBottomRight * colorBottomRight.B);
                return wMedia.Color.FromRgb((byte)red, (byte)green, (byte)blue);
            }
        }
        private wMedia.Color get0Gray(int x, int y)
        {
            int boardHeight = y2 - y1;
            int boardWidth = x2 - x1;

            wMedia.Color colorTopLeft = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#E2EFF8");
            wMedia.Color colorBottomRight = (wMedia.Color)wMedia.ColorConverter.ConvertFromString("#ADB6D9");
            int maxSum = boardWidth + boardHeight;
            int currentSum = x + y;
            double fractionBottomRight = currentSum / (double)maxSum;
            double fractionTopLeft = 1 - fractionBottomRight;
            int red = (int)(fractionBottomRight * colorBottomRight.R + fractionTopLeft * colorTopLeft.R);
            int green = (int)(fractionBottomRight * colorBottomRight.G + fractionTopLeft * colorTopLeft.G);
            int blue = (int)(fractionBottomRight * colorBottomRight.B + fractionTopLeft * colorTopLeft.B);
            return wMedia.Color.FromRgb((byte)red, (byte)green, (byte)blue);
        }
        private void positionMinesweeper()
        {
            Process p = findMinesweeper();
            if (p != null)
            {
                IntPtr windowHandle = p.MainWindowHandle;
                MoveWindow(windowHandle, 900, 200, 20, 20, true);
            }
            else
            {
                MessageBox.Show("Minesweeper is not open.");
            }
        }
        private Process findMinesweeper()
        {
            Process[] ps = Process.GetProcesses();
            foreach (Process p in ps)
            {
                if (p.ProcessName.IndexOf("MineSweeper") > -1)
                {
                    return p;
                }
            }
            return null;
        }
        private void scanForMoves()
        {
            foreach (MineSquare currentSquare in mineSquareGrid)
            {
                currentSquare.countAdjacentBombs();
                currentSquare.markAdjacentBombs();
            }
            updateAppearance();
        }
        private void doMoves()
        {
            int currentx = System.Windows.Forms.Cursor.Position.X;
            int currenty = System.Windows.Forms.Cursor.Position.Y;

            foreach (MineSquare currentSquare in mineSquareGrid)
            {
                currentSquare.updateStatus();
            }
            SetCursorPos(currentx, currenty);
            System.Threading.Thread.Sleep(scanDelay);
            captureTypes();
            updateAppearance();
        }


        private void setTopLeft()
        {
            x1 = fCursor.Position.X;
            y1 = fCursor.Position.Y;
            point1Assigned = true;
            TBtopLeft.Text = "Top left: (" + x1 + "," + y1 + ")";
            maybeShowPreview();
        }
        private void setBottomRight()
        {
            x2 = fCursor.Position.X;
            y2 = fCursor.Position.Y;
            point2Assigned = true;
            TBbottomRight.Text = "Bottom right: (" + x2 + "," + y2 + ")";
            maybeShowPreview();
        }
        private void maybeShowPreview()
        {
            if (point1Assigned && point2Assigned)
            {
                stopTimer();
                clearBoard();
                captureTypes();
                updateAppearance();
            }
        }
        private void ReadScreen_Click(object sender, RoutedEventArgs e)
        {
            maybeShowPreview();
        }

        
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (Keyboard.GetKeyStates(Key.Escape) == KeyStates.Down || Keyboard.GetKeyStates(Key.Escape) == KeyStates.Toggled)
            {
                stopTimer();
                return;
            }
            if (System.Windows.Forms.Control.MouseButtons == System.Windows.Forms.MouseButtons.Left)
            {
                return;
            }

            if (timerModeReadNotDo)
            {
                scanForMoves();
            }
            else
            {
                doMoves();
            }
            timerModeReadNotDo = !timerModeReadNotDo;
        }

        private void BNbegin_Click(object sender, RoutedEventArgs e)
        {
            if (!stopTimer())
            {
                if (point1Assigned && point2Assigned)
                {
                    //System.Threading.Thread.Sleep(3000);
                    dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(TBXdelayTime.Text));
                    dispatcherTimer.Start();
                    BNbegin.Content = "Stop";
                }
            }
        }

        private void Advanced_Click(object sender, RoutedEventArgs e)
        {
            stopTimer();
            buildMineGrid(30, 16);
        }

        private void Intermediate_Click(object sender, RoutedEventArgs e)
        {
            stopTimer();
            buildMineGrid(16, 16);
        }

        private void Beginner_Click(object sender, RoutedEventArgs e)
        {
            stopTimer();
            buildMineGrid(9, 9);
        }
        private bool stopTimer()
        {
            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
                BNbegin.Content = "Begin";
                return true;
            }
            return false;
        }
    }
}

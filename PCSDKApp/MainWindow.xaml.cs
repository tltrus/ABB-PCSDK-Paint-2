using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using ABB.Robotics.Controllers.Configuration;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers;
using RobotStudio.Services.RobApi;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Data;
using RobotStudio.Services.RobApi.RobApi1;
using static System.Net.Mime.MediaTypeNames;
using ABB.Robotics.Controllers.RapidDomain;
using System.Globalization;


namespace Painting
{
    public partial class MainWindow : Window
    {
        DrawingVisual visual;
        DrawingContext dc;
        double width, height;
        ABBRobot Robot;
        NetworkScanner Netscaner { get; set; }
        ControllerInfoCollection Controllers { get; set; }
        bool paint;


        public MainWindow()
        {
            InitializeComponent();

            
            Robot = new ABBRobot();

            width = g.Width;
            height = g.Height;

            visual = new DrawingVisual();

            NetScan();

            Drawing();
        }

        private void NetScan()
        {
            Netscaner = new NetworkScanner();
            Netscaner.Scan();
            Controllers = Netscaner.Controllers;

            foreach (ControllerInfo c in Controllers)
            {
                cbox_Controllers.Items.Add(c);
            }
        }

        public void Drawing()
        {
            g.RemoveVisual(visual);
            using (dc = visual.RenderOpen())
            {
                // Draw Axis
                DrawingAxis(dc);

                foreach (var point in Robot.positions)
                {
                    // Draw point
                    dc.DrawEllipse(Brushes.Blue, null, point, 2, 2);

                    // Draw point num
                    FormattedText formattedText = new FormattedText((Robot.positions.IndexOf(point) + 1).ToString(), CultureInfo.GetCultureInfo("en-us"),
                                    FlowDirection.LeftToRight, new Typeface("Verdana"), 8, Brushes.Black,
                                    VisualTreeHelper.GetDpi(visual).PixelsPerDip);
                    Point textPos = new Point(point.X - 3, point.Y - 12);
                    dc.DrawText(formattedText, textPos);
                }

                int pointsNum = Robot.positions.Count;

                for (int i = 1; i < pointsNum; ++i)
                {
                    if (pointsNum > 1)
                    {
                        // Draw line
                        dc.DrawLine(new Pen(Brushes.Blue, 1), Robot.positions[i - 1], Robot.positions[i]);
                    }
                }

                dc.Close();
                g.AddVisual(visual);
            }
        }

        private void DrawingAxis(DrawingContext dc)
        {
            // axis X
            dc.DrawLine(new Pen(Brushes.Black, 1), new Point(10, 10), new Point(30, 10));

            // axis Y
            dc.DrawLine(new Pen(Brushes.Black, 1), new Point(10, 10), new Point(10, 30));

            // X ortha text
            FormattedText formattedText = new FormattedText("X", CultureInfo.GetCultureInfo("en-us"),
                                                FlowDirection.LeftToRight, new Typeface("Verdana"), 10, Brushes.Black,
                                                VisualTreeHelper.GetDpi(visual).PixelsPerDip);
            Point textPos = new Point(32, 5);
            dc.DrawText(formattedText, textPos);

            // Y ortha text
            formattedText = new FormattedText("Y", CultureInfo.GetCultureInfo("en-us"),
                                                FlowDirection.LeftToRight, new Typeface("Verdana"), 10, Brushes.Black,
                                                VisualTreeHelper.GetDpi(visual).PixelsPerDip);
            textPos = new Point(7, 30);
            dc.DrawText(formattedText, textPos);
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            g.RemoveVisual(visual);
            Robot.positions.Clear();
            lbxPoints.Items.Clear();
            lbItems.Content = "0";

            Drawing();
        }
        private void btnToRapid_Click(object sender, RoutedEventArgs e)
        {
            if (Robot.SelectedController != null)
            {
                Robot.Move();
            }
        }


        private void btnStart_Click(object sender, RoutedEventArgs e) => Robot.StartExec();
        private void btnStop_Click(object sender, RoutedEventArgs e) => Robot.StopExec();

        private void g_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var x = (int)e.GetPosition(g).X;
            var y = (int)e.GetPosition(g).Y;

            // add point
            var point = new Point(x, y);
            Robot.positions.Add(point);


            // listbox
            var item = x + ", " + y + ", 0";
            lbxPoints.Items.Add(item);

            lbItems.Content = Robot.positions.Count.ToString() + " points (x, y, z)";

            // Drawing
            Drawing();
        }

        private void g_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (Robot.positions.Count < 1) return;

            // remove last point
            Robot.positions.RemoveAt(Robot.positions.Count - 1);

            // listbox
            lbxPoints.Items.RemoveAt(lbxPoints.Items.Count - 1); 

            lbItems.Content = Robot.positions.Count.ToString() + " points (x, y, z)";

            Drawing();
        }

        private void cbox_Controllers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var comboBoxControllers = sender as ComboBox;
                Robot.Connect((ControllerInfo)comboBoxControllers?.SelectedItem);
                lbSystem.Content = Robot.SelectedController.SystemName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



    }
}

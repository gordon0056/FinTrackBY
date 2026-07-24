using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OxyPlot;
using OxyPlot.Wpf;

namespace FinTrackBY.Views
{
    public partial class ChartView : UserControl
    {
        public ChartView()
        {
            InitializeComponent();
        }

        private void PlotView_MouseMove(object sender, MouseEventArgs e)
        {
            var plotView = (PlotView)sender;
            if (plotView.ActualModel == null)
                return;

            var pos = e.GetPosition(plotView);
            var screenPoint = new ScreenPoint(pos.X, pos.Y);

            TrackerHitResult best = null;
            double bestDist = double.MaxValue;

            foreach (var series in plotView.ActualModel.Series)
            {
                var result = series.GetNearestPoint(screenPoint, false);
                if (result != null)
                {
                    double dist = result.Position.DistanceToSquared(screenPoint);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = result;
                    }
                }
            }

            if (best != null && bestDist < 2500)
            {
                string title = best.Series?.Title ?? "";
                string date = DateTime.FromOADate(best.DataPoint.X).ToString("dd.MM.yyyy");
                string rate = best.DataPoint.Y.ToString("F4");
                TrackerText.Text = $"{title}\n{date}\n{rate} BYN";

                Canvas.SetLeft(TrackerDot, best.Position.X - 5);
                Canvas.SetTop(TrackerDot, best.Position.Y - 5);
                TrackerDot.Visibility = Visibility.Visible;

                TrackerPopup.Visibility = Visibility.Visible;
                TrackerPopup.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double pw = TrackerPopup.DesiredSize.Width;
                double ph = TrackerPopup.DesiredSize.Height;
                double pw_actual = plotView.ActualWidth;

                double left = best.Position.X + 12;
                if (left + pw > pw_actual)
                    left = best.Position.X - pw - 5;

                double top = best.Position.Y - ph - 5;
                if (top < 0)
                    top = best.Position.Y + 12;

                Canvas.SetLeft(TrackerPopup, left);
                Canvas.SetTop(TrackerPopup, top);
            }
            else
            {
                TrackerDot.Visibility = Visibility.Collapsed;
                TrackerPopup.Visibility = Visibility.Collapsed;
            }
        }

        private void PlotView_MouseLeave(object sender, MouseEventArgs e)
        {
            TrackerDot.Visibility = Visibility.Collapsed;
            TrackerPopup.Visibility = Visibility.Collapsed;
        }
    }
}

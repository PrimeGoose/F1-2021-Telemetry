﻿using Codemasters.F1_2021;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace F1_2021_Telemetry
{
    /// <summary>
    /// Manages and draws the driver circle and the estimated pitstop line.
    /// </summary>
    class DriverCircle
    {
        public static readonly float[] TrackPitstopDelta = { 22.5f, 25.0f /*No data*/, 23.0f, 24.6f, 22.5f, 25.0f, 23.6f, 24.7f, 25.0f /*No data*/, 22.0f, 23.0f, 25.0f, 29.2f, 23.5f, 22.0f, 25.0f /*No data*/, 24.5f, 22.5f, 30.5f, 22.8f, 25.0f /*No data*/, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f };

        private FormMain Parent;
        private PictureBox pictureBox;
        private Bitmap CircleBitmap;

        private System.Timers.Timer UpdateTimer;

        private float[] DriverLapTimeMeter; // Meter in lap at certain point of time. 10th second accuracy

        private float CustomPitstopDelta = 20;

        public DriverCircle(FormMain formMain, PictureBox pictureBox)
        {
            this.Parent = formMain;
            this.pictureBox = pictureBox;
            this.CircleBitmap = new Bitmap(this.pictureBox.Width, this.pictureBox.Height); // Create bitmap for driver circle

            DriverLapTimeMeter = new float[2400]; // 2min * 60 (seconds) * 10 (th second)

            this.UpdateTimer = new System.Timers.Timer();
            this.UpdateTimer.Interval = 50;
            this.UpdateTimer.Elapsed += new System.Timers.ElapsedEventHandler(UpdateDriverCircle); ;
            this.UpdateTimer.Start();
        }

        private void UpdateDriverCircle(object sender, EventArgs e)
        {
            if (this.Parent.LatestLeaderboard == null)
                return;

            pictureBox.Invoke((MethodInvoker)delegate () // Draw circle with all drivers
            {
                this.ClearCircle(pictureBox, CircleBitmap, FormMain.GlobalColorGray); // Clear the driver circle
                for (byte i = 0; i < FormMain.NumberOfDrivers; i++)
                {
                    LeaderboardDriver driver = this.Parent.LatestLeaderboard.getDriver(FormMain.NumberOfDrivers - 1 - i); // Driver in position i
                    if (driver != null)
                    {
                        this.DrawDrivers(driver.Index, driver.TeamColor, this.Parent.LapPacket, this.Parent.SessionPacket, pictureBox, CircleBitmap); // Tell circle to draw driver
                    }
                }
            });
        }

        public void SetCustomPitstopDelta(float delta)
        {
            CustomPitstopDelta = delta;
        }

        public void UseAutomaticPitstopDelta()
        {
            CustomPitstopDelta = -1;
        }

        /// <summary>
        /// Store position to array at certain point in time
        /// </summary>
        /// <param name="time"></param>
        /// <param name="position"></param>
        public void SetDriverTimeOnPosition(uint time, float position)
        {
            uint timeValue = (uint)(time / 100); // to 10th seconds
            if (timeValue < 2400)
            {
                DriverLapTimeMeter[timeValue] = position;
            }
        }

        /// <summary>
        /// Computes and retuns the meters the pitstop line should be.
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="currentLapTime"></param>
        /// <param name="track"></param>
        /// <returns></returns>
        private float GetMetersOfPitstopLine(uint lastLapTime, uint currentLapTime, Track track)
        {
            uint pitstopDelta = (uint)(TrackPitstopDelta[(int)track]);
            if (CustomPitstopDelta != -1)
            {
                pitstopDelta = (uint)(CustomPitstopDelta);
            }
            pitstopDelta *= 10;

            int delta = (int)((currentLapTime/100) - pitstopDelta);

            int pitstopLineTime;
            if (delta < 0) // If delta is negativ (e.g. when just crossed the start/finish line)
            {
                delta = (int)(delta + (lastLapTime/100)); // Add time of last lap
            }
            pitstopLineTime = delta;
            
            if (pitstopLineTime < 2400 && pitstopLineTime >= 0)
            {
                return DriverLapTimeMeter[pitstopLineTime];
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Draw a driver to the circle
        /// </summary>
        /// <param name="driver"></param>
        /// <param name="lapPacket"></param>
        /// <param name="pictureBox"></param>
        /// <param name="CircleBitmap"></param>
        /// <param name="track"></param>
        /// <param name="trackLength"></param>
        /// <param name="isPlayer"></param>
        /// <param name="isSpectating"></param>
        public void DrawDrivers(int index, Color teamColor, LapPacket lapPacket, SessionPacket sessionPacket, PictureBox pictureBox, Bitmap CircleBitmap)
        {
            LapPacket.LapData lapData = lapPacket.FieldLapData[index];
            if (sessionPacket.TrackLengthMeters == 0) return;

            if (lapData.LapDistance < 0) // On OutLap e.g. the lap distance is trackLength-drivenDistance
            {
                lapData.LapDistance = sessionPacket.TrackLengthMeters + lapData.LapDistance;
            }
            float position = lapData.LapDistance;
            int distanceToBorder = 30; // Distance to border
            int radius = (pictureBox.Width / 2) - distanceToBorder; // Radius of circle
            double angle = getAngleFromPosition(position, sessionPacket.TrackLengthMeters); // Angle of driver position
            double circle_x = radius * Math.Cos(angle); // Calculate coordinates
            double circle_y = radius * Math.Sin(angle); // Calculate coordinates
            int size = 12; // Size of the circle of the individual driver

            int l = (int)Math.Round((pictureBox.Width / 2) + circle_x, 0); // Map coordinates
            int t = (int)Math.Round((pictureBox.Height / 2) + circle_y, 0); // Map coordinates           

            int colorSum = teamColor.R + teamColor.G + teamColor.B;
            int colorValue = (colorSum < 300) ? 255 : 0;
            Color color = Color.FromArgb(colorValue, colorValue, colorValue); // Text color inverse of team color
            
            using (Graphics g = Graphics.FromImage(CircleBitmap))
            {
                g.FillEllipse(new SolidBrush(teamColor), Rectangle.FromLTRB(l - size, t - size, l + size, t + size));

                g.DrawString(lapData.CarPosition.ToString(), new Font("Arial", 9), new SolidBrush(color), Rectangle.FromLTRB(l - size + 4, t - size + 4, l + size, t + size)); // Draw driver position
            }

            if (sessionPacket.IsSpectating == false && sessionPacket.PlayerCarIndex == index || 
                sessionPacket.IsSpectating == true && sessionPacket.CarIndexBeingSpectated == index) // If driver is player or spectated player
            {
                uint currentLapTime = lapPacket.FieldLapData[index].CurrentLapTimeInMs;
                SetDriverTimeOnPosition(currentLapTime, lapData.LapDistance); // Save distance based on time

                position = GetMetersOfPitstopLine(lapData.LastLapTimeInMs, currentLapTime, sessionPacket.SessionTrack); // Get track position of pitstop line

                angle = getAngleFromPosition(position, sessionPacket.TrackLengthMeters); // Calculate angle (radian)
                int thickness = 40;
                double pitline_x1 = Math.Round((pictureBox.Width / 2) + (radius + thickness) * Math.Cos(angle), 0);
                double pitline_y1 = Math.Round((pictureBox.Height / 2) + (radius + thickness) * Math.Sin(angle), 0);
                double pitline_x2 = (int)Math.Round((pictureBox.Width / 2) + (radius - thickness) * Math.Cos(angle), 0);
                double pitline_y2 = (int)Math.Round((pictureBox.Height / 2) + (radius - thickness) * Math.Sin(angle), 0);

                // Pitstop line
                using (Graphics g = Graphics.FromImage(CircleBitmap))
                {
                    Point p1 = new Point((int)pitline_x1, (int)pitline_y1);

                    Point p2 = new Point((int)pitline_x2, (int)pitline_y2);

                    g.DrawLine(new Pen(teamColor, 4.0f), p1, p2); // Draw pitstop line in team color
                }
            }

            pictureBox.Image = CircleBitmap;

        }

        /// <summary>
        /// Calculate radian pased of track position. Offset of 25% to shift start/finish line to the bottom
        /// </summary>
        /// <param name="position"></param>
        /// <param name="trackLength"></param>
        /// <returns></returns>
        private static double getAngleFromPosition(float position, float trackLength)
        {
            return 6.2831853071794 * ((position / trackLength) + 0.25f);
        }

        /// <summary>
        /// Clears the driver circle
        /// </summary>
        public void ClearCircle(PictureBox pictureBox, Bitmap CircleBitmap, Color baseColor)
        {
            int distanceToBorder = 30; // distance of the circle to the border of the square
            using (Graphics g = Graphics.FromImage(CircleBitmap))
            {
                g.Clear(baseColor); // Make everythin gray
                Pen pen = new Pen(Color.White);
                g.DrawEllipse(pen, Rectangle.FromLTRB(distanceToBorder, distanceToBorder, pictureBox.Width - distanceToBorder, pictureBox.Height - distanceToBorder)); // l t r b
                g.DrawLine(pen, new Point(pictureBox.Width / 2, pictureBox.Height - 60), new Point(pictureBox.Width / 2, pictureBox.Height)); // Start/Finish line
            }

            pictureBox.Image = CircleBitmap;

        }

        public void Resize()
        {
            this.CircleBitmap = new Bitmap(pictureBox.Width, pictureBox.Height); // Adjust bitmap size within picturebox;
        }

    }
}

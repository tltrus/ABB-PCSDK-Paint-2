using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.RapidDomain;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Painting
{
    internal class ABBRobot
    {
        private RapidData rd_targets = null;
        private ArrayData targets = null;

        private RapidData rd_start = null;
        private int STOP_PROC = -1;
        private const int MSG_ToSTART = 1;
        private Num processFlag;

        private RapidData rd_targetsNum;
        private int maxLength;
        private int targetsNum;
        private Num rd_tgNumValue;

        public List<Point> positions;

        public Controller SelectedController { get; set;  }


        public ABBRobot()
        {
            positions = new List<Point>();
        }

        public void Connect(ControllerInfo controllerInfo)
        {
            if (controllerInfo is null) return;

            SelectedController = Controller.Connect(controllerInfo, ConnectionType.Standalone);
            SelectedController.Logon(UserInfo.DefaultUser);

            Init();
        }

        public void Init()
        {
            var taskRobot = SelectedController.Rapid.GetTask("T_ROB1");
            if (taskRobot != null)
            {
                rd_start = taskRobot.GetRapidData("Module1", "flag");
                if (rd_start.Value is Num)
                {
                    processFlag = (Num)rd_start.Value;
                }

                rd_targetsNum = taskRobot.GetRapidData("Module1", "targetsNum");
                if (rd_targetsNum.Value is Num)
                {
                    rd_tgNumValue = (Num)rd_targetsNum.Value;
                }

                rd_targets = taskRobot.GetRapidData("Module1", "tgPos");
                if (rd_targets.IsArray)
                {
                    targets = (ArrayData)rd_targets.Value;
                    int aRank = targets.Rank;
                    maxLength = targets.GetLength(aRank - 1);
                    ArrayModes am = targets.Mode;
                    targets.Mode = ArrayModes.Dynamic;
                }
                else MessageBox.Show("'targets' data is not array!");
                if (rd_targets == null) MessageBox.Show("'targets' data does not exist!");
            }
        }

        public void StartExec()
        {
            if (SelectedController == null) return;
            
            try
            {
                if (SelectedController.OperatingMode == ControllerOperatingMode.Auto && SelectedController.State == ControllerState.MotorsOn)
                {
                    using (Mastership m = Mastership.Request(SelectedController))
                    {
                        StartResult result = SelectedController.Rapid.Start(true);
                    }
                }
                else
                {
                    MessageBox.Show("Automatic mode is required to start execution from a remote client.");
                }
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show("Mastership is held by another client." + ex.Message);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Unexpected error occurred: " + ex.Message);
            }
        }
        public void StopExec()
        {
            if (SelectedController == null) return;

            try
            {
                if (SelectedController.OperatingMode == ControllerOperatingMode.Auto && SelectedController.State == ControllerState.MotorsOn)
                {
                    using (Mastership m = Mastership.Request(SelectedController))
                    {
                        SelectedController.Rapid.Stop(StopMode.Immediate);
                    }
                }
                else
                {
                    MessageBox.Show("Automatic mode is required to start execution from a remote client.");
                }
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show("Mastership is held by another client." + ex.Message);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Unexpected error occurred: " + ex.Message);
            }
        }

        public void Move()
        {
            CreatePath(positions);
            StartProc();
        }

        public void StopProc()
        {
            processFlag.FillFromString2(STOP_PROC.ToString());
            using (Mastership m = Mastership.Request(SelectedController))
            {
                rd_start.Value = processFlag;
            }
        }

        public void StartProc()
        {
            processFlag.FillFromString2(MSG_ToSTART.ToString());
            using (Mastership m = Mastership.Request(SelectedController))
            {
                rd_start.Value = processFlag;
            }
        }

        public void CreatePath(List<Point> points)
        {
            if (points.Count > maxLength) 
                targetsNum = maxLength;
            else 
                targetsNum = points.Count;

            rd_tgNumValue.FillFromString2(targetsNum.ToString());

            using (Mastership m = Mastership.Request(SelectedController))
            {
                rd_targetsNum.Value = rd_tgNumValue;
            }

            Pos rt;
            for (int i = 0; i < targetsNum; i++)
            {
                rt = new Pos();
                rt.FillFromString2("[" + points[i].X + "," + points[i].Y + ", 0]"); // Z = 0
                Debug.WriteLine(rt.ToString());

                using (Mastership m = Mastership.Request(SelectedController))
                {
                    rd_targets.WriteItem(rt, i);
                }
            }
        }
    }
}

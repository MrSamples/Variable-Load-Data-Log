using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO.Ports;

namespace VarioLoad
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        private void wait(int milliseconds)
        {
            var timer1 = new System.Windows.Forms.Timer();
            if (milliseconds == 0 || milliseconds < 0) return;

            // Console.WriteLine("start wait timer");
            timer1.Interval = milliseconds;
            timer1.Enabled = true;
            timer1.Start();

            timer1.Tick += (s, e) =>
            {
                timer1.Enabled = false;
                timer1.Stop();
                // Console.WriteLine("stop wait timer");
            };

            while (timer1.Enabled)
            {
                Application.DoEvents();
            }
        }

        private void comPortComboBox_Click(object sender, EventArgs e)
        {
            comPortComboBox.Items.Clear();
            String[] ports = SerialPort.GetPortNames();

            for (int i = 0; i < ports.Length; i++)
            {
                comPortComboBox.Items.Add(ports[i]);
            }
        }

        TextWriter tw;
        DateTime curTime;

        private void browse_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "csv files (*.csv)|*.csv|txt files (*.txt)|*.txt|All files (*.*)|*.*";
            curTime = DateTime.Now;
            openFileDialog1.FileName = "BatteryTest_" +
                                       curTime.Year.ToString("D4") +
                                       curTime.Month.ToString("D2") +
                                       curTime.Day.ToString("D2") + "_" +
                                       curTime.Hour.ToString("D2") +
                                       curTime.Minute.ToString("D2") +
                                       curTime.Second.ToString("D2");
            SendKeys.Send("{HOME}+{END}");
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                tw = new StreamWriter(openFileDialog1.FileName, true);
                filePathBox.Text = openFileDialog1.FileName;
                filePathBox.BackColor = Color.GreenYellow;
            }
        }

        bool toggle = false, inp1 = true, once = false, storeSettings = false, mAHFlag = false;
        float time = 0, data1, data2, mAHData = 0;
        float[] settings = new float[14];
        readonly string[] settingNames = { "Profile:",
                                           "Rest Voltage (V):",
                                           "Pulse 1 Voltage (V):",
                                           "Pulse 2 Voltage (V):",
                                           "Rest Current (A):",
                                           "Pulse 1 Current (A):",
                                           "Pulse 1 Duration (s):",
                                           "Pulse 1 Interval (s):",
                                           "Pulse 2 Current (A):",
                                           "Pulse 2 Duration (s):",
                                           "Pulse 2 Interval (s):",
                                           "Pulse Group Duration (s):",
                                           "Pulse Group Interval (s):",
                                           "Log Time (s):" };
        int numBytes = 1, settingsCount = 0, mAHStep = 0, mAHData1, mAHData2;
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string input;
            byte[] buffer;

            try
            {
                buffer = new byte[numBytes];
                serialPort1.Read(buffer, 0, numBytes);
                input = Encoding.UTF8.GetString(buffer);

                if (input.Contains("\"")) {
                    toggle = false;
                    tw.Flush();
                    tw.Close();
                    tw.Dispose();
                    tw = null;
                    serialPort1.Close();
                    working.Text = "Done.";
                    MessageBox.Show("Done storing data", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                if (storeSettings)
                {
                    if (settingsCount == 0)
                        settings[settingsCount] = Convert.ToInt16(input, 16);
                    else if (settingsCount < 6 || settingsCount == 8)
                        settings[settingsCount] = Convert.ToInt16(input, 16) / 1000.0f;
                    else
                        settings[settingsCount] = Convert.ToInt16(input, 16) / 100.0f;

                    if (++settingsCount == 14)
                    {
                        settingsCount = 0;
                        storeSettings = false;
                    }
                }

                if (input == "ffff")
                {
                    if (mAHStep == 0)
                    {
                        mAHStep++;
                        mAHFlag = true;
                    }
                    else if (mAHStep == 1)
                        mAHStep++;
                }
                else if (mAHStep == 2)
                {
                    mAHData1 = Convert.ToInt16(input, 16);
                    mAHStep++;
                }
                else if (mAHStep == 3)
                {
                    mAHData2 = Convert.ToInt16(input, 16);
                    mAHData = ((mAHData1 << 16) + mAHData2) / 3600.0f;

                    mAHStep++;
                    mAHFlag = false;
                }

                if (toggle && !mAHFlag && !storeSettings) {

                    if (inp1)
                    {
                        inp1 = false;
                        data2 = Convert.ToInt16(input, 16) / 1000.0f;
                    }
                    else if (!inp1)
                    {
                        inp1 = true;
                        data1 = Convert.ToInt16(input, 16) / 1000.0f;

                        if (once) {
                            data1 = 0;
                            data2 = 0;
                            once = false;
                        }

                        if (mAHStep == 4)
                        {
                            tw.WriteLine(time.ToString() + "," + data1.ToString() + "," + data2.ToString() + "," + mAHData.ToString());
                            mAHStep = 0;
                        }
                        else if (settingsCount < 14)
                        {
                            tw.WriteLine(time.ToString() + "," + data1.ToString() + "," + data2.ToString() + ",," + settingNames[settingsCount] + "," + settings[settingsCount].ToString());
                            settingsCount++;
                        }
                        else
                        {
                            tw.WriteLine(time.ToString() + "," + data1.ToString() + "," + data2.ToString());
                        }
                        tw.Flush();
                        time += settings[13];
                    }
                }

                if (input.Contains("'"))
                {
                    toggle = true;
                    once = true;
                    storeSettings = true;
                    numBytes = 4;
                }
            }
            catch (Exception ex)
            {
                if (tw != null)
                {
                    tw.WriteLine("Error: " + ex.Message);
                    working.Text = "Error.";
                }
            }
        }

        private void start_Click(object sender, EventArgs e)
        {
            if (comPortComboBox.SelectedItem.ToString().Length == 0)
                return;

            try
            {
                serialPort1.PortName = comPortComboBox.SelectedItem.ToString();
                serialPort1.BaudRate = 57600;
                serialPort1.Encoding = Encoding.UTF8;
                serialPort1.Open();
                working.Text = "Working...";
                if (tw != null)
                {
                    tw.WriteLine("Time (s),Min (V),Max (V),mAH,Settings");
                    tw.Flush();
                }
                serialPort1.WriteLine(",");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Open Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void clear_Click(object sender, EventArgs e)
        {
            if (comPortComboBox.SelectedItem.ToString().Length == 0)
                return;

            DialogResult dialogResult = MessageBox.Show("Are you sure you would like to clear the VLK's storage?", "Confirm Clear", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

            if (dialogResult == DialogResult.Yes)
            {
                try
                {
                    serialPort1.PortName = comPortComboBox.SelectedItem.ToString();
                    serialPort1.BaudRate = 57600;
                    serialPort1.Encoding = Encoding.UTF8;
                    serialPort1.Open();
                    working.Text = "Working...";
                    serialPort1.WriteLine("C");
                    wait(10000);
                    serialPort1.Close();
                    working.Text = "Done.";
                    MessageBox.Show("Done clearing data", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Could Not Open Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    working.Text = "Error.";
                    return;
                }
            }
        }

        private void serialPort1_ErrorReceived(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
        {
            //MessageBox.Show(e.ToString());
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string help = "This application connects to the serial port enumerated by the SparkFun Variable Load board and puts the logged data into a stored text file for user use.\n\n";
            help += "To run:\n";
            help += "1. Create the log file.\n";
            help += "2. Select the COM port number.\n";
            help += "3. Press the start button and wait for the \"done\" message.\n";

            MessageBox.Show(help, "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serialPort1.IsOpen)
                serialPort1.Close();
            if (tw != null)
            {
                tw.Flush();
                tw.Close();
            }
        }
    }
}

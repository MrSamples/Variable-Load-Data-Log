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

namespace VarioLoad
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        TextWriter tw;
        DateTime curTime;

        private void browse_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "csv files (*.csv)|*.csv|txt files (*.txt)|*.txt|All files (*.*)|*.*";
            curTime = DateTime.Now;
            openFileDialog1.FileName = "BatteryTest_" +
                                       curTime.Year.ToString("D4") + "_" +
                                       curTime.Month.ToString("D2") + "_" +
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

        bool toggle = false, inp1 = true, once = false;
        float data1, data2;
        int time = 0;
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            string input;
            byte[] buffer;

            try
            {
                buffer = new byte[4];
                serialPort1.Read(buffer, 0, 4);
                input = Encoding.UTF8.GetString(buffer);
                if (input.Contains("\"")) {
                    toggle = false;
                    tw.Flush();
                    tw.Close();
                    tw.Dispose();
                    tw = null;
                    serialPort1.Close();
                    working.Visible = false;
                    MessageBox.Show("Done storing data", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (toggle) {
                    if (inp1)
                    {
                        inp1 = false;
                        data1 = Convert.ToInt16(input, 16) / 1000.0f;
                    }
                    else
                    {
                        inp1 = true;
                        data2 = Convert.ToInt16(input, 16) / 1000.0f;
                        if (once) {
                            data1 = data2;
                            once = false;
                        }
                        tw.WriteLine(time.ToString() + "," + data1.ToString() + "," + data2.ToString());
                        tw.Flush();
                        time += 600;
                    }
                }
                if (input.Contains("'"))
                {
                    toggle = true;
                    once = true;
                }
            }
            catch (Exception ex)
            {
                if (tw != null)
                {
                    tw.WriteLine("Error: " + ex.Message);
                }
            }
        }

        private void start_Click(object sender, EventArgs e)
        {
            comPortBox.Text = comPortBox.Text.ToUpper().Trim();
            if (comPortBox.TextLength == 0)
                return;
            if (!comPortBox.Text.StartsWith("COM"))
                comPortBox.Text = "COM" + comPortBox.Text;
            try
            {
                serialPort1.PortName = comPortBox.Text;
                serialPort1.BaudRate = 57600;
                serialPort1.Encoding = Encoding.UTF8;
                serialPort1.Open();
                working.Visible = true;
                if (tw != null)
                {
                    tw.WriteLine("Time (s),Min (V),Max (V)");
                    tw.Flush();
                }
                serialPort1.WriteLine(",");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could Not Open Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void serialPort1_ErrorReceived(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
        {
            //MessageBox.Show(e.ToString());
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string help = "This application connects to the serial port enumerated by the SparkFun Variable Load board and puts the logged data into a stored text file for user use.\n\n";
            help += "To run:\n";
            help += "1. Create the log file.\n";
            help += "3. Select the COM port number.\n";
            help += "4. Press the start button and wait for the \"done\" message.\n";

            MessageBox.Show(help, "Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

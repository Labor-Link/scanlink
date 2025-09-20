using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using BarcodePrinter_API;
using BarcodePrinter_API.Comm;
using BarcodePrinter_API.Emulation.PPLB;
using BarcodePrinter_API.Emulation.PPLZ;

namespace VCSharp_2008
{
    public delegate void VoidFunction(int count);

    public partial class Form1 : Form
    {

        public class FunctionData
        {
            public string Descration;
            public VoidFunction Function;
        }

        #region Constant

        string strGraphicFilter = "All Graphic Type|*.bmp;*.gif;*.exig;*.jpg;*.png;*.tiff|All File|*.*||";
        string[] strEmulation = {
                                    "PPLB",
                                    "PPLZ",
                                };
        string[] strPort = { 
                               "File",
                               "COM",
#if !(WindowsCE)//[.
                               "LPT",
                               "USB",
#endif//].
                               "LAN",
                               "Multi-LAN",
                           };

        //PPLB test item.
        FunctionData[] PPLB_ItemList;
        //PPLB barcode item.
        string[] PPLB_BarcodeList = {
                                        "Code 128 UCC Serial Shipping Container Code",
                                        "Code 128 auto A, B, C modes",
                                        "Code 128 mode A",
                                        "Code 128 mode B",
                                        "Code 128 mode C",
                                        "UCC/EAN 128",
                                        "Interleaved 2 of 5",
                                        "Interleaved 2 of 5 with mod 10 check digit",
                                        "Interleaved 2 of 5 with human readable check digit",
                                        "German Post Code",
                                        "Matrix 2 of 5",
                                        "UPC Interleaved 2 of 5",
                                        "Code 39 std. or extended",
                                        "Code 39 with check digit",
                                        "Code 93",
                                        "EAN-13",
                                        "EAN-13 2 digit add-on",
                                        "EAN-13 5 digit add-on",
                                        "EAN-8",
                                        "EAN-8 2 digit add-on",
                                        "EAN-8 5 digit add-on",
                                        "Codabar",
                                        "Postnet 5, 9, 11 and 13 digit",
                                        "UPC-A",
                                        "UPC-A 2 digit add-on",
                                        "UPC-A 5 digit add-on",
                                        "UPC-E",
                                        "UPC-E 2 digit add-on",
                                        "UPC-E 5 digit add-on",
                                        "PDF417",
                                        "Aztec Code",
                                        "MaxiCode",
                                        "QR Code",
                                        "RSS",
                                        "Data Matrix",
                                    };
        //PPLZ test item.
        FunctionData[] PPLZ_ItemList;
        //PPLZ barcode item.
        string[] PPLZ_BarcodeList = {
                                        "Code 11",
                                        "Code 39",
                                        "Plessey",
                                        "Interleaved 2 of 5",
                                        "UPC-E",
                                        "Code 93",
                                        "UPC-A",
                                        "EAN-8",
                                        "EAN-13",
                                        "Industrial 2 of 5",
                                        "Standard 2 of 5",
                                        "UPC/EAN Extensions",
                                        "USPS POSTNET",
                                        "USPS Planet Code",
                                        "USPS Intelligent Mail",
                                        "Code 128",
                                        "ANSI Codabar",
                                        "LOGMARS",
                                        "MSI",
                                        "PDF417",
                                        "MicroPDF417",
                                        "Aztec Code",
                                        "MaxiCode",
                                        "QR Code",
                                        "RSS",
                                        "Data Matrix",
                                };

        #endregion

        #region Variables

        //printer variable.
        BarcodePrinter BarcodePrinter;

        //filestream connection variable.
#if (WindowsCE)//[.
        string strFolder = @"\BarcodePrinter";//default folder.
#else//][.
        string strFolder = @"C:\BarcodePrinter";//default folder.
#endif//].

        //serial connection variable.
        string m_ComName = SerialConnection.DefaultPortName;
        int m_baudRate = SerialConnection.DefaultBaudRate;
        int m_dataBits = SerialConnection.DefaultDataBits;
        SerialParity m_parity = SerialConnection.DefaultParity;
        SerialStopBits m_stopBits = SerialConnection.DefaultStopBits;
        SerialHandshake m_handshake = SerialConnection.DefaultHandshake;

#if !(WindowsCE)//[.
        //parallel connection variable.
        string m_LPTName = ParallelConnection.DefaultPortName;

        //USB connection variable.
        //string m_USBDevicePath = @"\\?\USB#Vid_1664&Pid_025e#000000001#{a5dcbf10-6530-11d2-901f-00c04fb951ed}";
        string m_USBDevicePath = "";
#endif//].

        //TCP connection variable.
        string m_TCPAddress = TCPConnection.DefaultAddress;
        int m_TCPPort = TCPConnection.DefaultPort;

        //Multi-TCP connection variable.
        TCPDialog.TCPListItem[] m_MultiTCPAddressList;
        string m_MultiTCPAddress = TCPConnection.DefaultAddress;
        int m_MultiTCPPort = TCPConnection.DefaultPort;

        //other.
        string strSelectFolder;//select folder.

        #endregion

        #region Constructors

        public Form1()
        {
            InitializeComponent();
        }

        #endregion

        #region general function

        string MergeIPAddressAndPort(string ipAddress, int port)
        {
            string str = "";
            IPAddress address = null;
            bool ret = true;
#if !(WindowsCE)//[.
            ret = IPAddress.TryParse(ipAddress, out address);
#else//][.
            try
            {
                address = IPAddress.Parse(ipAddress);
            }
            catch
            {
                ret = false;
            }
#endif//].
            if (true == ret)
            {
                if (AddressFamily.InterNetworkV6 == address.AddressFamily)
                {
                    str = "[" + ipAddress + "]:" + port;
                }
                else
                {
                    str = ipAddress + ":" + port;
                }
            }
            return str;
        }

        #endregion

        #region Events

        void InitFunctionData()
        {
            //PPLB.
            PPLB_ItemList = new FunctionData[15];
            PPLB_ItemList[0] = new FunctionData { Descration = "SetUtil 1 : reset", Function = __testPPLB_set1,};
            PPLB_ItemList[1] = new FunctionData { Descration = "SetUtil 2 : setting function", Function = __testPPLB_set2,};
            PPLB_ItemList[2] = new FunctionData { Descration = "Media Calibration: test calibrate command", Function = __testPPLB_calibrate, };
            PPLB_ItemList[3] = new FunctionData { Descration = "GraphicsUtil 1 : draw a graphic", Function = __testPPLB_draw1,};
            PPLB_ItemList[4] = new FunctionData { Descration = "GraphicsUtil 2 : draw line, rectangle, graphic", Function = __testPPLB_draw2,};
            PPLB_ItemList[5] = new FunctionData { Descration = "TextUtil 1 : internal font", Function = __testPPLB_text1,};
            PPLB_ItemList[6] = new FunctionData { Descration = "TextUtil 2 : true type font", Function = __testPPLB_text2,};
            PPLB_ItemList[7] = new FunctionData { Descration = "BarcodeUtil 1 : one barcode", Function = __testPPLB_barcode1,};
            PPLB_ItemList[8] = new FunctionData { Descration = "BarcodeUtil 2 : all barcode", Function = __testPPLB_barcode2,};
            PPLB_ItemList[9] = new FunctionData { Descration = "FormUtil 1 : print form", Function = __testPPLB_form1,};
            PPLB_ItemList[10] = new FunctionData { Descration = "FormUtil 2 : print last the same name forms", Function = __testPPLB_form2,};
            PPLB_ItemList[11] = new FunctionData { Descration = "Variable 1 : test Form and Variable command", Function = __testPPLB_variable1, };
            PPLB_ItemList[12] = new FunctionData { Descration = "Counter 1 : test Form and Counter command", Function = __testPPLB_counter1, };
            PPLB_ItemList[13] = new FunctionData { Descration = "Clear : clear storage (clear format, graphic)", Function = __testPPLB_clear,};
            PPLB_ItemList[14] = new FunctionData { Descration = "Send File: send file from the connection", Function = __test_sendfile, };
            //PPLZ.
            PPLZ_ItemList = new FunctionData[13];
            PPLZ_ItemList[0] = new FunctionData { Descration = "SetUtil 1 : reset", Function = __testPPLZ_set1, };
            PPLZ_ItemList[1] = new FunctionData { Descration = "SetUtil 2 : setting function", Function = __testPPLZ_set2, };
            PPLZ_ItemList[2] = new FunctionData { Descration = "Media Calibration: test calibrate command", Function = __testPPLZ_calibrate, };
            PPLZ_ItemList[3] = new FunctionData { Descration = "GraphicsUtil 1 : draw a graphic", Function = __testPPLZ_draw1, };
            PPLZ_ItemList[4] = new FunctionData { Descration = "GraphicsUtil 2 : draw line, rectangle, graphic", Function = __testPPLZ_draw2, };
            PPLZ_ItemList[5] = new FunctionData { Descration = "TextUtil 1 : internal font", Function = __testPPLZ_text1, };
            PPLZ_ItemList[6] = new FunctionData { Descration = "TextUtil 2 : true type font", Function = __testPPLZ_text2, };
            PPLZ_ItemList[7] = new FunctionData { Descration = "BarcodeUtil 1 : one barcode", Function = __testPPLZ_barcode1, };
            PPLZ_ItemList[8] = new FunctionData { Descration = "BarcodeUtil 2 : all barcode", Function = __testPPLZ_barcode2, };
            PPLZ_ItemList[9] = new FunctionData { Descration = "FormatUtil 1 : print format", Function = __testPPLZ_format1, };
            PPLZ_ItemList[10] = new FunctionData { Descration = "FormatUtil 2 : print last the same name formats", Function = __testPPLZ_format2, };
            PPLZ_ItemList[11] = new FunctionData { Descration = "Clear : clear storage (clear format, graphic)", Function = __testPPLZ_clear, };
            PPLZ_ItemList[12] = new FunctionData { Descration = "Send File: send file from the connection", Function = __test_sendfile, };
        }

        /// <summary>
        /// The function is used to load custom data or some control code when create the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {

            // set/create folder.
            strSelectFolder = strFolder;
            Directory.CreateDirectory(strFolder);

            InitFunctionData();
            //PS: Mobile/CE doesn't support Items.AddRange() method.
            // add port.
            foreach (string str in strPort)
                comboBox_port.Items.Add(str);
            comboBox_port.Text = "File";

            // add emulation.
            foreach (string str in strEmulation)
                comboBox_emulation.Items.Add(str);
            comboBox_emulation.Text = "PPLZ";
        }

        private void comboBox_port_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox_port.Text)
            {
                case "File":
                    textBox_port.Text = "Folder:" + strFolder;
                    break;
                case "COM":
                    textBox_port.Text = "Serial:" + this.m_ComName + ":" + this.m_baudRate + ":" + this.m_dataBits + ":" + 
                        this.m_parity + ":" + this.m_stopBits + ":" + this.m_handshake;
                    break;
#if !(WindowsCE)//[.
                case "LPT":
                    textBox_port.Text = "Parallel:" + this.m_LPTName;
                    break;
                case "USB":
                    textBox_port.Text = "USB:" + this.m_USBDevicePath;
                    break;
#endif//].
                case "LAN":
                    textBox_port.Text = "LAN:" + this.MergeIPAddressAndPort(this.m_TCPAddress, this.m_TCPPort);
                    break;
                case "Multi-LAN":
                    textBox_port.Text = "Multi-LAN";
                    break;
                default:
                    MessageBox.Show("No support " + comboBox_port.Text);
                    break;
            }
        }

        private void button_setting_Click(object sender, EventArgs e)
        {
            switch (comboBox_port.Text)
            {
                case "File":
#if !(WindowsCE)//[.
                    // open FolderBrowserDialog to select folder.
                    //PS: Mobile/CE not support FolderBrowserDialog class.
                    FolderBrowserDialog folderdlg = new FolderBrowserDialog();
                    folderdlg.SelectedPath = strFolder;
                    if (DialogResult.OK == folderdlg.ShowDialog())
                    {
                        strFolder = folderdlg.SelectedPath;
                    }
#endif//].
                    //update edit string.
                    comboBox_port_SelectedIndexChanged(null, null);
                    break;

                case "COM":
                    // open SerialDialog to select serial parameter.
                    SerialDialog serialsetdlg = new SerialDialog();
                    serialsetdlg.PortName  = this.m_ComName;
                    serialsetdlg.BaudRate  = this.m_baudRate;
                    serialsetdlg.DataBits  = this.m_dataBits;
                    serialsetdlg.Parity    = this.m_parity;
                    serialsetdlg.StopBits  = this.m_stopBits;
                    serialsetdlg.Handshake = this.m_handshake;
                    if (DialogResult.OK == serialsetdlg.ShowDialog())
                    {
                        // setting serial parameter.
                        this.m_ComName  = serialsetdlg.PortName;
                        this.m_baudRate  = serialsetdlg.BaudRate;
                        this.m_dataBits  = serialsetdlg.DataBits;
                        this.m_parity    = serialsetdlg.Parity;
                        this.m_stopBits  = serialsetdlg.StopBits;
                        this.m_handshake = serialsetdlg.Handshake;
                    }

                    //update edit string.
                    comboBox_port_SelectedIndexChanged(null, null);
                    break;
#if !(WindowsCE)//[.
                case "LPT":
                    // open ParallelDialog to select parallel parameter.
                    ParallelDialog parallelsetdlg = new ParallelDialog();
                    parallelsetdlg.PortName = this.m_LPTName;
                    if (DialogResult.OK == parallelsetdlg.ShowDialog())
                    {
                        // setting parallel parameter.
                        this.m_LPTName = parallelsetdlg.PortName;
                    }

                    //update edit string.
                    comboBox_port_SelectedIndexChanged(null, null);
                    break;

                case "USB":
                    // open USBDialog to select USB Device.
                    USBDialog USBsetdlg = new USBDialog();
                    USBsetdlg.DevicePath = this.m_USBDevicePath;
                    if (DialogResult.OK == USBsetdlg.ShowDialog())
                    {
                        // setting USB Device.
                        this.m_USBDevicePath = USBsetdlg.DevicePath;
                    }
                    //update edit string.
                    comboBox_port_SelectedIndexChanged(null, null);
                    break;
#endif//].
                case "LAN":
                    // open TCPDialog to select TCP/IP addres and port.
                    TCPDialog tcpsetdlg = new TCPDialog();
                    tcpsetdlg.Address = this.m_TCPAddress;
                    tcpsetdlg.Port = this.m_TCPPort;
                    tcpsetdlg.MultiSelect = false;
                    if (DialogResult.OK == tcpsetdlg.ShowDialog())
                    {
                        // setting TCP/IP addres and port.
                        this.m_TCPAddress = tcpsetdlg.Address;
                        this.m_TCPPort = tcpsetdlg.Port;
                    }

                    //update edit string.
                    comboBox_port_SelectedIndexChanged(null, null);
                    break;

                case "Multi-LAN":
                    // open TCPDialog to select TCP/IP addres and port.
                    TCPDialog multitcpsetdlg = new TCPDialog();
                    multitcpsetdlg.Address = this.m_MultiTCPAddress;
                    multitcpsetdlg.Port = this.m_MultiTCPPort;
                    multitcpsetdlg.AddressList = this.m_MultiTCPAddressList;
                    multitcpsetdlg.MultiSelect = true;
                    if (DialogResult.OK == multitcpsetdlg.ShowDialog())
                    {
                        // setting TCP/IP addres and port.
                        this.m_MultiTCPAddress = multitcpsetdlg.Address;
                        this.m_MultiTCPPort = multitcpsetdlg.Port;
                        this.m_MultiTCPAddressList = multitcpsetdlg.AddressList;
                    }

                    //update edit string.
                    comboBox_port_SelectedIndexChanged(null, null);
                    break;

                default:
                    MessageBox.Show("No support " + comboBox_port.Text);
                    break;
            }
        }

        private void comboBox_emulation_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboObj = sender as ComboBox;
            switch (comboObj.Text)
            {
                case "PPLB":
                    comboBox_test.Items.Clear();
                    // add/set test item.
                    foreach (FunctionData item in PPLB_ItemList)
                        comboBox_test.Items.Add(item.Descration);
                    comboBox_test.SelectedIndex = 0;

                    // add/set barcode.
                    comboBox_barcode.Items.Clear();
                    foreach (string str in PPLB_BarcodeList)
                        comboBox_barcode.Items.Add(str);
                    comboBox_barcode.SelectedIndex = 0;
                    break;
                case "PPLZ":
                    comboBox_test.Items.Clear();
                    // add/set test item.
                    foreach (FunctionData item in PPLZ_ItemList)
                        comboBox_test.Items.Add(item.Descration);
                    comboBox_test.SelectedIndex = 0;

                    // add/set barcode.
                    comboBox_barcode.Items.Clear();
                    foreach (string str in PPLZ_BarcodeList)
                        comboBox_barcode.Items.Add(str);
                    comboBox_barcode.SelectedIndex = 0;
                    break;
                default:
                    MessageBox.Show("No support " + comboObj.Text);
                    break;
            }
        }

        private void comboBox_test_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox_barcode.Enabled = ("BarcodeUtil 1 : one barcode" == comboBox_test.Text) ? true : false;
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            int printcount = (int)numericUpDown_count.Value;
            switch (comboBox_emulation.Text)
            {
                case "PPLB":
                    PPLB_ItemList[comboBox_test.SelectedIndex].Function(printcount);
                    break;
                case "PPLZ":
                    PPLZ_ItemList[comboBox_test.SelectedIndex].Function(printcount);
                    break;
                default:
                    MessageBox.Show("No support " + comboBox_emulation.Text);
                    break;
            }
        }

        #endregion

        #region BarcodePrinterTest

        /// <summary>
        /// new a connection, and give it to the BarcodePrinter.AddConnection() method.
        /// </summary>
        /// <param name="additionalname">[in]It is file name when want to create a connection.</param>
        /// <param name="index">[in]Only be used in the "Multi-TCP" interface that is index for address list to create connection.</param>
        /// <returns>[out]'true' is success; 'false' is fail. </returns>
        private bool __createPrn(string additionalname, int index)
        {
            bool isRight = true;
            IPrinterConnection fs = null;

            //The index only be used in the "Multi-LAN" interface.
            if ((0 != index) && ("Multi-LAN" != comboBox_port.Text))
            {
                return false;
            }
            try
            {
                //create a connection.
                switch (comboBox_port.Text)
                {
                    case "File":
                        fs = new FileStreamConnection(strFolder + "\\" + additionalname);
                        break;
                    case "COM":
                        fs = new SerialConnection(m_ComName, m_baudRate, m_parity, m_dataBits, m_stopBits, m_handshake);
                        break;
#if !(WindowsCE)//[.
                    case "LPT":
                        fs = new ParallelConnection(m_LPTName);
                        break;
                    case "USB":
                        fs = new USBConnection(m_USBDevicePath);
                        break;
#endif//].
                    case "LAN":
                        fs = new TCPConnection(m_TCPAddress, m_TCPPort);
                        break;
                    case "Multi-LAN":
                        if ((null != m_MultiTCPAddressList) && (index < m_MultiTCPAddressList.Count()))
                        {
                            fs = new TCPConnection(m_MultiTCPAddressList[index].Address, m_MultiTCPAddressList[index].Port);
                        }
                        break;
                    default:
                        break;
                }

                if (null == fs)
                    return false;
                //give a connection to the BarcodePrinter.AddConnection() method.
                BarcodePrinter = new BarcodePrinter();
                BarcodePrinter.AddConnection(fs);
                BarcodePrinter.Connection.Open(); // equal to fs.Open();
                //give a emulation to the BarcodePrinter.AddEmulation() method.
                switch (comboBox_emulation.Text)
                {
                    case "PPLB":
                        PPLBEmulation = new PPLB();
                        BarcodePrinter.AddEmulation(PPLBEmulation);
                        break;
                    case "PPLZ":
                        PPLZEmulation = new PPLZ();
                        BarcodePrinter.AddEmulation(PPLZEmulation);
                        break;
                }
                return true;
            }

            //exception.
            catch (Exception ex)
            {
                ShowException.Show(this.Name, "__createPrn", ex);
            }
            finally
            {
                if ((false == isRight) && (null != fs))
                {
                    fs.Close();// equal to BarcodePrinter.Connection.Close();
                    fs = null;
                }
            }
            return isRight;
        }

        /// <summary>
        /// Send File: send file from the connection.
        /// To send a file from connection.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __test_sendfile(int printcount)
        {
            //select a file.
            string strfilename = null;
            OpenFileDialog filedlg = new OpenFileDialog();
            filedlg.InitialDirectory = strSelectFolder;
            filedlg.Filter = "All File|*.*";
#if !(WindowsCE)//[.
            filedlg.Title = "Select a File";
#endif//].
            if (DialogResult.OK == filedlg.ShowDialog())
            {
                strfilename = filedlg.FileName;
                strSelectFolder = Path.GetDirectoryName(filedlg.FileName);
            }

            int index = -1;
            do
            {
                //create a connection.
                if (false == __createPrn("sendfile.txt", ++index))
                    break;

                try
                {
                    //send file.
                    //If you want to send data, you can use BarcodePrinter.Write() method to do it.
                    for (int i = 0; i < printcount; ++i)
                    {
                        BarcodePrinter.SendFile(strfilename);
                    }
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__test_sendfile", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        #endregion
    }
}

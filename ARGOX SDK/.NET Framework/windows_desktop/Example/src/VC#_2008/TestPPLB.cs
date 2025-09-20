using System;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using BarcodePrinter_API;
using BarcodePrinter_API.Comm;
using BarcodePrinter_API.Emulation.PPLB;

namespace VCSharp_2008
{
    public partial class Form1 : Form
    {
        #region Variables

        /// <summary>
        /// This is emulation reference.
        /// </summary>
        PPLB PPLBEmulation;

        #endregion

        #region TestPPLBItem

        /// <summary>
        /// Media Calibration: test calibrate command.
        /// Only test SetMediaCalibration() method.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_calibrate(int printcount)
        {
            int index = -1;
            do
            {
                if (false == __createPrn("PPLB_calibrate.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetMediaCalibration();
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_calibrate", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// SetUtil 1 : reset.
        /// Only test SetReset() method.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_set1(int printcount)
        {
            int index = -1;
            do
            {
                //create a connection.
                if (false == __createPrn("PPLB_set1.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetReset();
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_set1", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// SetUtil 2 : setting function.
        /// Test methods of SetUtil class, and AppendData() method of IOUtil class.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_set2(int printcount)
        {
            Encoding encoder = Encoding.Default;
            byte[] buf;

            int index = -1;
            do
            {
                //create a connection.
                if (false == __createPrn("PPLB_set2.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    //The IOUtil.AppendData() method will append data to buffer area.
                    //Because you can append any data at any time, so the method is very powerful.
                    //When you want to use this method, you must to read and find from the programing guide.
                    buf = encoder.GetBytes("Test setting function\r\nStart\r\n");
                    PPLBEmulation.IOUtil.AppendData(buf, 0, buf.Length);
                    PPLBEmulation.SetUtil.SetSerial(9600, SerialParity.None, 8, SerialStopBits.One);
                    PPLBEmulation.SetUtil.SetOrientation(false);
                    PPLBEmulation.SetUtil.SetHomePosition(0, 0);
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage.
                    PPLBEmulation.SetUtil.SetBackfeed(true, 0);
                    PPLBEmulation.SetUtil.SetDarkness(0);
                    PPLBEmulation.SetUtil.SetPrintRate(3);
                    PPLBEmulation.SetUtil.SetHomePosition(5, 5);
                    PPLBEmulation.SetUtil.SetLabelLength(PPLBMediaTrack.Gap_Mode, 203 * 3, 25);
                    PPLBEmulation.SetUtil.SetPrintWidth(203 * 4);
                    buf = encoder.GetBytes("End\r\n");
                    PPLBEmulation.IOUtil.AppendData(buf, 0, buf.Length);
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_set2", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// GraphicsUtil 1 : draw function.
        /// Test methods of GraphicsUtil class.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_draw1(int printcount)
        {
            byte[] buf;
            string strgraphic1 = "";
            Encoding encoder = Encoding.Default;
            OpenFileDialog filedlg = new OpenFileDialog();
            filedlg.InitialDirectory = strSelectFolder;
            filedlg.Filter = strGraphicFilter;
#if !(WindowsCE)//[.
            filedlg.Title = "Select a Graphic";
#endif//].
            if (DialogResult.OK == filedlg.ShowDialog())
            {
                strgraphic1 = filedlg.FileName;
                strSelectFolder = filedlg.FileName;
            }

            int index = -1;
            do
            {
                if (false == __createPrn("PPLB_draw1.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetOrientation(false);
                    PPLBEmulation.SetUtil.SetHomePosition(0, 0);
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage.
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    buf = encoder.GetBytes("Label 1: draw graphic using graphic id");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.GraphicsUtil.StoreGraphic(strgraphic1, "graphic");
                    PPLBEmulation.GraphicsUtil.PrintStoreGraphic(30, 50, "graphic");
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    //call methods that you want.
                    buf = encoder.GetBytes("Label 2: draw graphic direct");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.GraphicsUtil.PrintGraphic(30, 50, strgraphic1);
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    //call methods that you want.
                    buf = encoder.GetBytes("Label 3: draw graphic object direct");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    Bitmap bitmap = new Bitmap(strgraphic1);
                    PPLBEmulation.GraphicsUtil.PrintGraphic(30, 50, bitmap);
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage, set to 'dram' is better.
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_draw1", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// GraphicsUtil 2 : draw function.
        /// Test methods of GraphicsUtil class.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_draw2(int printcount)
        {
            byte[] buf;
            string strgraphic1 = "";
            Encoding encoder = Encoding.Default;
            //第一張印線, 矩形及圖; 第二張印圖; 第三張刪除圖後印圖, 但因先刪除圖, 所以無法列印.
            OpenFileDialog filedlg = new OpenFileDialog();
            filedlg.InitialDirectory = strSelectFolder;
            filedlg.Filter = strGraphicFilter;
#if !(WindowsCE)//[.
            filedlg.Title = "Select a Graphic";
#endif//].
            if (DialogResult.OK == filedlg.ShowDialog())
            {
                strgraphic1 = filedlg.FileName;
                strSelectFolder = filedlg.FileName;
            }

            int index = -1;
            do
            {
                if (false == __createPrn("PPLB_draw2.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetOrientation(false);
                    PPLBEmulation.SetUtil.SetHomePosition(0, 0);
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage.
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    buf = encoder.GetBytes("Label 1: draw line(Black/White/XOR), rectangle and graphic");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.GraphicsUtil.PrintLine(30, 90, 200, 20, PPLBDraw.Draw_Black);
                    PPLBEmulation.GraphicsUtil.PrintLine(80, 50, 10, 100, PPLBDraw.Draw_Black);
                    PPLBEmulation.GraphicsUtil.PrintLine(130, 50, 10, 100, PPLBDraw.Draw_White);
                    PPLBEmulation.GraphicsUtil.PrintLine(180, 50, 10, 100, PPLBDraw.Draw_XOR);
                    PPLBEmulation.GraphicsUtil.PrintBox(250, 50, 450, 150, 10);
                    PPLBEmulation.GraphicsUtil.StoreGraphic(strgraphic1, "graphic");
                    PPLBEmulation.GraphicsUtil.PrintStoreGraphic(30, 180, "graphic");
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    buf = encoder.GetBytes("Label 2: recall graphic object and the size is 180 * 180");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.GraphicsUtil.StoreGraphic(strgraphic1, "graphic2", 180, 180);
                    PPLBEmulation.GraphicsUtil.PrintStoreGraphic(30, 180, "graphic2");
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    buf = encoder.GetBytes("Label 3: delete graphic object");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.GraphicsUtil.DeleteStoreGraphic("graphic");
                    PPLBEmulation.GraphicsUtil.PrintStoreGraphic(30, 180, "graphic");
                    PPLBEmulation.GraphicsUtil.DeleteStoreGraphic("graphic2");
                    PPLBEmulation.GraphicsUtil.PrintStoreGraphic(210, 180, "graphic2");
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage, set to 'dram' is better.
                    //The output data.
                    //In fact, you can only call once PrintOut() to output buffer data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_draw2", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// TextUtil 1 : text function.
        /// Test methods of TextUtil class.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_text1(int printcount)
        {
            byte[] buf, buf2;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                if (false == __createPrn("PPLB_text1.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetOrientation(false);
                    PPLBEmulation.SetUtil.SetHomePosition(0, 0);
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    buf = encoder.GetBytes("Label: print internal font(Normal/Reverse)");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    buf = encoder.GetBytes("Font  text     Font  text     Font  text     Font  text");
                    PPLBEmulation.TextUtil.PrintText(30, 50, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    buf = encoder.GetBytes("12345");
                    //row 1.
                    buf2 = encoder.GetBytes("1");
                    PPLBEmulation.TextUtil.PrintText(30, 150, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 20, 150, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, false, buf);
                    buf2 = encoder.GetBytes("2");
                    PPLBEmulation.TextUtil.PrintText(30 + 170, 150, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 190, 150, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    buf2 = encoder.GetBytes("3");
                    PPLBEmulation.TextUtil.PrintText(30 + 340, 150, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 360, 150, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_3, 1, 1, false, buf);
                    buf2 = encoder.GetBytes("4");
                    PPLBEmulation.TextUtil.PrintText(30 + 510, 150, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 530, 150, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_4, 1, 1, false, buf);

                    //row 2.
                    buf2 = encoder.GetBytes("5");
                    PPLBEmulation.TextUtil.PrintText(30, 250, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 20, 250, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_5, 1, 1, false, buf);
                    buf2 = encoder.GetBytes("6");
                    PPLBEmulation.TextUtil.PrintText(30 + 340, 250, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 360, 250, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_6, 1, 1, false, buf);
                    buf2 = encoder.GetBytes("7");
                    PPLBEmulation.TextUtil.PrintText(30 + 510, 250, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 530, 250, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_7, 1, 1, false, buf);

                    //row 3.
                    buf2 = encoder.GetBytes("1");
                    PPLBEmulation.TextUtil.PrintText(30, 350, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 20, 350, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_1, 1, 1, true, buf);
                    buf2 = encoder.GetBytes("2");
                    PPLBEmulation.TextUtil.PrintText(30 + 170, 350, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 190, 350, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, true, buf);
                    buf2 = encoder.GetBytes("3");
                    PPLBEmulation.TextUtil.PrintText(30 + 340, 350, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 360, 350, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_3, 1, 1, true, buf);
                    buf2 = encoder.GetBytes("4");
                    PPLBEmulation.TextUtil.PrintText(30 + 510, 350, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 530, 350, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_4, 1, 1, true, buf);

                    //row 4.
                    buf2 = encoder.GetBytes("5");
                    PPLBEmulation.TextUtil.PrintText(30, 450, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 20, 450, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_5, 1, 1, true, buf);
                    buf2 = encoder.GetBytes("6");
                    PPLBEmulation.TextUtil.PrintText(30 + 340, 450, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 360, 450, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_6, 1, 1, true, buf);
                    buf2 = encoder.GetBytes("7");
                    PPLBEmulation.TextUtil.PrintText(30 + 510, 450, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                    PPLBEmulation.TextUtil.PrintText(30 + 530, 450, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_7, 1, 1, true, buf);
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_text1", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// TextUtil 2 : text function.
        /// Test methods of TextUtil class.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_text2(int printcount)
        {
            byte[] buf;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                if (false == __createPrn("PPLB_text2.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetOrientation(false);
                    PPLBEmulation.SetUtil.SetHomePosition(0, 0);
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage.
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    buf = encoder.GetBytes("Label: print true type font using graphic id");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.TextUtil.StoreTextGraphic("細明體", 20, true, true, true, true, "text1", "ARGOX 1996 成立");
                    PPLBEmulation.TextUtil.StoreTextGraphic("細明體", 20, true, true, true, true, "text2", "ARGOX 1996 成立", PPLBOrient.Clockwise_90_Degrees, 0);
                    PPLBEmulation.GraphicsUtil.PrintStoreGraphic(100, 50, "text1");
                    PPLBEmulation.GraphicsUtil.PrintStoreGraphic(30, 50, "text2");
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    buf = encoder.GetBytes("Label 2: print true type font direct");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.TextUtil.PrintTextGraphic(30, 50, "細明體", 16, false, false, false, false, "繁體字", PPLBOrient.Clockwise_0_Degrees, 0);
                    PPLBEmulation.TextUtil.PrintTextGraphic(30, 100, "細明體", 16, false, false, false, false, "繁體字", PPLBOrient.Clockwise_180_Degrees, 400);
                    PPLBEmulation.TextUtil.PrintTextGraphic(30, 150, "細明體", 32, false, false, false, false, "简体字", PPLBOrient.Clockwise_0_Degrees, 0);
                    PPLBEmulation.TextUtil.PrintTextGraphic(30, 200, "細明體", 32, false, false, false, false, "简体字", PPLBOrient.Clockwise_180_Degrees, 400);
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage, set to 'dram' is better.
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_text2", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// BarcodeUtil 1 : barcode function.
        /// Test methods of BarcodeUtil class.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_barcode1(int printcount)
        {
            byte[] buf, buf2;
            byte[] buf3, buf4;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                if (false == __createPrn("PPLB_barcode1_" + comboBox_barcode.Text + ".txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetOrientation(false);
                    PPLBEmulation.SetUtil.SetHomePosition(0, 0);
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    buf = encoder.GetBytes("Label: one barcode");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    buf = encoder.GetBytes(comboBox_barcode.Text);
                    PPLBEmulation.TextUtil.PrintText(50, 50, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    buf = encoder.GetBytes("23456");
                    switch (comboBox_barcode.Text)
                    {
                        case "Code 128 UCC Serial Shipping Container Code":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_128_UCC, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_128_UCC, 3, 0, 50, true, buf);
                            break;
                        case "Code 128 auto A, B, C modes":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_128_Auto_Mode, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_128_Auto_Mode, 3, 0, 50, true, buf);
                            break;
                        case "Code 128 mode A":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_128_Mode_A, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_128_Mode_A, 3, 0, 50, true, buf);
                            break;
                        case "Code 128 mode B":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_128_Mode_B, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_128_Mode_B, 3, 0, 50, true, buf);
                            break;
                        case "Code 128 mode C":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_128_Mode_C, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_128_Mode_C, 3, 0, 50, true, buf);
                            break;
                        case "UCC/EAN 128":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UCC_EAN_128, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UCC_EAN_128, 3, 0, 50, true, buf);
                            break;
                        case "Interleaved 2 of 5":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Interleaved_2_of_5, 3, 6, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Interleaved_2_of_5, 3, 6, 50, true, buf);
                            break;
                        case "Interleaved 2 of 5 with mod 10 check digit":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Interleaved_2_of_5_With_Mod_10_Check_Digit, 3, 6, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Interleaved_2_of_5_With_Mod_10_Check_Digit, 3, 6, 50, true, buf);
                            break;
                        case "Interleaved 2 of 5 with human readable check digit":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Interleaved_2_of_5_With_Human_Readable_Check_Digit, 3, 6, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Interleaved_2_of_5_With_Human_Readable_Check_Digit, 3, 6, 50, true, buf);
                            break;
                        case "German Post Code":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.German_Post_Code, 3, 6, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.German_Post_Code, 3, 6, 50, true, buf);
                            break;
                        case "Matrix 2 of 5":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Matrix_2_of_5, 3, 6, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Matrix_2_of_5, 3, 6, 50, true, buf);
                            break;
                        case "UPC Interleaved 2 of 5":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_Interleaved_2_of_5, 1, 2, 10, false, buf);
                            break;
                        case "Code 39 std. or extended":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees, 
                                PPLBBarCodeType.Code_39, 3, 6, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees, 
                                PPLBBarCodeType.Code_39, 3, 6, 50, true, buf);
                            break;
                        case "Code 39 with check digit":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_39_With_Check_Digit, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_39_With_Check_Digit, 3, 0, 50, true, buf);
                            break;
                        case "Code 93":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_93, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Code_93, 3, 0, 50, true, buf);
                            break;
                        case "EAN-13":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_13, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_13, 3, 0, 50, true, buf);
                            break;
                        case "EAN-13 2 digit add-on":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_13_2_Digit_Add_on, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_13_2_Digit_Add_on, 3, 0, 50, true, buf);
                            break;
                        case "EAN-13 5 digit add-on":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_13_5_Digit_Add_on, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_13_5_Digit_Add_on, 3, 0, 50, true, buf);
                            break;
                        case "EAN-8":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_8, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_8, 3, 0, 50, true, buf);
                            break;
                        case "EAN-8 2 digit add-on":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_8_2_Digit_Add_on, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_8_2_Digit_Add_on, 3, 0, 50, true, buf);
                            break;
                        case "EAN-8 5 digit add-on":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_8_5_Digit_Add_on, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.EAN_8_5_Digit_Add_on, 3, 0, 50, true, buf);
                            break;
                        case "Codabar":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Codabar, 3, 6, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Codabar, 3, 6, 50, true, buf);
                            break;
                        case "Postnet 5, 9, 11 and 13 digit":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Postnet, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.Postnet, 3, 0, 50, true, buf);
                            break;
                        case "UPC-A":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_A, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_A, 3, 0, 50, true, buf);
                            break;
                        case "UPC-A 2 digit add-on":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_A_2_Digit_Add_on, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_A_2_Digit_Add_on, 3, 0, 50, true, buf);
                            break;
                        case "UPC-A 5 digit add-on":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_A_5_Digit_Add_on, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_A_5_Digit_Add_on, 3, 0, 50, true, buf);
                            break;
                        case "UPC-E":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_E, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_E, 3, 0, 50, true, buf);
                            break;
                        case "UPC-E 2 digit add-on":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_E_2_Digit_Add_on, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_E_2_Digit_Add_on, 3, 0, 50, true, buf);
                            break;
                        case "UPC-E 5 digit add-on":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 110, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_E_5_Digit_Add_on, 3, 0, 50, false, buf);
                            buf2 = encoder.GetBytes("human readable");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLBOrient.Clockwise_0_Degrees,
                                PPLBBarCodeType.UPC_E_5_Digit_Add_on, 3, 0, 50, true, buf);
                            break;
                        case "PDF417":
                            buf2 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintPDF417(50, 110, PPLBOrient.Clockwise_0_Degrees, 400, 300, 1, 
                                PPLBPDF417CompressionMode.Auto_Encoding, 4, 10, 0, 0, false, buf);// PDF417, normal
                            buf2 = encoder.GetBytes("truncate");
                            PPLBEmulation.TextUtil.PrintText(450, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintPDF417(450, 110, PPLBOrient.Clockwise_0_Degrees, 400, 300, 1, 
                                PPLBPDF417CompressionMode.Auto_Encoding, 4, 10, 1, 30, true, buf);// PDF417, truncate
                            break;
                        case "Aztec Code":
                            buf3 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf3);
                            PPLBEmulation.BarcodeUtil.PrintAztecCode(50, 110, 3, 0, false, buf);// Aztec Code
                            buf3 = encoder.GetBytes("reverse Image");
                            PPLBEmulation.TextUtil.PrintText(400, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf3);
                            PPLBEmulation.BarcodeUtil.PrintAztecCode(400, 110, 3, 0, true, buf);// Aztec Code, reverse Image
                            break;
                        case "MaxiCode":
                            buf2 = encoder.GetBytes("mode 2");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            buf3 = encoder.GetBytes("[)>\x001E01\x001D961Z00004951\x001DUPSN\x001D06X610\x001D279\x001D\x001D1/1\x001D10\x001DN\x001D\x001DSEATTLE\x001DWA\x001E\x0004");
                            PPLBEmulation.BarcodeUtil.PrintMaxiCode(50, 110, PPLBMaxiCodeMode.Mode_2, 1, 840, "511470000", buf3);// MaxiCode, mode 2
                            buf2 = encoder.GetBytes("mode 3");
                            PPLBEmulation.TextUtil.PrintText(400, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            buf4 = encoder.GetBytes("[)>\x001E01\x001D961Z00004951\x001DUPSN\x001D06X610\x001D2017/10/11\x001D\x001D1/1\x001D10\x001DN\x001D\x001DSEATTLE\x001D\x001E\x0004");
                            PPLBEmulation.BarcodeUtil.PrintMaxiCode(400, 110, PPLBMaxiCodeMode.Mode_3, 1, 276, "123456", buf4);// MaxiCode, mode 3
                            break;
                        case "QR Code":
                            PPLBEmulation.BarcodeUtil.PrintQRCode(50, 80, PPLBQRCodeModel.Model_2, 3, PPLBQRCodeErrCorrect.Standard, buf);// QR Code
                            break;
                        case "RSS":
                            buf3 = encoder.GetBytes("0061414199999|RSS test");
                            buf2 = encoder.GetBytes("RSS-14");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintRSS(50, 110, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_14, 3, 30, 0, false, buf3);// RSS, RSS-14
                            buf2 = encoder.GetBytes("RSS Truncated");
                            PPLBEmulation.TextUtil.PrintText(450, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintRSS(450, 110, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_Truncated, 3, 30, 0, false, buf3);// RSS, RSS Truncated
                            buf2 = encoder.GetBytes("RSS Stacked");
                            PPLBEmulation.TextUtil.PrintText(50, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintRSS(50, 210, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_Stacked, 3, 30, 0, false, buf3);// RSS, RSS Stacked
                            buf2 = encoder.GetBytes("RSS Stacked Omnidirectional");
                            PPLBEmulation.TextUtil.PrintText(450, 180, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintRSS(450, 210, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_Stacked_Omnidirectional, 3, 30, 0, false, buf3);// RSS, RSS Stacked Omnidirectional
                            buf2 = encoder.GetBytes("RSS Limited (human readable)");
                            PPLBEmulation.TextUtil.PrintText(50, 350, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintRSS(50, 410, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_Limited, 3, 30, 0, true, buf3);// RSS, RSS Limited
                            buf2 = encoder.GetBytes("RSS Expanded (human readable)");
                            PPLBEmulation.TextUtil.PrintText(450, 350, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf2);
                            PPLBEmulation.BarcodeUtil.PrintRSS(450, 410, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_Expanded, 3, 30, 4, true, buf3);// RSS, RSS Expanded
                            break;
                        case "Data Matrix":
                            buf3 = encoder.GetBytes("normal");
                            PPLBEmulation.TextUtil.PrintText(50, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf3);
                            PPLBEmulation.BarcodeUtil.PrintDataMatrix(50, 110, 0, 0, 10, false, buf);// Data Matrix
                            buf3 = encoder.GetBytes("reverse Image");
                            PPLBEmulation.TextUtil.PrintText(400, 80, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf3);
                            PPLBEmulation.BarcodeUtil.PrintDataMatrix(400, 110, 0, 0, 10, true, buf);// Data Matrix, reverse Image
                            break;
                        default:
                            break;
                    }
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_barcode1", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// BarcodeUtil 2 : barcode function.
        /// Test methods of BarcodeUtil class.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_barcode2(int printcount)
        {
            byte[] buf;
            byte[] buf3, buf4;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                if (false == __createPrn("PPLB_barcode2.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetOrientation(false);
                    PPLBEmulation.SetUtil.SetHomePosition(0, 0);
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    buf = encoder.GetBytes("Label: all barcode");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    buf = encoder.GetBytes("23456");
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_UCC, 3, 0, 50, false, buf);//Code 128 UCC Serial Shipping Container Code
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_UCC, 3, 0, 50, true, buf);//Code 128 UCC Serial Shipping Container Code
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_Auto_Mode, 3, 0, 50, false, buf);//Code 128 auto A, B, C modes
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_Auto_Mode, 3, 0, 50, true, buf);//Code 128 auto A, B, C modes
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_Mode_A, 3, 0, 50, false, buf);//Code 128 mode A
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_Mode_A, 3, 0, 50, true, buf);//Code 128 mode A
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_Mode_B, 3, 0, 50, false, buf);//Code 128 mode B
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_Mode_B, 3, 0, 50, true, buf);//Code 128 mode B
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_Mode_C, 3, 0, 50, false, buf);//Code 128 mode C
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_Mode_C, 3, 0, 50, true, buf);//Code 128 mode C
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UCC_EAN_128, 3, 0, 50, false, buf);//UCC/EAN 128
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UCC_EAN_128, 3, 0, 50, true, buf);//UCC/EAN 128
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Interleaved_2_of_5, 3, 6, 50, false, buf);//Interleaved 2 of 5
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Interleaved_2_of_5, 3, 6, 50, true, buf);//Interleaved 2 of 5
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Interleaved_2_of_5_With_Mod_10_Check_Digit, 3, 6, 50, false, buf);//Interleaved 2 of 5 with mod 10 check digit
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Interleaved_2_of_5_With_Mod_10_Check_Digit, 3, 6, 50, true, buf);//Interleaved 2 of 5 with mod 10 check digit
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Interleaved_2_of_5_With_Human_Readable_Check_Digit, 3, 6, 50, false, buf);//Interleaved 2 of 5 with human readable check digit
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Interleaved_2_of_5_With_Human_Readable_Check_Digit, 3, 6, 50, true, buf);//Interleaved 2 of 5 with human readable check digit
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.German_Post_Code, 3, 6, 50, false, buf);//German Post Code
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.German_Post_Code, 3, 6, 50, true, buf);//German Post Code
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Matrix_2_of_5, 3, 6, 50, false, buf);//Matrix 2 of 5
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Matrix_2_of_5, 3, 6, 50, true, buf);//Matrix 2 of 5
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_Interleaved_2_of_5, 1, 2, 10, false, buf);//UPC Interleaved 2 of 5
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_39, 3, 6, 50, false, buf);//Code 39 std. or extended
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_39, 3, 6, 50, true, buf);//Code 39 std. or extended
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_39_With_Check_Digit, 3, 0, 50, false, buf);//Code 39 with check digit
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_39_With_Check_Digit, 3, 0, 50, true, buf);//Code 39 with check digit
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_93, 3, 0, 50, false, buf);//Code 93
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_93, 3, 0, 50, true, buf);//Code 93
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_13, 3, 0, 50, false, buf);//EAN-13
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_13, 3, 0, 50, true, buf);//EAN-13
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_13_2_Digit_Add_on, 3, 0, 50, false, buf);//EAN-13 2 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_13_2_Digit_Add_on, 3, 0, 50, true, buf);//EAN-13 2 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_13_5_Digit_Add_on, 3, 0, 50, false, buf);//EAN-13 5 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_13_5_Digit_Add_on, 3, 0, 50, true, buf);//EAN-13 5 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_8, 3, 0, 50, false, buf);//EAN-8
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_8, 3, 0, 50, true, buf);//EAN-8
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_8_2_Digit_Add_on, 3, 0, 50, false, buf);//EAN-8 2 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_8_2_Digit_Add_on, 3, 0, 50, true, buf);//EAN-8 2 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_8_5_Digit_Add_on, 3, 0, 50, false, buf);//EAN-8 5 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.EAN_8_5_Digit_Add_on, 3, 0, 50, true, buf);//EAN-8 5 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Codabar, 3, 6, 50, false, buf);//Codabar
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Codabar, 3, 6, 50, true, buf);//Codabar
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Postnet, 3, 0, 50, false, buf);//Postnet 5, 9, 11 and 13 digit
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Postnet, 3, 0, 50, true, buf);//Postnet 5, 9, 11 and 13 digit
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_A, 3, 0, 50, false, buf);//UPC-A
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_A, 3, 0, 50, true, buf);//UPC-A
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_A_2_Digit_Add_on, 3, 0, 50, false, buf);//UPC-A 2 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_A_2_Digit_Add_on, 3, 0, 50, true, buf);//UPC-A 2 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_A_5_Digit_Add_on, 3, 0, 50, false, buf);//UPC-A 5 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_A_5_Digit_Add_on, 3, 0, 50, true, buf);//UPC-A 5 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_E, 3, 0, 50, false, buf);//UPC-E
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_E, 3, 0, 50, true, buf);//UPC-E
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_E_2_Digit_Add_on, 3, 0, 50, false, buf);//UPC-E 2 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_E_2_Digit_Add_on, 3, 0, 50, true, buf);//UPC-E 2 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_E_5_Digit_Add_on, 3, 0, 50, false, buf);//UPC-E 5 digit add-on
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.UPC_E_5_Digit_Add_on, 3, 0, 50, true, buf);//UPC-E 5 digit add-on
                    
                    PPLBEmulation.BarcodeUtil.PrintPDF417(50, 50, PPLBOrient.Clockwise_0_Degrees, 400, 300, 1,
                        PPLBPDF417CompressionMode.Auto_Encoding, 4, 10, 0, 0, false, buf);// PDF417, normal
                    PPLBEmulation.BarcodeUtil.PrintPDF417(50, 50, PPLBOrient.Clockwise_0_Degrees, 400, 300, 1,
                        PPLBPDF417CompressionMode.Auto_Encoding, 4, 10, 1, 30, true, buf);// PDF417, truncate
                    PPLBEmulation.BarcodeUtil.PrintAztecCode(50, 50, 3, 0, false, buf);// Aztec Code
                    PPLBEmulation.BarcodeUtil.PrintAztecCode(50, 50, 3, 0, true, buf);// Aztec Code, reverse Image
                    buf3 = encoder.GetBytes("[)>\x001E01\x001D961Z00004951\x001DUPSN\x001D06X610\x001D279\x001D\x001D1/1\x001D10\x001DN\x001D\x001DSEATTLE\x001DWA\x001E\x0004");
                    PPLBEmulation.BarcodeUtil.PrintMaxiCode(50, 50, PPLBMaxiCodeMode.Mode_2, 1, 840, "511470000", buf3);// MaxiCode, mode 2
                    buf4 = encoder.GetBytes("[)>\x001E01\x001D961Z00004951\x001DUPSN\x001D06X610\x001D2017/10/11\x001D\x001D1/1\x001D10\x001DN\x001D\x001DSEATTLE\x001D\x001E\x0004");
                    PPLBEmulation.BarcodeUtil.PrintMaxiCode(50, 50, PPLBMaxiCodeMode.Mode_3, 1, 276, "123456", buf4);// MaxiCode, mode 3
                    PPLBEmulation.BarcodeUtil.PrintQRCode(50, 50, PPLBQRCodeModel.Model_2, 3, PPLBQRCodeErrCorrect.Standard, buf);// QR Code
                    buf3 = encoder.GetBytes("0061414199999|RSS test");
                    PPLBEmulation.BarcodeUtil.PrintRSS(50, 50, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_14, 3, 30, 0, false, buf3);// RSS, RSS-14
                    PPLBEmulation.BarcodeUtil.PrintRSS(50, 50, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_Truncated, 3, 30, 0, false, buf3);// RSS, RSS Truncated
                    PPLBEmulation.BarcodeUtil.PrintRSS(50, 50, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_Stacked, 3, 30, 0, false, buf3);// RSS, RSS Stacked
                    PPLBEmulation.BarcodeUtil.PrintRSS(50, 50, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_Stacked_Omnidirectional, 3, 30, 0, false, buf3);// RSS, RSS Stacked Omnidirectional
                    PPLBEmulation.BarcodeUtil.PrintRSS(50, 50, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_Limited, 3, 30, 0, true, buf3);// RSS, RSS Limited
                    PPLBEmulation.BarcodeUtil.PrintRSS(50, 50, PPLBOrient.Clockwise_0_Degrees, PPLBRSSType.RSS_Expanded, 3, 30, 4, true, buf3);// RSS, RSS Expanded
                    PPLBEmulation.BarcodeUtil.PrintDataMatrix(50, 110, 0, 0, 10, false, buf);// Data Matrix
                    PPLBEmulation.BarcodeUtil.PrintDataMatrix(400, 110, 0, 0, 10, true, buf);// Data Matrix, reverse Image

                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_barcode2", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// FormUtil 1 : form function.
        /// Test methods of FormUtil class.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_form1(int printcount)
        {
            byte[] buf;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                //Label 1: Create a Form and print it.
                //Label 2: Delete a Form and print it. But delete Form first, so cannot print.
                if (false == __createPrn("PPLB_form1.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage.
                    PPLBEmulation.FormUtil.DeleteStoreForm("form1");
                    PPLBEmulation.FormUtil.StoreFormStart("form1");//start form.
                    PPLBEmulation.GraphicsUtil.PrintBox(30, 50, 100, 200, 5);
                    PPLBEmulation.FormUtil.StoreFormEnd();//end form.
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    PPLBEmulation.FormUtil.PrintStoreForm("form1");//print form.
                    buf = encoder.GetBytes("Label 1: recall form object");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    PPLBEmulation.FormUtil.DeleteStoreForm("form1");
                    PPLBEmulation.FormUtil.PrintStoreForm("form1");//print form.
                    buf = encoder.GetBytes("Label 2: delete first, then recall form object");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage, set to 'dram' is better if you have set it.
                    //The output data.
                    //It will output two labels data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_form1", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// FormUtil 2 : form function.
        /// Test methods of FormUtil class.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_form2(int printcount)
        {
            byte[] buf;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                //Creat double Form that the same name and print. The new Form cannot be saved, so you will print first from.
                //Then check the result that Form Yes or No to print.
                if (false == __createPrn("PPLB_form2.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage.
                    PPLBEmulation.FormUtil.DeleteStoreForm("form1");
                    PPLBEmulation.FormUtil.StoreFormStart("form1");//start form.
                    buf = encoder.GetBytes("This is first Form.");
                    PPLBEmulation.TextUtil.PrintText(30, 50, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.FormUtil.StoreFormEnd();//end form.
                    PPLBEmulation.FormUtil.StoreFormStart("form1");//start form.
                    PPLBEmulation.GraphicsUtil.PrintBox(30, 50, 100, 200, 5);
                    PPLBEmulation.FormUtil.StoreFormEnd();//end form.
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    PPLBEmulation.FormUtil.PrintStoreForm("form1");//print form.
                    buf = encoder.GetBytes("Label: recall form object the last that print string");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage, set to 'dram' is better if you have set it.
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_form2", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// Variabe 1 : Variabe function.
        /// Test methods of FormUtil class.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_variable1(int printcount)
        {
            byte[] buf;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                if (false == __createPrn("PPLB_variable1.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    string variablename0 = "V00";
                    string variablename1 = "V01";
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage.
                    PPLBEmulation.FormUtil.DeleteStoreForm("form1");
                    PPLBEmulation.FormUtil.StoreFormStart("form1");//start form.
                    PPLBEmulation.FormUtil.SetVariable(variablename0, 5, PPLBFieldJustification.No_Justification, "Variable 00");//must be defined in order, and must be the next entried after StoreFormStart() function.
                    PPLBEmulation.FormUtil.SetVariable(variablename1, 10, PPLBFieldJustification.No_Justification, "Variable 01");
                    buf = encoder.GetBytes("Label: test form and variable command");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    buf = Encoding.ASCII.GetBytes("V00 string:");
                    PPLBEmulation.TextUtil.PrintText(30, 50, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.TextUtil.PrintText(250, 50, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, variablename0);
                    buf = Encoding.ASCII.GetBytes("V01 barcode:");
                    PPLBEmulation.TextUtil.PrintText(30, 100, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(250, 100, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_Auto_Mode, 3, 0, 50, true, variablename1);
                    PPLBEmulation.FormUtil.StoreFormEnd();//end form.
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    PPLBEmulation.FormUtil.PrintStoreForm("form1");//print form.
                    buf = encoder.GetBytes("abcde\r\n1234567890");//V00, V01
                    PPLBEmulation.FormUtil.SetDownloadVariable(buf);
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage, set to 'dram' is better if you have set it.
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_variable1", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        /// <summary>
        /// Counter 1 : Variabel function.
        /// Test methods of FormUtil class.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLB_counter1(int printcount)
        {
            byte[] buf;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                if (false == __createPrn("PPLB_counter1.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    string countername0 = "C0";
                    string countername1 = "C1";
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage.
                    PPLBEmulation.FormUtil.DeleteStoreForm("form1");
                    PPLBEmulation.FormUtil.StoreFormStart("form1");//start form.
                    PPLBEmulation.FormUtil.SetCounter(countername0, 5, PPLBFieldJustification.No_Justification, 1, "Counter 0");//must be defined in order, and must be the next entried after StoreFormStart() function.
                    PPLBEmulation.FormUtil.SetCounter(countername1, 10, PPLBFieldJustification.No_Justification, -1, "Counter 1");
                    buf = encoder.GetBytes("Label: test form and counter command");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    buf = Encoding.ASCII.GetBytes("C0 number:");
                    PPLBEmulation.TextUtil.PrintText(30, 50, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.TextUtil.PrintText(250, 50, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, countername0);
                    buf = Encoding.ASCII.GetBytes("C1 barcode:");
                    PPLBEmulation.TextUtil.PrintText(30, 100, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.BarcodeUtil.PrintOneDBarcode(250, 100, PPLBOrient.Clockwise_0_Degrees,
                        PPLBBarCodeType.Code_128_Auto_Mode, 3, 0, 50, true, countername1);
                    PPLBEmulation.FormUtil.StoreFormEnd();//end form.
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    PPLBEmulation.FormUtil.PrintStoreForm("form1");//print form.
                    buf = encoder.GetBytes("12345\r\n1234567890");//C0, C1
                    PPLBEmulation.FormUtil.SetDownloadVariable(buf);
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage, set to 'dram' is better if you have set it.
                    //The output data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_counter1", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        private void __testPPLB_clear(int printcount)
        {
            byte[] buf;
            string strgraphic1 = "";
            string strgraphic2 = "";
            Encoding encoder = Encoding.Default;

            //Label 1: print picture and Form.
            //Label 2: Clear storage that all object will be clear, then print picture and Form. But clear storage, so cannot print.
            OpenFileDialog filedlg = new OpenFileDialog();
            filedlg.InitialDirectory = strSelectFolder;
            filedlg.Filter = strGraphicFilter;
#if !(WindowsCE)//[.
            filedlg.Title = "Select one Graphic";
#endif//].
            if (DialogResult.OK == filedlg.ShowDialog())
            {
                strgraphic1 = filedlg.FileName;
                strSelectFolder = filedlg.FileName;
            }
#if !(WindowsCE)//[.
            filedlg.Title = "Select another Graphic";
#endif//].
            if (DialogResult.OK == filedlg.ShowDialog())
            {
                strgraphic2 = filedlg.FileName;
                strSelectFolder = filedlg.FileName;
            }

            int index = -1;
            do
            {
                if (false == __createPrn("PPLB_clear.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage.
                    PPLBEmulation.FormUtil.DeleteStoreForm("form1");
                    PPLBEmulation.FormUtil.StoreFormStart("form1");//start form.
                    PPLBEmulation.GraphicsUtil.PrintBox(30, 50, 100, 200, 5);
                    PPLBEmulation.FormUtil.StoreFormEnd();//end form.
                    PPLBEmulation.GraphicsUtil.StoreGraphic(strgraphic1, "graphic1");
                    PPLBEmulation.GraphicsUtil.StoreGraphic(strgraphic2, "graphic2");
                    PPLBEmulation.SetUtil.SetHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    PPLBEmulation.FormUtil.PrintStoreForm("form1");//print form.
                    buf = encoder.GetBytes("Label 1: recall graphic and form object");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.GraphicsUtil.PrintStoreGraphic(150, 50, "graphic1");
                    PPLBEmulation.GraphicsUtil.PrintStoreGraphic(150, 250, "graphic2");
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    PPLBEmulation.FormUtil.DeleteStoreForm("*");
                    PPLBEmulation.GraphicsUtil.DeleteStoreGraphic("*");
                    PPLBEmulation.SetUtil.SetClearImageBuffer();
                    PPLBEmulation.FormUtil.PrintStoreForm("form1");//print form.
                    buf = encoder.GetBytes("Label 2: clear storage first, then recall graphic and form object");
                    PPLBEmulation.TextUtil.PrintText(30, 0, PPLBOrient.Clockwise_0_Degrees, PPLBFont.Font_2, 1, 1, false, buf);
                    PPLBEmulation.GraphicsUtil.PrintStoreGraphic(150, 50, "graphic1");
                    PPLBEmulation.GraphicsUtil.PrintStoreGraphic(150, 250, "graphic2");
                    //set print conditions.
                    PPLBEmulation.SetUtil.SetPrintOut(printcount, 1);
                    PPLBEmulation.SetUtil.SetStorage(PPLBStorage.Dram);//storage, set to 'dram' is better if you have set it.
                    //The output data.
                    //It will output two labels data.
                    PPLBEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLB_clear", ex);
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

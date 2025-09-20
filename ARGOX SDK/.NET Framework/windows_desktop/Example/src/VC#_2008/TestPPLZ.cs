using System;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using BarcodePrinter_API;
using BarcodePrinter_API.Comm;
using BarcodePrinter_API.Emulation.PPLZ;

namespace VCSharp_2008
{
    public partial class Form1 : Form
    {
        #region Variables

        /// <summary>
        /// This is emulation reference.
        /// </summary>
        PPLZ PPLZEmulation;

        #endregion

        #region TestPPLZItem

        /// <summary>
        /// Media Calibration: test calibrate command.
        /// Only test SetMediaCalibration() method.
        /// </summary>
        /// <param name="printcount">[in]print label count.</param>
        private void __testPPLZ_calibrate(int printcount)
        {
            int index = -1;
            do
            {
                if (false == __createPrn("PPLZ_calibrate.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLZEmulation.SetUtil.SetMediaCalibration();
                    //The output data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLZ_calibrate", ex);
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
        private void __testPPLZ_set1(int printcount)
        {
            int index = -1;
            do
            {
                //create a connection.
                if (false == __createPrn("PPLZ_set1.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLZEmulation.SetUtil.SetReset();
                    //The output data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLZ_set1", ex);
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
        private void __testPPLZ_set2(int printcount)
        {
            Encoding encoder = Encoding.Default;
            byte[] buf;

            int index = -1;
            do
            {
                //create a connection.
                if (false == __createPrn("PPLZ_set2.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    //The IOUtil.AppendData() method will append data to buffer area.
                    //Because you can append any data at any time, so the method is very powerful.
                    //When you want to use this method, you must to read and find from the programing guide.
                    buf = encoder.GetBytes("Test setting function\r\nStart\r\n");
                    PPLZEmulation.IOUtil.AppendData(buf, 0, buf.Length);
                    PPLZEmulation.SetUtil.SetSerial(9600, SerialParity.None, 8, SerialStopBits.One);
                    PPLZEmulation.SetUtil.SetUnit(PPLZUnitType.Dot);
                    PPLZEmulation.SetUtil.SetMediaTrack(PPLZMediaTrack.Non_Continuous_Mdeia_Web_Sensing);
                    PPLZEmulation.SetUtil.SetOrientation(false);
                    PPLZEmulation.SetUtil.SetMirror(false);
                    PPLZEmulation.SetUtil.SetPrintMode(PPLZPrintMode.Tear_Off);
                    PPLZEmulation.SetUtil.SetMediaType(PPLZMediaType.Direct_Thermal_Media);
                    PPLZEmulation.SetUtil.SetBackfeed(0);
                    PPLZEmulation.SetUtil.SetDarkness(0);
                    PPLZEmulation.SetUtil.SetPrintRate(3, 3, 3);
                    PPLZEmulation.SetUtil.SetHomePosition(5, 5);
                    PPLZEmulation.SetUtil.SetLabelLength(203 * 3);
                    PPLZEmulation.SetUtil.SetPrintWidth(203 * 4);
                    buf = encoder.GetBytes("End\r\n");
                    PPLZEmulation.IOUtil.AppendData(buf, 0, buf.Length);
                    //The output data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLZ_set2", ex);
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
        private void __testPPLZ_draw1(int printcount)
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
                if (false == __createPrn("PPLZ_draw1.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLZEmulation.SetUtil.SetUnit(PPLZUnitType.Dot);
                    PPLZEmulation.SetUtil.SetMediaTrack(PPLZMediaTrack.Non_Continuous_Mdeia_Web_Sensing);
                    PPLZEmulation.SetUtil.SetOrientation(false);
                    PPLZEmulation.SetUtil.SetMirror(false);
                    PPLZEmulation.SetUtil.SetPrintMode(PPLZPrintMode.Tear_Off);
                    PPLZEmulation.SetUtil.SetMediaType(PPLZMediaType.Direct_Thermal_Media);
                    buf = encoder.GetBytes("Label 1: draw graphic using graphic id");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    PPLZEmulation.GraphicsUtil.StoreGraphic(strgraphic1, PPLZStorage.Dram, "graphic");
                    PPLZEmulation.GraphicsUtil.PrintStoreGraphic(30, 50, PPLZStorage.Dram, "graphic", 1, 1);
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //call methods that you want.
                    buf = encoder.GetBytes("Label 2: draw graphic direct");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    PPLZEmulation.GraphicsUtil.PrintGraphic(30, 50, strgraphic1, PPLZDataFormat.Base64);
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //call methods that you want.
                    buf = encoder.GetBytes("Label 3: draw graphic object direct");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    Bitmap bitmap = new Bitmap(strgraphic1);
                    PPLZEmulation.GraphicsUtil.PrintGraphic(30, 50, bitmap, PPLZDataFormat.Base64_LZ77);
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //The output data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLZ_draw1", ex);
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
        private void __testPPLZ_draw2(int printcount)
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
                if (false == __createPrn("PPLZ_draw2.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLZEmulation.SetUtil.SetUnit(PPLZUnitType.Dot);
                    PPLZEmulation.SetUtil.SetMediaTrack(PPLZMediaTrack.Non_Continuous_Mdeia_Web_Sensing);
                    PPLZEmulation.SetUtil.SetOrientation(false);
                    PPLZEmulation.SetUtil.SetMirror(false);
                    PPLZEmulation.SetUtil.SetPrintMode(PPLZPrintMode.Tear_Off);
                    PPLZEmulation.SetUtil.SetMediaType(PPLZMediaType.Direct_Thermal_Media);
                    buf = encoder.GetBytes("Label 1: draw line, rectangle and graphic");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    PPLZEmulation.GraphicsUtil.PrintLine(30, 50, 50, 100);
                    PPLZEmulation.GraphicsUtil.PrintBox(150, 50, 200, 100, 10);
                    PPLZEmulation.GraphicsUtil.StoreGraphic(strgraphic1, PPLZStorage.Dram, "graphic");
                    PPLZEmulation.GraphicsUtil.PrintStoreGraphic(30, 180, PPLZStorage.Dram, "graphic", 1, 1);
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //The output data.
                    PPLZEmulation.IOUtil.PrintOut();
                    buf = encoder.GetBytes("Label 2: recall graphic object and the size is double");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    PPLZEmulation.GraphicsUtil.PrintStoreGraphic(30, 180, PPLZStorage.Dram, "graphic", 2, 2);
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //The output data.
                    PPLZEmulation.IOUtil.PrintOut();
                    buf = encoder.GetBytes("Label 3: delete graphic object");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    PPLZEmulation.GraphicsUtil.DeleteStoreGraphic(PPLZStorage.Dram, "graphic");
                    PPLZEmulation.GraphicsUtil.PrintStoreGraphic(30, 180, PPLZStorage.Dram, "graphic", 2, 2);
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //The output data.
                    //In fact, you can only call once PrintOut() to output buffer data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLZ_draw2", ex);
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
        private void __testPPLZ_text1(int printcount)
        {
            byte[] buf, buf2;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                if (false == __createPrn("PPLZ_text1.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLZEmulation.SetUtil.SetUnit(PPLZUnitType.Dot);
                    PPLZEmulation.SetUtil.SetMediaTrack(PPLZMediaTrack.Non_Continuous_Mdeia_Web_Sensing);
                    PPLZEmulation.SetUtil.SetOrientation(false);
                    PPLZEmulation.SetUtil.SetMirror(false);
                    PPLZEmulation.SetUtil.SetPrintMode(PPLZPrintMode.Tear_Off);
                    PPLZEmulation.SetUtil.SetMediaType(PPLZMediaType.Direct_Thermal_Media);
                    buf = encoder.GetBytes("Label: print internal font");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    buf = encoder.GetBytes("Font  text     Font  text     Font  text     Font  text");
                    PPLZEmulation.TextUtil.PrintText(30, 50, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    buf = encoder.GetBytes("12345");
                    //row 1.
                    buf2 = encoder.GetBytes("0");
                    PPLZEmulation.TextUtil.PrintText(30, 150, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 20, 150, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 50, 50, buf, 1);
                    buf2 = encoder.GetBytes("A");
                    PPLZEmulation.TextUtil.PrintText(30 + 170, 150, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 190, 150, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_A, 5, 9, buf, 1);
                    buf2 = encoder.GetBytes("B");
                    PPLZEmulation.TextUtil.PrintText(30 + 340, 150, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 360, 150, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_B, 7, 11, buf, 1);
                    buf2 = encoder.GetBytes("C");
                    PPLZEmulation.TextUtil.PrintText(30 + 510, 150, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 530, 150, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_C, 10, 18, buf, 1);

                    //row 2.
                    buf2 = encoder.GetBytes("D");
                    PPLZEmulation.TextUtil.PrintText(30, 250, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 20, 250, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_D, 10, 18, buf, 1);
                    buf2 = encoder.GetBytes("E");
                    PPLZEmulation.TextUtil.PrintText(30 + 170, 250, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 190, 250, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_E, 15, 28, buf, 1);
                    buf2 = encoder.GetBytes("F");
                    PPLZEmulation.TextUtil.PrintText(30 + 340, 250, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 360, 250, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_F, 13, 26, buf, 1);
                    buf2 = encoder.GetBytes("G");
                    PPLZEmulation.TextUtil.PrintText(30 + 510, 250, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 530, 250, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_G, 40, 60, buf, 1);

                    //row 3.
                    buf2 = encoder.GetBytes("H");
                    PPLZEmulation.TextUtil.PrintText(30, 350, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 20, 350, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_H, 13, 21, buf, 1);
                    buf2 = encoder.GetBytes("P");
                    PPLZEmulation.TextUtil.PrintText(30 + 170, 350, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 190, 350, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_P, 50, 50, buf, 1);
                    buf2 = encoder.GetBytes("Q");
                    PPLZEmulation.TextUtil.PrintText(30 + 340, 350, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 360, 350, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Q, 50, 50, buf, 1);
                    buf2 = encoder.GetBytes("R");
                    PPLZEmulation.TextUtil.PrintText(30 + 510, 350, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 530, 350, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_R, 50, 50, buf, 1);

                    //row 4.
                    buf2 = encoder.GetBytes("S");
                    PPLZEmulation.TextUtil.PrintText(30, 450, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 20, 450, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_S, 50, 50, buf, 1);
                    buf2 = encoder.GetBytes("T");
                    PPLZEmulation.TextUtil.PrintText(30 + 170, 450, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 190, 450, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_T, 50, 50, buf, 1);
                    buf2 = encoder.GetBytes("U");
                    PPLZEmulation.TextUtil.PrintText(30 + 340, 450, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 360, 450, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_U, 50, 50, buf, 1);
                    buf2 = encoder.GetBytes("V");
                    PPLZEmulation.TextUtil.PrintText(30 + 510, 450, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                    PPLZEmulation.TextUtil.PrintText(30 + 530, 450, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_V, 50, 50, buf, 1);
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //The output data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLZ_text1", ex);
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
        private void __testPPLZ_text2(int printcount)
        {
            byte[] buf;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                if (false == __createPrn("PPLZ_text2.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLZEmulation.SetUtil.SetUnit(PPLZUnitType.Dot);
                    PPLZEmulation.SetUtil.SetMediaTrack(PPLZMediaTrack.Non_Continuous_Mdeia_Web_Sensing);
                    PPLZEmulation.SetUtil.SetOrientation(false);
                    PPLZEmulation.SetUtil.SetMirror(false);
                    PPLZEmulation.SetUtil.SetPrintMode(PPLZPrintMode.Tear_Off);
                    PPLZEmulation.SetUtil.SetMediaType(PPLZMediaType.Direct_Thermal_Media);
                    buf = encoder.GetBytes("Label: print true type font using graphic id");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    PPLZEmulation.TextUtil.StoreTextGraphic("細明體", 20, true, true, true, true, PPLZStorage.Dram, "text1", "ARGOX 1996 成立");
                    PPLZEmulation.TextUtil.StoreTextGraphic("細明體", 20, true, true, true, true, PPLZStorage.Dram, "text2", "ARGOX 1996 成立", PPLZDataFormat.Hex, PPLZOrient.Clockwise_90_Degrees, 0);
                    PPLZEmulation.GraphicsUtil.PrintStoreGraphic(100, 50, PPLZStorage.Dram, "text1", 2, 2);
                    PPLZEmulation.GraphicsUtil.PrintStoreGraphic(30, 50, PPLZStorage.Dram, "text2", 1, 1);
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //call methods that you want.
                    buf = encoder.GetBytes("Label 2: print true type font direct");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    PPLZEmulation.TextUtil.PrintTextGraphic(30, 50, "細明體", 16, false, false, false, false, "繁體字(Hex)", PPLZDataFormat.Hex, PPLZOrient.Clockwise_0_Degrees, 0);
                    PPLZEmulation.TextUtil.PrintTextGraphic(30, 100, "細明體", 16, false, false, false, false, "(Hex_Compressed)", PPLZDataFormat.Hex_Compressed, PPLZOrient.Clockwise_180_Degrees, 400);
                    PPLZEmulation.TextUtil.PrintTextGraphic(30, 150, "細明體", 32, false, false, false, false, "简体字(Base64)", PPLZDataFormat.Base64, PPLZOrient.Clockwise_0_Degrees, 0);
                    PPLZEmulation.TextUtil.PrintTextGraphic(30, 200, "細明體", 32, false, false, false, false, "(Base64_LZ77)", PPLZDataFormat.Base64_LZ77, PPLZOrient.Clockwise_180_Degrees, 400);
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //The output data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLZ_text2", ex);
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
        private void __testPPLZ_barcode1(int printcount)
        {
            byte[] buf, buf2;
            byte[] buf3, buf4;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                if (false == __createPrn("PPLZ_barcode1_" + comboBox_barcode.Text + ".txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLZEmulation.SetUtil.SetUnit(PPLZUnitType.Dot);
                    PPLZEmulation.SetUtil.SetMediaTrack(PPLZMediaTrack.Non_Continuous_Mdeia_Web_Sensing);
                    PPLZEmulation.SetUtil.SetOrientation(false);
                    PPLZEmulation.SetUtil.SetMirror(false);
                    PPLZEmulation.SetUtil.SetPrintMode(PPLZPrintMode.Tear_Off);
                    PPLZEmulation.SetUtil.SetMediaType(PPLZMediaType.Direct_Thermal_Media);
                    buf = encoder.GetBytes("Label: one barcode");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    buf = encoder.GetBytes(comboBox_barcode.Text);
                    PPLZEmulation.TextUtil.PrintText(50, 50, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    buf = encoder.GetBytes("23456");
                    switch (comboBox_barcode.Text)
                    {
                        case "Code 11":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.Code_11, 1, buf, 'N', 'Y', 'N', 'N', 'N');// Code 11
                            break;
                        case "Code 39":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.Code_39, 1, buf, 'N', 'Y', 'N', 'N', 'N');// Code 39
                            break;
                        case "Plessey":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.Plessey, 1, buf, 'N', 'Y', 'N', 'N', 'N');// Plessey
                            break;
                        case "Interleaved 2 of 5":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.Interleaved_2_of_5, 1, buf, 'Y', 'N', 'N', 'N', 'N');// Interleaved 2 of 5
                            break;
                        case "UPC-E":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.UPC_E, 1, buf, 'Y', 'N', 'N', 'N', 'N');// UPC-E
                            break;
                        case "Code 93":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.Code_93, 1, buf, 'Y', 'N', 'N', 'N', 'N');// Code 93
                            break;
                        case "UPC-A":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.UPC_A, 1, buf, 'Y', 'N', 'N', 'N', 'N');// UPC-A
                            break;
                        case "EAN-8":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.EAN_8, 1, buf, 'Y', 'N', 'N', 'N', 'N');// EAN-8
                            break;
                        case "EAN-13":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.EAN_13, 1, buf, 'Y', 'N', 'N', 'N', 'N');// EAN-13
                            break;
                        case "Industrial 2 of 5":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.Industrial_2_of_5, 1, buf, 'Y', 'N', 'N', 'N', 'N');// Industrial 2 of 5
                            break;
                        case "Standard 2 of 5":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.Standard_2_of_5, 1, buf, 'Y', 'N', 'N', 'N', 'N');// Standard 2 of 5
                            break;
                        case "UPC/EAN Extensions":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.UPC_EAN_Extensions, 1, buf, 'Y', 'N', 'N', 'N', 'N');// UPC/EAN Extensions
                            break;
                        case "USPS POSTNET":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 2, 13, 50,
                                PPLZBarCodeType.POSTAL, 1, buf, 'Y', 'N', '0', 'N', 'N');// USPS POSTNET
                            break;
                        case "USPS Planet Code":
                            buf2 = encoder.GetBytes("000113456789");
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 2, 13, 50,
                                PPLZBarCodeType.Planet_Code, 1, buf2, 'Y', 'N', 'N', 'N', 'N');// Planet Code
                            // equal to PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 2, 13, 50,
                            //    PPLZBarCodeType.POSTAL, 1, buf2, 'Y', 'N', '1', 'N', 'N');// USPS Planet Code
                            break;
                        case "USPS Intelligent Mail":
                            buf2 = encoder.GetBytes("01134567890123456789");
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 2, 13, 50,
                                PPLZBarCodeType.POSTAL, 1, buf2, 'Y', 'N', '3', 'N', 'N');// USPS Intelligent Mail
                            break;
                        case "Code 128":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.Code_128, 1, buf, 'Y', 'N', 'N', 'N', 'N');// Code 128
                            buf2 = encoder.GetBytes("Subsets A(Numeric Pairs give Alpha/Numerics)");
                            PPLZEmulation.TextUtil.PrintText(50, 180, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                            buf2 = encoder.GetBytes(">9123$%&");
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 210, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.Code_128, 1, buf2, 'Y', 'N', 'N', 'N', 'N');// Code 128, Subsets A(Numeric Pairs give Alpha/Numerics)
                            buf2 = encoder.GetBytes("Subsets B(Normal Alpha/Numeric)");
                            PPLZEmulation.TextUtil.PrintText(50, 310, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                            buf2 = encoder.GetBytes(">:ABC123");
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 340, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.Code_128, 1, buf2, 'Y', 'N', 'N', 'N', 'N');// Code 128, Subsets B(Normal Alpha/Numeric)
                            buf2 = encoder.GetBytes("Subsets C(All numeric (00 - 99))");
                            PPLZEmulation.TextUtil.PrintText(50, 440, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                            buf2 = encoder.GetBytes(">;123456");
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 470, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.Code_128, 1, buf2, 'Y', 'N', 'N', 'N', 'N');// Code 128, Subsets C(All numeric (00 - 99))
                            break;
                        case "ANSI Codabar":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.ANSI_Codabar, 1, buf, 'N', 'Y', 'N', 'A', 'A');// ANSI Codabar
                            break;
                        case "LOGMARS":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.LOGMARS, 1, buf, 'N', 'N', 'N', 'N', 'N');// LOGMARS
                            break;
                        case "MSI":
                            PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                                PPLZBarCodeType.MSI, 1, buf, 'B', 'Y', 'N', 'N', 'N');// MSI
                            break;
                        case "PDF417":
                            PPLZEmulation.BarcodeUtil.PrintPDF417(50, 80, PPLZOrient.Clockwise_0_Degrees, 10, 2, 1, 0, false, 4, buf, 1);// PDF417
                            break;
                        case "MicroPDF417":
                            PPLZEmulation.BarcodeUtil.PrintMicroPDF417(50, 80, PPLZOrient.Clockwise_0_Degrees, 10, 0, 4, buf, 1);// MicroPDF417
                            break;
                        case "Aztec Code":
                            PPLZEmulation.BarcodeUtil.PrintAztecCode(50, 80, PPLZOrient.Clockwise_0_Degrees, 3, 0, buf, 1);// Aztec Code
                            break;
                        case "MaxiCode":
                            buf2 = encoder.GetBytes("mode 2");
                            PPLZEmulation.TextUtil.PrintText(50, 80, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                            buf3 = encoder.GetBytes(@"[)>_1E01_1D961Z00004951_1DUPSN_1D06X610_1D279_1D_1D1/1_1D10_1DN_1D_1DSEATTLE_1DWA_1E_04");
                            PPLZEmulation.SetUtil.SetHexIndicator('_');//set Field Hexadecimal Indicator, must set.
                            PPLZEmulation.BarcodeUtil.PrintMaxiCode(50, 110, PPLZMaxiCodeMode.Mode_2, 1, 840, "511470000", buf3, 0);// MaxiCode, mode 2
                            buf2 = encoder.GetBytes("mode 3");
                            PPLZEmulation.TextUtil.PrintText(400, 80, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf2, 0);
                            buf4 = encoder.GetBytes(@"[)>\1E01\1D961Z00004951\1DUPSN\1D06X610\1D2017/10/11\1D\1D1/1\1D10\1DN\1D\1DSEATTLE\1D\1E\04");
                            PPLZEmulation.SetUtil.SetHexIndicator('\\');//set Field Hexadecimal Indicator, must set.
                            PPLZEmulation.BarcodeUtil.PrintMaxiCode(400, 110, PPLZMaxiCodeMode.Mode_3, 1, 276, "123456", buf4, 0);// MaxiCode, mode 3
                            break;
                        case "QR Code":
                            PPLZEmulation.BarcodeUtil.PrintQRCode(50, 80, PPLZQRCodeModel.Model_2, 3, PPLZQRCodeErrCorrect.Standard, buf, 1);// QR Code
                            break;
                        case "RSS":
                            buf3 = encoder.GetBytes("0061414199999|RSS test");
                            PPLZEmulation.BarcodeUtil.PrintRSS(50, 80, PPLZOrient.Clockwise_0_Degrees, PPLZRSSType.RSS_14, 3, 20, 22, buf3);// RSS
                            break;
                        case "Data Matrix":
                            PPLZEmulation.BarcodeUtil.PrintDataMatrix(50, 80, PPLZOrient.Clockwise_0_Degrees, 10, 0, 0, buf, 1);// Data Matrix
                            break;
                        default:
                            break;
                    }
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //The output data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name,"__testPPLZ_barcode1", ex);
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
        private void __testPPLZ_barcode2(int printcount)
        {
            byte[] buf, buf2;
            byte[] buf3, buf4;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                if (false == __createPrn("PPLZ_barcode2.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLZEmulation.SetUtil.SetUnit(PPLZUnitType.Dot);
                    PPLZEmulation.SetUtil.SetMediaTrack(PPLZMediaTrack.Non_Continuous_Mdeia_Web_Sensing);
                    PPLZEmulation.SetUtil.SetOrientation(false);
                    PPLZEmulation.SetUtil.SetMirror(false);
                    PPLZEmulation.SetUtil.SetPrintMode(PPLZPrintMode.Tear_Off);
                    PPLZEmulation.SetUtil.SetMediaType(PPLZMediaType.Direct_Thermal_Media);
                    buf = encoder.GetBytes("Label: all barcode");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    buf = encoder.GetBytes("23456");
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.Code_11, 1, buf, 'N', 'Y', 'N', 'N', 'N');// Code 11
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.Code_39, 1, buf, 'N', 'Y', 'N', 'N', 'N');// Code 39
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.Plessey, 1, buf, 'N', 'Y', 'N', 'N', 'N');// Plessey

                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.Interleaved_2_of_5, 1, buf, 'Y', 'N', 'N', 'N', 'N');// Interleaved 2 of 5
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.UPC_E, 1, buf, 'Y', 'N', 'N', 'N', 'N');// UPC-E
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.Code_93, 1, buf, 'Y', 'N', 'N', 'N', 'N');// Code 93
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.UPC_A, 1, buf, 'Y', 'N', 'N', 'N', 'N');// UPC-A

                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.EAN_8, 1, buf, 'Y', 'N', 'N', 'N', 'N');// EAN-8
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.EAN_13, 1, buf, 'Y', 'N', 'N', 'N', 'N');// EAN-13
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.Industrial_2_of_5, 1, buf, 'Y', 'N', 'N', 'N', 'N');// Industrial 2 of 5
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.Standard_2_of_5, 1, buf, 'Y', 'N', 'N', 'N', 'N');// Standard 2 of 5
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.UPC_EAN_Extensions, 1, buf, 'Y', 'N', 'N', 'N', 'N');// UPC/EAN Extensions
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 2, 13, 50,
                        PPLZBarCodeType.POSTAL, 1, buf, 'Y', 'N', '0', 'N', 'N');// USPS POSTNET
                    buf2 = encoder.GetBytes("000123456789");
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 2, 13, 50,
                        PPLZBarCodeType.Planet_Code, 1, buf2, 'Y', 'N', 'N', 'N', 'N');// USPS Planet Code
                    // equal to PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 80, PPLZOrient.Clockwise_0_Degrees, 2, 13, 50,
                    //    PPLZBarCodeType.POSTAL, 1, buf2, 'Y', 'N', '1', 'N', 'N');// USPS Planet Code
                    buf2 = encoder.GetBytes("01234567890123456789");
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 2, 13, 50,
                        PPLZBarCodeType.POSTAL, 1, buf2, 'Y', 'N', '3', 'N', 'N');// USPS Intelligent Mail
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.Code_128, 1, buf, 'Y', 'N', 'N', 'N', 'N');// Code 128
                    buf2 = encoder.GetBytes(">9123$%&");
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.Code_128, 1, buf2, 'Y', 'N', 'N', 'N', 'N');// Code 128, Subsets A(Numeric Pairs give Alpha/Numerics)
                    buf2 = encoder.GetBytes(">:ABC123");
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.Code_128, 1, buf2, 'Y', 'N', 'N', 'N', 'N');// Code 128, Subsets B(Normal Alpha/Numeric)
                    buf2 = encoder.GetBytes(">;123456");
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.Code_128, 1, buf2, 'Y', 'N', 'N', 'N', 'N');// Code 128, Subsets C(All numeric (00 - 99))
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.ANSI_Codabar, 1, buf, 'N', 'Y', 'N', 'A', 'A');// ANSI Codabar
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.LOGMARS, 1, buf, 'N', 'N', 'N', 'N', 'N');// LOGMARS
                    PPLZEmulation.BarcodeUtil.PrintOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                        PPLZBarCodeType.MSI, 1, buf, 'B', 'Y', 'N', 'N', 'N');// MSI

                    PPLZEmulation.BarcodeUtil.PrintPDF417(50, 50, PPLZOrient.Clockwise_0_Degrees, 10, 2, 1, 0, false, 4, buf, 1);// PDF417
                    PPLZEmulation.BarcodeUtil.PrintMicroPDF417(50, 50, PPLZOrient.Clockwise_0_Degrees, 10, 0, 4, buf, 1);// MicroPDF417
                    PPLZEmulation.BarcodeUtil.PrintAztecCode(50, 50, PPLZOrient.Clockwise_0_Degrees, 3, 0, buf, 1);// Aztec Code
                    buf3 = encoder.GetBytes(@"[)>_1E01_1D961Z00004951_1DUPSN_1D6X6110_1D279_1D_1D1/1_1D10_1DN_1D_1DSEATTLE_1DWA_1E_04");
                    PPLZEmulation.SetUtil.SetHexIndicator('_');//set Field Hexadecimal Indicator, must set.
                    PPLZEmulation.BarcodeUtil.PrintMaxiCode(50, 50, PPLZMaxiCodeMode.Mode_2, 1, 840, "511470000", buf3, 0);// MaxiCode, mode 2
                    buf4 = encoder.GetBytes(@"[)>\1E01\1D961Z00004951\1DUPSN\1D06X610\1D2017/10/11\1D\1D1/1\1D10\1DN\1D\1DSEATTLE\1D\1E\04");
                    PPLZEmulation.SetUtil.SetHexIndicator('\\');//set Field Hexadecimal Indicator, must set.
                    PPLZEmulation.BarcodeUtil.PrintMaxiCode(50, 50, PPLZMaxiCodeMode.Mode_3, 1, 276, "123456", buf4, 0);// MaxiCode, mode 3
                    PPLZEmulation.BarcodeUtil.PrintQRCode(50, 50, PPLZQRCodeModel.Model_2, 3, PPLZQRCodeErrCorrect.Standard, buf, 1);// QR Code
                    buf3 = encoder.GetBytes("0061414199999|RSS test");
                    PPLZEmulation.BarcodeUtil.PrintRSS(50, 50, PPLZOrient.Clockwise_0_Degrees, PPLZRSSType.RSS_14, 3, 20, 22, buf3);// RSS
                    PPLZEmulation.BarcodeUtil.PrintDataMatrix(50, 50, PPLZOrient.Clockwise_0_Degrees, 10, 0, 0, buf, 1);// Data Matrix
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //The output data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLZ_barcode2", ex);
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
        private void __testPPLZ_format1(int printcount)
        {
            byte[] buf;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                //Label 1: Create a Format and print it.
                //Label 2: Delete a Format and print it. But delete Format first, so cannot print.
                if (false == __createPrn("PPLZ_format1.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLZEmulation.FormatUtil.StoreFormatStart(PPLZStorage.Dram, "format1");//start format.
                    PPLZEmulation.GraphicsUtil.PrintBox(30, 50, 100, 200, 5);
                    PPLZEmulation.FormatUtil.StoreFormatEnd();//end format.
                    PPLZEmulation.SetUtil.SetMediaType(PPLZMediaType.Direct_Thermal_Media);
                    buf = encoder.GetBytes("Label 1: recall format object");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    PPLZEmulation.FormatUtil.PrintStoreFormat(PPLZStorage.Dram, "format1");//print format.
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    buf = encoder.GetBytes("Label 2: delete first, then recall format object");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    PPLZEmulation.FormatUtil.DeleteStoreFormat(PPLZStorage.Dram, "format1");
                    PPLZEmulation.FormatUtil.PrintStoreFormat(PPLZStorage.Dram, "format1");//print format.
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //The output data.
                    //It will output two labels data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLZ_format1", ex);
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
        private void __testPPLZ_format2(int printcount)
        {
            byte[] buf;
            Encoding encoder = Encoding.Default;

            int index = -1;
            do
            {
                //Creat double Format that the same name and print. Then check the result that Format Yes or No to print.
                if (false == __createPrn("PPLZ_format2.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLZEmulation.FormatUtil.StoreFormatStart(PPLZStorage.Dram, "format1");//start format.
                    PPLZEmulation.GraphicsUtil.PrintBox(30, 50, 100, 200, 5);
                    PPLZEmulation.FormatUtil.StoreFormatEnd();//end format.
                    PPLZEmulation.FormatUtil.StoreFormatStart(PPLZStorage.Dram, "format1");//start format.
                    PPLZEmulation.GraphicsUtil.PrintLine(30, 50, 50, 100);
                    PPLZEmulation.FormatUtil.StoreFormatEnd();//end format.
                    PPLZEmulation.SetUtil.SetMediaType(PPLZMediaType.Direct_Thermal_Media);
                    buf = encoder.GetBytes("Label: recall format object the last that draw line");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    PPLZEmulation.FormatUtil.PrintStoreFormat(PPLZStorage.Dram, "format1");//print format.
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //The output data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLZ_format2", ex);
                }
                //Close the connection.
                //Notice: If you don't call BarcodePrinter.Connection.Close() method at here, maybe you don't close the connection.
                finally
                {
                    BarcodePrinter.Connection.Close(); // equal to fs.Close();
                }
            } while (true);
        }

        private void __testPPLZ_clear(int printcount)
        {
            byte[] buf;
            string strgraphic1 = "";
            string strgraphic2 = "";
            Encoding encoder = Encoding.Default;

            //Label 1: print picture and Format.
            //Label 2: Clear storage that all object will be clear, then print picture and Format. But clear storage, so cannot print.
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
                if (false == __createPrn("PPLZ_clear.txt", ++index))
                    break;

                try
                {
                    //call methods that you want.
                    PPLZEmulation.FormatUtil.StoreFormatStart(PPLZStorage.Dram, "format1");//start format.
                    PPLZEmulation.GraphicsUtil.PrintBox(30, 50, 100, 200, 5);
                    PPLZEmulation.FormatUtil.StoreFormatEnd();//end format.
                    PPLZEmulation.GraphicsUtil.StoreGraphic(strgraphic1, PPLZStorage.Dram, "graphic1");
                    PPLZEmulation.GraphicsUtil.StoreGraphic(strgraphic2, PPLZStorage.Dram, "graphic2");
                    PPLZEmulation.SetUtil.SetMediaType(PPLZMediaType.Direct_Thermal_Media);
                    buf = encoder.GetBytes("Label 1: recall graphic and format object");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    PPLZEmulation.GraphicsUtil.PrintStoreGraphic(150, 50, PPLZStorage.Dram, "graphic1", 1, 1);
                    PPLZEmulation.GraphicsUtil.PrintStoreGraphic(150, 250, PPLZStorage.Dram, "graphic2", 1, 1);
                    PPLZEmulation.FormatUtil.PrintStoreFormat(PPLZStorage.Dram, "format1");//print format.
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    buf = encoder.GetBytes("Label 2: clear storage first, then recall graphic and format object");
                    PPLZEmulation.TextUtil.PrintText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
                    //PPLZEmulation.SetUtil.ClearStore(PPLZStorage.Dram);
                    PPLZEmulation.FormatUtil.DeleteStoreFormat(PPLZStorage.Dram, "*");
                    PPLZEmulation.GraphicsUtil.DeleteStoreGraphic(PPLZStorage.Dram, "*");
                    PPLZEmulation.GraphicsUtil.PrintStoreGraphic(150, 50, PPLZStorage.Dram, "graphic1", 1, 1);
                    PPLZEmulation.GraphicsUtil.PrintStoreGraphic(150, 250, PPLZStorage.Dram, "graphic2", 1, 1);
                    PPLZEmulation.FormatUtil.PrintStoreFormat(PPLZStorage.Dram, "format1");//print format.
                    //set print conditions.
                    PPLZEmulation.SetUtil.SetPrintOut(printcount, 0, 1, false);
                    //The output data.
                    //It will output two labels data.
                    PPLZEmulation.IOUtil.PrintOut();
                }

                //exception.
                catch (Exception ex)
                {
                    ShowException.Show(this.Name, "__testPPLZ_clear", ex);
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

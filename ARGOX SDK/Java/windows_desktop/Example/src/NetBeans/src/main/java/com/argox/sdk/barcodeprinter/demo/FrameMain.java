/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.argox.sdk.barcodeprinter.demo;

import com.argox.sdk.barcodeprinter.BarcodePrinter;
import com.argox.sdk.barcodeprinter.connection.file.FileConnection;
import com.argox.sdk.barcodeprinter.connection.tcp.TCPConnection;
import com.argox.sdk.barcodeprinter.emulation.pplb.PPLB;
import com.argox.sdk.barcodeprinter.emulation.pplb.PPLBFont;
import com.argox.sdk.barcodeprinter.emulation.pplb.PPLBMediaType;
import com.argox.sdk.barcodeprinter.emulation.pplb.PPLBOrient;
import com.argox.sdk.barcodeprinter.emulation.pplb.PPLBPrintMode;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZ;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZDataFormat;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZFont;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZMediaType;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZOrient;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZStorage;
import java.awt.Graphics;
import java.awt.Image;
import java.awt.image.ImageObserver;
import java.awt.image.ImageProducer;


import java.io.IOException;
import java.io.Console;
import java.io.File;
import java.io.FileInputStream;
import javax.imageio.ImageIO;


/**
 *
 * @author kenke
 */
public class FrameMain {

    private static void pplbtest()
    {
        BarcodePrinter<FileConnection, PPLB> printer = new BarcodePrinter<FileConnection, PPLB>();
        //BarcodePrinter<TCPConnection, PPLB> printer = new BarcodePrinter<TCPConnection, PPLB>();
        printer.setEnabledLogger(true);
        try {
            //if you use BluetoothConnection instead of TCPConnection , you must implements onActivityResult(...) callback function
            //with calling printer.getConnection().onActivityResult(...)
            printer.setConnection(new FileConnection("pplb.txt"));
            //printer.setConnection(new TCPConnection("192.168.7.224", 9100));
            printer.setEmulation(new PPLB());

            printer.getConnection().open();
            String text = "Hello World!";
            //call methods that you want.
            //setting.
            printer.getEmulation().getSetUtil().setHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
            printer.getEmulation().getSetUtil().setOrientation(false);
            printer.getEmulation().getSetUtil().setClearImageBuffer();
            printer.getEmulation().getSetUtil().setClearImageBuffer();
            //data.
            printer.getEmulation().getTextUtil().printText(0, 0, PPLBOrient.Clockwise_0_Degrees,
                                PPLBFont.Font_1, 1, 1, false, text.getBytes());
            //printer.getEmulation().getGraphicsUtil().storeGraphic("image1.png", "test1");
            /* image test.
            printer.getEmulation().getGraphicsUtil().storeGraphic("image1.png", "test1", 492, 66);
            File file = new File("image1.png");
            java.awt.Image im = ImageIO.read(file);
            printer.getEmulation().getGraphicsUtil().storeGraphic(im, "test2", 492, 66);
            */
            printer.getEmulation().getTextUtil().storeTextGraphic("細明體", 20, false, false, false, false, "test3", "Argox 1975 成立");
            /* image test.
            printer.getEmulation().getGraphicsUtil().printStoreGraphic(0, 30, "test1");
            printer.getEmulation().getGraphicsUtil().printStoreGraphic(0, 130, "test2");
            */
            printer.getEmulation().getGraphicsUtil().printStoreGraphic(0, 230, "test3");
            //set print conditions.
            printer.getEmulation().getSetUtil().setPrintOut(1, 1);
            printer.getEmulation().printOut();
            printer.getConnection().close();
        } catch (Exception ex) {
            System.out.println(ex.getMessage());
            try {
                printer.getConnection().close();
            }
            catch (Exception e) {
                System.out.println(e.getMessage());
            }
            finally {
                //Toast.makeText(FrameTCPConnection.this, ex.toString(), Toast.LENGTH_LONG).show();
                //Log.e("argox_demo", null, ex);
            }
        }
    }
    
    private static void pplztest()
    {
        BarcodePrinter<FileConnection, PPLZ> printer = new BarcodePrinter<FileConnection, PPLZ>();
        printer.setEnabledLogger(true);
        try {
            //if you use BluetoothConnection instead of TCPConnection , you must implements onActivityResult(...) callback function
            //with calling printer.getConnection().onActivityResult(...)
            printer.setConnection(new FileConnection("pplz.txt"));
            printer.setEmulation(new PPLZ());

            printer.getConnection().open();
            String text = "Hello World!";
            //call methods that you want.
            //setting.
            printer.getEmulation().getSetUtil().setMediaType(PPLZMediaType.Direct_Thermal_Media);
            //data.
            printer.getEmulation().getTextUtil().printText(0, 0, PPLZOrient.Clockwise_0_Degrees,
                    PPLZFont.Font_Zero, 20, 20, text.getBytes(), 0);
            /* image test.
            printer.getEmulation().getGraphicsUtil().storeGraphic("image1.png", PPLZStorage.Dram, "test1", PPLZDataFormat.Base64_LZ77);
            File file = new File("image1.png");
            java.awt.Image im = ImageIO.read(file);
            printer.getEmulation().getGraphicsUtil().storeGraphic(im, PPLZStorage.Dram, "test2", PPLZDataFormat.Base64_LZ77);
            */
            printer.getEmulation().getTextUtil().storeTextGraphic("細明體", 20, false, false, false, false, PPLZStorage.Dram, "test3", text, PPLZDataFormat.Base64_LZ77, PPLZOrient.Clockwise_90_Degrees, 0);
            /* image test.
            printer.getEmulation().getGraphicsUtil().printStoreGraphic(0, 30, PPLZStorage.Dram, "test1", 1, 1);
            printer.getEmulation().getGraphicsUtil().printStoreGraphic(0, 130, PPLZStorage.Dram, "test2", 1, 1);
            */
            printer.getEmulation().getGraphicsUtil().printStoreGraphic(0, 230, PPLZStorage.Dram, "test3", 1, 1);
            //set print conditions.
            printer.getEmulation().getSetUtil().setPrintOut(1, 0, 1, false);
            printer.getEmulation().printOut();
            printer.getConnection().close();
        } catch (Exception ex) {
            System.out.println(ex.getMessage());
            try {
                printer.getConnection().close();
            }
            catch (Exception e) {
                System.out.println(e.getMessage());
            }
            finally {
                //Toast.makeText(FrameTCPConnection.this, ex.toString(), Toast.LENGTH_LONG).show();
                //Log.e("argox_demo", null, ex);
            }
        }
    }
    
    /**
     * @param args the command line arguments
     */
    public static void main(String[] args) {
        // TODO code application logic here
        System.out.println("start");
        pplbtest();
        pplztest();
        System.out.println("end");
    }
    
}

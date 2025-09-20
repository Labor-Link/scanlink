/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.argox.sdk.barcodeprinter.demo;

import android.app.Activity;
import android.content.SharedPreferences;
import android.graphics.Color;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.Toast;
import com.argox.sdk.barcodeprinter.BarcodePrinter;
import com.argox.sdk.barcodeprinter.connection.tcp.TCPConnection;
import com.argox.sdk.barcodeprinter.emulation.pplz.*;
import com.argox.sdk.barcodeprinter.emulation.pplb.*;
import grandroid.view.LayoutMaker;
import grandroid.view.ViewDesigner;

/**
 *
 * @author Rovers
 */
public class FrameTCPConnection extends Activity {

    private static final String TAG = FrameTCPConnection.class.getName();
    String ip;
    int port;
    boolean pplzenable;
    //use SharedPreferences to save the setting data.
    SharedPreferences preferences;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        //use SharedPreferences to save the setting data.
        preferences = getSharedPreferences("preferences", MODE_PRIVATE);
        pplzenable = preferences.getBoolean("PPLZ", true);

        Bundle b = getIntent().getExtras();
        ip = b.getString("IP");
        port = b.getInt("Port");

        //Layout.
        LayoutMaker maker = new LayoutMaker(this);
        maker.getLastLayout().setBackgroundColor(Color.WHITE);
        maker.setDrawableDesignWidth(this, 640);
        ViewDesigner vd = new ViewDesigner() {

            @Override
            public Button stylise(Button btn) {
                btn.setBackgroundResource(R.drawable.b1);
                btn.setTextColor(Color.BLACK);
                return btn;
            }

        };
        maker.setDesigner(vd);
        maker.addColLayout(false, maker.layFF());
        {
            maker.setScalablePadding(maker.getLastLayout(), 20, 25, 20, 25);
            maker.addTextView("Emulation: " + (pplzenable ? "PPLZ" : "PPLB"));
            maker.addTextView("IP: " + ip.toString());
            maker.addTextView("Port: " + port);
            Button btnShowCases = maker.addButton("Print \"Hello World\"");
            btnShowCases.setBackgroundResource(R.drawable.b1);
            btnShowCases.setLayoutParams(maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 100));
            btnShowCases.setOnClickListener(new View.OnClickListener() {

                public void onClick(View v) {
                    if (pplzenable) {
                        PPLZFunction();
                    }
                    else {
                        PPLBFunction();
                    }
                }
            });
            maker.escape();
        }
    }

    private void PPLZFunction() {

        //Notice
        //In the main thread, if you call write function of the network.
        //You will get android.os.NetworkOnMainThreadException exception.
        //This exception is thrown when an application attempts to perform a networking operation on its main thread.
        Thread t = new Thread(new Runnable() {

            public void run() {
                BarcodePrinter<TCPConnection, PPLZ> printer = new BarcodePrinter<TCPConnection, PPLZ>();

                try {
                    //if you use BluetoothConnection instead of TCPConnection , you must implements onActivityResult(...) callback function
                    //with calling printer.getConnection().onActivityResult(...)
                    printer.setConnection(new TCPConnection(ip, port));
                    printer.setEmulation(new PPLZ());
                    //printer.setEnabledLogger(true);//If you need logger file. Please enable this line code.

                    printer.getConnection().open();
                    String text = "Hello World!";
                    //call methods that you want.
                    //setting.
                    printer.getEmulation().getSetUtil().setUnit(PPLZUnitType.Dot);
                    printer.getEmulation().getSetUtil().setMediaTrack(PPLZMediaTrack.Non_Continuous_Mdeia_Web_Sensing);
                    printer.getEmulation().getSetUtil().setOrientation(false);
                    printer.getEmulation().getSetUtil().setMirror(false);
                    printer.getEmulation().getSetUtil().setPrintMode(PPLZPrintMode.Tear_Off);
                    printer.getEmulation().getSetUtil().setMediaType(PPLZMediaType.Direct_Thermal_Media);
                    //data.
                    printer.getEmulation().getTextUtil().printText(0, 0, PPLZOrient.Clockwise_0_Degrees,
                            PPLZFont.Font_Zero, 20, 20, text.getBytes(), 0);
                    //set print conditions.
                    printer.getEmulation().getSetUtil().setPrintOut(1, 0, 1, false);
                    printer.getEmulation().printOut();
                    printer.getConnection().close();
                } catch (Exception ex) {
                    try {
                        printer.getConnection().close();
                    }
                    catch (Exception e) {
                    }
                    finally {
                        Toast.makeText(FrameTCPConnection.this, ex.toString(), Toast.LENGTH_LONG).show();
                        Log.e(TAG, null, ex);
                    }
                }
            }
        });
        t.start();
    }

    private void PPLBFunction() {

        //Notice
        //In the main thread, if you call write function of the network.
        //You will get android.os.NetworkOnMainThreadException exception.
        //This exception is thrown when an application attempts to perform a networking operation on its main thread.
        Thread t = new Thread(new Runnable() {

            public void run() {
                BarcodePrinter<TCPConnection, PPLB> printer = new BarcodePrinter<TCPConnection, PPLB>();

                try {
                    //if you use BluetoothConnection instead of TCPConnection , you must implements onActivityResult(...) callback function
                    //with calling printer.getConnection().onActivityResult(...)
                    printer.setConnection(new TCPConnection(ip, port));
                    printer.setEmulation(new PPLB());
                    //printer.setEnabledLogger(true);//If you need logger file. Please enable this line code.

                    printer.getConnection().open();
                    String text = "Hello World!";
                    //call methods that you want.
                    //setting.
                    printer.getEmulation().getSetUtil().setOrientation(false);
                    printer.getEmulation().getSetUtil().setHomePosition(0, 0);
                    printer.getEmulation().getSetUtil().setHardwareOption(PPLBMediaType.Direct_Thermal_Media, PPLBPrintMode.Tear_Off, 0);
                    printer.getEmulation().getSetUtil().setStorage(PPLBStorage.Dram);//storage.
                    printer.getEmulation().getSetUtil().setClearImageBuffer();
                    //data
                    printer.getEmulation().getTextUtil().printText(10, 0, PPLBOrient.Clockwise_0_Degrees,
                            PPLBFont.Font_1, 1, 1, false, text.getBytes());
                    //set print conditions.
                    printer.getEmulation().getSetUtil().setPrintOut(1, 1);
                    printer.getEmulation().printOut();
                    printer.getConnection().close();
                } catch (Exception ex) {
                    try {
                        printer.getConnection().close();
                    } catch (Exception e) {
                    } finally {
                        Toast.makeText(FrameTCPConnection.this, ex.toString(), Toast.LENGTH_LONG).show();
                        Log.e(TAG, null, ex);
                    }
                }
            }
        });
        t.start();
    }
}


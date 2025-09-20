/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.argox.sdk.barcodeprinter.demo;

import android.app.Activity;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Color;
import android.graphics.Typeface;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbManager;
import android.os.Bundle;
import android.os.Environment;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;

import com.argox.sdk.barcodeprinter.BarcodePrinter;
import com.argox.sdk.barcodeprinter.connection.ConnectionState;
import com.argox.sdk.barcodeprinter.connection.IConnectionStateListener;
import com.argox.sdk.barcodeprinter.connection.PrinterConnection;
import com.argox.sdk.barcodeprinter.connection.PrinterDataListener;
import com.argox.sdk.barcodeprinter.connection.bluetooth.BluetoothConnection;
import com.argox.sdk.barcodeprinter.connection.file.FileConnection;
import com.argox.sdk.barcodeprinter.connection.tcp.TCPConnection;
import com.argox.sdk.barcodeprinter.connection.usb.USBConnection;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZ;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZBarCodeType;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZDataFormat;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZFont;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZMediaType;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZOrient;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZQRCodeErrCorrect;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZQRCodeModel;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZStorage;
import com.argox.sdk.barcodeprinter.util.Encoding;

import java.io.File;
import java.util.HashMap;
import java.util.Random;

import grandroid.view.LayoutMaker;
import grandroid.view.ViewDesigner;

/**
 *
 * @author user
 */
public class FrameDemoCommandsPPLZ extends Activity {

    private static final String TAG = FrameDemoCommandsPPLZ.class.getName();
    private final String ACTION_USB_PERMISSION = TAG;
    protected BarcodePrinter<PrinterConnection, PPLZ> printer = null;
    private TextView tvState, tvQueue;
    Bundle bundle;

    @Override
    public void onCreate(Bundle icicle) {
        super.onCreate(icicle);

        //layout.
        LayoutMaker maker = new LayoutMaker(this);
        ViewDesigner vd = new ViewDesigner() {

            @Override
            public Button stylise(Button btn) {
                btn.setBackgroundResource(R.drawable.b1);
                btn.setTextColor(Color.BLACK);
                return btn;
            }

        };
        maker.setDesigner(vd);
        maker.getLastLayout().setBackgroundColor(Color.WHITE);
        maker.setDrawableDesignWidth(this, 640);
        this.bundle = getIntent().getExtras();

        maker.add(maker.createStyledText("Emulation : PPLZ").size(12).color(Color.BLACK).get(), maker.layFW());
        tvState = maker.add(maker.createStyledText("Connection State : ").size(12).color(Color.BLACK).get(), maker.layFW());
        tvQueue = maker.add(maker.createStyledText("Print Queue : ").size(12).color(Color.BLACK).get(), maker.layFW());
        maker.addColLayout(true, maker.layFF());
        {
            maker.setScalablePadding(maker.getLastLayout(), 20, 10, 20, 10);

            Button btnBarcode = maker.add(maker.createButton("Print Barcode"), maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 80));
            btnBarcode.setOnClickListener(new View.OnClickListener() {

                public void onClick(View arg0) {
                    printBarcode();
                }
            });

            Button btnQRcode = maker.add(maker.createButton("Print QRcode"), maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 80));
            btnQRcode.setOnClickListener(new View.OnClickListener() {

                public void onClick(View arg0) {
                    printQRcode();
                }
            });

            Button btnText = maker.add(maker.createButton("Print Text"), maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 80));
            btnText.setOnClickListener(new View.OnClickListener() {

                public void onClick(View arg0) {
                    printText();
                }
            });

            Button btnGraphicText = maker.add(maker.createButton("Print Graphic Text"), maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 80));
            btnGraphicText.setOnClickListener(new View.OnClickListener() {

                public void onClick(View arg0) {
                    printGraphicText();
                }
            });

            Button btnGraphics = maker.add(maker.createButton("Print Graphics"), maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 80));
            btnGraphics.setOnClickListener(new View.OnClickListener() {

                public void onClick(View arg0) {
                    printGraphics();
                }
            });

            Button btnFile = maker.add(maker.createButton("Print File"), maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 80));
            btnFile.setOnClickListener(new View.OnClickListener() {
                
                String m_chosen;
                public void onClick(View arg0) {
                    try {

                        /////////////////////////////////////////////////////////////////////////////////////////////////
                        //Create FileOpenDialog and register a callback
                        /////////////////////////////////////////////////////////////////////////////////////////////////
                        SimpleFileDialog FileOpenDialog =  new SimpleFileDialog(FrameDemoCommandsPPLZ.this, "FileOpen",
                                        new SimpleFileDialog.SimpleFileDialogListener()
                        {
                                @Override
                                public void onChosenDir(String chosenDir) 
                                {
                                    // The code in this function will be executed when the dialog OK button is pushed
                                    m_chosen = chosenDir;
                                    //Notice
                                    //In the main thread, if you call write function of the network.
                                    //You will get android.os.NetworkOnMainThreadException exception.
                                    //This exception is thrown when an application attempts to perform a networking operation on its main thread.
                                    Thread t = new Thread(new Runnable() {

                                        public void run() {
                                            try {
                                                printer.sendFile(m_chosen);
                                                apendQueue("File");
                                            } catch (Exception ex) {
                                                Log.e(TAG, null, ex);
                                            }
                                        }
                                    });
                                    t.start();
                                }
                        });

                        //You can change the default filename using the public variable "Default_File_Name"
                        FileOpenDialog.Default_File_Name = "";
                        FileOpenDialog.chooseFile_or_Dir();

                    } catch (Exception ex) {
                        Log.e(TAG, null, ex);
                    }
                }
            });

            Button btnSend = maker.add(maker.createButton("Send"), maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 80));
            btnSend.setOnClickListener(new View.OnClickListener() {

                public void onClick(View arg0) {
                    //Notice
                    //In the main thread, if you call write function of the network.
                    //You will get android.os.NetworkOnMainThreadException exception.
                    //This exception is thrown when an application attempts to perform a networking operation on its main thread.
                    Thread t = new Thread(new Runnable() {

                        public void run() {
                            try {
                                //To output data then you can get the result.
                                printer.getEmulation().getIOUtil().printOut();
                                tvQueue.setText("Print Queue : ");
                            } catch (Exception ex) {
                                Log.e(TAG, null, ex);
                            }
                        }
                    });
                    t.start();
                }
            });
            Button btnClose = maker.add(maker.createButton("Close Connection"), maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 80));
            btnClose.setOnClickListener(new View.OnClickListener() {

                public void onClick(View arg0) {
                    try {

                        //To close interface.
                        printer.getConnection().close();
                        tvQueue.setText("Print Queue : ");

                    } catch (Exception ex) {
                        Log.e(TAG, null, ex);
                    }
                }
            });

            maker.escape();
        }
        //create printer.
        this.createPrinter(this.bundle);
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        try {
            //To close interface.
            printer.getConnection().close();
            //tvQueue.setText("Print Queue : ");

        } catch (Exception ex) {
            Log.e(TAG, null, ex);
        }
    }

    private void createPrinter(Bundle b) {

        ConnectType type = ConnectType.values()[b.getInt("type")];
        try {
            printer = new BarcodePrinter<PrinterConnection, PPLZ>();
            printer.setEmulation(new PPLZ());
            //printer.setEnabledLogger(true);//If you need logger file. Please enable this line code.
        }
        catch (Exception ex){
            Log.e(TAG, null, ex);
            Toast.makeText(this, ex.toString(), Toast.LENGTH_SHORT).show();
            finish();
        }
        //
        IConnectionStateListener stateListener = new IConnectionStateListener() {

            public void onStateChanged(final ConnectionState state) {
                FrameDemoCommandsPPLZ.this.runOnUiThread(new Runnable() {

                    public void run() {
                        if (state == ConnectionState.Connected) {
                            Toast.makeText(FrameDemoCommandsPPLZ.this, "Printer connection is " + state.name(), Toast.LENGTH_SHORT).show();
                            tvState.setText("Connected: " + printer.getConnection().toString());
                        } else {
                            tvState.setText(state.name());
                        }
                    }
                });
                Log.e(TAG, "Printer connection is " + state.name());
            }
        };

        try {
            PrinterConnection connection = null;
            //Notice:
            //If the interface no close when you leave the activity.
            //You will cannot to open the interface again, Please don't forgot it!
            switch (type) {
                case NETWORK:
                    String ip = b.getString("IP");
                    int port = b.getInt("Port");
                    connection = new TCPConnection(ip, port);
                    break;
                case BLUETOOTH:
                    //Notice:
                    //Only use BD Address to open Bluetooth interface, when you send data finished, and  you can close interface immediately.
                    connection = new BluetoothConnection(this);
                    break;
                case USB:
                    UsbDevice device = (UsbDevice) getIntent().getParcelableExtra(UsbManager.EXTRA_DEVICE);
                    if (null == device) {
                        UsbManager manager = (UsbManager) getSystemService(Context.USB_SERVICE);
                        HashMap<String, UsbDevice> deviceList = manager.getDeviceList();
                        for (String key : deviceList.keySet()) {
                            device = deviceList.get(key);
                            if(device.getVendorId()==5732) {
                                break;
                            }
                            Log.d(TAG, device.getDeviceName() + ", vendorID=" + device.getVendorId());
                        }
                    }
                    if (null != device) {
                        UsbManager manager = (UsbManager) getSystemService(Context.USB_SERVICE);
                        if (!manager.hasPermission(device)) {//check permissions.
                            //Notice:
                            //If it is executed here, then the interface will open fail, because the device is no permission.
                            //When you append the "android.hardware.usb.action.USB_DEVICE_ATTACHED" request in the XXXManifest.xml file,
                            //then the system will ask for permission each time you plug in the USB.
                            final PendingIntent mPermissionIntent = PendingIntent.getBroadcast(this, 0, new Intent(ACTION_USB_PERMISSION), 0);
                            manager.requestPermission(device, mPermissionIntent);
                        }
                        connection = new USBConnection(this, device);
                    }
                    break;
                case FILE:
                    connection = new FileConnection(new File(Environment.getExternalStorageDirectory(), "argox_printer.txt"));
                    break;
            }
            printer.setConnection(connection);
            printer.getConnection().setStateListener(stateListener);
            //set data listener for receiving data
            printer.getConnection().setDataListener(new PrinterDataListener() {

                public void onReceive(byte[] bytes) {
                    try {
                        Log.d(TAG, "received bytes length=" + bytes.length);
                    } catch (Exception ex) {
                        Log.e(TAG, null, ex);
                    }
                }
            });
            //open connection
            printer.getConnection().open(); //or call connection.open() is tha same.
        } catch (Exception ex) {
            Log.e(TAG, null, ex);
            Toast.makeText(this, ex.toString(), Toast.LENGTH_SHORT).show();
            finish();
        }
    }

    private TextView apendQueue(String s) {
        tvQueue.setText(tvQueue.getText() + s + " ");
        return tvQueue;
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        printer.getConnection().onActivityResult(requestCode, resultCode, data);
    }

    private void printBarcode() {
        try {
            Encoding encode = Encoding.UTF_8;
            byte[] buf, buf2;
            buf = encode.getBytes("23456");
            //setting.
            printer.getEmulation().getSetUtil().setMediaType(PPLZMediaType.Direct_Thermal_Media);
            printer.getEmulation().getSetUtil().setLabelLength(500);
            //data.
            printer.getEmulation().getBarcodeUtil().printOneDBarcode(50, 50, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                    PPLZBarCodeType.Code_39, 1, buf, 'N', 'Y', 'N', 'N', 'N');// Code 39
            buf2 = encode.getBytes("01234567890123456789");
            printer.getEmulation().getBarcodeUtil().printOneDBarcode(50, 200, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                    PPLZBarCodeType.Code_128, 1, buf, 'Y', 'N', 'N', 'N', 'N');// Code 128
            buf2 = encode.getBytes(">9123$%&");
            printer.getEmulation().getBarcodeUtil().printOneDBarcode(50, 350, PPLZOrient.Clockwise_0_Degrees, 5, 13, 50,
                    PPLZBarCodeType.Code_128, 1, buf2, 'Y', 'N', 'N', 'N', 'N');// Code 128, Subsets A(Numeric Pairs give Alpha/Numerics)
            //set print conditions.
            printer.getEmulation().getSetUtil().setPrintOut(1, 0, 1, false);
            apendQueue("Barcode");
        } catch (Exception ex) {
            Log.e(TAG, null, ex);
        }
    }

    private void printQRcode() {
        try {
            Encoding encode = Encoding.UTF_8;
            byte[] buf;
            buf = encode.getBytes("[)_1E01_1D960001Z004951_1DUPSN_1D6X61_1D305_1D_1DN_1D_1DSEA_1DWA_1E_04");
            //setting.
            printer.getEmulation().getSetUtil().setMediaType(PPLZMediaType.Direct_Thermal_Media);
            printer.getEmulation().getSetUtil().setLabelLength(200);
            //data.
            printer.getEmulation().getBarcodeUtil().printQRCode(50, 50, PPLZQRCodeModel.Model_2, 3, PPLZQRCodeErrCorrect.Standard, buf, 1);// QR Code
            //set print conditions.
            printer.getEmulation().getSetUtil().setPrintOut(1, 0, 1, false);
            apendQueue("QRcode");
        } catch (Exception ex) {
            Log.e(TAG, null, ex);
        }
    }
    private void printText() {
        try {
            Encoding encode = Encoding.UTF_8;
            byte[] buf;
            buf = encode.getBytes("Label: print internal font");
            //setting.
            printer.getEmulation().getSetUtil().setMediaType(PPLZMediaType.Direct_Thermal_Media);
            printer.getEmulation().getSetUtil().setLabelLength(100);
            //data.
            printer.getEmulation().getTextUtil().printText(30, 0, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
            buf = encode.getBytes("ARGOX Printer SDK Demo");
            printer.getEmulation().getTextUtil().printText(30, 50, PPLZOrient.Clockwise_0_Degrees, PPLZFont.Font_Zero, 30, 30, buf, 0);
            //set print conditions.
            printer.getEmulation().getSetUtil().setPrintOut(1, 0, 1, false);
            apendQueue("Text");
        } catch (Exception ex) {
            Log.e(TAG, null, ex);
        }
    }

    private void printGraphicText() {
        try {
            Encoding encode = Encoding.UTF_8;
            byte[] buf;
            //Demo graphic text
            String imageName = "txt" + String.format("%05d", new Random().nextInt(100000));
            //setting.
            printer.getEmulation().getSetUtil().setMediaType(PPLZMediaType.Direct_Thermal_Media);
            printer.getEmulation().getSetUtil().setLabelLength(100);
            //data.
            //print using graphic id.
            printer.getEmulation().getTextUtil().storeTextGraphic(Typeface.DEFAULT, 30, false, false, false, false,
                    PPLZStorage.Dram, imageName, "Graphic Text Demo");
            printer.getEmulation().getGraphicsUtil().printStoreGraphic(50, 50, PPLZStorage.Dram, imageName, 1, 1);
            //print direct
            printer.getEmulation().getTextUtil().printTextGraphic(50, 100, Typeface.DEFAULT, 30, false, false, false, false,
                    "Print Text Direct", PPLZDataFormat.Hex_Compressed,  PPLZOrient.Clockwise_0_Degrees,  0);
            //set print conditions.
            printer.getEmulation().getSetUtil().setPrintOut(1, 0, 1, false);
            apendQueue("GraphicText");
        } catch (Exception ex) {
            Log.e(TAG, null, ex);
        }
    }

    private void printGraphics() {
        try {
            Bitmap bmp = BitmapFactory.decodeResource(FrameDemoCommandsPPLZ.this.getResources(), R.drawable.android);

            //setting.
            printer.getEmulation().getSetUtil().setMediaType(PPLZMediaType.Direct_Thermal_Media);
            printer.getEmulation().getSetUtil().setLabelLength(200);
            //data.
            //print using graphic id.
            printer.getEmulation().getGraphicsUtil().storeGraphic(bmp, PPLZStorage.Dram, "graphic");
            printer.getEmulation().getGraphicsUtil().printStoreGraphic(
                    30, 30, PPLZStorage.Dram, "graphic", 1, 1);
            //print direct
            printer.getEmulation().getGraphicsUtil().printGraphic(330, 30,bmp, PPLZDataFormat.Hex_Compressed);
            //set print conditions.
            printer.getEmulation().getSetUtil().setPrintOut(1, 0, 1, false);
            if (null != bmp) {
                bmp.recycle();
            }
            apendQueue("Graphics");
        } catch (Exception ex) {
            Log.e(TAG, null, ex);
        }
    }
}

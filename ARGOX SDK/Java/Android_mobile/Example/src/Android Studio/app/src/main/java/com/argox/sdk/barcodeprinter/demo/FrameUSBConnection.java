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
import android.content.SharedPreferences;
import android.graphics.Color;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbManager;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;

import com.argox.sdk.barcodeprinter.BarcodePrinter;
import com.argox.sdk.barcodeprinter.connection.usb.USBConnection;
import com.argox.sdk.barcodeprinter.emulation.pplb.PPLB;
import com.argox.sdk.barcodeprinter.emulation.pplb.PPLBFont;
import com.argox.sdk.barcodeprinter.emulation.pplb.PPLBMediaType;
import com.argox.sdk.barcodeprinter.emulation.pplb.PPLBOrient;
import com.argox.sdk.barcodeprinter.emulation.pplb.PPLBPrintMode;
import com.argox.sdk.barcodeprinter.emulation.pplb.PPLBStorage;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZ;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZFont;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZMediaTrack;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZMediaType;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZOrient;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZPrintMode;
import com.argox.sdk.barcodeprinter.emulation.pplz.PPLZUnitType;

import java.util.HashMap;

import grandroid.view.LayoutMaker;
import grandroid.view.ViewDesigner;

/**
 *
 * @author Rovers
 */
public class FrameUSBConnection extends Activity {

    private static final String TAG = FrameUSBConnection.class.getName();
    private final String ACTION_USB_PERMISSION = TAG;
    protected UsbDevice device = null;
    TextView tvDeviceName;
    TextView tvDeviceId;
    TextView tvVendorId;
    TextView tvProductId;
    TextView tvDeviceProtocol;
    TextView tvDeviceClass;
    boolean pplzenable;
    //use SharedPreferences to save the setting data.
    SharedPreferences preferences;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        //use SharedPreferences to save the setting data.
        preferences = getSharedPreferences("preferences", MODE_PRIVATE);
        pplzenable = preferences.getBoolean("PPLZ", true);

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
            tvDeviceName = maker.addTextView("DeviceName: ");
            tvDeviceId = maker.addTextView("DeviceId: ");
            tvVendorId = maker.addTextView("VendorId: ");
            tvProductId = maker.addTextView("ProductId: ");
            tvDeviceProtocol = maker.addTextView("DeviceProtocol: ");
            tvDeviceClass = maker.addTextView("DeviceClass: ");
            Button btnShowCases = maker.addButton("Print \"Hello World\"");
            btnShowCases.setBackgroundResource(R.drawable.b1);
            btnShowCases.setLayoutParams(maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 100));
            btnShowCases.setOnClickListener(new View.OnClickListener() {

                public void onClick(View v) {
                    if (getUSBDevice()) {
                        if (pplzenable) {
                            PPLZFunction();
                        } else {
                            PPLBFunction();
                        }
                    }
                }
            });
            maker.escape();
        }
        getUSBDevice();
    }

    private boolean getUSBDevice() {
        device = (UsbDevice) getIntent().getParcelableExtra(UsbManager.EXTRA_DEVICE);

        if (null == device) {
            UsbManager manager = (UsbManager) getSystemService(Context.USB_SERVICE);
            HashMap<String, UsbDevice> deviceList = manager.getDeviceList();
            for (String key : deviceList.keySet()) {
                device = deviceList.get(key);
                Log.d(TAG, device.getDeviceName() + ", vendorID=" + device.getVendorId());
                if(device.getVendorId()==5732) {
                    break;
                }
            }
        }
        if (null == device) {
            tvDeviceName.setText("DeviceName: ");
            tvDeviceId.setText("DeviceId: ");
            tvVendorId.setText("VendorId: ");
            tvProductId.setText("ProductId: ");
            tvDeviceProtocol.setText("DeviceProtocol: ");
            tvDeviceClass.setText("DeviceClass: ");
        }
        else {
            tvDeviceName.setText("DeviceName: " + device.getDeviceName());
            tvDeviceId.setText("DeviceId: " + device.getDeviceId());
            tvVendorId.setText("VendorId: " + device.getVendorId());
            tvProductId.setText("ProductId: " + device.getProductId());
            tvDeviceProtocol.setText("DeviceProtocol: " + device.getDeviceProtocol());
            tvDeviceClass.setText("DeviceClass: " + device.getDeviceClass());
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
            return true;
        }
        return false;
    }

    private void PPLZFunction() {

        BarcodePrinter<USBConnection, PPLZ> printer = new BarcodePrinter<USBConnection, PPLZ>();

        try {
            //if you use BluetoothConnection instead of TCPConnection , you must implements onActivityResult(...) callback function
            //with calling printer.getConnection().onActivityResult(...)
            printer.setConnection(new USBConnection(this, device));
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
                Toast.makeText(this, ex.toString(), Toast.LENGTH_LONG).show();
                Log.e(TAG, null, ex);
            }
        }
    }

    private void PPLBFunction() {

        BarcodePrinter<USBConnection, PPLB> printer = new BarcodePrinter<USBConnection, PPLB>();

        try {
            //if you use BluetoothConnection instead of TCPConnection , you must implements onActivityResult(...) callback function
            //with calling printer.getConnection().onActivityResult(...)
            printer.setConnection(new USBConnection(this, device));
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
            printer.getEmulation().getTextUtil().printText(10,0, PPLBOrient.Clockwise_0_Degrees,
                    PPLBFont.Font_1, 1, 1, false, text.getBytes());
            //set print conditions.
            printer.getEmulation().getSetUtil().setPrintOut(1, 1);
            printer.getEmulation().printOut();
            printer.getConnection().close();
        } catch (Exception ex) {
            try {
                printer.getConnection().close();
            }
            catch (Exception e) {
            }
            finally {
                Toast.makeText(this, ex.toString(), Toast.LENGTH_LONG).show();
                Log.e(TAG, null, ex);
            }
        }
    }
}

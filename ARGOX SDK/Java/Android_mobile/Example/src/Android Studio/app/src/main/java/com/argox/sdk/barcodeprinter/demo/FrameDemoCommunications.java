/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.argox.sdk.barcodeprinter.demo;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Color;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.Toast;
import grandroid.dialog.DialogMask;
import grandroid.view.LayoutMaker;
import grandroid.view.ViewDesigner;

/**
 *
 * @author user
 */
public class FrameDemoCommunications extends Activity {

    final String ipAddr = "192.168.0.100";
    final String portNumber = "2000";
    boolean pplzenable;
    //use SharedPreferences to save the setting data.
    SharedPreferences preferences;

    /**
     * Called when the activity is first created.
     */
    @Override
    public void onCreate(Bundle icicle) {
        super.onCreate(icicle);

        //use SharedPreferences to save the setting data.
        preferences = getSharedPreferences("preferences", MODE_PRIVATE);
        pplzenable = preferences.getBoolean("PPLZ", true);

        //layout.
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
            Button btnNetwork = maker.addButton("Connect via Network");
            btnNetwork.setBackgroundResource(R.drawable.b1);
            btnNetwork.setLayoutParams(maker.layAbsolute(0, 0, LinearLayout.LayoutParams.MATCH_PARENT, 100));
            btnNetwork.setOnClickListener(getOnClickListener(ConnectType.NETWORK));

            Button btnBluetooth = maker.addButton("Connect via Bluetooth");
            btnBluetooth.setBackgroundResource(R.drawable.b1);
            btnBluetooth.setLayoutParams(maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 100));
            btnBluetooth.setOnClickListener(getOnClickListener(ConnectType.BLUETOOTH));

            Button btnUSB = maker.addButton("Connect via USB");
            btnUSB.setBackgroundResource(R.drawable.b1);
            btnUSB.setLayoutParams(maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 100));
            btnUSB.setOnClickListener(getOnClickListener(ConnectType.USB));
            
            Button btnFile = maker.addButton("Connect via File");
            btnFile.setBackgroundResource(R.drawable.b1);
            btnFile.setLayoutParams(maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 100));
            btnFile.setOnClickListener(getOnClickListener(ConnectType.FILE));
            maker.escape();
        }
    }

    public View.OnClickListener getOnClickListener(final ConnectType type) {
        return new View.OnClickListener() {

            public void onClick(View arg0) {
                switch (type) {
                    case NETWORK: {
                        new DialogMask(FrameDemoCommunications.this) {
                            EditText etIP;
                            EditText etPort;
                            String ip;
                            String port;

                            @Override
                            public String getTitle() {
                                return "TCP Connection Setting";
                            }

                            @Override
                            public boolean setupDialogContent(Context context, LayoutMaker maker) throws Exception {
                                maker.getLastLayout().setBackgroundColor(Color.WHITE);
                                maker.setScalablePadding(10, 10, 10, 10);
                                //get the setting data from SharedPreferences.
                                ip = preferences.getString("IP", ipAddr);
                                port = preferences.getString("Port", portNumber);
                                maker.addTextView("IP");
                                etIP = maker.add(maker.createEditText(ip), maker.layFW());
                                maker.addTextView("Port");
                                etPort = maker.add(maker.createEditText(port), maker.layFW());

                                return true;
                            }

                            @Override
                            public boolean onClickPositiveButton(Context context) {
                                Intent intent = new Intent();
                                if (pplzenable) {
                                    intent.setClass(FrameDemoCommunications.this, FrameDemoCommandsPPLZ.class);
                                }
                                else {
                                    intent.setClass(FrameDemoCommunications.this, FrameDemoCommandsPPLB.class);
                                }
                                Bundle b = new Bundle();
                                b.putInt("type", type.ordinal());
                                if (etIP.getText() != null && etPort.getText() != null) {
                                    ip = etIP.getText().toString();
                                    port = etPort.getText().toString();
                                    b.putString("IP", ip);
                                    b.putInt("Port", Integer.parseInt(port));
                                    //save the setting data to SharedPreferences.
                                    preferences.edit()
                                            .putString("IP", ip)
                                            .putString("Port", port)
                                            .apply();
                                    intent.putExtras(b);
                                    FrameDemoCommunications.this.startActivity(intent);
                                } else {
                                    Toast.makeText(FrameDemoCommunications.this, "No data is entered for IP or Prot.", Toast.LENGTH_SHORT).show();
                                }
                                return true;
                            }
                        }.show();
                        break;
                    }
                    case BLUETOOTH: {
                        Intent intent = new Intent();
                        Bundle b = new Bundle();
                        b.putInt("type", type.ordinal());
                        if (pplzenable) {
                            intent.setClass(FrameDemoCommunications.this, FrameDemoCommandsPPLZ.class);
                        }
                        else {
                            intent.setClass(FrameDemoCommunications.this, FrameDemoCommandsPPLB.class);
                        }
                        intent.putExtras(b);
                        FrameDemoCommunications.this.startActivity(intent);
                        break;
                    }
                    case USB: {
                        Intent intent = new Intent();
                        Bundle b = new Bundle();
                        b.putInt("type", type.ordinal());
                        if (pplzenable) {
                            intent.setClass(FrameDemoCommunications.this, FrameDemoCommandsPPLZ.class);
                        }
                        else {
                            intent.setClass(FrameDemoCommunications.this, FrameDemoCommandsPPLB.class);
                        }
                        intent.putExtras(b);
                        FrameDemoCommunications.this.startActivity(intent);
                        break;
                    }
                    case FILE: {
                        Intent intent = new Intent();
                        Bundle b = new Bundle();
                        b.putInt("type", type.ordinal());
                        if (pplzenable) {
                            intent.setClass(FrameDemoCommunications.this, FrameDemoCommandsPPLZ.class);
                        }
                        else {
                            intent.setClass(FrameDemoCommunications.this, FrameDemoCommandsPPLB.class);
                        }
                        intent.putExtras(b);
                        FrameDemoCommunications.this.startActivity(intent);
                        break;
                    }
                }
            }
        };
    }
}

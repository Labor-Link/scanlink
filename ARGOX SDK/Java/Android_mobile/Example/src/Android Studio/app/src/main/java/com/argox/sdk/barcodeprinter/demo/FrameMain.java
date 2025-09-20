/*
 * To change this license header, choose License Headers in Project Properties.
 * To change this template file, choose Tools | Templates
 * and open the template in the editor.
 */
package com.argox.sdk.barcodeprinter.demo;

import android.Manifest;
import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.os.Bundle;
import android.support.v4.app.ActivityCompat;
import android.support.v4.content.ContextCompat;
import android.view.View;
import android.view.ViewGroup.LayoutParams;
import android.widget.Button;
import android.widget.CompoundButton;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.RadioButton;
import android.widget.RadioGroup;
import android.widget.RelativeLayout;
import android.widget.Toast;

import grandroid.dialog.DialogMask;
import grandroid.view.LayoutMaker;
import grandroid.view.ViewDesigner;

/**
 *
 * @author Rovers
 */
public class FrameMain extends Activity {

    //init data.
    final String ipAddr = "192.168.0.100";
    final String portNumber = "2000";

    //use SharedPreferences to save the setting data.
    SharedPreferences preferences;

    public  void checkPermission(String permission, int requestCode)
    {
        if (ContextCompat.checkSelfPermission(this, permission) == PackageManager.PERMISSION_DENIED) {
            ActivityCompat.requestPermissions( this, new String[] { permission }, requestCode);
        }
        else {
            //Toast.makeText(this, "Permission already granted", Toast.LENGTH_SHORT).show();
        }
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        // Android 6.0 (API 18) and later versions require runtime permissions.
        // Note that the permission query dialog box will not pop up by itself. The developer has to call it himself.
        // If the function that the developer wants to call requires a certain permission and the user refuses the authorization,
        // the function will throw an exception and directly cause the program to crash.
        checkPermission(Manifest.permission.WRITE_EXTERNAL_STORAGE, 1);

        //use SharedPreferences to save the setting data.
        preferences = getSharedPreferences("preferences", MODE_PRIVATE);

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
            maker.addTextView("Select Demo Emulation:");
            //step 1: select emulation.
            {
                // Initialize a new RadioGroup
                RadioGroup rg = new RadioGroup(getApplicationContext());
                rg.setOrientation(RadioGroup.VERTICAL);

                // Initialize the layout parameters for RadioGroup
                RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams(
                        LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);

                // Apply the layout parameters for RadioGroup
                rg.setLayoutParams(lp);

                // Create a Radio Button for RadioGroup
                RadioButton rb_pplz = new RadioButton(getApplicationContext());
                rb_pplz.setText("PPLZ Emulation");
                rb_pplz.setTextColor(Color.BLACK);
                rb_pplz.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener() {
                                                       @Override
                                                       public void onCheckedChanged(CompoundButton buttonView, boolean isChecked) {
                                                           preferences.edit()
                                                                   .putBoolean("PPLZ", isChecked)
                                                                   .apply();
                                                       }
                                                   }
                );
                rg.addView(rb_pplz);

                // Create another Radio Button for RadioGroup
                RadioButton rb_pplb = new RadioButton(getApplicationContext());
                rb_pplb.setText("PPLB Emulation");
                rb_pplb.setTextColor(Color.BLACK);
                rb_pplb.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener() {
                                                       @Override
                                                       public void onCheckedChanged(CompoundButton buttonView, boolean isChecked) {
                                                           preferences.edit()
                                                                   .putBoolean("PPLZ", !isChecked)
                                                                   .apply();
                                                       }
                                                   }
                );
                rg.addView(rb_pplb);

                // Finally, add the RadioGroup to main layout
                maker.getLastLayout().addView(rg);
                boolean pplzenable = preferences.getBoolean("PPLZ", true);
                if (pplzenable) {
                    rb_pplz.setChecked(pplzenable);
                }
                else {
                    rb_pplb.setChecked(!pplzenable);
                }
            }
            //step 2: select function.
            maker.addLine(Color.BLACK, 5);
            Button btnHelloWorld = maker.addButton("Hello World(via Network)");
            btnHelloWorld.setBackgroundResource(R.drawable.b1);
            btnHelloWorld.setLayoutParams(maker.layAbsolute(0, 0, LinearLayout.LayoutParams.MATCH_PARENT, 100));
            btnHelloWorld.setOnClickListener(new View.OnClickListener() {

                public void onClick(View v) {
                    new DialogMask(FrameMain.this) {
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
                            intent.setClass(FrameMain.this, FrameTCPConnection.class);
                            Bundle b = new Bundle();
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
                                startActivity(intent);
                            } else {
                                Toast.makeText(FrameMain.this, "No data is entered for IP or Prot.", Toast.LENGTH_SHORT).show();
                            }
                            return true;
                        }
                    }.show();
                }
            });

            Button btnUSBMode = maker.addButton("Hello World(via USB)");
            btnUSBMode.setBackgroundResource(R.drawable.b1);
            btnUSBMode.setLayoutParams(maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 100));
            btnUSBMode.setOnClickListener(new View.OnClickListener() {

                public void onClick(View v) {
                    startActivity(FrameUSBConnection.class);
                }
            });

            Button btnBTMode = maker.addButton("Hello World(via Bluetooth)");
            btnBTMode.setBackgroundResource(R.drawable.b1);
            btnBTMode.setLayoutParams(maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 100));
            btnBTMode.setOnClickListener(new View.OnClickListener() {

                public void onClick(View v) {
                    startActivity(FrameBluetoothConnection.class);
                }
            });

            Button btnShowCases = maker.addButton("Show Cases");
            btnShowCases.setBackgroundResource(R.drawable.b1);
            btnShowCases.setLayoutParams(maker.layAbsolute(0, 25, LinearLayout.LayoutParams.MATCH_PARENT, 100));
            btnShowCases.setOnClickListener(new View.OnClickListener() {

                public void onClick(View v) {
                    startActivity(FrameDemoCommunications.class);
                }
            });

            maker.escape();
        }
    }

    private void startActivity(Class activity) {
        Intent intent = new Intent();
        intent.setClass(this, activity);
        startActivity(intent);
    }
}

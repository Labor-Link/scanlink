# Datalogic Scanner Configuration Barcodes

## Important: Scan These Barcodes in Order

To configure your Datalogic scanner to permanently use USB COM port mode:

### Step 1: Enter Programming Mode
First, scan this barcode to enter programming mode:
```
**START PROGRAMMING**
(Generate barcode with content: PROGRAM)
```

### Step 2: Set USB Interface to COM Port
Scan this barcode to enable USB COM port (Virtual COM Port):
```
**USB COM PORT MODE**
(Generate barcode with content: USB_COM_PORT)
```
For most Datalogic scanners, the specific code is:
- Code: `USBCOMPORT` or `VCPMODE` or `USBVCP`

### Step 3: Set Communication Settings
```
**9600 BAUD RATE**
**NO PARITY**
**8 DATA BITS**  
**1 STOP BIT**
```

### Step 4: Enable Suffix (CR+LF)
```
**SUFFIX CR+LF**
```

### Step 5: Save and Exit Programming
```
**SAVE AND EXIT**
(Generate barcode with content: SAVE)
```

## Alternative: Use Datalogic Aladdin Software

If barcodes don't work, download and use **Datalogic Aladdin** configuration utility:

1. Download from: https://www.datalogic.com
2. Connect scanner via USB
3. Open Aladdin software
4. Navigate to: **Interface → USB**
5. Select: **USB COM Port (Virtual COM Port)**
6. Set baud rate: **9600**
7. Enable suffix: **CR+LF**
8. Click **"Write Configuration"** to save permanently to scanner

## For Quick Testing (Temporary)

If you need to test quickly without permanent configuration:
- Keep the scanner plugged in
- Don't unplug it (unplugging resets to default HID keyboard mode)
- The software will send initialization commands when it connects

## Which Datalogic Model Do You Have?

Different Datalogic models have different configuration methods:
- **Gryphon series (GD4xxx, GBT4xxx)**: Use Aladdin software
- **Magellan series**: Use configuration utility
- **QuickScan series (QD/QW/QM)**: Can use programming barcodes
- **PowerScan series**: Use Datalogic Scan Config utility

## Verify Configuration

After configuring, verify in Device Manager:
1. Open Device Manager (Win + X → Device Manager)
2. Expand **Ports (COM & LPT)**
3. You should see: **"Datalogic USB-COM Port (COMx)"**
4. If you see it under **Keyboards**, it's still in HID mode

## Need Programming Barcodes?

Visit Datalogic's official documentation:
https://www.datalogic.com/eng/support-services/technical-support-cd-46.html

Or search for your specific model + "programming guide" (PDF)
The PDF will contain actual scannable barcodes for configuration.







Scan Link Printer - Release (win-x64)

How to run:
- Run: ScanLinkPrinter.App.exe
- No .NET install needed (self-contained).

Connections:
- USB: Use full interface path (example): \\?\USB#VID_1664&PID_08E0#<serial>#{a5dcbf10-6530-11d2-901f-00c04fb951ed}
- Serial: e.g., COM3
- TCP/IP: e.g., 192.168.1.50:9100
- File: Generates label commands to a file

Tips:
- Use the Test button to validate Serial/TCP.
- Use Copy Error (bottom bar) to copy full error text.

Support:
- If USB fails with GetLastError=3, confirm the device path format and that no other app holds the device.

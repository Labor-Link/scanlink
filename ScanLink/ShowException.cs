using System;
using System.Windows.Forms;
using BarcodePrinter_API;

namespace ScanLink
{
    internal static class ShowException
    {
        static BarcodePrinterConnectionException exPrinterConnectionException = new BarcodePrinterConnectionException();
        static BarcodePrinterIllegalArgumentException exIllegalArgumentException = new BarcodePrinterIllegalArgumentException();
        static BarcodePrinterGeneralException exGeneralException = new BarcodePrinterGeneralException();
        static BarcodePrinterWriteTimeoutException exWriteTimeoutException = new BarcodePrinterWriteTimeoutException();
        static BarcodePrinterReadTimeoutException exReadTimeoutException = new BarcodePrinterReadTimeoutException();

        public static void Show(string className, string currentMethod, Exception ex)
        {
            string strContext;
            string strTitle;
            string strExceptionFrom = "Exception From:";
            string strMessage = "Message:";
            string strStackTrace = "Stack Trace:";
            string strExceptionName = "Exception Name:";
            if (Type.Equals(exPrinterConnectionException.GetType(), ex.GetType()))
            {
                strContext = strExceptionFrom + Environment.NewLine + className + "=>" + currentMethod + Environment.NewLine + Environment.NewLine +
                    strMessage + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                   strStackTrace + Environment.NewLine + ex.StackTrace;
                strTitle = ex.GetType().Name;
            }
            else if (Type.Equals(exIllegalArgumentException.GetType(), ex.GetType()))
            {
                strContext = strExceptionFrom + Environment.NewLine + className + "=>" + currentMethod + Environment.NewLine + Environment.NewLine +
                    strMessage + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                   strStackTrace + Environment.NewLine + ex.StackTrace;
                strTitle = ex.GetType().Name;
            }
            else if (Type.Equals(exGeneralException.GetType(), ex.GetType()))
            {
                strContext = strExceptionFrom + Environment.NewLine + className + "=>" + currentMethod + Environment.NewLine + Environment.NewLine +
                    strMessage + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                   strStackTrace + Environment.NewLine + ex.StackTrace;
                strTitle = ex.GetType().Name;
            }
            else if (Type.Equals(exWriteTimeoutException.GetType(), ex.GetType()))
            {
                strContext = strExceptionFrom + Environment.NewLine + className + "=>" + currentMethod + Environment.NewLine + Environment.NewLine +
                    strMessage + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                   strStackTrace + Environment.NewLine + ex.StackTrace;
                strTitle = ex.GetType().Name;
            }
            else if (Type.Equals(exReadTimeoutException.GetType(), ex.GetType()))
            {
                strContext = strExceptionFrom + Environment.NewLine + className + "=>" + currentMethod + Environment.NewLine + Environment.NewLine +
                    strMessage + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                   strStackTrace + Environment.NewLine + ex.StackTrace;
                strTitle = ex.GetType().Name;
            }
            else
            {
                strContext = strExceptionFrom + Environment.NewLine + className + "=>" + currentMethod + Environment.NewLine + Environment.NewLine +
                    strExceptionName + Environment.NewLine + ex.GetType().Name + Environment.NewLine + Environment.NewLine +
                    strMessage + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                   strStackTrace + Environment.NewLine + ex.StackTrace;
                strTitle = ex.GetType().Name;
            }
            MessageBox.Show(strContext, strTitle, MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1);
        }
    }
}



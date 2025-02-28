using System.Threading;
using System.Windows;


namespace CFIT.AppTools
{
    public static class ClipboardHelper
    {
        public static void SetClipboard(string text)
        {
            Thread thread = new Thread(() =>
            {
                Clipboard.SetText(text);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public static string GetClipboard()
        {
            string result = null;
            Thread thread = new Thread(() =>
            {
                result = Clipboard.GetText();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return result;
        }
    }
}

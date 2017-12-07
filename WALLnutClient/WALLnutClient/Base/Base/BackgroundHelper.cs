using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace WALLnutClient
{
    public static class BackgroundHelper
    {
        public static void DoInBackground(Action backgroundAction, Action mainThreadAction)
        {
            DoInBackground(backgroundAction, mainThreadAction, 200);
        }
        public static void DoInBackground(Action backgroundAction, Action mainThreadAction, int milliseconds)
        {
            try
            {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                Thread thread = new Thread(delegate ()
                {
                    Thread.Sleep(milliseconds);
                    if (backgroundAction != null)
                        backgroundAction();
                    if (mainThreadAction != null)
                        dispatcher.BeginInvoke(mainThreadAction);
                });
                thread.IsBackground = true;
                thread.TrySetApartmentState(ApartmentState.STA);
                thread.Priority = ThreadPriority.Lowest;
                thread.Start();
            }
            catch (Exception ex)
            {
            }
        }
        public static void DoWithDispatcher(Dispatcher dispatcher, Action action, DispatcherPriority dispatcherPriority = DispatcherPriority.Background)
        {
            try
            {
                if (dispatcher.CheckAccess())
                    action();
                else
                {
                    AutoResetEvent done = new AutoResetEvent(false);
                    dispatcher.BeginInvoke((Action)delegate ()
                    {
                        action();
                        done.Set();
                    }, dispatcherPriority);
                    done.WaitOne();
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}

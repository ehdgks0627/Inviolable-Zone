using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PIPEServerExample
{
    public class PipeServer
    {
        bool running;
        Thread runningThread;
        EventWaitHandle terminateHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        public string PipeName { get; set; }
        MainWindow window;

        public PipeServer(MainWindow window)
        {
            this.window = window;
        }

        void ServerLoop()
        {
            while (running)
            {
                ProcessNextClient();
            }

            terminateHandle.Set();
        }

        public void Run()
        {
            running = true;
            runningThread = new Thread(ServerLoop);
            runningThread.Start();
        }

        public void Stop()
        {
            running = false;
            terminateHandle.WaitOne();
        }

        public void ProcessClientThread(object o)
        {
            NamedPipeServerStream pipeStream = (NamedPipeServerStream)o;
            window.Dispatcher.Invoke((Action)(() =>
            {
                window.textBox1.Text += "new Client\n";
            }));
            char c;
            while((c = Convert.ToChar(pipeStream.ReadByte())) != 0xFF)
                {
                window.Dispatcher.Invoke((Action)(() =>
                {
                    window.textBox1.Text += c;
                }));
            }
            window.Dispatcher.Invoke((Action)(() =>
            {
                window.textBox1.Text += "exit Client\n";
            }));
            pipeStream.Close();
            pipeStream.Dispose();
        }

        public void ProcessNextClient()
        {
            try
            {
                NamedPipeServerStream pipeStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 254);
                pipeStream.WaitForConnection();
                
                Thread t = new Thread(ProcessClientThread);
                t.Start(pipeStream);
            }
            catch (Exception e)
            {
            }
        }
    }
}

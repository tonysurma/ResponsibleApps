using System;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace SampleAndroidApplication
{
    public class WebRequestTimeOut {
        readonly ManualResetEvent _waitHandle = new ManualResetEvent(false);

        readonly int _timeout;
        readonly WebRequest _request;

        public WebRequestTimeOut(WebRequest request, int timeout) {
            _request = request;
            _timeout = timeout;
        }

        public bool TimedOut { get; private set; }

        public void StartTimer() {
            _waitHandle.Reset();
            TimedOut = false;

            ThreadPool.QueueUserWorkItem(RunCountdownTimer);
        }

        void RunCountdownTimer(object state) {
            bool signalled = this._waitHandle.WaitOne(_timeout);
            if (!signalled) {
                TimedOut = true;
                Debug.WriteLine("Connection Timed Out");
                _request.Abort();
            }
        }
    }
}
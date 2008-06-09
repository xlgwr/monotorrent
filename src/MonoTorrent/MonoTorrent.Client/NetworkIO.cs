//
// NetworkIO.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2008 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Generic;
using System.Text;
using MonoTorrent.Client;
using MonoTorrent.Common;
using System.Threading;
using MonoTorrent.Client.Messages.Standard;
using System.Net.Sockets;

namespace MonoTorrent.Client
{
    internal static class NetworkIO
    {
        private static List<KeyValuePair<IAsyncResult, AsyncCallback>> receives;
        private static List<KeyValuePair<IAsyncResult, AsyncCallback>> sends;

        static NetworkIO()
        {
            receives = new List<KeyValuePair<IAsyncResult, AsyncCallback>>();
            sends = new List<KeyValuePair<IAsyncResult, AsyncCallback>>();

            Thread t = new Thread((ThreadStart)delegate
            {
                while (true)
                {
                    KeyValuePair<IAsyncResult, AsyncCallback>? r = null;
                    KeyValuePair<IAsyncResult, AsyncCallback>? s = null;

                    lock (receives)
                    {
                        for (int i = 0; i < receives.Count; i++)
                        {
                            if (!receives[i].Key.IsCompleted)
                                continue;
                            r = receives[i];
                            receives.RemoveAt(i);
                            break;
                        }
                    }

                    lock (sends)
                    {
                        for (int i = 0; i < sends.Count; i++)
                        {
                            if (!sends[i].Key.IsCompleted)
                                continue;
                            s = sends[i];
                            sends.RemoveAt(i);
                            break;
                        }
                    }

                    if (r.HasValue)
                    {
                        r.Value.Value(r.Value.Key);
                        r.Value.Key.AsyncWaitHandle.Close();
                    }
                    if (s.HasValue)
                    {
                        s.Value.Value(s.Value.Key);
                        s.Value.Key.AsyncWaitHandle.Close();
                    }

                    System.Threading.Thread.Sleep(1);
                }
            });
            t.IsBackground = true;
            t.Start();
        }


        internal static void EnqueueSend(ArraySegment<byte> sendBuffer, int bytesSent, int count, AsyncCallback callback, PeerIdInternal id)
        {
            IAsyncResult result = id.Connection.BeginSend(sendBuffer, bytesSent, count, SocketFlags.None, null, id);
            lock (sends)
                sends.Add(new KeyValuePair<IAsyncResult,AsyncCallback>(result, callback));
        }

        internal static void EnqueueReceive(ArraySegment<byte> receiveBuffer, int bytesReceived, int count, AsyncCallback callback, PeerIdInternal id)
        {
            IAsyncResult result = id.Connection.BeginReceive(receiveBuffer, bytesReceived, count, SocketFlags.None, null, id);
            lock (receives)
                receives.Add(new KeyValuePair<IAsyncResult,AsyncCallback>(result, callback));
        }
    }
}
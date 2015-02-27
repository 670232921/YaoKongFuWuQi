using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net;

namespace 遥控器WP
{
    class SocketClient
    {
        // Cached Socket object that will be used by each call for the lifetime of this class
        Socket _socket = null;
        // Signaling object used to notify when an asynchronous operation is completed
        static ManualResetEvent _clientDone = new ManualResetEvent(false);
        // Define a timeout in milliseconds for each asynchronous call. If a response is not received within this
        // timeout period, the call is aborted.
        const int TIMEOUT_MILLISECONDS = 5000;
        // The maximum size of the data buffer to use with the asynchronous socket methods
        const int MAX_BUFFER_SIZE = 2048;
        
        /// <summary>
        /// SocketClient Constructor
        /// </summary>
        public SocketClient()
        {
            // The following creates a socket with the following properties:
            // AddressFamily.InterNetwork - the socket will use the IP version 4 addressing scheme to resolve an address
            // SocketType.Dgram - a socket that supports datagram (message) packets
            // PrototcolType.Udp - the User Datagram Protocol (UDP)
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        /// <summary>
        /// Send the given data to the server using the established connection
        /// </summary>
        /// <param name="serverName">The name of the server</param>
        /// <param name="portNumber">The number of the port over which to send the data</param>
        /// <param name="data">The data to send to the server</param>
        /// <returns>The result of the Send request</returns>
        private string Send(string serverName, int portNumber, string data)
        {
            string response = "Operation Timeout";
            // We are re-using the _socket object that was initialized in the Connect method
            if (_socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                // Set properties on context object
                socketEventArg.RemoteEndPoint = new DnsEndPoint(serverName, portNumber);
                // Inline event handler for the Completed event.
                // Note: This event handler was implemented inline in order to make this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                {
                    response = e.SocketError.ToString();
                    // Unblock the UI thread
                    _clientDone.Set();
                });
                // Add the data to be sent into the buffer
                byte[] payload = Encoding.UTF8.GetBytes(data);
                socketEventArg.SetBuffer(payload, 0, payload.Length);
                // Sets the state of the event to nonsignaled, causing threads to block
                _clientDone.Reset();
                // Make an asynchronous Send request over the socket
                //_socket.SendToAsync(socketEventArg);
                _socket.ConnectAsync(socketEventArg);
                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                // If no response comes back within this time then proceed
                _clientDone.WaitOne(TIMEOUT_MILLISECONDS);
            }
            else
            {
                response = "Socket is not initialized";
            }
            return response;
        }

        public string Send(IPEndPoint ipe, string data)
        {
            return Send(ipe.Address.ToString(), ipe.Port, data);
            //byte[] payload = Encoding.UTF8.GetBytes(data);
            //SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            //args.RemoteEndPoint = ipe;
            //args.SetBuffer(payload, 0, payload.Length);
            //_socket.ConnectAsync(args);
            //return "aa";
        }

        /// <summary>
        /// Receive data from the server
        /// </summary>
        /// <param name="portNumber">The port on which to receive data</param>
        /// <returns>The data received from the server</returns>
        public string Receive(int portNumber)
        {
            string response = "Operation Timeout";
            // We are receiving over an established socket connection
            if (_socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                socketEventArg.RemoteEndPoint = new IPEndPoint(IPAddress.Any, portNumber);
                // Setup the buffer to receive the data
                socketEventArg.SetBuffer(new Byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);
                // Inline event handler for the Completed event.
                // Note: This even handler was implemented inline in order to make this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        // Retrieve the data from the buffer
                        response = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                        response = response.Trim('\0');
                    }
                    else
                    {
                        response = e.SocketError.ToString();
                    }
                    _clientDone.Set();
                });
                // Sets the state of the event to nonsignaled, causing threads to block
                _clientDone.Reset();
                // Make an asynchronous Receive request over the socket
                _socket.ReceiveFromAsync(socketEventArg);
                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                // If no response comes back within this time then proceed
                _clientDone.WaitOne(TIMEOUT_MILLISECONDS);
            }
            else
            {
                response = "Socket is not initialized";
            }
            return response;
        }

        /// <summary>
        /// Closes the Socket connection and releases all associated resources
        /// </summary>
        public void Close()
        {
            if (_socket != null)
            {
                _socket.Close();
            }
        }
    }




    //class SocketClient
    //{
    //    // Cached Socket object that will be used by each call for the lifetime of this class
    //    Socket _socket = null;

    //    // Signaling object used to notify when an asynchronous operation is completed
    //    static ManualResetEvent _clientDone = new ManualResetEvent(false);

    //    // Define a timeout in milliseconds for each asynchronous call. If a response is not received within this 
    //    // timeout period, the call is aborted.
    //    const int TIMEOUT_MILLISECONDS = 5000;

    //    // The maximum size of the data buffer to use with the asynchronous socket methods
    //    const int MAX_BUFFER_SIZE = 2048;
    //    /// <summary>
    //    /// Attempt a TCP socket connection to the given host over the given port
    //    /// </summary>
    //    /// <param name="hostName">The name of the host</param>
    //    /// <param name="portNumber">The port number to connect</param>
    //    /// <returns>A string representing the result of this connection attempt</returns>
    //    public string Connect(string hostName, int portNumber)
    //    {
    //        string result = string.Empty;

    //        // Create DnsEndPoint. The hostName and port are passed in to this method.
    //        DnsEndPoint hostEntry = new DnsEndPoint(hostName, portNumber);

    //        // Create a stream-based, TCP socket using the InterNetwork Address Family. 
    //        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    //        // Create a SocketAsyncEventArgs object to be used in the connection request
    //        SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
    //        socketEventArg.RemoteEndPoint = hostEntry;

    //        // Inline event handler for the Completed event.
    //        // Note: This event handler was implemented inline in order to make this method self-contained.
    //        socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
    //        {
    //            // Retrieve the result of this request
    //            result = e.SocketError.ToString();

    //            // Signal that the request is complete, unblocking the UI thread
    //            _clientDone.Set();
    //        });

    //        // Sets the state of the event to nonsignaled, causing threads to block
    //        _clientDone.Reset();

    //        // Make an asynchronous Connect request over the socket
    //        _socket.ConnectAsync(socketEventArg);

    //        // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
    //        // If no response comes back within this time then proceed
    //        _clientDone.WaitOne(TIMEOUT_MILLISECONDS);

    //        return result;
    //    }
    //    /// <summary>
    //    /// Send the given data to the server using the established connection
    //    /// </summary>
    //    /// <param name="data">The data to send to the server</param>
    //    /// <returns>The result of the Send request</returns>
    //    public string Send(string data)
    //    {
    //        string response = "Operation Timeout";

    //        // We are re-using the _socket object initialized in the Connect method
    //        if (_socket != null)
    //        {
    //            // Create SocketAsyncEventArgs context object
    //            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

    //            // Set properties on context object
    //            socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
    //            socketEventArg.UserToken = null;

    //            // Inline event handler for the Completed event.
    //            // Note: This event handler was implemented inline in order 
    //            // to make this method self-contained.
    //            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
    //            {
    //                response = e.SocketError.ToString();

    //                // Unblock the UI thread
    //                _clientDone.Set();
    //            });

    //            // Add the data to be sent into the buffer
    //            byte[] payload = Encoding.UTF8.GetBytes(data);
    //            socketEventArg.SetBuffer(payload, 0, payload.Length);

    //            // Sets the state of the event to nonsignaled, causing threads to block
    //            _clientDone.Reset();

    //            // Make an asynchronous Send request over the socket
    //            _socket.SendAsync(socketEventArg);

    //            // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
    //            // If no response comes back within this time then proceed
    //            _clientDone.WaitOne(TIMEOUT_MILLISECONDS);
    //        }
    //        else
    //        {
    //            response = "Socket is not initialized";
    //        }

    //        return response;
    //    }
    //    /// <summary>
    //    /// Receive data from the server using the established socket connection
    //    /// </summary>
    //    /// <returns>The data received from the server</returns>
    //    public string Receive()
    //    {
    //        string response = "Operation Timeout";

    //        // We are receiving over an established socket connection
    //        if (_socket != null)
    //        {
    //            // Create SocketAsyncEventArgs context object
    //            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
    //            socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;

    //            // Setup the buffer to receive the data
    //            socketEventArg.SetBuffer(new Byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);

    //            // Inline event handler for the Completed event.
    //            // Note: This even handler was implemented inline in order to make 
    //            // this method self-contained.
    //            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
    //            {
    //                if (e.SocketError == SocketError.Success)
    //                {
    //                    // Retrieve the data from the buffer
    //                    response = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
    //                    response = response.Trim('\0');
    //                }
    //                else
    //                {
    //                    response = e.SocketError.ToString();
    //                }

    //                _clientDone.Set();
    //            });

    //            // Sets the state of the event to nonsignaled, causing threads to block
    //            _clientDone.Reset();

    //            // Make an asynchronous Receive request over the socket
    //            _socket.ReceiveAsync(socketEventArg);

    //            // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
    //            // If no response comes back within this time then proceed
    //            _clientDone.WaitOne(TIMEOUT_MILLISECONDS);
    //        }
    //        else
    //        {
    //            response = "Socket is not initialized";
    //        }

    //        return response;
    //    }

    //    /// <summary>
    //    /// Closes the Socket connection and releases all associated resources
    //    /// </summary>
    //    public void Close()
    //    {
    //        if (_socket != null)
    //        {
    //            _socket.Close();
    //        }
    //    }
    //}
}

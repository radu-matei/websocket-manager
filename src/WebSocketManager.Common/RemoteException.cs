using System;

namespace WebSocketManager.Common
{
    /// <summary>
    /// An exception that occured remotely.
    /// </summary>
    /// <seealso cref="System.Exception"/>
    public class RemoteException : Exception
    {
        /// <summary>
        /// The actual remote exception.
        /// </summary>
        private Exception m_RemoteException;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteException"/> class.
        /// </summary>
        /// <param name="remoteException">The remote exception.</param>
        public RemoteException(Exception remoteException) : base("", remoteException)
        {
            m_RemoteException = remoteException;
        }

        public override string Message => $"A remote exception occured: '{m_RemoteException.Message}'.";
    }
}
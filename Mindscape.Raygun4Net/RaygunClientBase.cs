﻿using System;
using System.Net;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  public abstract class RaygunClientBase
  {
    protected internal const string SentKey = "AlreadySentByRaygun";

    /// <summary>
    /// Gets or sets the user identity string.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Gets or sets information about the user including the identity string.
    /// </summary>
    public RaygunIdentifierMessage UserInfo { get; set; }

    /// <summary>
    /// Gets or sets the username/password credentials which are used to authenticate with the system default Proxy server, if one is set
    /// and requires credentials.
    /// </summary>
    public ICredentials ProxyCredentials { get; set; }

    /// <summary>
    /// Gets or sets a custom application version identifier for all error messages sent to the Raygun.io endpoint.
    /// </summary>
    public string ApplicationVersion { get; set; }

    protected bool CanSend(Exception exception)
    {
      return exception == null || exception.Data == null || !exception.Data.Contains(SentKey) || false.Equals(exception.Data[SentKey]);
    }

    protected void FlagAsSent(Exception exception)
    {
      if (exception != null && exception.Data != null)
      {
        try
        {
          Type[] genericTypes = exception.Data.GetType().GetGenericArguments();
          if (genericTypes.Length == 0 || genericTypes[0].IsAssignableFrom(typeof(string)))
          {
            exception.Data[SentKey] = true;
          }
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine(String.Format("Failed to flag exception as sent: {0}", ex.Message));
        }
      }
    }

    /// <summary>
    /// Raised just before a message is sent. This can be used to make final adjustments to the <see cref="RaygunMessage"/>, or to cancel the send.
    /// </summary>
    public event EventHandler<RaygunSendingMessageEventArgs> SendingMessage;

    private bool _handlingRecursiveErrorSending;

    // Returns true if the message can be sent, false if the sending is canceled.
    protected bool OnSendingMessage(RaygunMessage raygunMessage)
    {
      bool result = true;

      if (!_handlingRecursiveErrorSending)
      {
        EventHandler<RaygunSendingMessageEventArgs> handler = SendingMessage;
        if (handler != null)
        {
          RaygunSendingMessageEventArgs args = new RaygunSendingMessageEventArgs(raygunMessage);
          try
          {
            handler(this, args);
          }
          catch (Exception e)
          {
            // Catch and send exceptions that occur in the SendingMessage event handler.
            // Set the _handlingRecursiveErrorSending flag to prevent infinite errors.
            _handlingRecursiveErrorSending = true;
            Send(e);
            _handlingRecursiveErrorSending = false;
          }
          result = !args.Cancel;
        }
      }

      return result;
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public abstract void Send(Exception exception);
  }
}
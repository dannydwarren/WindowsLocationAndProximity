﻿#if NETFX_CORE

#endif

#if WINDOWS_PHONE

using System.Windows;
using LocationAndProximityPoC_WP8.FeatureWrappers;
using System.Windows.Threading;
using Windows.Storage.Streams;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace ProximityDemo_Universal
{
	public enum PeerFindingState
	{
		Inactive,
		Searching,
		PeerFound,
		Connecting,
		Connected,
	}

	public class NfcWrapper
	{
		private NfcWrapper()
		{
			//TODO: 0 - NfcWrapper.cs Implement Ctor and Initialize Proximity Device	
			// For more information: http://msdn.microsoft.com/en-us/library/windowsphone/develop/jj207060(v=vs.105).aspx

			/*
			 * Update the WMAppManifest file
			 *	Capabilities
			 *		Enable ID_CAP_NETWORKING and ID_CAP_PROXIMITY
			 * 
			 * Get an instance of ProximityDevice via the Static Method GetDefault() 
			 *	and store the value in a field
			 * 
			 * Set ProximityMessagingStatus
			 *	If value == null 
			 *		ProximityMessagingStatus.NotSupported
			 *	else
			 *		ProximityMessagingStatus.Idle
			 * 
			 */

			_proximityDevice = ProximityDevice.GetDefault();

			ProximityMessagingStatus = _proximityDevice != null ? ProximityMessagingStatus.Idle : ProximityMessagingStatus.NotSupported;
		}

		private static NfcWrapper _instance;
		public static NfcWrapper Instance
		{
			get
			{
				if ( _instance == null )
					_instance = new NfcWrapper();
				return _instance;
			}
		}

		#region Proximity

		public static readonly string MESSAGE_TYPE_PREFIX = "Windows."; //NOTE: This is a mandatory messageType prefix

		private readonly ProximityDevice _proximityDevice;
		private long _activeMessageId = -1;

		public event EventHandler ConnectionStatusChanged = delegate { };
		private void OnConnectionStatusChanged()
		{
			ConnectionStatusChanged( this, EventArgs.Empty );
		}

		private ProximityMessagingStatus _proximityMessagingStatus;
		private PeerFindingState _state;

		public ProximityMessagingStatus ProximityMessagingStatus
		{
			get { return _proximityMessagingStatus; }
			set
			{
				if ( value == _proximityMessagingStatus )
					return;

				_proximityMessagingStatus = value;
				OnConnectionStatusChanged();
			}
		}

		public void SubscribeForMessage( string messageType, Action<string> messageReceivedCallback )
		{
			//TODO: 1.0 - NfcWrapper.cs Implement SubscribeForMessage
			/*
			 * if _activeMessageId field == -1 (if a message is not currently being processed) and ProximityMessagingStatus is Idle
			 *	Use the _proximityDevice to SubscribeForMessage and set _activeMessageId to the returned value
			 *		use a lambda to call the callback ex: (s, m) => messageReceivedCallback(m.DataAsString)
			 *	Set ProximityMessagingStatus to Subscribed
			 * 
			 */

			if ( _activeMessageId == -1 && ProximityMessagingStatus == ProximityMessagingStatus.Idle )
			{
			    _activeMessageId =
			        _proximityDevice.SubscribeForMessage(messageType,
			            (proximityDevice, proximityMessage) => messageReceivedCallback(proximityMessage.DataAsString));
				ProximityMessagingStatus = ProximityMessagingStatus.Subscribed;
			}
		}

		public void SubscribeForMessage<T>( string messageType, Action<T> messageReceivedCallback )
		{
			SubscribeForMessage( messageType, s => messageReceivedCallback( Deserialize<T>( s ) ) );
		}

		public void StopSubscribingForMessage()
		{
			//TODO: 1.1 - NfcWrapper.cs Implement StopSubscribingForMessage
			/*
			 * if _activeMessageId != -1 and ProximityMessagingStatus is Subscribed
			 *	use _proximityDevice to StopSubscribingForMessage _activeMessageId
			 *	set _activeMessageId to -1
			 *	set ProximityMessagingStatus to Idle
			 * 
			 */

			if ( _activeMessageId != -1 && ProximityMessagingStatus == ProximityMessagingStatus.Subscribed )
			{
				_proximityDevice.StopSubscribingForMessage( _activeMessageId );
				_activeMessageId = -1;
				ProximityMessagingStatus = ProximityMessagingStatus.Idle;
			}
		}

		public void StartPublishing( string messageType, string message )
		{
			//TODO: 2.0 - NfcWrapper.cs Implement StartPublishing
			/*
			 * if _activeMessageId field == -1 (if a message is not currently being processed) and ProximityMessagingStatus is Idle
			 *	use _proximityDevice to PublishMessage with available parameters 
			 *		and store the returned messageId in the _activeMessageId field
			 *	Set ProximityMessagingStatus to Publishing
			 * 
			 */

			if ( _activeMessageId == -1 && ProximityMessagingStatus == ProximityMessagingStatus.Idle )
			{
				_activeMessageId = _proximityDevice.PublishMessage( messageType, message);
				ProximityMessagingStatus = ProximityMessagingStatus.Publishing;
			}
		}

		/// <summary>
		/// SUGGESTION: The messageType parameter should include the type name you are sending to help ensure the data is recognized and processed correctly.
		/// </summary>
		public void StartPublishing( string messageType, object value )
		{
			StartPublishing( messageType, Serialize( value ) );
		}

		public void StopPublishing()
		{
			//TODO: 2.1 - NfcWrapper.cs Implement StopPublishing
			/*
			 * if _activeMessageId != -1 and ProximityMessagingStatus is Publishing
			 *	call _proximityDevice.StopPublishing with the _activeMessageId
			 *	set _activeMessageId to -1
			 *	set ProximityMessagingStatus to Idle
			 */

			if ( _activeMessageId != -1 && ProximityMessagingStatus == ProximityMessagingStatus.Publishing )
			{
				_proximityDevice.StopPublishingMessage( _activeMessageId );
				_activeMessageId = -1;
				ProximityMessagingStatus = ProximityMessagingStatus.Idle;
			}
		}

		private static string Serialize( object objectToSerialize )
		{
			using ( MemoryStream ms = new MemoryStream() )
			{
				DataContractSerializer serializer = new DataContractSerializer( objectToSerialize.GetType() );
				serializer.WriteObject( ms, objectToSerialize );
				ms.Position = 0;

				using ( StreamReader reader = new StreamReader( ms ) )
				{
					return reader.ReadToEnd();
				}
			}
		}

		private static T Deserialize<T>( string jsonString )
		{
			using ( MemoryStream ms = new MemoryStream( Encoding.Unicode.GetBytes( jsonString ) ) )
			{
				DataContractSerializer serializer = new DataContractSerializer( typeof( T ) );
				return (T)serializer.ReadObject( ms );
			}
		}

		#endregion

		#region PeerFinding

		public PeerFindingState State
		{
			get { return _state; }
			private set
			{
				if ( _state == value )
				{
					return;
				}
				_state = value;
				OnStateChanged();
			}
		}

		public StreamSocketManager PeerSocket { get; private set; }

		public event EventHandler StateChanged = delegate { };
		private void OnStateChanged()
		{
			StateChanged( this, EventArgs.Empty );
		}


		public void AdvertiseForPeers( string displayName, bool isHost, Dictionary<string, string> alternateIdentities = null )
		{
			if ( State != PeerFindingState.Inactive )
			{
				return;
			}

			PeerFinder.DisplayName = displayName; //could be stored LocalStorage
			if ( !isHost )
				PeerFinder.ConnectionRequested += ConnectionRequested;
			PeerFinder.TriggeredConnectionStateChanged += TriggeredConnectionStateChanged;

			if ( alternateIdentities != null )
			{
				foreach ( KeyValuePair<string, string> alternateIdentity in alternateIdentities )
				{
					if ( PeerFinder.AlternateIdentities.Keys.Contains( alternateIdentity.Key ) )
						continue;

					PeerFinder.AlternateIdentities.Add( alternateIdentity );
				}
			}

			PeerFinder.Start();
			SearchForPeers();

			if ( (PeerFinder.SupportedDiscoveryTypes & PeerDiscoveryTypes.Triggered) == PeerDiscoveryTypes.Triggered )
			{
				NotifyUser( "You can tap to connect a peer device that is also advertising for a connection." );
			}
			else
			{
				NotifyUser( "Tap to connect is not supported." );
			}

			if ( (PeerFinder.SupportedDiscoveryTypes & PeerDiscoveryTypes.Browse) != PeerDiscoveryTypes.Browse )
			{
				NotifyUser( "Peer discovery using Wifi-Direct is not supported." );
			}
		}

		public void DisconnectAndClosePeerConnections()
		{
			if ( State != PeerFindingState.Inactive )
			{
				PeerFinder.Stop();
				PeerFinder.ConnectionRequested -= ConnectionRequested;
				PeerFinder.TriggeredConnectionStateChanged -= TriggeredConnectionStateChanged;

				if ( PeerSocket != null )
				{
					PeerSocket.Dispose();
					PeerSocket = null;
				}

				State = PeerFindingState.Inactive;
			}
		}

		private async void ConnectionRequested( object sender, ConnectionRequestedEventArgs args )
		{
			string message = "Connection requested by " + args.PeerInformation.DisplayName + ". Click 'OK' to connect.";
			string title = "Peer Connection Request";
			bool connectionAccepted;
#if NETFX_CORE

			var dialog = new MessageDialog( message, title );

			string okLabel = "OK";
			dialog.Commands.Add( new UICommand( okLabel ) );
			dialog.Commands.Add( new UICommand( "Cancel" ) );
			IUICommand command = await dialog.ShowAsync();

			connectionAccepted = command.Label == okLabel;
#endif

#if WINDOWS_PHONE
			NotifyUser( "Connection requested by " + args.PeerInformation.DisplayName + " and will be automatically accepted for now on Windows Phone." );
			//TODO: MessageBox.Show must be called on the UI thread otherwise it will not display to the user, 
			//	but in order to do this we have to dispatch the call by using Deployment.Current.BeginInvoke and the problem with this is the need to get a response. 
			//	This code needs to be refactored for both WinRT and WP8 so both can be supported correctly and prompt for acceptance.
			connectionAccepted = true; //MessageBox.Show( message, title, MessageBoxButton.OKCancel ) == MessageBoxResult.OK;
#endif

			if ( connectionAccepted )
			{
				await ConnectToPeer( args.PeerInformation );
			}
		}

		private void TriggeredConnectionStateChanged( object sender, TriggeredConnectionStateChangedEventArgs args )
		{
			switch ( args.State )
			{
				case TriggeredConnectState.PeerFound:
					State = PeerFindingState.PeerFound;
					NotifyUser( "Peer found. You may now pull your devices out of proximity." );
					break;
				case TriggeredConnectState.Listening:
					NotifyUser( "Listening for peer." );
					break;
				case TriggeredConnectState.Connecting:
					State = PeerFindingState.Connecting;
					NotifyUser( "Connecting to peer." );
					break;
				case TriggeredConnectState.Completed:
					PeerSocket = new StreamSocketManager( args.Socket );
					State = PeerFindingState.Connected;
					NotifyUser( "Connected. You may now send a message." );
					break;
				case TriggeredConnectState.Canceled:
					NotifyUser( "Canceled finding peer." );
					break;
				case TriggeredConnectState.Failed:
					NotifyUser( "Failed to find peer." );
					break;
			}
		}


		public event EventHandler<EventArgs<IReadOnlyList<PeerInformation>>> PeersFound = delegate { };
		private void OnPeersFound( IReadOnlyList<PeerInformation> foundPeers )
		{
			PeersFound( this, new EventArgs<IReadOnlyList<PeerInformation>>( foundPeers ) );
		}

		private async void SearchForPeers()
		{
			State = PeerFindingState.Searching;
			while ( State == PeerFindingState.Searching )
			{
				IReadOnlyList<PeerInformation> foundPeers = await FindAvailablePeers();
				if ( foundPeers != null )
				{
					OnPeersFound( foundPeers );
				}
			}
		}

		private async Task<IReadOnlyList<PeerInformation>> FindAvailablePeers()
		{
			if ( State == PeerFindingState.Inactive )
			{
				return null;
			}

			IReadOnlyList<PeerInformation> peerInfoCollection = null;
			string errorMessage = null;
			try
			{
				peerInfoCollection = await PeerFinder.FindAllPeersAsync();
			}
			catch ( Exception e )
			{
				errorMessage = "Error finding peers: " + e.Message;
			}

			if ( errorMessage != null )
			{
				NotifyUser( errorMessage );
				//Possibly request retry?
			}

			return peerInfoCollection;
		}

		public async Task<StreamSocketManager> ConnectToPeer( PeerInformation peerInformation )
		{
			switch ( State )
			{
				case PeerFindingState.Inactive:
					NotifyUser( "You must first advertise for peers before trying to connect to one." );
					return null;
				case PeerFindingState.Connected:
					NotifyUser( "You are already connected to a peer" );
					return null;
				default:
					State = PeerFindingState.Connecting;
					break;
			}

			string errorMessage = null;
			try
			{
				PeerSocket = new StreamSocketManager( await PeerFinder.ConnectAsync( peerInformation ) );
				State = PeerFindingState.Connected;
			}
			catch ( Exception e )
			{
				errorMessage = "Connection Failed: " + e.Message;
			}

			if ( errorMessage != null )
			{
				NotifyUser( errorMessage );
				//Possibly request retry?
			}

			return PeerSocket;
		}

		#endregion

		internal static async void NotifyUser( string message )
		{
#if NETFX_CORE
			//dialog.ShowAsync() is getting an access exception when using this code... I don't get it...
			if ( !CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess )
			{
				CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync( CoreDispatcherPriority.Normal, () => NotifyUser( message ) );
				return;

			}
			try
			{
				var dialog = new MessageDialog( message );
				await dialog.ShowAsync();
			}
			catch ( UnauthorizedAccessException e )
			{
				//Not sure why we ar getting this....
			}
#endif

#if WINDOWS_PHONE

			Deployment.Current.Dispatcher.BeginInvoke( () => MessageBox.Show( message ) );

			//MessageBox.Show( message );
			Debug.WriteLine( message );
#endif
		}
	}

	public enum ProximityMessagingStatus
	{
		NotSupported,
		Idle,
		Publishing,
		Subscribed
	}
 
    public class StreamSocketManager : IDisposable
    {
        private readonly DataReader _reader;
        private readonly DataWriter _writer;
        private readonly CancellationTokenSource receiveMessageCancellationTokenSource = new CancellationTokenSource();
        public StreamSocketManager(StreamSocket socket)
        {
            Socket = socket;
            _reader = new DataReader(Socket.InputStream);
            _writer = new DataWriter(Socket.OutputStream);
        }

        public StreamSocket Socket { get; private set; }

        public async void SendMessage(string message)
        {
            await _writer.SendLengthPrefixedStringAsync(message);
        }

        public async Task<string> ReceiveMessage()
        {
            Task<string> receiveMessageTask = _reader.ReceiveLengthPrefixedStringAsync();
            await receiveMessageTask.WaitOrCancel(receiveMessageCancellationTokenSource.Token);
            if (receiveMessageTask.IsFaulted)
            {
                NfcWrapper.NotifyUser("PeerSocket Connection Failed");
                NfcWrapper.Instance.DisconnectAndClosePeerConnections();
                return null;
            }
            return receiveMessageTask.Result;
        }

        public void Dispose()
        {
            receiveMessageCancellationTokenSource.Cancel();
            Socket.Dispose();
        }
    }
    
    public static class DataReaderWriterExtensions
	{
		private static async Task SendInternalAsync( this DataWriter writer, object value )
		{
			if ( value == null )
				throw new ArgumentNullException( "value" );


			if ( value is Int32 )
				writer.WriteInt32( (int)value );
			else if ( value is UInt32 )
				writer.WriteUInt32( (uint)value );
			else if ( value is string )
			{
				var str = (string)value;
				var unitCount = writer.MeasureString( str );
				var stringLength = (uint)str.Length;
				var byteCount = unitCount;

				// Double the number of bytes for UTF16
				if ( writer.UnicodeEncoding != UnicodeEncoding.Utf8 )
					byteCount = unitCount * 2;


				// Total bytes to write:
				//    o  4 bytes for total payload length (charCount + stringLength + string data)
				//    o  4 bytes for charCount
				//    o  4 bytes for stringLength
				//    o  n bytes for string
				writer.WriteUInt32( sizeof( UInt32 ) + sizeof( UInt32 ) + byteCount );
				writer.WriteUInt32( unitCount );
				writer.WriteUInt32( stringLength );
				writer.WriteString( str );
			}
			else
			{
				throw new ArgumentOutOfRangeException( "value" );
			}

			try
			{
				await writer.StoreAsync();
			}
			catch ( Exception ex )
			{
				throw;
			}
		}

		private static async Task<object> ReceiveInternalAsync( this DataReader reader, Type type )
		{
			if ( type == null )
				throw new ArgumentNullException( "type" );


			uint expectedBytes = 0;
			uint bytesRead;

			if ( type == typeof( Int32 ) )
				expectedBytes = sizeof( Int32 );
			else if ( type == typeof( UInt32 ) )
				expectedBytes = sizeof( UInt32 );
			else if ( type == typeof( string ) )
				expectedBytes = await reader.ReceiveUInt32Async();
			else
			{
				throw new ArgumentOutOfRangeException( "type" );
			}

			try
			{
				bytesRead = await reader.LoadAsync( expectedBytes );
			}
			catch ( Exception ex )
			{
				throw;
			}


			if ( bytesRead != expectedBytes )
			{
				throw new EndOfStreamException();
			}

			object result = null;
			if ( type == typeof( Int32 ) )
				result = reader.ReadInt32();
			else if ( type == typeof( UInt32 ) )
				result = reader.ReadUInt32();
			else if ( type == typeof( string ) )
			{
				var unitCount = reader.ReadUInt32();
				var expectedStringLength = reader.ReadUInt32();
				var byteCount = bytesRead - sizeof( Int32 );
				result = reader.ReadString( unitCount );

				if ( ((string)result).Length != expectedStringLength )
				{
					throw new Exception( "String encoding broken(?)" );
				}
			}

			return result;
		}

		public static Task SendInt32Async( this DataWriter writer, int value )
		{
			return writer.SendInternalAsync( value );
		}

		public static Task SendUInt32Async( this DataWriter writer, uint value )
		{
			return writer.SendInternalAsync( value );
		}

		public static Task SendLengthPrefixedStringAsync( this DataWriter writer, string value )
		{
			return writer.SendInternalAsync( value );
		}

		public static async Task<Int32> ReceiveInt32Async( this DataReader reader )
		{
			return (Int32)await reader.ReceiveInternalAsync( typeof( Int32 ) );
		}

		public static async Task<UInt32> ReceiveUInt32Async( this DataReader reader )
		{
			return (UInt32)await reader.ReceiveInternalAsync( typeof( UInt32 ) );
		}

		public static async Task<string> ReceiveLengthPrefixedStringAsync( this DataReader reader )
		{
			return (string)await reader.ReceiveInternalAsync( typeof( string ) );
		}
	}

	public static class TaskExtensions
	{
		public static Task WaitOrCancel( this Task task, int milliseconds )
		{
			return task.WaitOrCancel( new CancellationTokenSource( milliseconds ).Token );
		}

		public static Task WaitOrCancel( this Task task, TimeSpan timespan )
		{
			return task.WaitOrCancel( new CancellationTokenSource( timespan ).Token );
		}

		public static Task<Task> WaitOrCancel( this Task task, CancellationToken token )
		{
			// Create a task that will complete when the token is signalled
			var taskSource = new TaskCompletionSource<bool>();
			Task.Run( () =>
			{
				token.WaitHandle.WaitOne();
				taskSource.TrySetResult( true );
			} );

			return Task.Run( () =>
			{
				int index = Task.WaitAny( new[] { task, taskSource.Task } );
				if ( index == 0 )
					return Task.FromResult( task );
				else if ( token.IsCancellationRequested )
					return new Task<Task>( () => new Task( () => { } ) );
				else
					throw new TimeoutException( "timeout" );
			} );
		}
	}

}

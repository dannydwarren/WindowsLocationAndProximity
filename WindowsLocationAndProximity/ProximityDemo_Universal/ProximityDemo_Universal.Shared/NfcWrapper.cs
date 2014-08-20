using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace ProximityDemo_Universal
{
	#region NFC

	public enum NfcMessagingStatus
	{
		NotSupported,
		Idle,
		Publishing,
		Subscribed
	}

	public class NfcWrapper
	{

		#region Singleton
		private static NfcWrapper _instance;
		public static NfcWrapper Instance
		{
			get { return _instance ?? ( _instance = new NfcWrapper() ); }
		}
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
			MessagingStatus = _proximityDevice != null ? NfcMessagingStatus.Idle : NfcMessagingStatus.NotSupported;
		}
		#endregion

		public static readonly string MESSAGE_TYPE_PREFIX = "Windows."; //NOTE: This is a mandatory messageType prefix

		private readonly ProximityDevice _proximityDevice;
		private long _activeMessageId = -1;

		private NfcMessagingStatus _messagingStatus;
		public NfcMessagingStatus MessagingStatus
		{
			get { return _messagingStatus; }
			set
			{
				if ( value == _messagingStatus )
					return;

				_messagingStatus = value;
			}
		}


		public void StartPublishing( string messageType, string message )
		{
			//TODO: NfcWrapper 1.0 - StartPublishing
			/*
			 * Pass the desired messageType and message to PublishMessage
			 * Save the message Id returned
			 * 
			 * NOTE: All messageTypes must be prefixed with "Windows."
			 */

			if ( MessagingStatus == NfcMessagingStatus.Idle )
			{
				_activeMessageId = _proximityDevice.PublishMessage( MESSAGE_TYPE_PREFIX + messageType, message );

				MessagingStatus = NfcMessagingStatus.Publishing;
			}
		}

		public void StopPublishing()
		{
			//TODO: NfcWrapper 2.0 - StopPublishing
			/*
			 * Pass the saved message Id to StopPublishingMessage
			 * 
			 */

			if ( MessagingStatus == NfcMessagingStatus.Publishing )
			{
				_proximityDevice.StopPublishingMessage( _activeMessageId );

				_activeMessageId = -1;
				MessagingStatus = NfcMessagingStatus.Idle;
			}
		}

		public void SubscribeForMessage( string messageType, Action<string> messageReceivedCallback )
		{
			//TODO: NfcWrapper 3.0 - SubscribeForMessage
			/*
			 * SubscribeForMessage
			 * Save the returned message Id
			 */

			if ( MessagingStatus == NfcMessagingStatus.Idle )
			{
				_activeMessageId =
					_proximityDevice.SubscribeForMessage( MESSAGE_TYPE_PREFIX + messageType,
						( proximityDevice, proximityMessage ) => messageReceivedCallback( proximityMessage.DataAsString ) );

				MessagingStatus = NfcMessagingStatus.Subscribed;
			}
		}

		public void StopSubscribingForMessage()
		{
			//TODO: NfcWrapper 4.0 - StopSubscribingForMessage
			/*
			 * Pass the saved message Id from SubscribeForMessage into StopSubscribingForMessage
			 * 
			 */

			if ( MessagingStatus == NfcMessagingStatus.Subscribed )
			{
				_proximityDevice.StopSubscribingForMessage( _activeMessageId );

				_activeMessageId = -1;
				MessagingStatus = NfcMessagingStatus.Idle;
			}
		}
	}

	#endregion

	#region PeerFinding

	public enum PeerFindingState
	{
		Inactive,
		Searching,
		PeerFound,
		Connecting,
		Connected,
	}

	public class PeerFinderWrapper
	{
		#region Singleton
		private static PeerFinderWrapper _instance;
		public static PeerFinderWrapper Instance
		{
			get { return _instance ?? ( _instance = new PeerFinderWrapper() ); }
		}
		private PeerFinderWrapper()
		{

		}
		#endregion


		private StreamSocket _socket;

		public StreamSocketManager PeerSocket { get; private set; }

		private PeerFindingState _state;
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

		public event EventHandler StateChanged = delegate { };
		private void OnStateChanged()
		{
			StateChanged( this, EventArgs.Empty );
		}

		public event EventHandler<EventArgs<IReadOnlyList<PeerInformation>>> PeersFound = delegate { };
		private void OnPeersFound( IReadOnlyList<PeerInformation> foundPeers )
		{
			PeersFound( this, new EventArgs<IReadOnlyList<PeerInformation>>( foundPeers ) );
		}

		public void AdvertiseForPeers( string displayName, bool isHost, Dictionary<string, string> alternateIdentities = null )
		{
			//TODO: PeerFinderWrapper 1.0 - AdvertiseForPeers
			/*
			 * Specifiy DisplayName
			 * Subscribe to the desired events
			 *		TriggeredConnectionStateChanged - For Tap to Find Peer
			 *		ConnectionRequested - For client to be notified when the host has submitted a connection request
			 * AlternateIdenties - allow multiple apps to work together like a Windows App and a Windows Phone App
			 * 
			 * PeerFinder.Start() - starts the device PeerFinder service
			 *		Windows - Searches via WiFi Direct ONLY
			 *		Windows Phone - Searches via BlueTooth ONLY
			 *		NOTE: This means that Windows Apps and Windows Phone Apps can find eachother ONLY via NFC Tap to Find Peer
			 *			Infrastructure - This is how ALL devices actually talk to eachother. Devices can communicate via infrastructure when
			 *				they are connected to the same network on the same router. That router must NOT be blocking the specific types of 
			 *				communication packets or connections will not succeed.
			 *			BlueTooth - Windows Phone can break the Infrastructure rule and communicate over BlueTooth, but it's not as fast as WiFi.
			 * 
			 */

			if ( State != PeerFindingState.Inactive )
			{
				return;
			}

			PeerFinder.DisplayName = displayName; //could be stored LocalStorage
			PeerFinder.TriggeredConnectionStateChanged += TriggeredConnectionStateChanged;

			if ( !isHost )
			{
				PeerFinder.ConnectionRequested += ConnectionRequested;
			}

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

			if ( ( PeerFinder.SupportedDiscoveryTypes & PeerDiscoveryTypes.Triggered ) == PeerDiscoveryTypes.Triggered )
			{
				NotifyUser( "You can tap to connect a peer device that is also advertising for a connection." );
			}
			else
			{
				NotifyUser( "Tap to connect is not supported." );
			}

			if ( ( PeerFinder.SupportedDiscoveryTypes & PeerDiscoveryTypes.Browse ) != PeerDiscoveryTypes.Browse )
			{
				NotifyUser( "Peer discovery using Wifi-Direct is not supported." );
			}
		}

		private int _findPeerFailures;
		private async void SearchForPeers()
		{
			//TODO: PeerFinderWrapper 2.0 - SearchForPeers
			/*
			 * Asynchronously search for peers via PeerFinder.FindAllPeersAsync
			 * 
			 */

			State = PeerFindingState.Searching;
			while ( State == PeerFindingState.Searching )
			{
				IReadOnlyList<PeerInformation> peerInfoCollection = null;
				string errorMessage = null;
				try
				{
					peerInfoCollection = await PeerFinder.FindAllPeersAsync();
				}
				catch ( Exception e )
				{
					_findPeerFailures++;
					errorMessage = "Error finding peers. Disconnecting. Details: " + e.Message;
				}

				if ( errorMessage != null && _findPeerFailures > 3 )
				{
					NotifyUser( errorMessage );
					DisconnectAndClosePeerConnections();
					break;
				}

				if ( peerInfoCollection != null )
				{
					OnPeersFound( peerInfoCollection );
				}
			}
		}

		public async Task ConnectToPeer( PeerInformation peerInformation )
		{
			//TODO: PeerFinderWrapper 3.0 - ConnectToPeer
			/*
			 * Use peerInformation to obtain a socket connection to that peer via PeerFinder.ConnectAsync
			 * Create StreamSocketManager with the obtained socket connection for sending and receiving messages
			 * 
			 */

			switch ( State )
			{
				case PeerFindingState.Inactive:
					NotifyUser( "You must first advertise for peers before trying to connect to one." );
					return;
				case PeerFindingState.Connected:
					NotifyUser( "You are already connected to a peer" );
					return;
				default:
					State = PeerFindingState.Connecting;
					break;
			}

			string errorMessage = null;
			try
			{
				_socket = await PeerFinder.ConnectAsync( peerInformation );
				NotifyUser( "Connected to socket." );
				PeerSocket = new StreamSocketManager( _socket );
				NotifyUser( "Connected with manager. You may now send a message." );
				State = PeerFindingState.Connected;
			}
			catch ( Exception e )
			{
				errorMessage = "Connection Failed: " + e.Message;
			}

			if ( errorMessage != null )
			{
				NotifyUser( errorMessage );
			}
		}

		private async void ConnectionRequested( object sender, ConnectionRequestedEventArgs args )
		{
			//TODO: PeerFinderWrapper 4.0 - ConnectionRequested
			/*
			 * Client is informed a host is requesting connection, open a socket to the host
			 * 
			 */

			await ConnectToPeer( args.PeerInformation );
		}

		private void TriggeredConnectionStateChanged( object sender, TriggeredConnectionStateChangedEventArgs args )
		{
			//TODO: PeerFinderWrapper 5.0 - TriggeredConnectionStateChanged
			/*
			 * Tap to Find Peer provides a socket ready for use
			 * Create StreamSocketManager with the obtained socket connection for sending and receiving messages
			 * 
			 */

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
					_socket = args.Socket;
					NotifyUser( "Connected to socket." );
					PeerSocket = new StreamSocketManager( _socket );
					NotifyUser( "Connected with manager. You may now send a message." );
					State = PeerFindingState.Connected;
					break;
				case TriggeredConnectState.Canceled:
					NotifyUser( "Canceled finding peer." );
					break;
				case TriggeredConnectState.Failed:
					NotifyUser( "Failed to find peer." );
					break;
			}
		}

		public void DisconnectAndClosePeerConnections()
		{
			//TODO: PeerFinderWrapper 8.0 - DisconnectAndClosePeerConnections
			/*
			 * Deactivate the devices peer finding service via PeerFinder.Stop
			 *		Terminates all connections
			 */

			if ( State != PeerFindingState.Inactive )
			{
				PeerFinder.Stop();
				PeerFinder.ConnectionRequested -= ConnectionRequested;
				PeerFinder.TriggeredConnectionStateChanged -= TriggeredConnectionStateChanged;

				if ( PeerSocket != null )
				{
					PeerSocket.Dispose();
					PeerSocket = null;
					_socket = null;
				}

				State = PeerFindingState.Inactive;
			}
		}



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
	#endregion

	#region Helpers

	public class StreamSocketManager : IDisposable
	{
		private readonly DataReader _reader;
		private readonly DataWriter _writer;
		private readonly CancellationTokenSource receiveMessageCancellationTokenSource = new CancellationTokenSource();
		public StreamSocketManager( StreamSocket socket )
		{
			Socket = socket;
			_reader = new DataReader( Socket.InputStream );
			_writer = new DataWriter( Socket.OutputStream );
		}

		public StreamSocket Socket { get; private set; }

		public async void SendMessage( string message )
		{
			await _writer.SendLengthPrefixedStringAsync( message );
		}

		public async Task<string> ReceiveMessage()
		{
			Task<string> receiveMessageTask = _reader.ReceiveLengthPrefixedStringAsync();
			await receiveMessageTask.WaitOrCancel( receiveMessageCancellationTokenSource.Token );
			if ( receiveMessageTask.IsFaulted )
			{
				PeerFinderWrapper.NotifyUser( "PeerSocket Connection Failed" );
				PeerFinderWrapper.Instance.DisconnectAndClosePeerConnections();
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
				writer.WriteInt32( (int) value );
			else if ( value is UInt32 )
				writer.WriteUInt32( (uint) value );
			else if ( value is string )
			{
				var str = (string) value;
				var unitCount = writer.MeasureString( str );
				var stringLength = (uint) str.Length;
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

				if ( ( (string) result ).Length != expectedStringLength )
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
			return (Int32) await reader.ReceiveInternalAsync( typeof( Int32 ) );
		}

		public static async Task<UInt32> ReceiveUInt32Async( this DataReader reader )
		{
			return (UInt32) await reader.ReceiveInternalAsync( typeof( UInt32 ) );
		}

		public static async Task<string> ReceiveLengthPrefixedStringAsync( this DataReader reader )
		{
			return (string) await reader.ReceiveInternalAsync( typeof( string ) );
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

	#endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Networking.Proximity;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using LocationDemo_Win8.FeatureWrappers;
using ProximityDemo_Win8.FeatureWrappers;

namespace ProximityDemo_Win8
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

		#region Proximity

		private static readonly string MESSAGE_TYPE = NfcWrapper.MESSAGE_TYPE_PREFIX + "LocationPoC.Message";
		private void PublishMessage_Click( object sender, RoutedEventArgs e )
		{
			NfcWrapper.Instance.StartPublishing( MESSAGE_TYPE, MessageToSend.Text );
		}

		private void StopPublishingMessage_Click( object sender, RoutedEventArgs e )
		{
			NfcWrapper.Instance.StopPublishing();
		}

		private void SubscribeMessage_Click( object sender, RoutedEventArgs e )
		{
			NfcWrapper.Instance.SubscribeForMessage( MESSAGE_TYPE, MessageReceivedHandler );
			MessageReceived.Text = "Waiting...";
		}

		private void MessageReceivedHandler( string value )
		{
			CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal, () => MessageReceived.Text = value );
		}

		private void StopSubscribingMessage_Click( object sender, RoutedEventArgs e )
		{
			NfcWrapper.Instance.StopSubscribingForMessage();
		}

		#endregion

		#region PeerFinding

		private void SendSocketMessage_Click( object sender, RoutedEventArgs e )
		{
			if ( NfcWrapper.Instance.PeerSocket != null )
			{
				NfcWrapper.Instance.PeerSocket.SendMessage( SocketMessageToSend.Text );
			}
		}

		private Dictionary<string, string> _alternateIdentities = new Dictionary<string, string>
			                                                          {
				                                                          {
					                                                          "WindowsPhone", "{baa00bef-d711-44aa-b6fb-73d542b98ff1}"
				                                                          }
			                                                          };
		private void AdvertiseForPeers_Click( object sender, RoutedEventArgs e )
		{
			if ( NfcWrapper.Instance.State == PeerFindingState.Inactive )
			{
				NfcWrapper.Instance.StateChanged += NfcStateChanged;
				NfcWrapper.Instance.PeersFound += NfcPeersFound;
				NfcWrapper.Instance.AdvertiseForPeers( "WinRT (HOST)", true, _alternateIdentities );
			}
		}

		private void NfcPeersFound( object sender, EventArgs<IReadOnlyList<PeerInformation>> e )
		{
			if ( e.Payload != null )
			{
				var selectedPeer = Peers.SelectedItem as PeerInformation;
				Peers.Items.Clear();
				foreach ( PeerInformation peerInfo in e.Payload )
				{
					Peers.Items.Add( peerInfo );
				}
				//NOTE: Maintain Selection as peers are found
				if ( selectedPeer != null && Peers.Items.OfType<PeerInformation>().Any( pi => pi.DisplayName == selectedPeer.DisplayName ) )
				{
					Peers.SelectedItem = Peers.Items.OfType<PeerInformation>().First( pi => pi.DisplayName == selectedPeer.DisplayName );
				}
			}
		}

		private void ListenForPeers_Click( object sender, RoutedEventArgs e )
		{
			if ( NfcWrapper.Instance.State == PeerFindingState.Inactive )
			{
				NfcWrapper.Instance.StateChanged += NfcStateChanged;
				NfcWrapper.Instance.AdvertiseForPeers( "WinRT (PEER)", false, _alternateIdentities );
			}
		}

		private void Disconnect_Click( object sender, RoutedEventArgs e )
		{
			NfcWrapper.Instance.DisconnectAndClosePeerConnections();
		}

		private void ConnectToPeer_Click( object sender, RoutedEventArgs e )
		{
			NfcWrapper.Instance.ConnectToPeer( (PeerInformation) Peers.SelectedItem );
		}

		private void NfcStateChanged( object sender, EventArgs e )
		{
			if ( NfcWrapper.Instance.State == PeerFindingState.Connected )
			{
				WaitForMessages();
			}

			CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync( CoreDispatcherPriority.Normal, () =>
			{
				StateTextBlock.Text = NfcWrapper.Instance.State.ToString();
			} );
		}

		private async void WaitForMessages()
		{
			while ( NfcWrapper.Instance.State == PeerFindingState.Connected
				&& NfcWrapper.Instance.PeerSocket != null )
			{
				string message = await NfcWrapper.Instance.PeerSocket.ReceiveMessage();
				CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync( CoreDispatcherPriority.Normal, () =>
				{
					SocketMessageReceived.Text = string.IsNullOrEmpty( message ) ? string.Empty : message;
				} );
			}
		}


		#endregion

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Networking.Proximity;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LocationDemo_Win8.FeatureWrappers;
using ProximityDemo_Win8.FeatureWrappers;

namespace ProximityDemo_Win8
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
	
		#region Nfc

		private static readonly string MESSAGE_TYPE = "LocationPoC.Message";
		private void PublishMessage_Click( object sender, RoutedEventArgs e )
		{
			NfcWrapper.Instance.StartPublishing( MESSAGE_TYPE, MessageToSend.Text );
			MessageReceived.Text = NfcWrapper.Instance.MessagingStatus.ToString();
		}

		private void StopPublishingMessage_Click( object sender, RoutedEventArgs e )
		{
			NfcWrapper.Instance.StopPublishing();
			MessageReceived.Text = NfcWrapper.Instance.MessagingStatus.ToString();
		}

		private void SubscribeMessage_Click( object sender, RoutedEventArgs e )
		{
			NfcWrapper.Instance.SubscribeForMessage( MESSAGE_TYPE, MessageReceivedHandler );
			MessageReceived.Text = NfcWrapper.Instance.MessagingStatus.ToString();
		}

		private void MessageReceivedHandler( string value )
		{
			//TODO: NfcWrapper 5.0 - MessageReceivedHandler
			/*
			 * MessageCallback returns on a background thread so developers must dispatch any UI interaction to the UI thread via the dispatcher
			 * 
			 */
			CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal, () => MessageReceived.Text = value );
		}

		private void StopSubscribingMessage_Click( object sender, RoutedEventArgs e )
		{
			NfcWrapper.Instance.StopSubscribingForMessage();
			MessageReceived.Text = NfcWrapper.Instance.MessagingStatus.ToString();
		}

		#endregion

		#region PeerFinding

		private void SendSocketMessage_Click( object sender, RoutedEventArgs e )
		{
			if ( PeerFinderWrapper.Instance.PeerSocket != null )
			{
				PeerFinderWrapper.Instance.PeerSocket.SendMessage( SocketMessageToSend.Text );
			}
		}

		private Dictionary<string, string> _alternateIdentities = new Dictionary<string, string>
		{
			{
			    "Win_New", "0bb8f684-755d-4948-b4db-37352cb1f70e_4c5b9g29w27se!ProximityDemo_Universal.Windows"
			},
			{
				"WP_New", "{c6b6f625-0ac3-4dbb-adca-71dd128b7f5b}"
			},
			{
				"WP_Old", "{5b4de5d7-6f61-4397-a7b5-4c3dfddfe330}"
			}
		};

		private void AdvertiseForPeers_Click( object sender, RoutedEventArgs e )
		{
			if ( PeerFinderWrapper.Instance.State == PeerFindingState.Inactive )
			{
				PeerFinderWrapper.Instance.StateChanged += NfcStateChanged;
				PeerFinderWrapper.Instance.PeersFound += NfcPeersFound;
				PeerFinderWrapper.Instance.AdvertiseForPeers( "WinRT (HOST)", true, _alternateIdentities );
			}
		}

		private void NfcPeersFound( object sender, EventArgs<IReadOnlyList<PeerInformation>> e )
		{
			if ( e.Payload != null )
			{
				var selectedPeer = Peers.SelectedItem as PeerInformation;
				//Peers.Items.Clear();
				foreach ( PeerInformation peerInfo in e.Payload )
				{
					if ( Peers.Items.OfType<PeerInformation>().All( p => p.DisplayName != peerInfo.DisplayName ) )
					{
						Peers.Items.Add( peerInfo );
					}
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
			if ( PeerFinderWrapper.Instance.State == PeerFindingState.Inactive )
			{
				PeerFinderWrapper.Instance.StateChanged += NfcStateChanged;
				PeerFinderWrapper.Instance.AdvertiseForPeers( "WinRT (PEER)", false, _alternateIdentities );
			}
		}

		private void Disconnect_Click( object sender, RoutedEventArgs e )
		{
			PeerFinderWrapper.Instance.DisconnectAndClosePeerConnections();
		}

		private void ConnectToPeer_Click( object sender, RoutedEventArgs e )
		{
			PeerFinderWrapper.Instance.ConnectToPeer( (PeerInformation) Peers.SelectedItem );
		}

		private void NfcStateChanged( object sender, EventArgs e )
		{
			if ( PeerFinderWrapper.Instance.State == PeerFindingState.Connected )
			{
				WaitForMessages();
			}

			CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync( CoreDispatcherPriority.Normal, () =>
			{
				StateTextBlock.Text = PeerFinderWrapper.Instance.State.ToString();
			} );
		}

		private async void WaitForMessages()
		{
			while ( PeerFinderWrapper.Instance.State == PeerFindingState.Connected
				&& PeerFinderWrapper.Instance.PeerSocket != null )
			{
				string message = await PeerFinderWrapper.Instance.PeerSocket.ReceiveMessage();
				CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync( CoreDispatcherPriority.Normal, () =>
				{
					SocketMessageReceived.Text = string.IsNullOrEmpty( message ) ? string.Empty : message;
				} );
			}
		}


		#endregion
	}
}

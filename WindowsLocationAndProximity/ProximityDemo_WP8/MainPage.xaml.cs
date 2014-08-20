using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Windows.Networking.Proximity;
using LocationDemo_Win8.FeatureWrappers;
using Microsoft.Phone.Controls;
using ProximityDemo_Win8.FeatureWrappers;

namespace ProximityDemo_WP8
{
	public partial class MainPage : PhoneApplicationPage
	{
		public MainPage()
		{
			InitializeComponent();
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
			Deployment.Current.Dispatcher.BeginInvoke( () =>
				MessageReceived.Text = value );
		}

		private void StopSubscribingMessage_Click( object sender, RoutedEventArgs e )
		{
			NfcWrapper.Instance.StopSubscribingForMessage();
			MessageReceived.Text = NfcWrapper.Instance.MessagingStatus.ToString();
		}

		#endregion

		#region PeerFinding

		private readonly Dictionary<string, string> _alternateIdentities = new Dictionary<string, string>
	    {
		    {
			    "Win_New", "0bb8f684-755d-4948-b4db-37352cb1f70e_4c5b9g29w27se!ProximityDemo_Universal.Windows"
		    },
		    {
			    "Win_Old", "34fa5942-c870-4b68-ab9f-08091e0524e3_4c5b9g29w27se!ProximityDemo_Win8"
		    },
			{
				"WP_New", "{c6b6f625-0ac3-4dbb-adca-71dd128b7f5b}"
			}
	    };

		private void PeerFindingStateChanged( object sender, EventArgs e )
		{
			if ( PeerFinderWrapper.Instance.State == PeerFindingState.Connected )
			{
				WaitForMessages();
			}

			Deployment.Current.Dispatcher.BeginInvoke( () =>
			{
				StateTextBlock.Text = PeerFinderWrapper.Instance.State.ToString();
			} );
		}

		private void AdvertiseForPeers_Click( object sender, RoutedEventArgs e )
		{
			if ( PeerFinderWrapper.Instance.State == PeerFindingState.Inactive )
			{
				PeerFinderWrapper.Instance.StateChanged += PeerFindingStateChanged;
				PeerFinderWrapper.Instance.PeersFound += PeersFound;
				PeerFinderWrapper.Instance.AdvertiseForPeers( "WinPhone (HOST)", true, _alternateIdentities );
			}
		}

		private void PeersFound( object sender, EventArgs<IReadOnlyList<PeerInformation>> e )
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
				PeerFinderWrapper.Instance.StateChanged += PeerFindingStateChanged;
				PeerFinderWrapper.Instance.AdvertiseForPeers( "WinPhone (PEER)", false, _alternateIdentities );
			}
		}

		private async void ConnectToPeer_Click( object sender, RoutedEventArgs e )
		{
			await PeerFinderWrapper.Instance.ConnectToPeer( (PeerInformation) Peers.SelectedItem );
		}

		private void SendSocketMessage_Click( object sender, RoutedEventArgs e )
		{
			//TODO: PeerFinderWrapper 6.0 - SendSocketMessage_Click
			/*
			 * Use the StreamSocketManager to send a message to the connected Peer
			 * 
			 */

			if ( PeerFinderWrapper.Instance.PeerSocket != null )
			{
				PeerFinderWrapper.Instance.PeerSocket.SendMessage( SocketMessageToSend.Text );
			}
		}

		private async void WaitForMessages()
		{
			//TODO: PeerFinderWrapper 7.0 - WaitForMessages
			/*
			 * Use the StreamSocketManager to asynchronously wait for messages sent from the connected Peer
			 * Dispatch received messages to the UI thread
			 * 
			 */

			while ( PeerFinderWrapper.Instance.State == PeerFindingState.Connected
				&& PeerFinderWrapper.Instance.PeerSocket != null )
			{
				string message = await PeerFinderWrapper.Instance.PeerSocket.ReceiveMessage();
				Deployment.Current.Dispatcher.BeginInvoke( () =>
				{
					SocketMessageReceived.Text = string.IsNullOrEmpty( message ) ? "Empty message received." : message;
				} );
			}
		}

		private void Disconnect_Click( object sender, RoutedEventArgs e )
		{
			PeerFinderWrapper.Instance.StateChanged -= PeerFindingStateChanged;
			PeerFinderWrapper.Instance.PeersFound -= PeersFound;

			PeerFinderWrapper.Instance.DisconnectAndClosePeerConnections();
		}


		#endregion
	}
}
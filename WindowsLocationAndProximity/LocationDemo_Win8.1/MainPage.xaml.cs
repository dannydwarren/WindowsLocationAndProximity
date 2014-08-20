using System;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using LocationDemo_Win8.FeatureWrappers;

namespace LocationDemo_Win8._1
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

		#region Location

		/// <summary>
		/// Invoked when this page is about to be displayed in a Frame.
		/// </summary>
		/// <param name="e">Event data that describes how this page was reached.  The Parameter
		/// property is typically used to configure the page.</param>
		protected override void OnNavigatedTo( NavigationEventArgs e )
		{
			ActivateLocationTracking();
		}

		private async void ActivateLocationTracking()
		{
			var position = await LocationWrapper.Instance.GetSingleShotLocationAsync();
			LogLocation( position );

			//NOTE: Uncomment for Constant Tracking. - Use simulator to change location
			//LocationWrapper.Instance.LocationChanged += LocationChangedHandler;
			//LocationWrapper.Instance.ActivateContinousLocationTracking();
		}

		private void LocationChangedHandler( object sender, Geoposition e )
		{
			LogLocation( e );
		}

		private async void LogLocation( Geoposition position )
		{
			if ( position == null )
			{
				return;
			}

			LocationPositions.Items.Add(
				string.Format(
					"Latitude: {1}, Longitude: {2},{0}Country: {3}, Accuracy: {4},{0}Altitude: {5}, AltitudeAccuracy: {6},{0}Heading: {7}, Speed: {8}",
					Environment.NewLine,
					position.Coordinate.Latitude,
					position.Coordinate.Longitude,
					position.CivicAddress.Country,	//CivicAddress not supported in WindowsPhone
					position.Coordinate.Accuracy,
					position.Coordinate.Altitude,
					position.Coordinate.AltitudeAccuracy,
					position.Coordinate.Heading,
					position.Coordinate.Speed ) );

			//NOTE: Turn off Civic Address for Readablility
			string civicAddres = await LocationWrapper.Instance.ConvertGeocoordinateToCivicAddress( position );
			LocationPositions.Items.Add( new TextBlock(){Text = civicAddres, TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap} );


			if ( LocationPositions.Items.Count == 11 )
				LocationPositions.Items.RemoveAt( 0 );
		}

		#endregion

    }
}

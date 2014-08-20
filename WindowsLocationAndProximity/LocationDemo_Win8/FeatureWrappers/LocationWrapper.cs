using System;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Windows.UI.Popups;

#if WINDOWS_PHONE

using System.Windows;
using System.IO.IsolatedStorage;

#endif

namespace LocationDemo_Win8.FeatureWrappers
{
	public class LocationWrapper
	{
		#region Singleton
		private static LocationWrapper _instance;
		public static LocationWrapper Instance
		{
			get { return _instance ?? ( _instance = new LocationWrapper() ); }
		}
		private LocationWrapper()
		{

		}
		#endregion

#if WINDOWS_PHONE
		private static readonly string USER_ALLOWS_LOCATION_KEY = "USER_ALLOWS_LOCATION_KEY";

		public async Task<bool> CheckAndRequestPermissionToUseLocation()
		{
			if ( !CheckIfUserAllowsLocation() )
			{
				if ( !await RequestPermissionToUseLocation() )
				{
					return false;
				}
			}
			return true;
		}

		public async Task<bool> RequestPermissionToUseLocation()
		{
			/*
			 * A simple MessageBox prompt will suffice
			 * Save the result in IsolatedStorageSettings.ApplicationSettings
			 */


			MessageBoxResult result = MessageBox.Show( "This app accesses your phone's location. Is that OK?", "Location",
													  MessageBoxButton.OKCancel );

			bool userAllowsLocation = result == MessageBoxResult.OK;

			IsolatedStorageSettings.ApplicationSettings[USER_ALLOWS_LOCATION_KEY] = userAllowsLocation;
			IsolatedStorageSettings.ApplicationSettings.Save();


			return userAllowsLocation;
		}

		public bool CheckIfUserAllowsLocation()
		{
			/*
			 * Try to get the value out of IsolatedStorageSettings.ApplicationSettings
			 * By using TryGetValue the boolean stored will either be retrieved or will return false
			 *	NOTE: We'll need to prompt the user for permission if false
			 */

			bool userAllowsLocation = false;

			IsolatedStorageSettings.ApplicationSettings.TryGetValue( USER_ALLOWS_LOCATION_KEY, out userAllowsLocation );
			return userAllowsLocation;
		}
#endif

		private Geolocator _continuousGeolocator;

		public event EventHandler<Geoposition> LocationChanged = delegate { };
		private void OnLocationChanged( Geoposition geoposition )
		{
			LocationChanged( this, geoposition );
		}

		public bool IsTrackingLocation { get; private set; }

		public async Task<Geoposition> GetSingleShotLocationAsync()
		{

			//TODO: LocationWrapper 1.0 - GetSingleShotLocationAsync
			/*
			 * Create Geolocator
			 * Get location via geolocator.GetGeopositionAsync
			 * 
			 */


#if NETFX_CORE
			Geolocator geolocator = new Geolocator
			{
				DesiredAccuracy = PositionAccuracy.High
			};
#endif

#if WINDOWS_PHONE
			bool allowsLocation = await CheckAndRequestPermissionToUseLocation();
			if ( !allowsLocation )
				return null;

			Geolocator geolocator = new Geolocator
			{
				DesiredAccuracyInMeters = 50
			};
#endif

			Geoposition geoposition = null;
			string errorMessage = string.Empty;
			try
			{
				geoposition = await geolocator.GetGeopositionAsync( maximumAge: TimeSpan.FromSeconds( 1 ),
																	timeout: TimeSpan.FromSeconds( 10 ) );
			}
			catch ( Exception ex )
			{
				if ( (uint) ex.HResult == 0x80004004 )
				{
					errorMessage = "Location is disabled in device settings.";
				}
				if ( (uint) ex.HResult == 0x80070005 )
				{
					errorMessage = "Access denied by user.";
				}
			}

			if ( geoposition == null && !string.IsNullOrEmpty( errorMessage ) )
			{
#if NETFX_CORE
				MessageDialog dialog = new MessageDialog( errorMessage );
				await dialog.ShowAsync();
#endif
#if WINDOWS_PHONE
					MessageBox.Show( errorMessage );
#endif
			}

			//NOTE: Return value will be null if the user does not allow location or if location services are disabled.
			return geoposition;
		}

		public async void ActivateContinousLocationTracking()
		{
			//TODO: LocationWrapper 2.0 - ActivateContinousLocationTracking
			/*
			 * Create Geolocation with reporting parameters
			 * Subscribe to geolocator.PositionChanged
			 * Call geolocator.GetGeopositionAsync to activate location tracking
			 * 
			 */

			if ( IsTrackingLocation )
				return;

#if WINDOWS_PHONE
			bool allowsLocation = await CheckAndRequestPermissionToUseLocation();
			if ( !allowsLocation )
				return;

						_continuousGeolocator = new Geolocator()
										{
											DesiredAccuracyInMeters = 50,
											ReportInterval = 2000,
											MovementThreshold = 0,
										};

#endif


#if NETFX_CORE
			_continuousGeolocator = new Geolocator()
										{
											DesiredAccuracy = PositionAccuracy.High,
											ReportInterval = 2000,
											MovementThreshold = 0,
										};
#endif

			_continuousGeolocator.PositionChanged += PositionChangedHandler;
			_continuousGeolocator.GetGeopositionAsync();

			IsTrackingLocation = true;
		}


		public void DeactivateContinuousLocaitonTracking()
		{
			_continuousGeolocator.PositionChanged -= PositionChangedHandler;
			_continuousGeolocator = null;

			IsTrackingLocation = false;
		}

		private void PositionChangedHandler( Geolocator sender, PositionChangedEventArgs args )
		{
#if NETFX_CORE
			CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync( CoreDispatcherPriority.Normal,
																	() => OnLocationChanged( args.Position ) );
#endif

#if WINDOWS_PHONE
			Deployment.Current.Dispatcher.BeginInvoke(() => OnLocationChanged(args.Position));
#endif
		}

		public async Task<string> ConvertGeocoordinateToCivicAddress( Geoposition geoposition )
		{
			//TODO: LocationWrapper 3.0 - ConvertGeocoordinateToCivicAddress
			/*
			 * Use a service like BingMaps to convert a Geoposition into a civic address
			 * 
			 */

			//Temp Key: Create your own here: http://www.microsoft.com/maps/create-a-bing-maps-key.aspx
			string conversionUrlFormat =
                @"http://dev.virtualearth.net/REST/v1/Locations/{0},{1}?o=xml&key=AobdDrOVYLltY6q5iT9tsFDGiJm93KTUL6_hlp6QJLJNGoxa6O0s7C3HPs6RxW1D";

			string conversionUrl = 
                string.Format( conversionUrlFormat,
				geoposition.Coordinate.Latitude,
				geoposition.Coordinate.Longitude );

			string response = null;
#if NETFX_CORE
			HttpClient client = new HttpClient();
			response = await client.GetStringAsync( new Uri( conversionUrl ) );
#endif

#if WINDOWS_PHONE
		    throw new NotSupportedException();
            //WebClient client = new WebClient();
            //response = await client.DownloadStringAsync(new Uri(conversionUrl));
#endif
			return response;
		}
	}
}

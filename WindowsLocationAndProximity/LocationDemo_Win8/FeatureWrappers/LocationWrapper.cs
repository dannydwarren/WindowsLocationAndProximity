using System;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
#if NETFX_CORE

#endif

#if WINDOWS_PHONE

using System.Windows;
using System.IO.IsolatedStorage;

#endif

namespace LocationDemo_Win8.FeatureWrappers
{
	public class LocationWrapper
	{
#if NETFX_CORE
		private static readonly string LOCATION_CONTAINER_KEY = "LOCATION_CONTAINER_KEY";
#endif

		private static readonly string USER_ALLOWS_LOCATION_KEY = "USER_ALLOWS_LOCATION_KEY";
		private Geolocator _continuousGeolocator;

		private LocationWrapper()
		{

		}

		public event EventHandler<Geoposition> LocationChanged = delegate { };
		private void OnLocationChanged( Geoposition geoposition )
		{
			LocationChanged( this, geoposition );
		}

		private static LocationWrapper _instance;
		public static LocationWrapper Instance
		{
			get
			{
				if ( _instance == null )
					_instance = new LocationWrapper();
				return _instance;
			}
		}

		public bool IsTrackingLocation { get; private set; }

		public async Task<bool> CheckAndRequestPermissionToUseLocation()
		{
#if NETFX_CORE
			//NOTE: In Windows 8 the OS handles whether or not an app has permission to use Location.
			//	Apps should not crash but handle the ACCESS_DENIED exception when accessing Geolocator APIs.
			return true;
#endif
#if WINDOWS_PHONE
			if ( !CheckIfUserAllowsLocation() )
			{
				if ( !await RequestPermissionToUseLocation() )
				{
					return false;
				}
			}
			return true;
#endif
		}

		public async Task<bool> RequestPermissionToUseLocation()
		{
			/*
			 * A simple MessageBox prompt will suffice
			 * Save the result in IsolatedStorageSettings.ApplicationSettings
			 */

#if NETFX_CORE
			var dialog = new MessageDialog( "This app accesses your devices's location. Is that OK?", "Location" );

			string yesLabel = "Yes";
			string noLabel = "No";

			dialog.Commands.Add( new UICommand( yesLabel ) );
			dialog.Commands.Add( new UICommand( noLabel ) );

			IUICommand selectedCommand = await dialog.ShowAsync();

			bool userAllowsLocation = selectedCommand.Label == yesLabel;

			ApplicationDataContainer locationContainer;
			ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

			if ( !localSettings.Containers.TryGetValue( LOCATION_CONTAINER_KEY, out locationContainer ) )
			{
				locationContainer = localSettings.CreateContainer( LOCATION_CONTAINER_KEY, ApplicationDataCreateDisposition.Always );
			}

			locationContainer.Values.Add( USER_ALLOWS_LOCATION_KEY, userAllowsLocation );

#endif
#if WINDOWS_PHONE
			MessageBoxResult result = MessageBox.Show( "This app accesses your phone's location. Is that OK?", "Location",
													  MessageBoxButton.OKCancel );

			bool userAllowsLocation = result == MessageBoxResult.OK;

			IsolatedStorageSettings.ApplicationSettings[USER_ALLOWS_LOCATION_KEY] = userAllowsLocation;
			IsolatedStorageSettings.ApplicationSettings.Save();
#endif


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

#if NETFX_CORE
			ApplicationDataContainer locationContainer;
			ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
			if ( localSettings.Containers.TryGetValue( LOCATION_CONTAINER_KEY, out locationContainer ) )
			{
				object userAllowsLocationObj;
				if ( locationContainer.Values.TryGetValue( USER_ALLOWS_LOCATION_KEY, out userAllowsLocationObj ) )
				{
					userAllowsLocation = (bool)userAllowsLocationObj;
				}
			}
#endif
#if WINDOWS_PHONE
			IsolatedStorageSettings.ApplicationSettings.TryGetValue( USER_ALLOWS_LOCATION_KEY, out userAllowsLocation );
#endif
			return userAllowsLocation;
		}

		/// <summary>
		/// NOTE: Return value will be null if the user does not allow location or if location services are disabled.
		/// </summary>
		/// <returns>Geoposition</returns>
		public async Task<Geoposition> GetSingleShotLocationAsync()
		{
			bool allowsLocation = await CheckAndRequestPermissionToUseLocation();
			if ( !allowsLocation )
				return null;

#if NETFX_CORE
			Geolocator geolocator = new Geolocator
			{
				DesiredAccuracy = PositionAccuracy.High
			};
#endif

#if WINDOWS_PHONE
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
				if ( (uint)ex.HResult == 0x80004004 )
				{
					errorMessage = "Location is disabled in device settings.";
				}
				if ( (uint)ex.HResult == 0x80070005 )
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

			return geoposition;
		}

		public async void ActivateContinousLocationTracking()
		{
			bool allowsLocation = await CheckAndRequestPermissionToUseLocation();
			if ( !allowsLocation )
				return;

			if ( IsTrackingLocation )
				return;

#if NETFX_CORE
			_continuousGeolocator = new Geolocator()
										{
											DesiredAccuracy = PositionAccuracy.High,
											ReportInterval = 2000,
											MovementThreshold = 0,
										};
#endif

#if WINDOWS_PHONE
			_continuousGeolocator = new Geolocator()
										{
											DesiredAccuracyInMeters = 50,
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
            string conversionUrlFormat =
                @"http://dev.virtualearth.net/REST/v1/Locations/{0},{1}?o=xml&key=Ajg4tVWZHFIIQlUe09wNu5-D4cthY0p0jed-2tq_rfbUAjAA08Jg_EzcbQDnxTtM";

            string conversionUrl = 
                string.Format(conversionUrlFormat,
                geoposition.Coordinate.Latitude,
                geoposition.Coordinate.Longitude);
            
            string response = null;
#if NETFX_CORE
            HttpClient client = new HttpClient();
            response = await client.GetStringAsync(new Uri(conversionUrl));
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

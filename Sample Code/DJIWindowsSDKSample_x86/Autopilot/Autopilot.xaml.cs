using DJI.WindowsSDK;
using DJI.WindowsSDK.Mission.Waypoint;
using DJIUWPSample.Commands;
using DJIWindowsSDKSample.DJISDKInitializing;
using DJIUWPSample.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DJIVideoParser;
using System.Windows.Input;
using DJIWindowsSDKSample.ViewModels;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace DJIWindowsSDKSample.Autopilot
{
    public sealed partial class Autopilot : Page
    {
        private DJIVideoParser.Parser videoParser;
        public List<Waypoint> Waypoints = new List<Waypoint>();
        public WaypointMission mission = new WaypointMission();
        int i = 1;
        

        public Autopilot()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            InitializeVideoFeedModule();
            await DJI.WindowsSDK.DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).SetCameraWorkModeAsync(new CameraWorkModeMsg { value = CameraWorkMode.SHOOT_PHOTO });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            UninitializeVideoFeedModule();
        }

        public WaypointMission GetWaypointMission()
        {
            return mission;
        }

        private async void InitializeVideoFeedModule()
        {
            //Must in UI thread
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                //Raw data and decoded data listener
                if (videoParser == null)
                {
                    videoParser = new DJIVideoParser.Parser();
                    videoParser.Initialize(delegate (byte[] data)
                    {
                        //Note: This function must be called because we need DJI Windows SDK to help us to parse frame data.
                        return DJISDKManager.Instance.VideoFeeder.ParseAssitantDecodingInfo(0, data);
                    });
                    //Set the swapChainPanel to display and set the decoded data callback.
                    videoParser.SetSurfaceAndVideoCallback(0, 0, swapChainPanel, ReceiveDecodedData);
                    DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated += OnVideoPush;
                }
                //get the camera type and observe the CameraTypeChanged event.
                DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).CameraTypeChanged += OnCameraTypeChanged;
                var type = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).GetCameraTypeAsync();
                OnCameraTypeChanged(this, type.value);
            });
        }


        private void UninitializeVideoFeedModule()
        {
            if (DJISDKManager.Instance.SDKRegistrationResultCode == SDKError.NO_ERROR)
            {
                videoParser.SetSurfaceAndVideoCallback(0, 0, null, null);
                DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated -= OnVideoPush;
            }
        }

        //raw data
        void OnVideoPush(VideoFeed sender, byte[] bytes)
        {
            videoParser.PushVideoData(0, 0, bytes, bytes.Length);
        }

        //Decode data. Do nothing here. This function would return a bytes array with image data in RGBA format.
        async void ReceiveDecodedData(byte[] data, int width, int height)
        {
        }

        //We need to set the camera type of the aircraft to the DJIVideoParser. After setting camera type, DJIVideoParser would correct the distortion of the video automatically.
        private void OnCameraTypeChanged(object sender, CameraTypeMsg? value)
        {
            if (value != null)
            {
                switch (value.Value.value)
                {
                    case CameraType.MAVIC_2_ZOOM:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Mavic2Zoom);
                        break;
                    case CameraType.MAVIC_2_PRO:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Mavic2Pro);
                        break;
                    default:
                        this.videoParser.SetCameraSensor(AircraftCameraType.Others);
                        break;
                }

            }
        }
        //Callback of SDKRegistrationEvent
        private async void Instance_SDKRegistrationEvent(SDKRegistrationState state, SDKError resultCode)
        {
            if (resultCode == SDKError.NO_ERROR)
            {
                System.Diagnostics.Debug.WriteLine("Register app successfully.");

                //Must in UI Thread
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    //Raw data and decoded data listener
                    if (videoParser == null)
                    {
                        videoParser = new DJIVideoParser.Parser();
                        videoParser.Initialize(delegate (byte[] data)
                        {
                            //Note: This function must be called because we need DJI Windows SDK to help us to parse frame data.
                            return DJISDKManager.Instance.VideoFeeder.ParseAssitantDecodingInfo(0, data);
                        });
                        //Set the swapChainPanel to display and set the decoded data callback.
                        videoParser.SetSurfaceAndVideoCallback(0, 0, swapChainPanel, ReceiveDecodedData);
                        DJISDKManager.Instance.VideoFeeder.GetPrimaryVideoFeed(0).VideoDataUpdated += OnVideoPush;
                    }
                    //get the camera type and observe the CameraTypeChanged event.
                    DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).CameraTypeChanged += OnCameraTypeChanged;
                    var type = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).GetCameraTypeAsync();
                    OnCameraTypeChanged(this, type.value);
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("SDK register failed, the error is: ");
                System.Diagnostics.Debug.WriteLine(resultCode.ToString());
            }
        }

        private void SetCameraWorkModeToShootPhoto_Click(object sender, RoutedEventArgs e)
        {
            SetCameraWorkMode(CameraWorkMode.SHOOT_PHOTO);
        }

        private void SetCameraModeToRecord_Click(object sender, RoutedEventArgs e)
        {
            SetCameraWorkMode(CameraWorkMode.RECORD_VIDEO);
        }

        private async void SetCameraWorkMode(CameraWorkMode mode)
        {
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                CameraWorkModeMsg workMode = new CameraWorkModeMsg
                {
                    value = mode,
                };
                var retCode = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).SetCameraWorkModeAsync(workMode);
                if (retCode != SDKError.NO_ERROR)
                {
                    Output.Text = "Set camera work mode to " + mode.ToString() + "failed, result code is " + retCode.ToString();
                }
            }
            else
            {
                Output.Text = "The application hasn't been registered successfully yet.";
            }
        }

        private async void StartRecordVideo_Click(object sender, RoutedEventArgs e)
        {
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                var retCode = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).StartRecordAsync();
                if (retCode != SDKError.NO_ERROR)
                {
                    Output.Text = "Failed to record video, result code is " + retCode.ToString();
                }
                else
                {
                    Output.Text = "Start Recording video successfully";
                    var location = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetAircraftLocationAsync();
                    var setLocation = DJIWindowsSDKSample.ViewModels.AutopilotViewModel.Instance.InitDumpWaypoint(location.value.Value.latitude, location.value.Value.longitude);
                    SetWaypoint(location.value.Value.latitude, location.value.Value.longitude);
                }
            }
            else
            {
                Output.Text = "The application hasn't been registered successfully yet.";
            }
        }

        private async void StopRecordVideo_Click(object sender, RoutedEventArgs e)
        {
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                var retCode = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).StopRecordAsync();
                if (retCode != SDKError.NO_ERROR)
                {
                    Output.Text = "Failed to stop record video, result code is " + retCode.ToString();
                }
                else
                {
                    Output.Text = "Stop record video successfully";
                    var location = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetAircraftLocationAsync();
                    var setLocation = DJIWindowsSDKSample.ViewModels.AutopilotViewModel.Instance.InitDumpWaypoint(location.value.Value.latitude, location.value.Value.longitude);
                    SetWaypoint(location.value.Value.latitude, location.value.Value.longitude);
                    WaypointPattern(Waypoints);
                }
            }
            else
            {
                Output.Text = "The application hasn't been registered successfully yet.";
            }
        }

        private async void Waypoint_Click(object sender, RoutedEventArgs e)
        {
            var location = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetAircraftLocationAsync();
            SetWaypoint(location.value.Value.latitude, location.value.Value.longitude);
        }

        private Waypoint SetWaypoint(double latitude, double longitude)
        {
            Waypoint waypoint = new Waypoint()
            {
                location = new LocationCoordinate2D() { latitude = latitude, longitude = longitude },
                altitude = 0,
                gimbalPitch = -30,
                turnMode = WaypointTurnMode.CLOCKWISE,
                heading = 0,
                actionRepeatTimes = 0,
                actionTimeoutInSeconds = 60,
                cornerRadiusInMeters = 0.2,
                speed = 0,
                shootPhotoTimeInterval = -1,
                shootPhotoDistanceInterval = -1,
                waypointActions = new List<WaypointAction>()
            };

            Waypoints.Add(waypoint);
            Output.Text = "The location is: " + waypoint.location.latitude + ", " + waypoint.location.longitude + ", Waypoint count: " + i;
            i++;
            return waypoint;
        }

        private void WaypointPattern(List<Waypoint> Waypoints)
        {
            mission.waypointCount = (i - 1);
            mission.maxFlightSpeed = 15;
            mission.autoFlightSpeed = 10;
            mission.finishedAction = WaypointMissionFinishedAction.GO_FIRST_WAYPOINT;
            mission.headingMode = WaypointMissionHeadingMode.AUTO;
            mission.flightPathMode = WaypointMissionFlightPathMode.CURVED;
            mission.gotoFirstWaypointMode = WaypointMissionGotoFirstWaypointMode.SAFELY;
            mission.exitMissionOnRCSignalLostEnabled = false;
            mission.pointOfInterest = new LocationCoordinate2D()
            {
                latitude = 0,
                longitude = 0
            };
            mission.gimbalPitchRotationEnabled = true;
            mission.repeatTimes = 0;
            mission.missionID = 5;
            mission.waypoints = Waypoints;
        }

        public async void StartAutoTakeOff(object sender, RoutedEventArgs e)
        {
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                SDKError res = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).StartTakeoffAsync();
                var messageDialog = new MessageDialog(String.Format("Start send takeoff command: {0}", res.ToString()));
                await messageDialog.ShowAsync();
            }
            else
            {
                var messageDialog = new MessageDialog("Error with equipment.");
                await messageDialog.ShowAsync();
            }
        }

        public async void LoadMissionAuto(object sender, RoutedEventArgs e)
        {
            BoolMsg sant = new BoolMsg
            {
                value = true
            };
            SDKError ground = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).SetGroundStationModeEnabledAsync(sant);
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                var state = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).GetGroundStationModeEnabledAsync();
                var messageDialog1 = new MessageDialog(String.Format("SDK load mission: {0}", state));
                await messageDialog1.ShowAsync();
                SDKError err = DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).LoadMission(mission);
                var messageDialog = new MessageDialog(String.Format("SDK load mission: {0}", err.ToString()));
                await messageDialog.ShowAsync();
            }
            else
            {
                var messageDialog = new MessageDialog("No mission available.");
                await messageDialog.ShowAsync();
            }
        }

        public async void UploadLoadMissionAuto(object sender, RoutedEventArgs e)
        {
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                var state = DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).GetCurrentState();
                var messageDialog1 = new MessageDialog(String.Format("Current state: ", state.ToString()));
                await messageDialog1.ShowAsync();
                var err = DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).GetLoadedMission();
                var messageDialog = new MessageDialog(String.Format("SDK load mission: {0}", err.ToString()));
                await messageDialog.ShowAsync();
            }
            else
            {
                var messageDialog = new MessageDialog("No mission available.");
                await messageDialog.ShowAsync();
            }
        }

        public async void StartMissionAuto(object sender, RoutedEventArgs e)
        {
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                var execute = DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).GetCurrentState();
                var messageDialog1 = new MessageDialog(String.Format("Current state: ", execute.ToString()));
                await messageDialog1.ShowAsync();
                SDKError err = await DJISDKManager.Instance.WaypointMissionManager.GetWaypointMissionHandler(0).StartMission();
                //var rec = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).StartRecordAsync();
                var messageDialog = new MessageDialog(String.Format("Start mission: {0}", err.ToString()));
                await messageDialog.ShowAsync();
            }
            else
            {
                var messageDialog = new MessageDialog("Failed to start mission");
                await messageDialog.ShowAsync();
            }
        }

        public async void AutoLanding(object sender, RoutedEventArgs e)
        {
            if (DJISDKManager.Instance.ComponentManager != null)
            {
                SDKError err = await DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).StartAutoLandingAsync();
                var stopRec = await DJISDKManager.Instance.ComponentManager.GetCameraHandler(0, 0).StopRecordAsync();
                var messageDialog = new MessageDialog(String.Format("SDK autolanding: {0}", err.ToString()));
                await messageDialog.ShowAsync();
            }
            else
            {
                var messageDialog = new MessageDialog("Failed to auto land.");
                await messageDialog.ShowAsync();
            }
        }
    }
}

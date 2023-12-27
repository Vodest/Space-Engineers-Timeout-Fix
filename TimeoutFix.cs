using System.Diagnostics;
using System.Reflection;
using VRage.Plugins;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.GameServices;
using VRage.Utils;

namespace TimeoutFixPlugin
{
    public class TimeoutFix : IPlugin
    {
        public void Dispose()
        {
            
        }

        private static MethodInfo _target = typeof(MyJoinGameHelper).GetMethod("DownloadWorld", BindingFlags.NonPublic | BindingFlags.Static);
        private static MethodInfo _patch = typeof(TimeoutFix).GetMethod(nameof(DownloadWorld), BindingFlags.NonPublic | BindingFlags.Static);

        private static FieldInfo _downloadField = typeof(MyWorkshop).GetField("m_downloadScreen", BindingFlags.NonPublic | BindingFlags.Static);

        private static MyGuiScreenMessageBox DownloadScreen => (MyGuiScreenMessageBox)_downloadField.GetValue(null);

        private static bool DownloadVisible => DownloadScreen?.Visible ?? false;

        public void Init(object gameInstance)
        {
            // source for MethodUtil not given on the reddit post
            //MethodUtil.ReplaceMethod(_patch, _target);
        }

        public void Update()
        {
        }

        private static void DownloadWorld(MyGuiScreenProgress progress, MyMultiplayerBase multiplayer)
        {
            if (progress.Text != null)
            {
                progress.Text.Clear();
                progress.Text.Append(MyTexts.Get(MyCommonTexts.MultiplayerStateConnectingToServer));
            }

            MyLog.Default.WriteLine("World requested");

            const float worldRequestTimeout = 40; // in seconds
            Stopwatch worldRequestTime = Stopwatch.StartNew();

            ulong serverId = multiplayer.GetOwner();
            bool connected = false;
            progress.Tick += () =>
            {
                MyP2PSessionState state = default(MyP2PSessionState);
                MyGameService.Peer2Peer.GetSessionState(multiplayer.ServerId, ref state);

                if (!connected && state.ConnectionActive)
                {
                    MyLog.Default.WriteLine("World requested - connection alive");
                    connected = true;
                    if (progress.Text != null)
                    {
                        progress.Text.Clear();
                        progress.Text.AppendLine("Using Rexxar's fixed join code! :D");
                        progress.Text.Append(MyTexts.Get(MyCommonTexts.MultiplayerStateWaitingForServer));
                    }
                }

                bool isTop = MyScreenManager.IsScreenOnTop(progress);
                //progress.Text.Clear();
                //progress.Text.AppendLine($"Elapsed: {worldRequestTime.ElapsedMilliseconds}");
                //progress.Text.AppendLine($"Download Status: {DownloadScreen?.Visible.ToString() ?? "Not open"} : {DownloadVisible}");
                //progress.Text.AppendLine("Connecting: " + state.Connecting);
                //progress.Text.AppendLine("ConnectionActive: " + state.ConnectionActive);
                //progress.Text.AppendLine("Relayed: " + state.UsingRelay);
                //progress.Text.AppendLine("Bytes queued: " + state.BytesQueuedForSend);
                //progress.Text.AppendLine("Packets queued: " + state.PacketsQueuedForSend);
                //progress.Text.AppendLine("Last session error: " + state.LastSessionError);
                //progress.Text.AppendLine("Original server: " + serverId);
                ////progress.Text.AppendLine("Current server: " + multiplayer.Lobby.GetOwner());
                //progress.Text.AppendLine("Game version: " + multiplayer.AppVersion);
                //progress.Text.Append($"IsTop: {isTop}");

                if (serverId != multiplayer.GetOwner())
                {
                    MyLog.Default.WriteLine("World requested - failed, server changed");
                    progress.Cancel();
                    MyGuiSandbox.Show(MyCommonTexts.MultiplayerErrorServerHasLeft);
                    multiplayer.Dispose();
                }

                bool visible = DownloadVisible;

                if (!visible && isTop && !worldRequestTime.IsRunning)
                {
                    worldRequestTime.Start();
                }
                else if (visible || !isTop && worldRequestTime.IsRunning)
                {
                    worldRequestTime.Stop();
                }

                if (visible && progress.Visible)
                    progress.HideScreen();
                else if (!visible && !progress.Visible)
                    progress.UnhideScreen();


                if (worldRequestTime.IsRunning && worldRequestTime.Elapsed.TotalSeconds > worldRequestTimeout)
                {
                    MyLog.Default.WriteLine("World requested - failed, server changed");
                    progress.Cancel();
                    MyGuiSandbox.Show(MyCommonTexts.MultiplaterJoin_ServerIsNotResponding);
                    multiplayer.Dispose();
                }
            };

            multiplayer.DownloadWorld(0);
        }
    }
}

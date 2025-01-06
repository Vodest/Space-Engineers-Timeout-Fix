using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using ClientPlugin;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Networking;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Game;
using VRage.GameServices;
using VRage.Plugins;
using VRage.Utils;

namespace TimeoutFixPlugin
{
    public class TimeoutFix : IPlugin, IDisposable
    {
        private static MyGuiScreenProgressBase m_progress
        {
            get
            {
                return (MyGuiScreenProgressBase)((MyGuiScreenProgressBase)TimeoutFix._progressField.GetValue(null));
            }
            set
            {
                TimeoutFix._progressField.SetValue(null, value);
            }
        }

        public void Dispose()
        {
        }

        public void Init(object gameInstance)
        {
            MethodUtil.ReplaceMethod(TimeoutFix._patch, TimeoutFix._target);
            MethodUtil.ReplaceMethod(TimeoutFix._receivePatch, TimeoutFix._receiveTarget);
        }

        public void Update()
        {
        }

        private static void DownloadWorld(MyGuiScreenProgress progress, MyMultiplayerBase multiplayer)
        {
            TimeoutFix._worldReceived = false;
            bool flag = progress.Text != null;
            if (flag)
            {
                progress.Text.Clear();
                progress.Text.Append(MyTexts.Get(MyCommonTexts.MultiplayerStateConnectingToServer));
            }
            MyLog.Default.WriteLine($"World requested: Timeout fix {VERSION}");
            Stopwatch worldRequestTime = Stopwatch.StartNew();
            ulong serverId = multiplayer.GetOwner();
            bool connected = false;
            progress.Tick += delegate
            {
                MyP2PSessionState myP2PSessionState = default(MyP2PSessionState);
                MyGameService.Peer2Peer.GetSessionState(multiplayer.ServerId, ref myP2PSessionState);
                bool flag2 = !connected && myP2PSessionState.ConnectionActive;
                if (flag2)
                {
                    MyLog.Default.WriteLine("World requested - connection alive");
                    connected = true;
                    bool flag3 = progress.Text != null;
                    if (flag3)
                    {
                        progress.Text.AppendLine($" - Using Rexxar's fixed join code {VERSION}");
                    }
                }
                bool flag4 = connected && !myP2PSessionState.ConnectionActive;
                if (flag4)
                {
                    MyLog.Default.WriteLine("World request - connection dropped");
                    progress.Cancel();
                    MyGuiSandbox.Show(MyCommonTexts.MultiplaterJoin_ServerIsNotResponding, default(MyStringId), MyMessageBoxStyleEnum.Error);
                    MySessionLoader.UnloadAndExitToMenu();
                }
                bool flag5 = MyScreenManager.IsScreenOnTop((MyGuiScreenBase)progress);
                bool flag6 = serverId != multiplayer.GetOwner();
                if (flag6)
                {
                    MyLog.Default.WriteLine(string.Format("World requested - failed, server changed: Expected {0} got {1}", serverId, multiplayer.GetOwner()));
                    progress.Cancel();
                    MyGuiSandbox.Show(MyCommonTexts.MultiplayerErrorServerHasLeft, default(MyStringId), MyMessageBoxStyleEnum.Error);
                    multiplayer.Dispose();
                }
                bool flag7 = MyScreenManager.IsScreenOfTypeOpen(typeof(MyGuiScreenDownloadMods));
                bool flag8 = !flag7 && flag5 && !worldRequestTime.IsRunning;
                if (flag8)
                {
                    MyLog.Default.WriteLine("World request - starting timer");
                    worldRequestTime.Start();
                }
                else
                {
                    bool flag9 = flag7 || (!flag5 && worldRequestTime.IsRunning);
                    if (flag9)
                    {
                        MyLog.Default.WriteLine("World request - stopping timer");
                        worldRequestTime.Stop();
                    }
                }
                bool flag10 = flag7 && progress.Visible;
                if (flag10)
                {
                    progress.HideScreen();
                }
                else
                {
                    bool flag11 = !flag7 && !progress.Visible;
                    if (flag11)
                    {
                        progress.UnhideScreen();
                    }
                }
                bool flag12 = !TimeoutFix._worldReceived && worldRequestTime.IsRunning && (float)(worldRequestTime.ElapsedTicks / Stopwatch.Frequency) > 300f;
                if (flag12)
                {
                    MyLog.Default.WriteLine("World requested - failed, timeout reached");
                    MyLog.Default.WriteLine(string.Format("Elapsed : {0:N2}", worldRequestTime.ElapsedTicks / Stopwatch.Frequency));
                    progress.Cancel();
                    MyGuiSandbox.Show(MyCommonTexts.MultiplaterJoin_ServerIsNotResponding, default(MyStringId), MyMessageBoxStyleEnum.Error);
                    MySessionLoader.UnloadAndExitToMenu();
                }
            };
            multiplayer.DownloadWorld(MyFinalBuildConstants.APP_VERSION.Version);
        }

        private static void CheckDx11AndJoin(MyObjectBuilder_World world, MyMultiplayerBase multiplayer)
        {
            bool scenario = multiplayer.Scenario;
            if (scenario)
            {
                MySessionLoader.LoadMultiplayerScenarioWorld(world, multiplayer);
            }
            else
            {
                MySessionLoader.LoadMultiplayerSession(world, multiplayer);
            }
        }

        private static void WorldReceived(MyObjectBuilder_World world, MyMultiplayerBase multiplayer)
        {
            MyLog.Default.WriteLine("World requested - Attempting World Download");
            bool flag = world == null;
            if (flag)
            {
                MyLog.Default.WriteLine("World requested - failed, version mismatch");
                TimeoutFix.m_progress.Cancel();
                TimeoutFix.m_progress = null;
                MyGuiSandbox.Show(MyCommonTexts.MultiplayerErrorAppVersionMismatch, default(MyStringId), MyMessageBoxStyleEnum.Error);
                multiplayer.Dispose();
            }
            else
            {
                TimeoutFix._worldReceived = true;
                MyLog.Default.WriteLine("World requested - world data received");
                bool flag2;
                if (world == null)
                {
                    flag2 = null != null;
                }
                else
                {
                    MyObjectBuilder_Checkpoint checkpoint = world.Checkpoint;
                    flag2 = ((checkpoint != null) ? checkpoint.Settings : null) != null;
                }
                bool flag3 = flag2 && !MySandboxGame.Config.ExperimentalMode;
                if (flag3)
                {
                    MySessionLoader.UnloadAndExitToMenu();
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat(MyCommonTexts.DialogTextJoinWorldFailed, MyTexts.GetString(MyCommonTexts.MultiplayerErrorExperimental));
                    MyGuiSandbox.AddScreen((MyGuiScreenBase)MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.OK, stringBuilder, MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), null, null, null, null, null, 0, MyGuiScreenMessageBox.ResultEnum.YES, true, null, true, null, true, false, null));
                }
                else
                {
                    TimeoutFix.m_progress = null;
                    TimeoutFix.CheckDx11AndJoin(world, multiplayer);
                }
            }
        }

        private static MethodInfo _target = typeof(MyJoinGameHelper).GetMethod("DownloadWorld", BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo _patch = typeof(TimeoutFix).GetMethod("DownloadWorld", BindingFlags.Static | BindingFlags.NonPublic);

        private static MethodInfo _receiveTarget = typeof(MyJoinGameHelper).GetMethod("WorldReceived", BindingFlags.Static | BindingFlags.Public);

        private static MethodInfo _receivePatch = typeof(TimeoutFix).GetMethod("WorldReceived", BindingFlags.Static | BindingFlags.NonPublic);

        private static bool _worldReceived;

        private static readonly FieldInfo _progressField = typeof(MyJoinGameHelper).GetField("m_progress", BindingFlags.Static | BindingFlags.NonPublic);

        private const string VERSION = "v2.3";
    }
}

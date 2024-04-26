using System;
using System.Linq;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.I18N;
using ClientGUI;
using ClientUpdater;
using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Multiplayer;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic
{
    public class LoadingScreen : XNAWindow
    {
#if NETCOREAPP
        private readonly Random _random = Random.Shared;
#else
        private readonly Random _random = new();
#endif

        public LoadingScreen(
            CnCNetManager cncnetManager,
            WindowManager windowManager,
            IServiceProvider serviceProvider,
            MapLoader mapLoader
        ) : base(windowManager)
        {
            this.cncnetManager = cncnetManager;
            this.serviceProvider = serviceProvider;
            this.mapLoader = mapLoader;
        }

        private static readonly object locker = new object();

        private MapLoader mapLoader;

        private PrivateMessagingPanel privateMessagingPanel;

        private bool visibleSpriteCursor;

        private Task updaterInitTask;
        private Task mapLoadTask;
        private readonly CnCNetManager cncnetManager;
        private readonly IServiceProvider serviceProvider;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 800, 600);
            Name = "LoadingScreen";

            BackgroundTexture = AssetLoader.LoadTexture("loadingscreen.png");

            base.Initialize();

            FullScreen();
            RandomBackground();
            RandomTips();

            CenterOnParent();

            bool initUpdater = !ClientConfiguration.Instance.ModMode;

            if (initUpdater)
            {
                updaterInitTask = new Task(InitUpdater);
                updaterInitTask.Start();
            }

            mapLoadTask = mapLoader.LoadMapsAsync();

            if (Cursor.Visible)
            {
                Cursor.Visible = false;
                visibleSpriteCursor = true;
            }
        }

        private void InitUpdater()
        {
            Updater.OnLocalFileVersionsChecked += LogGameClientVersion;
            Updater.CheckLocalFileVersions();
        }

        private void LogGameClientVersion()
        {
            Logger.Log($"Game Client Version: {ClientConfiguration.Instance.LocalGame} {Updater.GameVersion}");
            Updater.OnLocalFileVersionsChecked -= LogGameClientVersion;
        }

        private void Finish()
        {
            ProgramConstants.GAME_VERSION = ClientConfiguration.Instance.ModMode ? 
                "N/A" : Updater.GameVersion;

            MainMenu mainMenu = serviceProvider.GetService<MainMenu>();

            WindowManager.AddAndInitializeControl(mainMenu);
            mainMenu.PostInit();

            if (UserINISettings.Instance.AutomaticCnCNetLogin &&
                NameValidator.IsNameValid(ProgramConstants.PLAYERNAME) == null)
            {
                cncnetManager.Connect();
            }

            if (!UserINISettings.Instance.PrivacyPolicyAccepted)
            {
                WindowManager.AddAndInitializeControl(new PrivacyNotification(WindowManager));
            }

            WindowManager.RemoveControl(this);

            Cursor.Visible = visibleSpriteCursor;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (updaterInitTask == null || updaterInitTask.Status == TaskStatus.RanToCompletion)
            {
                if (mapLoadTask.Status == TaskStatus.RanToCompletion)
                    Finish();
            }
        }

        private void FullScreen()
        {
            if (ThemeIni is null)
                throw new ArgumentNullException("Must called after base.Initialize.");

            var isFullScreen = ThemeIni.GetBooleanValue(Name, "$IsFullScreen", false);

            if (!isFullScreen)
                return;

            (Width, Height) = (WindowManager.RenderResolutionX, WindowManager.RenderResolutionY);
        }

        private void RandomBackground()
        {
            if (ThemeIni is null)
                throw new ArgumentNullException("Must called after base.Initialize.");

            var backgrounds = ThemeIni
                .GetSection(Name)
                .Keys
                .Where(i => i.Key.StartsWith("$BG", StringComparison.OrdinalIgnoreCase))
                .Select(i => i.Value)
                .ToArray();

            if (backgrounds is { Length: 0 })
                return;

            var index = _random.Next(backgrounds.Length);

            BackgroundTexture = AssetLoader.LoadTexture(SafePath.CombineFilePath(backgrounds[index].Split('/', '\\')));
            _ = Task.Delay(2000).ContinueWith(_ => RandomBackground());
        }

        private void RandomTips()
        {
            if (ThemeIni is null)
                throw new ArgumentNullException("Must called after base.Initialize.");

            if (Children.FirstOrDefault(i => i.Name == "Tips") is not XNALabel label)
                return;

            var tips = Translation
                .Instance
                .DumpIni()
                .GetSection("Values")
                .Keys
                .Where(i => i.Key.StartsWith("INI:LoadingScreen:Tips:"))
                .Select(i => i.Value)
                .ToArray();

            if (tips is { Length: 0 })
                return;

            var index = _random.Next(tips.Length);
            label.Text = Renderer.GetStringWithLimitedWidth(tips[index], label.FontIndex, Width);
            var size = Renderer.GetTextDimensions(label.Text, label.FontIndex);
            label.Width = (int)size.X;
            label.Height = (int)size.Y;

            label.X = (Width - label.Width) / 2;

            _ = Task.Delay(2000).ContinueWith(_ => RandomTips());
        }
    }
}
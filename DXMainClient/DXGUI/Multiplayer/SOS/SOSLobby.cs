#nullable enable
using ClientCore;
using ClientCore.ExternalAccount;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.Online;
using DTAClient.Online.Backend;
using DTAClient.Online.Backend.Models;
using DTAClient.Online.Backend.EventArguments;
using DTAClient.Online.EventArguments;
using DTAClient.DXGUI.Multiplayer.GameLobby.CommandHandlers;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ClientCore.Enums;
using ClientCore.Extensions;
using SixLabors.ImageSharp;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DTAClient.DXGUI.Multiplayer.SOS
{
    using UserChannelPair = Tuple<string, string>;
    using InvitationIndex = Dictionary<Tuple<string, string>, WeakReference>;

    internal class SOSLobby : XNAWindow, ISwitchable
    {
        public event EventHandler UpdateCheck;

        public SOSLobby(WindowManager windowManager,
            IBackendManager backendManager,
            CnCNetGameLobby gameLobby, // TODO: 考虑创建SOSGameLobby
            CnCNetGameLoadingLobby gameLoadingLobby,
            TopBar topBar, PrivateMessagingWindow pmWindow, TunnelHandler tunnelHandler,
            GameCollection gameCollection, CnCNetUserData cncnetUserData,
            OptionsWindow optionsWindow, MapLoader mapLoader, Random random,
            ExternalAccountService externalAccountService,
            LoginWindow loginWindow,
            PlayerIdentityService playerIdentityService)
            : base(windowManager)
        {
            this.backendManager = backendManager;
            this.gameLobby = gameLobby;
            this.gameLoadingLobby = gameLoadingLobby;
            this.tunnelHandler = tunnelHandler;
            this.topBar = topBar;
            this.pmWindow = pmWindow;
            this.gameCollection = gameCollection;
            this.cncnetUserData = cncnetUserData;
            this.optionsWindow = optionsWindow;
            this.mapLoader = mapLoader;
            this.random = random;
            this.externalAccountService = externalAccountService;
            this.accountLoginWindow = loginWindow;
            this._playerIdentityService = playerIdentityService;

            // SOSLobby 不使用 IRC CTCP 命令，但保留游戏邀请处理的基础结构
            ctcpCommandHandlers = new CommandHandlerBase[]
            {
                // 后端游戏邀请可以通过其他机制实现
            };

            topBar.LogoutEvent += LogoutEvent;
        }

        private MapLoader mapLoader;

        private readonly IBackendManager backendManager;
        private CnCNetUserData cncnetUserData;
        private readonly OptionsWindow optionsWindow;
        private readonly ExternalAccountService externalAccountService;
        private readonly PlayerIdentityService _playerIdentityService;
        private bool _switchOnCalled;

        private PlayerListBox lbPlayerList;
        private ChatListBox lbChatMessages;
        private GameListBox lbGameList;
        private GlobalContextMenu globalContextMenu;

        private XNAClientButton btnLogout;
        private XNAClientButton btnNewGame;
        private XNAClientButton btnJoinGame;

        private XNAChatTextBox tbChatInput;

        // 移除IRC特定UI：颜色选择器和频道选择器
        // private XNALabel lblColor;
        // private XNAClientDropDown ddColor;
        // private XNALabel lblCurrentChannel;
        // private XNAClientDropDown ddCurrentChannel;

        private XNALabel lblOnline;
        private XNALabel lblOnlineCount;

        private XNASuggestionTextBox tbGameSearch;

        private XNAClientStateButton<SortDirection> btnGameSortAlpha;

        private XNAClientToggleButton btnGameFilterOptions;

        private DarkeningPanel gameCreationPanel;

        private Channel currentChatChannel;

        private GameCollection gameCollection;

        private Color cAdminNameColor;

        private Texture2D unknownGameIcon;
        private Texture2D adminGameIcon;

        private EnhancedSoundEffect sndGameCreated;
        private EnhancedSoundEffect sndGameInviteReceived;

        // 移除IRC颜色数组
        // private IRCColor[] chatColors;

        private CnCNetGameLobby gameLobby;
        private CnCNetGameLoadingLobby gameLoadingLobby;

        private TunnelHandler tunnelHandler;

        private CnCNetLoginWindow loginWindow;
        private LoginWindow accountLoginWindow;

        private TopBar topBar;

        private PrivateMessagingWindow pmWindow;

        private PasswordRequestWindow passwordRequestWindow;

        private bool isInGameRoom = false;
        private bool updateDenied = false;

        private string localGameID;
        private CnCNetGame localGame;

        private List<string> followedGames = new List<string>();

        private bool isJoiningGame = false;
        private HostedCnCNetGame gameOfLastJoinAttempt;

        private CancellationTokenSource gameCheckCancellation;
        private Timer? _backendGameRefreshTimer;
        private bool _isRefreshingBackendGames;

        private CommandHandlerBase[] ctcpCommandHandlers;

        private InvitationIndex invitationIndex;

        private GameFiltersPanel panelGameFilters;

        private Random random;

        private bool ctcpInvalidGameMessageShown = false;
        private bool ctcpNoTunnelMessageShown = false;
        private bool ctcpNoTunnelForGamesMessageShown = false;

        public override void Initialize()
        {
            invitationIndex = new InvitationIndex();

            // SOSLobby 始终使用 BackendManager
            Logger.Log("Using Backend Manager for SOS Lobby");

            ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX - 64,
                WindowManager.RenderResolutionY - 64);

            Name = nameof(SOSLobby);
            BackgroundTexture = AssetLoader.LoadTexture("cncnetlobbybg.png"); // 可以使用相同背景或新背景
            localGameID = ClientConfiguration.Instance.LocalGame;
            localGame = gameCollection.GameList.Find(g => g.InternalName.ToUpper() == localGameID.ToUpper());

            btnNewGame = new XNAClientButton(WindowManager);
            btnNewGame.Name = nameof(btnNewGame);
            btnNewGame.ClientRectangle = new Rectangle(12, Height - 29, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnNewGame.Text = "Create Game".L10N("Client:Main:CreateGame");
            btnNewGame.AllowClick = false;
            btnNewGame.LeftClick += BtnNewGame_LeftClick;

            btnJoinGame = new XNAClientButton(WindowManager);
            btnJoinGame.Name = nameof(btnJoinGame);
            btnJoinGame.ClientRectangle = new Rectangle(btnNewGame.Right + 12,
                btnNewGame.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnJoinGame.Text = "Join Game".L10N("Client:Main:JoinGame");
            btnJoinGame.AllowClick = false;
            btnJoinGame.LeftClick += BtnJoinGame_LeftClick;

            btnLogout = new XNAClientButton(WindowManager);
            btnLogout.Name = nameof(btnLogout);
            btnLogout.ClientRectangle = new Rectangle(Width - 145, btnNewGame.Y,
                UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLogout.Text = "Main Menu".L10N("Client:Main:MainMenu"); // SOSLobby 返回到主菜单
            btnLogout.LeftClick += BtnLogout_LeftClick;

            var gameListRectangle = new Rectangle(
                btnNewGame.X, 41,
                btnJoinGame.Right - btnNewGame.X, btnNewGame.Y - 47
            );

            panelGameFilters = new GameFiltersPanel(WindowManager);
            panelGameFilters.Name = nameof(panelGameFilters);
            panelGameFilters.ClientRectangle = gameListRectangle;
            panelGameFilters.Disable();

            lbGameList = new GameListBox(WindowManager, mapLoader, localGameID, HostedGameMatches);
            lbGameList.Name = nameof(lbGameList);
            lbGameList.ClientRectangle = gameListRectangle;
            lbGameList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbGameList.DoubleLeftClick += LbGameList_DoubleLeftClick;
            lbGameList.RightClick += LbGameList_RightClick;
            lbGameList.AllowMultiLineItems = false;
            lbGameList.ClientRectangleUpdated += GameList_ClientRectangleUpdated;

            lbPlayerList = new PlayerListBox(WindowManager, gameCollection);
            lbPlayerList.Name = nameof(lbPlayerList);
            lbPlayerList.ClientRectangle = new Rectangle(Width - 202,
                20, 190,
                btnLogout.Y - 26);
            lbPlayerList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbPlayerList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbPlayerList.LineHeight = 16;
            lbPlayerList.DoubleLeftClick += LbPlayerList_DoubleLeftClick;
            lbPlayerList.RightClick += LbPlayerList_RightClick;

            globalContextMenu = new GlobalContextMenu(WindowManager, null, cncnetUserData, pmWindow); // connectionManager 为 null
            // TODO: 需要适配 GlobalContextMenu 以支持后端用户

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = nameof(lbChatMessages);
            lbChatMessages.ClientRectangle = new Rectangle(lbGameList.Right + 12, lbGameList.Y,
                lbPlayerList.X - lbGameList.Right - 24, lbPlayerList.Height);
            lbChatMessages.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.LineHeight = 16;
            lbChatMessages.LeftClick += (sender, args) => lbGameList.SelectedIndex = -1;
            lbChatMessages.RightClick += LbChatMessages_RightClick;

            tbChatInput = new XNAChatTextBox(WindowManager);
            tbChatInput.Name = nameof(tbChatInput);
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.X,
                btnNewGame.Y, lbChatMessages.Width,
                btnNewGame.Height);
            tbChatInput.Suggestion = "Type here to chat...".L10N("Client:Main:ChatHere");
            tbChatInput.Enabled = false;
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            // 移除颜色选择器相关UI
            // lblColor = new XNALabel(WindowManager);
            // ddColor = new XNAClientDropDown(WindowManager);

            // 移除频道选择器相关UI
            // lblCurrentChannel = new XNALabel(WindowManager);
            // ddCurrentChannel = new XNAClientDropDown(WindowManager);

            lblOnline = new XNALabel(WindowManager);
            lblOnline.Name = nameof(lblOnline);
            lblOnline.ClientRectangle = new Rectangle(310, 14, 0, 0);
            lblOnline.Text = "Online:".L10N("Client:Main:OnlineLabel");
            lblOnline.FontIndex = 1;
            lblOnline.Disable();

            lblOnlineCount = new XNALabel(WindowManager);
            lblOnlineCount.Name = nameof(lblOnlineCount);
            lblOnlineCount.ClientRectangle = new Rectangle(lblOnline.X + 50, 14, 0, 0);
            lblOnlineCount.FontIndex = 1;
            lblOnlineCount.Disable();

            tbGameSearch = new XNASuggestionTextBox(WindowManager);
            tbGameSearch.Name = nameof(tbGameSearch);
            tbGameSearch.ClientRectangle = new Rectangle(lbGameList.X,
                12, lbGameList.Width - 62, 21);
            tbGameSearch.Suggestion = "Filter by name, map, game mode, player...".L10N("Client:Main:FilterByBlahBlah");
            tbGameSearch.MaximumTextLength = 64;
            tbGameSearch.InputReceived += TbGameSearch_InputReceived;
            tbGameSearch.Disable();

            btnGameSortAlpha = new XNAClientStateButton<SortDirection>(WindowManager, new Dictionary<SortDirection, Texture2D>()
            {
                { SortDirection.None, AssetLoader.LoadTexture("sortAlphaNone.png") },
                { SortDirection.Asc, AssetLoader.LoadTexture("sortAlphaAsc.png") },
                { SortDirection.Desc, AssetLoader.LoadTexture("sortAlphaDesc.png") },
            });
            btnGameSortAlpha.Name = nameof(btnGameSortAlpha);
            btnGameSortAlpha.ClientRectangle = new Rectangle(
                tbGameSearch.X + tbGameSearch.Width + 10, tbGameSearch.Y,
                21, 21);
            btnGameSortAlpha.LeftClick += BtnGameSortAlpha_LeftClick;
            btnGameSortAlpha.SetToolTipText("Sort Games Alphabetically".L10N("Client:Main:SortAlphabet"));
            RefreshGameSortAlphaBtn();

            btnGameFilterOptions = new XNAClientToggleButton(WindowManager);
            btnGameFilterOptions.Name = nameof(btnGameFilterOptions);
            btnGameFilterOptions.ClientRectangle = new Rectangle(
                btnGameSortAlpha.X + btnGameSortAlpha.Width + 10, tbGameSearch.Y,
                21, 21);
            btnGameFilterOptions.CheckedTexture = AssetLoader.LoadTexture("filterActive.png");
            btnGameFilterOptions.UncheckedTexture = AssetLoader.LoadTexture("filterInactive.png");
            btnGameFilterOptions.LeftClick += BtnGameFilterOptions_LeftClick;
            btnGameFilterOptions.SetToolTipText("Game Filters".L10N("Client:Main:GameFilters"));
            RefreshGameFiltersBtn();

            InitializeGameList();

            AddChild(btnNewGame);
            AddChild(btnJoinGame);
            AddChild(btnLogout);
            AddChild(lbPlayerList);
            AddChild(lbChatMessages);
            AddChild(lbGameList);
            AddChild(panelGameFilters);
            AddChild(tbChatInput);
            // AddChild(lblColor);
            // AddChild(ddColor);
            // AddChild(lblCurrentChannel);
            // AddChild(ddCurrentChannel);
            AddChild(globalContextMenu);
            AddChild(lblOnline);
            AddChild(lblOnlineCount);
            AddChild(tbGameSearch);
            AddChild(btnGameSortAlpha);
            AddChild(btnGameFilterOptions);

            panelGameFilters.VisibleChanged += GameFiltersPanel_VisibleChanged;

            // TODO: 需要适配在线人数统计
            // CnCNetPlayerCountTask.CnCNetGameCountUpdated += OnCnCNetGameCountUpdated;
            // UpdateOnlineCount(CnCNetPlayerCountTask.PlayerCount);

            pmWindow.SetJoinUserAction(JoinUser);

            base.Initialize();

            WindowManager.CenterControlOnScreen(this);

            PostUIInit();
        }

        private void GameList_ClientRectangleUpdated(object sender, EventArgs e)
        {
            panelGameFilters.ClientRectangle = lbGameList.ClientRectangle;
        }

        private void LogoutEvent(object sender, EventArgs e)
        {
            isJoiningGame = false;
        }

        // 以下方法需要从 CnCNetLobby 适配
        // 先占位实现，后续完善
        
        private void BtnGameSortAlpha_LeftClick(object sender, EventArgs e)
        {
            UserINISettings.Instance.SortState.Value = (int)btnGameSortAlpha.GetState();
            RefreshGameSortAlphaBtn();
            SortAndRefreshHostedGames();
            UserINISettings.Instance.SaveSettings();
        }

        private void SortAndRefreshHostedGames()
        {
            lbGameList.SortAndRefreshHostedGames();
        }

        private void BtnGameFilterOptions_LeftClick(object sender, EventArgs e)
        {
            if (panelGameFilters.Visible)
                panelGameFilters.Cancel();
            else
                panelGameFilters.Show();
        }

        private void RefreshGameSortAlphaBtn()
        {
            if (Enum.IsDefined(typeof(SortDirection), UserINISettings.Instance.SortState.Value))
                btnGameSortAlpha.SetState((SortDirection)UserINISettings.Instance.SortState.Value);
        }

        private void RefreshGameFiltersBtn()
        {
            btnGameFilterOptions.Checked = UserINISettings.Instance.IsGameFiltersApplied();
        }

        private void GameFiltersPanel_VisibleChanged(object sender, EventArgs e)
        {
            if (panelGameFilters.Visible)
                return;

            RefreshGameFiltersBtn();
            SortAndRefreshHostedGames();
        }

        private void TbGameSearch_InputReceived(object sender, EventArgs e)
        {
            SortAndRefreshHostedGames();
            lbGameList.ViewTop = 0;
        }

        private bool HostedGameMatches(GenericHostedGame hg)
        {
            // 从 CnCNetLobby 复制，稍后适配
            if (UserINISettings.Instance.ShowFriendGamesOnly)
                return hg.Players.Any(cncnetUserData.IsFriend);

            if (UserINISettings.Instance.HideLockedGames.Value && hg.Locked)
                return false;

            if (UserINISettings.Instance.HideIncompatibleGames.Value && hg.Incompatible)
                return false;

            if (UserINISettings.Instance.HidePasswordedGames.Value && hg.Passworded)
                return false;

            if (hg.MaxPlayers > UserINISettings.Instance.MaxPlayerCount.Value)
                return false;

            string textUpper = tbGameSearch?.Text?.ToUpperInvariant();

            string translatedGameMode = string.IsNullOrEmpty(hg.GameMode)
                ? "Unknown".L10N("Client:Main:Unknown")
                : hg.GameMode.L10N($"INI:GameModes:{hg.GameMode}:UIName", notify: false);

            string translatedMapName = string.IsNullOrEmpty(hg.Map)
                ? "Unknown".L10N("Client:Main:Unknown") : mapLoader.TranslatedMapNames.ContainsKey(hg.Map)
                ? mapLoader.TranslatedMapNames[hg.Map] : null;

            return
                string.IsNullOrWhiteSpace(tbGameSearch?.Text) ||
                tbGameSearch.Text == tbGameSearch.Suggestion ||
                hg.RoomName.ToUpperInvariant().Contains(textUpper) ||
                hg.GameMode.ToUpperInvariant().Equals(textUpper, StringComparison.Ordinal) ||
                translatedGameMode.ToUpperInvariant().Equals(textUpper, StringComparison.Ordinal) ||
                hg.Map.ToUpperInvariant().Contains(textUpper) ||
                (translatedMapName is not null && translatedMapName.ToUpperInvariant().Contains(textUpper)) ||
                hg.Players.Any(pl => pl.ToUpperInvariant().Equals(textUpper, StringComparison.Ordinal));
        }

        private void InitializeGameList()
        {
            // SOSLobby 的游戏列表初始化逻辑不同
            // 不需要IRC频道，直接从后端获取房间
            // 占位实现
        }

        private void PostUIInit()
        {
            // 从 CnCNetLobby 适配，移除IRC相关，保留后端相关
            sndGameCreated = new EnhancedSoundEffect("gamecreated.wav");
            sndGameInviteReceived = new EnhancedSoundEffect("pm.wav");

            cAdminNameColor = AssetLoader.GetColorFromString(ClientConfiguration.Instance.AdminNameColor);

            var assembly = Assembly.GetAssembly(typeof(GameCollection));
            using Stream unknownIconStream = assembly.GetManifestResourceStream("DTAClient.Icons.unknownicon.png");
            using Stream cncnetIconStream = assembly.GetManifestResourceStream("DTAClient.Icons.cncneticon.png");

            unknownGameIcon = AssetLoader.TextureFromImage(Image.Load(unknownIconStream));
            adminGameIcon = AssetLoader.TextureFromImage(Image.Load(cncnetIconStream));

            // 后端管理器事件订阅
            backendManager.Connected += BackendManager_Connected;
            backendManager.WelcomeMessageReceived += ConnectionManager_WelcomeMessageReceived;
            backendManager.Disconnected += ConnectionManager_Disconnected;
            // backendManager.PrivateCTCPReceived 可能不需要
            
            backendManager.MultipleUsersAdded += (sender, e) => 
            {
                Logger.Log("[SOSLobby] MultipleUsersAdded event fired, refreshing player list and ensuring UI is enabled");
                WindowManager.AddCallback(new Action(() =>
                {
                    EnableUIWhenReady();
                }), null);
            };

            cncnetUserData.UserFriendToggled += RefreshPlayerList;
            cncnetUserData.UserIgnoreToggled += RefreshPlayerList;

            gameCreationPanel = new DarkeningPanel(WindowManager);
            AddChild(gameCreationPanel);

            GameCreationWindow gcw = new GameCreationWindow(WindowManager, tunnelHandler);
            gameCreationPanel.AddChild(gcw);
            gameCreationPanel.Tag = gcw;
            gcw.Cancelled += Gcw_Cancelled;
            gcw.GameCreated += Gcw_GameCreated;
            gcw.LoadedGameCreated += Gcw_LoadedGameCreated;

            gameCreationPanel.Hide();

            string clientVersion = GitVersionInformation.AssemblySemVer;
#if DEVELOPMENT_BUILD
            clientVersion = $"{GitVersionInformation.CommitDate} {GitVersionInformation.BranchName}@{GitVersionInformation.ShortSha}";
#endif

            // 在主频道显示欢迎消息
            if (backendManager.MainChannel != null)
            {
                backendManager.MainChannel.AddMessage(new ChatMessage(Color.White, Renderer.GetSafeString(
                        string.Format("*** SOS Lobby - DTA CnCNet Client version {0} ***", clientVersion),
                        lbChatMessages.FontIndex)));
            }

            // 开发版本警告
#if DEVELOPMENT_BUILD
            if (ClientConfiguration.Instance.ShowDevelopmentBuildWarnings && backendManager.MainChannel != null)
            {
                backendManager.MainChannel.AddMessage(new ChatMessage(Color.Red, Renderer.GetSafeString(
                        "This is a development build of the client. Stability and reliability may not be fully guaranteed.".L10N("Client:Main:DevelopmentBuildWarning"),
                        lbChatMessages.FontIndex)));
            }
#endif

            backendManager.BannedFromChannel += ConnectionManager_BannedFromChannel;

            // Use the provided accountLoginWindow (backend login window)
            // Ensure it's properly initialized and event handlers are connected
            // Note: accountLoginWindow is already added to UI by MainMenu or other parent
            // We just need to make sure it's visible when needed
            accountLoginWindow.Connect += LoginWindow_Connect;
            accountLoginWindow.Cancelled += LoginWindow_Cancelled;
            accountLoginWindow.LoginRequested += LoginWindow_LoginRequested;

            passwordRequestWindow = new PasswordRequestWindow(WindowManager, pmWindow);
            passwordRequestWindow.PasswordEntered += PasswordRequestWindow_PasswordEntered;

            var passwordRequestWindowPanel = new DarkeningPanel(WindowManager);
            passwordRequestWindowPanel.Alpha = 0.0f;
            AddChild(passwordRequestWindowPanel);
            passwordRequestWindowPanel.AddChild(passwordRequestWindow);
            passwordRequestWindow.Disable();

            gameLobby.GameLeft += GameLobby_GameLeft;
            gameLoadingLobby.GameLeft += GameLoadingLobby_GameLeft;

            UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;

            GameProcessLogic.GameProcessStarted += SharedUILogic_GameProcessStarted;
            GameProcessLogic.GameProcessExited += SharedUILogic_GameProcessExited;

            // 初始化后端游戏刷新定时器
            _backendGameRefreshTimer = new Timer(BackendGameRefreshTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
            
            // 订阅房间事件
            backendManager.RoomCreated += OnBackendRoomCreated;
            backendManager.RoomUpdated += OnBackendRoomUpdated;
            backendManager.RoomDeleted += OnBackendRoomDeleted;
            
            // 初始加载后端游戏
            if (backendManager.IsConnected)
            {
                _ = RefreshBackendGamesAsync();
                
                // 如果已连接，设置UI状态
                WindowManager.AddCallback(new Action(() =>
                {
                    btnNewGame.AllowClick = true;
                    btnJoinGame.AllowClick = true;
                    tbChatInput.Enabled = true;
                    tbGameSearch.Enabled = true;
                    
                    if (backendManager.MainChannel != null)
                    {
                        currentChatChannel = backendManager.MainChannel;
                        SubscribeToChannelEvents(currentChatChannel);
                    }
                    
                    Logger.Log("[SOSLobby] Backend already connected, UI enabled.");
                }), null);
            }
        }

        // 后续需要实现的方法占位
        private void BtnNewGame_LeftClick(object sender, EventArgs e)
        {
            if (isInGameRoom)
            {
                topBar.SwitchToPrimary();
                return;
            }

            gameCreationPanel.Show();
            var gcw = (GameCreationWindow)gameCreationPanel.Tag;

            gcw.Refresh();
        }
        private void BtnJoinGame_LeftClick(object sender, EventArgs e) => JoinSelectedGame();
        private void LbGameList_DoubleLeftClick(object sender, EventArgs e) => JoinSelectedGame();
        private void LbGameList_RightClick(object sender, EventArgs e) { }
        private void LbPlayerList_DoubleLeftClick(object sender, EventArgs e) { }
        private void LbPlayerList_RightClick(object sender, EventArgs e) { }
        private void LbChatMessages_RightClick(object sender, EventArgs e) { }
        private void TbChatInput_EnterPressed(object sender, EventArgs e)
        {
            Logger.Log($"[SOSLobby] TbChatInput_EnterPressed called, text: {tbChatInput.Text}");
            
            if (string.IsNullOrEmpty(tbChatInput.Text))
            {
                Logger.Log("[SOSLobby] Text is empty, returning");
                return;
            }

            var channelToUse = currentChatChannel ?? backendManager.MainChannel;
            
            if (channelToUse == null)
            {
                Logger.Log("[SOSLobby] No channel available for chat (currentChatChannel and MainChannel are both null)");
                return;
            }

            Logger.Log($"[SOSLobby] Using channel: {channelToUse.UIName}, type: {channelToUse.GetType().Name}");
            
            // Backend channels may not require color, use default
            channelToUse.SendChatMessage(tbChatInput.Text, null);

            tbChatInput.Text = string.Empty;
        }
        private void BtnLogout_LeftClick(object sender, EventArgs e)
        {
            if (isInGameRoom)
            {
                topBar.SwitchToPrimary();
                return;
            }

            if (backendManager.IsConnected &&
                !UserINISettings.Instance.PersistentMode)
            {
                backendManager.Disconnect();
            }

            topBar.SwitchToPrimary();
        }
        
        private void BackendManager_Connected(object sender, EventArgs e)
        {
            Logger.Log("[SOSLobby] === BackendManager_Connected event received ===");
            Logger.Log($"[SOSLobby] BackendManager_Connected: backendManager.IsConnected = {backendManager.IsConnected}");
            Logger.Log($"[SOSLobby] BackendManager_Connected: backendManager.MainChannel is null = {backendManager.MainChannel == null}");
            Logger.Log($"[SOSLobby] BackendManager_Connected: backendManager.IsAttemptingConnection = {backendManager.IsAttemptingConnection}");
            
            // When backend is connected, ensure UI is enabled and channel is set up
            WindowManager.AddCallback(new Action(() =>
            {
                Logger.Log("[SOSLobby] BackendManager_Connected callback executing on UI thread");
                Logger.Log($"[SOSLobby] BackendManager_Connected (UI thread): backendManager.IsConnected = {backendManager.IsConnected}");
                Logger.Log($"[SOSLobby] BackendManager_Connected (UI thread): backendManager.MainChannel is null = {backendManager.MainChannel == null}");
                
                // 立即尝试启用UI
                EnableUIWhenReady();
                
                // 如果MainChannel仍然为null，设置一个定时检查（最多5次，每次500ms）
                if (backendManager.IsConnected && backendManager.MainChannel == null)
                {
                    Logger.Log("[SOSLobby] BackendManager_Connected: MainChannel is null after connection, scheduling periodic check");
                    _ = CheckMainChannelPeriodically(5, 500);
                }
            }), null);
        }
        
        private async Task CheckMainChannelPeriodically(int maxAttempts, int delayMs)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await Task.Delay(delayMs);
                
                WindowManager.AddCallback(new Action(() =>
                {
                    Logger.Log($"[SOSLobby] CheckMainChannelPeriodically attempt {attempt}/{maxAttempts}: MainChannel is null = {backendManager.MainChannel == null}");
                    
                    if (backendManager.MainChannel != null)
                    {
                        Logger.Log("[SOSLobby] MainChannel is now available, enabling UI");
                        EnableUIWhenReady();
                    }
                    else if (attempt == maxAttempts)
                    {
                        Logger.Log("[SOSLobby] Max attempts reached, MainChannel still null. UI may remain disabled.");
                    }
                }), null);
                
                if (backendManager.MainChannel != null)
                    break;
            }
        }
        
        private void ConnectionManager_WelcomeMessageReceived(object sender, ServerMessageEventArgs e)
        {
            Logger.Log($"[SOSLobby] WelcomeMessageReceived: {e.Message}");
            
            // Ensure UI is enabled when ready
            WindowManager.AddCallback(new Action(() =>
            {
                EnableUIWhenReady();
            }), null);
        }
        private void ConnectionManager_Disconnected(object sender, EventArgs e) 
        {
            Logger.Log("[SOSLobby] Backend disconnected, disabling UI.");
            btnNewGame.AllowClick = false;
            btnJoinGame.AllowClick = false;
            tbChatInput.Enabled = false;
            tbGameSearch.Enabled = false;
            
            // 重置SwitchOn标志，允许重新连接
            _switchOnCalled = false;
            Logger.Log("[SOSLobby] Reset _switchOnCalled to allow reconnection.");
        }
        
        private void ConnectionManager_BannedFromChannel(object sender, ChannelEventArgs e) 
        {
            Logger.Log($"[SOSLobby] Banned from channel: {e.ChannelName}");
        }
        
        private void LoginWindow_Connect(object sender, EventArgs e) 
        {
            Logger.Log("[SOSLobby] LoginWindow_Connect called, hiding login window.");
            accountLoginWindow.Disable();
            // 登录窗口应该已经建立了后端会话
            // WelcomeMessageReceived 事件将会触发以启用UI
        }
        
        private void LoginWindow_Cancelled(object sender, EventArgs e) 
        {
            Logger.Log("[SOSLobby] LoginWindow_Cancelled called, hiding login window and returning to main menu.");
            accountLoginWindow.Disable();
            topBar.SwitchToPrimary();
        }
        
        private void LoginWindow_LoginRequested(object sender, EventArgs e) 
        {
            Logger.Log("[SOSLobby] LoginWindow_LoginRequested called.");
            // 登录窗口会处理登录请求
        }
        
        private void GameLobby_GameLeft(object sender, EventArgs e) { }
        private void GameLoadingLobby_GameLeft(object sender, EventArgs e) { }
        
        private void PasswordRequestWindow_PasswordEntered(object sender, PasswordEventArgs e) => _JoinGame(e.HostedGame, e.Password);
        
        private void SharedUILogic_GameProcessStarted() { }
        private void SharedUILogic_GameProcessExited() { }
        
        private void Instance_SettingsSaved(object sender, EventArgs e) { }
        
        private void Gcw_Cancelled(object sender, EventArgs e) => gameCreationPanel.Hide();
        private async void Gcw_GameCreated(object sender, GameCreationEventArgs e)
        {
            if (gameLobby.Enabled || gameLoadingLobby.Enabled)
                return;

            string password = e.Password;
            bool isCustomPassword = true;
            if (string.IsNullOrEmpty(password))
            {
                // Backend will generate password automatically
                isCustomPassword = false;
            }

            try
            {
                var backendChannel = await backendManager.SpaceManager.CreateRoomAsync(e.GameRoomName, e.MaxPlayers, !isCustomPassword);
                backendManager.AddChannel(backendChannel);
                gameLobby.SetUp(backendChannel, true, e.MaxPlayers, e.Tunnel, ProgramConstants.PLAYERNAME, isCustomPassword, e.SkillLevel);
                backendChannel.UserAdded += GameChannel_UserAdded;
                backendChannel.JoinBackend();
                
                backendManager.MainChannel.AddMessage(new ChatMessage(Color.White,
                   string.Format("Creating a game named {0} ...".L10N("Client:Main:CreateGameNamed"), e.GameRoomName)));
                
                gameCreationPanel.Hide();
                
                pmWindow.SetInviteChannelInfo(backendChannel.ChannelName, e.GameRoomName, string.IsNullOrEmpty(e.Password) ? string.Empty : e.Password);
            }
            catch (Exception ex)
            {
                Logger.Log($"[SOSLobby] Failed to create room: {ex.Message}");
                backendManager.MainChannel.AddMessage(new ChatMessage(Color.Red, $"Failed to create room: {ex.Message}"));
            }
        }
        private void Gcw_LoadedGameCreated(object sender, GameCreationEventArgs e) { }
        
        private async Task RefreshBackendGamesAsync()
        {
            if (_isRefreshingBackendGames)
                return;
            
            try
            {
                _isRefreshingBackendGames = true;
                
                Logger.Log($"[SOSLobby] Refreshing backend games, backendManager type: {backendManager.GetType().Name}, SpaceManager is null: {backendManager.SpaceManager == null}");
                
                if (backendManager.SpaceManager == null)
                {
                    Logger.Log("[SOSLobby] SpaceManager is null, cannot refresh games");
                    return;
                }
                
                var spaces = await backendManager.SpaceManager.GetRoomSpacesAsync();
                
                Logger.Log($"[SOSLobby] Retrieved {spaces?.Count ?? 0} spaces from backend");
                
                // Capture spaces for UI thread callback
                var capturedSpaces = spaces;
                
                WindowManager.AddCallback(new Action(() =>
                {
                    // Clear current backend games
                    var currentGames = lbGameList.HostedGames.ToList();
                    var backendGames = currentGames.Where(g => g is HostedCnCNetGame hg && hg.ChannelName?.StartsWith("#") == true).ToList();
                    
                    Logger.Log($"[SOSLobby] Clearing {backendGames.Count} existing backend games");
                    
                    foreach (var game in backendGames)
                    {
                        lbGameList.HostedGames.Remove(game);
                    }

                    // Add new games
                    if (capturedSpaces != null)
                    {
                        foreach (var space in capturedSpaces)
                        {
                            Logger.Log($"[SOSLobby] Adding space: {space.Name} (ID: {space.Id}, Members: {space.MemberCount}/{space.MaxMembers})");
                            
                            // Convert SpaceResponse to HostedCnCNetGame
                            var hostedGame = new HostedCnCNetGame
                            {
                                ChannelName = $"#{space.Id}",
                                RoomName = space.Name,
                                MaxPlayers = space.MaxMembers,
                                Passworded = space.IsPrivate,
                                Tunneled = true, // Backend games are tunneled
                                Players = Array.Empty<string>(), // TODO: Get actual players from space members
                                HostName = space.OwnerUserId.HasValue ? $"User{space.OwnerUserId}" : "Host",
                                Map = "Unknown",
                                GameMode = "Standard",
                                MapHash = string.Empty,
                                Revision = ProgramConstants.CNCNET_PROTOCOL_REVISION,
                                GameVersion = ProgramConstants.GAME_VERSION,
                                Game = localGame,
                                LastRefreshTime = DateTime.Now,
                                Locked = space.Status != "waiting",
                                Incompatible = false,
                                IsLoadedGame = false,
                                IsLadder = false,
                                SkillLevel = 0
                            };

                            lbGameList.HostedGames.Add(hostedGame);
                        }
                    }
                    else
                    {
                        Logger.Log("[SOSLobby] capturedSpaces is null, no games to add");
                    }

                    SortAndRefreshHostedGames();
                    Logger.Log($"[SOSLobby] Game list refreshed, total games: {lbGameList.HostedGames.Count}");
                }), null);
            }
            catch (Exception ex)
            {
                Logger.Log($"[SOSLobby] Failed to refresh backend games: {ex.Message}");
                Logger.Log($"[SOSLobby] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                _isRefreshingBackendGames = false;
            }
        }
        private void OnBackendRoomCreated(object? sender, RoomCreatedEventArgs e)
        {
            Logger.Log($"[SOSLobby] Room created event: {e.Data.Name} (ID: {e.Data.Id})");
            
            // Convert event data to SpaceResponse
            var space = new SpaceResponse
            {
                Id = e.Data.Id,
                Name = e.Data.Name,
                Type = e.Data.Type ?? "room",
                MaxMembers = e.Data.MaxMembers,
                IsPrivate = e.Data.IsPrivate,
                Status = e.Data.Status,
                OwnerUserId = e.Data.OwnerUserId,
                MemberCount = e.Data.MemberCount,
                CreatedAt = e.Data.CreatedAt,
                UpdatedAt = e.Data.UpdatedAt
            };
            
            WindowManager.AddCallback(new Action(() =>
            {
                AddBackendGame(space);
            }), null);
        }
        private void OnBackendRoomUpdated(object? sender, RoomUpdatedEventArgs e)
        {
            Logger.Log($"[SOSLobby] Room updated event: ID {e.Data.Id} ({e.Data.Name})");
            
            // For updates, we need to fetch the complete space info
            // For now, trigger a full refresh
            WindowManager.AddCallback(new Action(() =>
            {
                // Mark for refresh on next timer tick
                _ = RefreshBackendGamesAsync();
            }), null);
        }
        private void OnBackendRoomDeleted(object? sender, RoomDeletedEventArgs e)
        {
            Logger.Log($"[SOSLobby] Room deleted event: ID {e.Data.Id}");
            
            WindowManager.AddCallback(new Action(() =>
            {
                RemoveBackendGame(e.Data.Id);
            }), null);
        }
        private void BackendGameRefreshTimerCallback(object state)
        {
            if (backendManager.IsConnected)
            {
                _ = RefreshBackendGamesAsync();
            }
        }
        
        private void SubscribeToChannelEvents(Channel channel)
        {
            if (channel == null)
                return;
            channel.UserAdded += RefreshPlayerList;
            channel.UserLeft += RefreshPlayerList;
            channel.UserQuitIRC += RefreshPlayerList;
            channel.UserKicked += RefreshPlayerList;
            channel.UserListReceived += RefreshPlayerList;
            channel.MessageAdded += CurrentChatChannel_MessageAdded;
            channel.UserGameIndexUpdated += CurrentChatChannel_UserGameIndexUpdated;
        }

        private void UnsubscribeFromChannelEvents(Channel channel)
        {
            if (channel == null)
                return;
            channel.UserAdded -= RefreshPlayerList;
            channel.UserLeft -= RefreshPlayerList;
            channel.UserQuitIRC -= RefreshPlayerList;
            channel.UserKicked -= RefreshPlayerList;
            channel.UserListReceived -= RefreshPlayerList;
            channel.MessageAdded -= CurrentChatChannel_MessageAdded;
            channel.UserGameIndexUpdated -= CurrentChatChannel_UserGameIndexUpdated;
        }

        private void RefreshPlayerList(object sender, EventArgs e)
        {
            Logger.Log($"[SOSLobby] RefreshPlayerList called, currentChatChannel is null: {currentChatChannel == null}, backendManager.MainChannel is null: {backendManager.MainChannel == null}");
            
            var channelToUse = currentChatChannel ?? backendManager.MainChannel;
            
            if (channelToUse == null)
            {
                Logger.Log("[SOSLobby] No channel available, cannot refresh player list");
                return;
            }
            
            Logger.Log($"[SOSLobby] Using channel: {channelToUse.UIName}, Users count: {channelToUse.Users.Count}");
            
            string selectedUserName = lbPlayerList.SelectedItem == null ?
                string.Empty : lbPlayerList.SelectedItem.Text;
            
            lbPlayerList.Clear();
            
            int userCount = 0;
            
            // Try different methods to iterate users for compatibility
            try
            {
                // Method 1: Use GetFirst() if available (for SortedUserCollection)
                if (channelToUse.Users is SortedUserCollection<ChannelUser> sortedCollection)
                {
                    var current = sortedCollection.GetFirst();
                    while (current != null)
                    {
                        var user = current.Value;
                        user.IRCUser.IsFriend = cncnetUserData.IsFriend(user.IRCUser.Name);
                        user.IRCUser.IsIgnored = cncnetUserData.IsIgnored(user.IRCUser.Ident);
                        lbPlayerList.AddUser(user);
                        current = current.Next;
                        userCount++;
                    }
                }
                // Method 2: Use foreach if Users implements IEnumerable<ChannelUser>
                else if (channelToUse.Users is System.Collections.Generic.IEnumerable<ChannelUser> enumerable)
                {
                    foreach (var user in enumerable)
                    {
                        user.IRCUser.IsFriend = cncnetUserData.IsFriend(user.IRCUser.Name);
                        user.IRCUser.IsIgnored = cncnetUserData.IsIgnored(user.IRCUser.Ident);
                        lbPlayerList.AddUser(user);
                        userCount++;
                    }
                }
                else
                {
                    Logger.Log("[SOSLobby] Warning: Could not determine how to iterate channel users collection");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[SOSLobby] Error refreshing player list: {ex.Message}");
            }
            
            Logger.Log($"[SOSLobby] Added {userCount} users to player list");
            
            if (selectedUserName != string.Empty)
            {
                lbPlayerList.SelectedIndex = lbPlayerList.Items.FindIndex(
                    i => i.Text == selectedUserName);
            }
        }

        private void CurrentChatChannel_UserGameIndexUpdated(object sender, ChannelUserEventArgs e)
        {
            var ircUser = e.User.IRCUser;
            var item = lbPlayerList.Items.Find(i => i.Text.StartsWith(ircUser.Name));
            
            if (ircUser.GameID < 0 || ircUser.GameID >= gameCollection.GameList.Count)
                item.Texture = unknownGameIcon;
            else
                item.Texture = gameCollection.GameList[ircUser.GameID].Texture;
        }

        private void AddMessageToChat(ChatMessage message)
        {
            if (!string.IsNullOrEmpty(message.SenderIdent) &&
                cncnetUserData.IsIgnored(message.SenderIdent) &&
                !message.SenderIsAdmin)
            {
                lbChatMessages.AddMessage(new ChatMessage(Color.Silver, string.Format("Message blocked from - {0}".L10N("Client:Main:PMBlockedFrom"), message.SenderName)));
            }
            else
            {
                lbChatMessages.AddMessage(message);
            }
        }

        private void CurrentChatChannel_MessageAdded(object sender, IRCMessageEventArgs e) =>
            AddMessageToChat(e.Message);
        private void AddBackendGame(SpaceResponse space)
        {
            // Check if game already exists
            var existingGame = lbGameList.HostedGames.FirstOrDefault(g => 
                g is HostedCnCNetGame hg && hg.ChannelName == $"#{space.Id}");
            
            if (existingGame != null)
            {
                UpdateBackendGame(space);
                return;
            }
            
            // Convert SpaceResponse to HostedCnCNetGame
            var hostedGame = new HostedCnCNetGame
            {
                ChannelName = $"#{space.Id}",
                RoomName = space.Name,
                MaxPlayers = space.MaxMembers,
                Passworded = space.IsPrivate,
                Tunneled = true, // Backend games are tunneled
                Players = Array.Empty<string>(), // TODO: Get actual players from space members
                HostName = space.OwnerUserId.HasValue ? $"User{space.OwnerUserId}" : "Host",
                Map = "Unknown",
                GameMode = "Standard",
                MapHash = string.Empty,
                Revision = ProgramConstants.CNCNET_PROTOCOL_REVISION,
                GameVersion = ProgramConstants.GAME_VERSION,
                Game = localGame,
                LastRefreshTime = DateTime.Now,
                Locked = space.Status != "waiting",
                Incompatible = false,
                IsLoadedGame = false,
                IsLadder = false,
                SkillLevel = 0
            };
            
            lbGameList.HostedGames.Add(hostedGame);
            SortAndRefreshHostedGames();
            
            Logger.Log($"[SOSLobby] Added backend game: {space.Name} (ID: {space.Id})");
        }

        private void UpdateBackendGame(SpaceResponse space)
        {
            var game = lbGameList.HostedGames.FirstOrDefault(g => 
                g is HostedCnCNetGame hg && hg.ChannelName == $"#{space.Id}") as HostedCnCNetGame;
            
            if (game == null)
                return;
            
            // Update game properties
            game.RoomName = space.Name;
            game.MaxPlayers = space.MaxMembers;
            game.Passworded = space.IsPrivate;
            game.Locked = space.Status != "waiting";
            game.LastRefreshTime = DateTime.Now;
            
            SortAndRefreshHostedGames();
            
            Logger.Log($"[SOSLobby] Updated backend game: {space.Name} (ID: {space.Id})");
        }

        private void RemoveBackendGame(int spaceId)
        {
            var game = lbGameList.HostedGames.FirstOrDefault(g => 
                g is HostedCnCNetGame hg && hg.ChannelName == $"#{spaceId}");
            
            if (game != null)
            {
                lbGameList.HostedGames.Remove(game);
                SortAndRefreshHostedGames();
                Logger.Log($"[SOSLobby] Removed backend game: ID {spaceId}");
            }
        }

        private string GetJoinGameErrorBase()
        {
            if (isJoiningGame)
                return "Cannot join game - joining game in progress. If you believe this is an error, please log out and back in.".L10N("Client:Main:JoinGameErrorInProgress");

            if (ProgramConstants.IsInGame)
                return "Cannot join game while the main game executable is running.".L10N("Client:Main:JoinGameErrorGameRunning");

            return null;
        }

        private string GetJoinGameErrorByIndex(int gameIndex)
        {
            if (gameIndex < 0 || gameIndex >= lbGameList.HostedGames.Count)
                return "Invalid game index".L10N("Client:Main:InvalidGameIndex");

            return GetJoinGameErrorBase();
        }

        private string GetJoinGameError(HostedCnCNetGame hg)
        {
            if (hg.Game.InternalName.ToUpper() != localGameID.ToUpper())
                return string.Format("The selected game is for {0}!".L10N("Client:Main:GameIsOfPurpose"), gameCollection.GetGameNameFromInternalName(hg.Game.InternalName));

            if (hg.Incompatible && ClientConfiguration.Instance.DisallowJoiningIncompatibleGames)
                return "Cannot join game. The host is on a different game version than you.".L10N("Client:Main:DisallowJoiningIncompatibleGames");

            if (hg.Locked)
                return "The selected game is locked!".L10N("Client:Main:GameLocked");

            if (hg.IsLoadedGame && !hg.Players.Contains(_playerIdentityService.GetIRCName()))
                return "You do not exist in the saved game!".L10N("Client:Main:NotInSavedGame");

            return GetJoinGameErrorBase();
        }

        private void JoinSelectedGame()
        {
            var listedGame = (HostedCnCNetGame)lbGameList.SelectedItem?.Tag;
            if (listedGame == null)
                return;
            var hostedGameIndex = lbGameList.HostedGames.IndexOf(listedGame);
            JoinGameByIndex(hostedGameIndex, string.Empty);
        }

        private bool JoinGameByIndex(int gameIndex, string password)
        {
            string error = GetJoinGameErrorByIndex(gameIndex);
            if (!string.IsNullOrEmpty(error))
            {
                backendManager.MainChannel.AddMessage(new ChatMessage(Color.White, error));
                return false;
            }

            return JoinGame((HostedCnCNetGame)lbGameList.HostedGames[gameIndex], password, backendManager.MainChannel);
        }

        private bool JoinGame(HostedCnCNetGame hg, string password, IMessageView messageView)
        {
            string error = GetJoinGameError(hg);
            if (!string.IsNullOrEmpty(error))
            {
                messageView.AddMessage(new ChatMessage(Color.White, error));
                return false;
            }

            if (isInGameRoom)
            {
                topBar.SwitchToPrimary();
                return false;
            }

            // if (hg.GameVersion != ProgramConstants.GAME_VERSION)
            // TODO Show warning

            if (hg.Passworded)
            {
                // only display password dialog if we've not been supplied with a password (invite)
                if (string.IsNullOrEmpty(password))
                {
                    passwordRequestWindow.SetHostedGame(hg);
                    passwordRequestWindow.Enable();
                    return true;
                }
            }
            else
            {
                if (!hg.IsLoadedGame)
                {
                    password = Utilities.CalculateSHA1ForString
                        (hg.ChannelName).Substring(0, 10);
                }
                else
                {
                    IniFile spawnSGIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "Saved Games", "spawnSG.ini"));
                    password = Utilities.CalculateSHA1ForString(
                        spawnSGIni.GetStringValue("Settings", "GameID", string.Empty)).Substring(0, 10);
                }
            }

            _JoinGame(hg, password);

            return true;
        }

        private void _JoinGame(HostedCnCNetGame hg, string password)
        {
            backendManager.MainChannel.AddMessage(new ChatMessage(Color.White,
                string.Format("Attempting to join game {0} ...".L10N("Client:Main:AttemptJoin"), hg.RoomName)));
            isJoiningGame = true;
            gameOfLastJoinAttempt = hg;

            Channel gameChannel = backendManager.CreateChannel(hg.RoomName, hg.ChannelName, false, true, password);
            backendManager.AddChannel(gameChannel);

            if (hg.IsLoadedGame)
            {
                gameLoadingLobby.SetUp(false, hg.TunnelServer, gameChannel, hg.HostName);
                gameChannel.UserAdded += GameLoadingChannel_UserAdded;
                //gameChannel.MessageAdded += GameLoadingChannel_MessageAdded;
                gameChannel.InvalidPasswordEntered += GameChannel_InvalidPasswordEntered_LoadedGame;
                isJoiningGame = false;
            }
            else
            {
                gameLobby.SetUp(gameChannel, false, hg.MaxPlayers, hg.TunnelServer, hg.HostName, hg.Passworded, hg.SkillLevel);
                gameChannel.UserAdded += GameChannel_UserAdded;
                gameChannel.InvalidPasswordEntered += GameChannel_InvalidPasswordEntered_NewGame;
                gameChannel.InviteOnlyErrorOnJoin += GameChannel_InviteOnlyErrorOnJoin;
                gameChannel.ChannelFull += GameChannel_ChannelFull;
                gameChannel.TargetChangeTooFast += GameChannel_TargetChangeTooFast;
            }

            backendManager.SendCustomMessage(new QueuedMessage("JOIN " + hg.ChannelName + " " + password,
                QueuedMessageType.INSTANT_MESSAGE, 0));
        }

        private HostedCnCNetGame FindGameByChannelName(string channelName)
        {
            var game = lbGameList.HostedGames.Find(hg => ((HostedCnCNetGame)hg).ChannelName == channelName);
            if (game == null)
                return null;

            return (HostedCnCNetGame)game;
        }

        private void ClearGameJoinAttempt(Channel channel)
        {
            ClearGameChannelEvents(channel);
            gameLobby.Clear();
        }

        private void ClearGameChannelEvents(Channel channel)
        {
            channel.UserAdded -= GameChannel_UserAdded;
            channel.InvalidPasswordEntered -= GameChannel_InvalidPasswordEntered_NewGame;
            channel.InviteOnlyErrorOnJoin -= GameChannel_InviteOnlyErrorOnJoin;
            channel.ChannelFull -= GameChannel_ChannelFull;
            channel.TargetChangeTooFast -= GameChannel_TargetChangeTooFast;
            isJoiningGame = false;
        }

        private void GameChannel_UserAdded(object sender, Online.ChannelUserEventArgs e)
        {
            Channel gameChannel = (Channel)sender;

            if (e.User.IRCUser.Name == ProgramConstants.PLAYERNAME)
            {
                ClearGameChannelEvents(gameChannel);
                gameLobby.OnJoined();
                isInGameRoom = true;
                SetLogOutButtonText();
            }
        }

        private void GameChannel_InvalidPasswordEntered_NewGame(object sender, EventArgs e)
        {
            backendManager.MainChannel.AddMessage(new ChatMessage(Color.White, "Incorrect password!".L10N("Client:Main:PasswordWrong")));
            ClearGameJoinAttempt((Channel)sender);
        }

        private void GameChannel_InviteOnlyErrorOnJoin(object sender, EventArgs e)
        {
            backendManager.MainChannel.AddMessage(new ChatMessage(Color.White, "The selected game is locked!".L10N("Client:Main:GameLocked")));
            var channel = (Channel)sender;

            var game = FindGameByChannelName(channel.ChannelName);
            if (game != null)
            {
                game.Locked = true;
                SortAndRefreshHostedGames();
            }

            ClearGameJoinAttempt((Channel)sender);
        }

        private void GameChannel_ChannelFull(object sender, EventArgs e) =>
            // We'd do the exact same things here, so we can just call the method below
            GameChannel_InviteOnlyErrorOnJoin(sender, e);

        private void GameChannel_TargetChangeTooFast(object sender, MessageEventArgs e)
        {
            backendManager.MainChannel.AddMessage(new ChatMessage(Color.White, e.Message));
            ClearGameJoinAttempt((Channel)sender);
        }

        private void GameLoadingChannel_UserAdded(object sender, ChannelUserEventArgs e)
        {
            Channel gameLoadingChannel = (Channel)sender;

            if (e.User.IRCUser.Name == ProgramConstants.PLAYERNAME)
            {
                gameLoadingChannel.UserAdded -= GameLoadingChannel_UserAdded;
                gameLoadingChannel.InvalidPasswordEntered -= GameChannel_InvalidPasswordEntered_LoadedGame;

                gameLoadingLobby.OnJoined();
                isInGameRoom = true;
                isJoiningGame = false;
            }
        }

        private void GameChannel_InvalidPasswordEntered_LoadedGame(object sender, EventArgs e)
        {
            var channel = (Channel)sender;
            channel.UserAdded -= GameLoadingChannel_UserAdded;
            channel.InvalidPasswordEntered -= GameChannel_InvalidPasswordEntered_LoadedGame;
            gameLoadingLobby.Clear();
            isJoiningGame = false;
        }

        private void JoinUser(IRCUser user, IMessageView messageView) { }

        private void EnableUIWhenReady()
        {
            Logger.Log($"[SOSLobby] EnableUIWhenReady called, IsConnected: {backendManager.IsConnected}, MainChannel is null: {backendManager.MainChannel == null}, externalAccountService.IsLoggedIn: {externalAccountService?.IsLoggedIn}");
            
            if (backendManager.IsConnected)
            {
                if (backendManager.MainChannel != null)
                {
                    Logger.Log($"[SOSLobby] Enabling UI controls, MainChannel UIName: {backendManager.MainChannel.UIName}");
                    btnNewGame.AllowClick = true;
                    btnJoinGame.AllowClick = true;
                    tbChatInput.Enabled = true;
                    tbGameSearch.Enabled = true;
                    
                    currentChatChannel = backendManager.MainChannel;
                    SubscribeToChannelEvents(currentChatChannel);
                    
                    // 刷新玩家列表
                    RefreshPlayerList(this, EventArgs.Empty);
                    
                    // 刷新后端游戏列表
                    _ = RefreshBackendGamesAsync();
                    
                    Logger.Log("[SOSLobby] UI fully enabled and ready.");
                }
                else
                {
                    // 已连接但MainChannel为空，启用基本UI但显示等待状态
                    Logger.Log($"[SOSLobby] Backend connected but MainChannel is null, enabling basic UI with waiting state.");
                    
                    // 启用UI控件，但显示等待消息
                    btnNewGame.AllowClick = false;
                    btnJoinGame.AllowClick = false;
                    tbChatInput.Enabled = false; // 保持禁用，因为没有频道可以发送消息
                    tbGameSearch.Enabled = true; // 允许搜索（虽然可能没有游戏）
                    
                    // 在聊天区域显示等待消息
                    lbChatMessages.Clear();
                    lbChatMessages.AddMessage(new ChatMessage(Color.Yellow, "Connected to backend. Setting up lobby channel..."));
                    
                    // 在玩家列表显示等待消息
                    lbPlayerList.Clear();
                    lbPlayerList.AddItem("Connecting...");
                    
                    Logger.Log($"[SOSLobby] Basic UI enabled, waiting for channel setup... IsAttemptingConnection: {backendManager.IsAttemptingConnection}");
                }
            }
            else
            {
                // 未连接，UI保持禁用
                Logger.Log($"[SOSLobby] Backend not connected, UI remains disabled. IsAttemptingConnection: {backendManager.IsAttemptingConnection}");
                
                btnNewGame.AllowClick = false;
                btnJoinGame.AllowClick = false;
                tbChatInput.Enabled = false;
                tbGameSearch.Enabled = false;
                
                // 显示未连接状态
                lbChatMessages.Clear();
                lbChatMessages.AddMessage(new ChatMessage(Color.Yellow, "Not connected to backend. Please login or wait for connection."));
            }
        }

        private async void TryAutoConnect()
        {
            try
            {
                Logger.Log($"[SOSLobby] Attempting auto-connect to backend... externalAccountService.IsLoggedIn: {externalAccountService?.IsLoggedIn}, _playerIdentityService.IsLoggedIn(): {_playerIdentityService?.IsLoggedIn()}");
                await backendManager.ConnectAsync();
                Logger.Log("[SOSLobby] Auto-connect initiated successfully, waiting for Connected event...");
                // Connected event will be fired, which will call EnableUIWhenReady
            }
            catch (Exception ex)
            {
                Logger.Log($"[SOSLobby] Auto-connect failed: {ex.Message}");
                Logger.Log($"[SOSLobby] Stack trace: {ex.StackTrace}");
                // If auto-connect fails, show login window
                WindowManager.AddCallback(new Action(() =>
                {
                    if (accountLoginWindow != null)
                    {
                        accountLoginWindow.Enable();
                        Logger.Log("[SOSLobby] Auto-connect failed, showing login window.");
                    }
                }), null);
            }
        }

        private void SetLogOutButtonText()
        {
            btnLogout.Text = isInGameRoom ? 
                "Return to Lobby".L10N("Client:Main:ReturnToLobby") : 
                "Main Menu".L10N("Client:Main:MainMenu");
        }

        public void SwitchOn()
        {
            Logger.Log("[SOSLobby] === SwitchOn called ===");
            
            // 防止重复调用
            if (_switchOnCalled)
            {
                Logger.Log("[SOSLobby] SwitchOn already called, skipping duplicate call.");
                return;
            }
            
            _switchOnCalled = true;
            Enable();

            Logger.Log($"[SOSLobby] SwitchOn: backendManager.IsConnected = {backendManager.IsConnected}");
            Logger.Log($"[SOSLobby] SwitchOn: backendManager.IsAttemptingConnection = {backendManager.IsAttemptingConnection}");
            Logger.Log($"[SOSLobby] SwitchOn: externalAccountService.IsLoggedIn = {externalAccountService?.IsLoggedIn}");
            Logger.Log($"[SOSLobby] SwitchOn: backendManager.MainChannel is null = {backendManager.MainChannel == null}");

            if (backendManager.IsConnected)
            {
                // 已经连接，直接启用UI
                Logger.Log("[SOSLobby] SwitchOn: backend already connected, enabling UI.");
                EnableUIWhenReady();
            }
            else if (backendManager.IsAttemptingConnection)
            {
                // 正在尝试连接，等待连接完成
                Logger.Log("[SOSLobby] SwitchOn: backend is attempting connection, waiting...");
            }
            else
            {
                // 未连接且未尝试连接
                if (externalAccountService.IsLoggedIn)
                {
                    // 用户已经登录过，尝试自动连接
                    Logger.Log("[SOSLobby] SwitchOn: user is logged in, attempting auto-connect.");
                    TryAutoConnect();
                }
                else
                {
                    // 用户未登录，显示登录窗口
                    if (accountLoginWindow != null)
                    {
                        Logger.Log("[SOSLobby] SwitchOn: user not logged in, attempting to open login window...");
                        Logger.Log($"[SOSLobby] SwitchOn: accountLoginWindow type: {accountLoginWindow.GetType().Name}");
                        Logger.Log($"[SOSLobby] SwitchOn: accountLoginWindow.Parent is null: {accountLoginWindow.Parent == null}");
                        
                        try
                        {
                            // 尝试调用Open()方法，如果存在的话
                            var openMethod = accountLoginWindow.GetType().GetMethod("Open");
                            if (openMethod != null)
                            {
                                Logger.Log("[SOSLobby] SwitchOn: Calling accountLoginWindow.Open()");
                                openMethod.Invoke(accountLoginWindow, null);
                            }
                            else
                            {
                                Logger.Log("[SOSLobby] SwitchOn: Open() method not found, falling back to Enable()");
                                accountLoginWindow.Enable();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"[SOSLobby] SwitchOn: Failed to open login window: {ex.Message}");
                            Logger.Log($"[SOSLobby] SwitchOn: Falling back to Enable()");
                            accountLoginWindow.Enable();
                        }
                        
                        Logger.Log("[SOSLobby] SwitchOn: user not logged in, showing login window.");
                    }
                    else
                    {
                        Logger.Log("[SOSLobby] ERROR: accountLoginWindow is null, cannot show login window");
                    }
                }
            }

            SetLogOutButtonText();
            Logger.Log("[SOSLobby] === SwitchOn completed ===");
        }

        public void SwitchOff()
        {
            Logger.Log("[SOSLobby] === SwitchOff called ===");
            _switchOnCalled = false;
            Disable();
        }

        public string GetSwitchName() => "SOS Lobby".L10N("Client:Main:SOSLobby");
    }
}
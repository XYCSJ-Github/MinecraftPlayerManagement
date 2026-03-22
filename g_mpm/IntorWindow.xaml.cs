using g_mpm.Enums;
using g_mpm.Structs;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Smc = g_mpm.SharedMemoryConfig.SharedMemoryConfig;

namespace g_mpm
{
    public partial class IntorWindow : Window
    {
        // 共享内存相关字段
        private SharedMemoryLauncher _launcher;
        private DispatcherTimer _statusTimer;

        // 世界显示相关字段
        private List<WorldCardItem> worldCards = new List<WorldCardItem>();
        private double worldNextY = 50;  // 下一个世界卡片的Y坐标起始位置
        private WorldCardItem? _selectedWorldCard = null;  // 当前选中的世界卡片

        // 玩家显示相关字段
        private List<PlayerCardItem> playerCards = new List<PlayerCardItem>();
        private double playerNextY = 50;  // 下一个玩家卡片的Y坐标起始位置
        private PlayerCardItem? _selectedPlayerCard = null;  // 当前选中的玩家卡片

        public bool flish = false;

        public enum ButtonStatic : int
        {
            About = 1,
            Level2
        };

        public ButtonStatic bs;

        // 用于等待命令完成的TaskCompletionSource
        private TaskCompletionSource<bool>? _listWorldCompletionSource;
        private TaskCompletionSource<bool>? _listPlayerCompletionSource;

        public IntorWindow()
        {
            InitializeComponent();

            // 初始化共享内存组件
            InitializeLauncher();
            SetupStatusTimer();

            // 启动时自动完成初始化
            this.Loaded += async (s, e) => await InitializeOnStartup();
            this.Closing += Window_Closing;

            // 初始化PlayerGrid和WorldGrid
            InitializePlayerGrid();
            InitializeWorldGrid();

            Storyboard sb = (Storyboard)this.Resources["Intor"];
            sb.SpeedRatio = 0.8;
            sb.Begin();
        }

        #region 世界显示初始化

        /// <summary>
        /// 初始化PlayerGrid容器（显示世界存档）
        /// </summary>
        private void InitializePlayerGrid()
        {
            // 设置PlayerGrid的背景为透明
            PlayerGrid.Background = Brushes.Transparent;

            // 创建滚动视图包装PlayerGrid
            ScrollViewer scrollViewer = new ScrollViewer();
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;  // 隐藏水平滚动条
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;    // 隐藏垂直滚动条

            // 创建Canvas作为内容容器
            Canvas worldCanvas = new Canvas();
            worldCanvas.Width = 380;
            worldCanvas.Height = 400;
            worldCanvas.Background = Brushes.Transparent;  // Canvas背景透明

            scrollViewer.Content = worldCanvas;

            // 清空并添加新内容
            PlayerGrid.Children.Clear();
            PlayerGrid.Children.Add(scrollViewer);

            // 存储Canvas引用
            PlayerGrid.Tag = worldCanvas;
        }

        /// <summary>
        /// 获取世界显示Canvas
        /// </summary>
        private Canvas GetWorldCanvas()
        {
            if (PlayerGrid.Tag is Canvas canvas)
                return canvas;

            InitializePlayerGrid();
            return PlayerGrid.Tag as Canvas;
        }

        #endregion

        #region 玩家显示初始化

        /// <summary>
        /// 初始化WorldGrid容器（显示玩家信息）
        /// </summary>
        private void InitializeWorldGrid()
        {
            // 设置WorldGrid的背景为透明
            WorldGrid.Background = Brushes.Transparent;

            // 创建滚动视图包装WorldGrid
            ScrollViewer scrollViewer = new ScrollViewer();
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;  // 隐藏水平滚动条
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;    // 隐藏垂直滚动条

            // 创建Canvas作为内容容器
            Canvas playerCanvas = new Canvas();
            playerCanvas.Width = 380;
            playerCanvas.Height = 400;
            playerCanvas.Background = Brushes.Transparent;  // Canvas背景透明

            scrollViewer.Content = playerCanvas;

            // 清空并添加新内容
            WorldGrid.Children.Clear();
            WorldGrid.Children.Add(scrollViewer);

            // 存储Canvas引用
            WorldGrid.Tag = playerCanvas;
        }

        /// <summary>
        /// 获取玩家显示Canvas
        /// </summary>
        private Canvas GetPlayerCanvas()
        {
            if (WorldGrid.Tag is Canvas canvas)
                return canvas;

            InitializeWorldGrid();
            return WorldGrid.Tag as Canvas;
        }

        #endregion

        #region 世界卡片创建和管理

        /// <summary>
        /// 世界卡片项（包含Border和数据）- 改为public
        /// </summary>
        public class WorldCardItem
        {
            public Border Border { get; set; }
            public string WorldName { get; set; }
            public string Directory { get; set; }
            public bool IsSelected { get; set; }

            // 用于高亮显示的背景颜色
            private SolidColorBrush NormalBackground { get; set; } = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            private SolidColorBrush SelectedBackground { get; set; } = new SolidColorBrush(Color.FromRgb(200, 230, 255));

            public WorldCardItem(Border border, string worldName, string directory)
            {
                Border = border;
                WorldName = worldName;
                Directory = directory;
                IsSelected = false;
            }

            public void SetSelected(bool selected)
            {
                IsSelected = selected;
                Border.Background = selected ? SelectedBackground : NormalBackground;
            }

            public void ToggleSelected()
            {
                SetSelected(!IsSelected);
            }
        }

        /// <summary>
        /// 加载世界数据
        /// </summary>
        private void LoadWorldData(WorldDirectoriesNameList data)
        {
            // 清除现有项
            ClearAllWorlds();

            // 确保两个列表长度一致
            int count = Math.Min(data.world_directory_list.Count, data.world_name_list.Count);

            worldNextY = 50; // 重置Y坐标起始位置

            for (int i = 0; i < count; i++)
            {
                // 创建UI元素
                CreateWorldCard(data.world_name_list[i], data.world_directory_list[i]);
            }
        }

        /// <summary>
        /// 创建世界卡片
        /// </summary>
        private void CreateWorldCard(string name, string directory)
        {
            Canvas worldCanvas = GetWorldCanvas();
            if (worldCanvas == null) return;

            // 创建主容器Border（实现圆角矩形效果）
            Border border = new Border();
            border.Width = 350;
            border.Height = 80;
            border.CornerRadius = new CornerRadius(12);

            // 设置纯色背景
            border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            // 添加边框
            border.BorderBrush = new SolidColorBrush(Colors.Transparent);
            border.BorderThickness = new Thickness(1.5);

            // 添加阴影效果
            DropShadowEffect shadow = new DropShadowEffect();
            shadow.BlurRadius = 6;
            shadow.ShadowDepth = 2;
            shadow.Color = Colors.Black;
            shadow.Opacity = 0.3;
            border.Effect = shadow;

            // 创建内容容器
            StackPanel stackPanel = new StackPanel();
            stackPanel.Margin = new Thickness(8);
            stackPanel.VerticalAlignment = VerticalAlignment.Center;

            // 世界名称
            TextBlock nameText = new TextBlock();
            nameText.Text = name;
            nameText.FontSize = 14;
            nameText.FontWeight = FontWeights.Bold;
            nameText.Foreground = Brushes.Black;
            nameText.TextWrapping = TextWrapping.Wrap;
            nameText.TextAlignment = TextAlignment.Center;
            nameText.Margin = new Thickness(0, 0, 0, 5);

            // 路径显示（缩短过长路径）
            TextBlock dirText = new TextBlock();
            string displayPath = directory;
            if (displayPath.Length > 64)
            {
                displayPath = displayPath.Substring(0, 64) + "...";
            }
            dirText.Text = displayPath;
            dirText.FontSize = 9;
            dirText.Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            dirText.TextWrapping = TextWrapping.Wrap;
            dirText.TextAlignment = TextAlignment.Center;

            // 组装内容
            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(dirText);

            border.Child = stackPanel;

            // 设置位置（垂直排列，X坐标固定为10）
            Canvas.SetLeft(border, 10);
            Canvas.SetTop(border, worldNextY);

            // 添加到画布
            worldCanvas.Children.Add(border);

            // 创建卡片项并保存
            var cardItem = new WorldCardItem(border, name, directory);
            worldCards.Add(cardItem);

            // 添加选中事件
            border.MouseLeftButtonDown += (s, e) => OnWorldCardSelected(cardItem);

            // 播放入场动画（从下向上飞入）
            PlayEntranceAnimation(border);

            // 递增Y坐标（卡片高度80 + 间距10）
            worldNextY += 90;
        }

        /// <summary>
        /// 世界卡片选中事件处理
        /// </summary>
        private void OnWorldCardSelected(WorldCardItem selectedCard)
        {
            // 清除之前的选中状态
            if (_selectedWorldCard != null && _selectedWorldCard != selectedCard)
            {
                _selectedWorldCard.SetSelected(false);
            }

            // 切换当前卡片的选中状态
            selectedCard.ToggleSelected();
            _selectedWorldCard = selectedCard.IsSelected ? selectedCard : null;

            Debug.WriteLine($"选中世界: {(selectedCard.IsSelected ? selectedCard.WorldName : "无")}");
        }

        /// <summary>
        /// 清空所有世界卡片
        /// </summary>
        private void ClearAllWorlds()
        {
            Canvas worldCanvas = GetWorldCanvas();
            if (worldCanvas == null) return;

            foreach (var cardItem in worldCards)
            {
                PlayExitAnimation(cardItem.Border);
            }

            worldCards.Clear();
            _selectedWorldCard = null;
            worldNextY = 50;
        }

        /// <summary>
        /// 获取当前选中的世界
        /// </summary>
        public WorldCardItem? GetSelectedWorld()
        {
            return _selectedWorldCard;
        }

        #endregion

        #region 玩家卡片创建和管理

        /// <summary>
        /// 玩家卡片项（包含Border和数据）- 改为public
        /// </summary>
        public class PlayerCardItem
        {
            public Border Border { get; set; }
            public UserInfo UserInfo { get; set; }
            public bool IsSelected { get; set; }

            // 用于高亮显示的背景颜色
            private SolidColorBrush NormalBackground { get; set; } = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            private SolidColorBrush SelectedBackground { get; set; } = new SolidColorBrush(Color.FromRgb(200, 230, 255));

            public PlayerCardItem(Border border, UserInfo userInfo)
            {
                Border = border;
                UserInfo = userInfo;
                IsSelected = false;
            }

            public void SetSelected(bool selected)
            {
                IsSelected = selected;
                Border.Background = selected ? SelectedBackground : NormalBackground;
            }

            public void ToggleSelected()
            {
                SetSelected(!IsSelected);
            }
        }

        /// <summary>
        /// 加载玩家数据
        /// </summary>
        private void LoadPlayerData(List<UserInfo> players)
        {
            // 清除现有项
            ClearAllPlayers();

            playerNextY = 50; // 重置Y坐标起始位置

            for (int i = 0; i < players.Count; i++)
            {
                // 创建UI元素
                CreatePlayerCard(players[i]);
            }
        }

        /// <summary>
        /// 创建玩家卡片
        /// </summary>
        private void CreatePlayerCard(UserInfo player)
        {
            Canvas playerCanvas = GetPlayerCanvas();
            if (playerCanvas == null) return;

            // 创建主容器Border（实现圆角矩形效果）
            Border border = new Border();
            border.Width = 350;
            border.Height = 80;
            border.CornerRadius = new CornerRadius(12);

            // 设置纯色背景（白色）
            border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            // 添加边框
            border.BorderBrush = new SolidColorBrush(Colors.White);
            border.BorderThickness = new Thickness(1.5);

            // 添加阴影效果
            DropShadowEffect shadow = new DropShadowEffect();
            shadow.BlurRadius = 6;
            shadow.ShadowDepth = 2;
            shadow.Color = Colors.Black;
            shadow.Opacity = 0.3;
            border.Effect = shadow;

            // 创建内容容器
            StackPanel stackPanel = new StackPanel();
            stackPanel.Margin = new Thickness(8);
            stackPanel.VerticalAlignment = VerticalAlignment.Center;

            // 玩家昵称
            TextBlock nameText = new TextBlock();
            nameText.Text = player.user_name;
            nameText.FontSize = 14;
            nameText.FontWeight = FontWeights.Bold;
            nameText.Foreground = Brushes.Black;
            nameText.TextWrapping = TextWrapping.Wrap;
            nameText.TextAlignment = TextAlignment.Center;
            nameText.Margin = new Thickness(0, 0, 0, 5);

            // UUID显示 - 修改为64字符
            TextBlock uuidText = new TextBlock();
            string displayUuid = player.uuid;
            if (displayUuid.Length > 64)
            {
                displayUuid = displayUuid.Substring(0, 61) + "...";
            }
            uuidText.Text = $"UUID: {displayUuid}";
            uuidText.FontSize = 9;
            uuidText.Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            uuidText.TextWrapping = TextWrapping.Wrap;
            uuidText.TextAlignment = TextAlignment.Center;
            uuidText.Margin = new Thickness(0, 0, 0, 3);

            // 过期时间显示
            TextBlock expireText = new TextBlock();
            string displayExpire = player.expiresOn;
            if (displayExpire.Length > 18)
            {
                displayExpire = displayExpire.Substring(0, 15) + "...";
            }
            expireText.Text = $"过期: {displayExpire}";
            expireText.FontSize = 9;
            expireText.Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            expireText.TextWrapping = TextWrapping.Wrap;
            expireText.TextAlignment = TextAlignment.Center;

            // 组装内容
            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(uuidText);
            stackPanel.Children.Add(expireText);

            border.Child = stackPanel;

            // 设置位置（垂直排列，X坐标固定为10）
            Canvas.SetLeft(border, 10);
            Canvas.SetTop(border, playerNextY);

            // 添加到画布
            playerCanvas.Children.Add(border);

            // 创建卡片项并保存
            var cardItem = new PlayerCardItem(border, player);
            playerCards.Add(cardItem);

            // 添加选中事件
            border.MouseLeftButtonDown += (s, e) => OnPlayerCardSelected(cardItem);

            // 播放入场动画（从下向上飞入）
            PlayEntranceAnimation(border);

            // 递增Y坐标（卡片高度80 + 间距10）
            playerNextY += 90;
        }

        /// <summary>
        /// 玩家卡片选中事件处理
        /// </summary>
        private void OnPlayerCardSelected(PlayerCardItem selectedCard)
        {
            // 清除之前的选中状态
            if (_selectedPlayerCard != null && _selectedPlayerCard != selectedCard)
            {
                _selectedPlayerCard.SetSelected(false);
            }

            // 切换当前卡片的选中状态
            selectedCard.ToggleSelected();
            _selectedPlayerCard = selectedCard.IsSelected ? selectedCard : null;

            Debug.WriteLine($"选中玩家: {(selectedCard.IsSelected ? selectedCard.UserInfo.user_name : "无")}");
        }

        /// <summary>
        /// 清空所有玩家卡片
        /// </summary>
        private void ClearAllPlayers()
        {
            Canvas playerCanvas = GetPlayerCanvas();
            if (playerCanvas == null) return;

            foreach (var cardItem in playerCards)
            {
                PlayExitAnimation(cardItem.Border);
            }

            playerCards.Clear();
            _selectedPlayerCard = null;
            playerNextY = 50;
        }

        /// <summary>
        /// 获取当前选中的玩家
        /// </summary>
        public PlayerCardItem? GetSelectedPlayer()
        {
            return _selectedPlayerCard;
        }

        #endregion

        #region 动画效果

        /// <summary>
        /// 入场动画 - 从下向上飞入
        /// </summary>
        private void PlayEntranceAnimation(FrameworkElement element)
        {
            // 设置初始位置（在下方）
            TranslateTransform translateTransform = new TranslateTransform(0, 30);
            element.RenderTransform = translateTransform;
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            // 初始透明度为0
            element.Opacity = 0;

            // 向上移动动画
            DoubleAnimation translateYAnimation = new DoubleAnimation
            {
                From = 100,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // 透明度淡入动画
            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(50)
            };

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1.5);
            timer.Tick += (s, args) =>
            {
                translateTransform.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);
                element.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
                timer.Stop();
            };
            timer.Start();
        }

        /// <summary>
        /// 退出动画 - 从上至下飞出
        /// </summary>
        private void PlayExitAnimation(FrameworkElement element)
        {
            // 创建平移动画
            TranslateTransform translateTransform = new TranslateTransform(0, 0);
            element.RenderTransform = translateTransform;
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            // 向下移动动画
            DoubleAnimation translateYAnimation = new DoubleAnimation
            {
                From = 0,
                To = 80,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            // 透明度淡出动画
            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            // 动画完成后移除元素
            translateYAnimation.Completed += (s, e) =>
            {
                Canvas worldCanvas = GetWorldCanvas();
                worldCanvas?.Children.Remove(element);
            };

            translateTransform.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);
            element.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);

        }

        /// <summary>
        /// 悬停放大动画
        /// </summary>
        private void Border_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
            {
                ScaleTransform scaleTransform = new ScaleTransform(1, 1);
                border.RenderTransform = scaleTransform;
                border.RenderTransformOrigin = new Point(0.5, 0.5);

                DoubleAnimation scaleAnimation = new DoubleAnimation
                {
                    To = 1.05,
                    Duration = TimeSpan.FromMilliseconds(150),
                    AutoReverse = true
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

                // 阴影加强
                if (border.Effect is DropShadowEffect shadow)
                {
                    DoubleAnimation blurAnimation = new DoubleAnimation
                    {
                        To = 12,
                        Duration = TimeSpan.FromMilliseconds(150)
                    };
                    shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnimation);
                }
            }
        }

        private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Border border)
            {
                if (border.Effect is DropShadowEffect shadow)
                {
                    DoubleAnimation blurAnimation = new DoubleAnimation
                    {
                        To = 6,
                        Duration = TimeSpan.FromMilliseconds(150)
                    };
                    shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnimation);
                }
            }
        }

        #endregion

        #region 共享内存初始化

        private void InitializeLauncher()
        {
            var config = new Smc
            {
                EnableVerboseLogging = true,
                InitTimeout = 30000,
                ReplyTimeout = 5000
            };

            _launcher = new SharedMemoryLauncher(config);

            // 订阅所有事件
            _launcher.ReplyReceived += OnReplyReceived;
            _launcher.ErrorOccurred += OnErrorOccurred;
            _launcher.OutputReceived += OnOutputReceived;
            _launcher.ProgramStatusChanged += OnProgramStatusChanged;
            _launcher.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        private void SetupStatusTimer()
        {
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _statusTimer.Tick += (s, e) => UpdateStatusDisplay();
            _statusTimer.Start();
        }

        private void UpdateStatusDisplay()
        {
            // 状态更新逻辑
        }

        private async System.Threading.Tasks.Task InitializeOnStartup()
        {
            bool success = await LaunchAsync();

            if (success)
            {
                Debug.WriteLine("共享内存初始化成功");
                // 发送列出世界命令
                SendCommand(Command.LIST_WORLD);
                // 发送获取玩家信息命令
                SendCommand(Command.LIST_PLAYER);
            }
            else
            {
                Debug.WriteLine("共享内存初始化失败");
            }
        }

        #endregion

        #region 事件处理

        private void OnOutputReceived(object? sender, SharedMemoryFunc.OutputReceivedEventArgs e)
        {
            // 处理C++输出
        }

        private void OnReplyReceived(object? sender, SharedMemoryFunc.ReplyReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 处理回复数据
                if (e.Title != "")
                {
                    Path.Text = e.Title;
                }

                switch (e.DataType)
                {
                    case StructDataType.WDNL:
                        if (e.Data != null)
                        {
                            // 反序列化世界数据
                            WorldDirectoriesNameList wdnl = WorldDirectoriesNameList.FromBytes(e.Data);

                            Debug.WriteLine($"收到世界数据: 共 {wdnl.world_name_list.Count} 个世界");

                            // 加载并显示世界数据
                            LoadWorldData(wdnl);

                            // 为所有新创建的卡片添加鼠标事件
                            AddMouseEventsToWorldCards();

                            flish = true;

                            // 完成世界列表等待任务
                            _listWorldCompletionSource?.TrySetResult(true);
                        }
                        break;

                    case StructDataType.UI:
                        if (e.Data != null)
                        {
                            // 反序列化玩家数据
                            List<UserInfo> users = UserInfoListSerializer.FromBytes(e.Data);

                            Debug.WriteLine($"收到玩家数据: 共 {users.Count} 个玩家");

                            // 加载并显示玩家数据
                            LoadPlayerData(users);

                            // 为所有新创建的卡片添加鼠标事件
                            AddMouseEventsToPlayerCards();

                            flish = true;

                            // 完成玩家列表等待任务
                            _listPlayerCompletionSource?.TrySetResult(true);
                        }
                        break;

                    case StructDataType.PIWIL:
                        if (e.Data != null)
                        {
                            PlayerInWorldInfoList piwil = PlayerInWorldInfoList.FromBytes(e.Data);
                            Debug.WriteLine($"收到玩家在世界信息数据");
                        }
                        break;
                }
            });
        }

        /// <summary>
        /// 为所有世界卡片添加鼠标事件
        /// </summary>
        private void AddMouseEventsToWorldCards()
        {
            foreach (var cardItem in worldCards)
            {
                cardItem.Border.MouseEnter += Border_MouseEnter;
                cardItem.Border.MouseLeave += Border_MouseLeave;
            }
        }

        /// <summary>
        /// 为所有玩家卡片添加鼠标事件
        /// </summary>
        private void AddMouseEventsToPlayerCards()
        {
            foreach (var cardItem in playerCards)
            {
                cardItem.Border.MouseEnter += Border_MouseEnter;
                cardItem.Border.MouseLeave += Border_MouseLeave;
            }
        }

        private void OnErrorOccurred(object? sender, ErrorEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"错误: {e.ErrorMessage}");
            });
        }

        private void OnProgramStatusChanged(object? sender, SharedMemoryFunc.ProgramStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"程序状态: {e.OldStatus} -> {e.NewStatus}");
            });
        }

        private void OnConnectionStatusChanged(object? sender, SharedMemoryFunc.ConnectionStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"连接状态: {e.OldStatus} -> {e.NewStatus}");
            });
        }

        #endregion

        #region 公共方法 - 供界面调用

        public async System.Threading.Tasks.Task<bool> LaunchAsync(string? args = null)
        {
            try
            {
                return await _launcher.LaunchAsync(args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"启动异常: {ex.Message}");
                return false;
            }
        }

        public bool Stage1_InitializeSharedMemory()
        {
            try
            {
                return _launcher.Stage1_InitializeSharedMemory();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段1异常: {ex.Message}");
                return false;
            }
        }

        public bool Stage2_StartCppProcess(string? args = null)
        {
            try
            {
                return _launcher.Stage2_StartCppProcess(args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段2异常: {ex.Message}");
                return false;
            }
        }

        public async System.Threading.Tasks.Task<bool> Stage3_WaitForCppReadyAsync()
        {
            try
            {
                return await _launcher.Stage3_WaitForCppReadyAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段3异常: {ex.Message}");
                return false;
            }
        }

        public bool Stage4_StartReplyListener()
        {
            try
            {
                _launcher.Stage4_StartReplyListener();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"阶段4异常: {ex.Message}");
                return false;
            }
        }

        public bool SendCommand(Command command, string additional = "")
        {
            try
            {
                return _launcher.SendCommand(command, additional);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"发送命令异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送命令并异步等待响应
        /// </summary>
        public async System.Threading.Tasks.Task<bool> SendCommandAndWaitAsync(Command command, string additional = "", int timeoutMs = 5000)
        {
            try
            {
                var result = await _launcher.SendCommandAndWaitAsync(command, additional, timeoutMs);
                return result.Success;  // 注意：这里是 Success（大写S）
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"发送命令并等待异常: {ex.Message}");
                return false;
            }
        }

        public void Shutdown()
        {
            try
            {
                _launcher?.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理异常: {ex.Message}");
            }
        }

        public SharedMemoryLauncher GetLauncher() => _launcher;

        public bool IsRunning => _launcher?.IsRunning ?? false;

        public ConnectStatus ConnectStatus => _launcher?.ConnectStatus ?? ConnectStatus.NOT_INITIALIZED;

        public ProgramStatus ProgramStatus => _launcher?.ProgramStatus ?? ProgramStatus.STOP;

        #endregion

        #region 清理

        protected override void OnClosed(EventArgs e)
        {
            _statusTimer?.Stop();
            _launcher?.Dispose();
            base.OnClosed(e);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Storyboard storyboard = (Storyboard)this.Resources["Closing"];
            storyboard.Begin(this);

            _launcher?.Dispose();
        }

        #endregion

        #region 按钮事件

        private void button_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = false;
            Back.IsEnabled = true;
            OK.IsEnabled = false;
            Choose.IsEnabled = false;

            Path.Text = "";

            bs = ButtonStatic.About;
            Storyboard sb = (Storyboard)this.Resources["AboutIntor"];

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += (s, args) =>
            {
                sb.Begin(this);
                timer.Stop();
            };
            timer.Start();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            switch (bs)
            {
                case ButtonStatic.About:
                    {
                        Back.IsEnabled = false;
                        button.IsEnabled = true;
                        OK.IsEnabled = true;
                        Choose.IsEnabled = true;

                        Storyboard sb = (Storyboard)this.Resources["AboutIntorB"];

                        DispatcherTimer timer = new DispatcherTimer();
                        timer.Interval = TimeSpan.FromSeconds(0.1);
                        timer.Tick += (s, args) =>
                        {
                            sb.Begin(this);
                            timer.Stop();
                        };
                        timer.Start();
                        break;
                    }
                case ButtonStatic.Level2:
                    {
                        Back.IsEnabled = false;
                        button.IsEnabled = true;
                        OK.IsEnabled = true;
                        Choose.IsEnabled = true;

                        ClearAllPlayers();
                        ClearAllWorlds();

                        Storyboard sb = (Storyboard)this.Resources["Level2IntorB"];
                        sb.Begin();
                        break;
                    }

                default:
                    break;
            }
        }

        private async void OK_Click(object sender, RoutedEventArgs e)
        {
            if (Path.Text.Length > 0)
            {
                bs = ButtonStatic.Level2;

                Back.IsEnabled = true;
                OK.IsEnabled = false;
                Choose.IsEnabled = false;
                button.IsEnabled = false;

                Storyboard sb = (Storyboard)this.Resources["Level2Intor"];
                sb.Begin(this);
            }

            // 创建等待任务
            _listWorldCompletionSource = new TaskCompletionSource<bool>();
            _listPlayerCompletionSource = new TaskCompletionSource<bool>();

            // 发送第一个命令
            SendCommand(Command.LIST_WORLD);

            // 等待第一个命令完成
            await _listWorldCompletionSource.Task;

            // 第一个完成后发送第二个命令
            SendCommand(Command.LIST_PLAYER);
        }

        private void Choose_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog open = new OpenFolderDialog();
            open.ShowDialog();
            Path.Text = open.FolderName;

            SendCommand(Command.M_SET_PATH, Path.Text.ToString());
        }

        #endregion
    }
}